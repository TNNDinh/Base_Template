using UnityEngine;

namespace BlackFace.Libraries.Modules.UIModule
{
    public sealed class FeatureClickBlock
    {
        private int _consumedFrame = -1;

        public bool IsLocked { get; private set; }

        public bool TryConsume()
        {
            if (IsLocked || (_consumedFrame != -1 && _consumedFrame <= Time.frameCount + 5))
                return false;

            _consumedFrame = Time.frameCount;
            return true;
        }

        public void Lock()
        {
            IsLocked = true;
        }

        public void Reset()
        {
            IsLocked = false;
            _consumedFrame = -1;
        }
    }
}