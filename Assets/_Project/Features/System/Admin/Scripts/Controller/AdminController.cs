using System;
using System.Collections.Generic;
using System.Linq;
using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Ezg.Core.Extensions;
using Ezg.Core.Utils;
using Ezg.Feature.Shared;
using Ezg.Feature.Social.GiftCode;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.System.Admin
{
    public class AdminController : FeatureBaseController
    {
        #region Fields

        [SerializeField] [TabGroup("Cấu hình")] [Required]
        private GameObject _loginLayout;

        [SerializeField] [TabGroup("Cấu hình")] [Required]
        private GameObject _changePassLayout;

        [SerializeField] [TabGroup("Cấu hình")] [Required]
        private InputField _adminEmail;

        [SerializeField] [TabGroup("Cấu hình")] [Required]
        private InputField _password;

        [SerializeField] [TabGroup("Cấu hình")] [Required]
        private Dropdown _resTypeDropDown;

        [SerializeField] [TabGroup("Cấu hình")] [Required]
        private Dropdown _resIdDropDown;

        [TabGroup("Cấu hình")] [SerializeField] [Required]
        private Dropdown _jewelLevel;

        [SerializeField] [TabGroup("Cấu hình")] [Required]
        private InputField _resNumber;

        [SerializeField] [TabGroup("Cấu hình")] [Required]
        private AdminItemController _itemTemplate;

        [SerializeField] [TabGroup("Cấu hình")] [Required]
        private Transform _itemParent;

        [SerializeField] [TabGroup("Cấu hình change pass")] [Required]
        private InputField _changePassOldPass;

        [SerializeField] [TabGroup("Cấu hình change pass")] [Required]
        private InputField _changePassNewPass1;

        [SerializeField] [TabGroup("Cấu hình change pass")] [Required]
        private InputField _changePassNewPass2;

        [SerializeField] [TabGroup("Cấu hình mail")] [Required]
        private InputField _userName;

        [SerializeField] [TabGroup("Cấu hình mail")] [Required]
        private Dropdown _titleTemplate;

        [SerializeField] [TabGroup("Cấu hình mail")] [Required]
        private Dropdown _contentTemplate;

        [SerializeField] [TabGroup("Cấu hình mail")] [Required]
        private InputField _title;

        [SerializeField] [TabGroup("Cấu hình mail")] [Required]
        private InputField _content;

        [SerializeField] [TabGroup("Cấu hình mail")] [Required]
        private UI_TabExtensions _functionTab;

        [SerializeField] [TabGroup("Cấu hình mail")] [Required]
        private Toggle _globalEmail;

        [SerializeField] [TabGroup("Cấu hình mail")] [Required]
        private InputField _lifeTime;

        [SerializeField] [TabGroup("Cấu hình gift code")] [Required]
        private InputField _limitTimes;

        [SerializeField] [TabGroup("Cấu hình gift code")] [Required]
        private Toggle _expiredTime;

        [SerializeField] [TabGroup("Cấu hình gift code")] [Required]
        private Toggle _manualGiftCode;

        [SerializeField] [TabGroup("Cấu hình gift code")] [Required]
        private InputField _dayText;

        [SerializeField] [TabGroup("Cấu hình gift code")] [Required]
        private InputField _monthText;

        [SerializeField] [TabGroup("Cấu hình gift code")] [Required]
        private InputField _yearText;

        [SerializeField] [TabGroup("Cấu hình gift code")] [Required]
        private InputField _giftCodeManual;

        [SerializeField] [TabGroup("Cấu hình gift code")] [Required]
        private InputField _versionLimit;

        private IEnumerable<ItemMergeModel> ItemList;
        //private IEnumerable<HeroesModel> HeroList;
        //private IEnumerable<PetModel> PetList;

        private List<Resource> _rewardList = new();

        private int _tabIndex;

        #endregion

        #region Functions

        protected override void Start()
        {
            base.Start();
            _adminEmail.text = PlayerDataManager.Account.AccountId;
            ItemList = Array.Empty<ItemMergeModel>(); // gameplay removed: không còn dữ liệu item merge
            //HeroList = DataManager.Heroes.GetAll();
            //PetList = null;//DataManager.Pet.GetAll();
            _jewelLevel.options.Clear();
            for (var i = 1; i < 13; i++) _jewelLevel.options.Add(new Dropdown.OptionData { text = "Level " + i });

            _titleTemplate.options.Clear();
            _titleTemplate.options.Add(new Dropdown.OptionData("custom"));
            for (var i = 1; i <= 10; i++) _titleTemplate.options.Add(new Dropdown.OptionData("mail_title_temp_" + i));
            _titleTemplate.onValueChanged.AddListener(OnChangeTitle);

            _contentTemplate.options.Clear();
            _contentTemplate.options.Add(new Dropdown.OptionData("custom"));
            for (var i = 1; i <= 10; i++)
                _contentTemplate.options.Add(new Dropdown.OptionData("mail_content_temp_" + i));
            _contentTemplate.onValueChanged.AddListener(OnChangeContent);

            AddOptionsResType();

            if (AdminManager.AdData != null && AdminManager.AdData.IsTest)
            {
                _userName.readOnly = true;
                _userName.text = PlayerDataManager.Account.AccountId;
            }

            ResetGiftCodeDateTime();

            _functionTab.RegisterOnchangeAction(0, () => _tabIndex = 0);
            _functionTab.RegisterOnchangeAction(1, () => _tabIndex = 1);

            _versionLimit.text = Application.version;

            _globalEmail.onValueChanged.AddListener(ChangeMailType);
        }

        private void ChangeMailType(bool isGlobal)
        {
            _lifeTime.readOnly = !isGlobal;
            if (!isGlobal) _lifeTime.text = 10.ToString();
        }

        private void AddOptionsResType()
        {
            _resTypeDropDown.ClearOptions();
            _resTypeDropDown.AddOptions(EnumBase.ResourceTypes.Names.ToList());
            _resTypeDropDown.onValueChanged.AddListener(OnChangeResType);
            //_resIdDropDown.onValueChanged.AddListener(OnChangeResId);
        }

        private int GetSelectedResourceType()
        {
            return EnumBase.ResourceTypes.All[_resTypeDropDown.value];
        }

        private void ResetGiftCodeDateTime()
        {
            _dayText.text = DateTime.Now.Day.ToString();
            _monthText.text = DateTime.Now.Month.ToString();
            _yearText.text = DateTime.Now.Year.ToString();
        }

        private void OnChangeTitle(int index)
        {
            _title.text = GameSystems.Localize(_titleTemplate.captionText.text);
        }

        private void OnChangeContent(int index)
        {
            _content.text = GameSystems.Localize(_contentTemplate.captionText.text);
        }

        private void OnChangeResType(int dropdownIndex)
        {
            _resNumber.gameObject.SetActive(true);
            _resIdDropDown.ClearOptions();
            _jewelLevel.gameObject.SetActive(false);
            var resType = EnumBase.ResourceTypes.All[dropdownIndex];
            switch (resType)
            {
                case EnumBase.ResourceTypes.Money:
                    Enum.GetValues(typeof(EnumBase.MoneyTypes)).Cast<EnumBase.MoneyTypes>()
                        .Select(x => (int)x).ToList().ForEach(x =>
                            _resIdDropDown.options.Add(new Dropdown.OptionData
                                { text = (int)(EnumBase.MoneyTypes)x + "-" + (EnumBase.MoneyTypes)x }));
                    break;
                case EnumBase.ResourceTypes.Item:
                    _resIdDropDown.AddOptions(ItemList.Select(x => x.id + ": " + GameSystems.Localize("item_" + x.id))
                        .ToList());
                    //OnChangeResId(0);
                    break;
                //case (int)EnumBase.ResourceTypes.Hero:
                //    _resIdDropDown.AddOptions(HeroList.Select(x => $"{x.id}-{GameSystems.Localize(DataManager.Heroes.GetById(x.id).name)}").ToList());
                //    break;
                //case (int)EnumBase.ResourceTypes.Skill:
                //_resIdDropDown.AddOptions(WeaponList.Select(x => GameSystems.GetLocalize(x.nameCode)).ToList());
                //break;
                case EnumBase.ResourceTypes.Package:
                    _resIdDropDown.AddOptions(ShopService.GetAllPackTemplates()
                        .Select(x => $"{x.packName} [{x.ProductId}]")
                        .ToList());
                    break;
                //case (int)EnumBase.ResourceTypes.Pet:
                //    _resIdDropDown.AddOptions(PetList.Select(x => GameSystems.Localize(x.name)).ToList());
                //    break;
            }
        }

        private List<int> ListCurrencies()
        {
            return Enum.GetValues(typeof(EnumBase.MoneyTypes)).Cast<EnumBase.MoneyTypes>()
                .Select(x => (int)x).ToList();
        }

        //private void OnChangeResId(int resIndex)
        //{
        //    if (_resTypeDropDown.value != (int)EnumBase.ResourceTypes.Item)
        //    {
        //        return;
        //    }

        //    var itemType = PlayerResource.GetItemTypeById(ItemList.ElementAt(resIndex).id);
        //    _jewelLevel.gameObject.SetActive(itemType == EnumBase.ItemTypes.Jewel);
        //    _equipmentRarities.gameObject.SetActive(itemType == EnumBase.ItemTypes.Equipment);
        //    _resNumber.gameObject.SetActive(itemType != EnumBase.ItemTypes.Equipment || _resTypeDropDown.value == (int)EnumBase.ResourceTypes.Hero);
        //    _resNumber.text = "1";
        //}

        public void OnAddReward()
        {
            var selectedType = GetSelectedResourceType();
            if (selectedType == EnumBase.ResourceTypes.None)
            {
                ShowError();
                return;
            }

            if (selectedType == EnumBase.ResourceTypes.Money && _resIdDropDown.value == 0)
            {
                ShowError();
                return;
            }

            if (string.IsNullOrEmpty(_resNumber.text)) _resNumber.text = "1";

            var resNumber = Convert.ToInt32(_resNumber.text);
            if (resNumber <= 0) resNumber = 1;
            var resType = selectedType;
            var resId = resType switch
            {
                EnumBase.ResourceTypes.Money => ListCurrencies()[_resIdDropDown.value],
                EnumBase.ResourceTypes.Package => _resIdDropDown.value + 1,
                EnumBase.ResourceTypes.Item => Convert.ToInt32(ItemList.ElementAt(_resIdDropDown.value).id),
                //(int)EnumBase.ResourceTypes.Hero => HeroList.ElementAt(_resIdDropDown.value).id,
                //(int)EnumBase.ResourceTypes.Pet => PetList.ElementAt(_resIdDropDown.value).id,
                _ => 0
            };


            //if (resType == EnumBase.ResourceTypes.Money && resId == 0)
            //{
            //    ShowError();
            //    return;
            //}

            //if (resId == 0)
            //{
            //    ShowError();
            //    return;
            //}

            var reward = new Resource
            {
                resType = selectedType,
                resId = resId,
                resNumber = resNumber,
                customValue = null
            };
            _rewardList.Add(reward.GenerateReward());
            _rewardList = _rewardList.CompileRewards();
            FillItem();
        }

        private void FillItem()
        {
            if (_rewardList == null)
            {
                ClearItems();
                return;
            }

            ClearItems();

            foreach (var item in _rewardList)
            {
                var controller = Instantiate(_itemTemplate, _itemParent);
                controller.InitData(item);
                controller.SetOnClickAction(() =>
                    controller.gameObject.ShowTooltip(GameSystems.Localize(
                        "#" + EnumBase.ResourceTypes.GetName(item.resType) + "_" +
                        item.resId.ToString().ToLower())));
                controller.gameObject.SetActive(true);
                var it = item;
                controller.SetRemoveItem(() =>
                {
                    _rewardList.Remove(it);
                    FillItem();
                });
            }
        }

        public void ClearItems()
        {
            foreach (Transform trans in _itemParent) Destroy(trans.gameObject);
        }

        private void ShowError()
        {
            GameSystems.ShowSimpleMessage("Error!");
        }

        public void OnLogin()
        {
            if (string.IsNullOrEmpty(_password.text))
            {
                GameSystems.ShowSimpleMessage("Input password");
                return;
            }

            AdminManager.Login(_password.text, () => _loginLayout.SetActive(false)).Forget();
        }

        public void OnOpenChangePass()
        {
            _changePassLayout.SetActive(true);
        }

        public void OnCancelChangePass()
        {
            _changePassLayout.SetActive(false);
            _changePassNewPass1.text = "";
            _changePassNewPass2.text = "";
            _changePassOldPass.text = "";
        }

        public void OnChangePassword()
        {
            if (string.IsNullOrEmpty(_changePassNewPass1.text) || string.IsNullOrEmpty(_changePassNewPass2.text) ||
                string.IsNullOrEmpty(_changePassOldPass.text))
            {
                GameSystems.ShowSimpleMessage("Input password please");
                return;
            }

            if (!_changePassNewPass1.text.Equals(_changePassNewPass2.text))
            {
                GameSystems.ShowSimpleMessage("New password not correct");
                return;
            }

            AdminManager.ChangePassword(_changePassOldPass.text, _changePassNewPass1.text, () =>
            {
                _password.text = _changePassNewPass1.text;
                OnCancelChangePass();
            }).Forget();
        }

        public void OnLogout()
        {
            GameSystems.ShowMessage("Logout?", acceptAction: () => CloseMe());
        }

        public void CheckUser()
        {
            if (string.IsNullOrEmpty(_userName.text))
            {
                GameSystems.ShowSimpleMessage("Input user ID please");
                return;
            }

            AdminManager.CheckUserId(_userName.text, () => { GameSystems.ShowSimpleMessage("User available"); })
                .Forget();
        }

        public void OnSend()
        {
            string resType = null;
            string resId = null;
            string resNumber = null;
            string resCustom = null;

            if (_rewardList.Any())
            {
                resType = "";
                resId = "";
                resNumber = "";
                resCustom = "";
                _rewardList.ForEach(x =>
                {
                    resType += x.resType + ";";
                    resId += x.resId + ";";
                    resNumber += x.resNumber + ";";
                    //resCustom += ";";
                });
            }

            //Mail
            if (_tabIndex == 1)
            {
                if ((string.IsNullOrEmpty(_userName.text) && !_globalEmail.isOn) ||
                    string.IsNullOrEmpty(_title.text) ||
                    string.IsNullOrEmpty(_content.text))
                {
                    GameSystems.ShowSimpleMessage("Input please");
                    return;
                }

                var lifeTime = Convert.ToInt32(_lifeTime.text);

                lifeTime = lifeTime <= 0 ? 7 : lifeTime;

                AdminManager.SendMail(_globalEmail.isOn, _adminEmail.text, _password.text, _userName.text,
                    _titleTemplate.value == 0 ? _title.text : _titleTemplate.options[_titleTemplate.value].text,
                    _contentTemplate.value == 0 ? _content.text : _contentTemplate.options[_contentTemplate.value].text,
                    resType, resId, resNumber, resCustom, lifeTime, () => { }).Forget();
            }
            else //Gift code
            {
                if (_rewardList is not { Count: > 0 })
                {
                    GameSystems.ShowSimpleMessage("Please add resources");
                    return;
                }

                if (_expiredTime.isOn)
                {
                    if (!AdminManager.ValidateDate(Convert.ToInt32(_yearText.text), Convert.ToInt32(_monthText.text),
                            Convert.ToInt32(_dayText.text)))
                    {
                        GameSystems.ShowSimpleMessage("Date time wrong");
                        return;
                    }
                }
                else
                {
                    ResetGiftCodeDateTime();
                }

                if (_manualGiftCode.isOn)
                {
                    if (string.IsNullOrEmpty(_giftCodeManual.text))
                    {
                        GameSystems.ShowSimpleMessage("Input gift code please");
                        return;
                    }

                    if (_giftCodeManual.text.Length < 6)
                    {
                        GameSystems.ShowSimpleMessage("Input 6 char of gift code");
                        return;
                    }
                }

                AdminManager.Login(_password.text, () =>
                {
                    var limitTimes = string.IsNullOrEmpty(_limitTimes.text) ? 0 : Convert.ToInt32(_limitTimes.text);
                    GiftCodeManager.CreateGiftCode(_rewardList, Math.Max(limitTimes, 0), resType, resId, resNumber,
                        resCustom, _expiredTime.isOn, new DateTimeOffset(
                            new DateTime(Convert.ToInt32(_yearText.text), Convert.ToInt32(_monthText.text),
                                Convert.ToInt32(_dayText.text)), TimeSpan.Zero).ToUnixTimeSeconds(),
                        _manualGiftCode.isOn ? _giftCodeManual.text : null, _versionLimit.text);
                }).Forget();
            }
        }

        #endregion
    }
}