using UnityEngine;
using UnityEngine.UI;

public class ButtonVibrate : MonoBehaviour
{
    #region Fields

    private Button _buttonClick;

    #endregion

    #region Initialize

    private void Start()
    {
        _buttonClick = GetComponent<Button>();
        if (_buttonClick == null) return;

        _buttonClick.onClick.AddListener(Vibrate);
    }

    #endregion

    #region Private Methods

    private void Vibrate()
    {
        VibrateManager.Vibrate();
    }

    #endregion
}