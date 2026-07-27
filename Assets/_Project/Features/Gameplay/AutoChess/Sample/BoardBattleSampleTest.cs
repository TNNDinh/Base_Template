using System.Collections.Generic;
using Ezg.Feature.Gameplay.Battle;
using UnityEngine;

namespace Ezg.Feature.Gameplay.AutoChess
{
    /// <summary>
    ///     Runner kiểm tra <see cref="BoardBattleEngine" /> bằng data mẫu, KHÔNG cần view.
    ///     Gắn vào 1 GameObject rồi chuột phải component → "Run Board Battle". Xem Console.
    ///     Đội player đặt hàng dưới (Y thấp), đội địch hàng trên (Y cao) — hợp màn hình dọc.
    /// </summary>
    public class BoardBattleSampleTest : MonoBehaviour
    {
        [SerializeField] private bool _runOnStart = true;
        [SerializeField] private int _level = 60;
        [SerializeField] private int _star = 3;
        [SerializeField] private float _stepSeconds = 0.1f; // bước mô phỏng cố định

        private void Start()
        {
            if (_runOnStart) Run();
        }

        [ContextMenu("Run Board Battle")]
        public void Run()
        {
            BattleDatabase.LoadSample();

            // --- Đội người chơi: 3 hero, đặt hàng dưới (Y = 1) ---
            var owned = new PlayerHeroData();
            owned.AddHero("hero_1001", _star, _level); // Ignis - Fire Mage (tầm xa)
            owned.AddHero("hero_2002", _star, _level); // Warden - Water Tank (cận chiến)
            owned.AddHero("hero_2001", _star, _level); // Valkyrie - Light Warrior

            var playerTeam = new List<Unit>();
            for (int i = 0; i < owned.heroes.Count; i++)
                playerTeam.Add(BattleService.BuildUnit(owned.heroes[i], BattleTeam.Player, i));

            // --- Đội địch: 3 quái, đặt hàng trên (Y = 6) ---
            var enemyTeam = new List<Unit>
            {
                BattleService.BuildEnemy("enemy_9001", _level, _star, 0),
                BattleService.BuildEnemy("mob_slime", _level, _star, 1),
                BattleService.BuildEnemy("enemy_9001", _level, _star, 2)
            };

            var ctx = BattleService.CreateBattle(playerTeam, enemyTeam);

            // --- Đặt vị trí trên bàn cờ 6×8 ---
            var placement = new Dictionary<Unit, BoardCell>();
            for (int i = 0; i < playerTeam.Count; i++)
                placement[playerTeam[i]] = new BoardCell(1 + i * 2, 1); // cột 1,3,5 hàng dưới
            for (int i = 0; i < enemyTeam.Count; i++)
                placement[enemyTeam[i]] = new BoardCell(1 + i * 2, 6); // cột 1,3,5 hàng trên

            var engine = new BoardBattleEngine(ctx, placement);
            engine.OnUnitMoved += (u, from, to) => { }; // (Phase 2 view sẽ hook)
            engine.OnUnitDied += u => BattleLog.Info($"[Board] {u.DisplayName} gục.");

            Debug.Log("=== BẮT ĐẦU trận cờ auto-battler 6×8 ===");
            int safety = 0;
            while (!engine.Tick(_stepSeconds) && safety++ < 100000)
            {
            }

            Debug.Log($"=== KẾT THÚC: {engine.Winner} thắng | {engine.ElapsedTime:0.0}s mô phỏng ===");
            foreach (var u in ctx.AllUnits())
                Debug.Log($"  [{u.Team}] {u.DisplayName}: HP {u.CurrentHp:0}/{u.MaxHp:0}" +
                          (u.IsAlive ? "" : " (chết)"));
        }
    }
}
