using System.Linq;
using Ezg.Core.Extensions;
using Ezg.Core.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;

namespace Assets.Scripts._2.BUS.Features.Item
{
    public class ItemElementController : MonoBehaviour
    {
        protected virtual void Awake()
        {
        }

        public virtual void InitData(Resource item, bool isViewOnly = false, bool viewQuantity = true,
            bool showRemaining = false, float scale = 1, UnityAction onClickAction = null, bool showLevel = true,
            bool isShowChecked = false, bool showFullQuantity = false, bool showNotif = false)
        {
            if (item == null) return;

            _onClickAction = onClickAction;
            _notifIcon.SetActive(showNotif);
            _thisItem = item;
            _skillBorder.SetActive(false);
            _itemBorder.SetActive(!_skillBorder.activeSelf);

            //_levelBackground.gameObject.SetActive((_thisItem is ItemJewelResource or ItemEquipmentResource) && showLevel);
            //if (_levelBackground.gameObject.activeSelf)
            //{
            //    _levelBackground.color =
            //        DataManager.GeneralAssets.ColorRarites[((ItemResource)_thisItem).Rarity];
            //    _levelText.text = _thisItem is ItemJewelResource ? ((ItemJewelResource)_thisItem).Level.ToString() : PlayerDataManager.PlayerResource.dataBase.EquipmentLevel[((ItemEquipmentResource)_thisItem).equipmentType].ToString();
            //}
            _levelText.text = "";
            if (viewQuantity && _quantityText != null)
            {
                _quantityText.gameObject.SetActive(_thisItem.resType == EnumBase.ResourceTypes.Money ||
                                                   _thisItem.resType == EnumBase.ResourceTypes.Item);

                if (_quantityText.gameObject.activeSelf)
                {
                    if (showRemaining)
                    {
                        if (_thisItem.resType == EnumBase.ResourceTypes.Money)
                            _quantityText.text =
                                PlayerResource.GetCurrencyValue((EnumBase.MoneyTypes)_thisItem.resId).MoneyConvert() +
                                "/" + _thisItem.resNumber;
                        else if (_thisItem.resType == EnumBase.ResourceTypes.Item)
                            _quantityText.text = PlayerResource.GetResNumber(_thisItem.resId).MoneyConvert() + "/" +
                                                 _thisItem.resNumber;
                        //else if (_thisItem.resType is EnumBase.ResourceTypes.Skill or EnumBase.ResourceTypes.Book)
                        //{
                        //    if (UIManager.Instance.GetCurrentScene() == GameEnums.Scenes.BattleScene)
                        //    {
                        //        if (_thisItem.resType is EnumBase.ResourceTypes.Skill)
                        //        {
                        //            if (BattleManager.PlayerController.GetSkills().SkillsController
                        //                .TryGetValue(item.resId, out var resultData))
                        //            {
                        //                _quantityText.text = resultData.Level.ToString();
                        //            }
                        //            else
                        //            {
                        //                _quantityText.text = "";
                        //            }
                        //        }
                        //        else
                        //        {
                        //            if (BattleManager.PlayerController.GetSkills().PassivesController
                        //                .TryGetValue(item.resId, out var resultData))
                        //            {
                        //                _quantityText.text = resultData.Level.ToString();
                        //            }
                        //            else
                        //            {
                        //                _quantityText.text = "";
                        //            }
                        //        }
                        //    }
                        //    else if (UIManager.Instance.GetLastFeature() == GameEnums.Features.BattleResume)
                        //    {
                        //        if (_thisItem.resType is EnumBase.ResourceTypes.Skill)
                        //        {
                        //            var skill = PlayerDataManager.BattleData.GetSkills()
                        //                .FirstOrDefault(x => x.Item1 == item.resId);
                        //            if (skill != null)
                        //            {
                        //                _quantityText.text = skill.Item2.ToString();
                        //            }
                        //            else
                        //            {
                        //                _quantityText.text = "";
                        //            }
                        //        }
                        //        else
                        //        {
                        //            if (PlayerDataManager.BattleData.GetPassives().TryGetValue(item.resId, out var result))
                        //            {
                        //                _quantityText.text = result.ToString();
                        //            }
                        //            else
                        //            {
                        //                _quantityText.text = "";
                        //            }
                        //        }
                        //    }
                        //    else
                        //    {
                        //        _quantityText.text = "";
                        //    }
                        //}
                    }
                    else
                    {
                        //if (_thisItem.resType == EnumBase.ResourceTypes.Item && ((ItemResource)_thisItem).ItemType == EnumBase.ItemTypes.Jewel)
                        //{
                        //    var numberToShow = _thisItem.resNumber - JewelManager.GetJewelNumberEquipment((ItemJewelResource)_thisItem);
                        //    _quantityText.text = numberToShow > 1
                        //        ? numberToShow.MoneyConvert()
                        //        : showFullQuantity ? numberToShow.MoneyConvert() : "";
                        //}
                        //else
                        {
                            _quantityText.text = _thisItem.resNumber > 0
                                ? _thisItem.resNumber.MoneyConvert()
                                // (_thisItem.resNumber.MoneyConvert() +
                                //  (_thisItem.resId is (int)EnumBase.MoneyTypes.InfinityEnergy
                                //      /*or (int)EnumBase.MoneyTypes.CoinX2*/
                                //      ? " mins"
                                //      : ""))
                                : showFullQuantity
                                    ? _thisItem.resNumber.MoneyConvert() +
                                      (_thisItem.resId is (int)EnumBase.MoneyTypes.InfinityEnergy
                                          /*or (int)EnumBase.MoneyTypes.CoinX2*/
                                          ? " mins"
                                          : "")
                                    : "";
                        }
                    }
                }
            }
            else
            {
                if (_quantityText != null) _quantityText.gameObject.SetActive(false);
            }

            _itemIcon.sprite = _thisItem.resType switch
            {
                EnumBase.ResourceTypes.Money => PlayerResource.GetCurrencyImage(_thisItem.resId),
                EnumBase.ResourceTypes.Package => PlayerResource.GetPackageImage(_thisItem.resId),
                // removed: EnumBase.ResourceTypes.Item => GameplayService.GetItemImage (gameplay removed)
                _ => _itemIcon.sprite
            };

            if (_exclamationIcon != null)
            {
                // removed: DataManager.ItemTool.dataGroup check (gameplay removed)
                _exclamationIcon.gameObject.SetActive(false);
            }

            _itemIcon.gameObject.SetActive(true);

            _itemButton.onClick.RemoveAllListeners();
            _itemButton.interactable = !isViewOnly;
            if (!isViewOnly) _itemButton?.onClick.AddListener(OnClickItem);

            _itemButton.GetComponent<Image>().raycastTarget = !isViewOnly;
            if (scale != 1) transform.localScale = Vector3.one * scale;

            SetChecked(isShowChecked);

            SetRarity();
        }

