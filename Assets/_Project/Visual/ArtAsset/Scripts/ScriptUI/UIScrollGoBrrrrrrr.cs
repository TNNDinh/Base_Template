using UnityEngine;
using UnityEngine.UI;

public class Scroller : MonoBehaviour
{
    [SerializeField] private RawImage _img;
    [SerializeField] private float _x, _y;
    [SerializeField] private bool _unScaleTime;

    private void Update()
    {
        _img.uvRect =
            new Rect(
                _img.uvRect.position + new Vector2(_x, _y) * (_unScaleTime ? Time.unscaledDeltaTime : Time.deltaTime),
                _img.uvRect.size);
    }

    public void ChangeXValue(float value)
    {
        _x = value;
    }

    public float GetXValue()
    {
        return _x;
    }
}