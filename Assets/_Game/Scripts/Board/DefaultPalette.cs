using UnityEngine;

namespace Cast.Game.Board
{

    public static class DefaultPalette
    {
        public static readonly Color[] Colors =
        {
            new Color32(0xF2, 0xC1, 0x4E, 0xFF), 
            new Color32(0xE8, 0x9B, 0xB0, 0xFF), 
            new Color32(0x8E, 0xCA, 0xE6, 0xFF), 
            new Color32(0x95, 0xD5, 0xB2, 0xFF), 
            new Color32(0xC8, 0xB6, 0xFF, 0xFF), 
            new Color32(0xFF, 0xB7, 0x8A, 0xFF), 
            new Color32(0x90, 0xBE, 0x6D, 0xFF), 
            new Color32(0xF4, 0x97, 0x9C, 0xFF), 
            new Color32(0x7C, 0xC5, 0xC9, 0xFF), 
            new Color32(0xB5, 0x9D, 0xA4, 0xFF), 
        };

        public static Color Resolve(Color[] levelColors, int colorIndex)
        {
            if (levelColors != null && colorIndex >= 0 && colorIndex < levelColors.Length)
                return levelColors[colorIndex];
            if (colorIndex >= 0 && colorIndex < Colors.Length)
                return Colors[colorIndex];
            return Color.gray;
        }
    }
}
