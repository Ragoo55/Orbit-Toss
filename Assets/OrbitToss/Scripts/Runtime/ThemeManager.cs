using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrbitToss
{
    /// <summary>A named colour palette. Essential information is never shown by colour alone - text labels always accompany it.</summary>
    [Serializable]
    public class ThemePalette
    {
        public string name = "Space";
        [Header("UI")]
        public Color panelBackground = new Color(0.05f, 0.07f, 0.14f, 0.92f);
        public Color panelText = Color.white;
        public Color buttonNormal = new Color(0.18f, 0.26f, 0.5f, 1f);
        public Color buttonHighlighted = new Color(0.3f, 0.42f, 0.78f, 1f);
        public Color buttonText = Color.white;
        public Color accentText = new Color(1f, 0.85f, 0.35f, 1f);
        [Header("World")]
        public Color courtSurface = new Color(0.23f, 0.26f, 0.33f, 1f);
        public Color courtLines = Color.white;
        public Color sideABalls = new Color(0.85f, 0.3f, 0.2f, 1f);  // player (Mars red)
        public Color sideBBalls = new Color(0.2f, 0.45f, 0.95f, 1f); // reserved for a future opponent (Neptune blue)
        public Color background = new Color(0.01f, 0.01f, 0.04f, 1f);
    }

    /// <summary>Marks a UI Graphic that should be recoloured by the active theme.</summary>
    public enum ThemeRole { PanelBackground, PanelText, ButtonBackground, ButtonText, AccentText }

    /// <summary>
    /// Applies the selected colour theme to UI elements (<see cref="ThemedGraphic"/>), the court materials,
    /// the player's ball material and the camera background.
    /// </summary>
    public class ThemeManager : MonoBehaviour
    {
        [SerializeField] private List<ThemePalette> themes = new List<ThemePalette>();

        [Header("World materials recoloured by the theme")]
        [SerializeField] private Material courtSurfaceMaterial;
        [SerializeField] private Material courtLineMaterial;
        [SerializeField] private Material sideABallMaterial;
        [SerializeField] private Material sideBBallMaterial;

        public int ThemeCount => themes.Count;
        public ThemePalette Current { get; private set; }
        public event Action<ThemePalette> ThemeApplied;

        public string ThemeName(int index) => (index >= 0 && index < themes.Count) ? themes[index].name : "Default";

        public void Configure(List<ThemePalette> palettes, Material court, Material lines, Material sideA, Material sideB)
        {
            themes = palettes;
            courtSurfaceMaterial = court;
            courtLineMaterial = lines;
            sideABallMaterial = sideA;
            sideBBallMaterial = sideB;
        }

        /// <summary>The three built-in themes: default space, high contrast, and a colour-vision-deficiency-safe palette.</summary>
        public static List<ThemePalette> CreateDefaultThemes()
        {
            return new List<ThemePalette>
            {
                new ThemePalette { name = "Space (Default)" },
                new ThemePalette
                {
                    name = "High Contrast",
                    panelBackground = new Color(0f, 0f, 0f, 1f),
                    panelText = Color.white,
                    buttonNormal = new Color(1f, 0.86f, 0f, 1f),
                    buttonHighlighted = Color.white,
                    buttonText = Color.black,
                    accentText = new Color(1f, 0.86f, 0f, 1f),
                    courtSurface = new Color(0.08f, 0.08f, 0.08f, 1f),
                    courtLines = new Color(1f, 0.86f, 0f, 1f),
                    sideABalls = new Color(1f, 0.45f, 0f, 1f),
                    sideBBalls = new Color(0f, 0.75f, 1f, 1f),
                    background = Color.black
                },
                new ThemePalette
                {
                    // Okabe-Ito colours: distinguishable for common colour-vision deficiencies.
                    name = "Colour-Blind Friendly",
                    panelBackground = new Color(0.06f, 0.06f, 0.1f, 0.95f),
                    panelText = Color.white,
                    buttonNormal = new Color(0f, 0.447f, 0.698f, 1f),       // blue
                    buttonHighlighted = new Color(0.337f, 0.706f, 0.914f, 1f), // sky blue
                    buttonText = Color.white,
                    accentText = new Color(0.941f, 0.894f, 0.259f, 1f),     // yellow
                    courtSurface = new Color(0.25f, 0.25f, 0.28f, 1f),
                    courtLines = Color.white,
                    sideABalls = new Color(0.902f, 0.624f, 0f, 1f),         // orange
                    sideBBalls = new Color(0f, 0.447f, 0.698f, 1f),         // blue
                    background = new Color(0.01f, 0.01f, 0.03f, 1f)
                }
            };
        }

        public void Apply(int index)
        {
            if (themes.Count == 0) themes = CreateDefaultThemes();
            index = Mathf.Clamp(index, 0, themes.Count - 1);
            Current = themes[index];

            foreach (var graphic in FindObjectsByType<ThemedGraphic>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                graphic.ApplyTheme(Current);

            SetMaterialColor(courtSurfaceMaterial, Current.courtSurface);
            SetMaterialColor(courtLineMaterial, Current.courtLines);
            SetMaterialColor(sideABallMaterial, Current.sideABalls);
            SetMaterialColor(sideBBallMaterial, Current.sideBBalls);

            foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Current.background;
            }
            ThemeApplied?.Invoke(Current);
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null) return;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }
    }
}
