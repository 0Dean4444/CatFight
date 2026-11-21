using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CatFight.Data;
using CatFight.Visuals;
using CatFight.Managers;

namespace CatFight.Controllers
{
    public class CatCompanionController : MonoBehaviour
    {
        public CatCompanionDefinition definition;
        public float cooldownRemaining;
        public bool autoCast = true;

        private SpriteSheetAnimator animator;
        private float expressionTimer;
        private LineRenderer laserLine;
        private static Material sharedLaserMaterial;

        public int EffectiveLevel =>
            definition == null ? 1 : (definition.permanentLevel + definition.battleStageLevel - 1);

        public float CooldownRatio =>
            definition == null || CurrentMaxCooldown <= 0.01f
                ? 0f
                : Mathf.Clamp01(cooldownRemaining / CurrentMaxCooldown);

        public float CurrentMaxCooldown =>
            definition == null ? 5f : Mathf.Max(1.8f, definition.baseCooldown * Mathf.Pow(0.94f, EffectiveLevel - 1));

        public float CurrentDamage =>
            definition == null
                ? 25f
                : definition.baseDamage
                  * (1f + 0.30f * (EffectiveLevel - 1))
                  * Mathf.Pow(1.10f, definition.permanentLevel - 1)
                  * definition.GetSkinDamageMultiplier();

        public float CurrentRadius =>
            definition == null ? 1.2f : definition.effectRadius * (1f + 0.08f * (definition.battleStageLevel - 1));

        public float CurrentAttackRange =>
            definition == null ? 3.8f : definition.attackRange * (1f + 0.03f * (EffectiveLevel - 1));

        public void Setup(CatCompanionDefinition def)
        {
            definition = def;
            cooldownRemaining = Random.Range(0.4f, 1.5f);

            SpriteRenderer sr = gameObject.GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 38;

            animator = gameObject.GetComponent<SpriteSheetAnimator>();
            if (animator == null) animator = gameObject.AddComponent<SpriteSheetAnimator>();

            Sprite[] frames = RuntimeSpriteBank.GetHorizontalSheet(def.sheetResourcePath, 4, 100f);
            animator.Initialize(frames, 5.2f, 0.66f);
            animator.SetBaseColor(def.GetActiveSkinTint());
            animator.ShowExpression(CharacterSpriteBank.CatFace(def.sheetResourcePath, CatExpression.Surprised), 1.2f);
            expressionTimer = Random.Range(3f, 6f);
        }

        public void RefreshSkinVisual()
        {
            if (animator != null && definition != null)
                animator.SetBaseColor(definition.GetActiveSkinTint());
        }

        public bool HasEnemyInAttackRange()
        {
            return MouseEnemyController.GetClosestEnemy(transform.position, CurrentAttackRange) != null;
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || definition == null)
                return;

            expressionTimer -= Time.deltaTime;
            if (expressionTimer <= 0f)
            {
                animator.ShowExpression(CharacterSpriteBank.CatFace(definition.sheetResourcePath,
                    HasEnemyInAttackRange() ? CatExpression.Determined : CatExpression.Happy), 1.4f);
                expressionTimer = Random.Range(4f, 7f);
            }

            if (cooldownRemaining > 0f)
                cooldownRemaining -= Time.deltaTime;

            if (cooldownRemaining <= 0f && HasEnemyInAttackRange())
            {
                TryTriggerSkill();
            }
        }

        public bool TryTriggerSkill()
        {
            if (cooldownRemaining > 0.05f || definition == null)
                return false;

            // Strictly require at least 1 enemy inside this Cat's Attack Range before casting
            if (!HasEnemyInAttackRange())
                return false;

            cooldownRemaining = CurrentMaxCooldown;
            if (animator != null)
            {
                animator.TriggerActionPunch(0.36f);
                animator.ShowExpression(CharacterSpriteBank.CatFace(definition.sheetResourcePath, CatExpression.Determined), 1.3f);
                expressionTimer = 3f;
            }
            if (CatFightAudioManager.Instance != null)
                CatFightAudioManager.Instance.PlaySkill();

            CombatVisualFeedback.SpawnSparkleExplosion(transform.position + Vector3.up * 0.2f, definition.themeColor, 6, 1.9f, true);
            CombatVisualFeedback.SpawnDamagePopup(transform.position + Vector3.up * 0.55f, definition.displayName + "!", definition.themeColor, 0.8f);

            switch (definition.skillType)
            {
                case CatSkillType.GiantPawStomp:
                    CastGiantPawStomp();
                    break;
                case CatSkillType.ShelfKnockdown:
                    StartCoroutine(CastShelfKnockdownRoutine());
                    break;
                case CatSkillType.GiantYarnBall:
                    CastGiantYarnBall();
                    break;
                case CatSkillType.NinjaClawFlurry:
                    StartCoroutine(CastNinjaClawFlurryRoutine());
                    break;
                case CatSkillType.CatnipTornado:
                    CastCatnipTornado();
                    break;
                case CatSkillType.RedDotLaser:
                    StartCoroutine(CastRedDotLaserRoutine());
                    break;
                case CatSkillType.RoyalFishBarrage:
                    StartCoroutine(CastRoyalFishBarrageRoutine());
                    break;
            }
            return true;
        }

