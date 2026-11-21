using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using CatFight.Data;
using CatFight.Controllers;
using CatFight.Visuals;
using CatFight.UI;

namespace CatFight.Managers
{
    public class GameManager : MonoBehaviour
    {
        private static readonly ProfilerMarker WaveSpawningMarker = new ProfilerMarker("CatFight.WaveSpawning");
        public static GameManager Instance { get; private set; }
        public static Camera MainCam { get; private set; }
        public static float CamHalfHeight { get; private set; } = 5.0f;
        public static float CamHalfWidth { get; private set; } = 2.8125f;
        private static readonly float[] HoleXPositions = new float[] { -1.65f, 0f, 1.65f };

        public const float DefenseLineY = -2.18f;
        public const float CatBedLineY = -2.42f;
        public const float HeroPosY = -3.68f;
        public static readonly Vector3[] CatBedPositions = new Vector3[]
        {
            new Vector3(-1.54f, -2.32f, 0f),
            new Vector3(-0.58f, -2.42f, 0f),
            new Vector3(0.58f, -2.42f, 0f),
            new Vector3(1.54f, -2.32f, 0f)
        };

        public GameAppState CurrentAppState { get; private set; } = GameAppState.MainMenu;

        public HumanPlayerController hero;
        public List<CatCompanionDefinition> allCatDefinitions = new List<CatCompanionDefinition>();
        public List<CatCompanionController> deployedCats = new List<CatCompanionController>();
        public Dictionary<MouseEnemyType, MouseEnemyDefinition> enemyDict;
        public List<EventMissionItem> eventMissions = new List<EventMissionItem>();
        public List<HeroWeaponDefinition> allWeapons = new List<HeroWeaponDefinition>();
        public List<CatSkinData> heroSkins = new List<CatSkinData>();

        // Meta Progression & Currencies
        public int gold = 800;
        public int gems = 450;
        public int summonTickets = 3;
        public int highestWaveReached = 1;

        // Permanent Hero Upgrades (Character Tab)
        public int heroAtkUpgradeLv = 1;
        public int heroSpeedUpgradeLv = 1;
        public int wallHpUpgradeLv = 1;
        public int equippedWeaponIndex = 0;
        public int equippedHeroSkinIndex = 0;
        public int menuShowcaseCatIndex = 0;
        public int currentChapter = 1;
        public int currentStage = 1; // 1..10 inside each Chapter (e.g., 1-1, 1-2 ... 1-10)
        public const int TotalChapters = 3;
        public const int StagesPerChapter = 10;
        public const int WavesPerChapter = 10;

        // Battle Session Stats (Each Stage has multiple Waves inside it!)
        public float wallMaxHp = 500f;
        public float wallCurrentHp = 500f;
        public int currentWave = 1;
        public int totalWavesInStage = 4;
        public int enemiesSpawnedInWave;
        public int totalEnemiesInWave = 14;
        public int totalKills;
        public int lifetimeKills;

        // Last Stage Clear / Attempt Summary (for Victory & Defeat Modals)
        public int lastClearedChapter = 1;
        public int lastClearedStage = 1;
        public int lastStageRewardGold;
        public int lastStageRewardGems;
        public int lastStageRewardTickets;
        public bool lastBattleWasVictory;

        public int playerLevel = 1;
        public int currentExp;
        public int expToNextLevel = 45;

        public bool IsPlaying => CurrentAppState == GameAppState.Battle && !isBattlePaused;
        private bool isBattlePaused;

        public bool AutoCastSkills { get; set; } = true;
        public int GameSpeedMultiplier { get; private set; } = 1;

        private float spawnTimer;
        private bool bossSpawnedThisWave;
        private Transform worldContainer;
        private Transform arenaBgTransform;
        private SpriteRenderer arenaBgSpriteRenderer;
        private Transform defenseWallTransform;
        private float lastCameraAspect = -1f;

        // Interactive Cat Attack Range Visualizer
        private GameObject rangeVisRoot;
        private Transform rangeOuterRingTr;
        private SpriteRenderer rangeOuterRingSr;
        private Transform rangeInnerAoETr;
        private SpriteRenderer rangeInnerAoESr;
        private LineRenderer rangeAimLine;
        private TextMesh rangeInfoTextMesh;
        private float rangeDisplayTimer;
        private float rangeBaseOuterScale = 1f;
        private float rangeBaseInnerScale = 1f;
        private string currentRangeCatId;

        // Cat Bed Slot Tracking & Drag-and-Drop Reordering across the 4 Cat Beds
        private readonly CatCompanionController[] catBedOccupancy = new CatCompanionController[4];
        private CatCompanionController draggingCat;
        private int draggingSourceSlot = -1;
        private Vector2 pointerPressScreenPos;
        private PointerEventData catPointerEvent;
        private readonly List<RaycastResult> catPointerHits = new List<RaycastResult>();
        private bool isPointerTrackingCat;
        private bool isActivelyDraggingCat;
        public bool IsDraggingCat => isActivelyDraggingCat;

        public event Action OnHudStateChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapIfNeeded()
        {
            if (FindFirstObjectByType<GameManager>() == null)
            {
                GameObject boot = new GameObject("CatFight_GameManager");
                boot.AddComponent<GameManager>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Mobile Performance Settings: Smooth 60 FPS target without VSync stutter on low-end phones
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            allCatDefinitions = CatFightDatabase.CreateDefaultCats();
            enemyDict = CatFightDatabase.CreateEnemyDefinitions();
            eventMissions = CatFightDatabase.CreateDefaultEventMissions();
            allWeapons = CatFightDatabase.CreateDefaultWeapons();
            heroSkins = CatFightDatabase.CreateHeroSkins();

            menuShowcaseCatIndex = Mathf.Clamp(PlayerPrefs.GetInt("CatFight_MenuShowcaseCatIdx", 0), 0, Mathf.Max(0, allCatDefinitions.Count - 1));
            currentChapter = Mathf.Clamp(PlayerPrefs.GetInt("CatFight_CurrentChapter", 1), 1, TotalChapters);
            currentStage = Mathf.Clamp(PlayerPrefs.GetInt("CatFight_CurrentStage", 1), 1, StagesPerChapter);
            highestWaveReached = Mathf.Max(1, (currentChapter - 1) * StagesPerChapter + currentStage);

            if (GetComponent<CatFightAudioManager>() == null)
                gameObject.AddComponent<CatFightAudioManager>();
            if (GetComponent<GoogleAdMobManager>() == null)
                gameObject.AddComponent<GoogleAdMobManager>();
        }

        private void Start()
        {
            ConfigureCameraAndLighting();
            ShutdownBattleArena();
            ApplyHeroMetaStats();
            CurrentAppState = GameAppState.MainMenu;
            CatFightUIManager.EnsureUIExists();
            if (CatFightUIManager.Instance != null)
            {
                CatFightUIManager.Instance.ShowMainMenu();
            }
        }

        private void ConfigureCameraAndLighting()
        {
            Camera cam = Camera.main;
            MainCam = cam;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 5.0f;
                cam.transform.position = new Vector3(0f, 0f, -10f);
                cam.transform.rotation = Quaternion.identity;
                cam.backgroundColor = new Color(0.96f, 0.91f, 0.82f, 1f);
                cam.allowHDR = false;
                CamHalfHeight = cam.orthographicSize;
                CamHalfWidth = CamHalfHeight * cam.aspect;
            }

            // Disable 3D URP Global Volume / Post-Processing Tonemapping & Vignette so 2D map sprites render at 100% pure brightness!
            GameObject globalVol = GameObject.Find("Global Volume");
            if (globalVol != null && globalVol.activeSelf)
                globalVol.SetActive(false);

            if (GetComponent<CombatVisualFeedback>() == null)
                gameObject.AddComponent<CombatVisualFeedback>();
        }

