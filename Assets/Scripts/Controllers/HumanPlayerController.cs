using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CatFight.Visuals;
using CatFight.Managers;

namespace CatFight.Controllers
{
    public class HeroBulletBehaviour : MonoBehaviour
    {
        private static readonly Stack<HeroBulletBehaviour> BulletPool = new Stack<HeroBulletBehaviour>(48);
        private static Transform bulletPoolRoot;

        private Vector3 velocity;
        private float damage;
        private int pierceCount;
        private float lifetime = 6.0f;
        private SpriteRenderer sr;

        private static Transform GetPoolRoot()
        {
            if (bulletPoolRoot == null)
            {
                GameObject go = new GameObject("CatFight_BulletPool");
                bulletPoolRoot = go.transform;
            }
            return bulletPoolRoot;
        }

        public static void Spawn(Vector3 startPos, Vector3 dir, float speed, float dmg, int pierce = 0, string spritePath = "Sprites/Projectiles/hero_bullet", float scale = 0.72f, float maxRange = 0f)
        {
            HeroBulletBehaviour b = null;
            while (BulletPool.Count > 0 && b == null)
                b = BulletPool.Pop();

            if (b == null)
            {
                GameObject go = new GameObject("HeroBullet");
                go.transform.SetParent(GetPoolRoot(), false);
                SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 52;
                b = go.AddComponent<HeroBulletBehaviour>();
                b.sr = renderer;
            }

            b.transform.position = startPos;
            b.transform.localScale = new Vector3(scale, scale, 1f);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            b.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            if (b.sr != null)
                b.sr.sprite = RuntimeSpriteBank.GetSingle(spritePath, 100f);

            b.velocity = dir.normalized * speed;
            b.damage = dmg;
            b.pierceCount = pierce;
            b.lifetime = 6.0f;
            b.gameObject.SetActive(true);
        }

        private void Despawn()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
            BulletPool.Push(this);
        }

        public static void ClearAllBullets()
        {
            BulletPool.Clear();
            if (bulletPoolRoot != null)
            {
                Destroy(bulletPoolRoot.gameObject);
                bulletPoolRoot = null;
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
                return;

            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
            {
                Despawn();
                return;
            }

            Vector3 p = transform.position + velocity * Time.deltaTime;
            transform.position = p;

            float halfH = GameManager.CamHalfHeight;
            float halfW = GameManager.CamHalfWidth;
            if (p.y > halfH + 0.65f || p.y < -halfH - 0.65f || p.x < -halfW - 0.65f || p.x > halfW + 0.65f)
            {
                Despawn();
                return;
            }

            MouseEnemyController hit = MouseEnemyController.GetClosestEnemy(p, 0.45f);
            if (hit != null && hit.IsAlive)
            {
                bool isCrit = Random.value < 0.18f;
                float finalDmg = isCrit ? damage * 1.75f : damage;
                hit.TakeDamage(finalDmg, isCrit, new Vector2(0f, 0.45f));

                if (isCrit)
                {
                    CombatVisualFeedback.SpawnImpactRing(p, new Color(1f, 0.9f, 0.35f), 0.38f, 0.15f);
                    CombatVisualFeedback.SpawnSparkleExplosion(p, new Color(1f, 0.65f, 0.2f), 3, 2.0f, false);
                }
                else
                {
                    CombatVisualFeedback.SpawnHitStarburst(p, new Color(1f, 0.92f, 0.42f), 0.42f);
                }

                if (CatFightAudioManager.Instance != null)
                    CatFightAudioManager.Instance.PlayHit();

                if (pierceCount > 0)
                {
                    pierceCount--;
                    damage *= 0.8f;
                }
                else
                {
                    Despawn();
                }
            }
        }
    }

    public class HumanPlayerController : MonoBehaviour
    {
        public float baseDamage = 20f;
        public float fireRate = 2.4f; // Shots per second (Balanced Gatling tempo)
        public int multishot = 1;     // Number of barrels/projectiles per volley
        public int pierceCount = 0;
        public float bulletSpeed = 7.5f;

        private SpriteSheetAnimator bodyAnimator;
        private Transform gunTransform;
        private SpriteRenderer gunRenderer;
        private float fireTimer;
        private Vector3 currentAimWorldPos = new Vector3(0f, 2f, 0f);