        // 1. Mèo Mập: Gọi bàn chân mèo khổng lồ dậm xuống & in hình dấu chân (trong tầm đánh)
        private void CastGiantPawStomp()
        {
            Vector3 target = MouseEnemyController.GetDensestClusterPositionInRange(transform.position, CurrentAttackRange);
            PawStompSkillEffect.Spawn(target, CurrentDamage, CurrentRadius, definition.effectDuration);
            if (definition.IsAwakened) StartCoroutine(EchoStomp(target));
        }

        private IEnumerator EchoStomp(Vector3 target)
        {
            yield return new WaitForSeconds(0.52f);
            if (GameManager.Instance.IsPlaying)
                PawStompSkillEffect.Spawn(target, CurrentDamage * 0.65f, CurrentRadius * 1.15f, definition.effectDuration * 0.5f);
        }

        // 2. Mèo Nghịch Ngợm: Hất đồ rơi xuống mục tiêu trong tầm đánh
        private IEnumerator CastShelfKnockdownRoutine()
        {
            bool awakened = definition.IsAwakened;
            int count = 3 + (definition.battleStageLevel - 1);
            for (int i = 0; i < count; i++)
            {
                List<MouseEnemyController> inRange = MouseEnemyController.GetEnemiesInRange(transform.position, CurrentAttackRange);
                if (inRange.Count == 0)
                {
                    var closest = MouseEnemyController.GetClosestEnemy(transform.position, CurrentAttackRange);
                    if (closest != null) inRange.Add(closest);
                }
                if (inRange.Count == 0) break;

                Vector3 target = inRange[Random.Range(0, inRange.Count)].transform.position;

                // Items knocked from high shelves on left/right sides of room
                Vector3 shelfOrigin = new Vector3(Random.value > 0.5f ? -2.75f : 2.75f, Mathf.Min(3.4f, target.y + 1.2f), 0f);
                FallingItemBomb.Spawn(shelfOrigin, target, CurrentDamage, CurrentRadius);
                if (awakened && i == count - 1)
                {
                    shelfOrigin.x = -shelfOrigin.x;
                    FallingItemBomb.Spawn(shelfOrigin, target, CurrentDamage * 0.8f, CurrentRadius);
                }
                yield return new WaitForSeconds(0.24f);
            }
        }

        // 3. Mèo Len Đỏ: Cuộn len khổng lồ nảy qua lại trong tầm đánh
        private void CastGiantYarnBall()
        {
            int balls = definition.battleStageLevel >= 3 ? 2 : 1;
            var closest = MouseEnemyController.GetClosestEnemy(transform.position, CurrentAttackRange);
            for (int i = 0; i < balls; i++)
            {
                Vector2 dir;
                if (closest != null && i == 0)
                {
                    dir = (closest.transform.position - transform.position).normalized;
                    if (dir.y < 0.35f) dir.y = 0.55f;
                    dir.Normalize();
                }
                else
                {
                    dir = new Vector2(Random.Range(-0.85f, 0.85f), 1f).normalized;
                    if (Mathf.Abs(dir.x) < 0.35f) dir.x = i % 2 == 0 ? 0.55f : -0.55f;
                }
                BouncingYarnBall.Spawn(transform.position + Vector3.up * 0.4f, dir, CurrentDamage, CurrentRadius, definition.effectDuration, CurrentAttackRange, definition.IsAwakened);
            }
        }

