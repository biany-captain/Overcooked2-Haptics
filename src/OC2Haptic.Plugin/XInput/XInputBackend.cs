using System;
using System.Runtime.InteropServices;
using OC2Haptic.Core;

namespace OC2Haptic.XInput
{
    public sealed class XInputBackend : IControllerRumbleBackend
    {
        private readonly Action<string> report;
        private bool unavailable;
        public uint LastError { get; private set; }

        public XInputBackend(Action<string> report) { this.report = report; }

        public bool IsConnected(int index)
        {
            if (index < 0 || index > 3 || unavailable) return false;
            try
            {
                XInputState state;
                LastError = GetState((uint)index, out state);
                return LastError == 0;
            }
            catch (DllNotFoundException error) { Disable(error); return false; }
            catch (EntryPointNotFoundException error) { Disable(error); return false; }
            catch (BadImageFormatException error) { Disable(error); return false; }
        }

        public bool SetMotors(int index, float left, float right)
        {
            if (index < 0 || index > 3 || unavailable || float.IsNaN(left) || float.IsNaN(right) ||
                float.IsInfinity(left) || float.IsInfinity(right)) return false;
            var vibration = new XInputVibration
            {
                Left = (ushort)Math.Round(Math.Max(0, Math.Min(1, left)) * 65535),
                Right = (ushort)Math.Round(Math.Max(0, Math.Min(1, right)) * 65535)
            };
            try { LastError = SetState((uint)index, ref vibration); return LastError == 0; }
            catch (DllNotFoundException error) { Disable(error); return false; }
            catch (EntryPointNotFoundException error) { Disable(error); return false; }
            catch (BadImageFormatException error) { Disable(error); return false; }
        }

        private void Disable(Exception error)
        {
            unavailable = true;
            if (report != null) report("XInput output disabled: " + error.GetType().Name + ": " + error.Message);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XInputVibration { internal ushort Left; internal ushort Right; }

        [StructLayout(LayoutKind.Sequential)]
        private struct XInputGamepad
        {
            internal ushort Buttons;
            internal byte LeftTrigger;
            internal byte RightTrigger;
            internal short LeftX, LeftY, RightX, RightY;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XInputState { internal uint PacketNumber; internal XInputGamepad Gamepad; }

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        private static extern uint GetState(uint index, out XInputState state);
        [DllImport("xinput1_4.dll", EntryPoint = "XInputSetState")]
        private static extern uint SetState(uint index, ref XInputVibration vibration);
    }
}
