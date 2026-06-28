using UnityEngine;

namespace Cast.Game
{

    public static class PlaceholderSprites
    {
        private const int Tex = 64;
        private const float PixelsPerUnit = 64f;

        private static Sprite _square;
        private static Sprite _circle;

        public static Sprite Square => _square != null ? _square : (_square = BuildSquare());
        public static Sprite Circle => _circle != null ? _circle : (_circle = BuildCircle());

        private static Sprite BuildSquare()
        {
            var tex = new Texture2D(Tex, Tex, TextureFormat.RGBA32, false);
            var px = new Color32[Tex * Tex];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            return ToSprite(tex);
        }

        private static Sprite BuildCircle()
        {
            var tex = new Texture2D(Tex, Tex, TextureFormat.RGBA32, false);
            var px = new Color32[Tex * Tex];
            float c = (Tex - 1) * 0.5f;
            float rr = c * c;
            for (int y = 0; y < Tex; y++)
                for (int x = 0; x < Tex; x++)
                {
                    float dx = x - c, dy = y - c;
                    bool inside = dx * dx + dy * dy <= rr;
                    px[y * Tex + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            tex.SetPixels32(px);
            tex.Apply();
            return ToSprite(tex);
        }

        private static Sprite ToSprite(Texture2D tex)
        {
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, Tex, Tex), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }
    }
}
