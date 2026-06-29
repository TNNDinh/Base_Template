/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.ComponentHeader
{
    public abstract class ComponentHeaderItem
    {
        protected bool initialized;
        protected virtual bool enabled => true;
        protected virtual bool isImportant => false;
        public abstract float order { get; }

        public bool Draw(Rect rect, Object[] targets)
        {
            if (!Prefs.componentExtraHeaderButtons) return false;
            if (!enabled) return false;
            if (targets.Length != 1) return false;

            if (!isImportant)
            {
                Event e = Event.current;
                if (e.mousePosition.y < rect.y || e.mousePosition.y > rect.yMax)
                {
                    return false;
                }
            }
            
            if (!Validate(targets[0])) return false;

            if (!initialized)
            {
                Initialize();
                initialized = true;
            }
            
            return DrawButton(rect, targets[0]);
        }
        
        protected abstract bool DrawButton(Rect rect, Object target);
        
        protected virtual void Initialize()
        {
            
        }

        protected virtual bool Validate(Object target) => true;
    }
}
