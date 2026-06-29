using System;
using Ezg.Feature.Shared;
using UnityEngine;
using Ezg.Feature.Shared.GameData;

public static class VibrateManager
{
    #region Fields

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaClass _unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
    private static AndroidJavaObject _currentActivity = _unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
    private static AndroidJavaObject _vibrator =
 _currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
    private static AndroidJavaClass _vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
#else
    private static AndroidJavaClass _unityPlayer;
    private static AndroidJavaObject _currentActivity;
    private static AndroidJavaObject _vibrator;
    private static AndroidJavaClass _vibrationEffectClass;
#endif

    #endregion

    #region Public Methods

    /// <summary>
    ///     Rung với thời gian và cường độ tùy chỉnh.
    /// </summary>
    /// <param name="milliseconds">Thời gian rung (ms)</param>
    /// <param name="amplitude">Cường độ rung (0–255)</param>
    public static void Vibrate(long milliseconds = 100, int amplitude = 255)
    {
        try
        {
            if (!PlayerDataManager.Settings.GetVibrate()) return;

            if (IsAndroid())
                _vibrator.Call("vibrate", milliseconds);
            else
                Taptic.Light();
        }
        catch (Exception)
        {
            // ignored
        }
    }

    /// <summary>
    ///     Rung theo pattern phức tạp.
    /// </summary>
    /// <param name="pattern">Mảng thời gian rung/nghỉ (ms)</param>
    /// <param name="amplitudes">Mảng cường độ tương ứng (0–255)</param>
    /// <param name="repeat">Số lần lặp (-1 = vô hạn)</param>
    public static void VibratePattern(long[] pattern, int[] amplitudes, int repeat = 0)
    {
        if (!PlayerDataManager.Settings.GetVibrate() || pattern == null || amplitudes == null) return;
        if (pattern.Length != amplitudes.Length) return;

        if (IsAndroid())
            using (var versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                var sdkVersion = versionClass.GetStatic<int>("SDK_INT");
                if (sdkVersion >= 26)
                {
                    var vibrationEffect = _vibrationEffectClass.CallStatic<AndroidJavaObject>(
                        "createWaveform", pattern, amplitudes, repeat);
                    _vibrator.Call("vibrate", vibrationEffect);
                }
                else
                {
                    _vibrator.Call("vibrate", pattern, repeat);
                }
            }
        else
            Handheld.Vibrate();
    }

    public static void Cancel()
    {
        if (IsAndroid()) _vibrator.Call("cancel");
    }

    public static bool IsAndroid()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return true;
#else
        return false;
#endif
    }

    #endregion
}