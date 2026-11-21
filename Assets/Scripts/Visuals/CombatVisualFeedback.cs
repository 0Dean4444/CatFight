using System.Collections.Generic;
using UnityEngine;

namespace CatFight.Visuals
{
    public class CombatVisualFeedback : MonoBehaviour
    {
        public static CombatVisualFeedback Instance { get; private set; }

        private Camera mainCam;
        private Vector3 camBasePos = new Vector3(0f, 0f, -10f);
        private float shakeTimer;
        private float shakeIntensity;

        // Object Pools & Active Caps for Low-End Mobile Optimization
        private static Transform vfxPoolRoot;
        private static readonly Stack<FloatingPopupBehaviour> PopupPool = new Stack<FloatingPopupBehaviour>(24);
        private static readonly Stack<ExpandingRingBehaviour> RingPool = new Stack<ExpandingRingBehaviour>(32);
        private static readonly Stack<SparkleParticleBehaviour> ParticlePool = new Stack<SparkleParticleBehaviour>(48);

        public static int ActivePopupCount;
        public static int ActiveRingCount;
        public static int ActiveParticleCount;
        private static float lastMinorPopupTime;

        private static Transform GetPoolRoot()
        {
            if (vfxPoolRoot == null)
            {
                GameObject go = new GameObject("CatFight_VFXPool");
                vfxPoolRoot = go.transform;
            }
            return vfxPoolRoot;
        }

        public static void ClearAllActiveVFX()
        {
            PopupPool.Clear();
            RingPool.Clear();
            ParticlePool.Clear();
            ActivePopupCount = 0;
            ActiveRingCount = 0;
            ActiveParticleCount = 0;
            if (vfxPoolRoot != null)
            {
                Destroy(vfxPoolRoot.gameObject);
                vfxPoolRoot = null;
            }
        }

        private void Awake()
        {
            Instance = this;
            mainCam = Camera.main;
            if (mainCam != null)
                camBasePos = mainCam.transform.position;
        }

        public void ShakeCamera(float intensity = 0.15f, float duration = 0.18f)
        {
            shakeIntensity = Mathf.Max(shakeIntensity, intensity);
            shakeTimer = Mathf.Max(shakeTimer, duration);
        }

        private void LateUpdate()
        {
            if (shakeTimer <= 0f)
                return;

            if (mainCam == null)
            {
                mainCam = Camera.main;
                if (mainCam != null) camBasePos = mainCam.transform.position;
                return;
            }

            shakeTimer -= Time.unscaledDeltaTime;
            if (shakeTimer > 0f)
            {
                Vector2 offset = Random.insideUnitCircle * shakeIntensity;
                mainCam.transform.position = camBasePos + new Vector3(offset.x, offset.y, 0f);
            }
            else
            {
                shakeIntensity = 0f;
                mainCam.transform.position = camBasePos;
            }
        }

        public static void SpawnDamagePopup(Vector3 worldPos, string text, Color color, float scale = 1.0f)
        {
            bool isImportant = scale >= 1.05f;
            if (!isImportant)
            {
                if (ActivePopupCount >= 10 || Time.unscaledTime - lastMinorPopupTime < 0.055f)
                    return;
                lastMinorPopupTime = Time.unscaledTime;
            }
            else if (ActivePopupCount >= 18)
            {
                return;
            }

            FloatingPopupBehaviour beh = null;
            while (PopupPool.Count > 0 && beh == null)
                beh = PopupPool.Pop();

            if (beh == null)
            {
                GameObject go = new GameObject("PopupText");
                go.transform.SetParent(GetPoolRoot(), false);
                TextMesh tm = go.AddComponent<TextMesh>();
                tm.fontSize = 28; // Smaller dynamic font size for 3x faster mobile glyph rasterization
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.fontStyle = FontStyle.Bold;

                MeshRenderer mr = go.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sortingOrder = 150;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                }

                beh = go.AddComponent<FloatingPopupBehaviour>();
                beh.CacheComponents(tm);
            }

            beh.transform.position = worldPos + new Vector3(Random.Range(-0.20f, 0.20f), Random.Range(0.1f, 0.28f), -1f);
            beh.gameObject.SetActive(true);
            beh.Initialize(text, color, 0.162f * scale);
        }

        internal static void ReturnPopupToPool(FloatingPopupBehaviour beh)
        {
            if (beh == null) return;
            beh.gameObject.SetActive(false);
            PopupPool.Push(beh);
        }

