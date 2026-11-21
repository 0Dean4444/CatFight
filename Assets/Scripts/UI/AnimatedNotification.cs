using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatFight.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class AnimatedNotification : MonoBehaviour
    {
        private CanvasGroup group;
        private RectTransform rect;
        private Text label;
        private Coroutine animationRoutine;
        private readonly Queue<(string message, Color color, float duration)> queue =
            new Queue<(string, Color, float)>();

        public void Initialize(Text messageLabel)
        {
            group = GetComponent<CanvasGroup>();
            rect = GetComponent<RectTransform>();
            label = messageLabel;
            group.blocksRaycasts = false;
            group.interactable = false;
            gameObject.SetActive(false);
        }

        public void Show(string message, Color color, float duration = 0f)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            if (duration <= 0f) duration = Mathf.Clamp(message.Length * 0.045f, 1.65f, 4f);
            queue.Enqueue((message, color, duration));
            if (animationRoutine != null) return;
            gameObject.SetActive(true);
            animationRoutine = StartCoroutine(PlayQueue());
        }

        private IEnumerator PlayQueue()
        {
            while (queue.Count > 0)
            {
                var item = queue.Dequeue();
                label.text = item.message;
                label.color = item.color;
                ResizeToMessage();
                yield return Animate(item.duration);
            }
            animationRoutine = null;
            gameObject.SetActive(false);
        }

        private void ResizeToMessage()
        {
            float width = Mathf.Min(920f, ((RectTransform)rect.parent).rect.width - 64f);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(1f, width));
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Ceil(label.preferredHeight) + 12f);
        }

        private IEnumerator Animate(float duration)
        {
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = elapsed / duration;
                rect.anchoredPosition = Vector2.up * Mathf.Lerp(0f, 196f, t);
                group.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.28f, 1f, t));
                yield return null;
            }
            group.alpha = 0f;
        }

        private void OnDisable()
        {
            queue.Clear();
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = null;
            if (group != null) group.alpha = 0f;
        }
    }
}
