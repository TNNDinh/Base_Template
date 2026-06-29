using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ToggleEventExtensions : MonoBehaviour
{
// Sự kiện khi Toggle bật (true)
    public UnityEvent onTrue;

    // Sự kiện khi Toggle tắt (false)
    public UnityEvent onFalse;

    private Toggle toggle;

    private void Awake()
    {
        // Lấy component Toggle từ GameObject
        toggle = GetComponent<Toggle>();
        // Đăng ký listener cho sự kiện onValueChanged
        toggle.onValueChanged.AddListener(OnValueChanged);
    }

    // Hàm được gọi khi giá trị Toggle thay đổi
    private void OnValueChanged(bool isOn)
    {
        if (isOn)
            onTrue.Invoke(); // Gọi sự kiện khi Toggle bật
        else
            onFalse.Invoke(); // Gọi sự kiện khi Toggle tắt
    }
}