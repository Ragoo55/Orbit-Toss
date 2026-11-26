using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace OrbitToss
{
    /// <summary>
    /// Switches between the world-space UI panels (Main menu, Tutorial, Settings, in-game HUD) and handles their
    /// buttons. Buttons are wired to these public methods in the Inspector (Button > On Click), and are pressed with
    /// the controller rays from the Meta Interaction SDK rig.
    /// </summary>
    public class VRMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [Tooltip("Canvas that holds the Main menu, Tutorial and Settings panels. Hidden entirely during play.")]
        [SerializeField] private GameObject menuCanvasRoot;
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject hudPanel;

        [Header("References")]
        [SerializeField] private GameManager game;
        [SerializeField] private SettingsManager settings;
        [SerializeField] private AudioManager audioManager;

        [Header("Tutorial")]
        [SerializeField] private TMP_Text tutorialBody;
        [SerializeField] private TMP_Text tutorialPageLabel;
        [TextArea(3, 8)]
        [SerializeField] private string[] tutorialPages =
        {
            "WELCOME TO ORBIT TOSS\n\nBoccia is a precision ball sport. Throw your planet balls as close as possible to the small WHITE ball, called the Jack.\n\nYou can play seated. You never need to walk.",
            "1. GRAB\n\nReach for a planet ball on the tray beside you. Hold the GRIP button (middle finger) on your controller to pick it up.",
            "2. AIM AND THROW\n\nFace the white Jack. Swing your arm gently towards it and let go of the GRIP button to release.\n\nA soft, smooth swing works best. A very gentle drop returns the ball to the tray.",
            "3. SCORE\n\nWhen the ball stops, you see its distance to the Jack. The closest ball is marked CLOSEST.\n\nFeedback: [***] Perfect, [**] Good Throw, [*] Close, [~] Try Again.",
            "4. MENUS\n\nPoint your controller at a button and pull the TRIGGER (index finger) to press it.\n\nUse RESET for a new round and MENU to return here. Settings change colours, volume, vibration and the tray height."
        };

        [Header("Settings labels")]
        [SerializeField] private TMP_Text themeValue;
        [SerializeField] private TMP_Text masterValue;
        [SerializeField] private TMP_Text musicValue;
        [SerializeField] private TMP_Text sfxValue;
        [SerializeField] private TMP_Text musicToggleValue;
        [SerializeField] private TMP_Text hapticsValue;
        [SerializeField] private TMP_Text rackHeightValue;
        [SerializeField] private TMP_Text rackSideValue;

        private int tutorialPage;
        private readonly List<GameObject> extraPanels = new List<GameObject>();
        private LeaderboardUI leaderboard;

        private void Awake()
        {
            // The leaderboard screens are built at start-up inside the menu canvas (see LeaderboardUI).
            if (mainMenuPanel == null) return;
            leaderboard = GetComponent<LeaderboardUI>();
            if (leaderboard == null) leaderboard = gameObject.AddComponent<LeaderboardUI>();
            leaderboard.Initialize(this, game, mainMenuPanel);
        }

        public void Configure(GameObject menuCanvas, GameObject main, GameObject tutorial, GameObject settingsPanelObject, GameObject hud,
            GameManager gameManager, SettingsManager settingsManager, AudioManager audio)
        {
            menuCanvasRoot = menuCanvas;
            mainMenuPanel = main; tutorialPanel = tutorial; settingsPanel = settingsPanelObject; hudPanel = hud;
            game = gameManager; settings = settingsManager; audioManager = audio;
        }

        public void ConfigureTutorial(TMP_Text body, TMP_Text pageLabel)
        {
            tutorialBody = body; tutorialPageLabel = pageLabel;
        }

        public void ConfigureSettingsLabels(TMP_Text theme, TMP_Text master, TMP_Text music, TMP_Text sfx,
            TMP_Text musicToggle, TMP_Text haptics, TMP_Text rackHeight, TMP_Text rackSide)
        {
            themeValue = theme; masterValue = master; musicValue = music; sfxValue = sfx;
            musicToggleValue = musicToggle; hapticsValue = haptics; rackHeightValue = rackHeight; rackSideValue = rackSide;
        }

        private void OnEnable() { if (settings != null) settings.Changed += RefreshSettingsLabels; }
        private void OnDisable() { if (settings != null) settings.Changed -= RefreshSettingsLabels; }

        // ---------------------------------------------------------------- panel switching

        public void ShowMainMenu() => ShowOnly(mainMenuPanel);
        public void ShowHud() => ShowOnly(hudPanel);

        /// <summary>Shows any registered panel (used by the leaderboard screens).</summary>
        public void ShowPanel(GameObject panel) => ShowOnly(panel);

        /// <summary>Adds an extra panel inside the menu canvas so panel switching hides it too.</summary>
        public void RegisterPanel(GameObject panel)
        {
            if (panel != null && !extraPanels.Contains(panel)) extraPanels.Add(panel);
        }

        /// <summary>Called by GameManager when a round ends: shows the score and initials entry.</summary>
        public void ShowRoundComplete(int score, float bestDistance)
        {
            if (leaderboard != null) leaderboard.ShowRoundComplete(score, bestDistance);
        }

        private void ShowOnly(GameObject panel)
        {
            if (menuCanvasRoot != null) menuCanvasRoot.SetActive(panel != hudPanel);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(panel == mainMenuPanel);
            if (tutorialPanel != null) tutorialPanel.SetActive(panel == tutorialPanel);
            if (settingsPanel != null) settingsPanel.SetActive(panel == settingsPanel);
            if (hudPanel != null) hudPanel.SetActive(panel == hudPanel);
            foreach (var extra in extraPanels) if (extra != null) extra.SetActive(extra == panel);
        }

        // ---------------------------------------------------------------- button handlers

        public void OnPlayPressed() { Click(); if (game != null) game.StartPractice(); }

        public void OnTutorialPressed()
        {
            Click();
            tutorialPage = 0;
            RefreshTutorial();
            ShowOnly(tutorialPanel);
        }

        public void OnSettingsPressed()
        {
            Click();
            RefreshSettingsLabels();
            ShowOnly(settingsPanel);
        }

        public void OnBackPressed() { Click(); ShowOnly(mainMenuPanel); }

        public void OnQuitPressed()
        {
            Click();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OnResetPressed() { Click(); if (game != null) game.ResetPractice(); }
        public void OnMenuPressed() { Click(); if (game != null) game.ReturnToMenu(); }

        public void OnTutorialNext()
        {
            Click();
            if (tutorialPage < tutorialPages.Length - 1) tutorialPage++;
            RefreshTutorial();
        }

        public void OnTutorialPrevious()
        {
            Click();
            if (tutorialPage > 0) tutorialPage--;
            RefreshTutorial();
        }

        /// <summary>Plays the UI click sound. Also wired to settings buttons.</summary>
        public void Click() { if (audioManager != null) audioManager.PlayUiClick(); }

        // ---------------------------------------------------------------- refresh

        private void RefreshTutorial()
        {
            if (tutorialPages == null || tutorialPages.Length == 0) return;
            tutorialPage = Mathf.Clamp(tutorialPage, 0, tutorialPages.Length - 1);
            if (tutorialBody != null) tutorialBody.text = tutorialPages[tutorialPage];
            if (tutorialPageLabel != null) tutorialPageLabel.text = $"Page {tutorialPage + 1} of {tutorialPages.Length}";
        }

        private void RefreshSettingsLabels()
        {
            if (settings == null) return;
            Set(themeValue, settings.ThemeName);
            Set(masterValue, Percent(settings.MasterVolume));
            Set(musicValue, Percent(settings.MusicVolume));
            Set(sfxValue, Percent(settings.SfxVolume));
            Set(musicToggleValue, settings.MusicEnabled ? "ON" : "OFF");
            Set(hapticsValue, settings.HapticsEnabled ? "ON" : "OFF");
            Set(rackHeightValue, settings.CurrentRackHeight.ToString().ToUpperInvariant());
            Set(rackSideValue, settings.RackOnRight ? "RIGHT HAND" : "LEFT HAND");
        }

        private static string Percent(float v) => $"{Mathf.RoundToInt(v * 100f)}%";
        private static void Set(TMP_Text t, string s) { if (t != null) t.text = s; }
    }
}