        private void SetupCameraAndArena()
        {
            ConfigureCameraAndLighting();

            if (worldContainer != null)
            {
                Destroy(worldContainer.gameObject);
                worldContainer = null;
            }

            GameObject existingWorld = GameObject.Find("CatFight_WorldArena");
            if (existingWorld != null)
                Destroy(existingWorld);

            worldContainer = new GameObject("CatFight_WorldArena").transform;

            // Arena Background (Dynamic per Chapter: Chap 1 Kitchen, Chap 2 Garden, Chap 3 Sewer)
            GameObject bgObj = new GameObject("ArenaBackground");
            bgObj.transform.SetParent(worldContainer, false);
            bgObj.transform.position = new Vector3(0f, 0.42f, 0f);
            arenaBgTransform = bgObj.transform;
            arenaBgSpriteRenderer = bgObj.AddComponent<SpriteRenderer>();
            arenaBgSpriteRenderer.sortingOrder = -20;
            ApplyChapterBackground();

            // Defense Barricade Wall (Pulled lower down near the bottom HP bar, spanning full screen width!)
            GameObject wallObj = new GameObject("DefenseBarricadeWall");
            wallObj.transform.SetParent(worldContainer, false);
            wallObj.transform.position = new Vector3(0f, -3.08f, 0f);
            wallObj.transform.localScale = new Vector3(0.76f, 0.52f, 1f);
            defenseWallTransform = wallObj.transform;
            SpriteRenderer wallsr = wallObj.AddComponent<SpriteRenderer>();
            wallsr.sprite = RuntimeSpriteBank.GetSingle("Sprites/Environment/defense_wall", 100f);
            wallsr.sortingOrder = 20;

            // 4 Cozy Cat Beds ("Ổ Mèo") placed in front of the fence (high-res plush cat-ear cushion beds!)
            Sprite catBedSprite = RuntimeSpriteBank.GetSingle("Sprites/Environment/cat_bed", 115f);
            for (int i = 0; i < CatBedPositions.Length; i++)
            {
                GameObject bedObj = new GameObject($"CatBed_Slot_{i + 1}");
                bedObj.transform.SetParent(worldContainer, false);
                bedObj.transform.position = CatBedPositions[i];
                bedObj.transform.localScale = Vector3.one * 0.42f;
                SpriteRenderer bsr = bedObj.AddComponent<SpriteRenderer>();
                bsr.sprite = catBedSprite;
                bsr.sortingOrder = 22;
            }

            CreateAttackRangeVisualizerObjects();
            lastCameraAspect = -1f;
            UpdateArenaForScreenAspect();
        }

        private void UpdateArenaForScreenAspect()
        {
            if (MainCam == null) MainCam = Camera.main;
            Camera cam = MainCam;
            if (cam == null) return;
            if (Mathf.Abs(cam.aspect - lastCameraAspect) < 0.001f) return;
            lastCameraAspect = cam.aspect;

            CamHalfHeight = cam.orthographicSize;
            CamHalfWidth = CamHalfHeight * cam.aspect;
            float camWidth = CamHalfWidth * 2f;

            // Scale ArenaBackground so the living room carpet & floor cover 100% of any aspect ratio without top/side seams
            if (arenaBgTransform != null)
            {
                float bgScaleX = Mathf.Max(1.08f, (camWidth + 0.4f) / 3.85f);
                arenaBgTransform.localScale = new Vector3(bgScaleX, 1.24f, 1f);
            }

            // Scale DefenseBarricadeWall horizontally so the fence spans 100% of the screen width on every aspect ratio
            if (defenseWallTransform != null)
            {
                float wallScaleX = Mathf.Max(0.76f, (camWidth + 0.35f) / 8.8f);
                defenseWallTransform.localScale = new Vector3(wallScaleX, 0.52f, 1f);
            }
        }

        private void CreateAttackRangeVisualizerObjects()
        {
            rangeVisRoot = new GameObject("CatAttackRangeVisualizer");
            rangeVisRoot.transform.SetParent(worldContainer, false);

            Sprite ringSp = RuntimeSpriteBank.GetSingle("Sprites/Effects/fx_range_circle", 100f);

            // 1. Outer Attack Range Ring (Centered on the clicked Cat)
            GameObject outerGo = new GameObject("OuterAttackRangeRing");
            outerGo.transform.SetParent(rangeVisRoot.transform, false);
            rangeOuterRingTr = outerGo.transform;
            rangeOuterRingSr = outerGo.AddComponent<SpriteRenderer>();
            rangeOuterRingSr.sprite = ringSp;
            rangeOuterRingSr.sortingOrder = 15;

            // 2. Inner Skill AoE Impact Zone Ring (Positioned in front of the Cat on the lane)
            GameObject innerGo = new GameObject("SkillImpactAoERing");
            innerGo.transform.SetParent(rangeVisRoot.transform, false);
            rangeInnerAoETr = innerGo.transform;
            rangeInnerAoESr = innerGo.AddComponent<SpriteRenderer>();
            rangeInnerAoESr.sprite = ringSp;
            rangeInnerAoESr.sortingOrder = 16;

            // 3. Connecting Aim Line from Cat to Target AoE Center
            GameObject lineGo = new GameObject("RangeAimLine");
            lineGo.transform.SetParent(rangeVisRoot.transform, false);
            rangeAimLine = lineGo.AddComponent<LineRenderer>();
            rangeAimLine.positionCount = 2;
            rangeAimLine.startWidth = 0.05f;
            rangeAimLine.endWidth = 0.05f;
            rangeAimLine.material = new Material(Shader.Find("Sprites/Default"));
            rangeAimLine.sortingOrder = 16;

            // 4. Floating World Info Badge above the Cat
            GameObject labelGo = new GameObject("RangeInfoLabel");
            labelGo.transform.SetParent(rangeVisRoot.transform, false);
            rangeInfoTextMesh = labelGo.AddComponent<TextMesh>();
            rangeInfoTextMesh.fontSize = 36;
            rangeInfoTextMesh.characterSize = 0.066f;
            rangeInfoTextMesh.anchor = TextAnchor.MiddleCenter;
            rangeInfoTextMesh.alignment = TextAlignment.Center;
            rangeInfoTextMesh.fontStyle = FontStyle.Bold;
            rangeInfoTextMesh.color = new Color(1f, 0.95f, 0.45f, 1f);
            MeshRenderer mr = labelGo.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 92;

            rangeVisRoot.SetActive(false);
        }

        public void ShowCatAttackRangeForController(CatCompanionController cat)
        {
            if (cat == null || cat.definition == null) return;
            ShowCatAttackRange(cat.transform.position, cat.definition, cat.CurrentAttackRange, cat.CurrentRadius);
        }

        public void ShowCatAttackRange(Vector3 catWorldPos, CatCompanionDefinition def, float attackRange, float aoeRadius)
        {
            if (rangeVisRoot == null || def == null) return;

            rangeVisRoot.SetActive(true);
            currentRangeCatId = def.id;
            rangeDisplayTimer = 5.5f;

            // fx_range_circle is 256x256 at 100 PPU -> outer ring radius in sprite is 1.20 world units (diameter 2.40)
            // Show ONE single, accurate Attack Range circle centered directly on the Cat!
            rangeBaseOuterScale = (attackRange * 2f) / 2.40f;
            rangeBaseInnerScale = 0f;

            rangeOuterRingTr.position = catWorldPos;
            rangeOuterRingTr.localScale = Vector3.one * rangeBaseOuterScale;
            Color c = def.themeColor;
            rangeOuterRingSr.color = new Color(c.r, c.g, c.b, 0.78f);

            if (rangeInnerAoETr != null)
                rangeInnerAoETr.gameObject.SetActive(false);

            if (rangeAimLine != null)
                rangeAimLine.enabled = false;

            if (rangeInfoTextMesh != null)
            {
                rangeInfoTextMesh.transform.position = catWorldPos + new Vector3(0f, 0.72f, 0f);
                rangeInfoTextMesh.text = $"★ {def.displayName.ToUpper()}\nTầm Đánh: {attackRange:0.1}m • Vùng Nổ: {aoeRadius:0.1}m";
            }
        }

        public void SetCombatActorsVisible(bool visible)
        {
            if (worldContainer != null)
                worldContainer.gameObject.SetActive(visible);
            if (!visible)
                HideCatAttackRange();
        }

        public void ShutdownBattleArena()
        {
            ClearAllActiveBattleObjects();

            if (hero != null)
            {
                Destroy(hero.gameObject);
                hero = null;
            }

            if (worldContainer != null)
            {
                Destroy(worldContainer.gameObject);
                worldContainer = null;
            }

            GameObject existingWorld = GameObject.Find("CatFight_WorldArena");
            if (existingWorld != null)
                Destroy(existingWorld);

            arenaBgTransform = null;
            arenaBgSpriteRenderer = null;
            defenseWallTransform = null;
            rangeVisRoot = null;
        }

        public void HideCatAttackRange()
        {
            if (rangeVisRoot != null)
                rangeVisRoot.SetActive(false);
            currentRangeCatId = null;
        }

        private void SpawnSoloHeroForPreview()
        {
            foreach (var c in deployedCats)
            {
                if (c != null) Destroy(c.gameObject);
            }
            deployedCats.Clear();

            if (hero != null)
                Destroy(hero.gameObject);

            if (worldContainer == null)
                SetupCameraAndArena();

            GameObject heroObj = new GameObject("HumanHero_Player");
            heroObj.transform.SetParent(worldContainer, false);
            heroObj.transform.position = new Vector3(0f, HeroPosY, 0f);
            hero = heroObj.AddComponent<HumanPlayerController>();
            hero.Initialize();
            ApplyHeroMetaStats();
        }

