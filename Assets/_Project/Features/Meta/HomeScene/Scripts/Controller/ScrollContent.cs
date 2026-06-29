using UnityEngine;

public class ScrollContent : MonoBehaviour
{
    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        rtChildren = new RectTransform[rectTransform.childCount];

        for (var i = 0; i < rectTransform.childCount; i++) rtChildren[i] = rectTransform.GetChild(i) as RectTransform;

        // Subtract the margin from both sides.
        Width = rectTransform.rect.width - 2 * horizontalMargin;

        // Subtract the margin from the top and bottom.
        height = rectTransform.rect.height - 2 * verticalMargin;

        ChildWidth = rtChildren[0].rect.width;
        childHeight = rtChildren[0].rect.height;

        horizontal = !vertical;
        if (vertical)
            InitializeContentVertical();
        else
            InitializeContentHorizontal();
    }

    /// <summary>
    ///     Initializes the scroll content if the scroll view is oriented horizontally.
    /// </summary>
    private void InitializeContentHorizontal()
    {
        var originX = 0 - Width * 0.5f;
        var posOffset = ChildWidth * 0.5f;
        for (var i = 0; i < rtChildren.Length; i++)
        {
            Vector2 childPos = rtChildren[i].localPosition;
            childPos.x = originX + posOffset + i * (ChildWidth + itemSpacing);
            rtChildren[i].localPosition = childPos;
        }
    }

    /// <summary>
    ///     Initializes the scroll content if the scroll view is oriented vertically.
    /// </summary>
    private void InitializeContentVertical()
    {
        var originY = 0 - height * 0.5f;
        var posOffset = childHeight * 0.5f;
        for (var i = 0; i < rtChildren.Length; i++)
        {
            Vector2 childPos = rtChildren[i].localPosition;
            childPos.y = originY + posOffset + i * (childHeight + itemSpacing);
            rtChildren[i].localPosition = childPos;
        }
    }

    #region Public Properties

    /// <summary>
    ///     How far apart each item is in the scroll view.
    /// </summary>
    public float ItemSpacing => itemSpacing;

    /// <summary>
    ///     How much the items are indented from left and right of the scroll view.
    /// </summary>
    public float HorizontalMargin => horizontalMargin;

    /// <summary>
    ///     How much the items are indented from top and bottom of the scroll view.
    /// </summary>
    public float VerticalMargin => verticalMargin;

    /// <summary>
    ///     Is the scroll view oriented horizontally?
    /// </summary>
    public bool Horizontal => horizontal;

    /// <summary>
    ///     Is the scroll view oriented vertically?
    /// </summary>
    public bool Vertical => vertical;

    /// <summary>
    ///     The width of the scroll content.
    /// </summary>
    public float Width { get; private set; }

    /// <summary>
    ///     The height of the scroll content.
    /// </summary>
    public float Height => height;

    /// <summary>
    ///     The width for each child of the scroll view.
    /// </summary>
    public float ChildWidth { get; private set; }

    /// <summary>
    ///     The height for each child of the scroll view.
    /// </summary>
    public float ChildHeight => childHeight;

    #endregion

    #region Private Members

    /// <summary>
    ///     The RectTransform component of the scroll content.
    /// </summary>
    private RectTransform rectTransform;

    /// <summary>
    ///     The RectTransform components of all the children of this GameObject.
    /// </summary>
    private RectTransform[] rtChildren;

    /// <summary>
    ///     The width and height of the parent.
    /// </summary>
    private float height;

    /// <summary>
    ///     The width and height of the children GameObjects.
    /// </summary>
    private float childHeight;

    /// <summary>
    ///     How far apart each item is in the scroll view.
    /// </summary>
    [SerializeField] private float itemSpacing;

    /// <summary>
    ///     How much the items are indented from the top/bottom and left/right of the scroll view.
    /// </summary>
    [SerializeField] private float horizontalMargin, verticalMargin;

    /// <summary>
    ///     Is the scroll view oriented horizontall or vertically?
    /// </summary>
    [SerializeField] private bool horizontal, vertical;

    #endregion
}