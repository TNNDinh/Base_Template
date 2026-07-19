using UnityEngine;
using UnityEngine.UI;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Thanh 3 nút vũ khí, CHỈ hiện ở round player (nghe <see cref="ArenaSceneController" />).
    ///     Bấm 1 nút = đổi sang vũ khí đó (anim/range/ô target). Bấm LẠI đúng nút đang chọn = phát động tấn công.
    ///     Ẩn/hiện bằng CanvasGroup (không SetActive cả screen). Glow = highlight slot đang chọn.
    /// </summary>
    public class ArenaWeaponBar : MonoBehaviour
    {
        [SerializeField] private ArenaSceneController _controller;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Button[] _buttons;          // 3 nút
        [SerializeField] private Text[] _labels;             // tuỳ chọn: tên vũ khí
        [SerializeField] private GameObject[] _selectedGlow; // tuỳ chọn: viền slot đang chọn
        [SerializeField] private WeaponCollection _weapons;  // để hiện tên

        private void Awake()
        {
            if (_controller == null) _controller = FindFirstObjectByType<ArenaSceneController>(); // prefab → tự tìm controller trong scene
            if (_group == null) _group = GetComponent<CanvasGroup>();
            if (_weapons == null) _weapons = Resources.Load<WeaponCollection>("ArenaCsv/WeaponCollection");

            for (int i = 0; i < _buttons.Length; i++)
            {
                if (_buttons[i] == null) continue;
                int idx = i;
                _buttons[i].onClick.AddListener(() => { if (_controller != null) _controller.OnWeaponButton(idx); });
            }

            SetVisible(false);
        }

        private void OnEnable()
        {
            if (_controller == null) return;
            _controller.OnPlayerRoundStart += HandleStart;
            _controller.OnPlayerRoundEnd += HandleEnd;
            _controller.OnWeaponChanged += HandleChanged;
            _controller.OnHeroChanged += RefreshLabels; // đổi hero → loadout đổi → cập nhật nhãn
        }

        private void OnDisable()
        {
            if (_controller == null) return;
            _controller.OnPlayerRoundStart -= HandleStart;
            _controller.OnPlayerRoundEnd -= HandleEnd;
            _controller.OnWeaponChanged -= HandleChanged;
            _controller.OnHeroChanged -= RefreshLabels;
        }

        private void HandleStart()
        {
            RefreshLabels();
            SetVisible(true);
            HandleChanged(_controller.SelectedSlot);
        }

        private void HandleEnd() => SetVisible(false);

        private void HandleChanged(int slot)
        {
            if (_selectedGlow == null) return;
            for (int i = 0; i < _selectedGlow.Length; i++)
                if (_selectedGlow[i] != null)
                    _selectedGlow[i].SetActive(i == slot);
        }

        private void RefreshLabels()
        {
            if (_labels == null || _weapons == null || _controller == null) return;
            for (int i = 0; i < _labels.Length; i++)
            {
                if (_labels[i] == null) continue;
                string id = _controller.SlotWeaponId(i);
                _labels[i].text = string.IsNullOrEmpty(id) ? "" : _weapons.GetById(id).name;
            }
        }

        private void SetVisible(bool on)
        {
            if (_group == null) return;
            _group.alpha = on ? 1f : 0f;
            _group.interactable = on;
            _group.blocksRaycasts = on;
        }
    }
}
