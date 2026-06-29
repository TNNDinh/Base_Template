/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Collections.Generic;
using System.Linq;
using InfinityCode.UltimateEditorEnhancer.HierarchyTools;
using InfinityCode.UltimateEditorEnhancer.PostHeader;
using InfinityCode.UltimateEditorEnhancer.References;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer
{
    public class NoteManager : EditorWindow
    {
        private static Dictionary<GameObject, NoteItem> cachedNotes = new Dictionary<GameObject, NoteItem>();
        
        private static GUIContent copyContent;
        private static GUIContent selectContent;
        private static GUIContent removeContent;
        
        private Vector2 scrollPosition;
        private string filter;
        private Dictionary<GameObject, NoteItem> filteredNotes = new Dictionary<GameObject, NoteItem>();

        public static void ClearCache()
        {
            cachedNotes.Clear();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            EditorGUI.BeginChangeCheck();
            filter = EditorGUILayout.TextField(filter, EditorStyles.toolbarSearchField);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateFilteredItems();
            }

            if (GUILayout.Button("?", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
            {
                Links.OpenDocumentation("notes");
            }

            EditorGUILayout.EndHorizontal();
        }

        private void OnEnable()
        {
            selectContent = new GUIContent(EditorIconContents.rectTransformBlueprint.image, "Select GameObject");
            copyContent = new GUIContent(EditorIconContents.textAsset.image, "Copy text");
            removeContent = new GUIContent("X", "Remove note");
            UpdateFilteredItems();
        }

        private void OnGUI()
        {
            DrawToolbar();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            GUIStyle style = EditorStyles.textArea;
            
            foreach (KeyValuePair<GameObject, NoteItem> pair in filteredNotes)
            {
                if (pair.Key == null) continue;
                NoteItem note = pair.Value;
                if (note.text == null) continue;
                
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

                Texture icon = BestIconDrawer.GetGameObjectIcon(pair.Key);
                if (icon != null) GUILayout.Label(icon, GUILayout.Width(16), GUILayout.Height(16));
                else GUILayout.Space(16);
                
                GUILayout.Label(pair.Key.name);

                if (GUILayout.Button(selectContent, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    Selection.activeGameObject = pair.Key;
                    EditorGUIUtility.PingObject(pair.Key);
                }
                
                if (GUILayout.Button(copyContent, EditorStyles.toolbarButton, GUILayout.Width(22)))
                {
                    EditorGUIUtility.systemCopyBuffer = note.text;
                }
                
                if (GUILayout.Button(removeContent, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    note.text = null;
                    SaveNote(pair.Key, note);
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                
                Vector2 size = style.CalcSize(TempContent.Get(note.text));
                Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(Mathf.Min(size.y, note.maxHeight)));
                EditorGUI.BeginChangeCheck();
                note.text = EditorGUIRef.ScrollableTextAreaInternal(rect, note.text, ref note.scrollPosition, style);
                if (EditorGUI.EndChangeCheck()) SaveNote(pair.Key, note);
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }

        [MenuItem(WindowsHelper.MenuPath + "Notes", false, MenuItemOrder.Notes)]
        public static NoteManager Open()
        {
            NoteManager window = GetWindow<NoteManager>("Notes", true);
            return window;
        }

        public static void RemoveEmptyNotes()
        {
            int count = NoteReferences.RemoveAll(n => string.IsNullOrEmpty(n.text));
        }

        public static void SaveNote(GameObject gameObject, NoteItem note)
        {
            RuntimeNote runtimeNote = gameObject.GetComponent<RuntimeNote>();
            if (EditorApplication.isPlaying || GameObjectUtils.IsInUntitledScene(gameObject))
            {
                SaveToRuntime(gameObject, note, runtimeNote);
                return;
            }

            if (runtimeNote)
            {
                if (string.IsNullOrEmpty(note.text)) note.text = runtimeNote.text;
                DestroyImmediate(runtimeNote);
                note.gid = GlobalObjectIdHelper.GetGlobalObjectIdSlow(gameObject).ToString();
            }

            if (note.isEmpty) NoteReferences.RemoveAll(n => n.gid == note.gid);
            else if (NoteReferences.All(n => n.gid != note.gid)) NoteReferences.Add(note);

            NoteReferences.Save();
        }

        private static void SaveToRuntime(GameObject gameObject, NoteItem note, RuntimeNote runtimeNote)
        {
            if (note.isEmpty)
            {
                if (runtimeNote) DestroyImmediate(runtimeNote);
            }
            else
            {
                if (!runtimeNote)
                {
                    runtimeNote = gameObject.AddComponent<RuntimeNote>();
                    runtimeNote.hideFlags = HideFlags.HideInInspector;
                }

                runtimeNote.text = note.text;
            }
        }

        public static void TryGetValue(GameObject gameObject, out NoteItem note)
        {
            if (cachedNotes.TryGetValue(gameObject, out note))
            {
                RuntimeNote cachedRuntimeNote = gameObject.GetComponent<RuntimeNote>();
                if (cachedRuntimeNote)
                {
                    note.text = cachedRuntimeNote.text;
                    if (!GameObjectUtils.IsInUntitledScene(gameObject) && !EditorApplication.isPlaying) SaveNote(gameObject, note);
                }
                else if (GameObjectUtils.IsInUntitledScene(gameObject))
                {
                    note.text = null;
                }

                return;
            }

            bool isInUntitledScene = GameObjectUtils.IsInUntitledScene(gameObject);
            RuntimeNote runtimeNote = gameObject.GetComponent<RuntimeNote>();
            if (runtimeNote)
            {
                note = new NoteItem { text = runtimeNote.text };
                if (!isInUntitledScene) note.gid = GlobalObjectIdHelper.GetGlobalObjectIdSlow(gameObject).ToString();
                else note.gid = null;

                cachedNotes[gameObject] = note;
                if (!isInUntitledScene && !EditorApplication.isPlaying) SaveNote(gameObject, note);
                return;
            }

            if (isInUntitledScene)
            {
                note = new NoteItem();
                cachedNotes[gameObject] = note;
                return;
            }

            string gid = GlobalObjectIdHelper.GetGlobalObjectIdSlow(gameObject).ToString();
            note = NoteReferences.FirstOrDefault(n => n.gid == gid);
            if (note == null)
            {
                note = new NoteItem { gid = gid };
            }
            else if (string.IsNullOrEmpty(note.gid))
            {
                note.gid = gid;
            }

            cachedNotes[gameObject] = note;
        }

        private void UpdateFilteredItems()
        {
            if (string.IsNullOrEmpty(filter))
            {
                filteredNotes = cachedNotes;
                return;
            }
            
            string pattern = SearchableItem.GetPattern(filter);

            filteredNotes = cachedNotes.Where(p => p.Key != null && SearchableItem.Match(pattern, p.Key.name))
                .ToDictionary(p => p.Key, p => p.Value);
        }
    }
}
