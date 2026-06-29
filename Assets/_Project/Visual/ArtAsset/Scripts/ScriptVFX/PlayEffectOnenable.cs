using UnityEngine;

[ExecuteInEditMode]
public class PlayEffectOnenable : MonoBehaviour
{
    private ParticleSystem _particleSystem;

    private void Awake()
    {
        _particleSystem = transform.GetComponent<ParticleSystem>();
    }

    private void OnEnable()
    {
        if (_particleSystem != null) _particleSystem.Play();
    }
}