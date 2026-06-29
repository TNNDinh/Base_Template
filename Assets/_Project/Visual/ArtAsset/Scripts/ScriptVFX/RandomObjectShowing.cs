using System.Collections.Generic;
using UnityEngine;

public class RandomObjectShowing : MonoBehaviour
{
    public int current;
    public List<GameObject> objects;

    private void OnEnable()
    {
        if (objects.Count == 0) return; // Check to avoid errors if the list is empty

        var randomIndex = Random.Range(0, objects.Count); // Get a random index
        objects[randomIndex].SetActive(true); // Activate the randomly selected object

        current = randomIndex; // Store the index of the currently active object
    }

    private void OnDisable()
    {
        foreach (var obj in objects) obj.SetActive(false); // Deactivate all objects
    }
}