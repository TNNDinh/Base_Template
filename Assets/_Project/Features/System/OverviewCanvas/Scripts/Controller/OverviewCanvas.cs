using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Ezg.Core.Utils;
using Ezg.Package.Pooling;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.Config;

namespace Ezg.Feature.System.OverviewCanvas
{
    /// <summary>
    ///     Canvas tổng nằm trên mọi canvas
    /// </summary>
    public class OverviewCanvas : FeatureBaseController
    {
        #region Fields

        [SerializeField] [TabGroup("Loaing scene")]
        public Transform ParentSimpleMessage;

        [SerializeField] [TabGroup("Loaing scene")]
        private GameObject _simpleMessageTemplate;

        [SerializeField] [TabGroup("Loaing scene")]
        private GameObject _simpleMessageSetPosTemplate;


        [SerializeField] [TabGroup("Loaing scene")]
        private GameObject _loadingObject;

        [SerializeField] [TabGroup("Loaing scene")]
        private CanvasGroup _loadingCanvasGrp;

        [SerializeField] [TabGroup("Loaing scene")]
        private Image _loadingProgress;

        [SerializeField] [TabGroup("Loaing scene")]
        private GameObject _waitingPurchase;

        [SerializeField] [TabGroup("Message box")]
        private Button _backgroundButton;

        [SerializeField] [TabGroup("Message box")]
        private GameObject _messageBox;

        [SerializeField] [TabGroup("Message box")]
        private GameObject _messageBoxPopup;

        [SerializeField] [TabGroup("Message box")]
        private Button _acceptButton;

        [SerializeField] [TabGroup("Message box")]
        private Button _cancelButton;

        [SerializeField] [TabGroup("Message box")]
        private Text _titleText;

        [SerializeField] [TabGroup("Message box")]
        private Text _textContent;

        // [SerializeField]
        // [TabGroup("Message box")]
        // private CurrencyPreviewController _currency;

        [SerializeField] [TabGroup("Message box")]
        private Sprite _focusSprite;

        [SerializeField] [TabGroup("Message box")]
        private Sprite _notFocusSprite;

        [SerializeField] [TabGroup("General")] private Text _versionText;

        [SerializeField] [TabGroup("Target Item")]
        private Transform screenTargetItem;

        [SerializeField] [TabGroup("Target Item")]
        private Transform posTargetItem;

        public Transform PosTargetItem => posTargetItem;

        [SerializeField] [TabGroup("Target Item")]
        private Transform animTargetItem;

        [SerializeField] [TabGroup("Target Item")]
        private Vector3 posStartAnim;

        [SerializeField] [TabGroup("Target Item")]
        private Vector3 posEndAnim;

        private UnityAction _acceptAction, _cancelAction;
        private Image _messageBoxBackImage;
        private Image _acceptButtonImg, _cancelButtonImg;
        private bool _isAdventureHeroLoaded;
        private Tween _animTargetDelayTween;

        #endregion

        #region Functions

        protected override void Awake()
        {
            base.Awake();
            EventManager.StartListening(nameof(EventName.OnLoadAccountSuccess), LoadAccountSuccess);
            var canvasComponent = GetComponent<Canvas>();
            canvasComponent.worldCamera = Camera.main;
            canvasComponent.sortingLayerName = nameof(EnumBase.LayerSorting.CurrencyBar);
            GameSystems.OverviewCanvasController = this;
            GameSystems.SimpleMessageTemplate = _simpleMessageTemplate;
            GameSystems.SimpleMessageSetPosTemplate = _simpleMessageSetPosTemplate;
            _acceptButton.onClick.AddListener(OnAccept);
            _cancelButton.onClick.AddListener(OnCancel);
            _backgroundButton.onClick.AddListener(() => OnClose());
            _messageBoxBackImage = _messageBox.GetComponent<Image>();
            _acceptButtonImg = _acceptButton.GetComponent<Image>();
            _cancelButtonImg = _cancelButton.GetComponent<Image>();
            UIManager.CloseMessageBoxAction = OnCancel;
            _versionText.text = $"v.{Application.version}";
            LoadAccountSuccess();
        }

        private void LoadAccountSuccess()
        {
            //_versionText.text = $"v.{Application.version}{(string.IsNullOrEmpty(PlayerDataManager.Account.Email) ? "." : "")}";
        }

        public async UniTask ChangeScene(GameEnums.Scenes scene)
        {
            EventManager.EmitEvent(EventName.ClearAllTut);
            UIManager.EnableTouch(false);
            _loadingObject.SetActive(false);
            _loadingCanvasGrp.alpha = 0;
            await UniTask.WaitForEndOfFrame(this);
            _loadingObject.SetActive(true);
            _loadingCanvasGrp.DOFade(1f, .3f).SetUpdate(true);
            await UniTask.Delay(300, DelayType.Realtime);

            PoolingManager.Instance.ClearCache();
            UIManager.Instance.ResetDataWhenChangeScene();
            await Resources.UnloadUnusedAssets();
            UIManager.Instance.SetCurrentScene(scene);
            EventManager.EmitEvent(nameof(EventName.OnChangeScene));
            var asyncLoadLevel = SceneManager.LoadSceneAsync(scene.ToString());

            while (!asyncLoadLevel.isDone)
            {
                _loadingProgress.fillAmount = asyncLoadLevel.progress;
                await UniTask.Yield();
            }

            PoolingManager.Instance.EnableReturnPool();
            Time.timeScale = 1;
            await UniTask.Delay(3000, DelayType.Realtime);
            _loadingCanvasGrp.DOFade(0, .3f).SetUpdate(true);
            await UniTask.Delay(300, DelayType.Realtime);
            _loadingObject.SetActive(false);
            UIManager.EnableTouch(true);
            EventManager.EmitEvent(nameof(EventName.OnChangedScene));
        }

