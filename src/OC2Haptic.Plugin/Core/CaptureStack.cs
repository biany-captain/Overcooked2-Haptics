using System.Collections.Generic;
namespace OC2Haptic.Core
{
    public sealed class CaptureStack<T> where T:class
    {
        private readonly List<T> frames=new List<T>();
        public T Current {get{return frames.Count==0?null:frames[frames.Count-1];}}
        public void Push(T frame){frames.Add(frame);}
        public void Release(T frame){if(object.ReferenceEquals(Current,frame)&&frames.Count>0)frames.RemoveAt(frames.Count-1);}
        public void Clear(){frames.Clear();}
    }
}
