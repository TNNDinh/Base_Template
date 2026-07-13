using System;
using System.Collections.Generic;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>Bản ghi kết quả 1 ải người chơi đã đánh.</summary>
    [Serializable]
    public class StageRecord
    {
        public string stageId;
        public int stars; // 0 = chưa clear
    }

    /// <summary>
    ///     Tiến trình ải của người chơi (persist). MVP plain class; production extends DataPlayerBase,
    ///     truy cập qua PlayerDataManager.StageData.
    /// </summary>
    [Serializable]
    public class PlayerStageData
    {
        public List<StageRecord> records = new List<StageRecord>();

        public StageRecord Get(string stageId) => records.Find(r => r.stageId == stageId);

        public bool IsCleared(string stageId)
        {
            var r = Get(stageId);
            return r != null && r.stars > 0;
        }

        public int GetStars(string stageId)
        {
            var r = Get(stageId);
            return r != null ? r.stars : 0;
        }

        /// <summary>Ghi kết quả, chỉ nâng sao (không hạ) khi đánh lại tốt hơn.</summary>
        public void SetResult(string stageId, int stars)
        {
            var r = Get(stageId);
            if (r == null)
            {
                r = new StageRecord { stageId = stageId };
                records.Add(r);
            }

            if (stars > r.stars) r.stars = stars;
        }
    }
}