        public HeroWeaponDefinition GetEquippedWeapon()
        {
            if (allWeapons != null && equippedWeaponIndex >= 0 && equippedWeaponIndex < allWeapons.Count)
                return allWeapons[equippedWeaponIndex];
            return new HeroWeaponDefinition { displayName = "Gatling Tiêu Chuẩn", tintColor = Color.white };
        }

        public CatSkinData GetEquippedHeroSkin()
        {
            if (heroSkins != null && equippedHeroSkinIndex >= 0 && equippedHeroSkinIndex < heroSkins.Count)
                return heroSkins[equippedHeroSkinIndex];
            return new CatSkinData { skinName = "Mặc Định", tintColor = Color.white };
        }

        public CatCompanionDefinition GetMenuShowcaseCat()
        {
            if (allCatDefinitions == null || allCatDefinitions.Count == 0)
                return null;
            menuShowcaseCatIndex = Mathf.Clamp(menuShowcaseCatIndex, 0, allCatDefinitions.Count - 1);
            if (allCatDefinitions[menuShowcaseCatIndex] == null || !allCatDefinitions[menuShowcaseCatIndex].unlocked)
            {
                int unlockedIndex = allCatDefinitions.FindIndex(cat => cat != null && cat.unlocked);
                if (unlockedIndex < 0) return null;
                menuShowcaseCatIndex = unlockedIndex;
            }
            return allCatDefinitions[menuShowcaseCatIndex];
        }

        public void SetMenuShowcaseCatIndex(int index)
        {
            if (allCatDefinitions == null || allCatDefinitions.Count == 0)
                return;
            if (index < 0 || index >= allCatDefinitions.Count ||
                allCatDefinitions[index] == null || !allCatDefinitions[index].unlocked)
                return;
            menuShowcaseCatIndex = index;
            SaveProgressValue("CatFight_MenuShowcaseCatIdx", menuShowcaseCatIndex);
        }

        public void CycleMenuShowcaseCat(int delta)
        {
            if (allCatDefinitions == null || allCatDefinitions.Count == 0)
                return;
            if (delta == 0) return;
            int direction = delta > 0 ? 1 : -1;
            for (int step = 1; step <= allCatDefinitions.Count; step++)
            {
                int index = ((menuShowcaseCatIndex + direction * step) % allCatDefinitions.Count + allCatDefinitions.Count) % allCatDefinitions.Count;
                if (allCatDefinitions[index] != null && allCatDefinitions[index].unlocked)
                {
                    SetMenuShowcaseCatIndex(index);
                    return;
                }
            }
        }

        public string GetChapterName(int chapter)
        {
            switch (chapter)
            {
                case 1: return "CHAP 1: PHÒNG BẾP";
                case 2: return "CHAP 2: SÂN VƯỜN";
                case 3: return "CHAP 3: CỐNG NGẦM";
                default: return $"CHAP {chapter}";
            }
        }

        public string GetChapterShortTitle(int chapter)
        {
            switch (chapter)
            {
                case 1: return "PHÒNG BẾP";
                case 2: return "SÂN VƯỜN";
                case 3: return "CỐNG NGẦM";
                default: return $"CHAP {chapter}";
            }
        }

        public string GetStageCode()
        {
            return $"{currentChapter}-{currentStage}";
        }

        public string GetClearedStageCode()
        {
            return $"{lastClearedChapter}-{lastClearedStage}";
        }

        public bool IsKeyMilestoneStage(int chapter, int stage)
        {
            return stage == 5 || stage == StagesPerChapter;
        }

        public int GetTotalWavesForStage(int chapter, int stage)
        {
            if (stage >= StagesPerChapter) return 6; // Chapter Finale (1-10, 2-10, 3-10): 6 intense waves + Chapter Boss!
            if (stage == 5) return 5;                // Mid-Boss Milestone (1-5, 2-5, 3-5): 5 waves + Mid-Boss!
            return 4;                                // Normal stage: 4 waves of mice!
        }

        public float GetStageBaseHpScale(int chapter, int stage)
        {
            int c = Mathf.Clamp(chapter, 1, TotalChapters);
            int s = Mathf.Clamp(stage, 1, StagesPerChapter);

            float scale;
            if (c == 1)
            {
                // Chap 1 (1-1 to 1-10): 0.72x at 1-1 -> ~2.60x at 1-10
                scale = 0.72f * Mathf.Pow(1.13f, s - 1);
            }
            else if (c == 2)
            {
                // Chap 2 (2-1 to 2-10): 3.00x at 2-1 -> ~9.00x at 2-10
                scale = 3.00f * Mathf.Pow(1.13f, s - 1);
            }
            else
            {
                // Chap 3 (3-1 to 3-10): 10.50x at 3-1 -> ~31.50x at 3-10
                scale = 10.50f * Mathf.Pow(1.13f, s - 1);
            }

            // Key Milestone Gear-Check Spikes (1-5, 1-10, 2-5, 2-10, 3-5, 3-10)
            // Requires the player to upgrade Hero Attack, Attack Speed, Fence HP, and Cats to break through!
            if (s == 5)
                scale *= 1.32f;
            else if (s == StagesPerChapter)
                scale *= 1.50f;

            return scale;
        }

        public float GetHeroDamage(int level)
        {
            var weapon = GetEquippedWeapon();
            float weaponBonus = 1f + (weapon.level - 1) * 0.18f;
            return 22f * (1f + (level - 1) * 0.28f) * Mathf.Pow(1.06f, level - 1)
                * weapon.damageMultiplier * weaponBonus * (1f + GetEquippedHeroSkin().damageBonusPct);
        }

        public float GetHeroFireRate(int level) => 2.4f * (1f + (level - 1) * 0.12f) * GetEquippedWeapon().fireRateMultiplier;
        public int GetWallHealth(int level) => 500 + (level - 1) * 160;

        public int GetCurrentPowerScore()
        {
            float heroDps = GetHeroDamage(heroAtkUpgradeLv) * GetHeroFireRate(heroSpeedUpgradeLv) * (1 + GetEquippedWeapon().bonusBarrels);

            float catPower = 0f;
            foreach (var c in allCatDefinitions)
            {
                if (c != null && c.unlocked)
                {
                    float cDmg = c.baseDamage * Mathf.Pow(1.10f, c.permanentLevel - 1) * c.GetSkinDamageMultiplier();
                    catPower += (cDmg / Mathf.Max(0.5f, c.baseCooldown)) * 4.2f;
                }
            }

            float wallScore = GetWallHealth(wallHpUpgradeLv) * 0.22f;
            return Mathf.RoundToInt(heroDps * 2.4f + catPower + wallScore);
        }

        public int GetRecommendedPowerForStage(int chapter, int stage)
        {
            float baseScale = GetStageBaseHpScale(chapter, stage);
            return Mathf.RoundToInt(210f * Mathf.Pow(baseScale, 0.88f));
        }

        public string GetChapterBackgroundPath(int chapter)
        {
            switch (chapter)
            {
                case 1: return "Sprites/Environment/bg_chap1_kitchen";
                case 2: return "Sprites/Environment/bg_chap2_garden";
                case 3: return "Sprites/Environment/bg_chap3_sewer";
                default: return "Sprites/Environment/bg_chap1_kitchen";
            }
        }

        private void SaveProgressValue(string key, int value)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (DebugSessionActive) return;
#endif
            PlayerPrefs.SetInt(key, value);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool DebugSessionActive { get; private set; }

        public void BeginDebugSession()
        {
            DebugSessionActive = true;
        }

        public void DebugSummonBoss()
        {
            if (!DebugSessionActive || !IsPlaying || MouseEnemyController.IsAnyBossAlive()) return;
            SpawnEnemy(MouseEnemyType.Boss);
            AnnounceBoss();
        }

        public void DebugClearWave()
        {
            if (!DebugSessionActive || !IsPlaying) return;
            for (int i = MouseEnemyController.ActiveMice.Count - 1; i >= 0; i--)
                MouseEnemyController.ActiveMice[i].DespawnToPool();
            enemiesSpawnedInWave = totalEnemiesInWave;
            UpdateWaveSpawning();
        }

        public void DebugFinishStage()
        {
            if (DebugSessionActive && IsPlaying) CompleteCurrentStageVictory();
        }
#endif

