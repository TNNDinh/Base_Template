using Sirenix.OdinInspector;
using UnityEngine;

public class AddSpritesToParticleSystem : MonoBehaviour
{
    [SerializeField] private ParticleSystem targetParticleSystem;
    [SerializeField] private Sprite[] spritesToAdd;

    [Button]
    public void AddSprites()
    {
        // Ensure a valid particle system is selected
        if (targetParticleSystem == null)
        {
            Debug.LogError("Please select a Particle System in the Inspector.");
            return;
        }

        // Remove existing sprites manually
        while (targetParticleSystem.textureSheetAnimation.spriteCount > 0)
            targetParticleSystem.textureSheetAnimation.RemoveSprite(0);

        // Add the new sprites in the specified order
        foreach (var sprite in spritesToAdd) targetParticleSystem.textureSheetAnimation.AddSprite(sprite);

        // Notify the user
        Debug.Log("Sprites added to Particle System successfully!");
    }
}