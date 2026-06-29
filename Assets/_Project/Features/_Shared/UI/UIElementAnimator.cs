using DG.Tweening;
using UnityEngine;

public class UIElementAnimator : MonoBehaviour
{
    public enum Direction
    {
        Left,
        Right,
        Up,
        Down
    }

    [Header("Animation Settings")] public Direction moveDirection = Direction.Left;

    public float moveDistance = 800f; // khoảng cách bay ra khỏi cam
    public float duration = 0.5f;
    public Ease easeIn = Ease.OutCubic;
    public Ease easeOut = Ease.InCubic;
    private Vector2 offscreenPos;
    private Vector2 originalPos;

    private RectTransform rect;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        originalPos = rect.anchoredPosition;
        offscreenPos = GetOffscreenPos();
    }

    // Tính vị trí ngoài màn hình
    private Vector2 GetOffscreenPos()
    {
        switch (moveDirection)
        {
            case Direction.Left: return originalPos + Vector2.left * moveDistance;
            case Direction.Right: return originalPos + Vector2.right * moveDistance;
            case Direction.Up: return originalPos + Vector2.up * moveDistance;
            case Direction.Down: return originalPos + Vector2.down * moveDistance;
            default: return originalPos;
        }
    }

    public void MoveOut()
    {
        rect.DOKill();
        rect.DOAnchorPos(offscreenPos, duration).SetEase(easeOut);
    }

    public void MoveIn()
    {
        rect.DOKill();
        rect.anchoredPosition = offscreenPos;
        rect.DOAnchorPos(originalPos, duration).SetEase(easeIn);
    }
}