        public void SetChapter(int chapter)
        {
            currentChapter = ((chapter - 1) % TotalChapters + TotalChapters) % TotalChapters + 1;
            SaveProgressValue("CatFight_CurrentChapter", currentChapter);
            ApplyChapterBackground();
            OnHudStateChanged?.Invoke();
        }

        public void SetStageProgression(int chapter, int stage)
        {
            currentChapter = Mathf.Clamp(chapter, 1, TotalChapters);
            currentStage = Mathf.Clamp(stage, 1, StagesPerChapter);
            SaveProgressValue("CatFight_CurrentChapter", currentChapter);
            SaveProgressValue("CatFight_CurrentStage", currentStage);

            int globalStageIndex = (currentChapter - 1) * StagesPerChapter + currentStage;
            if (globalStageIndex > highestWaveReached)
                highestWaveReached = globalStageIndex;

            ApplyChapterBackground();
            OnHudStateChanged?.Invoke();
        }

        public void CycleChapter(int delta)
        {
            SetChapter(currentChapter + delta);
        }

        public void ApplyChapterBackground()
        {
            if (arenaBgSpriteRenderer != null)
            {
                arenaBgSpriteRenderer.sprite = RuntimeSpriteBank.GetSingle(GetChapterBackgroundPath(currentChapter), 108f);
            }
        }

        public void ApplyHeroMetaStats()
        {
            wallMaxHp = GetWallHealth(wallHpUpgradeLv);
            if (hero == null) return;

            var wpn = GetEquippedWeapon();
            var skin = GetEquippedHeroSkin();
            hero.baseDamage = GetHeroDamage(heroAtkUpgradeLv);
            hero.fireRate = GetHeroFireRate(heroSpeedUpgradeLv);
            hero.ApplyVisualTints(skin.tintColor, wpn.tintColor);
        }

        // ==========================================
        // BATTLE ENTRY: Completely rebuilds & loads the Stage Arena,
        // shows the Stage Loading Screen, then begins Wave 1!
        // ==========================================
        public void StartBattleFromMainMenu()
        {
            ShutdownBattleArena();
            SetupCameraAndArena();

            foreach (var catDef in allCatDefinitions)
                catDef.battleStageLevel = 1;

            SpawnSoloHeroForPreview();
            hero.multishot = 1 + GetEquippedWeapon().bonusBarrels;
            hero.pierceCount = 0;
            wallCurrentHp = wallMaxHp;

            playerLevel = 1;
            currentExp = 0;
            expToNextLevel = 32; // Fast first level-up so player quickly summons their first Cat in Wave 1!
            totalKills = 0;

            currentChapter = Mathf.Clamp(currentChapter, 1, TotalChapters);
            currentStage = Mathf.Clamp(currentStage, 1, StagesPerChapter);
            totalWavesInStage = GetTotalWavesForStage(currentChapter, currentStage);
            currentWave = 1;
            enemiesSpawnedInWave = 0;
            bossSpawnedThisWave = false;
            lastBattleWasVictory = false;

            CurrentAppState = GameAppState.Battle;
            isBattlePaused = true;
            Time.timeScale = 1f;
            ApplyChapterBackground();

            if (CatFightAudioManager.Instance != null)
                CatFightAudioManager.Instance.PlayNormalChillBgm();

            if (CatFightUIManager.Instance != null)
            {
                CatFightUIManager.Instance.ShowBattleHUD();
                CatFightUIManager.Instance.ShowStageLoadingScreen();
            }
            else
            {
                FinishStageLoadingAndStartWave1();
            }
        }

        public void FinishStageLoadingAndStartWave1()
        {
            if (CurrentAppState != GameAppState.Battle) return;
            isBattlePaused = false;
            Time.timeScale = GameSpeedMultiplier;
            StartWave(1);
        }

        public void ReturnToMainMenu()
        {
            // Completely shut down and unload the Battle Stage when returning to the Main Menu!
            ShutdownBattleArena();

            CurrentAppState = GameAppState.MainMenu;
            isBattlePaused = false;
            Time.timeScale = 1f;

            if (CatFightAudioManager.Instance != null)
                CatFightAudioManager.Instance.PlayNormalChillBgm();

            if (CatFightUIManager.Instance != null)
                CatFightUIManager.Instance.ShowMainMenu();
        }

        private void ClearAllActiveBattleObjects()
        {
            HideCatAttackRange();
            MouseEnemyController.DestroyAllAndClearPool();
            HeroBulletBehaviour.ClearAllBullets();
            PawStompSkillEffect.ClearAllSkillEffects();
            CombatVisualFeedback.ClearAllActiveVFX();

            foreach (var c in deployedCats)
            {
                if (c != null) Destroy(c.gameObject);
            }
            deployedCats.Clear();
            for (int i = 0; i < catBedOccupancy.Length; i++)
                catBedOccupancy[i] = null;
            draggingCat = null;
            draggingSourceSlot = -1;
            isPointerTrackingCat = false;
            isActivelyDraggingCat = false;
        }

        public CatCompanionController DeployCatCompanion(CatCompanionDefinition def)
        {
            // Deployment must never bypass collection unlocks, including stale upgrade callbacks.
            if (def == null || !def.unlocked || !allCatDefinitions.Contains(def))
                return null;
            foreach (var c in deployedCats)
            {
                if (c != null && c.definition.id == def.id)
                {
                    if (def.IsAwakened) return c;
                    def.battleStageLevel++;
                    if (def.IsAwakened)
                    {
                        CombatVisualFeedback.SpawnImpactRing(c.transform.position, def.themeColor, 1.5f, 0.45f);
                        CombatVisualFeedback.SpawnSparkleExplosion(c.transform.position, def.themeColor, 10, 2.8f, true);
                        CatFightUIManager.Instance?.ShowNotification($"{def.displayName} thức tỉnh! {def.AwakeningDescription}");
                    }
                    c.RefreshSkinVisual();
                    RearrangeCompanionPositions();
                    OnHudStateChanged?.Invoke();
                    return c;
                }
            }

            if (deployedCats.Count >= CatBedPositions.Length) return null;

            GameObject catObj = new GameObject($"Cat_{def.id}");
            catObj.transform.SetParent(worldContainer, false);
            CatCompanionController ctrl = catObj.AddComponent<CatCompanionController>();
            ctrl.Setup(def);
            deployedCats.Add(ctrl);

            // Assign to the first empty Cat Bed slot
            for (int i = 0; i < catBedOccupancy.Length; i++)
            {
                if (catBedOccupancy[i] == null)
                {
                    catBedOccupancy[i] = ctrl;
                    break;
                }
            }

            RearrangeCompanionPositions();
            CombatVisualFeedback.SpawnImpactRing(ctrl.transform.position, def.themeColor, 1.2f, 0.35f);
            CombatVisualFeedback.SpawnSparkleExplosion(ctrl.transform.position, def.themeColor, 8, 2.4f, true);
            CatFightUIManager.Instance?.ShowSummonNotice(def);
            OnHudStateChanged?.Invoke();
            return ctrl;
        }

        public void RearrangeCompanionPositions()
        {
            // Keep catBedOccupancy synchronized with deployedCats while preserving custom drag-and-drop bed assignments!
            for (int i = 0; i < catBedOccupancy.Length; i++)
            {
                if (catBedOccupancy[i] != null && !deployedCats.Contains(catBedOccupancy[i]))
                    catBedOccupancy[i] = null;
            }

            foreach (var cat in deployedCats)
            {
                if (cat == null) continue;
                bool hasSlot = false;
                for (int i = 0; i < catBedOccupancy.Length; i++)
                {
                    if (catBedOccupancy[i] == cat)
                    {
                        hasSlot = true;
                        break;
                    }
                }
                if (!hasSlot)
                {
                    for (int i = 0; i < catBedOccupancy.Length; i++)
                    {
                        if (catBedOccupancy[i] == null)
                        {
                            catBedOccupancy[i] = cat;
                            break;
                        }
                    }
                }
            }

            for (int i = 0; i < catBedOccupancy.Length; i++)
            {
                var cat = catBedOccupancy[i];
                if (cat == null) continue;
                if (isActivelyDraggingCat && cat == draggingCat) continue;

                Vector3 bedPos = CatBedPositions[i % CatBedPositions.Length];
                // Offset cat upward by +0.26f so the cat's feet/bottom stand right in the center of the cat bed cushion!
                cat.transform.position = bedPos + new Vector3(0f, 0.26f, 0f);
            }
        }

        private void Update()
        {
            UpdateArenaForScreenAspect();
            UpdateRangeVisualizerAnimation();
            HandleWorldCatClick();

            if (!IsPlaying) return;
            UpdateWaveSpawning();
        }