        private static ExpandingRingBehaviour GetPooledRing()
        {
            ExpandingRingBehaviour ring = null;
            while (RingPool.Count > 0 && ring == null)
                ring = RingPool.Pop();

            if (ring == null)
            {
                GameObject go = new GameObject("PooledRingFX");
                go.transform.SetParent(GetPoolRoot(), false);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                ring = go.AddComponent<ExpandingRingBehaviour>();
                ring.CacheRenderer(sr);
            }
            return ring;
        }

        internal static void ReturnRingToPool(ExpandingRingBehaviour ring)
        {
            if (ring == null) return;
            ring.gameObject.SetActive(false);
            RingPool.Push(ring);
        }

        public static void SpawnImpactRing(Vector3 worldPos, Color color, float maxRadius = 1.8f, float duration = 0.32f)
        {
            if (ActiveRingCount >= 16) return;

            ExpandingRingBehaviour ring = GetPooledRing();
            ring.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
            ring.transform.rotation = Quaternion.identity;
            ring.gameObject.SetActive(true);
            ring.Initialize(RuntimeSpriteBank.GetSingle("Sprites/UI/circle_white", 64f), 45, maxRadius, duration, new Color(color.r, color.g, color.b, 0.55f));

            if (maxRadius >= 0.8f && ActiveRingCount < 14)
                SpawnHitStarburst(worldPos, color, maxRadius * 0.55f);
        }

        public static void SpawnHitStarburst(Vector3 worldPos, Color color, float scale = 0.65f)
        {
            if (ActiveRingCount >= 18) return;

            ExpandingRingBehaviour b = GetPooledRing();
            b.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
            b.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            b.gameObject.SetActive(true);
            b.Initialize(RuntimeSpriteBank.GetSingle("Sprites/Effects/fx_hit_burst", 100f), 68, scale, 0.16f, color);
        }

        public static void SpawnMuzzleFlash(Vector3 worldPos, Vector3 dir, Color color)
        {
            if (ActiveRingCount >= 14) return;

            ExpandingRingBehaviour b = GetPooledRing();
            b.transform.position = worldPos;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            b.transform.rotation = Quaternion.Euler(0f, 0f, angle + Random.Range(-15f, 15f));
            b.gameObject.SetActive(true);
            b.Initialize(RuntimeSpriteBank.GetSingle("Sprites/Effects/fx_star_sparkle", 100f), 70, 0.42f, 0.10f, color);
        }

        public static void SpawnSparkleExplosion(Vector3 worldPos, Color color, int count = 7, float speed = 2.6f, bool usePawSprite = false)
        {
            if (ActiveParticleCount >= 26) return;

            // Clamp particle count per burst to keep fill-rate & CPU smooth on low-end phones
            int cappedCount = Mathf.Min(count, 4);
            string path = usePawSprite ? "Sprites/Effects/fx_paw_particle" : "Sprites/Effects/fx_star_sparkle";
            Sprite sp = RuntimeSpriteBank.GetSingle(path, 100f);

            for (int i = 0; i < cappedCount; i++)
            {
                if (ActiveParticleCount >= 28) break;

                SparkleParticleBehaviour beh = null;
                while (ParticlePool.Count > 0 && beh == null)
                    beh = ParticlePool.Pop();

                if (beh == null)
                {
                    GameObject p = new GameObject("VFXParticle");
                    p.transform.SetParent(GetPoolRoot(), false);
                    SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = 74;
                    beh = p.AddComponent<SparkleParticleBehaviour>();
                    beh.CacheRenderer(sr);
                }

                beh.transform.position = worldPos;
                beh.gameObject.SetActive(true);

                float ang = (i * (360f / Mathf.Max(1, cappedCount)) + Random.Range(-22f, 22f)) * Mathf.Deg2Rad;
                Vector3 vel = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * (speed * Random.Range(0.65f, 1.25f));
                beh.Initialize(sp, vel, Random.Range(0.24f, 0.38f), Random.Range(0.32f, 0.52f), color);
            }
        }

