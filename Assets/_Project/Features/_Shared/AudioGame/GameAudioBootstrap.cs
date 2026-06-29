using Ezg.Package.Audio;
using UnityEngine;

namespace Ezg.Game.Audio
{
    /// <summary>
    ///     Wires the game's <see cref="GameSoundSettings" /> into the audio package and assigns
    ///     the shared <see cref="AudioService.Default" /> instance before any scene loads.
    /// </summary>
    public static class GameAudioBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            AudioService.Default = new AudioService(new GameSoundSettings());
        }
    }
}