        private void UpdateRangeVisualizerAnimation()
        {
            if (rangeVisRoot == null || !rangeVisRoot.activeSelf) return;

            rangeDisplayTimer -= Time.unscaledDeltaTime;
            if (rangeDisplayTimer <= 0f)
            {
                HideCatAttackRange();
                return;
            }

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 5.5f) * 0.025f;
            if (rangeOuterRingTr != null)
            {
                rangeOuterRingTr.Rotate(0f, 0f, -18f * Time.unscaledDeltaTime);
                rangeOuterRingTr.localScale = Vector3.one * (rangeBaseOuterScale * pulse);
            }
            if (rangeInnerAoETr != null)
            {
                rangeInnerAoETr.Rotate(0f, 0f, 38f * Time.unscaledDeltaTime);
                rangeInnerAoETr.localScale = Vector3.one * (rangeBaseInnerScale * (2f - pulse));
            }
        }

        private bool IsCatTapOverUI(Vector2 screenPos)
        {
            var events = EventSystem.current;
            if (events == null) return false;
            if (catPointerEvent == null || catPointerEvent.currentInputModule != events.currentInputModule)
                catPointerEvent = new PointerEventData(events);
            catPointerEvent.position = screenPos;
            catPointerHits.Clear();
            events.RaycastAll(catPointerEvent, catPointerHits);
            return catPointerHits.Exists(hit => hit.module is UnityEngine.UI.GraphicRaycaster);
        }

        private void HandleWorldCatClick()
        {
            if (MainCam == null) MainCam = Camera.main;
            if (MainCam == null || CurrentAppState != GameAppState.Battle) return;

            bool pressedThisFrame = false;
            bool isHeld = false;
            bool releasedThisFrame = false;
            Vector2 screenPos = Vector2.zero;

            if (Pointer.current != null)
            {
                pressedThisFrame = Pointer.current.press.wasPressedThisFrame;
                isHeld = Pointer.current.press.isPressed;
                releasedThisFrame = Pointer.current.press.wasReleasedThisFrame;
                screenPos = Pointer.current.position.ReadValue();
            }
            else if (Mouse.current != null)
            {
                pressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
                isHeld = Mouse.current.leftButton.isPressed;
                releasedThisFrame = Mouse.current.leftButton.wasReleasedThisFrame;
                screenPos = Mouse.current.position.ReadValue();
            }

            if (pressedThisFrame)
            {
                if (!IsPlaying || IsCatTapOverUI(screenPos))
                {
                    return;
                }

                Vector3 worldPt = MainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
                worldPt.z = 0f;

                RearrangeCompanionPositions();
                for (int slot = 0; slot < catBedOccupancy.Length; slot++)
                {
                    var cat = catBedOccupancy[slot];
                    if (cat == null || cat.definition == null) continue;
                    if (Vector2.Distance(worldPt, cat.transform.position) <= 0.65f)
                    {
                        draggingCat = cat;
                        draggingSourceSlot = slot;
                        pointerPressScreenPos = screenPos;
                        isPointerTrackingCat = true;
                        isActivelyDraggingCat = false;
                        return;
                    }
                }

                // Clicked on empty battlefield -> hide attack range visualizer
                if (rangeVisRoot != null && rangeVisRoot.activeSelf)
                {
                    HideCatAttackRange();
                }
            }

            if (isPointerTrackingCat && draggingCat != null)
            {
                if (isHeld)
                {
                    if (!isActivelyDraggingCat && Vector2.Distance(screenPos, pointerPressScreenPos) >= 16f)
                    {
                        isActivelyDraggingCat = true;
                        HideCatAttackRange();
                        SpriteRenderer sr = draggingCat.GetComponent<SpriteRenderer>();
                        if (sr != null) sr.sortingOrder = 95;
                        if (CatFightAudioManager.Instance != null)
                            CatFightAudioManager.Instance.PlayUiClick();
                    }

                    if (isActivelyDraggingCat)
                    {
                        Vector3 worldPt = MainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
                        worldPt.z = 0f;
                        draggingCat.transform.position = worldPt + new Vector3(0f, 0.12f, 0f);
                    }
                }

                if (releasedThisFrame || !isHeld)
                {
                    SpriteRenderer sr = draggingCat.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sortingOrder = 38;

                    if (isActivelyDraggingCat)
                    {
                        Vector3 worldPt = MainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
                        worldPt.z = 0f;

                        // Find nearest Cat Bed slot to drop/swap into
                        int bestSlot = draggingSourceSlot;
                        float bestDist = float.MaxValue;
                        for (int i = 0; i < CatBedPositions.Length; i++)
                        {
                            Vector3 slotCenter = CatBedPositions[i] + new Vector3(0f, 0.20f, 0f);
                            float d = Vector2.Distance(worldPt, slotCenter);
                            if (d < bestDist && d <= 1.25f)
                            {
                                bestDist = d;
                                bestSlot = i;
                            }
                        }

                        if (bestSlot != draggingSourceSlot && bestSlot >= 0 && bestSlot < catBedOccupancy.Length)
                        {
                            // Swap or move the dragged cat into the target Cat Bed slot!
                            CatCompanionController otherCat = catBedOccupancy[bestSlot];
                            catBedOccupancy[bestSlot] = draggingCat;
                            catBedOccupancy[draggingSourceSlot] = otherCat;

                            // Keep deployedCats order consistent with bed slots
                            deployedCats.Clear();
                            for (int i = 0; i < catBedOccupancy.Length; i++)
                            {
                                if (catBedOccupancy[i] != null)
                                    deployedCats.Add(catBedOccupancy[i]);
                            }

                            Vector3 dropFxPos = CatBedPositions[bestSlot] + new Vector3(0f, 0.26f, 0f);
                            CombatVisualFeedback.SpawnImpactRing(dropFxPos, draggingCat.definition.themeColor, 0.95f, 0.26f);
                            CombatVisualFeedback.SpawnSparkleExplosion(dropFxPos, draggingCat.definition.themeColor, 6, 1.8f, true);
                            if (CatFightAudioManager.Instance != null)
                                CatFightAudioManager.Instance.PlayUiClick();
                        }

                        isActivelyDraggingCat = false;
                        isPointerTrackingCat = false;
                        draggingCat = null;
                        draggingSourceSlot = -1;
                        RearrangeCompanionPositions();
                        OnHudStateChanged?.Invoke();
                    }
                    else
                    {
                        // Short tap without dragging -> toggle Attack Range Visualizer!
                        var tappedCat = draggingCat;
                        isActivelyDraggingCat = false;
                        isPointerTrackingCat = false;
                        draggingCat = null;
                        draggingSourceSlot = -1;
                        RearrangeCompanionPositions();

                        if (IsPlaying) CatFightUIManager.Instance?.PetCat(tappedCat.definition, MainCam.WorldToScreenPoint(tappedCat.transform.position));

                        if (rangeVisRoot != null && rangeVisRoot.activeSelf && currentRangeCatId == tappedCat.definition.id)
                            HideCatAttackRange();
                        else
                            ShowCatAttackRangeForController(tappedCat);
                    }
                }
            }
        }

        public void StartWave(int waveIndex)
        {
            totalWavesInStage = GetTotalWavesForStage(currentChapter, currentStage);
            currentWave = Mathf.Clamp(waveIndex, 1, totalWavesInStage);

            int globalStageIndex = (currentChapter - 1) * StagesPerChapter + currentStage;
            if (globalStageIndex > highestWaveReached)
                highestWaveReached = globalStageIndex;

            UpdateMissionProgress("ev_wave_3", Mathf.Max(highestWaveReached, currentWave), true);

            enemiesSpawnedInWave = 0;
            bossSpawnedThisWave = false;

            // Each Stage (e.g. 1-1) has multiple waves inside it (4 waves normal, 5 waves X-5, 6 waves X-10)
            int baseCount = 6 + (currentChapter - 1) * 4 + (currentStage - 1);
            totalEnemiesInWave = baseCount + (currentWave - 1) * 3;
            if (currentWave == totalWavesInStage && IsKeyMilestoneStage(currentChapter, currentStage))
            {
                totalEnemiesInWave += 4;
            }

            spawnTimer = 0.85f;

            if (!MouseEnemyController.IsAnyBossAlive() && CatFightAudioManager.Instance != null)
                CatFightAudioManager.Instance.PlayNormalChillBgm();

            OnHudStateChanged?.Invoke();
        }

