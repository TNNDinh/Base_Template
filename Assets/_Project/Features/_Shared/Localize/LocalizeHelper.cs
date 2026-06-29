using Ezg.Package.Localize;
using TigerForge;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.Shared.Localize
{
public enum TextCaseComponent
{
    None,
    UpperCase,
    LowerCase,
    CamelCase
}

public class LocalizeHelper : MonoBehaviour
{
    public TextCaseComponent caseSelect = TextCaseComponent.None;
    public string Key;
    public LocalizeCategory Category;

    private bool _mStarted;

    private Text textComponent;

    public string Value
    {
        set
        {
            if (string.IsNullOrEmpty(value))
                textComponent.text = $"#{Category}_{Key}";
            else
                FinalizeText(value);
        }
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif
        textComponent = GetComponent<Text>();
        _mStarted = true;
        OnLocalize();
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif
        if (_mStarted)
            OnLocalize();

        EventManager.StartListening(EventName.SettingLanguageChanged, OnLocalize);
    }

    private void OnDisable()
    {
        EventManager.StopListening(EventName.SettingLanguageChanged, OnLocalize);
    }

    private void OnLocalize()
    {
        // If no localization key has been specified, use the label's text as the key
        if (string.IsNullOrEmpty(Key))
            if (textComponent != null)
                Key = textComponent.text;

        // If we still don't have a key, leave the value as default text in Text Component
        if (!string.IsNullOrEmpty(Key)) Value = GameSystems.Localize(Key, Category);
    }

    private void FinalizeText(string text)
    {
        if (textComponent == null) return;

        switch (caseSelect)
        {
            case TextCaseComponent.None:
                textComponent.text = text;
                break;
            case TextCaseComponent.UpperCase:
                textComponent.text = text.ToUpper();
                break;
            case TextCaseComponent.LowerCase:
                textComponent.text = text.ToLower();
                break;
            case TextCaseComponent.CamelCase:
                textComponent.text = text;
                break;
        }
    }
}
}