        internal static void ReturnParticleToPool(SparkleParticleBehaviour beh)
        {
            if (beh == null) return;
            beh.gameObject.SetActive(false);
            ParticlePool.Push(beh);
        }
    }

    public class SparkleParticleBehaviour : MonoBehaviour
    {
        private Vector3 velocity;
        private float duration;
        private float elapsed;
        private float startScale;
        private Color baseColor;
        private SpriteRenderer sr;
        private float rotSpeed;
        private bool isRegistered;

        public void CacheRenderer(SpriteRenderer renderer)
        {
            sr = renderer;
        }

        public void Initialize(Sprite sprite, Vector3 vel, float dur, float scale, Color col)
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = sprite;
                sr.color = col;
            }
            velocity = vel;
            duration = Mathf.Max(0.1f, dur);
            elapsed = 0f;
            startScale = scale;
            baseColor = col;
            transform.localScale = Vector3.one * startScale;
            rotSpeed = Random.Range(-300f, 300f);

            if (!isRegistered)
            {
                isRegistered = true;
                CombatVisualFeedback.ActiveParticleCount++;
            }
        }

        private void OnDisable()
        {
            if (isRegistered)
            {
                isRegistered = false;
                CombatVisualFeedback.ActiveParticleCount = Mathf.Max(0, CombatVisualFeedback.ActiveParticleCount - 1);
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= duration)
            {
                CombatVisualFeedback.ReturnParticleToPool(this);
                return;
            }

            float t = elapsed / duration;
            transform.position += velocity * Time.deltaTime;
            velocity *= (1f - Time.deltaTime * 3.5f);
            transform.Rotate(0f, 0f, rotSpeed * Time.deltaTime);
            float sc = Mathf.Lerp(startScale, 0.06f, t);
            transform.localScale = new Vector3(sc, sc, 1f);

            if (sr != null)
            {
                Color c = baseColor;
                c.a = 1f - t;
                sr.color = c;
            }
        }
    }

    public class FloatingPopupBehaviour : MonoBehaviour
    {
        private float elapsed;
        private const float Lifetime = 0.62f;
        private TextMesh tm;
        private Color baseColor;
        private Vector3 velocity;
        private bool isRegistered;

        public void CacheComponents(TextMesh textMesh)
        {
            tm = textMesh;
        }

        public void Initialize(string text, Color col, float charSize)
        {
            if (tm == null) tm = GetComponent<TextMesh>();
            if (tm != null)
            {
                tm.text = text;
                tm.characterSize = charSize;
                tm.color = col;
            }
            elapsed = 0f;
            baseColor = col;
            velocity = new Vector3(Random.Range(-0.30f, 0.30f), 1.50f, 0f);

            if (!isRegistered)
            {
                isRegistered = true;
                CombatVisualFeedback.ActivePopupCount++;
            }
        }

        private void OnDisable()
        {
            if (isRegistered)
            {
                isRegistered = false;
                CombatVisualFeedback.ActivePopupCount = Mathf.Max(0, CombatVisualFeedback.ActivePopupCount - 1);
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= Lifetime)
            {
                CombatVisualFeedback.ReturnPopupToPool(this);
                return;
            }

            transform.position += velocity * Time.deltaTime;
            velocity.y = Mathf.Max(0.4f, velocity.y - Time.deltaTime * 2.1f);

            float t = elapsed / Lifetime;
            if (tm != null && t > 0.35f)
            {
                Color c = baseColor;
                c.a = Mathf.Clamp01(1f - t * t);
                tm.color = c;
            }
        }
    }

    public class ExpandingRingBehaviour : MonoBehaviour
    {
        private float maxRadius;
        private float duration;
        private float elapsed;
        private SpriteRenderer sr;
        private Color baseColor;
        private bool isRegistered;

        public void CacheRenderer(SpriteRenderer renderer)
        {
            sr = renderer;
        }

        public void Initialize(Sprite sprite, int sortingOrder, float radius, float dur, Color col)
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = sprite;
                sr.sortingOrder = sortingOrder;
                sr.color = col;
            }
            maxRadius = radius;
            duration = Mathf.Max(0.08f, dur);
            elapsed = 0f;
            baseColor = col;
            transform.localScale = new Vector3(0.2f, 0.2f, 1f);

            if (!isRegistered)
            {
                isRegistered = true;
                CombatVisualFeedback.ActiveRingCount++;
            }
        }

        private void OnDisable()
        {
            if (isRegistered)
            {
                isRegistered = false;
                CombatVisualFeedback.ActiveRingCount = Mathf.Max(0, CombatVisualFeedback.ActiveRingCount - 1);
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= duration)
            {
                CombatVisualFeedback.ReturnRingToPool(this);
                return;
            }

            float t = elapsed / duration;
            float inv = 1f - t;
            float scale = Mathf.Lerp(0.2f, maxRadius * 2f, 1f - inv * inv * inv);
            transform.localScale = new Vector3(scale, scale, 1f);

            if (sr != null)
            {
                Color c = baseColor;
                c.a = Mathf.Lerp(baseColor.a, 0f, t);
                sr.color = c;
            }
        }
    }
}