        private void UpdateWaveSpawning()
        {
            using var sample = WaveSpawningMarker.Auto();
            if (enemiesSpawnedInWave < totalEnemiesInWave)
            {
                spawnTimer -= Time.deltaTime;
                if (spawnTimer <= 0f)
                {
                    float baseInterval;
                    if (currentChapter == 1)
                        baseInterval = Mathf.Max(1.05f, 1.75f - (currentStage - 1) * 0.045f - (currentWave - 1) * 0.06f);
                    else if (currentChapter == 2)
                        baseInterval = Mathf.Max(0.85f, 1.38f - (currentStage - 1) * 0.035f - (currentWave - 1) * 0.05f);
                    else
                        baseInterval = Mathf.Max(0.68f, 1.12f - (currentStage - 1) * 0.030f - (currentWave - 1) * 0.04f);

                    spawnTimer = baseInterval;
                    SpawnWaveEnemy();
                    enemiesSpawnedInWave++;
                    OnHudStateChanged?.Invoke();
                }
            }
            else if (MouseEnemyController.ActiveMice.Count == 0)
            {
                if (currentWave < totalWavesInStage)
                {
                    // Advance to the next Wave inside the current Stage!
                    HealWall(wallMaxHp * 0.14f);
                    int waveClearGold = 18 + currentStage * 4 + (currentChapter - 1) * 18 + currentWave * 5;
                    gold += waveClearGold;
                    StartWave(currentWave + 1);
                }
                else
                {
                    // All Waves in this Stage cleared! Pay out Stage Clear Rewards, unlock next Stage (e.g. 1-1 -> 1-2), and show Stage Victory Modal!
                    CompleteCurrentStageVictory();
                }
            }
        }

        private void CompleteCurrentStageVictory()
        {
            lastClearedChapter = currentChapter;
            lastClearedStage = currentStage;
            lastBattleWasVictory = true;

            bool wasMidBoss = (lastClearedStage == 5);
            bool wasChapterBoss = (lastClearedStage == StagesPerChapter);

            lastStageRewardGold = 140 + (lastClearedChapter - 1) * 180 + lastClearedStage * 45 + (wasMidBoss ? 180 : 0) + (wasChapterBoss ? 380 : 0);
            lastStageRewardGems = 25 + (lastClearedChapter - 1) * 15 + lastClearedStage * 5 + (wasMidBoss ? 45 : 0) + (wasChapterBoss ? 90 : 0);
            lastStageRewardTickets = (wasMidBoss || wasChapterBoss) ? 1 : 0;

            gold += lastStageRewardGold;
            gems += lastStageRewardGems;
            summonTickets += lastStageRewardTickets;

            // Advance campaign progression to the next Stage (e.g., 1-1 -> 1-2, 1-10 -> 2-1)
            int nextChap = currentChapter;
            int nextStg = currentStage + 1;
            if (nextStg > StagesPerChapter)
            {
                if (nextChap < TotalChapters)
                {
                    nextChap++;
                    nextStg = 1;
                }
                else
                {
                    nextStg = StagesPerChapter;
                }
            }

            SetStageProgression(nextChap, nextStg);
            UpdateMissionProgress("ev_wave_3", (lastClearedChapter - 1) * StagesPerChapter + lastClearedStage + 1, true);

            isBattlePaused = true;
            Time.timeScale = 1f;
            OnHudStateChanged?.Invoke();

            if (CatFightUIManager.Instance != null)
            {
                CatFightUIManager.Instance.ShowStageVictoryModal();
            }
        }

        private void SpawnWaveEnemy()
        {
            MouseEnemyType chosenType = MouseEnemyType.Normal;
            bool isMidBossStage = (currentStage == 5);
            bool isChapterBossStage = (currentStage >= StagesPerChapter);
            bool isFinalWaveOfStage = (currentWave >= totalWavesInStage);

            // Spawn Stage Leader / Boss at the climax of the final wave!
            if (!bossSpawnedThisWave && isFinalWaveOfStage && enemiesSpawnedInWave >= totalEnemiesInWave - 2)
            {
                if (isMidBossStage || isChapterBossStage)
                {
                    chosenType = MouseEnemyType.Boss;
                    bossSpawnedThisWave = true;
                    AnnounceBoss();
                }
                else if (currentStage >= 3)
                {
                    // Normal stages >= X-3 spawn an Elite Tank Mouse at the end of the final wave
                    chosenType = MouseEnemyType.Tank;
                    bossSpawnedThisWave = true;
                }
            }

            if (chosenType == MouseEnemyType.Normal && !bossSpawnedThisWave)
            {
                float r = UnityEngine.Random.value;
                if (currentChapter == 1)
                {
                    // Chap 1 (1-1 to 1-10): Gradual introduction of enemy types across stages & waves
                    if (currentStage <= 2 && currentWave <= 2)
                    {
                        chosenType = (r < 0.28f) ? MouseEnemyType.Runner : MouseEnemyType.Normal;
                    }
                    else if (currentStage <= 5)
                    {
                        if (r < 0.18f + currentWave * 0.03f) chosenType = MouseEnemyType.Tank;
                        else if (r < 0.52f) chosenType = MouseEnemyType.Runner;
                        else chosenType = MouseEnemyType.Normal;
                    }
                    else
                    {
                        if (r < 0.15f + currentWave * 0.02f) chosenType = MouseEnemyType.Bomber;
                        else if (r < 0.38f) chosenType = MouseEnemyType.Tank;
                        else if (r < 0.68f) chosenType = MouseEnemyType.Runner;
                        else chosenType = MouseEnemyType.Normal;
                    }
                }
                else if (currentChapter == 2)
                {
                    // Chap 2 (2-1 to 2-10, Garden): Faster runners & tougher tanks
                    if (r < 0.18f + currentWave * 0.02f) chosenType = MouseEnemyType.Bomber;
                    else if (r < 0.45f) chosenType = MouseEnemyType.Tank;
                    else if (r < 0.75f) chosenType = MouseEnemyType.Runner;
                    else chosenType = MouseEnemyType.Normal;
                }
                else
                {
                    // Chap 3 (3-1 to 3-10, Sewer): Elite sewer swarm
                    if (r < 0.22f + currentWave * 0.02f) chosenType = MouseEnemyType.Bomber;
                    else if (r < 0.52f) chosenType = MouseEnemyType.Tank;
                    else if (r < 0.80f) chosenType = MouseEnemyType.Runner;
                    else chosenType = MouseEnemyType.Normal;
                }
            }

            SpawnEnemy(chosenType);
        }

        private void AnnounceBoss()
        {
            string bossAlert = currentStage != 5
                ? $"★ {CharacterSpriteBank.BossName(currentChapter).ToUpperInvariant()} ({GetStageCode()}) ★"
                : $"★ {CharacterSpriteBank.BossName(currentChapter).ToUpperInvariant()} — TIỀN ĐỘI ({GetStageCode()}) ★";
            CombatVisualFeedback.SpawnDamagePopup(new Vector3(0f, 2.6f, 0f), bossAlert, new Color(1f, 0.25f, 0.3f), 1.40f);
            if (CatFightAudioManager.Instance != null)
                CatFightAudioManager.Instance.PlayBossBgm();
        }

        private void SpawnEnemy(MouseEnemyType chosenType)
        {
            MouseEnemyDefinition def = enemyDict[chosenType];

            // Stage base HP scale (with gear-check spikes at 1-5, 1-10, 2-5, 2-10, 3-5, 3-10)
            // + Wave ramp inside the stage (+10% per wave)
            float stageBaseScale = GetStageBaseHpScale(currentChapter, currentStage);
            float waveRamp = 1f + (currentWave - 1) * 0.10f;
            float hpScale = stageBaseScale * waveRamp;

            float speedScale;
            if (currentChapter == 1)
                speedScale = 0.90f + (currentStage - 1) * 0.014f + (currentWave - 1) * 0.015f;
            else if (currentChapter == 2)
                speedScale = 1.04f + (currentStage - 1) * 0.015f + (currentWave - 1) * 0.015f;
            else
                speedScale = Mathf.Min(1.38f, 1.16f + (currentStage - 1) * 0.015f + (currentWave - 1) * 0.015f);

            if (chosenType == MouseEnemyType.Boss)
            {
                // Mid-boss at X-5 has 75% of full Boss HP; Finale boss at X-10 has 115% Boss HP!
                hpScale *= currentStage == 5 ? 0.75f : 1.15f;
            }

            float spawnX = HoleXPositions[UnityEngine.Random.Range(0, HoleXPositions.Length)] + UnityEngine.Random.Range(-0.42f, 0.42f);
            Vector3 spawnPos = new Vector3(Mathf.Clamp(spawnX, -2.2f, 2.2f), 4.15f, 0f);

            MouseEnemyController.SpawnFromPool(worldContainer, spawnPos, def, hpScale, speedScale);
        }

