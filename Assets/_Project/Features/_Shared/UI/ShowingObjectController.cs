using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.Shared.UI
{
    /// <summary>
    ///     Show danh sách các object với animation DoTween
    /// </summary>
    public class ShowingObjectController : MonoBehaviour
    {
        #region Fields

        [SerializeField] [TabGroup("Cấu hình chung")] [Title("Khởi tạo các object con tự động")]
        private bool InitChildObjectLater;

        [SerializeField] [ShowIf("InitChildObjectLater")] [TabGroup("Cấu hình chung")] [Title("Khởi tạo khi Enable")]
        private bool InitChildObjectWithOnEnable;

        [ShowIf("InitChildObjectLater")]
        [SerializeField]
        [TabGroup("Cấu hình chung")]
        [Title("Thời gian delay khởi tạo các object con")]
        [MinValue(0.03)]
        private float DelayTimeInitChildObject;

        [ShowIf("InitChildObjectLater")] [SerializeField] [TabGroup("Cấu hình chung")] [Title("Có show fx không")]
        private bool isShowFx;

        [ShowIf("InitChildObjectLater")]
        [SerializeField]
        [TabGroup("Cấu hình chung")]
        [Title("Thời gian delay show fx")]
        private float delayShowFx;

        [HideIf("AnimType", AnimTypes.Fade)]
        [SerializeField]
        [TabGroup("Cấu hình chung")]
        [Title("Danh sách gameobject ẩn/hiện")]
        [Required]
        private List<GameObject> _gameObjectList;

        [ShowIf("AnimType", AnimTypes.Fade)]
        [SerializeField]
        [TabGroup("Cấu hình chung")]
        [Title("Danh sách image")]
        [Required]
        private List<Image> _imageList;

        [SerializeField] [TabGroup("Cấu hình chung")] [Title("Kiểu hiển thị")]
        private Ease _tweenType = Ease.OutBack;

        [SerializeField] [TabGroup("Cấu hình chung")] [Title("Delay xuất hiện")]
        private float _delayTimeStart;

        [SerializeField] [TabGroup("Cấu hình chung")] [Title("Delay time giữa 2 object")]
        private float _delayTimeBetween = 0.05f;

        [SerializeField] [TabGroup("Cấu hình chung")] [Title("Thời gian thực thi anim")]
        private float _duration = 0.3f;

        [ShowIf("AnimType", AnimTypes.Fade)] [SerializeField] [TabGroup("Cấu hình chung")] [Title("Fade min")]
        private float _fadeMin;

        [ShowIf("AnimType", AnimTypes.Fade)] [SerializeField] [TabGroup("Cấu hình chung")] [Title("Fade max")]
        private float _fadeMax = 1f;

        [ShowIf("@AnimType == AnimTypes.MoveToVertical || AnimType == AnimTypes.MoveToHorizontal")]
        [SerializeField]
        [TabGroup("Cấu hình chung")]
        [Title("Làm mới child object trước khi thực thi")]
        private bool _isRefreshChildBeforeActive;

        [SerializeField] [TabGroup("Cấu hình chung")] [Title("Thời gian thực thi anim")]
        private bool _isRealTime;

        [SerializeField] [TabGroup("Cấu hình hiển thị")] [Title("Xuất hiện khi")]
        private ApperTypes ApperType = ApperTypes.Awake;

        [SerializeField] [TabGroup("Cấu hình hiển thị")] [Title("Kiểu xuất hiện")]
        private AnimTypes AnimType = AnimTypes.Scale;

        [ShowIf("AnimType", AnimTypes.Scale)] [SerializeField] [TabGroup("Cấu hình hiển thị")] [Title("Scale ban đầu")]
        private Vector3 _fromScale;

        [ShowIf("AnimType", AnimTypes.Scale)] [SerializeField] [TabGroup("Cấu hình hiển thị")] [Title("Scale target")]
        private Vector3 _toScale = Vector3.one;

        [ShowIf("AnimType", AnimTypes.MovePos)]
        [SerializeField]
        [TabGroup("Cấu hình hiển thị")]
        [Title("Di chuyển tới")]
        private List<Vector3> _toPos;

        [ShowIf("@AnimType == AnimTypes.MoveToVertical || AnimType == AnimTypes.MoveToHorizontal")]
        [SerializeField]
        [TabGroup("Cấu hình hiển thị")]
        [Title("Di chuyển tới")]
        private List<float> _movePixel;

        private List<Vector3> _fromPosCached;
        private bool _isCacheObjects;
        private List<Transform> _gameObjectListCached;

        private readonly CancellationTokenSource _cancelToken = new();

        #endregion

        #region Functions

        private void Awake()
        {
            if (ApperType == ApperTypes.Awake)
            {
                SetupDefaultValue();
                UniTask.Create(() => StartAction());
            }

            if (ApperType == ApperTypes.Calling) SetupDefaultValue();

            if (AnimType is AnimTypes.MovePos or AnimTypes.MoveToHorizontal or AnimTypes.MoveToVertical)
            {
                _fromPosCached = new List<Vector3>(_gameObjectList.Count);
                foreach (var pos in _gameObjectList) _fromPosCached.Add(pos.transform.localPosition);
            }

            if (InitChildObjectLater && !InitChildObjectWithOnEnable)
            {
                if (DelayTimeInitChildObject > 0)
                    this.DelayRealTimeMethod(DelayTimeInitChildObject, InitCustomListObject);
                else
                    InitCustomListObject();
            }
        }

        public void OnEnable()
        {
            if (InitChildObjectLater && InitChildObjectWithOnEnable)
            {
                if (DelayTimeInitChildObject > 0)
                    this.DelayRealTimeMethod(DelayTimeInitChildObject, InitCustomListObject);
                else
                    InitCustomListObject();
            }

            if (ApperType == ApperTypes.OnEnable)
            {
                SetupDefaultValue();
                UniTask.Create(() => StartAction());
            }
        }

        //private void OnDisable()
        //{
        //    if (ApperType == ApperTypes.OnEnable)
        //    {
        //        SetupDefaultValue();
        //        StartAction(true).Forget();
        //    }
        //}

        private void OnDestroy()
        {
            _cancelToken.Cancel();
        }

        /// <summary>
        ///     Gọi anim từ nơi khác
        /// </summary>
        public void Active(bool isReveser = false)
        {
            StartAction(isReveser).Forget();
        }

        public void ReActive(bool isReveser = false)
        {
            _isCacheObjects = false;
            InitCustomListObject();
            SetupDefaultValue();
            StartAction(isReveser).Forget();
        }

        /// <summary>
        ///     Cache danh sách các item
        ///     Sử dụng trong các tính năng custom, xuất hiện item spawn ra chứ ko set sẵn
        /// </summary>
        public void InitCustomListObject()
        {
            if (_isCacheObjects)
                return;

            var customList = (from Transform child in transform select child.gameObject).ToList();

            _gameObjectList = customList.Where(x => !_gameObjectList.Contains(x.gameObject) && x.gameObject != null)
                .ToList();

            _isCacheObjects = true;

            if (ApperType == ApperTypes.OnEnable)
                OnEnable();

            if (ApperType == ApperTypes.Awake)
                Awake();
        }

        /// <summary>
        ///     Làm mới lại các object con
        /// </summary>
        private void RefreshChildObject()
        {
            _fromPosCached = new List<Vector3>(_gameObjectList.Count);
            foreach (var pos in _gameObjectList) _fromPosCached.Add(pos.transform.localPosition);
        }

        /// <summary>
        ///     Khởi tạo giá trị mặc định cho các object
        /// </summary>
        private void SetupDefaultValue()
        {
            if (AnimType == AnimTypes.Scale)
                foreach (var obj in _gameObjectList)
                    obj.transform.localScale = _fromScale;
        }

        /// <summary>
        ///     Thực thi move
        /// </summary>
        /// <returns></returns>
        private async UniTask StartAction(bool isRevese = false)
        {
            void ExecuteFx(GameObject obj)
            {
                if (isShowFx)
                {
                    var component = obj.GetComponent<IExecute>();
                    component?.Execute();
                }
            }

            var index = 0;
            await UniTask.Delay(_delayTimeStart.ToMiliseconds(), _isRealTime)
                .AttachExternalCancellation(_cancelToken.Token);

            if (_isRefreshChildBeforeActive)
                RefreshChildObject();

            switch (AnimType)
            {
                case AnimTypes.Scale:
                    foreach (var obj in _gameObjectList)
                    {
                        if (obj == null) continue;

                        if (isShowFx) this.DelayMethod(delayShowFx, () => ExecuteFx(obj));

                        obj.SetActive(true);
                        obj.transform.DOScale(!isRevese ? _toScale : _fromScale, _duration).SetEase(_tweenType)
                            .SetUpdate(_isRealTime);
                        await UniTask.Delay(_delayTimeBetween.ToMiliseconds(), _isRealTime)
                            .AttachExternalCancellation(_cancelToken.Token).ContinueWith(() =>
                            {
                                if (isRevese)
                                    obj.SetActive(false);
                            });
                    }

                    break;
                case AnimTypes.MovePos:
                    index = 0;
                    foreach (var obj in _gameObjectList)
                    {
                        if (isShowFx) this.DelayMethod(delayShowFx, () => ExecuteFx(obj));

                        obj.transform.localPosition = isRevese ? _toPos[index] : _fromPosCached[index];
                        obj.transform.DOLocalMove(isRevese ? _fromPosCached[index] : _toPos[index], _duration)
                            .SetEase(_tweenType).SetUpdate(_isRealTime);
                        index++;
                        await UniTask.Delay(_delayTimeBetween.ToMiliseconds(), _isRealTime)
                            .AttachExternalCancellation(_cancelToken.Token);
                    }

                    break;
                case AnimTypes.MoveToVertical:
                    index = 0;
                    foreach (var obj in _gameObjectList)
                    {
                        if (isRevese)
                            obj.transform.DOLocalMoveX(obj.transform.localPosition.y + _movePixel[index], 0)
                                .SetUpdate(true);
                        else
                            obj.transform.localPosition = _fromPosCached[index];

                        obj.transform
                            .DOLocalMoveY(
                                obj.transform.localPosition.y + (isRevese ? 0 - _movePixel[index] : _movePixel[index]),
                                _duration)
                            .SetEase(_tweenType).SetUpdate(_isRealTime);
                        index++;
                        await UniTask.Delay(_delayTimeBetween.ToMiliseconds(), _isRealTime)
                            .AttachExternalCancellation(_cancelToken.Token);
                    }

                    break;
                case AnimTypes.MoveToHorizontal:
                    index = 0;
                    foreach (var obj in _gameObjectList)
                    {
                        if (isRevese)
                            obj.transform.DOLocalMoveX(obj.transform.localPosition.x + _movePixel[index], 0)
                                .SetUpdate(true);
                        else
                            obj.transform.localPosition = _fromPosCached[index];

                        obj.transform
                            .DOLocalMoveX(
                                obj.transform.localPosition.x + (isRevese ? 0 - _movePixel[index] : _movePixel[index]),
                                _duration)
                            .SetEase(_tweenType).SetUpdate(_isRealTime);
                        index++;
                        await UniTask.Delay(_delayTimeBetween.ToMiliseconds(), _isRealTime)
                            .AttachExternalCancellation(_cancelToken.Token);
                    }

                    break;
                case AnimTypes.Fade:
                    foreach (var img in _imageList)
                    {
                        if (isShowFx) this.DelayMethod(delayShowFx, () => ExecuteFx(img.gameObject));

                        img.DOFade(isRevese ? _fadeMax : _fadeMin, 0).SetUpdate(true);
                        img.DOFade(isRevese ? _fadeMin : _fadeMax, _duration).SetEase(_tweenType)
                            .SetUpdate(_isRealTime);
                        await UniTask.Delay(_delayTimeBetween.ToMiliseconds(), _isRealTime)
                            .AttachExternalCancellation(_cancelToken.Token);
                    }

                    break;
            }
        }

        public void SetMovePixel(List<float> movePixel)
        {
            _movePixel = movePixel;
        }

        public void SetDuration(float duration)
        {
            _duration = duration;
        }

        public enum AnimTypes
        {
            Scale,
            MovePos,
            Fade,
            MoveToHorizontal,
            MoveToVertical
        }

        public enum ApperTypes
        {
            Awake,
            OnEnable,
            Calling
        }

        #endregion
    }
}