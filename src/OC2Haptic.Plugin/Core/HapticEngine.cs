using System;
using System.Collections.Generic;

namespace OC2Haptic.Core
{
    public interface IControllerRumbleBackend
    {
        bool IsConnected(int index);
        bool SetMotors(int index, float left, float right);
    }

    public sealed class HapticEngine
    {
        private readonly IControllerRumbleBackend backend;
        private readonly OutputState[] outputs = { new OutputState(), new OutputState(), new OutputState(), new OutputState() };

        public HapticEngine(IControllerRumbleBackend backend) { this.backend = backend; }

        public bool Play(int index, string eventKey, HapticPattern pattern, int priority,
            int cooldownMs, double nowMs, float masterIntensity)
        {
            if (index < 0 || index > 3 || string.IsNullOrEmpty(eventKey) || pattern == null ||
                cooldownMs < 0 || double.IsNaN(nowMs) || double.IsInfinity(nowMs) || nowMs < 0 ||
                float.IsNaN(masterIntensity) || float.IsInfinity(masterIntensity) || masterIntensity <= 0 || masterIntensity > 1)
                return false;
            OutputState state = outputs[index];
            if (!backend.IsConnected(index))
            {
                Finish(index);
                state.RecentEvents.Clear();
                return false;
            }
            double previous;
            if (state.RecentEvents.TryGetValue(eventKey, out previous) && nowMs - previous < cooldownMs) return false;
            if (state.Pattern != null && nowMs - state.StartedMs >= state.Pattern.TotalDurationMs) Finish(index);
            if (state.Pattern != null && priority <= state.Priority) return false;
            state.Pattern = pattern;
            state.StartedMs = nowMs;
            state.Priority = priority;
            state.Intensity = masterIntensity;
            state.RecentEvents[eventKey] = nowMs;
            return true;
        }

        public void Tick(double nowMs)
        {
            for (int index = 0; index < outputs.Length; index++)
            {
                OutputState state = outputs[index];
                if (state.Pattern == null) continue;
                if (!backend.IsConnected(index))
                {
                    Finish(index);
                    state.RecentEvents.Clear();
                    continue;
                }
                double elapsed = nowMs - state.StartedMs;
                if (elapsed < 0) continue;
                if (elapsed >= state.Pattern.TotalDurationMs) { Finish(index); continue; }
                for (int segmentIndex = 0; segmentIndex < state.Pattern.Count; segmentIndex++)
                {
                    HapticSegment segment = state.Pattern.SegmentAt(segmentIndex);
                    if (elapsed < segment.DurationMs)
                    {
                        if (!Write(index, segment.Left * state.Intensity, segment.Right * state.Intensity))
                        {
                            Finish(index);
                            state.RecentEvents.Clear();
                        }
                        break;
                    }
                    elapsed -= segment.DurationMs;
                }
            }
        }

        public void StopAll()
        {
            for (int index = 0; index < outputs.Length; index++)
            {
                Finish(index);
                outputs[index].RecentEvents.Clear();
            }
        }

        private void Finish(int index)
        {
            OutputState state = outputs[index];
            state.Pattern = null;
            if (state.Touched) backend.SetMotors(index, 0, 0);
            state.Touched = false;
            state.LastLeft = 0; state.LastRight = 0;
        }

        private bool Write(int index, float left, float right)
        {
            OutputState state = outputs[index];
            if (state.Touched && state.LastLeft == left && state.LastRight == right) return true;
            state.Touched = true;
            bool success = backend.SetMotors(index, left, right);
            if (success) { state.LastLeft = left; state.LastRight = right; }
            return success;
        }

        private sealed class OutputState
        {
            internal HapticPattern Pattern;
            internal double StartedMs;
            internal int Priority;
            internal float Intensity;
            internal bool Touched;
            internal float LastLeft;
            internal float LastRight;
            internal readonly Dictionary<string, double> RecentEvents = new Dictionary<string, double>();
        }
    }
}
