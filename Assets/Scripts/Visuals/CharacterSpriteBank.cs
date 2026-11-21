using System.Collections.Generic;
using UnityEngine;

namespace CatFight.Visuals
{
    public enum CatExpression { Happy, Sleepy, Surprised, Determined }

    public static class CharacterSpriteBank
    {
        private static readonly Dictionary<string, Sprite[]> Cache = new Dictionary<string, Sprite[]>();
        private static readonly string[] CatIds = { "stomp", "thrower", "yarn", "ninja", "mage", "laser", "royal" };
        private static readonly string[] BossNames = { "Chuột Bếp Trưởng", "Chuột Chúa Gai", "Chuột Vua Cống" };

        public static string BossName(int chapter) => BossNames[Mathf.Clamp(chapter - 1, 0, 2)];

        public static Sprite CatFace(string sheetPath, CatExpression expression)
        {
            for (int row = 0; row < CatIds.Length; row++)
            {
                if (!sheetPath.EndsWith("cat_" + CatIds[row] + "_sheet")) continue;
                Sprite[] sprites = AtlasRow("Sprites/Cats/cat_expressions", row, 7, 1.28f);
                return sprites.Length == 4 ? sprites[(int)expression] : null;
            }
            return null;
        }

        public static Sprite[] BossFrames(int chapter) => AtlasRow("Sprites/Mice/chapter_bosses", Mathf.Clamp(chapter - 1, 0, 2), 3, 1.6f);

        private static Sprite[] AtlasRow(string path, int row, int rows, float worldWidth)
        {
            string key = path + row;
            if (Cache.TryGetValue(key, out Sprite[] cached)) return cached;
            Texture2D texture = Resources.Load<Texture2D>(path);
            if (texture == null) return new Sprite[0];
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            float width = texture.width / 4f;
            float height = texture.height / (float)rows;
            Sprite[] sprites = new Sprite[4];
            for (int column = 0; column < 4; column++)
                sprites[column] = Sprite.Create(texture, new Rect(column * width, texture.height - (row + 1) * height, width, height), new Vector2(0.5f, 0.5f), width / worldWidth);
            Cache[key] = sprites;
            return sprites;
        }
    }
}
