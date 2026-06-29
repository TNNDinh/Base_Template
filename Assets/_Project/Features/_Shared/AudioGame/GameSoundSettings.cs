using Ezg.Feature.Shared;
using Ezg.Package.Audio;
using Ezg.Feature.Shared.GameData;

namespace Ezg.Game.Audio
{
    /// <summary>
    ///     Game-side bridge that lets the audio package read/persist volumes through the
    ///     player-data layer without the package itself depending on it.
    /// </summary>
    public class GameSoundSettings : ISoundSettings
    {
        public float GetMusicVolume()
        {
            return PlayerDataManager.Settings.GetMusic();
        }

        public float GetSoundVolume()
        {
            return PlayerDataManager.Settings.GetSound();
        }

        public void SetMusicVolume(float value)
        {
            PlayerDataManager.Settings.SetMusicVolumne(value);
        }

        public void SetSoundVolume(float value)
        {
            PlayerDataManager.Settings.SetSoundVolume(value);
        }

        public void Save()
        {
            PlayerDataManager.Settings.Save();
        }
    }
}