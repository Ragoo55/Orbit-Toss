using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OrbitToss
{
    /// <summary>Put on any UI Image / TextMeshPro / Button to have it recoloured by the active <see cref="ThemeManager"/> theme.</summary>
    public class ThemedGraphic : MonoBehaviour
    {
        [SerializeField] private ThemeRole role;

        public ThemeRole Role { get => role; set => role = value; }

        public void ApplyTheme(ThemePalette palette)
        {
            switch (role)
            {
                case ThemeRole.PanelBackground: SetColor(palette.panelBackground); break;
                case ThemeRole.PanelText: SetColor(palette.panelText); break;
                case ThemeRole.ButtonText: SetColor(palette.buttonText); break;
                case ThemeRole.AccentText: SetColor(palette.accentText); break;
                case ThemeRole.ButtonBackground:
                    var button = GetComponent<Button>();
                    if (button != null)
                    {
                        ColorBlock colors = button.colors;
                        colors.normalColor = palette.buttonNormal;
                        colors.highlightedColor = palette.buttonHighlighted;
                        colors.selectedColor = palette.buttonNormal;
                        colors.pressedColor = palette.buttonHighlighted * 0.85f;
                        colors.colorMultiplier = 1f;
                        button.colors = colors;
                    }
                    SetColor(Color.white); // the Button tint supplies the colour
                    break;
            }
        }

        private void SetColor(Color color)
        {
            var text = GetComponent<TMP_Text>();
            if (text != null) { text.color = color; return; }
            var graphic = GetComponent<Graphic>();
            if (graphic != null) graphic.color = color;
        }
    }
}
