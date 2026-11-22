using System;
using System.Collections.Generic;

namespace OC2Haptic.Core
{
    // Hook callbacks submit immutable observations; Update owns physical playback.
    public sealed class FeedbackQueue
    {
        private readonly Queue<Entry> entries=new Queue<Entry>();
        private readonly object gate=new object();
        public void Enqueue(GameEvent gameEvent,string correlation){lock(gate)entries.Enqueue(new Entry{Event=gameEvent,Correlation=correlation});}
        public void Clear(){lock(gate)entries.Clear();}
        public void Drain(Action<GameEvent,string> emit)
        {while(true){Entry entry;lock(gate){if(entries.Count==0)return;entry=entries.Dequeue();}emit(entry.Event,entry.Correlation);}}
        private struct Entry{internal GameEvent Event;internal string Correlation;}
    }
}
