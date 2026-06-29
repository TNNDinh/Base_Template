using Ezg.Feature.Shared;
using Ezg.Package.Localize;
using Ezg.Package.Localize.Localization;
using TigerForge;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;

public class ButtonLanguage : MonoBehaviour
{
    [SerializeField] private Button buttonLanguage;
    [SerializeField] private Text textLabel;
    [SerializeField] private Image iconFlag;
    [SerializeField] private GameObject iconTick;
    [SerializeField] private GameObject buttonNormal;
    [SerializeField] private GameObject buttonSelected;
    [SerializeField] private SystemLanguage systemLanguage;

    private string _nameLanguage;

    private void Start()
    {
        buttonLanguage.onClick.AddListener(Click);
    }

    private void OnEnable()
    {
        EventManager.StartListening(EventName.SettingLanguageChanged, UpdateView);

        UpdateView();
    }

    private void OnDisable()
    {
        EventManager.StopListening(EventName.SettingLanguageChanged, UpdateView);
    }

    public void Init(string nameLanguage, SystemLanguage language)
    {
        systemLanguage = language;

        _nameLanguage = nameLanguage;
        UpdateView();
    }

    private void UpdateView()
    {
        textLabel.text = GameSystems.Localize(systemLanguage.ToString().ToLower(), LocalizeCategory.Settings);

        // var languageCodeThreeLetter =
        //     SelectLanguageService.FromIsoName(systemLanguage.ToString()).ThreeLetterISOLanguageName;
        // iconFlag.sprite = PlayerResource.GetIconFlag(languageCodeThreeLetter);
        var saveLanguage = PlayerDataManager.Settings.GetLanguage();
        iconTick.SetActive(systemLanguage == saveLanguage);
        buttonNormal.SetActive(systemLanguage != saveLanguage);
        buttonSelected.SetActive(systemLanguage == saveLanguage);
    }

    private void Click()
    {
        Localization.Current.localCultureInfo = Locale.GetCultureInfoByLanguage(systemLanguage);
        var saveLanguage = PlayerDataManager.Settings.GetLanguage();
        if (systemLanguage == saveLanguage) return;
        PlayerDataManager.Settings.SetLanguage(systemLanguage);
        PlayerDataManager.Settings.Save();

        EventManager.EmitEvent(EventName.SettingLanguageChanged);
    }
}