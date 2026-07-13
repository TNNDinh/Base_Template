using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     View 2D cho 1 unit dùng Animator (nhân vật Spriter/sprite-rig). Điều khiển anim theo trạng thái
    ///     <see cref="Unit" />, có thanh máu/mana và lao vào target khi cận chiến.
    ///     Attack/Hit là one-shot: phát xong TỰ VỀ Idle (theo đúng độ dài clip); Die giữ nguyên.
    /// </summary>
    public class UnitView : MonoBehaviour, IUnitView
    {
        #region Fields

        [Header("Refs")]
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _modelRoot;
        [SerializeField] private Transform _hitPoint;
        [SerializeField] private Image _hpFill;
        [SerializeField] private Image _manaFill;

        [Header("Animator State Names")]
        [SerializeField] private string _idleState = "Idle";
        [SerializeField] private string _attackState = "Slashing";
        [SerializeField] private string _hitState = "Hurt";
        [SerializeField] private string _dieState = "Dying";
        [SerializeField] private string _walkState = "Walking";

        [Tooltip("Fallback độ dài anim one-shot nếu không đọc được clip (giây).")]
        [SerializeField] private float _defaultOnceDuration = 0.5f;

        public Unit Bound { get; private set; }

        private Vector3 _home;
        private bool _lunging;
        private bool _attacking; // đang trong pha tấn công → không cho hurt chen vào
        private bool _dead;

        private global::System.Action _pendingImpact; // callback chạm đòn (target flinch)
        private bool _impactFired;                      // đã fire chưa (event hoặc fallback, chỉ 1 lần)
        private Tween _dieTween;                        // fallback timer chết → ẩn
        private bool _deathEnded;                        // đã kết thúc anim chết (fade+ẩn) chưa
        private Tween _returnTween;                    // hẹn giờ về Idle sau one-shot
        private readonly Dictionary<string, float> _clipLen = new Dictionary<string, float>();

        #endregion

        #region Public

        public void Bind(Unit unit)
        {
            Bound = unit;
            _dead = false;
            _home = transform.position;
            CacheClipLengths();

            if (_modelRoot != null)
            {
                var s = _modelRoot.localScale;
                var sign = unit.Team == BattleTeam.Player ? 1f : -1f; // flip hướng nhìn
                _modelRoot.localScale = new Vector3(Mathf.Abs(s.x) * sign, s.y, s.z);
            }

            SetHp(unit.CurrentHp, unit.MaxHp);
            SetMana(unit.Mana, unit.MaxMana);
            PlayIdle();
        }

        public void PlayIdle()
        {
            if (_dead) return;
            KillReturn();
            Play(_idleState);
        }

        public void PlayAttack()
        {
            _attacking = true;
            PlayOnce(_attackState);
        }

        public void PlayHit()
        {
            if (_dead || _attacking || _lunging) return; // đang tấn công/lao → không flinch (attack ưu tiên)
            PlayOnce(_hitState);
        }

        public void PlayDie()
        {
            _dead = true;
            _deathEnded = false;
            KillReturn();
            KillDie();
            Play(_dieState); // phát anim chết 1 lần (clip non-loop) — AnimationEvent cuối clip gọi OnDeathEnd

            // Fallback: nếu clip Dying chưa gắn event OnDeathEnd thì tự kết thúc sau độ dài clip.
            _dieTween = DOVirtual.DelayedCall(ClipLength(_dieState) + 0.5f, OnDeathEnd, false);
        }

        /// <summary>Gọi từ AnimationEvent "OnDeathEnd" ở cuối clip Dying (qua <see cref="AnimEventRelay" />): mờ dần → ẩn.</summary>
        public void OnDeathEnd()
        {
            if (!_dead || _deathEnded) return;
            _deathEnded = true;

            var srs = GetComponentsInChildren<SpriteRenderer>();
            if (srs.Length == 0) { if (this != null) gameObject.SetActive(false); return; }

            var seq = DOTween.Sequence();
            var first = true;
            foreach (var sr in srs)
            {
                var t = sr.DOFade(0f, 0.35f);
                if (first) { seq.Append(t); first = false; }
                else seq.Join(t);
            }

            seq.OnComplete(() => { if (this != null) gameObject.SetActive(false); });
            _dieTween = seq;
        }

        public void Revive()
        {
            KillReturn();
            KillDie();
            if (this != null && !gameObject.activeSelf) gameObject.SetActive(true);
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            {
                var c = sr.color; c.a = 1f; sr.color = c; // hiện lại rõ
            }

            _dead = false;
            _deathEnded = false;
            _lunging = false;
            _attacking = false;
            if (_animator != null) _animator.speed = 1f;
            PlayIdle();
        }

        private void KillDie()
        {
            if (_dieTween != null && _dieTween.IsActive()) _dieTween.Kill();
            _dieTween = null;
        }

        public void SetHp(float current, float max)
        {
            if (_hpFill != null) _hpFill.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }

        public void SetMana(float current, float max)
        {
            if (_manaFill != null) _manaFill.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }

        /// <summary>Gọi từ AnimationEvent "OnAttackImpact" (qua <see cref="AnimEventRelay" />) — chạm đòn đúng frame.</summary>
        public void OnAttackImpact() => FireImpact();

        public void AttackTarget(Vector3 targetPos, float range, global::System.Action onImpact)
        {
            if (_dead) { onImpact?.Invoke(); return; }
            if (_lunging) { onImpact?.Invoke(); return; } // đang lao → xử lý ngay, khỏi lao lại

            _pendingImpact = onImpact;
            _impactFired = false;

            // Fallback: nếu clip KHÔNG có AnimationEvent, tự fire ở ~55% clip (đo từ lúc anim attack bắt đầu).
            var fallbackT = Mathf.Max(0.05f, ClipLength(_attackState) * 0.55f);
            var dist = Vector3.Distance(transform.position, targetPos);

            if (dist <= range)
            {
                // Tầm xa: đứng đánh — AnimationEvent trong clip sẽ gọi OnAttackImpact.
                PlayAttack();
                DOVirtual.DelayedCall(fallbackT, FireImpact, false);
                return;
            }

            // Cận chiến: lao tới ĐỨNG TRƯỚC MẶT target (cùng độ cao, phía đội mình), cách = range.
            _lunging = true;
            _attacking = true;
            KillReturn();
            var side = Bound != null && Bound.Team == BattleTeam.Player ? -1f : 1f; // player đứng bên trái enemy, enemy bên phải
            var stop = new Vector3(targetPos.x + side * range, targetPos.y, _home.z);

            Play(_walkState);
            DOTween.Sequence()
                .Append(transform.DOMove(stop, 0.2f).SetEase(Ease.OutQuad))
                .AppendCallback(() => Play(_attackState))
                .AppendInterval(fallbackT)
                .AppendCallback(FireImpact)                        // fallback nếu clip chưa gắn event
                .AppendInterval(0.15f)
                .AppendCallback(() => Play(_walkState))
                .Append(transform.DOMove(_home, 0.2f).SetEase(Ease.InQuad))
                .OnComplete(() => { _lunging = false; _attacking = false; if (!_dead) Play(_idleState); });
        }

        private void FireImpact()
        {
            if (_impactFired) return;
            _impactFired = true;
            var cb = _pendingImpact;
            _pendingImpact = null;
            cb?.Invoke();
        }

        public Transform HitPoint => _hitPoint != null ? _hitPoint : transform;

        #endregion

        #region Private

        /// <summary>Phát anim 1 lần rồi tự về Idle sau đúng độ dài clip.</summary>
        private void PlayOnce(string state)
        {
            if (_dead || _animator == null || string.IsNullOrEmpty(state)) return;
            Play(state);
            KillReturn();
            var dur = ClipLength(state);
            _returnTween = DOVirtual.DelayedCall(dur, () =>
            {
                _attacking = false;
                if (!_dead && !_lunging) Play(_idleState);
            }, false);
        }

        private void Play(string state)
        {
            if (_animator == null || string.IsNullOrEmpty(state)) return;
            _animator.CrossFade(state, 0.1f);
        }

        private void KillReturn()
        {
            if (_returnTween != null && _returnTween.IsActive()) _returnTween.Kill();
            _returnTween = null;
        }

        private void CacheClipLengths()
        {
            _clipLen.Clear();
            if (_animator == null || _animator.runtimeAnimatorController == null) return;
            foreach (var clip in _animator.runtimeAnimatorController.animationClips)
                if (clip != null) _clipLen[clip.name] = clip.length;
        }

        private float ClipLength(string state)
        {
            return _clipLen.TryGetValue(state, out var len) && len > 0.01f ? len : _defaultOnceDuration;
        }

        private void OnDestroy() { KillReturn(); KillDie(); }

        #endregion
    }
}
