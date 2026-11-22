using System;
using System.Collections.Generic;
namespace OC2Haptic.Core
{
    // Tracks only sources selected by the caller. Failed restores stay owned for retry.
    public sealed class TemporaryMusicMute<T> where T:class
    {
        private readonly Func<T,bool> alive,music;
        private readonly Func<T,bool?> read;
        private readonly Func<T,bool,bool> write;
        private readonly Dictionary<T,bool> originals=new Dictionary<T,bool>();
        private readonly HashSet<T> selected=new HashSet<T>();
        private readonly List<T> finished=new List<T>();
        public int ActiveCount {get{return originals.Count;}}
        public bool Focused {get;private set;}
        public string LastError {get;private set;}
        public TemporaryMusicMute(Func<T,bool> alive,Func<T,bool> music,Func<T,bool?> read,Func<T,bool,bool> write)
        {this.alive=alive;this.music=music;this.read=read;this.write=write;Focused=true;}
        public bool SetFocused(bool focused){Focused=focused;return focused || End();}
        public bool Apply(IEnumerable<T> sources)
        {
            if(!Focused)return End();
            selected.Clear();LastError="";bool success=true;
            foreach(T source in sources)
            {
                if(source==null)continue;
                try
                {
                    if(!alive(source) || !music(source))continue;
                    selected.Add(source);bool? value=read(source);
                    if(!value.HasValue){LastError="source state unavailable";success=false;continue;}
                    if(!originals.ContainsKey(source))originals.Add(source,value.Value);
                    else if(!value.Value)originals[source]=false;
                    if(!value.Value && !write(source,true)){LastError="source mute refused";success=false;}
                }
                catch(Exception error){LastError=error.Message;success=false;}
            }
            return ReleaseUnselected() && success;
        }
        public bool End(){selected.Clear();LastError="";return ReleaseUnselected();}
        private bool ReleaseUnselected()
        {
            finished.Clear();bool success=true;
            foreach(var pair in originals)
            {
                if(selected.Contains(pair.Key))continue;
                try
                {
                    if(!alive(pair.Key)){finished.Add(pair.Key);continue;}
                    bool? current=read(pair.Key);
                    if(!current.HasValue){LastError="source restore state unavailable";success=false;continue;}
                    if(current.Value && !pair.Value && !write(pair.Key,false)){LastError="source restore refused";success=false;continue;}
                    finished.Add(pair.Key);
                }
                catch(Exception error){LastError=error.Message;success=false;}
            }
            foreach(T source in finished)originals.Remove(source);
            return success;
        }
    }
}
