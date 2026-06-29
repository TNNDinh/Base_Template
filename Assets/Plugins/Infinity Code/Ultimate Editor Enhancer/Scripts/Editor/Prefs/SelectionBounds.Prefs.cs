/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer
{
    public static partial class Prefs
    {
        public static bool selectionBounds = true;
        public static KeyCode selectionBoundsKeyCode = KeyCode.CapsLock;
        public static EventModifiers selectionBoundsModifiers = EventModifiers.None;

        public class SelectionBoundsManager : StandalonePrefManager<SelectionBoundsManager>, IHasShortcutPref
        {
            public override IEnumerable<string> keywords
            {
                get
                {
                    return new[]
                    {
                        "Display Bounds of Selected Renderers"
                    };
                }
            }

            public override void Draw()
            {
                DrawFieldWithHotKey("Display Bounds of Selected Renderers", ref selectionBounds, ref selectionBoundsKeyCode, ref selectionBoundsModifiers, EditorStyles.label, 17);
            }

            public IEnumerable<Shortcut> GetShortcuts()
            {
                if (!selectionBounds) return new Shortcut[0];

                return new[]
                {
                    new Shortcut("Display Bounds of Selected Renderers", "Scene View", selectionBoundsModifiers, selectionBoundsKeyCode),
                };
            }

            public override void SetState(bool state)
            {
                base.SetState(state);
                
                selectionBounds = state;
            }
        }
    }
}
