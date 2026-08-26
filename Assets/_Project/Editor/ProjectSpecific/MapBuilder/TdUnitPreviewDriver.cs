using Ezg.Feature.MapBuilder;
using Ezg.Feature.Movement;
using Ezg.Feature.Units;
using UnityEditor;
using UnityEngine;

namespace Ezg.Feature.MapBuilder.EditorTools
{
    /// <summary>
    /// "Play giả": cho lính chạy ngay trong Scene View mà không phải vào Play Mode.
    /// <para>
    /// Ngoài Play Mode, Unity không gọi <c>Update</c> nên <see cref="PathMover"/> đứng im.
    /// Lớp này bơm nhịp thời gian thật vào từng mover qua <see cref="PathMover.Tick"/>.
    /// </para>
    /// </summary>
    [InitializeOnLoad]
    public static class TdUnitPreviewDriver
    {
        #region Constants

        /// <summary>
        /// Bỏ qua bước thời gian lớn hơn ngưỡng này. Editor mất focus rồi quay lại sẽ sinh ra
        /// một delta khổng lồ, đủ để lính nhảy thẳng tới đích trong một tick.
        /// </summary>
        private const float MAX_STEP_SECONDS = 0.1f;

        #endregion

        #region Fields

        private static double _lastTime;

        #endregion

        #region Initialize

        static TdUnitPreviewDriver() => Hook();

        [InitializeOnLoadMethod]
        private static void Hook()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            _lastTime = EditorApplication.timeSinceStartup;
        }

        #endregion

        #region Public

        /// <summary>Sinh một lính từ spawner có trong scene, tạo spawner nếu chưa có.</summary>
        /// <returns><c>false</c> nếu chưa có map hoặc tuyến chưa thông.</returns>
        public static bool SpawnOne()
        {
            // Không có TdMapRenderer thì đây không phải scene dựng map — tuyệt đối không
            // tạo spawner vào đó, nếu không sẽ rải rác GameObject sang scene khác.
            if (!TdMapEditSession.HasSceneContext) return false;

            TdUnitSpawner spawner = FindOrCreateSpawner();
            if (spawner == null) return false;

            Undo.RegisterFullObjectHierarchyUndo(spawner.gameObject, "Spawn TD Unit");
            return spawner.Spawn() != null;
        }

        /// <summary>Thu hồi toàn bộ lính trong scene.</summary>
        public static void DespawnAll()
        {
            foreach (TdUnitSpawner spawner in Object.FindObjectsByType<TdUnitSpawner>(FindObjectsSortMode.None))
            {
                Undo.RegisterFullObjectHierarchyUndo(spawner.gameObject, "Despawn TD Units");
                spawner.DespawnAll();
            }
        }

        /// <summary>Số lính đang chạy trong scene.</summary>
        public static int AliveCount
        {
            get
            {
                int total = 0;
                foreach (TdUnitSpawner spawner in Object.FindObjectsByType<TdUnitSpawner>(FindObjectsSortMode.None))
                {
                    total += spawner.AliveCount;
                }

                return total;
            }
        }

        #endregion

        #region Private

        private static void OnEditorUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            float deltaTime = (float)(now - _lastTime);
            _lastTime = now;

            if (Application.isPlaying) return; // Play Mode đã có Update lo rồi.
            if (deltaTime <= 0f || deltaTime > MAX_STEP_SECONDS) return;

            var walkers = PathWalkerRegistry.Active;
            if (walkers.Count == 0) return;

            // Duyệt ngược: lính tới đích có thể tự huỷ và rút khỏi danh sách ngay trong vòng lặp.
            bool ticked = false;
            for (int i = walkers.Count - 1; i >= 0; i--)
            {
                if (i >= walkers.Count) continue;

                IPathWalker walker = walkers[i];
                if (walker == null || !walker.NeedsExternalTick) continue;

                walker.Tick(deltaTime);
                ticked = true;
            }

            if (ticked) SceneView.RepaintAll();
        }

        private static TdUnitSpawner FindOrCreateSpawner()
        {
            TdUnitSpawner existing = Object.FindFirstObjectByType<TdUnitSpawner>();
            if (existing != null)
            {
                if (existing.Map == null) existing.SetMap(TdMapEditSession.Map);
                return existing;
            }

            TdMapData map = TdMapEditSession.Map;
            if (map == null) return null;

            // Luôn gắn lên object đang dựng map. Không có renderer thì bỏ cuộc chứ không
            // tự đẻ GameObject mới — HasSceneContext đã chặn từ trên, đây là chốt thứ hai.
            TdMapRenderer renderer = Object.FindFirstObjectByType<TdMapRenderer>();
            if (renderer == null) return null;

            var spawner = Undo.AddComponent<TdUnitSpawner>(renderer.gameObject);
            spawner.SetMap(map);
            return spawner;
        }

        #endregion
    }
}
