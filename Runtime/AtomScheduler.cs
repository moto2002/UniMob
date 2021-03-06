using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace UniMob
{
    public class AtomScheduler : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly CustomSampler ProfilerSampler = CustomSampler.Create("UniMob.Sync");
#endif
        private static Queue<AtomBase> _updatingCurrentFrame = new Queue<AtomBase>();
        private static Queue<AtomBase> _updatingNextFrame = new Queue<AtomBase>();
        private static Queue<AtomBase> _reaping = new Queue<AtomBase>();

        private static AtomScheduler _current;
        private static bool _dirty;

        private void Update()
        {
            if (!_dirty)
            {
                return;
            }

            _dirty = false;

            Sync();
        }

        internal static void Actualize(AtomBase atom)
        {
            _dirty = true;
            _updatingNextFrame.Enqueue(atom);

            if (ReferenceEquals(_current, null) && Application.isPlaying)
            {
                var go = new GameObject(nameof(AtomScheduler));
                _current = go.AddComponent<AtomScheduler>();
                DontDestroyOnLoad(_current);
            }
        }

        internal static void Reap(AtomBase atom)
        {
            _dirty = true;
            atom.Reaping = true;
            _reaping.Enqueue(atom);
        }

        internal static void Unreap(AtomBase atom)
        {
            atom.Reaping = false;
        }

        public static void Sync()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ProfilerSampler.Begin();
#endif
            var toSwap = _updatingCurrentFrame;
            _updatingCurrentFrame = _updatingNextFrame;
            _updatingNextFrame = toSwap;

            while (_updatingCurrentFrame.Count > 0)
            {
                var atom = _updatingCurrentFrame.Dequeue();

                if (atom.IsActive && !atom.Reaping && atom.State != AtomBase.AtomState.Actual)
                {
                    atom.Actualize();
                }
            }

            while (_reaping.Count > 0)
            {
                var atom = _reaping.Dequeue();
                if (atom.Reaping && atom.Subscribers == null)
                {
                    atom.Deactivate();
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ProfilerSampler.End();
#endif
        }
    }
}