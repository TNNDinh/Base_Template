using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Ezg.Feature.Shared.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Cầu nối logic ↔ view 2D: spawn model cho toàn bộ unit của 1 trận, và nghe
    ///     <see cref="BattleLog" /> để phát animation (attack/hit) đúng unit. Gắn vào 1 GameObject trong scene battle.
    ///     Lúc Start, nếu <see cref="BattleLaunch.PendingStageId" /> có giá trị → auto đánh ải đó có hình.
    /// </summary>
    public class BattleSceneController : MonoBehaviour
    {
        #region Fields

        [Tooltip("Parent chứa model unit (để trống = spawn ra scene root).")]
        [SerializeField] private Transform _unitRoot;

        [Tooltip("Nhịp chờ giữa mỗi lượt (giây) để kịp thấy anim + chuyển trạng thái.")]
        [SerializeField] private float _turnDelay = 1.2f;

        [Tooltip("Chờ trước khi bắt đầu đánh (giây) — để loading/transition scene xong mới vào trận.")]
        [SerializeField] private float _startDelay = 1.2f;

        [Tooltip("Text hiển thị round hiện tại (tuỳ chọn).")]
        [SerializeField] private Text _roundText;

        private readonly Dictionary<Unit, IUnitView> _views = new Dictionary<Unit, IUnitView>();
        private readonly List<GameObject> _followers = new List<GameObject>();
        private BattleContext _ctx; // trận hiện tại (cho cheat test)

        #endregion

        #region Lifecycle

        private void Start()
        {
            if (string.IsNullOrEmpty(BattleLaunch.PendingStageId)) return;
            var stageId = BattleLaunch.PendingStageId;
            BattleLaunch.PendingStageId = null; // tiêu thụ, tránh đánh lại khi reload
            RunStageVisual(stageId).Forget();
        }

        #endregion

        #region Public

        /// <summary>Spawn model cho cả 2 phe của 1 trận rồi đăng ký hiệu ứng.</summary>
        public void SpawnBattle(BattleContext ctx)
        {
            ClearViews();
            _ctx = ctx;

            for (int i = 0; i < ctx.PlayerTeam.Count; i++)
            {
                var v = HeroSpawner.Spawn(ctx.PlayerTeam[i], _unitRoot);
                _views[ctx.PlayerTeam[i]] = v;
                if (HasShadow(ctx.PlayerTeam[i])) SpawnShadowFollower(ctx.PlayerTeam[i], v); // bóng bám theo (visual)
            }
            for (int i = 0; i < ctx.EnemyTeam.Count; i++)
                _views[ctx.EnemyTeam[i]] = HeroSpawner.Spawn(ctx.EnemyTeam[i], _unitRoot);

            BattleLog.OnDamage -= HandleDamage; // tránh double-subscribe khi qua nhiều wave
            BattleLog.OnDamage += HandleDamage;
            BattleLog.OnRound -= HandleRound;
            BattleLog.OnRound += HandleRound;
            BattleLog.OnUnitSpawned -= HandleUnitSpawned;
            BattleLog.OnUnitSpawned += HandleUnitSpawned;
            BattleLog.OnUnitDespawned -= HandleUnitDespawned;
            BattleLog.OnUnitDespawned += HandleUnitDespawned;
        }

        /// <summary>Spawn model cho unit thêm giữa trận (vd Bóng thay hero) + tint tối cho ra dáng "bóng".</summary>
        private void HandleUnitSpawned(Unit unit)
        {
            if (unit == null || _views.ContainsKey(unit)) return;
            var view = HeroSpawner.Spawn(unit, _unitRoot);
            _views[unit] = view;
            if (view is Component comp && comp != null)
                foreach (var sr in comp.GetComponentsInChildren<SpriteRenderer>(true))
                    sr.color = new Color(0.25f, 0.25f, 0.45f, 0.85f); // bóng tối, hơi trong
        }

        private void HandleUnitDespawned(Unit unit)
        {
            if (unit == null || !_views.TryGetValue(unit, out var v)) return;
            if (v is Component comp && comp != null) Destroy(comp.gameObject);
            _views.Remove(unit);
        }

        #region Cheat / Test

        /// <summary>CHEAT: giết 1 đồng minh còn sống (ưu tiên không phải chủ Bóng) để test triệu hồi bóng.</summary>
        [ContextMenu("Cheat: Kill 1 đồng minh")]
        public void DebugKillAlly()
        {
            if (_ctx == null) return;
            Unit fallback = null;
            foreach (var u in _ctx.PlayerTeam)
            {
                if (!u.IsAlive) continue;
                if (fallback == null) fallback = u;
                if (!HasShadow(u)) { KillWithVisual(u); return; }
            }
            if (fallback != null) KillWithVisual(fallback);
        }

        /// <summary>CHEAT: hồi sinh đồng minh chết đầu tiên (50% máu) — sau đó Bóng sẽ rút về ở round kế.</summary>
        [ContextMenu("Cheat: Hồi sinh đồng minh")]
        public void DebugReviveAlly()
        {
            if (_ctx == null) return;
            foreach (var u in _ctx.PlayerTeam)
            {
                if (u.IsAlive || u.DisplayName.StartsWith("Bóng")) continue; // bỏ qua bóng-hero
                u.CurrentHp = u.MaxHp * 0.5f;
                if (_views.TryGetValue(u, out var v)) { v.Revive(); v.SetHp(u.CurrentHp, u.MaxHp); }
                Debug.Log($"[Cheat] Revived {u.DisplayName} ({u.CurrentHp:0} HP)");
                return;
            }
        }

        private void KillWithVisual(Unit u)
        {
            DamagePipeline.Kill(_ctx, u);                 // logic chết + hook (bóng thế chỗ)
            if (_views.TryGetValue(u, out var v)) v.PlayDie(); // view: anim chết → mờ → ẩn
            Debug.Log($"[Cheat] Killed {u.DisplayName}");
        }

        private static bool HasShadow(Unit u)
        {
            for (int i = 0; i < u.Passives.Count; i++)
                if (u.Passives[i] is ShadowPassive) return true;
            return false;
        }

        /// <summary>Tạo bóng cosmetic bám theo hero (clone model, tint tối, bỏ UI/logic).</summary>
        private void SpawnShadowFollower(Unit unit, IUnitView heroView)
        {
            if (!(heroView is Component hc) || hc == null) return;
            var prefab = Resources.Load<GameObject>("Heroes/" + unit.ModelKey);
            if (prefab == null) return;

            var go = Instantiate(prefab, hc.transform.position, hc.transform.rotation, _unitRoot);
            go.name = "ShadowFollower_" + unit.DisplayName;

            var uv = go.GetComponent<UnitView>(); if (uv != null) Destroy(uv);
            foreach (var n in new[] { "HpBar", "ManaBar", "HitPoint" })
            {
                var t = go.transform.Find(n);
                if (t != null) Destroy(t.gameObject);
            }
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
                sr.color = new Color(0.12f, 0.12f, 0.28f, 0.55f); // bóng tối, mờ

            var hm = hc.transform.Find("ModelRoot");
            var sm = go.transform.Find("ModelRoot");
            if (hm != null && sm != null) sm.localScale = hm.localScale; // đồng bộ hướng flip
            var anim = go.GetComponentInChildren<Animator>();
            if (anim != null) anim.CrossFade("Idle", 0.1f);

            var f = go.AddComponent<ShadowFollower>();
            f.target = hc.transform;
            _followers.Add(go);
        }

        #endregion

        /// <summary>Đánh 1 ải có hình: spawn từng wave, chạy từng lượt có nhịp, hiện kết quả.</summary>
        public async UniTask RunStageVisual(string stageId, float? turnDelay = null)
        {
            BattleDatabase.LoadSample();
            var delay = turnDelay ?? _turnDelay;
            var ct = this.GetCancellationTokenOnDestroy();

            var team = BattlePlayerData.GetActiveTeam();
            var playerUnits = new List<Unit>(team.Count);
            for (int i = 0; i < team.Count; i++)
                playerUnits.Add(BattleService.BuildUnit(team[i], BattleTeam.Player, i));

            // Gom bot theo wave
            var bots = BattleDatabase.GetStageBots(stageId);
            var waveMap = new SortedDictionary<int, List<StageBotModel>>();
            for (int i = 0; i < bots.Count; i++)
            {
                var b = bots[i];
                if (!waveMap.TryGetValue(b.wave, out var list))
                {
                    list = new List<StageBotModel>();
                    waveMap[b.wave] = list;
                }

                list.Add(b);
            }

            bool win = true;
            int wavesCleared = 0;
            bool firstWave = true;
            foreach (var kv in waveMap)
            {
                var enemies = new List<Unit>(kv.Value.Count);
                for (int i = 0; i < kv.Value.Count; i++) enemies.Add(BattleService.BuildBot(kv.Value[i]));

                var ctx = BattleService.CreateBattle(playerUnits, enemies);
                SpawnBattle(ctx);

                // Wave đầu: chờ loading/transition scene xong (unit đã hiện) rồi mới đánh.
                if (firstWave)
                {
                    firstWave = false;
                    await UniTask.Delay(TimeSpan.FromSeconds(_startDelay), cancellationToken: ct);
                }

                var winner = await BattleService.RunAutoVisual(ctx, delay, ct);
                for (int i = 0; i < playerUnits.Count; i++) playerUnits[i].Buffs.Clear();

                if (winner != BattleTeam.Player) { win = false; break; }
                wavesCleared++;
                await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: ct);
            }

            int survivors = 0;
            for (int i = 0; i < playerUnits.Count; i++)
                if (playerUnits[i].IsAlive)
                    survivors++;
            int deaths = playerUnits.Count - survivors;
            int stars = !win ? 0 : deaths <= 0 ? 3 : deaths <= 1 ? 2 : 1;

            var result = new StageRunResult
            {
                Win = win, WavesCleared = wavesCleared, TotalWaves = waveMap.Count,
                TeamSize = playerUnits.Count, Survivors = survivors, Stars = stars
            };
            StageService.ApplyResult(BattlePlayerData.Stage, stageId, result);

            await UniTask.Delay(TimeSpan.FromSeconds(0.6f), cancellationToken: ct); // để anim đòn cuối kịp chạy
            BattleResultController.Open(result);
        }

        public IUnitView GetView(Unit unit) => _views.TryGetValue(unit, out var v) ? v : null;

        #endregion

        #region Events

        private void HandleDamage(Unit caster, Unit target, float amount, bool isCrit)
        {
            IUnitView cv = caster != null && _views.TryGetValue(caster, out var c) ? c : null;
            IUnitView tv = target != null && _views.TryGetValue(target, out var t) ? t : null;

            var tgt = target;
            var targetView = tv;

            // Phản ứng của target — chạy tại ĐÚNG lúc chạm đòn (onImpact), không phải lúc trigger.
            Action reactTarget = () =>
            {
                if (!(targetView is Component comp) || comp == null) return; // view có thể đã bị dọn (đổi wave)
                targetView.SetHp(tgt.CurrentHp, tgt.MaxHp);
                targetView.SetMana(tgt.Mana, tgt.MaxMana);
                if (tgt.IsAlive) targetView.PlayHit();
                else targetView.PlayDie();
            };

            if (cv != null)
            {
                cv.SetMana(caster.Mana, caster.MaxMana); // caster vừa tiêu mana khi cast
                if (tv != null)
                {
                    var tpos = tv is Component tc && tc != null ? tc.transform.position : cv.HitPoint.position;
                    cv.AttackTarget(tpos, caster.AttackRange, reactTarget); // đứng trước mặt target → đánh
                }
                else cv.PlayAttack();
            }
            else if (tv != null)
            {
                reactTarget(); // không có attacker view → phản ứng ngay
            }
        }

        private void HandleRound(int round)
        {
            if (_roundText != null) _roundText.text = $"Round {round}";

            // Đồng bộ mọi thanh máu/mana theo giá trị thật đầu mỗi round
            // (tránh thanh kẹt giá trị cũ khi unit không dính damage event).
            foreach (var kv in _views)
            {
                if (kv.Key == null || kv.Value == null) continue;
                kv.Value.SetHp(kv.Key.CurrentHp, kv.Key.MaxHp);
                kv.Value.SetMana(kv.Key.Mana, kv.Key.MaxMana);
            }
        }

        private void OnDestroy()
        {
            BattleLog.OnDamage -= HandleDamage;
            BattleLog.OnRound -= HandleRound;
            BattleLog.OnUnitSpawned -= HandleUnitSpawned;
            BattleLog.OnUnitDespawned -= HandleUnitDespawned;
        }

        private void ClearViews()
        {
            foreach (var kv in _views)
                if (kv.Value is Component c && c != null)
                    Destroy(c.gameObject);
            _views.Clear();

            for (int i = 0; i < _followers.Count; i++)
                if (_followers[i] != null) Destroy(_followers[i]);
            _followers.Clear();
        }

        #endregion

        #region Preview (editor)

        /// <summary>Xem thử đội hình 6v6 bằng placeholder ngay trong scene (Play mode hoặc editor).</summary>
        [ContextMenu("Preview Sample Formation (6v6)")]
        public void PreviewSampleFormation()
        {
            BattleDatabase.LoadSample();

            var players = new List<Unit>();
            string[] pIds = { "hero_1001", "hero_1001_fg", "hero_1001_tk", "hero_1001_as", "hero_1001", "hero_1001_fg" };
            for (int i = 0; i < pIds.Length; i++)
                players.Add(BattleService.BuildUnit(new OwnedHero { heroId = pIds[i], star = 3, level = 60 }, BattleTeam.Player, i));

            var enemies = new List<Unit>();
            string[] eIds = { "mob_slime", "mob_goblin", "mob_orc", "mob_goblin", "mob_slime", "boss_ogre" };
            for (int i = 0; i < eIds.Length; i++)
                enemies.Add(BattleService.BuildEnemy(eIds[i], 1, 1, i));

            var ctx = BattleService.CreateBattle(players, enemies);
            SpawnBattle(ctx);
        }

        #endregion
    }
}
