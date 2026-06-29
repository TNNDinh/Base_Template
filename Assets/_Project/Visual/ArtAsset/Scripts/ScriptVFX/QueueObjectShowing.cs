using System.Collections.Generic;
using UnityEngine;

public class QueueObjectShowing : MonoBehaviour
{
    public int current;
    public List<GameObject> objects;

    private void OnEnable()
    {
        objects[current].gameObject.SetActive(true);
        current++;
        if (current >= objects.Count) current = 0;
    }

    private void OnDisable()
    {
        foreach (var obj in objects) obj.SetActive(false);
    }
}