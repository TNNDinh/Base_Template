using Sirenix.OdinInspector;
using UnityEngine;

public class
    ParticleColorSetter : MonoBehaviour
{
    [Title("Color Settings")] [LabelText("Start Color")]
    public Color32 startColor = Color.white;

    [Button("Execute")]
    private void Execute()
    {
#if UNITY_EDITOR

        var systems = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            var main = ps.main;
            main.startColor = (Color)startColor;
        }

        Debug.Log($"Set start color to {startColor} for {systems.Length} particle systems.");
#endif
    }
}