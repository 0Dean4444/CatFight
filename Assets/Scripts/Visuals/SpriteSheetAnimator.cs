using System.Collections.Generic;
using UnityEngine;

namespace CatFight.Visuals
{
    public static class RuntimeSpriteBank
    {
        private static readonly Dictionary<string, Sprite[]> SheetCache = new Dictionary<string, Sprite[]>();
        private static readonly Dictionary<string, Sprite> SingleCache = new Dictionary<string, Sprite>();

        public static Sprite GetSingle(string resourcePath, float pixelsPerUnit = 100f)
        {
            if (SingleCache.TryGetValue(resourcePath, out Sprite cached) && cached != null)
                return cached;

            Texture2D tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null)
            {
                Debug.LogWarning($"[RuntimeSpriteBank] Missing texture at Resources/{resourcePath}");
                return null;
            }
            tex.wrapMode = TextureWrapMode.Clamp;

            Sprite sp = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit
            );
            sp.name = tex.name;
            SingleCache[resourcePath] = sp;
            return sp;
        }

        public static Sprite[] GetHorizontalSheet(string resourcePath, int frameCount, float pixelsPerUnit = 100f)
        {
            string key = $"{resourcePath}_{frameCount}";
            if (SheetCache.TryGetValue(key, out Sprite[] cached) && cached != null && cached.Length > 0 && cached[0] != null)
                return cached;

            Texture2D tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null)
            {
                Debug.LogWarning($"[RuntimeSpriteBank] Missing sheet texture at Resources/{resourcePath}");
                return new Sprite[0];
            }
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            int frameWidth = tex.width / Mathf.Max(1, frameCount);
            int frameHeight = tex.height;
            Sprite[] frames = new Sprite[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                Rect rect = new Rect(i * frameWidth, 0, frameWidth, frameHeight);
                frames[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
                frames[i].name = $"{tex.name}_{i}";
            }

            SheetCache[key] = frames;
            return frames;
        }
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteSheetAnimator : MonoBehaviour
    {
        public SpriteRenderer spriteRenderer;
        public Sprite[] frames;
        public float fps = 8f;
        public bool loop = true;
        public bool enableSquashStretch = true;
        public float squashAmount = 0.045f;
        public float squashSpeed = 4.2f;

        private Sprite expressionSprite;
        private float expressionTime;
        private float timer;
        private int currentFrame;
        private Vector3 baseScale = Vector3.one;
        private float punchTimer;
        private float flashTimer;
        private bool isColorFlashed;
        private Color baseColor = Color.white;
        private float timeOffset;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            baseScale = transform.localScale;
            timeOffset = Random.Range(0f, 10f);
        }

        public void Initialize(Sprite[] animationFrames, float frameRate = 8f, float scale = 1f)
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            expressionSprite = null;
            expressionTime = 0f;
            frames = animationFrames;
            fps = frameRate;
            baseScale = Vector3.one * scale;
            transform.localScale = baseScale;
            punchTimer = 0f;
            flashTimer = 0f;
            isColorFlashed = false;
            spriteRenderer.color = baseColor;
            currentFrame = Random.Range(0, frames != null && frames.Length > 0 ? frames.Length : 1);
            if (frames != null && frames.Length > 0)
                spriteRenderer.sprite = frames[currentFrame];
        }

        public void ShowExpression(Sprite sprite, float duration)
        {
            if (sprite == null) return;
            expressionSprite = sprite;
            expressionTime = duration;
            spriteRenderer.sprite = sprite;
        }

        public void SetBaseScale(float scale)
        {
            baseScale = Vector3.one * scale;
            transform.localScale = baseScale;
        }

        public void SetBaseColor(Color color)
        {
            baseColor = color;
            if (spriteRenderer != null && !isColorFlashed)
                spriteRenderer.color = baseColor;
        }

        public void TriggerActionPunch(float intensity = 0.28f)
        {
            punchTimer = intensity;
            if (frames != null && frames.Length >= 4)
            {
                currentFrame = 3;
                spriteRenderer.sprite = frames[currentFrame];
            }
        }

        public void TriggerHitFlash()
        {
            flashTimer = 0.12f;
            if (!isColorFlashed && spriteRenderer != null)
            {
                isColorFlashed = true;
                spriteRenderer.color = new Color(1f, 0.45f, 0.45f, 1f);
            }
        }

        private void Update()
        {
            if (spriteRenderer == null) return;

            bool frameAdvanced = false;
            if (frames != null && frames.Length > 1)
            {
                timer += Time.deltaTime;
                float interval = 1f / Mathf.Max(1f, fps);
                if (timer >= interval)
                {
                    timer -= interval;
                    currentFrame++;
                    if (currentFrame >= frames.Length)
                        currentFrame = loop ? 0 : frames.Length - 1;
                    spriteRenderer.sprite = frames[currentFrame];
                    frameAdvanced = true;
                }
            }

            if (expressionTime > 0f)
            {
                expressionTime -= Time.deltaTime;
                spriteRenderer.sprite = expressionTime > 0f ? expressionSprite : frames[currentFrame];
            }

            // Only dirty transform.localScale when animation frame advances or during an active action punch!
            if (punchTimer > 0f || (enableSquashStretch && frameAdvanced))
            {
                Vector3 targetScale = baseScale;
                if (enableSquashStretch)
                {
                    float wave = Mathf.Sin((Time.time + timeOffset) * squashSpeed);
                    targetScale.x = baseScale.x * (1f - wave * squashAmount);
                    targetScale.y = baseScale.y * (1f + wave * squashAmount);
                }

                if (punchTimer > 0f)
                {
                    punchTimer -= Time.deltaTime * 2.2f;
                    if (punchTimer > 0f)
                    {
                        targetScale.x *= (1f + punchTimer * 0.6f);
                        targetScale.y *= (1f + punchTimer * 0.9f);
                    }
                    else
                    {
                        punchTimer = 0f;
                    }
                }

                transform.localScale = targetScale;
            }

            // Only update spriteRenderer.color when flashTimer expires (avoids dirtying vertex color buffer every frame!)
            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0f && isColorFlashed)
                {
                    isColorFlashed = false;
                    spriteRenderer.color = baseColor;
                }
            }
        }
    }
}
