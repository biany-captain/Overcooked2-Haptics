using System;
using System.Globalization;
using System.Threading;
using OC2Haptic.Core;

namespace OC2Haptic.GameEvents
{
    // Diagnostic capture only. This adapter never emits haptic/audio events or writes game state.
    internal sealed class ScoreSourceProbe
    {
        private readonly Action<string> log;
        private readonly Func<double> now;
        private readonly int mainThreadId;
        private int generation, flowId;
        private long sequence;
        private LevelContext context;

        internal ScoreSourceProbe(Action<string> log, Func<double> now)
        {
            this.log = log; this.now = now;
            mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        internal Observation Before(ClientKitchenFlowControllerBase flow, KitchenFlowMessage message)
        {
            if (Thread.CurrentThread.ManagedThreadId != mainThreadId)
                throw new InvalidOperationException("Score probe callback is not on the Unity main thread; capture disabled.");
            if (message == null || (message.m_teamID != TeamID.One && message.m_teamID != TeamID.Two) ||
                (message.m_msgType != KitchenFlowMessage.MsgType.Delivery &&
                 message.m_msgType != KitchenFlowMessage.MsgType.OrderExpired &&
                 message.m_msgType != KitchenFlowMessage.MsgType.ScoreOnly)) return null;

            int currentId = flow.GetInstanceID();
            if (flowId != currentId) { Reset("client flow changed"); flowId = currentId; }
            if(context!=null && !context.IsCurrent()) {Reset("level/mode/player-count context changed");flowId=currentId;}
            if (context == null) context = ReadContext(flow);
            ClientTeamMonitor monitor = flow.GetMonitorForTeam(message.m_teamID);
            // Snapshot both sides by value: network messages and monitors may reuse mutable stats objects.
            return new Observation(++sequence, generation, flowId, now(), message,
                Sample.Read(monitor == null ? null : monitor.Score), Sample.Read(message.m_teamScore), context);
        }

        internal void After(ClientKitchenFlowControllerBase flow, Observation observation)
        {
            if (observation == null) return;
            if (Thread.CurrentThread.ManagedThreadId != mainThreadId)
                throw new InvalidOperationException("Score probe completion is not on the Unity main thread.");
            ClientTeamMonitor monitor = flow.GetMonitorForTeam(observation.Team);
            Sample after = Sample.Read(monitor == null ? null : monitor.Score);
            if (!ShouldLog(observation.Kind == KitchenFlowMessage.MsgType.ScoreOnly,
                Same(observation.Before, after), Same(observation.Payload, after))) return;
            string result = after == null || observation.Payload == null ? "Unavailable" :
                Same(observation.Payload, after) ? "AppliedClientState" : "PayloadClientMismatch";
            log("Score source probe | observation=" + observation.Sequence +
                " localRound=" + observation.Generation + ":" + observation.FlowId +
                " roundIdSource=local-boundary team=" + (int)observation.Team + " kind=" + observation.Kind +
                " success=" + observation.Success + " order=" + observation.OrderId +
                " wasCombo=" + observation.WasCombo + " tip=" + observation.Tip +
                " before={" + Format(observation.Before) + "} payload={" + Format(observation.Payload) +
                "} after={" + Format(after) + "} result=" + result +
                " timestampMs=" + observation.Timestamp.ToString("F1", CultureInfo.InvariantCulture) +
                " thread=" + mainThreadId + " source=ClientKitchenFlowControllerBase.ApplyServerEvent" +
                " confidence=CapturedClientState | " + observation.Context.Description);
        }

        internal void Reset(string reason)
        {
            if (context != null) log("Score probe boundary | localRound=" + generation + ":" + flowId + " reason=" + reason);
            generation++; flowId = 0; context = null;
        }

        private static LevelContext ReadContext(ClientKitchenFlowControllerBase flow)
        {
            GameSession session = GameUtils.GetGameSession();
            if (session == null || session.LevelSettings == null || session.LevelSettings.SceneDirectoryVarientEntry == null)
                return new LevelContext("thresholdCandidate=Unavailable reason=missing-current-session-or-variant",null,"unknown",false);
            var variant = session.LevelSettings.SceneDirectoryVarientEntry;
            GameProgress progress = session.Progress;
            SceneDirectoryData directory = progress == null ? null : progress.GetSceneDirectory();
            int levelId = ScoreProbeMetadata.FindLevelId(directory == null ? null : directory.Scenes, variant);
            bool hasBoundaries = directory != null && directory.Scenes != null && levelId >= 0 &&
                levelId < directory.Scenes.Length && directory.Scenes[levelId] != null && directory.Scenes[levelId].HasScoreBoundaries;
            var save = progress == null ? null : progress.SaveData;
            bool ngpForLevel = save != null && levelId >= 0 && save.IsNGPEnabledForLevel(levelId);
            bool ngpDialogShown = save != null && save.NewGamePlusDialogShown;
            // Match ClientCampaignFlowController.GetStarRating's two gates, not just the fourth score's existence.
            bool fourthAvailable = ngpForLevel && ngpDialogShown;
            bool contextMatches = hasBoundaries && variant.LevelConfig != null && variant.LevelConfig == flow.GetLevelConfig() &&
                flow is ClientCampaignFlowController && session.GameModeKind == GameModes.Kind.Campaign && variant.PlayerCount > 0;
            ScoreThresholds thresholds;
            string raw;
            bool usable = ScoreProbeMetadata.TryReadThresholds(variant, fourthAvailable, out thresholds, out raw) && contextMatches;
            string selected = "Unavailable";
            if (usable)
            {
                var values = new string[thresholds.Count];
                for (int i = 0; i < values.Length; i++) values[i] = thresholds.At(i).ToString(CultureInfo.InvariantCulture);
                selected = string.Join(",", values);
            }
            bool calibrated = contextMatches && typeof(GameUtils).Module.ModuleVersionId == new Guid("114d606a-29d5-472c-8ebe-c2c20fbe3be3");
            string description = "level=" + levelId + " dlc=" + session.DLC + " mode=" + session.GameModeKind +
                " flow=" + flow.GetType().Name + " scene=" + variant.SceneName + " playerCount=" + variant.PlayerCount +
                " hasScoreBoundaries=" + hasBoundaries + " contextMatches=" + contextMatches +
                " ngpForLevel=" + ngpForLevel + " ngpDialogShown=" + ngpDialogShown + " fourthAvailable=" + fourthAvailable +
                " rawThresholds=" + raw +
                " thresholdCandidate=" + selected + " calibratedBuild=" + calibrated + " thresholdSource=GameSession.LevelSettings.SceneDirectoryVarientEntry";
            int selectedPlayers=variant.PlayerCount;
            var selectedMode=session.GameModeKind;
            return new LevelContext(description, usable ? thresholds : null,
                session.DLC+":"+levelId+":"+selectedMode+":"+selectedPlayers,calibrated,
                ()=>session!=null && session.LevelSettings!=null &&
                    object.ReferenceEquals(session.LevelSettings.SceneDirectoryVarientEntry,variant) &&
                    variant.PlayerCount==selectedPlayers && session.GameModeKind==selectedMode);
        }

        internal static bool ShouldLog(bool scoreOnly, bool beforeMatchesAfter, bool payloadMatchesAfter)
        {
            return !scoreOnly || !beforeMatchesAfter || !payloadMatchesAfter;
        }

        internal static bool Same(Sample first, Sample second)
        {
            return first != null && second != null && first.Base == second.Base && first.Tips == second.Tips &&
                first.Deductions == second.Deductions && first.Multiplier == second.Multiplier &&
                first.Combo == second.Combo && first.Deliveries == second.Deliveries && first.Maintained == second.Maintained;
        }

        private static string Format(Sample sample)
        {
            return sample == null ? "Unavailable" : "score=" + sample.Score + " base=" + sample.Base + " tips=" + sample.Tips +
                " deductions=" + sample.Deductions + " rawMultiplier=" + sample.Multiplier +
                " rawTotalCombo=" + sample.Combo + " successfulDeliveries=" + sample.Deliveries + " comboMaintained=" + sample.Maintained;
        }

        internal sealed class Sample
        {
            internal readonly int Score, Base, Tips, Deductions, Multiplier, Combo, Deliveries;
            internal readonly bool Maintained;
            private Sample(TeamMonitor.TeamScoreStats stats)
            {
                Score = stats.GetTotalScore(); Base = stats.TotalBaseScore; Tips = stats.TotalTipsScore;
                Deductions = stats.TotalTimeExpireDeductions; Multiplier = stats.TotalMultiplier;
                Combo = stats.TotalCombo; Deliveries = stats.TotalSuccessfulDeliveries; Maintained = stats.ComboMaintained;
            }
            internal static Sample Read(TeamMonitor.TeamScoreStats stats) { return stats == null ? null : new Sample(stats); }
        }

        internal sealed class Observation
        {
            internal readonly long Sequence;
            internal readonly int Generation, FlowId, Tip;
            internal readonly uint OrderId;
            internal readonly double Timestamp;
            internal readonly TeamID Team;
            internal readonly KitchenFlowMessage.MsgType Kind;
            internal readonly bool Success, WasCombo;
            internal readonly Sample Before, Payload;
            internal readonly LevelContext Context;
            internal Observation(long sequence, int generation, int flowId, double timestamp, KitchenFlowMessage message,
                Sample before, Sample payload, LevelContext context)
            {
                Sequence = sequence; Generation = generation; FlowId = flowId; Timestamp = timestamp;
                Team = message.m_teamID; Kind = message.m_msgType; Success = message.m_success;
                OrderId = message.m_orderID.m_id; WasCombo = message.m_wasCombo; Tip = message.m_tip;
                Before = before; Payload = payload; Context = context;
            }
        }

        internal sealed class LevelContext
        {
            internal readonly string Description, Key;
            internal readonly ScoreThresholds Thresholds;
            internal readonly bool Calibrated;
            private readonly Func<bool> current;
            internal bool IsCurrent(){return current==null || current();}
            internal LevelContext(string description,ScoreThresholds thresholds,string key,bool calibrated,Func<bool> current=null)
            {Description=description;Thresholds=thresholds;Key=key;Calibrated=calibrated;this.current=current;}
        }
    }
}