        public void Initialize()
        {
            SpriteRenderer sr = gameObject.GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 40;

            bodyAnimator = gameObject.GetComponent<SpriteSheetAnimator>();
            if (bodyAnimator == null) bodyAnimator = gameObject.AddComponent<SpriteSheetAnimator>();

            Sprite[] frames = RuntimeSpriteBank.GetHorizontalSheet("Sprites/Characters/human_hero_sheet", 4, 100f);
            bodyAnimator.Initialize(frames, 5.2f, 0.72f);

            // Create Rotating Gatling Gun child
            GameObject gunObj = new GameObject("GatlingLauncher");
            gunObj.transform.SetParent(transform, false);
            gunObj.transform.localPosition = new Vector3(0f, 0.14f, 0f);
            gunObj.transform.localScale = Vector3.one * 0.85f;
            gunRenderer = gunObj.AddComponent<SpriteRenderer>();
            gunRenderer.sprite = RuntimeSpriteBank.GetSingle("Sprites/Characters/human_gun", 100f);
            gunRenderer.sortingOrder = 42;
            gunTransform = gunObj.transform;
        }

        public void ApplyVisualTints(Color bodyTint, Color gunTint)
        {
            if (bodyAnimator != null)
                bodyAnimator.SetBaseColor(bodyTint);
            if (gunRenderer != null)
                gunRenderer.color = gunTint;
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
                return;

            UpdateAiming();

            fireTimer -= Time.deltaTime;
            if (fireTimer <= 0f)
            {
                bool hasTargetInSight = MouseEnemyController.GetClosestEnemy(transform.position, 6.2f) != null;
                if (hasTargetInSight || IsPointerHeldInArena())
                {
                    fireTimer = 1f / Mathf.Max(0.5f, fireRate);
                    FireVolley();
                }
            }
        }

        private bool IsPointerHeldInArena()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsDraggingCat)
                return false;

            if (Pointer.current != null && Pointer.current.press.isPressed)
            {
                Vector2 screenPos = Pointer.current.position.ReadValue();
                // Ignore bottom HP bar strip (bottom 7% of screen) and top header (top 14%)
                if (screenPos.y > Screen.height * 0.07f && screenPos.y < Screen.height * 0.86f)
                    return true;
            }
            return false;
        }

        private void UpdateAiming()
        {
            Camera cam = GameManager.MainCam;
            if (IsPointerHeldInArena() && cam != null)
            {
                Vector2 screenPos = Pointer.current.position.ReadValue();
                Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
                wp.z = 0f;
                if (wp.y > transform.position.y + 0.35f)
                    currentAimWorldPos = wp;
            }
            else
            {
                MouseEnemyController target = MouseEnemyController.GetClosestEnemy(transform.position, 15f);
                if (target != null)
                    currentAimWorldPos = Vector3.Lerp(currentAimWorldPos, target.transform.position, Time.deltaTime * 14f);
                else
                    currentAimWorldPos = Vector3.Lerp(currentAimWorldPos, transform.position + Vector3.up * 3f, Time.deltaTime * 5f);
            }

            if (gunTransform != null)
            {
                Vector3 dir = (currentAimWorldPos - transform.position).normalized;
                if (dir.y < 0.15f) dir = new Vector3(dir.x, 0.2f, 0f).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                angle = Mathf.Clamp(angle, -62f, 62f);
                gunTransform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private void FireVolley()
        {
            Vector3 baseDir = (currentAimWorldPos - transform.position).normalized;
            if (baseDir.y < 0.15f) baseDir = Vector3.up;

            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
            float spreadStep = 8.5f;
            float startOffset = -(multishot - 1) * 0.5f * spreadStep;

            Vector3 muzzlePos = transform.position + baseDir * 0.45f;
            CombatVisualFeedback.SpawnMuzzleFlash(muzzlePos, baseDir, new Color(1f, 0.92f, 0.45f));

            for (int i = 0; i < multishot; i++)
            {
                float a = (baseAngle + startOffset + i * spreadStep) * Mathf.Deg2Rad;
                Vector3 shotDir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                HeroBulletBehaviour.Spawn(muzzlePos, shotDir, bulletSpeed, baseDamage, pierceCount, "Sprites/Projectiles/hero_bullet", 0.42f);
            }

            if (bodyAnimator != null)
                bodyAnimator.TriggerActionPunch(0.11f);
            if (CatFightAudioManager.Instance != null)
                CatFightAudioManager.Instance.PlayShoot();
        }
    }
}
