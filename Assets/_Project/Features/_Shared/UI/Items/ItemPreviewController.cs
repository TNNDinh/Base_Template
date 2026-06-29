using System.Collections.Generic;
using Ezg.Core.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.UI;

namespace Assets.Scripts._2.BUS.Features.Item
{
    public class ItemPreviewController : MonoBehaviour
    {
        [SerializeField] [TabGroup("Cấu hình")]
        private ItemElementController _itemTemplate;

        [SerializeField] [TabGroup("Cấu hình")]
        private List<Transform> _listIgnore;

        private readonly List<ItemElementController> _itemCached = new();

        private ShowingObjectController _showingAnim;

        private void Awake()
        {
            _showingAnim = GetComponent<ShowingObjectController>();
        }

        public void InitData(List<Resource> items, bool isViewOnly = false, bool showRemaining = false,
            bool useAnim = true, bool showFullQuantity = false)
        {
            if (items == null)
            {
                ClearItems();
                return;
            }

            ClearItems();

            foreach (var item in items)
            {
                var controller = Instantiate(_itemTemplate, transform);
                controller.InitData(item, isViewOnly, showRemaining: showRemaining, showFullQuantity: showFullQuantity);
                //controller.SetOnClickAction(() =>
                //    {
                //        controller.gameObject.ShowTooltip(GameSystems.Localize(item.resType.ToString().ToLower() + "_" +
                //                                          item.resId.ToString().ToLower(),
                //                    item.resType == EnumBase.ResourceTypes.Item
                //                        ? LocalizeCategory.Equipment
                //                        : LocalizeCategory.Common));
                //    }
                //);
                controller.gameObject.SetActive(true);
                _itemCached.Add(controller);
            }

            if (useAnim) _showingAnim?.ReActive();
        }

        public void InitData(Resource[] items, bool isViewOnly = false, bool showRemaining = false, bool useAnim = true,
            bool isShowChecked = false)
        {
            ClearItems();

            if (items == null) return;

            foreach (var item in items)
            {
                var controller = Instantiate(_itemTemplate, transform);
                controller.InitData(item, isViewOnly, showRemaining: showRemaining, isShowChecked: isShowChecked);
                //controller.SetOnClickAction(() =>
                //{
                //    controller.gameObject.ShowTooltip(GameSystems.Localize(item.resType.ToString().ToLower() + "_" +
                //                                      item.resId.ToString().ToLower(),
                //                item.resType == EnumBase.ResourceTypes.Item
                //                    ? LocalizeCategory.Equipment
                //                    : LocalizeCategory.Common));
                //});
                controller.gameObject.SetActive(true);
            }

            if (useAnim) _showingAnim?.ReActive();
        }

        public void InitData(Resource item, bool isViewOnly = false, bool showRemaining = false, bool useAnim = true)
        {
            ClearItems();

            var obj = Instantiate(_itemTemplate, transform);
            obj.InitData(item, isViewOnly, showRemaining: showRemaining);
            //obj.SetOnClickAction(() =>
            //{
            //    obj.gameObject.ShowTooltip(GameSystems.Localize(item.resType.ToString().ToLower() + "_" +
            //                                      item.resId.ToString().ToLower(),
            //                item.resType == EnumBase.ResourceTypes.Item
            //                    ? LocalizeCategory.Equipment
            //                    : LocalizeCategory.Common));
            //});
            obj.gameObject.SetActive(true);

            if (useAnim) _showingAnim.ReActive();
        }

        public void ClearItems()
        {
            foreach (Transform trans in transform)
                if (_listIgnore.Count <= 0)
                    Destroy(trans.gameObject);
                else
                    foreach (var transIgnore in _listIgnore)
                    {
                        if (_listIgnore.Contains(trans)) continue;
                        Destroy(trans.gameObject);
                    }
        }

        public List<ItemElementController> GetItems()
        {
            return _itemCached;
        }
    }
}