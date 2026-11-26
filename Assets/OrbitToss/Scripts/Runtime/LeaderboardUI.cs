using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OrbitToss
{
    /// <summary>
    /// "My Best Scores" screens (single player), created at start-up inside the existing menu canvas so the
    /// controller rays already work:
    ///   - MY BEST SCORES button on the main menu
    ///   - "Round complete" panel: score, best ball, and whether this round is a new best (saved automatically)
    ///   - "My Best Scores" panel: the 10 best rounds with score, best ball, date and time; this round is marked "&gt; NEW"
    /// Buttons and texts are copied from the main menu, so they follow the colour themes automatically.
    /// </summary>
    public class LeaderboardUI : MonoBehaviour
    {
        private VRMenuController menu;
        private GameManager game;
        private LeaderboardService service;

        private Button buttonTemplate;
        private TMP_Text textTemplate;
        private TMP_Text accentTemplate;
        private Image panelTemplate;

        private GameObject roundPanel;
        private GameObject scoresPanel;

        private TMP_Text roundScoreText;
        private TMP_Text roundBestText;
        private TMP_Text roundStatusText;

        private readonly List<TMP_Text> rowTexts = new List<TMP_Text>();
        private TMP_Text emptyText;
        private int lastSavedRank = -1;

        public LeaderboardService Service => service;

        /// <summary>Builds the panels. Called once by VRMenuController.</summary>
        public void Initialize(VRMenuController menuController, GameManager gameManager, GameObject mainMenuPanel)
        {
            menu = menuController;
            game = gameManager;
            service = new LeaderboardService();

            Transform main = mainMenuPanel.transform;
            buttonTemplate = main.Find("PlayButton")?.GetComponent<Button>();
            textTemplate = main.Find("Subtitle")?.GetComponent<TMP_Text>();
            accentTemplate = main.Find("Title")?.GetComponent<TMP_Text>();
            panelTemplate = mainMenuPanel.GetComponent<Image>();
            if (buttonTemplate == null || textTemplate == null || accentTemplate == null)
            {
                Debug.LogWarning("[OrbitToss] My Best Scores: main menu templates not found, feature disabled.");
                return;
            }

            AddMainMenuButton(main);
            BuildRoundPanel(main.parent);
            BuildScoresPanel(main.parent);
            menu.RegisterPanel(roundPanel);
            menu.RegisterPanel(scoresPanel);
        }

        // ================================================================== public flow

        /// <summary>Shown at the end of a round. The round is saved automatically if it is one of the 10 best.</summary>
        public void ShowRoundComplete(int score, float bestDistance)
        {
            if (roundPanel == null) return;

            bool qualifies = service.Qualifies(score);
            lastSavedRank = qualifies ? service.Add(score, bestDistance) : -1;

            roundScoreText.text = $"Your score: {score} points";
            roundBestText.text = bestDistance >= 0f ? $"Best ball: {DistanceCalculator.Format(bestDistance)} from the Jack" : "Best ball: none in play";
            if (lastSavedRank == 0) roundStatusText.text = "NEW PERSONAL BEST!";
            else if (lastSavedRank > 0) roundStatusText.text = $"Saved as #{lastSavedRank + 1} in your best scores.";
            else roundStatusText.text = "Not in your 10 best this time - keep practising!";

            menu.ShowPanel(roundPanel);
        }

        /// <summary>Shows the 10 best rounds. highlightIndex marks this round's entry (-1 for none).</summary>
        public void ShowScores(int highlightIndex = -1)
        {
            if (scoresPanel == null) return;
            var entries = service.Entries;
            for (int i = 0; i < rowTexts.Count; i++)
            {
                if (i >= entries.Count) { rowTexts[i].text = ""; continue; }
                var e = entries[i];
                bool isNew = i == highlightIndex;
                string best = e.bestDistance >= 0f ? DistanceCalculator.Format(e.bestDistance) : "--";
                string date = e.date != null && e.date.Length >= 10 ? $"{e.date.Substring(8, 2)}/{e.date.Substring(5, 2)}" : "";
                string when = string.IsNullOrEmpty(e.time) ? date : $"{date} {e.time}";
                rowTexts[i].text = $"<pos=0%>{(isNew ? ">" : "")}{i + 1}<pos=12%>{e.score}<pos=32%>{best}<pos=58%>{when}{(isNew ? "  NEW" : "")}";
                rowTexts[i].fontStyle = isNew ? FontStyles.Bold : FontStyles.Normal;
            }
            emptyText.gameObject.SetActive(entries.Count == 0);
            menu.ShowPanel(scoresPanel);
        }

        // ================================================================== button handlers

        private void OnBestScoresPressed() { menu.Click(); ShowScores(); }
        private void OnSeeScoresPressed() { menu.Click(); ShowScores(lastSavedRank); }
        private void OnPlayAgainPressed() { menu.Click(); if (game != null) game.StartPractice(); }
        private void OnMainMenuPressed() { menu.Click(); if (game != null) game.ReturnToMenu(); else menu.ShowMainMenu(); }

        // ================================================================== building

        /// <summary>Adds MY BEST SCORES between SETTINGS and QUIT and re-spaces the main menu buttons.</summary>
        private void AddMainMenuButton(Transform main)
        {
            Button scores = MakeButton(main, "BestScoresButton", "MY BEST SCORES", 0, 0, 620, 110, 46, OnBestScoresPressed);
            Transform quit = main.Find("QuitButton");
            if (quit != null) scores.transform.SetSiblingIndex(quit.GetSiblingIndex());

            string[] order = { "PlayButton", "TutorialButton", "SettingsButton", "BestScoresButton", "QuitButton" };
            for (int i = 0; i < order.Length; i++)
            {
                var rt = main.Find(order[i]) as RectTransform;
                if (rt != null) Place(rt, 0, 270 + i * 125, 620, 110);
            }
            var hint = main.Find("Hint") as RectTransform;
            if (hint != null) Place(hint, 0, 925, 840, 80);
        }

        private void BuildRoundPanel(Transform canvas)
        {
            roundPanel = MakePanel(canvas, "RoundCompletePanel");
            Transform p = roundPanel.transform;

            MakeText(p, "Title", "ROUND COMPLETE", 70, 60, 840, 100, accent: true, bold: true);
            roundScoreText = MakeText(p, "Score", "", 60, 220, 840, 90, accent: false, bold: true);
            roundBestText = MakeText(p, "Best", "", 40, 320, 840, 70, accent: false);
            roundStatusText = MakeText(p, "Status", "", 46, 420, 840, 120, accent: true, bold: true);

            MakeButton(p, "SeeScoresButton", "MY BEST SCORES", 0, 600, 620, 110, 46, OnSeeScoresPressed);
            MakeButton(p, "PlayAgainButton", "PLAY AGAIN", 0, 730, 620, 110, 46, OnPlayAgainPressed);
            MakeButton(p, "MenuButton", "MAIN MENU", 0, 860, 620, 110, 46, OnMainMenuPressed);
            roundPanel.SetActive(false);
        }

        private void BuildScoresPanel(Transform canvas)
        {
            scoresPanel = MakePanel(canvas, "BestScoresPanel");
            Transform p = scoresPanel.transform;

            MakeText(p, "Title", "MY BEST SCORES", 66, 30, 840, 90, accent: true, bold: true);
            var header = MakeText(p, "Header", "<pos=0%>#<pos=12%>SCORE<pos=32%>BEST BALL<pos=58%>DATE", 32, 130, 800, 50, accent: true, bold: true);
            header.alignment = TextAlignmentOptions.Left;
            for (int i = 0; i < LeaderboardService.MaxEntries; i++)
            {
                var row = MakeText(p, $"Row{i + 1}", "", 36, 190 + i * 62, 800, 58, accent: false);
                row.alignment = TextAlignmentOptions.Left;
                rowTexts.Add(row);
            }
            emptyText = MakeText(p, "Empty", "No scores yet.\nPlay a round to set your first record!", 40, 420, 800, 160, accent: false);

            MakeButton(p, "PlayAgainButton", "PLAY AGAIN", -200, 840, 360, 110, 44, OnPlayAgainPressed);
            MakeButton(p, "MenuButton", "MAIN MENU", 200, 840, 360, 110, 44, OnMainMenuPressed);
            scoresPanel.SetActive(false);
        }

        // ================================================================== helpers (copy the main menu's look)

        private GameObject MakePanel(Transform canvas, string panelName)
        {
            var go = new GameObject(panelName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            if (panelTemplate != null)
            {
                img.sprite = panelTemplate.sprite;
                img.type = panelTemplate.type;
                img.color = panelTemplate.color;
            }
            go.AddComponent<ThemedGraphic>().Role = ThemeRole.PanelBackground;
            return go;
        }

        private TMP_Text MakeText(Transform parent, string textName, string content, float size, float y, float width, float height,
            bool accent, bool bold = false, float x = 0f)
        {
            TMP_Text t = Instantiate(accent ? accentTemplate : textTemplate, parent);
            t.name = textName;
            t.text = content;
            t.fontSize = size;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.alignment = TextAlignmentOptions.Center;
            Place(t.rectTransform, x, y, width, height);
            return t;
        }

        private Button MakeButton(Transform parent, string buttonName, string label, float x, float y, float width, float height,
            float fontSize, UnityEngine.Events.UnityAction onClick)
        {
            Button b = Instantiate(buttonTemplate, parent);
            b.name = buttonName;
            b.onClick = new Button.ButtonClickedEvent(); // drop the copied PLAY action
            b.onClick.AddListener(onClick);
            var text = b.GetComponentInChildren<TMP_Text>();
            text.text = label;
            text.fontSize = fontSize;
            Place((RectTransform)b.transform, x, y, width, height);
            return b;
        }

        /// <summary>Positions an element: y from the top of the panel, x from its centre (same layout as the other menus).</summary>
        private static void Place(RectTransform rt, float x, float y, float width, float height)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(x, -y);
        }
    }
}
