using System.Collections;
using UnityEngine;

public class UVAutoScroll : MonoBehaviour
{
    public int materialIndex;
    public Vector2 fromOffset = Vector2.zero;
    public Vector2 toOffset = Vector2.one;
    public float scrollDuration = 1f;
    public bool loop;
    private Coroutine scrollRoutine;

    private Material targetMat;

    private void Start()
    {
        if (TryGetComponent(out Renderer renderer))
        {
            if (materialIndex >= 0 && materialIndex < renderer.materials.Length)
            {
                targetMat = renderer.materials[materialIndex]; // Get specific material instance
                StartScroll();
            }
            else
            {
                Debug.LogWarning("Invalid material index.");
            }
        }
    }

    public void StopScroll()
    {
        if (scrollRoutine != null)
        {
            StopCoroutine(scrollRoutine);
            scrollRoutine = null;
        }
    }

    public void StartScroll()
    {
        StopScroll();
        scrollRoutine = StartCoroutine(ScrollUV());
    }

    private IEnumerator ScrollUV()
    {
        do
        {
            var elapsedTime = 0f;
            while (elapsedTime < scrollDuration)
            {
                var t = elapsedTime / scrollDuration;
                targetMat.mainTextureOffset = Vector2.Lerp(fromOffset, toOffset, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            targetMat.mainTextureOffset = toOffset;
        } while (loop);
    }
}