        public async UniTask OpenLoading()
        {
            UIManager.EnableTouch(false);
            _loadingCanvasGrp.alpha = 0;
            _loadingCanvasGrp.DOFade(1f, .3f).SetUpdate(true);
            _loadingObject.SetActive(true);
            await UniTask.Delay(300, DelayType.Realtime);

            PoolingManager.Instance.ClearCache();
            //UIManager.Instance.ResetDataWhenChangeScene();
            await Resources.UnloadUnusedAssets();
            PoolingManager.Instance.EnableReturnPool();
            Time.timeScale = 1;
            await UniTask.Delay(1500, DelayType.Realtime);
            _loadingCanvasGrp.DOFade(0, .3f).SetUpdate(true);
            await UniTask.Delay(300, DelayType.Realtime);
            _loadingObject.SetActive(false);
            UIManager.EnableTouch(true);
            EventManager.EmitEvent(nameof(EventName.OnChangedScene));
        }

        public void WaitingPurchase(bool isWait)
        {
            _waitingPurchase.SetActive(isWait);
        }

        public bool IsShowWaitingScreen()
        {
            return _waitingPurchase.activeSelf;
        }

        public void ShowMessage(string content, string title = null, bool focusYes = true)
        {
            UIManager.IsShowMessageBox = true;
            _titleText.text = string.IsNullOrEmpty(title) ? GameSystems.Localize("information") :
                title.Contains("_") ? GameSystems.Localize(title) : title;
            _textContent.text = content.Contains("_") ? GameSystems.Localize(content) : content;
            _cancelButton.gameObject.SetActive(false);
            _acceptAction = null;
            _backgroundButton.interactable = true;
            _messageBox.gameObject.SetActive(true);
            //_currency.gameObject.SetActive(false);
            _acceptButtonImg.sprite = focusYes ? _focusSprite : _notFocusSprite;
            _cancelButtonImg.sprite = !focusYes ? _focusSprite : _notFocusSprite;
        }

        public void ShowMessage(string content, string title = null, UnityAction acceptAction = null,
            UnityAction cancelAction = null, EnumBase.MoneyTypes moneyType = EnumBase.MoneyTypes.None, long value = -1,
            bool focusYes = true, bool showCancelButton = true)
        {
            UIManager.IsShowMessageBox = true;
            _titleText.text = string.IsNullOrEmpty(title) ? GameSystems.Localize("information") :
                title.Contains("_") ? GameSystems.Localize(title) : title;
            _textContent.text = content.Contains("_") ? GameSystems.Localize(content) : content;
            _cancelButton.gameObject.SetActive(showCancelButton);
            _acceptAction = acceptAction;
            _cancelAction = cancelAction;
            _backgroundButton.interactable = false;

            _acceptButtonImg.sprite = focusYes ? _focusSprite : _notFocusSprite;
            _cancelButtonImg.sprite = !focusYes ? _focusSprite : _notFocusSprite;

            if (moneyType != EnumBase.MoneyTypes.None)
            {
                //_currency.InitData(moneyType, value);
                //_currency.gameObject.SetActive(true);
            }

            //_currency.gameObject.SetActive(false);
            _messageBox.gameObject.SetActive(true);
        }

        private void OnAccept()
        {
            OnClose(_acceptAction);
        }

        private void OnCancel()
        {
            OnClose(_cancelAction);
        }

        public void OnClose(UnityAction callback = null)
        {
            UIManager.EnableTouch(false);
            _messageBoxBackImage.DOFade(0, .3f);
            _messageBoxPopup.transform.DOScale(0, .3f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
            {
                UIManager.EnableTouch(true);
                _messageBox.gameObject.SetActive(false);
                UIManager.IsShowMessageBox = false;
                callback?.Invoke();
            });
        }

        public void AnimTargetItem()
        {
            _animTargetDelayTween?.Kill();

            if (!screenTargetItem.gameObject.activeSelf)
            {
                animTargetItem.transform.position = posStartAnim;
                screenTargetItem.gameObject.SetActive(true);
                animTargetItem.GetComponent<RectTransform>().DOAnchorPos(posEndAnim, .3f).SetUpdate(true);
            }

            _animTargetDelayTween = DOVirtual.DelayedCall(2f, () => { screenTargetItem.gameObject.SetActive(false); });
        }

        #endregion
    }
}