        public void OnEnemyKilled(MouseEnemyDefinition def)
        {
            totalKills++;
            lifetimeKills++;
            gold += def.goldReward;
            UpdateMissionProgress("ev_kill_20", 1, false);
            AddExperience(def.expReward);
            OnHudStateChanged?.Invoke();
        }

        public void AddExperience(int amount)
        {
            currentExp += amount;
            if (currentExp >= expToNextLevel)
            {
                currentExp -= expToNextLevel;
                playerLevel++;
                expToNextLevel = Mathf.RoundToInt(expToNextLevel * 1.3f + 20f);

                if (CatFightUIManager.Instance != null)
                {
                    CatFightUIManager.Instance.ShowLevelUpUpgradeModal();
                }
            }
        }

        public void DamageWall(float dmg)
        {
            wallCurrentHp = Mathf.Max(0f, wallCurrentHp - dmg);
            if (CombatVisualFeedback.Instance != null)
                CombatVisualFeedback.Instance.ShakeCamera(0.08f, 0.12f);

            OnHudStateChanged?.Invoke();

            if (wallCurrentHp <= 0f && IsPlaying)
            {
                isBattlePaused = true;
                Time.timeScale = 1f;
                lastBattleWasVictory = false;
                lastClearedChapter = currentChapter;
                lastClearedStage = currentStage;

                // Award consolation training Gold & Gems based on waves/kills achieved so the player can upgrade and beat key stages (1-5, 1-10, etc.)!
                lastStageRewardGold = 45 + (currentWave - 1) * 35 + totalKills * 3 + (currentChapter - 1) * 40;
                lastStageRewardGems = 8 + (currentWave - 1) * 4;
                lastStageRewardTickets = 0;
                gold += lastStageRewardGold;
                gems += lastStageRewardGems;

                if (CatFightUIManager.Instance != null)
                    CatFightUIManager.Instance.ShowGameOverModal();
            }
        }

        public void HealWall(float amount)
        {
            wallCurrentHp = Mathf.Min(wallMaxHp, wallCurrentHp + amount);
            OnHudStateChanged?.Invoke();
        }

        public void ReviveBattleFromAdMob()
        {
            wallCurrentHp = wallMaxHp;
            isBattlePaused = false;
            Time.timeScale = GameSpeedMultiplier;

            // Clear mice crowding the fence and deal massive shockwave!
            for (int i = MouseEnemyController.ActiveMice.Count - 1; i >= 0; i--)
            {
                var m = MouseEnemyController.ActiveMice[i];
                if (m != null && m.IsAlive)
                {
                    m.ApplyStun(1.5f);
                    m.TakeDamage(m.maxHp * 0.65f, true, new Vector2(0f, 3.5f), true);
                }
            }

            CombatVisualFeedback.SpawnImpactRing(new Vector3(0f, DefenseLineY, 0f), new Color(1f, 0.88f, 0.3f), 3.5f, 0.45f);
            CombatVisualFeedback.SpawnDamagePopup(new Vector3(0f, 0f, 0f), "HỒI SINH TƯỜNG 100% HP!", new Color(0.45f, 1f, 0.55f), 1.45f);
            OnHudStateChanged?.Invoke();
        }

        public void ToggleSpeedMultiplier()
        {
            if (GameSpeedMultiplier == 1)
                GameSpeedMultiplier = 2;
            else if (GameSpeedMultiplier == 2)
                GameSpeedMultiplier = 4;
            else
                GameSpeedMultiplier = 1;

            if (IsPlaying)
                Time.timeScale = GameSpeedMultiplier;
            OnHudStateChanged?.Invoke();
        }

        public void ToggleAutoCast()
        {
            AutoCastSkills = !AutoCastSkills;
            OnHudStateChanged?.Invoke();
        }

        public void SetPaused(bool paused)
        {
            isBattlePaused = paused;
            Time.timeScale = paused ? 0f : GameSpeedMultiplier;
        }

        public void UpdateMissionProgress(string missionId, int amount, bool overwriteValue)
        {
            foreach (var m in eventMissions)
            {
                if (m.id == missionId && !m.claimed)
                {
                    if (overwriteValue)
                        m.currentProgress = Mathf.Max(m.currentProgress, amount);
                    else
                        m.currentProgress += amount;
                }
            }
        }

        // ==========================================
        // GACHA ROLL SYSTEM (3 Banners: Cat, Weapon, Skin)
        // ==========================================
        private static Color GetRarityColor(string rarity)
        {
            if (rarity == "UR") return new Color(1f, 0.35f, 0.42f, 1f);
            if (rarity == "SSR") return new Color(1f, 0.84f, 0.25f, 1f);
            return new Color(0.38f, 0.82f, 1f, 1f);
        }

