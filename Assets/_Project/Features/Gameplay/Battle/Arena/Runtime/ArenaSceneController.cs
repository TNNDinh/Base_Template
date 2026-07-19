using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Điều khiển scene arena: dựng hero ở tâm + vòng lặp turn xen kẽ. Mỗi vòng = 1 ENEMY ROUND
    ///     (hành động TRƯỚC: spawn theo round rồi enemy di chuyển/telegraph/đánh) → 1 PLAYER ROUND
    ///     (hiện 3 nút vũ khí: bấm để đổi vũ khí — anim/range/ô target; bấm lại = phát động tấn công).
    ///     Đọc stage từ <see cref="ArenaLaunch.PendingStageId" />. Nối <see cref="ArenaCombat" /> với view.
    /// </summary>
    public class ArenaSceneController : MonoBehaviour
    {
        #region Fields

        [Header("Refs")]
        [SerializeField] private RadialGridArena _arena;
        [SerializeField] private ArenaPrefabRegistry _registry;
        [SerializeField] private WeaponTriggerVisual _weaponVisual;

        [Header("Collections (để trống = tự load Resources/ArenaCsv)")]
        [SerializeField] private ArenaStageCollection _stages;
        [SerializeField] private ArenaSpawnCollection _spawns;
        [SerializeField] private EnemyStatCollection _enemyStats;
        [SerializeField] private HeroStatCollection _heroStats;
        [SerializeField] private EnemyMovePatternCollection _patterns;
        [SerializeField] private WeaponCollection _weapons;
        [SerializeField] private WeatherCollection _weathers;

        [Header("Hero")]
        [SerializeField] private string _heroId = "44000";
        [SerializeField] private int _heroLevel = 1;
        [Tooltip("3 vũ khí trang bị — hiện 3 nút ở round player.")]
        [SerializeField] private string[] _weaponSlots = { "wp_wood_sword", "wp_scythe", "wp_hammer" };

        [Header("Debug")]
        [SerializeField] private string _debugStageId = "arena_1_1";
        [Tooltip("Ép thời tiết map khi test (bỏ trống = lấy theo stage).")]
        [SerializeField] private string _debugWeatherId = "";

        [Header("View")]
        [Tooltip("Scale model enemy để vừa ô grid.")]
        [SerializeField] private float _enemyScale = 0.7f;

        [Header("UI (scene sạch — UI nằm trong prefab, dựng lúc runtime)")]
        [Tooltip("Prefab màn UI gameplay (ArenaCanvas: weapon bar, hero HUD, upgrade, thắng/thua). Để trống = UI đã có sẵn trong scene.")]
        [SerializeField] private GameObject _uiScreenPrefab;

        [Header("Kết thúc trận (tuỳ chọn — để trống = tự tìm trong UI prefab)")]
        [Tooltip("Panel/label hiện khi THUA (hero hết máu).")]
        [SerializeField] private GameObject _defeatUi;
        [Tooltip("Panel/label hiện khi THẮNG (qua hết số round).")]
        [SerializeField] private GameObject _victoryUi;

        private GameObject _uiScreen;

        [Header("Timing")]
        [SerializeField] private float _startDelay = 0.8f;
        [SerializeField] private float _roundDelay = 0.9f;
        [SerializeField] private float _moveDuration = 0.35f;

        private ArenaCombat _combat;
        private string _stageId;
        private ArenaWeather _weather = new ArenaWeather(default); // biome CỐ ĐỊNH của map (vd forest cho chương 1)
        private GameObject _weatherOverlay;                        // sprite phủ màu môi trường theo biome
        private bool _weatherOnThisRound;                          // round này biome CÓ phát tác (vd forest = mưa) hay tạnh
        private GameObject _resultView;                            // overlay kết quả (thắng/thua) khi kết thúc trận
        private GameObject _hero;
        private CharacterRig _heroRig;
        private ArenaHealthBar _heroBar;
        private float _heroHp;
        private float _heroMaxHp;
        private readonly Dictionary<ArenaEnemyUnit, CharacterRig> _views = new Dictionary<ArenaEnemyUnit, CharacterRig>();
        private readonly Dictionary<ArenaEnemyUnit, float> _viewYOffset = new Dictionary<ArenaEnemyUnit, float>();
        private readonly Dictionary<ArenaEnemyUnit, ArenaHealthBar> _bars = new Dictionary<ArenaEnemyUnit, ArenaHealthBar>();
        private readonly List<ArenaEnemyUnit> _pendingAttackers = new List<ArenaEnemyUnit>();

        private int _selectedSlot = -1;
        private bool _attacking;
        private bool _defeated;
        private UniTaskCompletionSource _fireSignal;

        // Nhớ vũ khí đã dùng ở round hero gần nhất (static = giữ qua các lần vào lại scene trong 1 phiên chơi).
        private static int _lastUsedSlot;
        // Hero đang chọn (static = giữ qua các lần vào lại scene trong 1 phiên).
        private static string _selectedHeroId;
        private UnitStats _heroStatsCur; // stat hero hiện tại (cho ultimate: atk, maxHp)
        private int _ultCdLeft;          // cooldown ult còn lại (round). <=0 = dùng được
        private bool _ultSelected;       // đã chọn ult (tap 1) — tap 2 mới kích hoạt
        private float _heroDmgReduce;    // passive: giảm % damage nhận
        private float _heroRegenPct;     // passive: hồi %/round máu tối đa

        // Loadout vũ khí mặc định theo hero (mỗi hero 3 "skill" vũ khí khác nhau).
        private static readonly Dictionary<string, string[]> HeroLoadouts = new Dictionary<string, string[]>
        {
            { "44000", new[] { "wp_wood_sword", "wp_trap", "wp_hammer" } },
            { "44001", new[] { "wp_hammer", "wp_spear", "wp_wood_sword" } },
            { "44003", new[] { "wp_arrow", "wp_scythe", "wp_spear" } },
            { "44004", new[] { "wp_wood_sword", "wp_spear", "wp_scythe" } },
            { "44005", new[] { "wp_scythe", "wp_arrow", "wp_hammer" } },
            { "44006", new[] { "wp_wood_sword", "wp_hammer", "wp_arrow" } },
            { "44008", new[] { "wp_hammer", "wp_scythe", "wp_spear" } },
            { "44010", new[] { "wp_spear", "wp_arrow", "wp_wood_sword" } },
            { "44011", new[] { "wp_wood_sword", "wp_scythe", "wp_arrow" } },
            { "44013", new[] { "wp_hammer", "wp_spear", "wp_arrow" } }
        };

        #endregion

        #region Public (cho UI weapon bar)

        /// <summary>Đang ở round player (lúc hiện 3 nút vũ khí).</summary>
        public bool IsPlayerRound { get; private set; }

        public int SlotCount => _weaponSlots != null ? _weaponSlots.Length : 0;
        public int SelectedSlot => _selectedSlot;
        public string SlotWeaponId(int i) => i >= 0 && i < SlotCount ? _weaponSlots[i] : null;

        /// <summary>Id hero hiện tại (cho UI nâng cấp).</summary>
        public string HeroId => _heroId;
        /// <summary>Collection vũ khí đang dùng (cho UI nâng cấp).</summary>
        public WeaponCollection Weapons => _weapons;
        /// <summary>Collection stat hero (cho UI đổi hero).</summary>
        public HeroStatCollection HeroStats => _heroStats;

        /// <summary>Toàn bộ id vũ khí khả dụng (cho UI chọn vũ khí).</summary>
        public string[] AllWeaponIds()
        {
            if (_weapons == null || _weapons.dataGroup == null) return new string[0];
            var ids = new string[_weapons.dataGroup.Length];
            for (int i = 0; i < ids.Length; i++) ids[i] = _weapons.dataGroup[i].id;
            return ids;
        }

        /// <summary>Đổi vũ khí slot sang vũ khí KẾ TIẾP (UI chọn vũ khí) — lưu loadout theo hero.</summary>
        public void CycleWeaponSlot(int slot)
        {
            if (slot < 0 || slot >= SlotCount || _weapons == null) return;
            var all = AllWeaponIds();
            if (all.Length == 0) return;
            int cur = Array.IndexOf(all, _weaponSlots[slot]);
            _weaponSlots[slot] = all[(cur + 1 + all.Length) % all.Length];
            ArenaUpgradeService.SaveLoadout(_heroId, string.Join(";", _weaponSlots));
            OnHeroChanged?.Invoke();
            if (IsPlayerRound && _selectedSlot == slot) EquipSlot(slot); // đổi ngay nếu đang cầm slot đó
        }

        /// <summary>Loadout 3 vũ khí của hero: đã lưu (đã chọn) → dùng; chưa → mặc định. Luôn trả mảng MỚI (không sửa default).</summary>
        private string[] WeaponsForHero(string heroId)
        {
            var saved = ArenaUpgradeService.GetSavedLoadout(heroId);
            if (!string.IsNullOrEmpty(saved))
            {
                var arr = saved.Split(';');
                if (arr.Length > 0) return arr;
            }

            var def = HeroLoadouts.TryGetValue(heroId, out var lo) && lo != null && lo.Length > 0 ? lo : _weaponSlots;
            return (string[])def.Clone();
        }

        /// <summary>Tên hero hiện tại (localize sau; giờ lấy từ stat).</summary>
        public string HeroName => _heroStats != null ? _heroStats.GetById(_heroId).name : _heroId;

        /// <summary>Danh sách id hero khả dụng (theo HeroStats).</summary>
        public string[] HeroIds()
        {
            if (_heroStats == null || _heroStats.dataGroup == null) return new[] { _heroId };
            var ids = new string[_heroStats.dataGroup.Length];
            for (int i = 0; i < ids.Length; i++) ids[i] = _heroStats.dataGroup[i].id;
            return ids;
        }

        /// <summary>Đổi hero (UI gọi) — dựng lại hero ở tâm với stat + loadout mới. Chỉ cho đổi ngoài lúc đang đánh.</summary>
        public void SetHero(string heroId)
        {
            if (_attacking || string.IsNullOrEmpty(heroId) || heroId == _heroId) return;
            if (!ArenaHeroUnlockService.IsUnlocked(heroId)) return; // chưa mở khóa → không đổi
            _selectedHeroId = heroId;
            SpawnHero(heroId);
            OnHeroChanged?.Invoke();
            if (IsPlayerRound) EquipSlot(Mathf.Clamp(_lastUsedSlot, 0, Mathf.Max(0, SlotCount - 1)));
        }

        /// <summary>Đổi sang hero ĐÃ MỞ KHÓA kế tiếp (bỏ qua hero còn khóa).</summary>
        public void CycleHero()
        {
            var ids = HeroIds();
            if (ids.Length == 0) return;
            int idx = Array.IndexOf(ids, _heroId);
            for (int k = 1; k <= ids.Length; k++)
            {
                var cand = ids[(idx + k + ids.Length) % ids.Length];
                if (ArenaHeroUnlockService.IsUnlocked(cand)) { SetHero(cand); return; }
            }
        }

        /// <summary>Ultimate còn dùng được không (hết cooldown).</summary>
        public bool CanUseUltimate => IsPlayerRound && !_attacking && _ultCdLeft <= 0;
        /// <summary>Số round cooldown ult còn lại (0 = sẵn sàng) — cho UI hiển thị.</summary>
        public int UltCooldownLeft => _ultCdLeft;
        /// <summary>Ult đang được chọn (tap 1) — UI highlight nút ult.</summary>
        public bool UltSelected => _ultSelected;

        /// <summary>
        ///     UI gọi khi bấm nút ULTIMATE — giống nút vũ khí: lần 1 = CHỌN (bỏ chọn vũ khí; ult kiểu "vung"
        ///     sẽ hiện trigger), lần 2 (cùng nút) = KÍCH HOẠT rồi QUA ROUND.
        /// </summary>
        public void OnUltimateButton()
        {
            if (!IsPlayerRound || _attacking || _ultCdLeft > 0) return;

            if (!_ultSelected)
            {
                // Tap 1: chọn ult, bỏ chọn vũ khí (ẩn trigger vũ khí). Ult hiện tại là AoE toàn sân → không có trigger.
                _ultSelected = true;
                _selectedSlot = -1;
                HideWeaponVisual();
                OnWeaponChanged?.Invoke(-1);   // bỏ highlight vũ khí
                OnUltSelectedChanged?.Invoke(); // UI highlight nút ult
                return;
            }

            // Tap 2: kích hoạt → vào cooldown theo skill.
            _ultSelected = false;
            _attacking = true;
            _ultCdLeft = ArenaUpgradeService.SkillCooldown(_heroId);
            OnUltSelectedChanged?.Invoke();
            ExecuteUltimate().Forget();
        }

        /// <summary>Đổi hero xong (UI nghe để refresh nhãn).</summary>
        public event Action OnHeroChanged;
        /// <summary>Trạng thái chọn ult đổi (UI nghe để highlight nút ult).</summary>
        public event Action OnUltSelectedChanged;

        /// <summary>Bắt đầu / kết thúc round player — UI bar nghe để hiện/ẩn.</summary>
        public event Action OnPlayerRoundStart;
        public event Action OnPlayerRoundEnd;
        /// <summary>Đổi vũ khí đang chọn (index slot) — UI bar nghe để highlight.</summary>
        public event Action<int> OnWeaponChanged;

        /// <summary>UI gọi khi bấm nút vũ khí slot i: lần 1 = đổi vũ khí, lần 2 (cùng slot) = phát động.</summary>
        public void OnWeaponButton(int slot)
        {
            if (!IsPlayerRound || _attacking || slot < 0 || slot >= SlotCount) return;
            if (slot != _selectedSlot) EquipSlot(slot);
            else FireSelected();
        }

        #endregion

        #region Lifecycle

        private void Start() => Run().Forget();

        private void OnDestroy() => ArenaUpgradeService.OnChanged -= HandleUpgradeChanged;

        /// <summary>Nâng cấp hero ĐANG dùng → cập nhật máu tối đa (cộng phần tăng) + atk (cho ult) ngay trong trận.</summary>
        private void HandleUpgradeChanged()
        {
            if (_heroStats == null || string.IsNullOrEmpty(_heroId) || _heroMaxHp <= 0f) return;
            int lvl = ArenaUpgradeService.HeroLevel(_heroId);
            var st = _heroStats.StatsAt(_heroId, lvl);
            float newMax = st.hp > 0f ? st.hp : 100f;
            float delta = newMax - _heroMaxHp;
            _heroStatsCur = st; // atk mới → ult mạnh hơn
            _heroMaxHp = newMax;
            _heroHp = delta > 0f ? Mathf.Min(newMax, _heroHp + delta) : Mathf.Min(_heroHp, newMax);
            if (_heroBar != null) _heroBar.SetRatio(_heroHp / _heroMaxHp);
        }

        private async UniTaskVoid Run()
        {
            var ct = this.GetCancellationTokenOnDestroy();

            _stageId = !string.IsNullOrEmpty(ArenaLaunch.PendingStageId) ? ArenaLaunch.PendingStageId : _debugStageId;
            ArenaLaunch.PendingStageId = null;

            ResolveCollections();
            if (_arena == null)
            {
                Debug.LogError("[Arena] Chưa gán RadialGridArena cho ArenaSceneController.");
                return;
            }

            SpawnUiScreen(); // dựng UI từ prefab (scene sạch), UI tự bind vào controller này
            ArenaUpgradeService.OnChanged += HandleUpgradeChanged; // nâng cấp hero → cập nhật stat ngay
            if (_arena.Grid == null) _arena.Build();
            SpawnHero(!string.IsNullOrEmpty(_selectedHeroId) ? _selectedHeroId : _heroId);
            HideWeaponVisual();

            _combat = new ArenaCombat(_arena.Config, _stageId, _spawns, _enemyStats, _patterns);
            _combat.OnEnemySpawned = HandleEnemySpawned;
            _combat.OnTrapPlaced = HandleTrapPlaced;
            _combat.OnEnemyUseSkill = HandleEnemyUseSkill;

            var stage = _stages != null ? _stages.GetById(_stageId) : default;
            int maxRounds = stage.maxRounds > 0 ? stage.maxRounds : 20;

            ResolveWeather(stage.weather); // thời tiết map: aura DoT/regen + hệ số damage + màu phủ

            // Cảnh báo rõ ràng khi stage KHÔNG có spawn → arena sẽ trống (thường do map id chưa có dòng trong ArenaSpawns).
            if (_spawns == null || _spawns.GetByStage(_stageId).Count == 0)
                Debug.LogWarning($"[Arena] Stage '{_stageId}' không có spawn nào (ArenaSpawns) → arena sẽ TRỐNG. Kiểm tra ArenaSpawns.csv / ArenaSpawnCollection.");

            await UniTask.Delay(TimeSpan.FromSeconds(_startDelay), cancellationToken: ct);

            for (int r = 0; r < maxRounds; r++)
            {
                RollWeatherRound(); // biome cố định nhưng mỗi round random BẬT/TẮT hiệu ứng (forest: mưa hay tạnh)

                // ===== ENEMY ROUND — hành động TRƯỚC =====
                _combat.RunEnemyRound();
                if (_weatherOnThisRound) ApplyWeatherToEnemies(); // chỉ áp aura ở round biome phát tác
                await UniTask.Delay(TimeSpan.FromSeconds(_roundDelay), cancellationToken: ct);
                await PlayEnemyAttacks(ct); // enemy vào tầm lao vào đánh TỪNG CON theo ưu tiên

                if (_defeated) { EndBattle(false); return; } // hero chết trong round enemy → THUA
                if (AllCleared()) { EndBattle(true); return; } // hết spawn + sạch quái → THẮNG NGAY

                // Chưa hết spawn nhưng hiện KHÔNG còn quái → round trống: lướt nhanh (không bắt player bấm),
                // sang round kế để spawn đợt tiếp — tránh phải đợi lâu khi giết quái đợt trước quá nhanh.
                if (_combat.AliveCount == 0)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_roundDelay), cancellationToken: ct);
                    continue;
                }

                // ===== PLAYER ROUND — hiện 3 nút vũ khí, chờ user phát động =====
                await PlayerRound(ct);

                if (_defeated) { EndBattle(false); return; }
                if (AllCleared()) { EndBattle(true); return; } // player giết con cuối cùng → THẮNG NGAY
            }

            EndBattle(true); // qua hết round mà chưa chết → THẮNG (sống sót giới hạn round)
        }

        /// <summary>Đã dọn sạch stage: qua round spawn cuối + không còn enemy sống → thắng ngay (không đợi hết round).</summary>
        private bool AllCleared() =>
            _combat != null && _combat.RoundNo >= _combat.LastSpawnRound && _combat.AliveCount == 0;

        /// <summary>Kết thúc trận: dừng vòng lặp, ẩn UI vũ khí, tính sao + nhiệm vụ, hiện màn kết quả.</summary>
        private void EndBattle(bool win)
        {
            IsPlayerRound = false;
            HideWeaponVisual();
            OnPlayerRoundEnd?.Invoke();

            int stars = 0, gold = 0;
            var missions = new List<ResultMission>();
            if (win)
            {
                // Sao PHỔ QUÁT (mọi stage): thắng = 1 sao, + máu còn ≥50% / ≥90%.
                float hpPct = _heroMaxHp > 0f ? _heroHp / _heroMaxHp * 100f : 0f;
                bool s2 = hpPct >= 50f, s3 = hpPct >= 90f;
                stars = 1 + (s2 ? 1 : 0) + (s3 ? 1 : 0);
                missions.Add(new ResultMission { desc = "Chien thang man", met = true });
                missions.Add(new ResultMission { desc = "Mau con >= 50%", met = s2 });
                missions.Add(new ResultMission { desc = "Mau con >= 90%", met = s3 });
                gold = 200;
                ArenaUpgradeService.AddGold(gold);                 // thưởng gold nâng cấp
                ArenaHeroUnlockService.MarkStageCleared(_stageId); // clear stage → mở hero method=Stage
                ArenaHeroUnlockService.SetStars(_stageId, stars);  // lưu kỷ lục sao
            }

            // Ẩn label legacy (nếu prefab có) rồi hiện màn kết quả mới.
            if (_defeatUi != null) _defeatUi.SetActive(false);
            if (_victoryUi != null) _victoryUi.SetActive(false);
            if (_resultView != null) Destroy(_resultView);
            _resultView = ArenaResultView.Show(win, stars, missions, gold, ReloadStage, GoToStageSelect);

            Debug.Log(win ? $"[Arena] VICTORY stars={stars}" : "[Arena] DEFEAT");
        }

        /// <summary>Nút CHƠI LẠI: chơi lại đúng stage này.</summary>
        private void ReloadStage()
        {
            ArenaLaunch.PendingStageId = _stageId;
            LoadSceneSafe("BattleScene");
        }

        /// <summary>Nút VỀ: quay lại màn chính (home) để chọn map khác.</summary>
        private void GoToStageSelect() => LoadSceneSafe("HomeScene");

        /// <summary>Đổi scene an toàn: ưu tiên GameSystems.ChangeScene (có mask), fallback SceneManager.</summary>
        private static void LoadSceneSafe(string sceneName)
        {
            try
            {
                var scene = (Ezg.Feature.Shared.Config.GameEnums.Scenes)Enum.Parse(
                    typeof(Ezg.Feature.Shared.Config.GameEnums.Scenes), sceneName);
                Ezg.Feature.Shared.Systems.GameSystems.ChangeScene(scene);
            }
            catch
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
        }

        #endregion

        #region Player round

        private async UniTask PlayerRound(CancellationToken ct)
        {
            IsPlayerRound = true;
            _selectedSlot = -1;
            if (_ultCdLeft > 0) _ultCdLeft--;  // giảm cooldown ult mỗi round player
            _ultSelected = false;
            ApplyHeroRegen();                  // passive hồi máu mỗi round
            if (_weatherOnThisRound) ApplyWeatherToHero(); // aura biome (round phát tác): mưa hồi máu / nắng-lạnh-dung nham đốt hero
            if (_defeated) { IsPlayerRound = false; return; } // hero gục vì thời tiết → thoát để Run kết thúc trận
            if (_weaponVisual != null) _weaponVisual.ResetAim(); // reset trigger về vị trí ban đầu mỗi round
            EquipSlot(Mathf.Clamp(_lastUsedSlot, 0, Mathf.Max(0, SlotCount - 1))); // mặc định = vũ khí dùng ở round gần nhất
            OnPlayerRoundStart?.Invoke();
            OnUltSelectedChanged?.Invoke();

            _fireSignal = new UniTaskCompletionSource();
            await _fireSignal.Task.AttachExternalCancellation(ct); // chờ tới khi user phát động (FireSelected)

            IsPlayerRound = false;
            HideWeaponVisual();
            OnPlayerRoundEnd?.Invoke();
        }

        /// <summary>Đổi sang vũ khí slot i: đổi model (anim), range + ô target (trigger visual xoay).</summary>
        private void EquipSlot(int slot)
        {
            _selectedSlot = slot;
            if (_ultSelected) { _ultSelected = false; OnUltSelectedChanged?.Invoke(); } // chọn vũ khí → bỏ chọn ult
            string id = SlotWeaponId(slot);
            if (string.IsNullOrEmpty(id)) return;

            // Prefab vũ khí tra theo modelKey (registry key = "41000"...), KHÔNG phải id vũ khí.
            var wpModel = _weapons != null ? _weapons.GetById(id) : default;
            var wp = _registry != null && !string.IsNullOrEmpty(wpModel.modelKey) ? _registry.Weapon(wpModel.modelKey) : null;
            if (_heroRig != null) _heroRig.MountWeapon(wp); // đổi (hoặc gỡ) vũ khí = đổi anim tay của hero

            if (_weaponVisual != null)
            {
                _weaponVisual.gameObject.SetActive(true);
                _weaponVisual.SetWeapon(id);      // đổi range + ô target
                _weaponVisual.SetSpinning(true);  // quét vòng
            }

            OnWeaponChanged?.Invoke(slot);
        }

        /// <summary>Phát động: dừng trigger rồi chạy chuỗi lao đánh (async). Kết thúc player round khi xong.</summary>
        private void FireSelected()
        {
            if (_attacking) return;
            _attacking = true;
            _lastUsedSlot = _selectedSlot; // nhớ vũ khí vừa dùng cho round hero sau
            if (_weaponVisual != null) _weaponVisual.SetSpinning(false);

            string fireId = SlotWeaponId(_selectedSlot);
            if (_weapons != null && _weapons.IsTrap(fireId)) ExecuteTrapPlacement().Forget(); // TRAP: đặt bẫy
            else ExecuteAttack().Forget();                                                     // thường: lao đánh
        }

        /// <summary>
        ///     Chuỗi tấn công của hero: gom enemy trong ô nhắm (ưu tiên máu thấp) → lao đánh từng con
        ///     (damage cộng dồn comboBonus mỗi lần chuyển) → đẩy lùi (va chạm/rìa) → bỏ qua con đã chết.
        /// </summary>
        private async UniTaskVoid ExecuteAttack()
        {
            var ct = this.GetCancellationTokenOnDestroy();
            try
            {
                if (_combat != null && _weapons != null && _selectedSlot >= 0)
                {
                    string id = SlotWeaponId(_selectedSlot);
                    var w = _weapons.GetById(id);
                    int weaponLvl = ArenaUpgradeService.WeaponLevel(id);   // cấp vũ khí đã nâng
                    float wDamage = _weapons.DamageAt(id, weaponLvl);      // damage hiệu dụng theo cấp
                    float wCombo = _weapons.ComboAt(id, weaponLvl);        // comboBonus hiệu dụng theo cấp
                    int facing = _weaponVisual != null ? _weaponVisual.CurrentFacingSector() : 0;
                    int sectors = _arena.Config.SectorsPerRing;

                    // Gom enemy trong tập ô trigger (đã xoay theo hướng nhắm).
                    var targets = new List<ArenaEnemyUnit>();
                    var cells = _weapons.TriggerCells(id);
                    for (int i = 0; i < cells.Count; i++)
                    {
                        int ring = cells[i].y - 1;
                        if (ring < 0 || ring >= _arena.Config.RingCount) continue;
                        int sector = ((facing + cells[i].x) % sectors + sectors) % sectors;
                        var e = _combat.EnemyAt(new GridCell(ring, sector));
                        if (e != null && !targets.Contains(e)) targets.Add(e);
                    }

                    targets.Sort((a, b) => a.CurrentHp.CompareTo(b.CurrentHp)); // máu thấp đánh trước
                    var kb = _weapons.KnockbackSteps(id);
                    int combo = 0;

                    for (int i = 0; i < targets.Count; i++)
                    {
                        var t = targets[i];
                        if (!t.IsAlive) continue; // đã chết (do đẩy lùi va chạm) → bỏ qua

                        await DashHeroTo(t.Cell, ct);
                        float dmg = ScaleHeroDmg(wDamage * (1f + wCombo * combo)); // thời tiết: chỉnh damage hero
                        combo++;
                        t.TakeDamage(dmg, _combat.Occupancy);
                        if (t.IsAlive) _combat.ApplyKnockback(t, kb, w.collisionDamage);
                        await UniTask.Delay(TimeSpan.FromSeconds(0.12f), cancellationToken: ct);
                    }

                    await ReturnHeroToCenter(ct);
                }
            }
            finally
            {
                _attacking = false;
                _fireSignal?.TrySetResult(); // kết thúc player round
            }
        }

        /// <summary>
        ///     TRAP: hero ĐỨNG YÊN + anim, đặt bẫy lên đúng tập ô trigger (đã xoay theo hướng nhắm).
        ///     Bẫy do <see cref="ArenaCombat.PlaceTrap" /> tạo (logic) → view rơi từ trên xuống qua <see cref="HandleTrapPlaced" />.
        /// </summary>
        private async UniTaskVoid ExecuteTrapPlacement()
        {
            var ct = this.GetCancellationTokenOnDestroy();
            try
            {
                if (_combat != null && _weapons != null && _selectedSlot >= 0)
                {
                    string id = SlotWeaponId(_selectedSlot);
                    int facing = _weaponVisual != null ? _weaponVisual.CurrentFacingSector() : 0;
                    int sectors = _arena.Config.SectorsPerRing;
                    int wl = ArenaUpgradeService.WeaponLevel(id);
                    float dmg = ScaleHeroDmg(_weapons.DamageAt(id, wl)); // thời tiết: chỉnh damage bẫy
                    var kb = _weapons.KnockbackSteps(id);
                    int rounds = _weapons.TrapRounds(id);
                    int maxHits = _weapons.TrapMaxHits(id);
                    float colDmg = _weapons.GetById(id).collisionDamage;

                    if (_heroRig != null) _heroRig.PlayAttack(); // đứng yên tại tâm, chỉ chơi anim

                    var cells = _weapons.TriggerCells(id);
                    for (int i = 0; i < cells.Count; i++)
                    {
                        int ring = cells[i].y - 1;
                        if (ring < 0 || ring >= _arena.Config.RingCount) continue;
                        int sector = ((facing + cells[i].x) % sectors + sectors) % sectors;
                        _combat.PlaceTrap(new GridCell(ring, sector), dmg, kb, rounds, maxHits, colDmg);
                    }

                    await UniTask.Delay(TimeSpan.FromSeconds(0.45f), cancellationToken: ct); // chờ bẫy rơi
                    if (_heroRig != null) _heroRig.PlayIdle();
                }
            }
            finally
            {
                _attacking = false;
                _fireSignal?.TrySetResult(); // kết thúc player round
            }
        }

        private void HandleTrapPlaced(ArenaTrap trap)
        {
            float size = _arena.Config.RingThickness * 0.72f;
            var view = ArenaTrapView.Create(_arena.transform, CellWorld(trap.Cell), size);
            trap.OnTriggered = () => { if (view != null) view.Flash(); };
            trap.OnExpired = () => { if (view != null) view.Kill(); };
        }

        private async UniTask DashHeroTo(GridCell cell, CancellationToken ct)
        {
            if (_heroRig == null || _hero == null) return;
            Vector3 target = CellWorld(cell);
            Vector3 center = _arena.CenterWorld;
            Vector3 stop = Vector3.Lerp(target, center, 0.35f); // đứng sát trước enemy

            _heroRig.SetFacing(target.x - center.x);
            _heroRig.PlayMove();
            _hero.transform.DOKill();
            _hero.transform.DOMove(stop, 0.18f).SetEase(Ease.OutQuad);
            await UniTask.Delay(TimeSpan.FromSeconds(0.18f), cancellationToken: ct);
            _heroRig.PlayAttack();
            await UniTask.Delay(TimeSpan.FromSeconds(0.16f), cancellationToken: ct);
        }

        private async UniTask ReturnHeroToCenter(CancellationToken ct)
        {
            if (_heroRig == null || _hero == null) return;
            _heroRig.PlayMove();
            _hero.transform.DOKill();
            _hero.transform.DOMove(_arena.CenterWorld, 0.2f).SetEase(Ease.InQuad);
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: ct);
            _heroRig.PlayIdle();
        }

        private void HideWeaponVisual()
        {
            if (_weaponVisual == null) return;
            _weaponVisual.SetSpinning(false);
            _weaponVisual.gameObject.SetActive(false);
        }

        #endregion

        #region Ultimate

        /// <summary>Kích hoạt ultimate rồi KẾT THÚC round player (giống bắn vũ khí).</summary>
        private async UniTaskVoid ExecuteUltimate()
        {
            var ct = this.GetCancellationTokenOnDestroy();
            try { await ApplyUltimate(ct); }
            finally
            {
                _attacking = false;
                _fireSignal?.TrySetResult(); // qua round
            }
        }

        /// <summary>Kích hoạt SKILL hiện tại của hero (data-driven theo id): effect + power lấy từ SkillCollection.</summary>
        private async UniTask ApplyUltimate(CancellationToken ct)
        {
            if (_combat == null) return;

            var skill = ArenaUpgradeService.CurrentSkill(_heroId);
            if (string.IsNullOrEmpty(skill.id)) { await UniTask.Delay(TimeSpan.FromSeconds(0.1f), cancellationToken: ct); return; }

            var eff = (ArenaSkillEffect)skill.effect;
            float power = skill.power > 0f ? skill.power : 1f;
            float atk = _heroStatsCur.atk > 0f ? _heroStatsCur.atk : 20f;
            if (_heroRig != null) _heroRig.PlayAttack();

            if (eff == ArenaSkillEffect.Heal)
            {
                _heroHp = Mathf.Min(_heroMaxHp, _heroHp + _heroMaxHp * power); // hồi power% máu tối đa
                if (_heroBar != null) _heroBar.SetRatio(_heroHp / _heroMaxHp);
            }
            else
            {
                bool shock = eff == ArenaSkillEffect.Shockwave;
                float dmg = ScaleHeroDmg(atk * power); // thời tiết: chỉnh damage ult hero
                var kb = new List<Vector2Int> { new Vector2Int(0, 1), new Vector2Int(0, 1) }; // đẩy ra 2 ô
                var all = new List<ArenaEnemyUnit>(_combat.Enemies);
                for (int i = 0; i < all.Count; i++)
                {
                    var e = all[i];
                    if (e == null || !e.IsAlive) continue;
                    e.TakeDamage(dmg, _combat.Occupancy);
                    if (shock && e.IsAlive) _combat.ApplyKnockback(e, kb, 0f);
                }
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.35f), cancellationToken: ct);
        }

        #endregion

        #region Enemy attack (lao vào TỪNG CON theo ưu tiên)

        /// <summary>
        ///     Các enemy vào tầm lao vào đánh hero LẦN LƯỢT (không đồng loạt), sắp theo ưu tiên:
        ///     (1) gần hero nhất (ring thấp) → (2) ATK cao → (3) máu thấp → (4) spawn trước (phá hoà TUYỆT ĐỐI,
        ///     nên 2 con dù bằng mọi tiêu chí vẫn có thứ tự cố định — không bao giờ đánh cùng lúc).
        /// </summary>
        private async UniTask PlayEnemyAttacks(CancellationToken ct)
        {
            if (_pendingAttackers.Count == 0) return;

            var list = new List<ArenaEnemyUnit>(_pendingAttackers);
            _pendingAttackers.Clear();
            list.Sort(CompareAttackPriority);

            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || !e.IsAlive) continue;
                await EnemyLungeAttack(e, ct);
                if (_defeated) return; // hero gục giữa chuỗi → dừng các con còn lại
            }
        }

        private static int CompareAttackPriority(ArenaEnemyUnit a, ArenaEnemyUnit b)
        {
            int c = a.Cell.ring.CompareTo(b.Cell.ring);   // 1) gần hero nhất trước
            if (c != 0) return c;
            c = b.Stats.atk.CompareTo(a.Stats.atk);        // 2) ATK cao hơn trước
            if (c != 0) return c;
            c = a.CurrentHp.CompareTo(b.CurrentHp);        // 3) máu thấp hơn trước
            if (c != 0) return c;
            return a.SpawnOrder.CompareTo(b.SpawnOrder);   // 4) phá hoà: con spawn trước
        }

        /// <summary>1 đòn đánh của enemy — rẽ nhánh theo <see cref="EnemyAttackType" />.</summary>
        private async UniTask EnemyLungeAttack(ArenaEnemyUnit e, CancellationToken ct)
        {
            _views.TryGetValue(e, out var rig);
            switch (e.Stats.attackType)
            {
                case EnemyAttackType.Ranged: await RangedAttack(e, rig, ct); break;
                case EnemyAttackType.Charge: await ChargeAttack(e, rig, ct); break;
                case EnemyAttackType.Blink: await BlinkAttack(e, rig, ct); break;
                default: await MeleeAttack(e, rig, ct); break;
            }
        }

        /// <summary>
        ///     GẦN: lao vào ô 1 vòng VỀ PHÍA TÂM (theo vị trí hiện tại, đã kể đẩy lùi) rồi lùi về ô cũ.
        ///     Ring 0 → HERO trúng; ô đích có enemy → enemy đó trúng (friendly fire); ô trống → đánh nhưng không ai trúng.
        /// </summary>
        private async UniTask MeleeAttack(ArenaEnemyUnit e, CharacterRig rig, CancellationToken ct)
        {
            GridCell cur = e.Cell;
            float atk = e.Stats.atk;

            Vector3 targetWorld;
            Action onStrike;
            if (cur.ring <= 0)
            {
                targetWorld = _arena.CenterWorld;
                onStrike = () => DamageHero(atk);
            }
            else
            {
                var target = new GridCell(cur.ring - 1, cur.sector);
                targetWorld = CellWorld(target);
                var occupant = _combat.EnemyAt(target);
                onStrike = occupant != null && occupant != e
                    ? () => { if (occupant.IsAlive) occupant.TakeDamage(atk, _combat.Occupancy); }
                    : (Action)null;
            }

            if (rig == null) { onStrike?.Invoke(); return; }
            await LungeAndReturn(rig, targetWorld, onStrike, ct);
        }

        /// <summary>XA: đứng yên tại chỗ, bắn thẳng vào HERO (không di chuyển).</summary>
        private async UniTask RangedAttack(ArenaEnemyUnit e, CharacterRig rig, CancellationToken ct)
        {
            float atk = e.Stats.atk;
            if (rig == null) { DamageHero(atk); return; }

            rig.SetFacing(_arena.CenterWorld.x - rig.transform.position.x);
            rig.PlayAttack();
            await UniTask.Delay(TimeSpan.FromSeconds(0.22f), cancellationToken: ct);
            DamageHero(atk);
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: ct);
            if (rig != null && e.IsAlive) rig.PlayIdle();
        }

        /// <summary>
        ///     LAO VÀO (bò húc): lao thẳng về tâm theo sector. Trúng ENEMY đầu tiên trên đường → enemy đó ăn
        ///     damage, dừng ở đó rồi lui về. Không có ai chắn → tới tâm, HERO ăn damage, lui về.
        /// </summary>
        private async UniTask ChargeAttack(ArenaEnemyUnit e, CharacterRig rig, CancellationToken ct)
        {
            GridCell cur = e.Cell;
            float atk = e.Stats.atk;

            ArenaEnemyUnit hit = null;
            int stopRing = -1; // -1 = tới tâm (hero)
            for (int r = cur.ring - 1; r >= 0; r--)
            {
                var occ = _combat.EnemyAt(new GridCell(r, cur.sector));
                if (occ != null && occ != e) { hit = occ; stopRing = r; break; }
            }

            Action onStrike = hit != null
                ? () => { if (hit.IsAlive) hit.TakeDamage(atk, _combat.Occupancy); }
                : () => DamageHero(atk);

            if (rig == null) { onStrike(); return; }

            Vector3 dashTarget = stopRing >= 0 ? CellWorld(new GridCell(stopRing, cur.sector)) : _arena.CenterWorld;
            await LungeAndReturn(rig, dashTarget, onStrike, ct, lungeFactor: 0.9f, dashTime: 0.1f);
        }

        /// <summary>DỊCH CHUYỂN: teleport tới sát hero, đánh (hero trúng), rồi teleport về ô cũ.</summary>
        private async UniTask BlinkAttack(ArenaEnemyUnit e, CharacterRig rig, CancellationToken ct)
        {
            float atk = e.Stats.atk;
            if (rig == null) { DamageHero(atk); return; }

            Vector3 home = rig.transform.position;
            Vector3 near = Vector3.Lerp(_arena.CenterWorld, home, 0.18f);

            rig.transform.DOKill();
            rig.transform.position = near;                                 // teleport tới
            rig.transform.DOPunchScale(Vector3.one * 0.2f, 0.15f, 6, 0.5f);
            rig.SetFacing(_arena.CenterWorld.x - near.x);
            rig.PlayAttack();
            await UniTask.Delay(TimeSpan.FromSeconds(0.18f), cancellationToken: ct);
            DamageHero(atk);
            await UniTask.Delay(TimeSpan.FromSeconds(0.15f), cancellationToken: ct);

            if (rig != null)
            {
                rig.transform.position = home;                             // teleport về
                rig.transform.DOPunchScale(Vector3.one * 0.2f, 0.15f, 6, 0.5f);
                if (e.IsAlive) rig.PlayIdle();
            }
        }

        /// <summary>Lao tới <paramref name="targetWorld" />, đánh (gọi <paramref name="onStrike" /> đúng lúc chạm), rồi lùi về ô cũ.</summary>
        private async UniTask LungeAndReturn(CharacterRig rig, Vector3 targetWorld, Action onStrike, CancellationToken ct,
            float lungeFactor = 0.6f, float dashTime = 0.15f)
        {
            Vector3 home = rig.transform.position;
            Vector3 stop = Vector3.Lerp(home, targetWorld, lungeFactor);

            rig.PlayMove();
            rig.transform.DOKill();
            rig.transform.DOMove(stop, dashTime).SetEase(Ease.OutQuad);
            await UniTask.Delay(TimeSpan.FromSeconds(dashTime), cancellationToken: ct);

            rig.PlayAttack();
            onStrike?.Invoke();
            await UniTask.Delay(TimeSpan.FromSeconds(0.18f), cancellationToken: ct);

            if (rig == null) return;
            rig.transform.DOKill();
            rig.transform.DOMove(home, dashTime).SetEase(Ease.InQuad);
            await UniTask.Delay(TimeSpan.FromSeconds(dashTime), cancellationToken: ct);
            if (rig != null) rig.PlayIdle();
        }

        #endregion

        #region Setup

        private void ResolveCollections()
        {
            if (_stages == null) _stages = Resources.Load<ArenaStageCollection>("ArenaCsv/ArenaStageCollection");
            if (_spawns == null) _spawns = Resources.Load<ArenaSpawnCollection>("ArenaCsv/ArenaSpawnCollection");
            if (_enemyStats == null) _enemyStats = Resources.Load<EnemyStatCollection>("ArenaCsv/EnemyStatCollection");
            if (_heroStats == null) _heroStats = Resources.Load<HeroStatCollection>("ArenaCsv/HeroStatCollection");
            if (_patterns == null) _patterns = Resources.Load<EnemyMovePatternCollection>("ArenaCsv/EnemyMovePatternCollection");
            if (_weapons == null) _weapons = Resources.Load<WeaponCollection>("ArenaCsv/WeaponCollection");
            if (_weathers == null) _weathers = Resources.Load<WeatherCollection>("ArenaCsv/WeatherCollection");
        }

        /// <summary>Dựng màn UI gameplay từ prefab (scene sạch). UI tự tìm & bind vào controller; tìm panel thắng/thua theo tên.</summary>
        private void SpawnUiScreen()
        {
            if (_uiScreenPrefab == null) return; // không gán = UI đã nằm sẵn trong scene
            _uiScreen = Instantiate(_uiScreenPrefab);
            _uiScreen.name = _uiScreenPrefab.name;

            if (_defeatUi == null)
            {
                var t = FindDeepByName(_uiScreen.transform, "DEFEATLabel");
                if (t != null) _defeatUi = t.gameObject;
            }

            if (_victoryUi == null)
            {
                var t = FindDeepByName(_uiScreen.transform, "VICTORYLabel");
                if (t != null) _victoryUi = t.gameObject;
            }
        }

        private static Transform FindDeepByName(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeepByName(root.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }

        /// <summary>Dựng (hoặc dựng lại) hero ở tâm theo <paramref name="heroId" />: model + stat + loadout vũ khí.</summary>
        private void SpawnHero(string heroId)
        {
            // Dọn hero + thanh máu cũ (khi đổi hero).
            if (_hero != null)
            {
                _hero.transform.DOKill();
                Destroy(_hero);
            }
            if (_heroBar != null) { _heroBar.Kill(); _heroBar = null; }

            _heroId = heroId;
            _weaponSlots = WeaponsForHero(heroId);

            var prefab = _registry != null ? _registry.Hero(_heroId) : null;
            Vector3 pos = _arena.CenterWorld;
            _hero = prefab != null ? Instantiate(prefab, pos, Quaternion.identity, _arena.transform) : new GameObject("Hero");
            _hero.name = "Hero_" + _heroId;
            _hero.transform.position = pos;

            _heroRig = _hero.GetComponent<CharacterRig>();
            if (_heroRig == null) _heroRig = _hero.AddComponent<CharacterRig>();
            _heroRig.PlayIdle();

            int heroLvl = ArenaUpgradeService.HeroLevel(_heroId); // cấp từ roster đã nâng
            _heroLevel = heroLvl;
            _heroStatsCur = _heroStats != null ? _heroStats.StatsAt(_heroId, heroLvl) : default;
            float baseHp = _heroStatsCur.hp > 0f ? _heroStatsCur.hp : 100f;

            // Passive skill của hero: buff atk (dùng cho ult), +maxHp, giảm damage, regen.
            var passives = ArenaUpgradeService.AggregatePassives(ArenaUpgradeService.HeroSkills(_heroId));
            _heroStatsCur.atk *= 1f + passives.atkPct;
            _heroMaxHp = baseHp * (1f + passives.maxHpPct);
            _heroDmgReduce = Mathf.Clamp01(passives.dmgReducePct);
            _heroRegenPct = passives.regenPct;
            _heroHp = _heroMaxHp;

            float top = MeasureVisualTop(_hero);
            _heroBar = ArenaHealthBar.Create(_arena.transform, 1.1f, 0.16f, 600);
            _heroBar.Follow(_hero.transform, top + 0.2f);
            _heroBar.SetRatio(1f);
        }

        /// <summary>Passive regen của hero mỗi round player.</summary>
        private void ApplyHeroRegen()
        {
            if (_heroRegenPct <= 0f || _heroMaxHp <= 0f || _heroHp <= 0f) return;
            _heroHp = Mathf.Min(_heroMaxHp, _heroHp + _heroMaxHp * _heroRegenPct);
            if (_heroBar != null) _heroBar.SetRatio(_heroHp / _heroMaxHp);
        }

        #region Weather (thời tiết map)

        /// <summary>
        ///     Chọn BIOME CỐ ĐỊNH của map (debug ép trước, không có thì theo stage). Biome giữ nguyên suốt trận;
        ///     mỗi round <see cref="RollWeatherRound" /> random BẬT/TẮT hiệu ứng (vd forest: round mưa / round tạnh).
        /// </summary>
        private void ResolveWeather(string stageWeatherId)
        {
            string id = !string.IsNullOrEmpty(_debugWeatherId) ? _debugWeatherId : stageWeatherId;
            var model = _weathers != null && !string.IsNullOrEmpty(id) ? _weathers.GetById(id) : default;
            _weather = new ArenaWeather(model);
            EnsureWeatherOverlay();     // dựng lớp phủ màu biome (ẩn) — bật/tắt theo round
            ShowWeatherOverlay(false);
            if (_weather.Active) Debug.Log($"[Arena] Biome: {_weather.Name} ({_weather.Type}), activeChance={_weather.ActiveChance}");
        }

        /// <summary>
        ///     Mỗi round: biome cố định nhưng random CÓ "phát tác" hay không (forest → round mưa / round tạnh).
        ///     <see cref="ArenaWeather.ActiveChance" /> &lt;=0 = luôn phát tác; 0..1 = xác suất mỗi round.
        /// </summary>
        private void RollWeatherRound()
        {
            if (!_weather.Active) { _weatherOnThisRound = false; ShowWeatherOverlay(false); return; }
            float chance = _weather.ActiveChance;
            _weatherOnThisRound = chance <= 0f || UnityEngine.Random.value < chance;
            ShowWeatherOverlay(_weatherOnThisRound);
            Debug.Log(_weatherOnThisRound
                ? $"[Arena] {_weather.Name}: round CÓ hiệu ứng"
                : $"[Arena] {_weather.Name}: round TẠNH");
        }

        /// <summary>Nhân damage đòn HERO theo biome — CHỈ khi round đang phát tác.</summary>
        private float ScaleHeroDmg(float dmg) => _weatherOnThisRound ? dmg * _weather.HeroDamageMul : dmg;

        /// <summary>Nhân damage đòn ENEMY (đánh hero) theo biome — CHỈ khi round đang phát tác.</summary>
        private float ScaleEnemyDmg(float dmg) => _weatherOnThisRound ? dmg * _weather.EnemyDamageMul : dmg;

        /// <summary>Aura thời tiết lên hero mỗi PLAYER round: hồi (mưa) rồi mất máu môi trường (nắng/lạnh/dung nham).</summary>
        private void ApplyWeatherToHero()
        {
            if (_heroMaxHp <= 0f || _heroHp <= 0f) return;
            float regen = _weather.HeroRegenPct;
            float dot = _weather.HeroDotPct;
            if (regen <= 0f && dot <= 0f) return;

            if (regen > 0f) _heroHp = Mathf.Min(_heroMaxHp, _heroHp + _heroMaxHp * regen);
            if (dot > 0f) _heroHp = Mathf.Max(0f, _heroHp - _heroMaxHp * dot); // DoT môi trường: KHÔNG qua giảm-damage/mul đòn enemy
            if (_heroBar != null) _heroBar.SetRatio(_heroHp / _heroMaxHp);

            if (_heroHp <= 0f)
            {
                _defeated = true;
                if (_heroRig != null) _heroRig.PlayDie();
            }
            else if (dot > 0f && _heroRig != null)
            {
                _heroRig.PlayDamaged();
            }
        }

        /// <summary>Aura thời tiết lên toàn bộ enemy mỗi ENEMY round: hồi (rừng) rồi bỏng/tê cóng (dung nham/tuyết).</summary>
        private void ApplyWeatherToEnemies()
        {
            if (_combat == null) return;
            float dot = _weather.EnemyDotPct;
            float regen = _weather.EnemyRegenPct;
            if (dot <= 0f && regen <= 0f) return;

            // Snapshot: DoT có thể giết enemy → skill OnDeath (vd slime tách con) sửa danh sách enemy đang lặp.
            var snapshot = new List<ArenaEnemyUnit>(_combat.Enemies);
            for (int i = 0; i < snapshot.Count; i++)
            {
                var e = snapshot[i];
                if (e == null || !e.IsAlive) continue;
                if (regen > 0f)
                {
                    e.Heal(e.MaxHp * regen);
                    if (_bars.TryGetValue(e, out var b) && b != null) b.SetRatio(e.CurrentHp / e.MaxHp);
                }

                if (dot > 0f) e.TakeDamage(e.MaxHp * dot, _combat.Occupancy); // OnHurt cập nhật bar; chết → OnDied dọn view
            }
        }

        /// <summary>
        ///     Dựng (1 LẦN) lớp sprite phủ màu biome — nằm DƯỚI unit, TRÊN grid (chỉ tô nền nhẹ).
        ///     Màu cố định theo biome; bật/tắt theo round qua <see cref="ShowWeatherOverlay" />.
        /// </summary>
        private void EnsureWeatherOverlay()
        {
            if (_weatherOverlay != null || _arena == null) return;
            if (!_weather.TryGetTint(out var tint)) return;

            var go = new GameObject("WeatherOverlay");
            go.transform.SetParent(_arena.transform, false);
            Vector3 c = _arena.CenterWorld;
            go.transform.position = new Vector3(c.x, c.y, c.z + 0.1f); // hơi sau mặt phẳng chơi
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WeatherOverlaySprite();
            sr.color = tint;
            sr.sortingOrder = -5; // grid = -10 → phủ trên nền lưới nhưng dưới unit (>=0)
            float d = _arena.Config.OuterRadius * 2.4f;
            go.transform.localScale = new Vector3(d, d, 1f);
            _weatherOverlay = go;
        }

        /// <summary>Bật/tắt lớp phủ biome (round phát tác → hiện màu; round tạnh → ẩn).</summary>
        private void ShowWeatherOverlay(bool on)
        {
            if (_weatherOverlay != null) _weatherOverlay.SetActive(on);
        }

        private static Sprite _overlaySprite;

        /// <summary>Sprite 1 màu trắng dùng chung cho lớp phủ thời tiết (tô màu bằng SpriteRenderer.color).</summary>
        private static Sprite WeatherOverlaySprite()
        {
            if (_overlaySprite != null) return _overlaySprite;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            _overlaySprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return _overlaySprite;
        }

        #endregion

        /// <summary>Enemy kích hoạt skill (không phải Spawn): Heal = hồi máu cả đội; còn lại = đánh mạnh vào hero.</summary>
        private void HandleEnemyUseSkill(ArenaEnemyUnit unit, ArenaSkillModel skill)
        {
            var eff = (ArenaSkillEffect)skill.effect;
            float power = skill.power > 0f ? skill.power : 1f;

            if (_views.TryGetValue(unit, out var rr) && rr != null)
                rr.transform.DOPunchScale(Vector3.one * 0.25f, 0.35f, 5, 0.5f); // báo hiệu phát skill

            if (eff == ArenaSkillEffect.Heal)
            {
                var all = _combat.Enemies;
                for (int i = 0; i < all.Count; i++)
                {
                    var a = all[i];
                    if (a == null || !a.IsAlive) continue;
                    a.Heal(a.MaxHp * power);
                    if (_bars.TryGetValue(a, out var b) && b != null) b.SetRatio(a.CurrentHp / a.MaxHp);
                }
            }
            else
            {
                DamageHero(unit.Stats.atk * power); // Nuke/Shockwave → đòn mạnh vào hero
            }
        }

        /// <summary>Hero trúng đòn enemy: trừ máu + cập nhật thanh máu + anim damage (chết → anim die).</summary>
        private void DamageHero(float dmg)
        {
            if (_heroMaxHp <= 0f) return;
            dmg = Mathf.Max(0f, dmg) * (1f - Mathf.Clamp01(_heroDmgReduce)); // passive giảm damage
            dmg = ScaleEnemyDmg(dmg);                                        // thời tiết (round phát tác): chỉnh damage đòn enemy
            _heroHp = Mathf.Max(0f, _heroHp - dmg);
            if (_heroBar != null) _heroBar.SetRatio(_heroHp / _heroMaxHp);
            if (_heroHp <= 0f)
            {
                _defeated = true;
                if (_heroRig != null) _heroRig.PlayDie();
            }
            else if (_heroRig != null)
            {
                _heroRig.PlayDamaged();
            }
        }

        #endregion

        #region Enemy view wiring

        private void HandleEnemySpawned(ArenaEnemyUnit unit, ArenaSpawnModel s)
        {
            var prefab = _registry != null ? _registry.Enemy(s.enemyId) : null;
            GameObject go = prefab != null
                ? Instantiate(prefab, Vector3.zero, Quaternion.identity, _arena.transform)
                : MakePlaceholder(Vector3.zero);
            go.name = "Enemy_" + s.enemyId;
            go.transform.localScale = Vector3.one * _enemyScale;

            var rig = go.GetComponent<CharacterRig>();
            if (rig == null) rig = go.AddComponent<CharacterRig>();
            rig.PlayIdle();

            float yOff = MeasureVisualYOffset(go); // bù lệch pivot để sprite CENTER nằm đúng tâm ô
            _viewYOffset[unit] = yOff;
            go.transform.position = PlaceOnCell(unit.Cell, yOff);
            FaceCenter(rig, unit.Cell);
            _views[unit] = rig;

            float barTop = MeasureVisualTop(go);
            var bar = ArenaHealthBar.Create(_arena.transform, 0.8f, 0.12f, 600);
            bar.Follow(go.transform, barTop + 0.12f);
            bar.SetRatio(1f);
            _bars[unit] = bar;

            unit.OnMoved = cell =>
            {
                if (!_views.TryGetValue(unit, out var rr) || rr == null) return;
                Vector3 target = PlaceOnCell(cell, _viewYOffset.TryGetValue(unit, out var o) ? o : 0f);
                rr.PlayMove();
                rr.transform.DOKill();
                rr.transform.DOMove(target, _moveDuration).SetEase(Ease.Linear)
                    .OnComplete(() => { if (rr != null && unit.IsAlive) rr.PlayIdle(); });
                FaceCenter(rr, cell);
            };

            unit.OnPrepare = () =>
            {
                if (_views.TryGetValue(unit, out var rr) && rr != null)
                    rr.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 4, 0.5f);
            };

            unit.OnAttack = () =>
            {
                // KHÔNG đánh ngay: gom lại để lao vào đánh LẦN LƯỢT theo ưu tiên (xem PlayEnemyAttacks).
                if (!_pendingAttackers.Contains(unit)) _pendingAttackers.Add(unit);
            };

            unit.OnIdleBlocked = () =>
            {
                if (_views.TryGetValue(unit, out var rr) && rr != null) rr.PlayIdle();
            };

            unit.OnWander = () => { };

            unit.OnHurt = () =>
            {
                if (_views.TryGetValue(unit, out var rr) && rr != null) rr.PlayDamaged();
                if (_bars.TryGetValue(unit, out var hb) && hb != null)
                    hb.SetRatio(unit.Stats.hp > 0f ? unit.CurrentHp / unit.Stats.hp : 0f);
            };

            unit.OnDied = () =>
            {
                if (_views.TryGetValue(unit, out var rr) && rr != null)
                {
                    rr.transform.DOKill();
                    rr.PlayDie();
                    var goDie = rr.gameObject;
                    DG.Tweening.DOVirtual.DelayedCall(1.1f, () => { if (goDie != null) Destroy(goDie); });
                }

                if (_bars.TryGetValue(unit, out var hb) && hb != null) hb.Kill();
                _bars.Remove(unit);
                _views.Remove(unit);
                _viewYOffset.Remove(unit);
                _pendingAttackers.Remove(unit);
            };
        }

        private Vector3 CellWorld(GridCell cell) => _arena.CellCenterWorld(_arena.Grid.GetCell(cell.ring, cell.sector));

        /// <summary>Vị trí đặt model sao cho TÂM sprite trùng tâm ô (bù lệch pivot theo trục dọc).</summary>
        private Vector3 PlaceOnCell(GridCell cell, float yOffset)
        {
            Vector3 w = CellWorld(cell);
            return new Vector3(w.x, w.y - yOffset, w.z);
        }

        private static float MeasureVisualYOffset(GameObject go)
        {
            var rends = go.GetComponentsInChildren<SpriteRenderer>();
            if (rends.Length == 0) return 0f;
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b.center.y - go.transform.position.y;
        }

        /// <summary>Khoảng cách từ gốc object tới ĐỈNH sprite (để đặt thanh máu nổi trên đầu).</summary>
        private static float MeasureVisualTop(GameObject go)
        {
            var rends = go.GetComponentsInChildren<SpriteRenderer>();
            if (rends.Length == 0) return 0.5f;
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b.max.y - go.transform.position.y;
        }

        private void FaceCenter(CharacterRig rig, GridCell cell)
        {
            Vector3 pos = CellWorld(cell);
            rig.SetFacing(_arena.CenterWorld.x - pos.x);
        }

        private GameObject MakePlaceholder(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(_arena.transform, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.5f;
            return go;
        }

        #endregion
    }
}
