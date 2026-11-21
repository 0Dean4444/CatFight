using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CatFight.Visuals;
using CatFight.Managers;

namespace CatFight.Controllers
{
    // 1. Giant Paw Stomp + Imprinted Ground Paw Print Decal
    public class PawStompSkillEffect : MonoBehaviour
    {
        private static Transform skillEffectsRoot;

        public static Transform GetSkillEffectsRoot()
        {
            if (skillEffectsRoot == null)
            {
                GameObject go = new GameObject("CatFight_SkillEffectsRoot");
                skillEffectsRoot = go.transform;
            }
            return skillEffectsRoot;
        }

        public static void ClearAllSkillEffects()
        {
            if (skillEffectsRoot != null)
            {
                Destroy(skillEffectsRoot.gameObject);
                skillEffectsRoot = null;
            }
        }

        public static void Spawn(Vector3 targetPos, float damage, float radius, float decalDuration)
        {
            GameObject go = new GameObject("GiantPawStomp");
            go.transform.SetParent(GetSkillEffectsRoot(), false);
            PawStompSkillEffect comp = go.AddComponent<PawStompSkillEffect>();
            comp.StartCoroutine(comp.StompRoutine(targetPos, damage, radius, decalDuration));
        }

        private IEnumerator StompRoutine(Vector3 targetPos, float damage, float radius, float decalDuration)
        {
            // Target Warning Shadow on ground
            GameObject shadow = new GameObject("PawShadow");
            shadow.transform.SetParent(GetSkillEffectsRoot(), false);
            shadow.transform.position = targetPos;
            SpriteRenderer ssr = shadow.AddComponent<SpriteRenderer>();
            ssr.sprite = RuntimeSpriteBank.GetSingle("Sprites/UI/circle_white", 64f);
            ssr.color = new Color(0f, 0f, 0f, 0.32f);
            ssr.sortingOrder = 12;

            // Giant Descending Cat Paw from the sky
            GameObject paw = new GameObject("DescendingCatPaw");
            paw.transform.SetParent(GetSkillEffectsRoot(), false);
            Vector3 startSkyPos = targetPos + new Vector3(0f, 5.5f, 0f);
            paw.transform.position = startSkyPos;
            paw.transform.localScale = Vector3.one * (radius * 0.95f);
            SpriteRenderer psr = paw.AddComponent<SpriteRenderer>();
            psr.sprite = RuntimeSpriteBank.GetSingle("Sprites/Effects/giant_cat_paw", 100f);
            psr.sortingOrder = 80;

            float fallDuration = 0.35f;
            float t = 0f;
            while (t < fallDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / fallDuration);
                float eased = p * p;
                paw.transform.position = Vector3.Lerp(startSkyPos, targetPos + new Vector3(0f, 0.35f, 0f), eased);
                shadow.transform.localScale = Vector3.one * Mathf.Lerp(0.4f, radius * 2.1f, eased);
                yield return null;
            }

            Destroy(shadow);

            // IMPACT! Squash the paw + Screen Shake + AoE Damage + Stun
            paw.transform.localScale = new Vector3(radius * 1.25f, radius * 0.72f, 1f);
            if (CombatVisualFeedback.Instance != null)
                CombatVisualFeedback.Instance.ShakeCamera(0.24f, 0.28f);

            CombatVisualFeedback.SpawnImpactRing(targetPos, new Color(1f, 0.68f, 0.25f), radius * 1.15f, 0.38f);
            CombatVisualFeedback.SpawnSparkleExplosion(targetPos, new Color(1f, 0.65f, 0.78f), 6, 3.0f, true);
            CombatVisualFeedback.SpawnDamagePopup(targetPos + Vector3.up * 0.5f, "BẸP!!", new Color(1f, 0.62f, 0.2f), 1.35f);

            float rSq = radius * radius;
            for (int i = MouseEnemyController.ActiveMice.Count - 1; i >= 0; i--)
            {
                var m = MouseEnemyController.ActiveMice[i];
                if (m == null || !m.IsAlive) continue;
                if ((m.transform.position - targetPos).sqrMagnitude <= rSq)
                {
                    Vector2 push = (m.transform.position - targetPos).normalized * 1.4f;
                    m.ApplyStun(0.85f);
                    m.ApplySlow(0.45f, 3.0f);
                    m.TakeDamage(damage, true, push, true);
                }
            }

