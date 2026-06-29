using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Ezg.Core.Extensions;
using Ezg.Package.Audio;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.Config;

namespace Ezg.Feature.Monetization.Shop
{
    public class ScreenShopController : FeatureBaseController
    {
        [SerializeField] [TabGroup("Cấu hình tính năng")]
        protected List<GameObject> listPackage;

        [SerializeField] [TabGroup("Cấu hình tính năng")]
        protected List<GameObject> listPackageNotInit;

        [SerializeField] [TabGroup("Cấu hình tính năng")]
        protected List<GameObject> listPackageDefaultShow;

        [SerializeField] [TabGroup("Cấu hình tính năng")]
        protected Transform container;

        [SerializeField] [TabGroup("Cấu hình tính năng")]
        protected ScrollRect _scroll;

        private bool _isInit;

        private List<RectTransform> _shopRawContent;
        private bool isHaveScrollHorizontal;

        private bool isHaveScrollVertical;

        private bool IsUnlock => UnlockFeatureService.IsUnlocked(GameEnums.Features.Shop);

        protected override void Start()
        {
            base.Start();
            isHaveScrollVertical = _scroll.vertical;
            isHaveScrollHorizontal = _scroll.horizontal;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            AudioService.Default.PlaySound(DataManager.SoundConfig.OpenShop);
            EventManager.StartListening(EventName.HandleScrollShop, HandleScroll);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventManager.StopListening(EventName.HandleScrollShop, HandleScroll);
        }

        protected override async void LoadData()
        {
            base.LoadData();
            await Init();
        }

        public override async void LoadData(object data)
        {
            base.LoadData(data);
            // await InitPackage();
            // InitData();
            if (data is ShopProperties properties)
            {
                if (properties.groupName == UIManager.UIGroupName.Overlay_Container)
                    EventManager.EmitEvent(EventName.ChangeUIOverlays);

                await Init();

                if (IsUnlock) SnapTo(properties.indexScroll);
            }
        }

        private async Task Init()
        {
            if (_isInit) return;
            _isInit = true;
            await InitPackage();
            InitData();
        }

        private void HandleSnapTo(int index)
        {
            SnapTo(index);
        }

        private async Task InitPackage()
        {
            foreach (var packageNotInit in listPackageNotInit) packageNotInit.SetActive(IsUnlock);

            foreach (var package in listPackage)
            {
                if (!IsUnlock &&
                    !listPackageDefaultShow.Contains(package)) continue;
                Instantiate(package, container);
                await UniTask.DelayFrame(5);
            }
        }

        public void SnapTo(int index)
        {
            this.DelayMethod(0.01f, () => _scroll.SnapTo(_scroll.content, _shopRawContent, index));
        }

        private void InitData()
        {
            _shopRawContent = _scroll.content.Cast<Transform>().Select(t => t.GetComponent<RectTransform>())
                .Where(t => t != null)
                .ToList();
        }

        private void HandleScroll()
        {
            var canScroll = EventManager.GetBool(EventName.HandleScrollShop);
            _scroll.vertical = canScroll && isHaveScrollVertical;
            _scroll.horizontal = canScroll && isHaveScrollHorizontal;
        }

        public override void CloseMe(Action completeAction = null)
        {
            base.CloseMe(completeAction);
            EventManager.EmitEvent(EventName.ChangeUIDefault);
        }
    }
}