        // 4. Mèo Ninja: Lao vút chém liên hoàn vết cào vào mục tiêu trong tầm đánh
        private IEnumerator CastNinjaClawFlurryRoutine()
        {
            bool awakened = definition.IsAwakened;
            int slashes = 3 + (definition.battleStageLevel - 1);
            for (int i = 0; i < slashes; i++)
            {
                List<MouseEnemyController> inRange = MouseEnemyController.GetEnemiesInRange(transform.position, CurrentAttackRange);
                if (inRange.Count == 0) break;

                var target = inRange[Random.Range(0, inRange.Count)];
                if (target == null || !target.IsAlive) continue;

                Vector3 hitPos = target.transform.position;
                bool finisher = awakened && i == slashes - 1;
                float angle = Random.Range(-35f, 35f);
                SpawnSlashVisual(hitPos, angle);
                if (finisher)
                {
                    SpawnSlashVisual(hitPos, angle + 90f);
                    CombatVisualFeedback.SpawnImpactRing(hitPos, definition.themeColor, CurrentRadius, 0.3f);
                }

                float rSq = CurrentRadius * CurrentRadius;
                for (int j = MouseEnemyController.ActiveMice.Count - 1; j >= 0; j--)
                {
                    var m = MouseEnemyController.ActiveMice[j];
                    if (m == null || !m.IsAlive) continue;
                    if ((m.transform.position - hitPos).sqrMagnitude <= rSq)
                    {
                        if (finisher) m.ApplyStun(1.2f);
                        m.TakeDamage(CurrentDamage, true, new Vector2(0f, 0.8f));
                    }
                }
                yield return new WaitForSeconds(0.20f);
            }
        }

        private void SpawnSlashVisual(Vector3 pos, float angle)
        {
            GameObject slashObj = new GameObject("NinjaSlash");
            slashObj.transform.SetParent(PawStompSkillEffect.GetSkillEffectsRoot(), false);
            slashObj.transform.position = pos;
            slashObj.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            slashObj.transform.localScale = Vector3.one * 1.15f;

            SpriteRenderer sr = slashObj.AddComponent<SpriteRenderer>();
            sr.sprite = RuntimeSpriteBank.GetSingle("Sprites/Effects/claw_slash", 100f);
            sr.sortingOrder = 72;
            Destroy(slashObj, 0.22f);
        }

        // 5. Mèo Phù Thủy: Lốc xoáy cỏ mèo hút gom chuột trong tầm đánh & gây DoT
        private void CastCatnipTornado()
        {
            Vector3 center = MouseEnemyController.GetDensestClusterPositionInRange(transform.position, CurrentAttackRange);
            CatnipTornadoZone.Spawn(center, CurrentDamage, CurrentRadius, definition.effectDuration, definition.IsAwakened);
        }

        // 6. Mèo Bác Học: Tia Laser chấm đỏ quét chạm tận rìa màn hình (trái, phải và cạnh trên màn hình)
        private IEnumerator CastRedDotLaserRoutine()
        {
            if (laserLine == null)
            {
                if (sharedLaserMaterial == null)
                    sharedLaserMaterial = new Material(Shader.Find("Sprites/Default"));

                GameObject lineObj = new GameObject("RedDotLaserLine");
                lineObj.transform.SetParent(transform, false);
                laserLine = lineObj.AddComponent<LineRenderer>();
                laserLine.positionCount = 2;
                laserLine.startWidth = 0.16f;
                laserLine.endWidth = 0.22f;
                laserLine.sharedMaterial = sharedLaserMaterial;
                laserLine.sortingOrder = 75;
            }

            laserLine.enabled = true;
            laserLine.startColor = new Color(1f, 0.15f, 0.25f, 0.95f);
            laserLine.endColor = new Color(1f, 0.65f, 0.2f, 0.95f);
            bool awakened = definition.IsAwakened;
            const float forwardDuration = 1.55f;
            float returnDuration = Mathf.Min(0.8f, CurrentMaxCooldown - forwardDuration - 0.05f);
            float sweepDuration = awakened ? forwardDuration + returnDuration : forwardDuration;
            float elapsed = 0f;
            float tickTimer = 0f;

            while (elapsed < sweepDuration)
            {
                elapsed += Time.deltaTime;
                bool returning = awakened && elapsed > forwardDuration;
                float t = returning ? 1f - Mathf.Clamp01((elapsed - forwardDuration) / returnDuration)
                    : Mathf.Clamp01(elapsed / forwardDuration);
                if (returning)
                {
                    laserLine.startColor = Color.cyan;
                    laserLine.endColor = Color.white;
                }

                float camHalfH = GameManager.CamHalfHeight;
                float camHalfW = GameManager.CamHalfWidth;

                Vector3 origin = transform.position + Vector3.up * 0.3f;
                // Sweep angle from left screen edge across top screen edge to right screen edge (158 deg -> 22 deg)
                float angleDeg = Mathf.Lerp(158f, 22f, t);
                float rad = angleDeg * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

                // Intersect ray with actual left, right, and top edges of the camera screen
                float distToEdge = 20f;
                if (Mathf.Abs(dir.x) > 0.001f)
                {
                    float targetX = dir.x > 0f ? camHalfW : -camHalfW;
                    float dx = (targetX - origin.x) / dir.x;
                    if (dx > 0f) distToEdge = Mathf.Min(distToEdge, dx);
                }
                if (dir.y > 0.001f)
                {
                    float dy = (camHalfH - origin.y) / dir.y;
                    if (dy > 0f) distToEdge = Mathf.Min(distToEdge, dy);
                }

                Vector3 endPt = origin + (Vector3)(dir * distToEdge);

                laserLine.SetPosition(0, origin);
                laserLine.SetPosition(1, endPt);

                tickTimer += Time.deltaTime;
                if (tickTimer >= 0.18f)
                {
                    tickTimer = 0f;
                    CombatVisualFeedback.SpawnImpactRing(endPt, new Color(1f, 0.2f, 0.3f), 0.75f, 0.15f);
                    float beamHalfWidth = Mathf.Max(0.48f, CurrentRadius * 0.42f);
                    float beamRadiusSq = beamHalfWidth * beamHalfWidth;
                    Vector2 seg = endPt - origin;
                    float segLenSq = Mathf.Max(0.001f, seg.sqrMagnitude);

                    for (int i = MouseEnemyController.ActiveMice.Count - 1; i >= 0; i--)
                    {
                        var m = MouseEnemyController.ActiveMice[i];
                        if (m == null || !m.IsAlive) continue;
                        // Damage any mouse touched along the full laser beam from Cat to screen edge!
                        Vector2 toMouse = (Vector2)(m.transform.position - origin);
                        float proj = Mathf.Clamp01(Vector2.Dot(toMouse, seg) / segLenSq);
                        Vector2 closestPt = (Vector2)origin + seg * proj;
                        if (((Vector2)m.transform.position - closestPt).sqrMagnitude <= beamRadiusSq)
                        {
                            m.TakeDamage(CurrentDamage * 0.42f, true);
                        }
                    }
                }
                yield return null;
            }

            laserLine.enabled = false;
        }

