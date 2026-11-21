using UnityEngine;
using UnityEngine.UI;

namespace CatFight.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CatDockGraphic : MaskableGraphic
    {
        public bool selectedTile;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect bounds = rectTransform.rect;
            if (selectedTile)
            {
                Rounded(vh, bounds, 28f, new Color32(245, 186, 103, 255), new Color32(255, 223, 157, 255));
                return;
            }

            // Leave room for a short shadow inside the safe area.
            Rounded(vh, bounds, 42f, new Color32(22, 23, 38, 110), new Color32(22, 23, 38, 110));
            bounds.yMin += 6f;
            Rounded(vh, bounds, 41f, new Color32(38, 38, 55, 255), new Color32(95, 94, 116, 255));
            bounds.xMin += 2.5f;
            bounds.xMax -= 2.5f;
            bounds.yMin += 2.5f;
            bounds.yMax -= 2.5f;
            Rounded(vh, bounds, 39f, new Color32(36, 38, 58, 255), new Color32(51, 53, 77, 255));
        }

        private static void Rounded(VertexHelper vh, Rect rect, float radius, Color bottom, Color top)
        {
            const int steps = 10;
            radius = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) * 0.5f);
            int first = vh.currentVertCount;
            vh.AddVert(rect.center, Color.Lerp(bottom, top, 0.5f), Vector2.zero);
            for (int corner = 0; corner < 4; corner++)
            {
                float x = corner == 0 || corner == 3 ? rect.xMax - radius : rect.xMin + radius;
                float y = corner < 2 ? rect.yMax - radius : rect.yMin + radius;
                for (int step = 0; step <= steps; step++)
                {
                    float angle = (corner + step / (float)steps) * Mathf.PI * 0.5f;
                    Vector2 p = new Vector2(x + radius * Mathf.Cos(angle), y + radius * Mathf.Sin(angle));
                    vh.AddVert(p, Color.Lerp(bottom, top, Mathf.InverseLerp(rect.yMin, rect.yMax, p.y)), Vector2.zero);
                }
            }
            const int rim = 4 * (steps + 1);
            for (int i = 0; i < rim; i++)
                vh.AddTriangle(first, first + 1 + i, first + 1 + (i + 1) % rim);
        }
    }
}
