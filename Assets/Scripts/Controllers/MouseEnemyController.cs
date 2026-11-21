using System.Collections.Generic;
using UnityEngine;
using CatFight.Data;
using CatFight.Visuals;
using CatFight.Managers;

namespace CatFight.Controllers
{
    public class MouseEnemyController : MonoBehaviour
    {
        public static readonly List<MouseEnemyController> ActiveMice = new List<MouseEnemyController>(32);
        private static readonly Stack<MouseEnemyController> EnemyPool = new Stack<MouseEnemyController>(32);
        private static readonly List<MouseEnemyController> SharedRangeResult = new List<MouseEnemyController>(32);

        public MouseEnemyDefinition definition;
        public float currentHp;
        public float maxHp;
        public float moveSpeed;
        public float attackDamage;

        private SpriteSheetAnimator animator;
        private Transform hpBarFill;
        private SpriteRenderer hpBarFillSr;
        private Transform hpBarRoot;

        private float slowTimer;
        private float slowMultiplier = 1f;
        private float stunTimer;
        private float attackCooldown;
        private Vector3 knockbackVelocity;
        private float wobbleSeed;
        private bool isInPool;

        public bool IsAlive => currentHp > 0f;

        public static MouseEnemyController SpawnFromPool(Transform parent, Vector3 pos, MouseEnemyDefinition def, float hpMultiplier, float speedMultiplier)
        {
            MouseEnemyController ctrl = null;
            while (EnemyPool.Count > 0 && ctrl == null)
                ctrl = EnemyPool.Pop();

            if (ctrl == null)
            {
                GameObject mObj = new GameObject($"Mouse_{def.type}");
                mObj.transform.SetParent(parent, false);
                ctrl = mObj.AddComponent<MouseEnemyController>();
            }
            else
            {
                if (ctrl.transform.parent != parent)
                    ctrl.transform.SetParent(parent, false);
            }

            ctrl.isInPool = false;
            ctrl.transform.position = pos;
            ctrl.Setup(def, hpMultiplier, speedMultiplier);
            ctrl.gameObject.SetActive(true);
            if (!ActiveMice.Contains(ctrl))
                ActiveMice.Add(ctrl);
            return ctrl;
        }

        public void DespawnToPool()
        {
            currentHp = 0f;
            ActiveMice.Remove(this);
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
            if (!isInPool)
            {
                isInPool = true;
                EnemyPool.Push(this);
            }
        }

        public static void DestroyAllAndClearPool()
        {
            for (int i = ActiveMice.Count - 1; i >= 0; i--)
            {
                var m = ActiveMice[i];
                if (m != null && m.gameObject != null)
                    Destroy(m.gameObject);
            }
            ActiveMice.Clear();

            while (EnemyPool.Count > 0)
            {
                var m = EnemyPool.Pop();
                if (m != null && m.gameObject != null)
                    Destroy(m.gameObject);
            }
            EnemyPool.Clear();
            SharedRangeResult.Clear();
        }

        private void OnEnable()
        {
            if (IsAlive && !isInPool && !ActiveMice.Contains(this))
                ActiveMice.Add(this);
        }

        private void OnDisable()
        {
            ActiveMice.Remove(this);
        }

        private void OnDestroy()
        {
            ActiveMice.Remove(this);
        }