        // 7. Mèo Hoàng Thượng: Mưa xương cá vàng nhắm vào mục tiêu trong tầm đánh + Hồi máu tường thành
        private IEnumerator CastRoyalFishBarrageRoutine()
        {
            if (GameManager.Instance != null)
            {
                float healAmt = 35f + 15f * EffectiveLevel;
                GameManager.Instance.HealWall(healAmt);
                CombatVisualFeedback.SpawnDamagePopup(transform.position + Vector3.up * 0.3f, $"+{Mathf.RoundToInt(healAmt)} HP TƯỜNG", new Color(0.35f, 1f, 0.55f), 1.0f);
            }

            bool awakened = definition.IsAwakened;
            int count = 5 + definition.battleStageLevel;
            for (int i = 0; i < count; i++)
            {
                Vector3 spawnOrigin = transform.position + Vector3.up * 0.4f;
                List<MouseEnemyController> inRange = MouseEnemyController.GetEnemiesInRange(transform.position, CurrentAttackRange);
                Vector3 dir;
                if (inRange.Count > 0)
                {
                    var picked = inRange[Random.Range(0, inRange.Count)];
                    dir = (picked.transform.position - spawnOrigin).normalized;
                    if (dir.y < 0.2f) dir = new Vector3(dir.x, 0.35f, 0f).normalized;
                }
                else
                {
                    dir = new Vector3(Random.Range(-0.45f, 0.45f), 1f, 0f).normalized;
                }

                HeroBulletBehaviour.Spawn(
                    spawnOrigin,
                    dir,
                    6.5f,
                    CurrentDamage,
                    1,
                    "Sprites/Projectiles/fishbone_missile",
                    0.95f,
                    0f
                );
                yield return new WaitForSeconds(0.14f);
            }

            if (awakened && GameManager.Instance.IsPlaying)
            {
                Vector3 origin = transform.position + Vector3.up * 0.4f;
                var target = MouseEnemyController.GetClosestEnemy(origin, CurrentAttackRange);
                Vector3 aim = target != null ? (target.transform.position - origin).normalized : Vector3.up;
                CombatVisualFeedback.SpawnHitStarburst(origin, definition.themeColor, 0.9f);
                for (int i = -2; i <= 2; i++)
                {
                    Vector3 dir = Quaternion.Euler(0f, 0f, 16f * i) * aim;
                    HeroBulletBehaviour.Spawn(origin, dir, 7f, CurrentDamage * 0.8f, 3,
                        "Sprites/Projectiles/fishbone_missile", 1.1f);
                }
            }
        }
    }
}
