using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Snap : MonoBehaviour
{
    [SerializeField] private RectTransform scrollContent;
    [SerializeField] private RectTransform center;

    public bool scaleSnap;

    public int minIndex;

    public float[] distance;
    public float MaxDistance;
    public float scaleSubElement = 0.75f;

    private readonly List<RectTransform> posList = new();
    private float bttnDistance;

    private Action callback;
    private bool dragging;
    private bool startSnap;

    private void Update()
    {
        // if (startSnap)
        // {
        //     for (int i = 0; i < posList.Count; i++)
        //     {
        //         var pos = Mathf.Abs(center.transform.position.x - posList[i].transform.position.x);
        //         distance[i] = (int) pos;
        //         if (scaleSnap)
        //         {
        //             float x = 0f;
        //             if (pos <= MaxDistance)
        //             {
        //                 x = Mathf.Abs(pos - MaxDistance) / MaxDistance;
        //                 if (x < scaleSubElement) x = scaleSubElement;
        //             }
        //             else
        //             {
        //                 x = scaleSubElement;
        //             }
        //             Vector3 scale = new Vector3(Mathf.Clamp01(x), Mathf.Clamp01(x), 0);
        //             posList[i].transform.DOScale(scale, 0f);
        //         }
        //     }
        //
        //     float minDistance = Mathf.Min(distance);
        //
        //
        //     for (int a = 0; a < posList.Count; a++)
        //     {
        //         if (minDistance == distance[a])
        //         {
        //             var oldMinIndex = minIndex;
        //             minIndex = a;
        //             if (oldMinIndex != minIndex)
        //             {
        //                 callback?.Invoke();
        //             }
        //
        //             break;
        //         }
        //     }
        //
        //     if (!dragging)
        //     {
        //         LerpToImage(minIndex * (-bttnDistance));
        //     }
        // }
    }

    private void OnDisable()
    {
        startSnap = false;
    }

    public void SetupSnap(int currentIndex = 0, Action callback = null)
    {
        minIndex = currentIndex;
        this.callback = callback;
        StartCoroutine(_setupSnap());
    }

    private IEnumerator _setupSnap()
    {
        yield return new WaitForEndOfFrame();
        MaxDistance = (int)Mathf.Abs(posList[1].transform.position.x - posList[0].transform.position.x) * 2;
        ;
        bttnDistance = (int)Mathf.Abs(posList[1].anchoredPosition.x - posList[0].anchoredPosition.x);
        ;
        distance = new float[posList.Count];

        _initPos(minIndex * -bttnDistance);
        startSnap = true;
    }

    public void AddRectTransform(List<RectTransform> rects)
    {
        posList.AddRange(rects);
    }

    public void AddRectTransform(RectTransform rect)
    {
        posList.Add(rect);
    }

    private void LerpToImage(float position)
    {
        var newX = Mathf.Lerp(scrollContent.anchoredPosition.x, position, 0.025f);
        var newPosition = new Vector2(newX, scrollContent.anchoredPosition.y);
        scrollContent.anchoredPosition = newPosition;
    }

    private void _initPos(float position)
    {
        var newX = position;
        var newPosition = new Vector2(newX, scrollContent.anchoredPosition.y);
        scrollContent.anchoredPosition = newPosition;
    }

    public void OnPointerDown()
    {
    }

    public void StartDrag()
    {
        dragging = true;
    }

    public void EndDrag()
    {
        dragging = false;
    }

    public int GetIndex()
    {
        return minIndex;
    }
}