        protected virtual void SetRarity()
        {
            //if (_thisItem.resType is EnumBase.ResourceTypes.Item or EnumBase.ResourceTypes.Pet)
            //{
            //    if (_thisItem.resType is EnumBase.ResourceTypes.Item)
            //    {
            //        var rarity = ((ItemResource)_thisItem).Rarity;
            //        _itemBackground.sprite = ((ItemResource)_thisItem).ItemType == EnumBase.ItemTypes.Jewel ? PlayerResource.GetCurrencyBackground(((ItemResource)_thisItem).Rarity) : PlayerResource.GetItemBackground(rarity);
            //        _itemBackground.gameObject.SetActive(rarity != EnumBase.ItemRarities.None);
            //    }

            //    if (_thisItem.resType is EnumBase.ResourceTypes.Pet)
            //    {
            //        _itemBackground.sprite = PlayerResource.GetCurrencyBackground(DataManager.Pet.GetByPetId(_thisItem.resId).rarity);
            //    }

            //}
            //else
            //{
            //    _itemBackground.gameObject.SetActive(false);
            //}

            //if (_thisItem.resType == EnumBase.ResourceTypes.Money)
            //{
            //    _itemBackground.gameObject.SetActive(true);
            //    _itemBackground.sprite = PlayerResource.GetCurrencyBackground(DataManager.RarityBackground.GetRarity(_thisItem.resId, _thisItem.resNumber));
            //}
        }

        public virtual void InitNull()
        {
            _itemBackground.gameObject.SetActive(false);
            _levelBackground.gameObject.SetActive(false);
            _notifIcon.SetActive(false);
            _thisItem = null;
            if (_quantityText != null) _quantityText.gameObject.SetActive(false);
            _itemIcon.gameObject.SetActive(false);
            _onClickAction = null;
            //_itemButton.GetComponent<Image>().raycastTarget = false;
        }

        public void SetNotification(bool isShow)
        {
            _notifIcon.SetActive(isShow);
        }

        public void SetChecked(bool isShow)
        {
            if (_checkedObject != null)
                _checkedObject.SetActive(isShow);
        }

        public virtual void SetOnClickAction(UnityAction action)
        {
            _onClickAction = action;

            _itemButton.onClick.RemoveAllListeners();
            _itemButton?.onClick.AddListener(OnClickItem);
        }

        public virtual void OnClickItem()
        {
            if (_onClickAction != null) _onClickAction.Invoke();

            //await UIManager.Instance.Show(GameEnums.Features.ItemDetail, isAync: true, data: ThisItem);
        }

        public void UpdateChangeNumber(long number)
        {
            _thisItem.resNumber = number;
            if (_quantityText != null) _quantityText.text = number.MoneyConvert();
        }

        public Resource GetItem()
        {
            return _thisItem;
        }

        #region Fields

        [SerializeField] [TabGroup("Cấu hình tính năng")]
        private Image _itemBackground;

        [SerializeField] [TabGroup("Cấu hình tính năng")]
        private Image _itemIcon;

        [SerializeField] [TabGroup("Cấu hình tính năng")]
        private Image _exclamationIcon;

        [SerializeField] [TabGroup("Cấu hình tính năng")]
        private Text _quantityText;

        [SerializeField] [TabGroup("Cấu hình tính năng")]
        protected Button _itemButton;

        [SerializeField] [TabGroup("Cấu hình tính năng")]
        private GameObject _notifIcon;

        [SerializeField] [TabGroup("Cấu hình tính năng")]
        protected GameObject _skillBorder;

        [SerializeField] [TabGroup("Cấu hình tính năng")]
        private GameObject _itemBorder;

        [SerializeField] [TabGroup("Cấu hình tính năng")]
        private GameObject _checkedObject;

        [SerializeField] [TabGroup("Cấu hình tính năng")]
        private Image _levelBackground;

        [SerializeField] [TabGroup("Cấu hình tính năng")]
        private Text _levelText;

        protected Resource _thisItem;

        private UnityAction _onClickAction;

        #endregion
    }
}