            // Spawn the persistent Paw Print Decal imprinted on the ground!
            SpawnPawPrintDecal(targetPos, radius, decalDuration, damage * 0.15f);

            yield return new WaitForSeconds(0.22f);

            // Lift paw back up and fade out
            float liftTime = 0.25f;
            float lt = 0f;
            while (lt < liftTime)
            {
                lt += Time.deltaTime;
                float lp = lt / liftTime;
                paw.transform.position += Vector3.up * (Time.deltaTime * 8f);
                psr.color = new Color(1f, 1f, 1f, 1f - lp);
                yield return null;
            }

            Destroy(paw);
            Destroy(gameObject);
        }

        private void SpawnPawPrintDecal(Vector3 pos, float radius, float duration, float tickDmg)
        {
            GameObject decalObj = new GameObject("GroundPawPrintDecal");
            decalObj.transform.SetParent(GetSkillEffectsRoot(), false);
            decalObj.transform.position = pos;
            decalObj.transform.localScale = Vector3.one * (radius * 0.95f);
            decalObj.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-15f, 15f));

            SpriteRenderer dsr = decalObj.AddComponent<SpriteRenderer>();
            dsr.sprite = RuntimeSpriteBank.GetSingle("Sprites/Effects/paw_print_decal", 100f);
            dsr.sortingOrder = 5; // Under enemies, above floor

            PawPrintGroundHazard hazard = decalObj.AddComponent<PawPrintGroundHazard>();
            hazard.Initialize(radius * 0.85f, duration, tickDmg);
        }
    }

    public class PawPrintGroundHazard : MonoBehaviour
    {
        private float radius;
        private float duration;
        private float tickDamage;
        private float elapsed;
        private float tickTimer;
        private SpriteRenderer sr;

        public void Initialize(float rad, float dur, float dmg)
        {
            radius = rad;
            duration = dur;
            tickDamage = dmg;
            sr = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
                return;

            elapsed += Time.deltaTime;
            tickTimer += Time.deltaTime;

            if (tickTimer >= 0.5f)
            {
                tickTimer = 0f;
                float rSq = radius * radius;
                for (int i = MouseEnemyController.ActiveMice.Count - 1; i >= 0; i--)
                {
                    var m = MouseEnemyController.ActiveMice[i];
                    if (m == null || !m.IsAlive) continue;
                    if ((m.transform.position - transform.position).sqrMagnitude <= rSq)
                    {
                        m.ApplySlow(0.45f, 0.7f);
                        m.TakeDamage(tickDamage, false);
                    }
                }
            }

            if (elapsed > duration - 1.2f && sr != null)
            {
                float alpha = Mathf.Clamp01((duration - elapsed) / 1.2f);
                sr.color = new Color(1f, 1f, 1f, alpha);
            }

            if (elapsed >= duration)
                Destroy(gameObject);
        }
    }

    // 2. Shelf Knockdown Falling Household Objects (Potted Plant, Mug, Clock, Vase)
    public class FallingItemBomb : MonoBehaviour
    {
        public static void Spawn(Vector3 startPos, Vector3 targetPos, float damage, float blastRadius)
        {
            GameObject go = new GameObject("FallingShelfItem");
            go.transform.SetParent(PawStompSkillEffect.GetSkillEffectsRoot(), false);
            go.transform.position = startPos;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            Sprite[] itemSprites = RuntimeSpriteBank.GetHorizontalSheet("Sprites/Projectiles/thrown_items_sheet", 4, 100f);
            if (itemSprites != null && itemSprites.Length > 0)
                sr.sprite = itemSprites[Random.Range(0, itemSprites.Length)];
            sr.sortingOrder = 65;

            FallingItemBomb bomb = go.AddComponent<FallingItemBomb>();
            bomb.StartCoroutine(bomb.FallArcRoutine(startPos, targetPos, damage, blastRadius));
        }

        private IEnumerator FallArcRoutine(Vector3 startPos, Vector3 targetPos, float damage, float blastRadius)
        {
            float duration = 0.68f;
            float elapsed = 0f;
            float rotSpeed = Random.Range(240f, 380f) * (Random.value > 0.5f ? 1f : -1f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 linear = Vector3.Lerp(startPos, targetPos, t);
                float arcHeight = Mathf.Sin(t * Mathf.PI) * 1.85f;
                transform.position = linear + new Vector3(0f, arcHeight, 0f);
                transform.Rotate(0f, 0f, rotSpeed * Time.deltaTime);
                yield return null;
            }

            // Smash on ground!
            CombatVisualFeedback.SpawnImpactRing(targetPos, new Color(0.55f, 0.85f, 1f), blastRadius, 0.28f);
            CombatVisualFeedback.SpawnSparkleExplosion(targetPos, new Color(0.68f, 0.92f, 1f), 4, 2.4f, false);
            CombatVisualFeedback.SpawnDamagePopup(targetPos + Vector3.up * 0.35f, "CHOANG!", new Color(0.65f, 0.9f, 1f), 1.15f);
            if (CombatVisualFeedback.Instance != null)
                CombatVisualFeedback.Instance.ShakeCamera(0.1f, 0.14f);

            float rSq = blastRadius * blastRadius;
            for (int i = MouseEnemyController.ActiveMice.Count - 1; i >= 0; i--)
            {
                var m = MouseEnemyController.ActiveMice[i];
                if (m == null || !m.IsAlive) continue;
                if ((m.transform.position - targetPos).sqrMagnitude <= rSq)
                {
                    m.ApplyStun(0.4f);
                    m.TakeDamage(damage, false, new Vector2(Random.Range(-0.5f, 0.5f), 2.2f), true);
                }
            }

            Destroy(gameObject);
        }
    }

    // 3. Giant Bouncing Yarn Ball
    public class BouncingYarnBall : MonoBehaviour
    {
        private Vector3 velocity;
        private float damage;
        private float radius;
        private float lifetime;
        private readonly Dictionary<MouseEnemyController, float> hitTimestamps = new Dictionary<MouseEnemyController, float>(16);

        private bool splitOnBounce;

        public static BouncingYarnBall Spawn(Vector3 startPos, Vector2 dir, float damage, float radius, float duration, float maxRange = 3.6f, bool splitOnBounce = false)
        {
            GameObject go = new GameObject("GiantYarnBall");
            go.transform.SetParent(PawStompSkillEffect.GetSkillEffectsRoot(), false);
            go.transform.position = startPos;
            go.transform.localScale = Vector3.one * Mathf.Clamp(radius * 0.65f, 0.72f, 0.96f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = RuntimeSpriteBank.GetSingle("Sprites/Projectiles/yarn_ball", 100f);
            sr.sortingOrder = 55;

            BouncingYarnBall ball = go.AddComponent<BouncingYarnBall>();
            ball.velocity = dir.normalized * 4.2f;
            ball.damage = damage;
            ball.radius = radius;
            ball.lifetime = Mathf.Max(duration, 5.5f);
            ball.splitOnBounce = splitOnBounce;
            return ball;
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
                return;

            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += velocity * Time.deltaTime;
            transform.Rotate(0f, 0f, -300f * Time.deltaTime);

            // Bounce off the true left, right, and top edges of the screen (and defense fence at bottom) using cached bounds
            float camHalfH = GameManager.CamHalfHeight;
            float camHalfW = GameManager.CamHalfWidth;
            float maxX = Mathf.Max(2.1f, camHalfW - 0.35f);
            float topY = camHalfH - 0.32f;
            float bottomY = -2.85f;

            Vector3 p = transform.position;
            bool bounced = p.x < -maxX || p.x > maxX || p.y < bottomY || p.y > topY;
            if (p.x < -maxX) { p.x = -maxX; velocity.x = Mathf.Abs(velocity.x); }
            if (p.x > maxX) { p.x = maxX; velocity.x = -Mathf.Abs(velocity.x); }
            if (p.y > topY) { p.y = topY; velocity.y = -Mathf.Abs(velocity.y); }
            if (p.y < bottomY) { p.y = bottomY; velocity.y = Mathf.Abs(velocity.y); }
            transform.position = p;
            if (bounced && splitOnBounce)
            {
                CombatVisualFeedback.SpawnSparkleExplosion(p, new Color(1f, 0.45f, 0.58f), 5, 2f);
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector2 direction = Quaternion.Euler(0f, 0f, sign * 26f) * velocity.normalized;
                    var fragment = Spawn(p, direction, damage * 0.65f, radius * 0.65f, lifetime);
                    fragment.lifetime = lifetime;
                    fragment.transform.localScale = transform.localScale * 0.66f;
                }
                Destroy(gameObject);
                return;
            }

            float rSq = radius * radius;
            float now = Time.time;
            for (int i = MouseEnemyController.ActiveMice.Count - 1; i >= 0; i--)
            {
                var m = MouseEnemyController.ActiveMice[i];
                if (m == null || !m.IsAlive) continue;
                if ((m.transform.position - p).sqrMagnitude <= rSq)
                {
                    if (!hitTimestamps.TryGetValue(m, out float lastHit) || now - lastHit >= 0.38f)
                    {
                        hitTimestamps[m] = now;
                        m.ApplySlow(0.6f, 1.2f);
                        CombatVisualFeedback.SpawnSparkleExplosion(m.transform.position, new Color(1f, 0.45f, 0.58f), 2, 1.8f, false);
                        m.TakeDamage(damage, false, new Vector2(0f, 1.1f), true);
                    }
                }
            }
        }
    }

    // 4. Catnip Tornado Vortex (Pulls mice inward & deals DoT)
    public class CatnipTornadoZone : MonoBehaviour
    {
        private float damagePerTick;
        private float radius;
        private float duration;
        private float tickTimer;
        private SpriteRenderer sr;

        private bool burstOnExpiry;

        public static void Spawn(Vector3 center, float damagePerTick, float radius, float duration, bool burstOnExpiry = false)
        {
            GameObject go = new GameObject("CatnipTornado");
            go.transform.SetParent(PawStompSkillEffect.GetSkillEffectsRoot(), false);
            go.transform.position = center;
            go.transform.localScale = Vector3.one * (radius * 1.45f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = RuntimeSpriteBank.GetSingle("Sprites/Effects/catnip_vortex", 100f);
            sr.sortingOrder = 18;

            CatnipTornadoZone zone = go.AddComponent<CatnipTornadoZone>();
            zone.damagePerTick = damagePerTick;
            zone.radius = radius;
            zone.duration = duration;
            zone.sr = sr;
            zone.burstOnExpiry = burstOnExpiry;
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
                return;

            duration -= Time.deltaTime;
            transform.Rotate(0f, 0f, 160f * Time.deltaTime);

            float rSq = radius * radius;
            Vector3 center = transform.position;
            for (int i = MouseEnemyController.ActiveMice.Count - 1; i >= 0; i--)
            {
                var m = MouseEnemyController.ActiveMice[i];
                if (m == null || !m.IsAlive) continue;
                if ((m.transform.position - center).sqrMagnitude <= rSq)
                {
                    m.PullTowards(center, 1.15f);
                }
            }

            tickTimer += Time.deltaTime;
            if (tickTimer >= 0.45f)
            {
                tickTimer = 0f;
                for (int i = MouseEnemyController.ActiveMice.Count - 1; i >= 0; i--)
                {
                    var m = MouseEnemyController.ActiveMice[i];
                    if (m == null || !m.IsAlive) continue;
                    if ((m.transform.position - center).sqrMagnitude <= rSq)
                    {
                        m.TakeDamage(damagePerTick, false);
                    }
                }
            }

            if (duration <= 0.5f && sr != null)
            {
                sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(duration / 0.5f));
            }

            if (duration <= 0f)
            {
                if (burstOnExpiry)
                {
                    float burstRadius = radius * 1.35f;
                    CombatVisualFeedback.SpawnImpactRing(center, new Color(0.45f, 1f, 0.67f), burstRadius, 0.42f);
                    CombatVisualFeedback.SpawnSparkleExplosion(center, new Color(0.65f, 1f, 0.8f), 8, 2.8f);
                    foreach (var mouse in MouseEnemyController.GetEnemiesInRange(center, burstRadius))
                    {
                        Vector2 push = (mouse.transform.position - center).normalized;
                        mouse.TakeDamage(damagePerTick * 1.5f, true, push * 3f + Vector2.up);
                    }
                }
                Destroy(gameObject);
            }
        }
    }
}
