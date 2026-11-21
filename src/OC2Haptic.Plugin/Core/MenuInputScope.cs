using System;
using System.Collections.Generic;

namespace OC2Haptic.Core
{
    public sealed class MenuInputScope<T>
    {
        private readonly Dictionary<T, Action> owned = new Dictionary<T, Action>();
        private readonly List<T> retired = new List<T>();
        public void Reconcile(ICollection<T> eligible, Func<T, Action> acquire)
        {
            retired.Clear();
            foreach (T participant in owned.Keys)
                if (!eligible.Contains(participant)) retired.Add(participant);
            ReleaseRetired();
            foreach (T participant in eligible)
                if (!owned.ContainsKey(participant)) owned.Add(participant, acquire(participant));
        }
        public void Clear()
        {
            retired.Clear();
            retired.AddRange(owned.Keys);
            ReleaseRetired();
        }

        private void ReleaseRetired()
        {
            Exception firstError = null;
            foreach (T participant in retired)
            {
                Action release = owned[participant];
                owned.Remove(participant);
                // One external release failure must not leave other local controls locked.
                try { release(); }
                catch (Exception error) { if (firstError == null) firstError = error; }
            }
            if (firstError != null) throw new InvalidOperationException("A menu input release failed; remaining tokens were released.", firstError);
        }
    }
}
