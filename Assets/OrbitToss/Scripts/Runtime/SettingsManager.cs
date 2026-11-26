using System;
using UnityEngine;

namespace OrbitToss
{
    /// <summary>
    /// Player settings (accessibility + comfort), saved with PlayerPrefs.
    /// Other systems listen to <see cref="Changed"/> and apply the values (theme, audio, haptics, rack position).
    /// All setters are simple public methods so they can be wired directly to UI buttons in the Inspector.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public enum RackHeight { Low = 0, Medium = 1, High = 2 }

        [Header("Defaults")]
        [SerializeField] private int defaultTheme = 0;
        [SerializeField, Range(0f, 1f)] private float defaultMasterVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.4f;
        [SerializeField, Range(0f, 1f)] private float defaultSfxVolume = 0.8f;
        [SerializeField] private bool defaultMusicEnabled = true;
        [SerializeField] private bool defaultHapticsEnabled = true;
        [SerializeField] private RackHeight defaultRackHeight = RackHeight.Medium;
        [SerializeField] private bool defaultRackOnRight = true;
        [Tooltip("Volume change per +/- button press.")]
        [SerializeField] private float volumeStep = 0.1f;

        [Header("References")]
        [SerializeField] private ThemeManager themeManager;

        public event Action Changed;

        public int ThemeIndex { get; private set; }
        public float MasterVolume { get; private set; }
        public float MusicVolume { get; private set; }
        public float SfxVolume { get; private set; }
        public bool MusicEnabled { get; private set; }
        public bool HapticsEnabled { get; private set; }
        public RackHeight CurrentRackHeight { get; private set; }
        public bool RackOnRight { get; private set; }

        public string ThemeName => themeManager != null ? themeManager.ThemeName(ThemeIndex) : $"Theme {ThemeIndex + 1}";

        private const string Prefix = "OrbitToss.";

        private void Awake()
        {
            Load();
        }

        private void Start()
        {
            Notify(); // apply loaded values once every listener has subscribed
        }

        // ---------------------------------------------------------------- button actions

        public void NextTheme()
        {
            int count = themeManager != null ? Mathf.Max(1, themeManager.ThemeCount) : 1;
            ThemeIndex = (ThemeIndex + 1) % count;
            SaveAndNotify();
        }

        public void MasterVolumeUp() { MasterVolume = Step(MasterVolume, +volumeStep); SaveAndNotify(); }
        public void MasterVolumeDown() { MasterVolume = Step(MasterVolume, -volumeStep); SaveAndNotify(); }
        public void MusicVolumeUp() { MusicVolume = Step(MusicVolume, +volumeStep); SaveAndNotify(); }
        public void MusicVolumeDown() { MusicVolume = Step(MusicVolume, -volumeStep); SaveAndNotify(); }
        public void SfxVolumeUp() { SfxVolume = Step(SfxVolume, +volumeStep); SaveAndNotify(); }
        public void SfxVolumeDown() { SfxVolume = Step(SfxVolume, -volumeStep); SaveAndNotify(); }
        public void ToggleMusic() { MusicEnabled = !MusicEnabled; SaveAndNotify(); }
        public void ToggleHaptics() { HapticsEnabled = !HapticsEnabled; SaveAndNotify(); }
        public void NextRackHeight() { CurrentRackHeight = (RackHeight)(((int)CurrentRackHeight + 1) % 3); SaveAndNotify(); }
        public void ToggleRackSide() { RackOnRight = !RackOnRight; SaveAndNotify(); }

        public void ResetToDefaults()
        {
            ThemeIndex = defaultTheme;
            MasterVolume = defaultMasterVolume;
            MusicVolume = defaultMusicVolume;
            SfxVolume = defaultSfxVolume;
            MusicEnabled = defaultMusicEnabled;
            HapticsEnabled = defaultHapticsEnabled;
            CurrentRackHeight = defaultRackHeight;
            RackOnRight = defaultRackOnRight;
            SaveAndNotify();
        }

        // ---------------------------------------------------------------- persistence

        private void Load()
        {
            ThemeIndex = PlayerPrefs.GetInt(Prefix + "Theme", defaultTheme);
            MasterVolume = PlayerPrefs.GetFloat(Prefix + "MasterVolume", defaultMasterVolume);
            MusicVolume = PlayerPrefs.GetFloat(Prefix + "MusicVolume", defaultMusicVolume);
            SfxVolume = PlayerPrefs.GetFloat(Prefix + "SfxVolume", defaultSfxVolume);
            MusicEnabled = PlayerPrefs.GetInt(Prefix + "MusicEnabled", defaultMusicEnabled ? 1 : 0) == 1;
            HapticsEnabled = PlayerPrefs.GetInt(Prefix + "HapticsEnabled", defaultHapticsEnabled ? 1 : 0) == 1;
            CurrentRackHeight = (RackHeight)Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "RackHeight", (int)defaultRackHeight), 0, 2);
            RackOnRight = PlayerPrefs.GetInt(Prefix + "RackOnRight", defaultRackOnRight ? 1 : 0) == 1;
            if (themeManager != null && ThemeIndex >= themeManager.ThemeCount) ThemeIndex = 0;
        }

        private void SaveAndNotify()
        {
            PlayerPrefs.SetInt(Prefix + "Theme", ThemeIndex);
            PlayerPrefs.SetFloat(Prefix + "MasterVolume", MasterVolume);
            PlayerPrefs.SetFloat(Prefix + "MusicVolume", MusicVolume);
            PlayerPrefs.SetFloat(Prefix + "SfxVolume", SfxVolume);
            PlayerPrefs.SetInt(Prefix + "MusicEnabled", MusicEnabled ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "HapticsEnabled", HapticsEnabled ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "RackHeight", (int)CurrentRackHeight);
            PlayerPrefs.SetInt(Prefix + "RackOnRight", RackOnRight ? 1 : 0);
            PlayerPrefs.Save();
            Notify();
        }

        private void Notify()
        {
            BallGrabThrow.HapticsEnabled = HapticsEnabled;
            if (themeManager != null) themeManager.Apply(ThemeIndex);
            Changed?.Invoke();
        }

        private static float Step(float value, float delta)
        {
            return Mathf.Round(Mathf.Clamp01(value + delta) * 10f) / 10f;
        }
    }
}
