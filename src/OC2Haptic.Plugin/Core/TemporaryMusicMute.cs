using System;
namespace OC2Haptic.Core
{
    public sealed class TemporaryMusicMute
    {
        public const float MutedDb=-80;
        private readonly Func<float?> read;
        private readonly Func<float,bool> write;
        private float restore;
        public bool Active {get;private set;}
        public TemporaryMusicMute(Func<float?> read,Func<float,bool> write){this.read=read;this.write=write;}
        private bool Read(out float value)
        {float? result=read();value=result.GetValueOrDefault();return result.HasValue && !float.IsNaN(value) && !float.IsInfinity(value);}
        public bool Begin()
        {
            if(Active)return true;
            float value;if(!Read(out value) || !write(MutedDb))return false;
            restore=value;Active=true;return true;
        }
        public bool Tick()
        {
            if(!Active)return false;
            float value;if(!Read(out value))return false;
            if(Math.Abs(value-MutedDb)<.001f)return true;
            restore=value;return write(MutedDb);
        }
        public bool End()
        {
            if(!Active)return true;
            Active=false;float value;
            return Read(out value) && (Math.Abs(value-MutedDb)>=.001f || write(restore));
        }
    }
}
