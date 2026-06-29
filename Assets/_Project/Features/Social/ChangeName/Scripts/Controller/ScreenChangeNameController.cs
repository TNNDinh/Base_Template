using System.Text.RegularExpressions;
using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Ezg.Core.Utils;
using Ezg.Feature.Shared;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.Config;

public class ScreenChangeNameController : FeatureBaseController
{
    [SerializeField] [TabGroup("Custom")] private InputField inputFieldName;

    [SerializeField] [TabGroup("Custom")] private Button buttonChangeName;

    [SerializeField] [TabGroup("Custom")] private GameObject textFree;

    [SerializeField] [TabGroup("Custom")] private MoneyBarRequireView requireView;

    [SerializeField] [TabGroup("Custom")] private Text namePlayer;

    [SerializeField] [TabGroup("Custom")] private int maxLength = 20;

    [SerializeField] [TabGroup("Custom")] private Text counterText;


    private bool IsFreeChangeName => PlayerDataManager.Settings.NumberChangeName <
                                     DataManager.ChangeName.dataGroups.freeNumber;
    //private bool IsFreeChangeName => true;

    protected override void Start()

    {
        base.Start();
        buttonChangeName.onClick.AddListener(ChangeName);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        EventManager.StartListening(EventName.UpdateResource, UpdateView);
        EventManager.StartListening(EventName.UpdatePlayerName, UpdateName);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EventManager.StartListening(EventName.UpdateResource, UpdateView);
        EventManager.StartListening(EventName.UpdatePlayerName, UpdateName);
    }

    protected override void LoadData()
    {
        base.LoadData();
        LoadPlayerName();
        UpdateView();

        inputFieldName.characterLimit = maxLength;
        UpdateCounter();

        inputFieldName.onValueChanged.AddListener(delegate { UpdateCounter(); });

        UpdateName();
    }

    private void UpdateCounter()
    {
        var remaining = maxLength - inputFieldName.text.Length;
        counterText.text = remaining.ToString();
    }

    private void UpdateView()
    {
        textFree.SetActive(IsFreeChangeName);
        requireView.SetData(DataManager.ChangeName.dataGroups.cost);
        requireView.gameObject.SetActive(!IsFreeChangeName);
    }

    private void LoadPlayerName()
    {
        var textName = PlayerDataManager.Settings.dataBase.PlayerName;
        if (string.IsNullOrEmpty(textName))
        {
            textName = "Player" + Random.Range(1000, 99999);
            PlayerDataManager.Settings.SetPlayerName(textName);
        }

        inputFieldName.text = textName;
    }

    private void UpdateName()
    {
        namePlayer.text = string.Format(GameSystems.Localize("current_name"),
            PlayerDataManager.Settings.dataBase.PlayerName);
    }

    private void ChangeName()
    {
        var newName = inputFieldName.text.Trim();

        if (string.IsNullOrEmpty(newName))
        {
            GameSystems.ShowSimpleMessage("name_cannot_empty");
            return;
        }

        if (IsFreeChangeName)
        {
            if (newName.Equals(PlayerDataManager.Settings.dataBase.PlayerName))
            {
                GameSystems.ShowSimpleMessage("same_current_name");
                return;
            }

            if (!IsNameValid(newName)) return;

            PlayerDataManager.Settings.SetPlayerName(newName);
            PlayerDataManager.Settings.NumberChangeName++;
            CloseMe();
            EventManager.EmitEvent(nameof(EventName.UpdatePlayerName));
            GameSystems.ShowSimpleMessage("change_name_success");
        }
        else
        {
            if (PlayerResource.IsEnough(DataManager.ChangeName.dataGroups.cost))
            {
                if (newName.Equals(PlayerDataManager.Settings.dataBase.PlayerName))
                {
                    GameSystems.ShowSimpleMessage("same_current_name");
                    return;
                }

                if (!IsNameValid(newName)) return;

                PlayerDataManager.Settings.SetPlayerName(newName);
                var cost = DataManager.ChangeName.dataGroups.cost;
                PlayerResource.RemoveCurrency((EnumBase.MoneyTypes)cost.resId, cost.resNumber, () =>
                    {
                        PlayerDataManager.Settings.NumberChangeName++;
                        UpdateView();
                        CloseMe();
                        //EventManager.EmitEvent(nameof(EventName.UpdatePlayerName));
                        GameSystems.ShowSimpleMessage("change_name_success");
                    },
                    unSuccess: () =>
                    {
                        UIManager.Instance.Show(GameEnums.Features.Shop, UIManager.UIGroupName.Modal_Container)
                            .Forget();
                    });
            }
            else
            {
                UIManager.Instance.Show(GameEnums.Features.Shop, UIManager.UIGroupName.Modal_Container).Forget();
            }
        }
    }

    private bool IsNameValid(string newName)
    {
        if (IsHasSpecialChars(newName)) return false;
        if (IsLengthNotValid(newName)) return false;
        return true;
    }

    private bool IsLengthNotValid(string newName)
    {
        if (newName.Length < 3 || newName.Length > 20)
        {
            GameSystems.ShowSimpleMessage("should_be");
            return true;
        }

        return false;
    }

    public bool IsHasSpecialChars(string input)
    {
        if (Regex.IsMatch(input, @"[^a-zA-Z0-9]"))
        {
            GameSystems.ShowSimpleMessage("should_be");
            return true;
        }

        return false;
    }
}