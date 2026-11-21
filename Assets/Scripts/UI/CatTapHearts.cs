using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatFight.UI
{
    public sealed class CatTapHearts : MaskableGraphic
    {
        private const float Lifetime = 0.95f;
        private const int Segments = 32;
        private static readonly Vector2[] Outline = CreateOutline();
        private readonly List<(Vector2 origin, float time)> bursts = new List<(Vector2, float)>(8);

        public void ShowAt(Vector2 screenPoint)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, null, out Vector2 local))
                return;
            gameObject.SetActive(true);
            if (bursts.Count == 8) bursts.RemoveAt(0);
            bursts.Add((local + Vector2.up * 22f, Time.unscaledTime));
            SetVerticesDirty();
        }

        private void Update()
        {
            if (bursts.Count == 0) return;
            while (bursts.Count > 0 && Time.unscaledTime - bursts[0].time >= Lifetime)
                bursts.RemoveAt(0);
            if (bursts.Count == 0) gameObject.SetActive(false);
            else SetVerticesDirty();
        }

        protected override void OnDisable()
        {
            bursts.Clear();
            base.OnDisable();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            foreach (var burst in bursts)
            {
                float age = (Time.unscaledTime - burst.time) / Lifetime;
                for (int i = 0; i < 3; i++)
                {
                    float t = (age - i * 0.08f) / (1f - i * 0.08f);
                    if (t < 0f || t >= 1f) continue;
                    float side = i == 0 ? 0f : (i == 1 ? -1f : 1f);
                    Vector2 offset = new Vector2(side * (14f + 55f * t),
                        Mathf.Lerp(0f, i == 0 ? 195f : 147f, t));
                    float size = (i == 0 ? 26f : 19f) * Mathf.Lerp(0.65f, 1f, Mathf.Min(1f, t * 7f));
                    float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, t));
                    Vector2 center = burst.origin + offset;
                    Heart(vh, center, size, new Color(0.63f, 0.11f, 0.29f, alpha));
                    Heart(vh, center + Vector2.up * 1.5f, size * 0.83f, new Color(1f, 0.44f + i * 0.08f, 0.65f, alpha));
                }
            }
        }

        private static Vector2[] CreateOutline()
        {
            var points = new Vector2[Segments];
            for (int i = 0; i < Segments; i++)
            {
                float a = i * Mathf.PI * 2f / Segments;
                float s = Mathf.Sin(a);
                points[i] = new Vector2(s * s * s, (13f * Mathf.Cos(a) - 5f * Mathf.Cos(2f * a)
                    - 2f * Mathf.Cos(3f * a) - Mathf.Cos(4f * a)) / 16f);
            }
            return points;
        }

        private static void Heart(VertexHelper vh, Vector2 center, float size, Color tint)
        {
            int first = vh.currentVertCount;
            vh.AddVert(center, tint, Vector2.zero);
            foreach (var point in Outline) vh.AddVert(center + point * size, tint, Vector2.zero);
            for (int i = 0; i < Segments; i++)
                vh.AddTriangle(first, first + 1 + i, first + 1 + (i + 1) % Segments);
        }
    }
}
