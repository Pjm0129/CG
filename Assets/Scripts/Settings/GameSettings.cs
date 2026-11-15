using UnityEngine;

public static class GameSettings
{
    private const string MasterVolumeKey = "MasterVolume";
    private const string MouseSensitivityKey = "MouseSensitivity";

    public const float DefaultMasterVolume = 0.7f;
    public const float DefaultMouseSensitivity = 1.0f;

    public const float MinMouseSensitivity = 0.3f;
    public const float MaxMouseSensitivity = 3.0f;

    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(MasterVolumeKey, DefaultMasterVolume);
        set
        {
            float volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterVolumeKey, volume);
            AudioListener.volume = volume;
        }
    }

    public static float MouseSensitivity
    {
        get => PlayerPrefs.GetFloat(MouseSensitivityKey, DefaultMouseSensitivity);
        set
        {
            float sensitivity = Mathf.Clamp(
                value,
                MinMouseSensitivity,
                MaxMouseSensitivity
            );

            PlayerPrefs.SetFloat(MouseSensitivityKey, sensitivity);
        }
    }

    public static void Apply()
    {
        AudioListener.volume = MasterVolume;
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    public static void ResetToDefaults()
    {
        MasterVolume = DefaultMasterVolume;
        MouseSensitivity = DefaultMouseSensitivity;
        Save();
    }
}
