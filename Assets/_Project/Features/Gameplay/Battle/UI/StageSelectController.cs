using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using BlackFace.Libraries.Modules.UIModule;
using Ezg.Feature.Shared.Config;
using Ezg.Feature.Shared.Systems;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Màn chọn ải (stage-select). Kế thừa <see cref="FeatureBaseController" />; mở bằng
    ///     <see cref="Open" /> hoặc UIManager.Instance.Show(GameEnums.Features.StageSelect).
    ///     Prefab đặt tên <c>screen_stage_select</c>, set FeatureType = StageSelect trên inspector.
    /// </summary>
    public class StageSelectController : FeatureBaseController
    {
        #region Fields

        [SerializeField] private Transform _content;         // parent chứa các ô ải
        [SerializeField] private StageCellView _cellPrefab;  // prefab 1 ô ải
        [SerializeField] private int _chapter = 1;

        private readonly List<StageCellView> _cells = new List<StageCellView>();

        #endregion

        #region Open

        /// <summary>Mở màn chọn ải (layer Main).</summary>
        public static void Open(int chapter = 1)
        {
            UIManager.Instance.Show(GameEnums.Features.StageSelect, UIManager.UIGroupName.Main_Container,
                data: chapter).Forget();
        }

        #endregion

        #region Lifecycle

        protected override void Start()
        {
            base.Start();
            BattleDatabase.LoadSample(); // đảm bảo config đã nạp từ CSV
            Refresh();
        }

        public override void LoadData(object data)
        {
            if (data is int chapter) _chapter = chapter;
            BattleDatabase.LoadSample();
            Refresh();
        }

        #endregion

        #region Build list

        private void Refresh()
        {
            if (_content == null || _cellPrefab == null) return;

            ClearCells();

            foreach (var stage in GetStagesOfChapter(_chapter))
            {
                var cell = Instantiate(_cellPrefab, _content);
                var progress = BattlePlayerData.Stage;
                var unlocked = progress.IsCleared(stage.id) || StageService.CanEnter(stage.id, progress);
                cell.Bind(stage, unlocked, progress.GetStars(stage.id), OnStageClicked);
                _cells.Add(cell);
            }
        }

        private void OnStageClicked(string stageId)
        {
            // Chuyển sang BattleScene đánh có hình (gating + đổi scene nằm trong service).
            StageFlowService.EnterBattleScene(stageId);
        }

        private static List<StageModel> GetStagesOfChapter(int chapter)
        {
            var list = new List<StageModel>();
            foreach (var s in BattleDatabase.AllStages())
                if (s.chapter == chapter)
                    list.Add(s);
            list.Sort((a, b) => a.index.CompareTo(b.index));
            return list;
        }

        private void ClearCells()
        {
            for (int i = 0; i < _cells.Count; i++)
                if (_cells[i] != null)
                    Destroy(_cells[i].gameObject);
            _cells.Clear();
        }

        #endregion
    }
}
