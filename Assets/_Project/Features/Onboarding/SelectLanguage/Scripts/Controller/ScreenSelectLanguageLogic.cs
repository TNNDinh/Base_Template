using UnityEngine;

public class ScreenSelectLanguageLogic : BaseScreenLogic
{
    [SerializeField] private ButtonLanguage prefabButtonLanguage;
    [SerializeField] private Transform buttonParent;

    private void Start()
    {
        CreateButtonLanguage();
    }

    private void CreateButtonLanguage()
    {
        foreach (var language in SelectLanguageService.systemLanguages)
        {
            var button = Instantiate(prefabButtonLanguage, buttonParent);
            button.name = language.systemLanguage.ToString();
            button.gameObject.SetActive(true);
            button.Init(language.name, language.systemLanguage);
        }
    }
}