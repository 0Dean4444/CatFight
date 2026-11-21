using UnityEngine;
using UnityEngine.UI;

namespace CatFight.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ShopIconGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Color gold = new Color(1f, 0.79f, 0.28f);
            Color dark = new Color(0.25f, 0.17f, 0.12f);
            Box(vh, 0.13f, 0.08f, 0.74f, 0.61f, dark);
            Box(vh, 0.17f, 0.12f, 0.66f, 0.53f, gold);
            Box(vh, 0.49f, 0.12f, 0.24f, 0.39f, dark);
            Box(vh, 0.24f, 0.30f, 0.18f, 0.21f, dark);
            Box(vh, 0.08f, 0.60f, 0.84f, 0.24f, dark);
            for (int i = 0; i < 5; i++)
                Box(vh, 0.10f + i * 0.16f, 0.62f, 0.14f, 0.19f,
                    i % 2 == 0 ? gold : new Color(1f, 0.91f, 0.61f));
            Box(vh, 0.16f, 0.85f, 0.68f, 0.08f, gold);
        }

        private void Box(VertexHelper vh, float x, float y, float width, float height, Color tint)
        {
            Rect r = rectTransform.rect;
            int n = vh.currentVertCount;
            vh.AddVert(new Vector2(r.xMin + x * r.width, r.yMin + y * r.height), tint, Vector2.zero);
            vh.AddVert(new Vector2(r.xMin + x * r.width, r.yMin + (y + height) * r.height), tint, Vector2.zero);
            vh.AddVert(new Vector2(r.xMin + (x + width) * r.width, r.yMin + (y + height) * r.height), tint, Vector2.zero);
            vh.AddVert(new Vector2(r.xMin + (x + width) * r.width, r.yMin + y * r.height), tint, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2);
            vh.AddTriangle(n, n + 2, n + 3);
        }
    }
}
