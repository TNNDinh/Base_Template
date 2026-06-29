using System;
using UnityEngine;
using UnityEngine.UI;

public class ItemCheat : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Button button;

    private Action currentAction;

    private void Awake()
    {
        button.onClick.AddListener(OnButtonClick);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnButtonClick);
    }

    public void SetData(Sprite sprite, string name, Action actionSelect)
    {
        iconImage.sprite = sprite;
        nameText.text = name;
        currentAction = actionSelect;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnButtonClick()
    {
        currentAction?.Invoke();
    }
}