using System;
using System.Collections.Generic;
using OC2Haptic.Core;
using OC2Haptic.XInput;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--devices")
        {
            var native = new XInputBackend(Console.WriteLine);
            for (int index = 0; index < 4; index++)
                Console.WriteLine("XInput slot=" + index + " connected=" + native.IsConnected(index) + " code=" + native.LastError);
            return 0;
        }
        var tests = new Dictionary<string, Action>
        {
            { "throw pulse lasts 65 ms and applies master multiplier", ThrowPulse },
            { "test pulse lasts 80 ms", TestPulse },
            { "cooldown survives completed pulse", CooldownAfterCompletion },
            { "duplicate events within 20 ms do not repeat output", Duplicates },
            { "higher priority interrupts without replaying the old pulse", Priority },
            { "lower priority cannot replace an active pulse", LowerPriority },
            { "multi-segment pattern includes a silent gap", PatternGap },
            { "late tick stops instead of replaying stale segments", LateTick },
            { "zero master intensity produces no nonzero command", ZeroIntensity },
            { "stop clears pending effects and only touches owned slots", StopOwned },
            { "disconnect clears pending effect before reconnect", Disconnect },
            { "disconnect observed by Play clears old pulse even during cooldown", DisconnectDuringPlayCooldown },
            { "disconnect observed by a new event clears old pulse", DisconnectDuringPlayNewEvent },
            { "stop retires ownership before an unrelated controller can reuse a slot", RetireOwnership },
            { "disconnect retires ownership before a later stop", DisconnectRetiresOwnership },
            { "invalid controller indices never reach backend", InvalidIndices },
            { "nonfinite master intensity is rejected", NonfiniteIntensity },
            { "pattern rejects negative or excessive durations", InvalidPatterns },
            { "matched local gamepad resolves selected controller", MatchedRouting },
            { "unbound or mismatched gamepad never resolves", UnboundRouting },
            { "keyboard remote unknown and input-intent events never resolve", UnsafeRouting },
            { "native backend rejects invalid indices and nonfinite strengths", NativeBoundary },
            { "v16 candidate catch profile has two pulses within205ms", CatchProfile },
            { "stage C successful delivery has two pulses and a silent gap", DeliveryProfile },
            { "v16 rejected delivery is430ms fading low motor feedback", FailedProfile },
            { "v16 expired order is505ms separated low motor feedback", ExpiredProfile },
            { "stage C team routing rejects wrong or unknown local team", TeamRouting },
            { "stage C catch retains personal binding rules", CatchRouting },
            { "stage C invalid-order failures can repeat after cooldown", InvalidOrderFailure },
            { "stage C expiry interrupts success gap without replay", ExpiryInterruptsGap },
            { "stage C repeated order callback cannot duplicate a pulse", DuplicateOrder },
            { "menu opening does not automatically vibrate", MenuOpenNoRumble },
            { "menu previews require open menu and global enable", MenuPreviewGate },
            { "menu event preview uses the existing double-pulse profile", MenuEventPreview },
            { "menu generic test stops at 80ms on real-time ticks", MenuGenericPreview },
            { "menu closing cancels pending preview with no replay", MenuCloseStops },
            { "menu invalid or unbound output never reaches backend", MenuInvalidOutput },
            { "menu input ownership change releases old participant immediately", MenuInputReassignment },
            { "menu unchanged participant keeps one owned token", MenuInputRetained },
            { "menu cleanup releases remaining participants after one release fails", MenuInputReleaseFailure },
            { "score probe accepts positive strictly increasing level thresholds", ProbeValidThresholds },
            { "score probe rejects missing invalid or unordered thresholds", ProbeInvalidThresholds },
            { "score probe keeps threshold snapshots independent of mutable input", ProbeThresholdSnapshot },
            { "score probe requires explicit four-star availability", ProbeFourStarGate }
        };
        V13Tests.Add(tests);
        MenuStrengthTests.Add(tests);
        SoundPackTests.Add(tests);
        V14Tests.Add(tests);
        V16Tests.Add(tests);
        V17Tests.Add(tests);
        MusicMuteTests.Add(tests);
        CoordinatorTests.Add(tests);
        HazardTests.Add(tests);
        int failed = 0;
        int run = 0;
        foreach (var test in tests)
        {
            if (args.Length != 0 && test.Key.IndexOf(args[0], StringComparison.OrdinalIgnoreCase) < 0) continue;
            run++;
            try { test.Value(); Console.WriteLine("PASS " + test.Key); }
            catch (Exception error) { failed++; Console.WriteLine("FAIL " + test.Key + ": " + error.Message); }
        }
        Console.WriteLine("RESULT " + (run - failed) + "/" + run + " passed");
        return failed == 0 && run > 0 ? 0 : 1;
    }

    private static HapticPattern Pulse(float left, float right, int duration)
    {
        return new HapticPattern(new HapticSegment(left, right, duration));
    }

    private static void ThrowPulse()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b);
        Check(e.Play(0, "throw", Pulse(.15f, .32f, 65), 20, 120, 1000, .75f), "throw was rejected");
        e.Tick(1000); Check(b.Last.Index == 0, "wrong controller");
        Near(.1125f, b.Last.Left); Near(.24f, b.Last.Right);
        e.Tick(1064); Near(.24f, b.Last.Right);
        e.Tick(1065); Near(0, b.Last.Left); Near(0, b.Last.Right);
    }

    private static void TestPulse()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b);
        e.Play(2, "test", Pulse(.22f, .28f, 80), 10, 120, 0, .75f);
        e.Tick(0); Near(.165f, b.Last.Left); Near(.21f, b.Last.Right);
        e.Tick(79); Near(.21f, b.Last.Right);
        e.Tick(80); Near(0, b.Last.Right);
    }

    private static void CooldownAfterCompletion()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b);
        e.Play(0, "throw", Pulse(.1f, .2f, 65), 20, 120, 0, 1); e.Tick(65);
        Check(!e.Play(0, "throw", Pulse(.1f, .2f, 65), 20, 120, 100, 1), "cooldown was lost at pulse end");
        Check(e.Play(0, "throw", Pulse(.1f, .2f, 65), 20, 120, 120, 1), "boundary should allow next throw");
    }

    private static void Duplicates()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b); var p = Pulse(.1f, .2f, 65);
        e.Play(0, "throw", p, 20, 120, 0, 1); e.Tick(0);
        Check(!e.Play(0, "throw", p, 20, 120, 10, 1), "duplicate 1 accepted");
        Check(!e.Play(0, "throw", p, 20, 120, 20, 1), "duplicate 2 accepted");
        e.Tick(20); Check(b.Commands.Count == 1, "unchanged motor output was rewritten");
        e.Tick(65); Check(b.Commands.Count == 2, "expected one pulse and one stop");
    }

    private static void Priority()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b);
        e.Play(0, "ordinary", Pulse(.1f, .1f, 300), 20, 0, 0, 1); e.Tick(0);
        Check(e.Play(0, "priority", Pulse(.4f, .5f, 80), 90, 0, 20, 1), "priority rejected");
        e.Tick(20); Near(.5f, b.Last.Right); e.Tick(100); Near(0, b.Last.Right);
        int count = b.Commands.Count; e.Tick(200); Check(count == b.Commands.Count, "old effect replayed");
    }

    private static void LowerPriority()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b);
        e.Play(0, "priority", Pulse(.4f, .5f, 100), 90, 0, 0, 1); e.Tick(0);
        Check(!e.Play(0, "ordinary", Pulse(.1f, .1f, 50), 20, 0, 10, 1), "lower priority accepted");
        e.Tick(10); Near(.5f, b.Last.Right);
    }

    private static void PatternGap()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b);
        var p = new HapticPattern(new HapticSegment(0, .2f, 70), new HapticSegment(0, 0, 80), new HapticSegment(0, .24f, 70));
        e.Play(0, "segments", p, 20, 0, 0, 1); e.Tick(0); Near(.2f, b.Last.Right);
        e.Tick(70); Near(0, b.Last.Right); e.Tick(150); Near(.24f, b.Last.Right);
        e.Tick(220); Near(0, b.Last.Right);
    }

    private static void LateTick()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b);
        e.Play(0, "test", Pulse(.2f, .3f, 80), 10, 0, 0, 1); e.Tick(200);
        Check(b.NonzeroCount == 0, "expired pulse was played");
    }

    private static void ZeroIntensity()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b);
        e.Play(0, "test", Pulse(.2f, .3f, 80), 10, 0, 0, 0); e.Tick(0); e.Tick(80);
        Check(b.NonzeroCount == 0, "zero multiplier vibrated");
    }

    private static void StopOwned()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b);
        e.Play(2, "test", Pulse(.2f, .3f, 80), 10, 0, 0, 1); e.Tick(0);
        e.StopAll(); Near(0, b.Last.Right); Check(b.Last.Index == 2, "stop used an unowned slot");
        foreach (var c in b.Commands) Check(c.Index == 2, "another user's controller was touched");
        int count = b.Commands.Count; e.Tick(1000); Check(count == b.Commands.Count, "stopped effect resumed");
        Check(e.Play(2, "test", Pulse(.2f, .3f, 80), 10, 120, 10, 1), "lifecycle stop did not clear cooldown");
    }

    private static void Disconnect()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b);
        e.Play(0, "test", Pulse(.2f, .3f, 80), 10, 0, 0, 1); e.Tick(0);
        b.Connected = false; e.Tick(10); b.Connected = true; e.Tick(20);
        Check(b.NonzeroCount == 1, "effect resumed after reconnect"); Near(0, b.Last.Right);
    }

    private static void DisconnectDuringPlayCooldown() { DisconnectDuringPlay("test"); }
    private static void DisconnectDuringPlayNewEvent() { DisconnectDuringPlay("other"); }

    private static void DisconnectDuringPlay(string nextKey)
    {
        var b = new FakeBackend(); var e = new HapticEngine(b); var p = Pulse(.2f, .3f, 80);
        e.Play(0, "test", p, 10, 120, 0, 1); e.Tick(0);
        b.Connected = false;
        Check(!e.Play(0, nextKey, p, 20, 120, 10, 1), "disconnected event accepted");
        b.Connected = true; e.Tick(20);
        Near(0, b.Last.Right);
        Check(b.NonzeroCount == 1, "old pulse replayed after Play observed disconnect");
    }

    private static void RetireOwnership()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b);
        e.Play(0, "test", Pulse(.2f, .3f, 80), 10, 0, 0, 1); e.Tick(0); e.StopAll();
        int afterFinalZero = b.Commands.Count;
        e.StopAll();
        Check(b.Commands.Count == afterFinalZero, "released slot received another stop");
        e.Play(2, "test", Pulse(.2f, .3f, 80), 10, 0, 10, 1); e.Tick(10); e.StopAll();
        for (int i = afterFinalZero; i < b.Commands.Count; i++) Check(b.Commands[i].Index == 2, "old slot was touched after reassignment");
    }

    private static void DisconnectRetiresOwnership()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b);
        e.Play(0, "test", Pulse(.2f, .3f, 80), 10, 0, 0, 1); e.Tick(0);
        b.Connected = false; e.Tick(10); int afterDisconnect = b.Commands.Count;
        b.Connected = true; e.StopAll();
        Check(b.Commands.Count == afterDisconnect, "disconnected slot remained owned");
    }

    private static void InvalidIndices()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b); var p = Pulse(.2f, .3f, 80);
        Check(!e.Play(-1, "test", p, 10, 0, 0, 1), "-1 accepted");
        Check(!e.Play(4, "test", p, 10, 0, 0, 1), "4 accepted");
        e.Tick(0); Check(b.ConnectionChecks == 0 && b.Commands.Count == 0, "invalid index reached backend");
    }

    private static void NonfiniteIntensity()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b); var p = Pulse(.2f, .3f, 80);
        foreach (float value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -1f, 2f })
            Check(!e.Play(0, "test", p, 10, 0, 0, value), "invalid intensity accepted");
        Check(b.Commands.Count == 0, "invalid intensity reached device");
    }

    private static void InvalidPatterns()
    {
        Throws(delegate { new HapticSegment(.1f, .2f, -1); });
        Throws(delegate { new HapticSegment(float.NaN, .2f, 65); });
        Throws(delegate { new HapticPattern(new HapticSegment(.1f, .2f, 501)); });
        Throws(delegate { new HapticPattern(new HapticSegment(.1f, .2f, 300), new HapticSegment(.1f, .2f, 300)); });
    }

    private static GameEvent LocalEvent()
    {
        return new GameEvent(1000, 0, 3, 42, true, false, EventConfidence.ConfirmedState, "StartFlight");
    }

    private static void MatchedRouting()
    {
        Check(EventRouter.Resolve(LocalEvent(), new ControllerBinding(3, 2)) == 2, "manual mapping ignored");
    }

    private static void UnboundRouting()
    {
        Check(EventRouter.Resolve(LocalEvent(), new ControllerBinding(-1, 2)) == -1, "unbound pad resolved");
        Check(EventRouter.Resolve(LocalEvent(), new ControllerBinding(3, -1)) == -1, "unbound device resolved");
        Check(EventRouter.Resolve(LocalEvent(), new ControllerBinding(4, 2)) == -1, "other pad resolved");
    }

    private static void UnsafeRouting()
    {
        var binding = new ControllerBinding(3, 0);
        Check(EventRouter.Resolve(new GameEvent(0, 0, 3, 1, true, true, EventConfidence.ConfirmedState, "test"), binding) == -1, "keyboard resolved");
        Check(EventRouter.Resolve(new GameEvent(0, 0, 3, 1, false, false, EventConfidence.ConfirmedState, "test"), binding) == -1, "remote resolved");
        Check(EventRouter.Resolve(new GameEvent(0, -1, -1, 1, true, false, EventConfidence.ConfirmedState, "test"), binding) == -1, "unknown identity resolved");
        Check(EventRouter.Resolve(new GameEvent(0, 0, 3, 1, true, false, EventConfidence.InputIntent, "test"), binding) == -1, "intent resolved as success");
    }

    private static void NativeBoundary()
    {
        var native = new XInputBackend(null);
        Check(!native.IsConnected(-1) && !native.IsConnected(4), "invalid native connection index accepted");
        Check(!native.SetMotors(-1, .2f, .3f) && !native.SetMotors(4, .2f, .3f), "invalid native output index accepted");
        Check(!native.SetMotors(0, float.NaN, .3f), "NaN crossed native boundary");
        Check(!native.SetMotors(0, .2f, float.PositiveInfinity), "Infinity crossed native boundary");
    }

    private static GameEvent TeamEvent(GameEventType type, int order, int? sourceTeam, int? localTeam, double now = 0)
    {
        return new GameEvent(type, EventScope.LocalTeam, now, 0, 3, order, true, false,
            EventConfidence.ConfirmedState, "kitchen", sourceTeam, localTeam);
    }

    private static void CatchProfile()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b); var f = EventFeedback.For(GameEventType.CatchSuccess);
        Check(f.Priority == 20 && f.CooldownMs == 300, "catch policy changed");
        e.Play(0, "catch", f.Pattern, f.Priority, f.CooldownMs, 0, .75f); e.Tick(0);
        Near(0, b.Last.Left); Near(.24f, b.Last.Right);
        e.Tick(65); Near(0,b.Last.Right);e.Tick(125);Near(.2925f,b.Last.Right);
        e.Tick(204);Near(.2925f,b.Last.Right);e.Tick(205);Near(0,b.Last.Right);
    }

    private static void DeliveryProfile()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b); var f = EventFeedback.For(GameEventType.DeliverySuccess);
        Check(f.Priority == 30, "success priority changed");
        e.Play(0, "delivery", f.Pattern, f.Priority, f.CooldownMs, 0, .75f); e.Tick(0);
        Near(0, b.Last.Left); Near(.15f, b.Last.Right);
        e.Tick(70); Near(0, b.Last.Right); e.Tick(149); Near(0, b.Last.Right);
        e.Tick(150); Near(.18f, b.Last.Right); e.Tick(220); Near(0, b.Last.Right);
    }

    private static void FailedProfile()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b); var f = EventFeedback.For(GameEventType.DeliveryFailure);
        Check(f.Priority == 80, "failure priority changed");
        e.Play(0, "failure", f.Pattern, f.Priority, f.CooldownMs, 0, .75f); e.Tick(0);
        Near(.54f, b.Last.Left); Near(.015f, b.Last.Right);
        e.Tick(210); Near(.3525f,b.Last.Left);Near(0,b.Last.Right);
        e.Tick(305);Near(.18f,b.Last.Left);e.Tick(429);Near(.18f,b.Last.Left);e.Tick(430);Near(0,b.Last.Left);
    }

    private static void ExpiredProfile()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b); var f = EventFeedback.For(GameEventType.OrderExpired);
        Check(f.Priority == 83, "expiry priority changed");
        e.Play(0, "expiry", f.Pattern, f.Priority, f.CooldownMs, 0, .75f); e.Tick(0);
        Near(.4575f, b.Last.Left); Near(.015f, b.Last.Right);
        e.Tick(190);Near(0,b.Last.Left);e.Tick(275);Near(.4125f,b.Last.Left);Near(0,b.Last.Right);
        e.Tick(504);Near(.4125f,b.Last.Left);e.Tick(505);Near(0,b.Last.Left);
    }

    private static void TeamRouting()
    {
        var binding = new ControllerBinding(3, 2);
        Check(EventRouter.Resolve(TeamEvent(GameEventType.DeliverySuccess, 4, 0, 0), binding) == 2, "own team did not route");
        Check(EventRouter.Resolve(TeamEvent(GameEventType.DeliverySuccess, 4, 1, 0), binding) == -1, "opponent team routed");
        Check(EventRouter.Resolve(TeamEvent(GameEventType.OrderExpired, 4, 0, null), binding) == -1, "unknown local team routed");
        Check(EventRouter.Resolve(TeamEvent(GameEventType.OrderExpired, 4, 2, 2), binding) == -1, "None team routed");
        Check(EventRouter.Resolve(TeamEvent(GameEventType.CatchSuccess, 4, 0, 0), binding) == -1, "personal event accepted as team event");
    }

    private static void CatchRouting()
    {
        var caught = new GameEvent(GameEventType.CatchSuccess, EventScope.LocalPlayer, 0, 0, 3, 40,
            true, false, EventConfidence.ConfirmedState, "catch");
        Check(EventRouter.Resolve(caught, new ControllerBinding(3, 1)) == 1, "catch binding failed");
        Check(EventRouter.Resolve(caught, new ControllerBinding(4, 1)) == -1, "other chef catch routed");
    }

    private static void InvalidOrderFailure()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b); var f = EventFeedback.For(GameEventType.DeliveryFailure);
        var first = TeamEvent(GameEventType.DeliveryFailure, -1, 0, 0);
        Check(e.Play(0, f.Key(first), f.Pattern, f.Priority, f.CooldownMs, 0, 1), "first invalid-order failure rejected");
        e.Tick(0); e.Tick(430);
        Check(e.Play(0, f.Key(TeamEvent(GameEventType.DeliveryFailure, -1, 0, 0, 1200)), f.Pattern, f.Priority, f.CooldownMs, 1200, 1), "invalid sentinel was deduped forever");
    }

    private static void ExpiryInterruptsGap()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b);
        var success = EventFeedback.For(GameEventType.DeliverySuccess); var expiry = EventFeedback.For(GameEventType.OrderExpired);
        e.Play(0, "success", success.Pattern, success.Priority, 120, 0, 1); e.Tick(0); e.Tick(70);
        Check(e.Play(0, "expiry", expiry.Pattern, expiry.Priority, 120, 100, 1), "expiry did not interrupt");
        e.Tick(150); Near(.61f, b.Last.Left); Near(.02f, b.Last.Right);
        e.Tick(350); Near(0, b.Last.Right);e.Tick(375);Near(.55f,b.Last.Left);e.Tick(605);Near(0,b.Last.Left);int count=b.Commands.Count;
        e.Tick(800); Check(b.Commands.Count == count, "success second pulse replayed");
    }

    private static void DuplicateOrder()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b); var f = EventFeedback.For(GameEventType.DeliverySuccess);
        var result = TeamEvent(GameEventType.DeliverySuccess, 42, 0, 0);
        Check(e.Play(0, f.Key(result), f.Pattern, f.Priority, f.CooldownMs, 0, 1), "first result rejected");
        e.Tick(0);
        Check(!e.Play(0, f.Key(result), f.Pattern, f.Priority, f.CooldownMs, 20, 1), "duplicate accepted");
        e.Tick(20); Check(b.NonzeroCount == 1, "duplicate restarted output");
    }

    private static void MenuOpenNoRumble()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b); var menu = new HapticMenuState(e);
        menu.Toggle(); Check(menu.IsOpen, "F6 did not open menu"); e.Tick(0);
        Check(b.Commands.Count == 0, "opening menu triggered vibration");
        menu.Toggle(); Check(!menu.IsOpen, "second F6 did not close menu");
    }

    private static void MenuPreviewGate()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b); var menu = new HapticMenuState(e);
        Check(!menu.Preview(null, 0, true, .75f, 0), "closed menu preview accepted");
        menu.Toggle();
        Check(!menu.Preview(GameEventType.CatchSuccess, 0, false, .75f, 0), "global-off preview accepted");
        Check(b.ConnectionChecks == 0 && b.Commands.Count == 0, "disabled preview reached backend");
        Check(menu.Preview(GameEventType.CatchSuccess, 0, true, .75f, 0), "enabled preview rejected");
    }

    private static void MenuEventPreview()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b); var menu = new HapticMenuState(e); menu.Toggle();
        Check(menu.Preview(GameEventType.DeliverySuccess, 0, true, .75f, 0), "manual success preview rejected");
        e.Tick(0); Near(.15f, b.Last.Right); e.Tick(70); Near(0, b.Last.Right);
        e.Tick(150); Near(.18f, b.Last.Right); e.Tick(220); Near(0, b.Last.Right);
    }

    private static void MenuGenericPreview()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b); var menu = new HapticMenuState(e); menu.Toggle();
        Check(menu.Preview(null, 0, true, .75f, 1000), "generic test rejected");
        e.Tick(1000); Near(.165f, b.Last.Left); Near(.21f, b.Last.Right);
        e.Tick(1079); Near(.21f, b.Last.Right); e.Tick(1080); Near(0, b.Last.Right);
    }

    private static void MenuCloseStops()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b); var menu = new HapticMenuState(e); menu.Toggle();
        menu.Preview(GameEventType.OrderExpired, 0, true, .75f, 0); e.Tick(0); Near(.4575f, b.Last.Left);
        menu.Close(); Near(0, b.Last.Left); int count = b.Commands.Count;
        e.Tick(500); Check(b.Commands.Count == count, "closed menu replayed a pulse");
    }

    private static void MenuInvalidOutput()
    {
        var b = new FakeBackend(); var e = new HapticEngine(b); var menu = new HapticMenuState(e); menu.Toggle();
        Check(!menu.Preview(null, -1, true, .75f, 0), "unbound preview accepted");
        Check(!menu.Preview(null, 4, true, .75f, 0), "invalid slot preview accepted");
        Check(b.ConnectionChecks == 0 && b.Commands.Count == 0, "invalid output touched backend");
    }

    private static void MenuInputReassignment()
    {
        var released = new List<int>(); var acquired = new List<int>(); var scope = new MenuInputScope<int>();
        Func<int, Action> acquire = id => { acquired.Add(id); return () => released.Add(id); };
        scope.Reconcile(new HashSet<int> { 1 }, acquire);
        scope.Reconcile(new HashSet<int> { 2 }, acquire);
        Check(released.Count == 1 && released[0] == 1, "old pad or remote participant retained its input lock");
        Check(acquired.Count == 2 && acquired[1] == 2, "new eligible participant not acquired");
        scope.Clear(); Check(released.Count == 2 && released[1] == 2, "current participant not released on close");
    }

    private static void MenuInputRetained()
    {
        int acquired = 0; int released = 0; var scope = new MenuInputScope<int>();
        Func<int, Action> acquire = id => { acquired++; return () => released++; };
        scope.Reconcile(new HashSet<int> { 1 }, acquire); scope.Reconcile(new HashSet<int> { 1 }, acquire);
        Check(acquired == 1 && released == 0, "unchanged participant churned input tokens");
        scope.Clear(); scope.Clear(); Check(released == 1, "released a token more than once");
    }

    private static void MenuInputReleaseFailure()
    {
        bool secondReleased = false; var scope = new MenuInputScope<int>();
        scope.Reconcile(new HashSet<int> { 1, 2 }, id => id == 1 ? (Action)(() => { throw new InvalidOperationException("release failed"); }) : (() => secondReleased = true));
        bool surfaced = false;
        try { scope.Clear(); } catch (InvalidOperationException) { surfaced = true; }
        Check(surfaced && secondReleased, "one failed release prevented cleanup of other local inputs");
        scope.Clear();
    }

    private static void ProbeValidThresholds()
    {
        ScoreThresholds thresholds;
        Check(ScoreThresholds.TryCreate(new[] { 100, 200, 300 }, out thresholds), "valid three-star set rejected");
        Check(thresholds.Count == 3 && thresholds.At(2) == 300, "three-star thresholds changed");
        Check(ScoreThresholds.TryCreate(new[] { 100, 200, 300, 500 }, out thresholds), "valid four-star set rejected");
        Check(thresholds.Count == 4 && thresholds.At(3) == 500, "four-star thresholds changed");
    }

    private static void ProbeInvalidThresholds()
    {
        foreach (int[] values in new int[][] { null, new int[0], new[] { 0, 100, 200 }, new[] { -1, 100 },
            new[] { 100, 100, 200 }, new[] { 200, 100, 300 }, new[] { 1, 2, 3, 4, 5 } })
        {
            ScoreThresholds thresholds;
            Check(!ScoreThresholds.TryCreate(values, out thresholds) && thresholds == null,
                "invalid threshold set became usable");
        }
    }

    private static void ProbeThresholdSnapshot()
    {
        var values = new[] { 100, 200, 300 };
        ScoreThresholds thresholds;
        Check(ScoreThresholds.TryCreate(values, out thresholds), "valid set rejected");
        values[1] = 999;
        Check(thresholds.At(1) == 200, "game/config mutation changed recorded thresholds");
    }

    private static void ProbeFourStarGate()
    {
        ScoreThresholds thresholds;
        Check(ScoreThresholds.TryForLevel(100, 200, 300, 500, false, out thresholds), "normal mode rejected");
        Check(thresholds.Count == 3, "four-star value bypassed NGP availability");
        Check(ScoreThresholds.TryForLevel(100, 200, 300, 500, true, out thresholds) && thresholds.Count == 4,
            "available fourth star lost");
        Check(!ScoreThresholds.TryForLevel(100, 200, 300, 0, true, out thresholds),
            "missing fourth threshold guessed");
    }

    private static void Throws(Action action)
    {
        try { action(); } catch (ArgumentException) { return; }
        throw new Exception("expected invalid input rejection");
    }

    private static void Near(float expected, float actual)
    {
        Check(Math.Abs(expected - actual) < .00001f, "expected " + expected + ", got " + actual);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class FakeBackend : IControllerRumbleBackend
    {
        public bool Connected = true;
        public int ConnectionChecks;
        public readonly List<Command> Commands = new List<Command>();
        public bool IsConnected(int index) { ConnectionChecks++; return Connected; }
        public bool SetMotors(int index, float left, float right)
        {
            Commands.Add(new Command { Index = index, Left = left, Right = right });
            return Connected;
        }
        public Command Last { get { Check(Commands.Count > 0, "no motor command"); return Commands[Commands.Count - 1]; } }
        public int NonzeroCount { get { return Commands.FindAll(c => c.Left != 0 || c.Right != 0).Count; } }
    }

    private sealed class Command { public int Index; public float Left; public float Right; }
}
