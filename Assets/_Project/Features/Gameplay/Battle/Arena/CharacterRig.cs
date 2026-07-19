using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     View điều khiển 1 nhân vật rig 2D (SpriteSkin + Animator) của bộ art ST09.
    ///     Anim theo tên clip: <c>idle / move / die</c>. Hero KHÔNG có clip attack — đòn đánh do
    ///     prefab VŨ KHÍ (gắn vào bone <c>b_weapon</c>) tự chơi clip <c>attack</c>; đổi vũ khí = đổi
    ///     prefab. Enemy có sẵn clip attack trong thân. <c>damaged</c> không có trong art nên fake
    ///     bằng code (nháy đỏ + rung).
    /// </summary>
    public class CharacterRig : MonoBehaviour
    {
        #region Fields

        private const string WeaponBoneName = "b_weapon";
        private const float DamagedFlashDuration = 0.12f;
        private const float DamagedShakeStrength = 0.18f;

        [Header("Animator (để trống = tự lấy ở gốc)")]
        [SerializeField] private Animator _animator;

        [Header("Tên state Animator")]
        [SerializeField] private string _idleState = "idle";
        [SerializeField] private string _moveState = "move";
        [SerializeField] private string _attackState = "attack";
        [SerializeField] private string _dieState = "die";

        [Header("Điểm gắn vũ khí (để trống = tự tìm bone b_weapon)")]
        [SerializeField] private Transform _weaponMount;

        [Header("Damaged (fake, không có clip)")]
        [SerializeField] private Color _damagedColor = new Color(1f, 0.35f, 0.35f, 1f);

        [Header("Ẩn các lớp guide của art (root/label…)")]
        [SerializeField] private bool _hideGuideLayers = true;
        [SerializeField] private string[] _guideLayerNames = { "root", "root copy", "BG" };

        private Animator _weaponAnimator;
        private GameObject _weapon;
        private SpriteRenderer[] _renderers;
        private Color[] _baseColors;
        private bool _dead;
        private bool _inited;
        private Sequence _damagedTween;
        private Tween _returnIdleTween;
        private Tween _dieLoopTween;

        #endregion

        #region Initialize

        private void Awake()
        {
            EnsureInit();
        }

        /// <summary>Khởi tạo lười — an toàn cả khi gọi ở edit mode (Awake chưa chạy).</summary>
        private void EnsureInit()
        {
            if (_inited) return;
            _inited = true;

            if (_animator == null) _animator = GetComponent<Animator>();
            if (_weaponMount == null) _weaponMount = FindDeep(transform, WeaponBoneName);

            var all = GetComponentsInChildren<SpriteRenderer>(true);
            var visible = new List<SpriteRenderer>(all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                if (_hideGuideLayers && IsGuideLayer(all[i]))
                {
                    all[i].enabled = false;
                    continue;
                }

                visible.Add(all[i]);
            }

            _renderers = visible.ToArray();
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++) _baseColors[i] = _renderers[i].color;
        }

        private bool IsGuideLayer(SpriteRenderer sr)
        {
            if (sr == null) return false;

            for (int i = 0; i < _guideLayerNames.Length; i++)
                if (sr.name == _guideLayerNames[i]) return true;

            // Lớp label id: tên toàn chữ số (vd "44000") → ẩn.
            string n = sr.name.Trim();
            if (n.Length == 0) return false;
            for (int i = 0; i < n.Length; i++)
                if (!char.IsDigit(n[i])) return false;
            return true;
        }

        private void OnDestroy()
        {
            KillTween(_damagedTween);
            KillTween(_returnIdleTween);
            KillTween(_dieLoopTween);
        }

        private void DestroyWeapon()
        {
            if (_weapon == null) return;
            if (Application.isPlaying) Destroy(_weapon);
            else DestroyImmediate(_weapon);
            _weapon = null;
            _weaponAnimator = null;
        }

        #endregion

        #region Public

        public bool IsDead => _dead;
        public Transform WeaponMount => _weaponMount;

        /// <summary>Gắn (thay) prefab vũ khí vào bone tay; vũ khí tự chơi clip idle của nó.</summary>
        public GameObject MountWeapon(GameObject weaponPrefab)
        {
            EnsureInit();
            if (_weapon != null) DestroyWeapon();
            _weapon = null;
            _weaponAnimator = null;
            if (weaponPrefab == null || _weaponMount == null) return null;

            _weapon = Instantiate(weaponPrefab, _weaponMount);
            _weapon.transform.localPosition = Vector3.zero;
            _weapon.transform.localRotation = Quaternion.identity;
            _weapon.transform.localScale = Vector3.one;
            _weaponAnimator = _weapon.GetComponent<Animator>();
            PlayWeapon(_idleState);
            return _weapon;
        }

        public void PlayIdle()
        {
            EnsureInit();
            if (_dead) return;
            KillTween(_returnIdleTween);
            PlayBody(_idleState);
            PlayWeapon(_idleState);
        }

        public void PlayMove()
        {
            if (_dead) return;
            KillTween(_returnIdleTween);
            PlayBody(_moveState);
            PlayWeapon(_idleState);
        }

        /// <summary>Đánh: nếu có vũ khí → chơi attack của vũ khí; enemy (không vũ khí) → attack ở thân. Xong về idle.</summary>
        public void PlayAttack()
        {
            EnsureInit();
            if (_dead) return;

            float len;
            if (_weaponAnimator != null)
            {
                PlayWeapon(_attackState, true);
                len = StateLength(_weaponAnimator, _attackState);
            }
            else
            {
                PlayBody(_attackState, true);
                len = StateLength(_animator, _attackState);
            }

            KillTween(_returnIdleTween);
            _returnIdleTween = DOVirtual.DelayedCall(Mathf.Max(0.1f, len), PlayIdle, false);
        }

        /// <summary>Trúng đòn (fake): nháy màu + rung nhẹ. Không chen khi đang chết.</summary>
        public void PlayDamaged()
        {
            EnsureInit();
            if (_dead || _renderers == null) return;

            KillTween(_damagedTween);
            var seq = DOTween.Sequence();
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                int idx = i;
                seq.Join(_renderers[idx].DOColor(_damagedColor, DamagedFlashDuration * 0.4f)
                    .OnComplete(() => { if (_renderers[idx] != null) _renderers[idx].DOColor(_baseColors[idx], DamagedFlashDuration * 0.6f); }));
            }

            transform.DOShakePosition(DamagedFlashDuration, DamagedShakeStrength, 20, 90f, false, true);
            _damagedTween = seq;
        }

        public void PlayDie()
        {
            EnsureInit();
            _dead = true;
            KillTween(_returnIdleTween);
            KillTween(_damagedTween);
            KillTween(_dieLoopTween);
            if (_animator != null) _animator.speed = 1f;
            PlayBody(_dieState, true);
            PlayWeapon(_dieState, true); // vũ khí thường không có 'die' → PlayOn bỏ qua nếu thiếu state

            // Chơi 1 LẦN rồi GIỮ khung cuối: sau độ dài clip, nhảy tới frame cuối + dừng animator
            // (an toàn kể cả khi clip 'die' bị bật Loop Time — không cho lặp lại anim chết).
            float len = StateLength(_animator, _dieState);
            if (len > 0.05f)
                _dieLoopTween = DOVirtual.DelayedCall(len, FreezeDieLastFrame, false);
        }

        /// <summary>Giữ nguyên khung cuối anim chết (dừng animator) — không loop.</summary>
        private void FreezeDieLastFrame()
        {
            if (!_dead || _animator == null) return;
            int hash = Animator.StringToHash(_dieState);
            if (_animator.HasState(0, hash)) _animator.Play(hash, 0, 1f); // frame cuối clip
            _animator.speed = 0f;
        }

        /// <summary>Lật hướng nhìn: dir &gt; 0 nhìn phải, &lt; 0 nhìn trái.</summary>
        public void SetFacing(float dir)
        {
            if (Mathf.Approximately(dir, 0f)) return;
            var s = transform.localScale;
            transform.localScale = new Vector3(Mathf.Abs(s.x) * Mathf.Sign(dir), s.y, s.z);
        }

        #endregion

        #region Private

        private void PlayBody(string state, bool fromStart = false)
        {
            PlayOn(_animator, state, fromStart);
        }

        private void PlayWeapon(string state, bool fromStart = false)
        {
            PlayOn(_weaponAnimator, state, fromStart);
        }

        private static void PlayOn(Animator anim, string state, bool fromStart)
        {
            if (anim == null || string.IsNullOrEmpty(state)) return;
            int hash = Animator.StringToHash(state);
            if (!anim.HasState(0, hash)) return; // state không tồn tại → bỏ qua an toàn
            if (fromStart) anim.Play(hash, 0, 0f);
            else anim.CrossFade(hash, 0.1f);
        }

        private static float StateLength(Animator anim, string state)
        {
            if (anim == null || anim.runtimeAnimatorController == null) return 0.5f;
            foreach (var clip in anim.runtimeAnimatorController.animationClips)
                if (clip != null && clip.name == state) return clip.length;
            return 0.5f;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }

        private static void KillTween(Tween t)
        {
            if (t != null && t.IsActive()) t.Kill();
        }

        #endregion
    }
}