        public void Setup(MouseEnemyDefinition def, float hpMultiplier, float speedMultiplier)
        {
            definition = def;
            maxHp = def.maxHp * hpMultiplier;
            currentHp = maxHp;
            moveSpeed = def.moveSpeed * speedMultiplier;
            attackDamage = def.attackDamage * Mathf.Pow(Mathf.Max(0.6f, hpMultiplier), 0.65f);
            wobbleSeed = Random.Range(0f, 100f);
            slowTimer = 0f;
            slowMultiplier = 1f;
            stunTimer = 0f;
            attackCooldown = 0f;
            knockbackVelocity = Vector3.zero;

            SpriteRenderer sr = gameObject.GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 25;

            if (animator == null)
            {
                animator = gameObject.GetComponent<SpriteSheetAnimator>();
                if (animator == null) animator = gameObject.AddComponent<SpriteSheetAnimator>();
            }

            Sprite[] frames = def.type == MouseEnemyType.Boss
                ? CharacterSpriteBank.BossFrames(GameManager.Instance != null ? GameManager.Instance.currentChapter : 1)
                : RuntimeSpriteBank.GetHorizontalSheet(def.sheetResourcePath, 4, 100f);
            if (frames.Length == 0)
                frames = RuntimeSpriteBank.GetHorizontalSheet(def.sheetResourcePath, 4, 100f);
            gameObject.name = def.type == MouseEnemyType.Boss
                ? CharacterSpriteBank.BossName(GameManager.Instance != null ? GameManager.Instance.currentChapter : 1)
                : "Mouse_" + def.type;
            animator.Initialize(frames, 5.2f, def.scale);

            EnsureWorldHealthBar();
        }

        private void EnsureWorldHealthBar()
        {
            if (hpBarRoot == null)
            {
                hpBarRoot = new GameObject("HPBar").transform;
                hpBarRoot.SetParent(transform, false);
                hpBarRoot.localPosition = new Vector3(0f, 0.72f, 0f);

                Sprite pixelSp = RuntimeSpriteBank.GetSingle("Sprites/UI/ui_white_pixel", 32f);

                GameObject bg = new GameObject("BG");
                bg.transform.SetParent(hpBarRoot, false);
                bg.transform.localScale = new Vector3(0.96f, 0.14f, 1f);
                SpriteRenderer bgsr = bg.AddComponent<SpriteRenderer>();
                bgsr.sprite = pixelSp;
                bgsr.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);
                bgsr.sortingOrder = 35;

                GameObject fill = new GameObject("Fill");
                fill.transform.SetParent(hpBarRoot, false);
                hpBarFillSr = fill.AddComponent<SpriteRenderer>();
                hpBarFillSr.sprite = pixelSp;
                hpBarFillSr.sortingOrder = 36;
                hpBarFill = fill.transform;
            }

            if (hpBarFill != null)
            {
                hpBarFill.localScale = new Vector3(0.90f, 0.09f, 1f);
                hpBarFill.localPosition = Vector3.zero;
            }
            if (hpBarFillSr != null)
            {
                hpBarFillSr.color = definition.type == MouseEnemyType.Boss
                    ? new Color(1f, 0.22f, 0.38f, 1f)
                    : new Color(0.28f, 0.92f, 0.45f, 1f);
            }
        }

        private void Update()
        {
            if (!IsAlive)
            {
                DespawnToPool();
                return;
            }
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
                return;

            // Update status timers
            if (slowTimer > 0f)
            {
                slowTimer -= Time.deltaTime;
                if (slowTimer <= 0f) slowMultiplier = 1f;
            }

            if (stunTimer > 0f)
            {
                stunTimer -= Time.deltaTime;
            }

            // Apply knockback decay
            if (knockbackVelocity.sqrMagnitude > 0.01f)
            {
                transform.position += knockbackVelocity * Time.deltaTime;
                knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.deltaTime * 8f);
            }

