using UnityEngine;
using UnityEngine.UI;

public class SliderValueText : MonoBehaviour
{
    public Slider slider;
    public Text textComp;

    private void Awake()
    {
        // Update the text value in the editor as well
        if (Application.isPlaying) // Check if application is playing
            UpdateText(slider.value);
        else // Update text in the editor (assuming a default value for slider)
            UpdateText(slider.value); // You can replace this with a specific default value if needed
    }

    private void Start()
    {
        if (Application.isPlaying) // Only add listener during runtime
            slider.onValueChanged.AddListener(UpdateText);
    }

    private void UpdateText(float val)
    {
        textComp.text = val.ToString();
    }
}