using System;
namespace OC2Haptic.Core
{
    public enum ControllerBackendKind { XInput, SDL3, SteamInput }
    public sealed class OutputDevice
    {
        public readonly ControllerBackendKind Backend;
        public readonly string Port,PhysicalKey;
        public readonly bool IdentityVerified;
        public OutputDevice(ControllerBackendKind backend,string port,string physicalKey,bool identityVerified)
        {Backend=backend;Port=port;PhysicalKey=physicalKey;IdentityVerified=identityVerified && !string.IsNullOrEmpty(physicalKey);}
    }
    // Plugin-local ownership contract. An unidentified XInput slot is not a stable physical identity.
    public sealed class OutputDeviceRegistry
    {
        private readonly OutputDevice[] owners=new OutputDevice[4];
        public bool TryClaim(int slot,OutputDevice device)
        {
            if(slot<0 || slot>=owners.Length || device==null || string.IsNullOrEmpty(device.Port))return false;
            if(owners[slot]!=null)return Same(owners[slot],device);
            foreach(var existing in owners)
            {
                if(existing==null)continue;
                if(existing.Backend==device.Backend && existing.Port==device.Port)return false;
                if(existing.IdentityVerified && device.IdentityVerified && existing.PhysicalKey==device.PhysicalKey)return false;
                if(existing.Backend!=device.Backend && (!existing.IdentityVerified || !device.IdentityVerified))return false;
            }
            owners[slot]=device;return true;
        }
        private static bool Same(OutputDevice a,OutputDevice b)
        {return a.Backend==b.Backend && a.Port==b.Port && a.PhysicalKey==b.PhysicalKey && a.IdentityVerified==b.IdentityVerified;}
        // Caller must stop its output before releasing ownership.
        public void Release(int slot){if(slot>=0 && slot<owners.Length)owners[slot]=null;}
        public void Clear(){Array.Clear(owners,0,owners.Length);}
    }
}
