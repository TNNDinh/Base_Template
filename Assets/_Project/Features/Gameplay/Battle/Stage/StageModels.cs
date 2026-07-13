using System;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>Config 1 ải (CSV <c>Stages.csv</c>).</summary>
    [Serializable]
    public struct StageModel
    {
        public string id;
        public int chapter;
        public int index;
        public string name;
        public int staminaCost;
        public string unlockStageId;   // phải clear ải này trước (rỗng = mở sẵn)
        public int recommendedPower;   // gợi ý lực chiến
    }

    /// <summary>
    ///     1 bot trong 1 wave của ải (CSV <c>StageBots.csv</c>) — "config bot" theo từng ải.
    ///     1 ải → nhiều dòng (nhiều wave × nhiều slot). Hệ số mul để tinh chỉnh độ khó mà không cần enemy id mới.
    /// </summary>
    [Serializable]
    public struct StageBotModel
    {
        public string stageId;
        public int wave;      // wave 1..n (đánh tuần tự)
        public int slot;      // 0..5 vị trí
        public string enemyId; // id config enemy/hero
        public int level;
        public int star;
        public float hpMul;   // nhân HP (0/1 = giữ nguyên)
        public float atkMul;  // nhân ATK
        public float defMul;  // nhân DEF
    }
}
