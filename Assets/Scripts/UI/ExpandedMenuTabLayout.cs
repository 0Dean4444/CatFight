using UnityEngine;
using UnityEngine.UI;

namespace CatFight.UI
{
    // Keeps touch targets at their designed proportions while allowing tall pages to scroll.
    [RequireComponent(typeof(ScrollRect))]
    public sealed class ExpandedMenuTabLayout : MonoBehaviour
    {
        public const float TextScale = 1.16f;
        private ScrollRect scroll;
        private float designWidth;
        private Vector2 lastViewportSize;

        public void Initialize(ScrollRect scrollRect, float width)
        {
            scroll = scrollRect;
            designWidth = width;
        }

        private void LateUpdate()
        {
            if (scroll == null || scroll.content == null) return;
            Vector2 viewportSize = scroll.viewport.rect.size;
            if (viewportSize.x <= 0f || viewportSize.y <= 0f) return;

            float scale = viewportSize.x / designWidth;
            RectTransform content = scroll.content;
            float height = Mathf.Max(viewportSize.y / scale, LayoutUtility.GetPreferredHeight(content));
            Vector2 size = new Vector2(designWidth, height);
            if (content.sizeDelta == size && lastViewportSize == viewportSize) return;

            float position = scroll.verticalNormalizedPosition;
            content.localScale = Vector3.one * scale;
            content.sizeDelta = size;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            scroll.verticalNormalizedPosition = lastViewportSize == Vector2.zero ? 1f : position;
            lastViewportSize = viewportSize;
        }
    }
}
