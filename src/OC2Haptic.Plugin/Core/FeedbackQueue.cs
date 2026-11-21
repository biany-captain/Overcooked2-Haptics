using System;
using System.Collections.Generic;

namespace OC2Haptic.Core
{
    // Hook callbacks submit immutable observations; Update owns physical playback.
    public sealed class FeedbackQueue
    {
        private readonly Queue<Entry> entries=new Queue<Entry>();
        public void Enqueue(GameEvent gameEvent,string correlation){entries.Enqueue(new Entry{Event=gameEvent,Correlation=correlation});}
        public void Clear(){entries.Clear();}
        public void Drain(Action<GameEvent,string> emit)
        {while(entries.Count>0){Entry entry=entries.Dequeue();emit(entry.Event,entry.Correlation);}}
        private struct Entry{internal GameEvent Event;internal string Correlation;}
    }
}