            float wallY = GameManager.DefenseLineY;
            if (transform.position.y > wallY)
            {
                if (stunTimer <= 0f)
                {
                    float effectiveSpeed = moveSpeed * slowMultiplier;
                    float sway = Mathf.Sin((Time.time + wobbleSeed) * 2.4f) * 0.14f;
                    Vector3 pos = transform.position;
                    pos.y -= effectiveSpeed * Time.deltaTime;
                    pos.x = Mathf.Clamp(pos.x + sway * Time.deltaTime, -2.35f, 2.35f);
                    transform.position = pos;
                }
            }
            else
            {
                // Reached the defense wall!
                if (definition.type == MouseEnemyType.Bomber)
                {
                    // Bomber explodes on contact with wall!
                    CombatVisualFeedback.SpawnImpactRing(transform.position, new Color(1f, 0.45f, 0.15f), 1.6f, 0.3f);
                    CombatVisualFeedback.SpawnDamagePopup(transform.position, "BÙM!", new Color(1f, 0.35f, 0.1f), 1.3f);
                    GameManager.Instance.DamageWall(attackDamage);
                    Die(false);
                    return;
                }

                attackCooldown -= Time.deltaTime;
                if (attackCooldown <= 0f && stunTimer <= 0f)
                {
                    attackCooldown = 1.65f;
                    animator.TriggerActionPunch(0.25f);
                    GameManager.Instance.DamageWall(attackDamage);
                    CombatVisualFeedback.SpawnDamagePopup(transform.position + Vector3.down * 0.3f, $"-{Mathf.RoundToInt(attackDamage)}", new Color(1f, 0.3f, 0.3f), 0.9f);
                }
            }
        }

        public void TakeDamage(float dmg, bool isCritical = false, Vector2 knockback = default, bool canKnockbackBoss = false)
        {
            if (!IsAlive) return;

            currentHp -= dmg;
            if (animator != null)
                animator.TriggerHitFlash();

            if (knockback.sqrMagnitude > 0.001f)
            {
                bool isBoss = definition != null && definition.type == MouseEnemyType.Boss;
                // Boss is ONLY knocked back by specific heavy skills (canKnockbackBoss == true)
                if (!isBoss || canKnockbackBoss)
                {
                    float kbScale = isBoss ? 0.55f : 1f;
                    knockbackVelocity = new Vector3(knockback.x * kbScale, Mathf.Max(0f, knockback.y * kbScale), 0f);
                }
            }

            if (hpBarFill != null)
            {
                float ratio = Mathf.Clamp01(currentHp / Mathf.Max(1f, maxHp));
                hpBarFill.localScale = new Vector3(0.9f * ratio, 0.1f, 1f);
                hpBarFill.localPosition = new Vector3(-0.45f * (1f - ratio), 0f, 0f);
            }

            Color popupCol = isCritical ? new Color(1f, 0.88f, 0.2f) : Color.white;
            string label = isCritical ? $"{Mathf.RoundToInt(dmg)}!" : $"{Mathf.RoundToInt(dmg)}";
            CombatVisualFeedback.SpawnDamagePopup(transform.position, label, popupCol, isCritical ? 1.25f : 0.95f);

            if (currentHp <= 0f)
            {
                Die(true);
            }
        }

        public void ApplySlow(float factor, float duration)
        {
            slowMultiplier = Mathf.Min(slowMultiplier, Mathf.Clamp(factor, 0.2f, 1f));
            slowTimer = Mathf.Max(slowTimer, duration);
        }

        public void ApplyStun(float duration)
        {
            bool isBoss = definition != null && definition.type == MouseEnemyType.Boss;
            float effectiveStun = isBoss ? duration * 0.5f : duration;
            stunTimer = Mathf.Max(stunTimer, effectiveStun);
        }

        public void PullTowards(Vector3 center, float pullSpeed, bool canAffectBoss = false)
        {
            if (!IsAlive) return;
            if (definition != null && definition.type == MouseEnemyType.Boss && !canAffectBoss)
                return;

            Vector3 next = Vector3.MoveTowards(transform.position, center, pullSpeed * Time.deltaTime);
            next.x = Mathf.Clamp(next.x, -2.4f, 2.4f);
            next.y = Mathf.Clamp(next.y, GameManager.DefenseLineY + 0.2f, 4.5f);
            transform.position = next;
        }

        private void Die(bool giveRewards)
        {
            currentHp = 0f;
            ActiveMice.Remove(this);

            if (giveRewards && GameManager.Instance != null)
            {
                GameManager.Instance.OnEnemyKilled(definition);
                CombatVisualFeedback.SpawnImpactRing(transform.position, new Color(1f, 0.9f, 0.55f), 0.65f, 0.20f);
                CombatVisualFeedback.SpawnSparkleExplosion(transform.position, new Color(1f, 0.85f, 0.35f), 3, 2.0f, false);
            }

            if (definition != null && definition.type == MouseEnemyType.Boss)
            {
                if (!IsAnyBossAlive() && CatFightAudioManager.Instance != null)
                    CatFightAudioManager.Instance.PlayNormalChillBgm();
            }

            DespawnToPool();
        }

        public static bool IsAnyBossAlive()
        {
            for (int i = ActiveMice.Count - 1; i >= 0; i--)
            {
                var m = ActiveMice[i];
                if (m != null && m.IsAlive && m.definition != null && m.definition.type == MouseEnemyType.Boss)
                    return true;
            }
            return false;
        }

        public static MouseEnemyController GetClosestEnemy(Vector3 fromPos, float maxRange = 99f)
        {
            MouseEnemyController best = null;
            float bestDist = maxRange * maxRange;
            for (int i = ActiveMice.Count - 1; i >= 0; i--)
            {
                MouseEnemyController m = ActiveMice[i];
                if (m == null || !m.IsAlive) continue;
                float d = (m.transform.position - fromPos).sqrMagnitude;
                if (d <= bestDist)
                {
                    bestDist = d;
                    best = m;
                }
            }
            return best;
        }

        public static List<MouseEnemyController> GetEnemiesInRange(Vector3 fromPos, float maxRange)
        {
            SharedRangeResult.Clear();
            float rangeSq = maxRange * maxRange;
            for (int i = ActiveMice.Count - 1; i >= 0; i--)
            {
                MouseEnemyController m = ActiveMice[i];
                if (m == null || !m.IsAlive) continue;
                if ((m.transform.position - fromPos).sqrMagnitude <= rangeSq)
                    SharedRangeResult.Add(m);
            }
            return SharedRangeResult;
        }

        public static Vector3 GetDensestClusterPositionInRange(Vector3 fromPos, float maxRange)
        {
            List<MouseEnemyController> inRange = GetEnemiesInRange(fromPos, maxRange);
            if (inRange.Count == 0)
            {
                MouseEnemyController closest = GetClosestEnemy(fromPos, maxRange);
                if (closest != null) return closest.transform.position;
                return fromPos + new Vector3(0f, maxRange * 0.65f, 0f);
            }

            MouseEnemyController bestTarget = inRange[0];
            int bestCount = -1;

            for (int i = 0; i < inRange.Count; i++)
            {
                var candidate = inRange[i];
                if (candidate == null || !candidate.IsAlive) continue;
                int count = 0;
                for (int j = 0; j < inRange.Count; j++)
                {
                    var other = inRange[j];
                    if (other == null || !other.IsAlive) continue;
                    if ((other.transform.position - candidate.transform.position).sqrMagnitude <= 3.5f)
                        count++;
                }
                if (count > bestCount)
                {
                    bestCount = count;
                    bestTarget = candidate;
                }
            }

            return bestTarget != null ? bestTarget.transform.position : inRange[0].transform.position;
        }

        public static Vector3 GetDensestClusterPosition()
        {
            if (ActiveMice.Count == 0)
                return new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(0.5f, 2.8f), 0f);

            MouseEnemyController bestTarget = ActiveMice[0];
            int bestCount = -1;

            for (int i = 0; i < ActiveMice.Count; i++)
            {
                var candidate = ActiveMice[i];
                if (candidate == null || !candidate.IsAlive) continue;
                int count = 0;
                for (int j = 0; j < ActiveMice.Count; j++)
                {
                    var other = ActiveMice[j];
                    if (other == null || !other.IsAlive) continue;
                    if ((other.transform.position - candidate.transform.position).sqrMagnitude <= 4.0f)
                        count++;
                }
                if (count > bestCount)
                {
                    bestCount = count;
                    bestTarget = candidate;
                }
            }

            return bestTarget != null ? bestTarget.transform.position : new Vector3(0f, 1.5f, 0f);
        }
    }
}