        public List<GachaRollResultItem> PerformGachaRollDetailed(ShopGachaCategory category, int count)
        {
            List<GachaRollResultItem> detailed = new List<GachaRollResultItem>();
            UpdateMissionProgress("ev_roll_gacha", 1, false);
            if (CatFightAudioManager.Instance != null)
                CatFightAudioManager.Instance.PlayGacha();

            for (int i = 0; i < count; i++)
            {
                if (category == ShopGachaCategory.WeaponArsenal)
                {
                    int wIdx = UnityEngine.Random.Range(0, allWeapons.Count);
                    var wpn = allWeapons[wIdx];
                    Sprite gunSp = RuntimeSpriteBank.GetSingle("Sprites/Characters/human_gun", 100f);
                    if (!wpn.unlocked)
                    {
                        wpn.unlocked = true;
                        equippedWeaponIndex = wIdx;
                        detailed.Add(new GachaRollResultItem
                        {
                            rarity = wpn.rarity,
                            itemName = wpn.displayName,
                            subText = "★ MỞ KHÓA MỚI!",
                            logLine = $"★ MỞ KHÓA VŨ KHÍ [{wpn.rarity}]: {wpn.displayName} (Đã tự động trang bị)!",
                            iconSprite = gunSp,
                            iconTint = wpn.tintColor,
                            rarityColor = GetRarityColor(wpn.rarity),
                            isNewUnlock = true
                        });
                    }
                    else
                    {
                        wpn.level++;
                        detailed.Add(new GachaRollResultItem
                        {
                            rarity = wpn.rarity,
                            itemName = wpn.displayName,
                            subText = $"▲ Lên Cấp {wpn.level} (+15% Công)",
                            logLine = $"▲ CƯỜNG HÓA VŨ KHÍ: [{wpn.rarity}] {wpn.displayName} lên Cấp {wpn.level} (+15% Công)!",
                            iconSprite = gunSp,
                            iconTint = wpn.tintColor,
                            rarityColor = GetRarityColor(wpn.rarity),
                            isNewUnlock = false
                        });
                    }
                    ApplyHeroMetaStats();
                }
                else if (category == ShopGachaCategory.SkinWardrobe)
                {
                    if (UnityEngine.Random.value < 0.35f && heroSkins.Count > 1)
                    {
                        int sIdx = UnityEngine.Random.Range(1, heroSkins.Count);
                        var hSkin = heroSkins[sIdx];
                        Sprite heroSp = RuntimeSpriteBank.GetSingle("Sprites/UI/tab_hero", 100f);
                        if (!hSkin.unlocked)
                        {
                            hSkin.unlocked = true;
                            equippedHeroSkinIndex = sIdx;
                            detailed.Add(new GachaRollResultItem
                            {
                                rarity = "SSR",
                                itemName = hSkin.skinName,
                                subText = $"✨ SKIN CHỈ HUY (+{Mathf.RoundToInt(hSkin.damageBonusPct * 100f)}% Công)",
                                logLine = $"✨ SKIN NHÂN VẬT CHÍNH: {hSkin.skinName}!",
                                iconSprite = heroSp,
                                iconTint = hSkin.tintColor,
                                rarityColor = GetRarityColor("SSR"),
                                isNewUnlock = true
                            });
                        }
                        else
                        {
                            heroAtkUpgradeLv++;
                            detailed.Add(new GachaRollResultItem
                            {
                                rarity = "SSR",
                                itemName = hSkin.skinName,
                                subText = $"▲ Hỏa Lực Lv.{heroAtkUpgradeLv}",
                                logLine = $"▲ Trùng Skin Chỉ Huy -> Cường hóa Hỏa Lực lên Cấp {heroAtkUpgradeLv}!",
                                iconSprite = heroSp,
                                iconTint = hSkin.tintColor,
                                rarityColor = GetRarityColor("SSR"),
                                isNewUnlock = false
                            });
                        }
                        ApplyHeroMetaStats();
                    }
                    else
                    {
                        var cat = allCatDefinitions[UnityEngine.Random.Range(0, allCatDefinitions.Count)];
                        int skinIdx = UnityEngine.Random.Range(1, cat.skins.Count);
                        var cSkin = cat.skins[skinIdx];
                        Sprite catSp = RuntimeSpriteBank.GetSingle(cat.iconResourcePath, 100f);
                        if (!cSkin.unlocked)
                        {
                            cSkin.unlocked = true;
                            cat.selectedSkinIndex = skinIdx;
                            detailed.Add(new GachaRollResultItem
                            {
                                rarity = cat.rarity,
                                itemName = $"{cSkin.skinName}",
                                subText = $"✨ Skin {cat.displayName}",
                                logLine = $"✨ MỞ KHÓA SKIN MÈO: {cSkin.skinName} ({cat.displayName})!",
                                iconSprite = catSp,
                                iconTint = cSkin.tintColor,
                                rarityColor = GetRarityColor(cat.rarity),
                                isNewUnlock = true
                            });
                        }
                        else
                        {
                            cat.permanentLevel++;
                            detailed.Add(new GachaRollResultItem
                            {
                                rarity = cat.rarity,
                                itemName = $"{cSkin.skinName}",
                                subText = $"▲ {cat.displayName} Lv.{cat.permanentLevel}",
                                logLine = $"▲ Trùng Skin [{cat.displayName}] -> Tăng cấp vĩnh viễn lên Lv.{cat.permanentLevel}!",
                                iconSprite = catSp,
                                iconTint = cSkin.tintColor,
                                rarityColor = GetRarityColor(cat.rarity),
                                isNewUnlock = false
                            });
                        }
                    }
                }
                else
                {
                    CatCompanionDefinition picked = allCatDefinitions[UnityEngine.Random.Range(0, allCatDefinitions.Count)];
                    Sprite catSp = RuntimeSpriteBank.GetSingle(picked.iconResourcePath, 100f);
                    if (!picked.unlocked)
                    {
                        picked.unlocked = true;
                        picked.shards += 10;
                        detailed.Add(new GachaRollResultItem
                        {
                            rarity = picked.rarity,
                            itemName = picked.displayName,
                            subText = "★ MỞ KHÓA MÈO MỚI!",
                            logLine = $"★ MỞ KHÓA MÈO MỚI: [{picked.rarity}] {picked.displayName}!",
                            iconSprite = catSp,
                            iconTint = picked.GetActiveSkinTint(),
                            rarityColor = GetRarityColor(picked.rarity),
                            isNewUnlock = true
                        });
                    }
                    else
                    {
                        int shardGain = UnityEngine.Random.Range(14, 28);
                        picked.shards += shardGain;
                        if (picked.shards >= 25)
                        {
                            picked.shards -= 25;
                            picked.permanentLevel++;
                            detailed.Add(new GachaRollResultItem
                            {
                                rarity = picked.rarity,
                                itemName = picked.displayName,
                                subText = $"▲ Đột Phá Lv.{picked.permanentLevel} (+{shardGain} Mảnh)",
                                logLine = $"▲ [{picked.rarity}] {picked.displayName} đột phá lên Lv.{picked.permanentLevel} (+{shardGain} Mảnh)!",
                                iconSprite = catSp,
                                iconTint = picked.GetActiveSkinTint(),
                                rarityColor = GetRarityColor(picked.rarity),
                                isNewUnlock = false
                            });
                        }
                        else
                        {
                            detailed.Add(new GachaRollResultItem
                            {
                                rarity = picked.rarity,
                                itemName = picked.displayName,
                                subText = $"+{shardGain} Mảnh Hồn ({picked.shards}/25)",
                                logLine = $"• Nhận +{shardGain} Mảnh Hồn [{picked.displayName}] ({picked.shards}/25)",
                                iconSprite = catSp,
                                iconTint = picked.GetActiveSkinTint(),
                                rarityColor = GetRarityColor(picked.rarity),
                                isNewUnlock = false
                            });
                        }
                    }
                }
            }

            OnHudStateChanged?.Invoke();
            return detailed;
        }

        public List<string> PerformGachaRoll(ShopGachaCategory category, int count)
        {
            var detailed = PerformGachaRollDetailed(category, count);
            List<string> results = new List<string>();
            foreach (var d in detailed)
                results.Add(d.logLine);
            return results;
        }

        public List<RogueliteUpgradeOption> GenerateThreeUpgradeChoices()
        {
            List<RogueliteUpgradeOption> pool = new List<RogueliteUpgradeOption>();

            // 4 Cat Beds in front of the fence hold up to 4 summoned cats; once 4 beds are full, only offer upgrades for those 4 cats!
            foreach (var catDef in allCatDefinitions)
            {
                if (catDef == null || !catDef.unlocked) continue;
                var capturedDef = catDef;
                bool isAlreadyDeployed = deployedCats.Exists(c => c != null && c.definition.id == capturedDef.id);
                if (!isAlreadyDeployed && deployedCats.Count >= CatBedPositions.Length)
                    continue;

                if (isAlreadyDeployed && capturedDef.IsAwakened) continue;
                bool awakens = isAlreadyDeployed && capturedDef.battleStageLevel + 1 == CatCompanionDefinition.MaxBattleStageLevel;
                Sprite iconSp = RuntimeSpriteBank.GetSingle(capturedDef.iconResourcePath, 100f);

                pool.Add(new RogueliteUpgradeOption
                {
                    title = awakens ? $"THỨC TỈNH: {capturedDef.displayName}" : isAlreadyDeployed
                        ? $"Nâng Kỹ Năng: {capturedDef.displayName} (Bậc {capturedDef.battleStageLevel + 1})"
                        : $"TRIỆU HỒI VÀO Ổ MÈO: {capturedDef.displayName} [{capturedDef.rarity}]",
                    subtitle = $"{capturedDef.title} • Skin: {capturedDef.GetActiveSkinName()}",
                    description = awakens ? $"Bậc {CatCompanionDefinition.MaxBattleStageLevel} • {capturedDef.AwakeningDescription}" : capturedDef.description + (isAlreadyDeployed ? "\n[+28% Sát thương & Giảm hồi chiêu]" : $"\n[Triệu hồi ra Ổ Mèo số {deployedCats.Count + 1}/4 phía trước tường rào!]"),
                    cardColor = capturedDef.themeColor,
                    icon = iconSp,
                    onSelect = () => DeployCatCompanion(capturedDef)
                });
            }

            Sprite bulletIcon = RuntimeSpriteBank.GetSingle("Sprites/Projectiles/hero_bullet", 100f);
            pool.Add(new RogueliteUpgradeOption
            {
                title = "Súng Máy Gatling: Thêm Nòng +1",
                subtitle = "Vũ Khí Nhân Vật Chính",
                description = "Tăng thêm +1 tia đạn bắn ra mỗi loạt và tăng 15% tốc độ xả đạn Gatling.",
                cardColor = new Color(0.32f, 0.68f, 1f),
                icon = bulletIcon,
                onSelect = () =>
                {
                    if (hero != null)
                    {
                        hero.multishot = Mathf.Min(7, hero.multishot + 1);
                        hero.fireRate *= 1.15f;
                    }
                }
            });

            pool.Add(new RogueliteUpgradeOption
            {
                title = "Đạn Cường Hóa Xuyên Phá",
                subtitle = "Vũ Khí Nhân Vật Chính",
                description = "Tăng +35% sát thương đạn nhân vật chính và đạn bắn xuyên thêm +1 con chuột.",
                cardColor = new Color(1f, 0.55f, 0.25f),
                icon = bulletIcon,
                onSelect = () =>
                {
                    if (hero != null)
                    {
                        hero.baseDamage *= 1.35f;
                        hero.pierceCount++;
                    }
                }
            });

            // Keep three distinct choices available even after all four cats awaken.
            if (pool.Count < 3)
                pool.Add(new RogueliteUpgradeOption
                {
                    title = "Gia cố phòng tuyến",
                    subtitle = "Tường phòng thủ",
                    description = "Tăng 100 máu tối đa và hồi 100 máu tường trong ván này.",
                    icon = bulletIcon,
                    cardColor = new Color(0.45f, 1f, 0.64f),
                    onSelect = () => { wallMaxHp += 100f; HealWall(100f); }
                });

            List<RogueliteUpgradeOption> result = new List<RogueliteUpgradeOption>();
            while (result.Count < 3 && pool.Count > 0)
            {
                int idx = UnityEngine.Random.Range(0, pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }

            return result;
        }
    }
}
