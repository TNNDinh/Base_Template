/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.ComponentHeader
{
    public abstract class ComponentHeaderItem<T> : ComponentHeaderItem where T : Object
    {
        protected override bool DrawButton(Rect rect, Object target)
        {
            return DrawButton(rect, (T)target);
        }

        protected abstract bool DrawButton(Rect rect, T target);

        protected override bool Validate(Object target)
        {
            T t = target as T;
            return t && Validate(t);
        }

        protected virtual bool Validate(T target) => true;
    }
}
