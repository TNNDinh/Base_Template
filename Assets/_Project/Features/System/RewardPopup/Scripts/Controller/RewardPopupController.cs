using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts._2.BUS.Features.Item;
using BlackFace.Libraries.Modules.UIModule;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.System.RewardPopup
{
    [Serializable]
    public class RewardPopupProperty
    {
        public PurchaseType purchaseType;
        public object data;
        public Action onClose;

        public RewardPopupProperty(object data, Action onClose = null, PurchaseType purchaseType = PurchaseType.None)
        {
            this.onClose = onClose;
            this.data = data;
            this.purchaseType = purchaseType;
        }
    }

    public class RewardPopupController : FeatureBaseController
    {
        #region Fields

        [SerializeField] [TabGroup("Cấu hình tính năng")]
        private ItemPreviewController _itemPreview;

        [SerializeField] [TabGroup("Cấu hình tính năng")]
        private Text _title;

        private bool _initData;
        private List<Transform> _itemCached;

        private RewardPopupProperty _property;

        #endregion

        #region Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            _itemCached = new List<Transform>();
            //AudioService.Default.PlaySound(DataManager.SoundConfig.RewardClaim);
        }

        public override void LoadData(object data)
        {
            base.LoadData(data);
            _property = (RewardPopupProperty)data;
            if (_property.data is IList)
                InitData((List<Resource>)_property.data);
            else
                InitData((Resource)_property.data);
            if (_property.purchaseType == PurchaseType.IAP)
                _title.text = GameSystems.Localize("purchase_succesful");
            else
                _title.text = GameSystems.Localize("collect_rewards");
        }

        public void InitData(List<Resource> resources)
        {
            if (_initData)
                HideAllItem();

            _itemPreview.InitData(resources.ToArray());
            _initData = true;
        }

        public void InitData(Resource resources)
        {
            if (_initData)
                HideAllItem();

            _itemPreview.InitData(new[] { resources });

            _initData = true;
        }

        private void HideAllItem()
        {
            foreach (var item in _itemCached) item.gameObject.SetActive(false);

            _itemCached.Clear();
        }

        public override void CloseMe(Action completeAction = null)
        {
            base.CloseMe(_property.onClose);
        }

        #endregion
    }
}