using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Ezg.Feature.Shared.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     HUD hero (tự dựng UI bằng code — đặt 1 GameObject có script này lên ArenaCanvas):
    ///     <list type="bullet">
    ///         <item>Nút HERO (góc trên-phải): bấm để ĐỔI hero (cycle) — stat + loadout vũ khí + model đổi theo.</item>
    ///         <item>Nút ULT (góc dưới-phải, chỉ hiện ở round player): tap 1 = chọn, tap 2 = kích hoạt & qua round.</item>
    ///     </list>
    /// </summary>
    public class ArenaHeroHud : MonoBehaviour
    {
        [SerializeField] private ArenaSceneController _controller;

        private static readonly Color BtnColor = new Color(0.35f, 0.28f, 0.6f, 1f);
        private static readonly Color UltColor = new Color(0.85f, 0.45f, 0.15f, 1f);
        private static readonly Color UltSelected = new Color(1f, 0.8f, 0.25f, 1f);

        private Font _font;
        private Text _heroLabel;
        private Button _ultButton;
        private Image _ultImage;
        private Text _ultLabel;

        private void Awake()
        {
            if (_controller == null) _controller = FindFirstObjectByType<ArenaSceneController>();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            Build();
        }

        private void OnEnable()
        {
            if (_controller == null) return;
            _controller.OnHeroChanged += RefreshHero;
            _controller.OnPlayerRoundStart += ShowUlt;
            _controller.OnPlayerRoundEnd += HideUlt;
            _controller.OnUltSelectedChanged += RefreshUlt;
        }

        private void OnDisable()
        {
            if (_controller == null) return;
            _controller.OnHeroChanged -= RefreshHero;
            _controller.OnPlayerRoundStart -= ShowUlt;
            _controller.OnPlayerRoundEnd -= HideUlt;
            _controller.OnUltSelectedChanged -= RefreshUlt;
        }

        private void Build()
        {
            var heroBtn = MakeButton("HeroBtn", new Vector2(1f, 1f), new Vector2(340f, 92f),
                new Vector2(-190f, -70f), BtnColor, out _heroLabel);
            heroBtn.onClick.AddListener(() =>
            {
                // Mở feature screen chuẩn (prefab + FeatureBaseController) qua UIManager.
                try { UIManager.Instance.Show(GameEnums.Features.ArenaLoadout).Forget(); }
                catch { if (_controller != null) _controller.CycleHero(); } // fallback khi UIManager chưa init
                RefreshHero();
            });

            // Nút STAGE (dưới nút HERO): mở màn chọn map/stage.
            var stageBtn = MakeButton("StageBtn", new Vector2(1f, 1f), new Vector2(340f, 84f),
                new Vector2(-190f, -168f), new Color(0.28f, 0.45f, 0.62f, 1f), out var stageLabel);
            stageLabel.text = "STAGE";
            stageBtn.onClick.AddListener(() => { UIManager.Instance.Show(GameEnums.Features.ArenaStageSelect).Forget(); });

            _ultButton = MakeButton("UltBtn", new Vector2(1f, 0f), new Vector2(210f, 120f),
                new Vector2(-130f, 340f), UltColor, out var ultLabel);
            ultLabel.text = "ULT";
            ultLabel.fontSize = 46;
            ultLabel.fontStyle = FontStyle.Bold;
            _ultLabel = ultLabel;
            _ultImage = _ultButton.GetComponent<Image>();
            _ultButton.onClick.AddListener(() => { if (_controller != null) _controller.OnUltimateButton(); });

            RefreshHero();
            HideUlt();
        }

        private void RefreshHero()
        {
            if (_heroLabel != null && _controller != null) _heroLabel.text = "HERO: " + _controller.HeroName;
        }

        private void ShowUlt() { if (_ultButton != null) _ultButton.gameObject.SetActive(true); RefreshUlt(); }
        private void HideUlt() { if (_ultButton != null) _ultButton.gameObject.SetActive(false); }

        private void RefreshUlt()
        {
            if (_ultImage == null || _controller == null) return;
            int cd = _controller.UltCooldownLeft;
            if (cd > 0)
            {
                _ultImage.color = new Color(0.3f, 0.3f, 0.32f, 1f); // xám = đang cooldown
                if (_ultLabel != null) _ultLabel.text = "CD " + cd;
                if (_ultButton != null) _ultButton.interactable = false;
            }
            else
            {
                _ultImage.color = _controller.UltSelected ? UltSelected : UltColor;
                if (_ultLabel != null) _ultLabel.text = "ULT";
                if (_ultButton != null) _ultButton.interactable = true;
            }
        }

        private Button MakeButton(string name, Vector2 anchor, Vector2 size, Vector2 anchoredPos, Color color, out Text label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            go.GetComponent<Image>().color = color;

            var lbl = new GameObject("Label", typeof(RectTransform), typeof(Text));
            lbl.transform.SetParent(go.transform, false);
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            label = lbl.GetComponent<Text>();
            label.font = _font; label.text = ""; label.fontSize = 38;
            label.alignment = TextAnchor.MiddleCenter; label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Overflow; label.verticalOverflow = VerticalWrapMode.Overflow;
            return go.GetComponent<Button>();
        }
    }
}
