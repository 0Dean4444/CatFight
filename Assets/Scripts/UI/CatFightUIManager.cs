using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using CatFight.Data;
using CatFight.Controllers;
using CatFight.Managers;
using CatFight.Visuals;

namespace CatFight.UI
{
    public partial class CatFightUIManager : MonoBehaviour
    {
        private static readonly ProfilerMarker RefreshUIMarker = new ProfilerMarker("CatFight.RefreshUI");
        public static CatFightUIManager Instance { get; private set; }

        private Font defaultFont;
        private Sprite roundedPanelSprite;
        private Sprite cardGoldNavySprite;
        private Sprite currencyPillSprite;
        private Sprite tintableTileSprite;
        private Sprite ribbonHeaderSprite;
        private Sprite glossyBtnSprite;
        private Sprite btnAmberSprite;
        private Sprite btnGreenSprite;
        private Sprite btnBlueSprite;
        private Sprite btnPurpleSprite;
        private Sprite btnRedSprite;
        private Sprite whitePixelSprite;
        private Sprite barTrackSprite;
        private Sprite barFillSprite;
        private Sprite speed1Sprite;
        private Sprite speed2Sprite;
        private Sprite speed4Sprite;

        // Initial Loading / Start Screen before entering Main Menu
        private GameObject splashLoadingModalObj;
        private RectTransform splashLogoRt;
        private RectTransform splashBarFillRt;
        private Text splashBarLabelText;
        private GameObject splashBarTrackObj;
        private GameObject splashEnterBtnObj;
        private float splashLoadingProgress;
        private bool isSplashLoadingActive;

        // Stage Entry Loading Screen when entering a Battle Stage
        private GameObject stageLoadingModalObj;
        private Image stageLoadingBgImg;
        private RectTransform stageLoadingIconRt;
        private RectTransform stageLoadingBarFillRt;
        private float stageLoadingProgress;
        private bool isStageLoadingActive;

        // Safe Area & Multi-Aspect Ratio Adapter
        private CanvasScaler mainCanvasScaler;
        private RectTransform safeAreaRoot;
        private Rect lastSafeArea = Rect.zero;
        private Vector2Int lastScreenSize = Vector2Int.zero;

        // Root Layer Containers (Inside SafeArea)
        private GameObject mainMenuLayer;
        private GameObject battleHudLayer;

        // Top Currency Bar (Main Menu)
        private Text menuGoldText;
        private Text menuGemText;
        private Text menuTicketText;
        private Text menuPowerText;

        // 5 Main Menu Tab Panels & Bottom Tab Buttons
        private MainMenuTab activeMenuTab = MainMenuTab.Battle;
        private readonly Dictionary<MainMenuTab, GameObject> tabPanels = new Dictionary<MainMenuTab, GameObject>();
        private readonly Dictionary<MainMenuTab, Graphic> tabButtonImages = new Dictionary<MainMenuTab, Graphic>();
        private readonly Dictionary<MainMenuTab, Image> tabIcons = new Dictionary<MainMenuTab, Image>();
        private readonly Dictionary<MainMenuTab, Text> tabLabelTexts = new Dictionary<MainMenuTab, Text>();

        private readonly Dictionary<ShopGachaCategory, Text> shopResults = new Dictionary<ShopGachaCategory, Text>();
        private ShopGachaCategory rewardCategory;
        private AnimatedNotification notification;
        private CatTapHearts tapHearts;

        // Battle Home Tab Interactive Main Menu Showcase Cat & Stage Start Button
        private Text battleHomeStatsText;
        private Text startBattleBtnTitleText;
        private Text startBattleBtnSubText;

        private enum MenuCatActivity
        {
            SleepingInBed = 0,
            RunningChaseYarn = 1,
            EatingAtBowl = 2,
            ScratchingCatTree = 3,
            SunbathingGrooming = 4,
            ZoomiesAndBackflip = 5,
            PouncePractice = 6
        }

        private RectTransform menuCatStageRt;
        private RectTransform menuCatBedRt;
        private RectTransform menuCatShadowRt;
        private RectTransform menuShowcaseCatRt;
        private Image menuShowcaseCatImg;
        private RectTransform menuToyYarnRt;
        private Image menuToyYarnImg;
        private RectTransform menuFoodKibbleRt1;
        private RectTransform menuFoodKibbleRt2;
        private Text petSetMenuShowcaseBtnText;
        private Image petSetMenuShowcaseBtnImg;
        private Button petSetMenuShowcaseButton;
        private Button petPreviousSkinButton;
        private Button petNextSkinButton;

        private Sprite[] menuCatFrames;
        private string loadedMenuCatId;
        private int loadedMenuCatSkinIdx = -1;
        private MenuCatActivity currentMenuCatActivity = MenuCatActivity.SleepingInBed;
        private float menuActivityElapsed;
        private float menuAnimFrameTimer;
        private int menuAnimFrameIdx;
        private Vector2 menuPounceStartPos;
        private Vector2 menuPounceTargetPos;
        private Vector2 menuNextYarnPos;
        private Vector2 menuYarnCurrentPos;
        private Vector2 menuYarnVelocity;
        private Vector2 menuCatRunPos;
        private float menuYarnRollAngle;
        private float menuYarnBatCooldown;
        private int lastPounceCycle = -1;

        // Pet Tab Dynamic Elements (Thú Cưng Bão Đạn 4-Stat Grid)
        private Transform petGridContainer;
        private int selectedPetIndex = 0;
        private GameObject petInspector;
        private Transform petDetailStars;
        private Button petUpgradeButton;
        private GameObject eventRewardDot;
        private Image petDetailPortrait;
        private Text petDetailTitleText;
        private Text petDetailStatsText;
        private Text petStatDmgText;
        private Text petStatCdText;
        private Text petStatRangeText;
        private Text petStatSkinBuffText;
        private Text petSkinLabelText;
        private Text petUpgradeBtnText;
        private RectTransform petShardFillRt;
        private Text petShardBarText;

        // Character Tab Dynamic Elements (4-Stat Dashboard Grid)
        private Image heroPreviewIcon;
        private Image weaponPreviewIcon;
        private Text heroStatsSummaryText;
        private Text heroStatBarrelBoxText;
        private Text heroWeaponNameText;
        private Text heroSkinNameText;
        private Text heroAtkLvText;
        private Text heroSpdLvText;
        private Text wallHpLvText;

        // Event Tab Dynamic Elements
        private Transform eventMissionListContainer;
        private Text eventHeaderProgressText;
        private RectTransform eventHeaderFillRt;
        private Text eventHeaderBarLabel;

        // Battle HUD Elements (Thú Cưng Bão Đạn Style Clear Stat Tracking)
        private Text waveTitleText;
        private Text levelText;
        private Text battleGoldText;
        private Text battleLiveStatsText;
        private Text killCountText;
        private RectTransform expFillRt;
        private RectTransform wallHpFillRt;
        private Image wallHpFillImg;
        private Text wallHpText;
        private Text autoBtnText;
        private Image autoBtnBg;
        private Text speedBtnText;
        private Image speedBtnIconImg;

        private Transform skillSlotsContainer;
        private readonly List<SkillSlotWidget> skillWidgets = new List<SkillSlotWidget>();

        // Full-Screen Modals
        private GameObject upgradeModalObj;
        private Transform upgradeCardsContainer;
        private List<RogueliteUpgradeOption> displayedUpgradeChoices;
        private GameObject gameOverModalObj;
        private Text gameOverSummaryText;
        private GameObject adReviveBtnObj;
        private GameObject adDoubleStageRewardBtnObj;
        private GameObject defeatButtonsGroupObj;
        private GameObject victoryClaimMenuBtnObj;
        private GameObject settingsModalObj;
        private Slider bgmSlider;
        private Slider sfxSlider;
        private Text bgmValText;
        private Text sfxValText;
        private Text muteBtnText;
        private Image muteBtnBg;

        // Gacha Roll Animation Modal Elements
        private GameObject gachaAnimModalObj;
        private GameObject gachaStageChestGroup;
        private RectTransform gachaSpinCircleRt;
        private RectTransform gachaChestIconRt;
        private Image gachaChestIconImg;
        private Text gachaStatusText;
        private GameObject gachaCardsGroupObj;
        private Transform gachaCardsContainer;
        private GameObject gachaCloseButtonsGroup;
        private Image gachaFlashImg;
        private Coroutine activeGachaAnimCoroutine;

        private class SkillSlotWidget
        {
            public CatCompanionController linkedCat;
            public GameObject rootObj;
            public Image iconImg;
            public Image cooldownOverlay;
            public Text cooldownText;
            public Text levelBadgeText;
        }

        public static void EnsureUIExists()
        {
            if (Instance != null) return;
            GameObject uiRoot = new GameObject("CatFight_UICanvas");
            Instance = uiRoot.AddComponent<CatFightUIManager>();
            Instance.BuildCompleteUI();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnHudStateChanged += RefreshAllUI;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnHudStateChanged -= RefreshAllUI;
        }

        private void Update()
        {
            UpdateSafeAreaIfChanged();

            if (isSplashLoadingActive)
            {
                UpdateSplashLoadingScreen(Time.unscaledDeltaTime);
            }

            if (isStageLoadingActive)
            {
                UpdateStageLoadingScreen(Time.unscaledDeltaTime);
            }

            if (GameManager.Instance == null)
                return;

            if (GameManager.Instance.CurrentAppState == GameAppState.MainMenu)
            {
                if (activeMenuTab == MainMenuTab.Battle)
                    UpdateMenuCatShowcase(Time.unscaledDeltaTime);
                return;
            }

            if (GameManager.Instance.CurrentAppState != GameAppState.Battle)
                return;

            for (int i = 0; i < skillWidgets.Count; i++)
            {
                var w = skillWidgets[i];
                if (w.linkedCat == null || w.linkedCat.definition == null) continue;
                float ratio = w.linkedCat.CooldownRatio;
                if (w.cooldownOverlay != null)
                    w.cooldownOverlay.fillAmount = ratio;
                if (w.cooldownText != null)
                {
                    w.cooldownText.text = w.linkedCat.cooldownRemaining > 0.1f
                        ? $"{w.linkedCat.cooldownRemaining:0.0}s"
                        : "SẴN SÀNG";
                }
            }
        }

        private void UpdateSafeAreaIfChanged()
        {
            if (safeAreaRoot == null) return;
            Rect safe = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
            if (safe == lastSafeArea && screenSize == lastScreenSize)
                return;

            lastSafeArea = safe;
            lastScreenSize = screenSize;

            if (Screen.width <= 0 || Screen.height <= 0) return;

            // Dynamic Aspect-Ratio Scaling:
            // - For phones (aspect <= 9:16), match width (0f) so 1080 width is preserved.
            // - For wider screens / tablets (aspect > 9:16), match height (1f) so 1920 height is preserved.
            if (mainCanvasScaler != null)
            {
                float screenAspect = (float)Screen.width / Mathf.Max(1f, Screen.height);
                float refAspect = 1080f / 1920f;
                mainCanvasScaler.matchWidthOrHeight = (screenAspect > refAspect) ? 1.0f : 0.0f;
            }

            Vector2 anchorMin = safe.position;
            Vector2 anchorMax = safe.position + safe.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            // Top: clear the camera notch / Dynamic Island
            anchorMax.y = Mathf.Min(anchorMax.y, 0.962f);

            // Bottom: on curved-bezel devices (iPhone / modern Android where safe.position.y > 0),
            // lift the bottom anchor enough (~2.6%–3.2%) so the bottom-left and bottom-right rounded corners
            // never get clipped by the device bezel curve, while keeping flat-bezel screens snug (~1.4%).
            if (anchorMin.y > 0.005f)
                anchorMin.y = Mathf.Clamp(anchorMin.y * 0.72f, 0.026f, 0.034f);
            else
                anchorMin.y = 0.014f;

            // Horizontal: enforce a consistent side inset so curved device corners never clip left/right UI borders
            anchorMin.x = Mathf.Max(anchorMin.x, 0.018f);
            anchorMax.x = Mathf.Min(anchorMax.x, 0.982f);

            safeAreaRoot.anchorMin = anchorMin;
            safeAreaRoot.anchorMax = anchorMax;
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
        }

        public void BuildCompleteUI()
        {
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
                defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 24);

            roundedPanelSprite = CreateSlicedSprite("Sprites/UI/ui_rounded_panel", 34);
            cardGoldNavySprite = CreateSlicedSprite("Sprites/UI/ui_card_gold_navy", 34);
            tintableTileSprite = CreateModernPillSprite(Color.white, new Color(0.94f, 0.94f, 0.94f), Color.white);
            currencyPillSprite = CreateModernPillSprite(
                new Color(0.11f, 0.15f, 0.27f, 0.94f),
                new Color(0.07f, 0.10f, 0.19f, 0.94f),
                new Color(0.32f, 0.44f, 0.68f, 0.95f)
            );
            ribbonHeaderSprite = CreateSlicedSprite("Sprites/UI/ui_ribbon_header", 32);

            glossyBtnSprite = CreateModernButtonSprite(
                new Color(0.34f, 0.40f, 0.56f, 1f),
                new Color(0.22f, 0.27f, 0.40f, 1f),
                new Color(0.13f, 0.16f, 0.26f, 1f)
            );
            btnAmberSprite = CreateModernButtonSprite(
                new Color(1.00f, 0.78f, 0.22f, 1f),
                new Color(0.95f, 0.52f, 0.10f, 1f),
                new Color(0.70f, 0.32f, 0.04f, 1f)
            );
            btnGreenSprite = CreateModernButtonSprite(
                new Color(0.22f, 0.88f, 0.46f, 1f),
                new Color(0.10f, 0.68f, 0.32f, 1f),
                new Color(0.05f, 0.45f, 0.20f, 1f)
            );
            btnBlueSprite = CreateModernButtonSprite(
                new Color(0.24f, 0.72f, 1.00f, 1f),
                new Color(0.10f, 0.48f, 0.92f, 1f),
                new Color(0.05f, 0.30f, 0.66f, 1f)
            );
            btnPurpleSprite = CreateModernButtonSprite(
                new Color(0.64f, 0.42f, 1.00f, 1f),
                new Color(0.44f, 0.24f, 0.88f, 1f),
                new Color(0.27f, 0.12f, 0.62f, 1f)
            );
            btnRedSprite = CreateModernButtonSprite(
                new Color(1.00f, 0.36f, 0.44f, 1f),
                new Color(0.86f, 0.18f, 0.28f, 1f),
                new Color(0.58f, 0.09f, 0.18f, 1f)
            );

            whitePixelSprite = RuntimeSpriteBank.GetSingle("Sprites/UI/ui_white_pixel", 32f);
            barTrackSprite = CreateSlicedSprite("Sprites/UI/ui_bar_track", 20);
            barFillSprite = CreateSlicedSprite("Sprites/UI/ui_bar_fill", 16);
            speed1Sprite = RuntimeSpriteBank.GetSingle("Sprites/UI/icon_speed_1", 100f);
            speed2Sprite = RuntimeSpriteBank.GetSingle("Sprites/UI/icon_speed_2", 100f);
            speed4Sprite = RuntimeSpriteBank.GetSingle("Sprites/UI/icon_speed_4", 100f);

            EnsureEventSystem();

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            mainCanvasScaler = gameObject.AddComponent<CanvasScaler>();
            mainCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            mainCanvasScaler.referenceResolution = new Vector2(1080f, 1920f);
            mainCanvasScaler.matchWidthOrHeight = 0.0f;

            gameObject.AddComponent<GraphicRaycaster>();

            GameObject safeGo = new GameObject("SafeAreaContainer", typeof(RectTransform));
            safeGo.transform.SetParent(transform, false);
            safeAreaRoot = safeGo.GetComponent<RectTransform>();
            StretchFull(safeAreaRoot);
            UpdateSafeAreaIfChanged();

            BuildMainMenuLayer(safeAreaRoot);
            BuildBattleHudLayer(safeAreaRoot);

            BuildLevelUpModal(transform);
            BuildGameOverModal(transform);
            BuildSettingsModal(transform);
            BuildGachaAnimationModal(transform);
            BuildStageLoadingScreen(transform);
            BuildSplashLoadingScreen(transform);
            BuildNotificationLayer();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnHudStateChanged -= RefreshAllUI;
                GameManager.Instance.OnHudStateChanged += RefreshAllUI;
            }

            ShowMainMenu();
            ShowSplashLoadingScreen();
        }

        private void BuildNotificationLayer()
        {
            GameObject layer = new GameObject("NotificationLayer", typeof(RectTransform), typeof(Canvas));
            layer.transform.SetParent(transform, false);
            StretchFull(layer.GetComponent<RectTransform>());
            Canvas overlay = layer.GetComponent<Canvas>();
            overlay.overrideSorting = true;
            overlay.sortingOrder = 200;

            GameObject hearts = new GameObject("CatTapHearts", typeof(RectTransform), typeof(CatTapHearts));
            hearts.transform.SetParent(layer.transform, false);
            tapHearts = hearts.GetComponent<CatTapHearts>();
            StretchFull(tapHearts.rectTransform);
            tapHearts.raycastTarget = false;

            Text message = CreateOverlayText(layer.transform, "Notification", 42, Color.white,
                new Vector2(0.5f, 0.5f), 0f, new Vector2(920f, 100f));
            message.supportRichText = false;
            notification = message.gameObject.AddComponent<AnimatedNotification>();
            notification.Initialize(message);
        }

        public void PetCat(CatCompanionDefinition cat, Vector2 screenPoint)
        {
            if (cat == null || !cat.unlocked || tapHearts == null) return;
            tapHearts.ShowAt(screenPoint);
            CatFightAudioManager.Instance?.PlayCatVoice(cat.id);
        }

        public void ShowSummonNotice(CatCompanionDefinition cat)
        {
            notification?.Show($"Triệu hồi {cat.displayName}!", new Color(1f, 0.91f, 0.52f), 1.6f);
        }

        public void ShowNotification(string message, bool isError = false)
        {
            if (notification != null)
                notification.Show(message, isError ? new Color(1f, 0.55f, 0.50f) : new Color(1f, 0.91f, 0.52f));
        }

        private Sprite CreateSlicedSprite(string resourcePath, int borderPx)
        {
            Texture2D tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null)
                return RuntimeSpriteBank.GetSingle("Sprites/UI/circle_white", 64f);

            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(borderPx, borderPx, borderPx, borderPx)
            );
        }

        private static float SdfRoundedBox(Vector2 p, Vector2 halfSize, float radius)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - halfSize + new Vector2(radius, radius);
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        private Sprite CreateModernButtonSprite(Color topColor, Color bottomColor, Color lipColor)
        {
            int w = 64;
            int h = 48;
            float radius = 12f;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[w * h];
            Vector2 center = new Vector2((w - 1) * 0.5f, (h - 1) * 0.5f);
            Vector2 halfSize = new Vector2(w * 0.5f - 1.5f, h * 0.5f - 1.5f);
            Color outerBorderCol = new Color(0.06f, 0.09f, 0.16f, 0.96f);
            Color topHighlightCol = Color.Lerp(topColor, Color.white, 0.45f);

            for (int y = 0; y < h; y++)
            {
                float vNorm = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x, y) - center;
                    float dist = SdfRoundedBox(p, halfSize, radius);

                    if (dist > 0.7f)
                    {
                        pixels[y * w + x] = Color.clear;
                        continue;
                    }

                    float outerAlpha = Mathf.Clamp01(0.6f - dist);
                    // Inner face SDF shifted up slightly to form the 3D bottom lip
                    float innerDist = SdfRoundedBox(p - new Vector2(0f, 1.8f), halfSize - new Vector2(1.8f, 3.6f), Mathf.Max(4f, radius - 2.0f));
                    float rimDist = SdfRoundedBox(p, halfSize - new Vector2(1.6f, 1.6f), Mathf.Max(4f, radius - 1.6f));

                    Color col;
                    if (rimDist > 0f)
                    {
                        col = outerBorderCol;
                    }
                    else if (innerDist > 0f)
                    {
                        // Bottom 3D lip or top crisp inner highlight
                        col = y < h * 0.45f ? lipColor : topHighlightCol;
                    }
                    else
                    {
                        col = Color.Lerp(bottomColor, topColor, Mathf.SmoothStep(0f, 1f, vNorm));
                        // Subtle upper-half modern sheen
                        if (vNorm > 0.58f && innerDist > -2.8f)
                            col = Color.Lerp(col, topHighlightCol, 0.38f);
                    }

                    col.a *= outerAlpha;
                    pixels[y * w + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            int border = 14;
            return Sprite.Create(
                tex,
                new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border)
            );
        }

        private Sprite CreateModernPillSprite(Color topColor, Color bottomColor, Color rimColor)
        {
            int w = 64;
            int h = 44;
            float radius = 12f;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[w * h];
            Vector2 center = new Vector2((w - 1) * 0.5f, (h - 1) * 0.5f);
            Vector2 halfSize = new Vector2(w * 0.5f - 1.5f, h * 0.5f - 1.5f);
            Color outerDarkCol = new Color(0.04f, 0.06f, 0.12f, 0.96f);

            for (int y = 0; y < h; y++)
            {
                float vNorm = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x, y) - center;
                    float dist = SdfRoundedBox(p, halfSize, radius);

                    if (dist > 0.7f)
                    {
                        pixels[y * w + x] = Color.clear;
                        continue;
                    }

                    float outerAlpha = Mathf.Clamp01(0.6f - dist);
                    Color col;
                    if (dist > -1.5f)
                        col = outerDarkCol;
                    else if (dist > -3.0f)
                        col = rimColor;
                    else
                        col = Color.Lerp(bottomColor, topColor, vNorm);

                    col.a *= outerAlpha;
                    pixels[y * w + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            int border = 14;
            return Sprite.Create(
                tex,
                new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border)
            );
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }
        }

        public void ShowMainMenu()
        {
            if (notification != null) notification.gameObject.SetActive(false);
            if (tapHearts != null) tapHearts.gameObject.SetActive(false);
            isStageLoadingActive = false;
            if (stageLoadingModalObj != null) stageLoadingModalObj.SetActive(false);
            if (mainMenuLayer != null) mainMenuLayer.SetActive(true);
            if (battleHudLayer != null) battleHudLayer.SetActive(false);
            if (upgradeModalObj != null) upgradeModalObj.SetActive(false);
            if (gameOverModalObj != null) gameOverModalObj.SetActive(false);
            if (settingsModalObj != null) settingsModalObj.SetActive(false);
            if (gachaAnimModalObj != null) gachaAnimModalObj.SetActive(false);

            RollRandomMenuCatActivity(true);
            SwitchMainMenuTab(activeMenuTab);
            RefreshAllUI();
        }

        public void ShowBattleHUD()
        {
            if (notification != null) notification.gameObject.SetActive(false);
            if (tapHearts != null) tapHearts.gameObject.SetActive(false);
            if (mainMenuLayer != null) mainMenuLayer.SetActive(false);
            if (battleHudLayer != null) battleHudLayer.SetActive(true);
            if (upgradeModalObj != null) upgradeModalObj.SetActive(false);
            if (gameOverModalObj != null) gameOverModalObj.SetActive(false);
            if (settingsModalObj != null) settingsModalObj.SetActive(false);
            if (gachaAnimModalObj != null) gachaAnimModalObj.SetActive(false);

            if (GameManager.Instance != null)
                GameManager.Instance.SetCombatActorsVisible(true);

            RefreshSkillSlotBar();
            RefreshAllUI();
        }

        // ====================================================================
        // PART 1: MAIN MENU HUB (Supercell-style Gold/Navy Cards + 3D Buttons)
        // ====================================================================
        private void BuildMainMenuLayer(Transform parent)
        {
            mainMenuLayer = new GameObject("MainMenuLayer", typeof(RectTransform));
            mainMenuLayer.transform.SetParent(parent, false);
            StretchFull(mainMenuLayer.GetComponent<RectTransform>());

            // Dedicated Full-Bleed Main Menu Background Image (Cozy Sunny Cat Cafe Living Room!)
            GameObject menuBgGo = new GameObject("MainMenuBackground", typeof(RectTransform), typeof(Image));
            menuBgGo.transform.SetParent(mainMenuLayer.transform, false);
            RectTransform menuBgRt = menuBgGo.GetComponent<RectTransform>();
            menuBgRt.anchorMin = Vector2.zero;
            menuBgRt.anchorMax = Vector2.one;
            menuBgRt.offsetMin = new Vector2(-120f, -120f);
            menuBgRt.offsetMax = new Vector2(120f, 120f);
            Image menuBgImg = menuBgGo.GetComponent<Image>();
            menuBgImg.sprite = RuntimeSpriteBank.GetSingle("Sprites/Environment/bg_main_menu", 100f);
            menuBgImg.type = Image.Type.Simple;
            menuBgImg.color = Color.white;
            menuBgImg.raycastTarget = false;

            // Build the 5 Tab Content Panels first so Full-Screen tabs cover the background edge-to-edge while Top Header & Bottom Dock stay on top!
            tabPanels.Clear();
            tabPanels[MainMenuTab.Battle] = BuildBattleHomeTab(mainMenuLayer.transform);
            tabPanels[MainMenuTab.Shop] = BuildShopGachaTab(mainMenuLayer.transform);
            tabPanels[MainMenuTab.Pet] = BuildPetSkinAndUpgradeTab(mainMenuLayer.transform);
            tabPanels[MainMenuTab.Character] = BuildHeroCharacterTab(mainMenuLayer.transform);
            tabPanels[MainMenuTab.Event] = BuildEventMissionsTab(mainMenuLayer.transform);

            // Separate floating tiles; no enclosing header frame.
            GameObject topBar = new GameObject("MenuTopHeader", typeof(RectTransform));
            topBar.transform.SetParent(mainMenuLayer.transform, false);
            RectTransform headerRt = topBar.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = Vector2.one;
            headerRt.anchoredPosition = new Vector2(0f, -76f);
            headerRt.sizeDelta = new Vector2(-16f, 108f);
            HorizontalLayoutGroup headerLayout = topBar.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 16f;
            headerLayout.childControlWidth = headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = headerLayout.childForceExpandHeight = true;

            // Left: Enlarged Commander Badge
            GameObject powerPill = CreateCurrencyPill(topBar.transform, "CommanderPill", new Vector2(0f, 0.5f), new Vector2(168f, 0f), new Vector2(298f, 100f));
            CreateIconImage(powerPill.transform, "HeroIcon", "Sprites/UI/app_icon", new Vector2(0f, 0.5f), new Vector2(48f, 0f), new Vector2(76f, 76f));
            menuPowerText = CreateLabel(powerPill.transform, "CHỈ HUY\n<color=#6BFF8B>LC: 1,250</color>", 30, Color.white, TextAnchor.MiddleLeft, new Vector2(90f, 6f), new Vector2(-10f, -6f));

            menuGoldText = CreateResourceTile(topBar.transform, "GoldPill", "Sprites/UI/icon_coin", 188f,
                new Color32(255, 213, 128, 255), new Color32(64, 40, 13, 255));
            menuGemText = CreateResourceTile(topBar.transform, "GemPill", "Sprites/UI/icon_gem", 188f,
                new Color32(151, 224, 239, 255), new Color32(17, 58, 75, 255));
            menuTicketText = CreateResourceTile(topBar.transform, "TicketPill", "Sprites/UI/icon_gacha_capsule", 148f,
                new Color32(248, 181, 208, 255), new Color32(78, 32, 56, 255));

            // Far Right: Enlarged 3D Blue Settings Gear Button
            Button setBtn = Create3DButton(topBar.transform, "SettingsBtn", new Vector2(1f, 0.5f), new Vector2(-68f, 0f), new Vector2(102f, 102f), btnBlueSprite, "", 18, () => ShowSettingsModal());
            CreateIconImage(setBtn.transform, "GearIcon", "Sprites/UI/icon_settings", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 64f));

            foreach (RectTransform tile in topBar.transform)
            {
                LayoutElement slot = tile.gameObject.AddComponent<LayoutElement>();
                slot.flexibleWidth = tile.sizeDelta.x;
                slot.minWidth = 0f;
            }

            BuildBottomNavigationDock(mainMenuLayer.transform);
        }

        private Text CreateResourceTile(Transform parent, string name, string iconPath, float width, Color fill, Color ink)
        {
            GameObject tile = CreateCurrencyPill(parent, name, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, 92f));
            Image background = tile.GetComponent<Image>();
            background.sprite = tintableTileSprite;
            background.color = fill;
            CreateIconImage(tile.transform, "Icon", iconPath, new Vector2(0f, 0.5f), new Vector2(36f, 0f), new Vector2(56f, 56f));
            Text amount = CreateLabel(tile.transform, "", 36, ink, TextAnchor.MiddleCenter, new Vector2(63f, 4f), new Vector2(-8f, -4f));
            amount.GetComponent<Shadow>().enabled = false;
            amount.horizontalOverflow = HorizontalWrapMode.Wrap;
            amount.resizeTextForBestFit = true;
            amount.resizeTextMinSize = 15;
            amount.resizeTextMaxSize = amount.fontSize;
            return amount;
        }

        private void BuildBottomNavigationDock(Transform parent)
        {
            // Container anchored at the bottom of the screen
            GameObject navBar = new GameObject("BottomNavDock", typeof(RectTransform));
            navBar.transform.SetParent(parent, false);
            RectTransform navRt = navBar.GetComponent<RectTransform>();
            navRt.anchorMin = new Vector2(0f, 0f);
            navRt.anchorMax = new Vector2(1f, 0f);
            navRt.pivot = new Vector2(0.5f, 0f);
            navRt.anchoredPosition = Vector2.zero;
            navRt.sizeDelta = new Vector2(0f, 178f);

            GameObject dock = new GameObject("CatLoungeDock", typeof(RectTransform), typeof(CatDockGraphic));
            dock.transform.SetParent(navBar.transform, false);
            RectTransform dockRt = dock.GetComponent<RectTransform>();
            StretchFull(dockRt);
            dockRt.offsetMin = new Vector2(16f, 8f);
            dockRt.offsetMax = new Vector2(-16f, -2f);
            dock.GetComponent<CatDockGraphic>().raycastTarget = false;

            // The five equal-width hit areas include both the glyph and its label.
            GameObject rowGo = new GameObject("NavButtonsRow", typeof(RectTransform));
            rowGo.transform.SetParent(navBar.transform, false);
            StretchFull(rowGo.GetComponent<RectTransform>());

            HorizontalLayoutGroup hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.padding = new RectOffset(31, 31, 13, 23);
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            tabButtonImages.Clear();
            tabIcons.Clear();
            tabLabelTexts.Clear();

            Sprite[] glyphs = RuntimeSpriteBank.GetHorizontalSheet("Sprites/UI/navigation_glyphs", 5);
            CreateNavTabButton(rowGo.transform, MainMenuTab.Shop, "Cửa hàng", glyphs);
            CreateNavTabButton(rowGo.transform, MainMenuTab.Pet, "Mèo cưng", glyphs);
            CreateNavTabButton(rowGo.transform, MainMenuTab.Battle, "Chiến đấu", glyphs);
            CreateNavTabButton(rowGo.transform, MainMenuTab.Character, "Nhân vật", glyphs);
            CreateNavTabButton(rowGo.transform, MainMenuTab.Event, "Sự kiện", glyphs);
        }

        private void CreateNavTabButton(Transform parent, MainMenuTab tab, string label, Sprite[] glyphs)
        {
            GameObject btnObj = new GameObject($"Tab_{tab}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            btnObj.GetComponent<Image>().color = Color.clear;

            GameObject selected = new GameObject("SelectedTile", typeof(RectTransform), typeof(CatDockGraphic));
            selected.transform.SetParent(btnObj.transform, false);
            RectTransform tile = selected.GetComponent<RectTransform>();
            StretchFull(tile);
            tile.offsetMin = new Vector2(1f, 0f);
            tile.offsetMax = new Vector2(-1f, 0f);
            CatDockGraphic plate = selected.GetComponent<CatDockGraphic>();
            plate.selectedTile = true;
            plate.raycastTarget = false;
            tabButtonImages[tab] = plate;

            Image stamp = CreateIconImage(tile, "PawStamp", null, new Vector2(0f, 1f),
                new Vector2(28f, -28f), new Vector2(31f, 31f));
            stamp.sprite = glyphs[(int)MainMenuTab.Pet];
            stamp.color = new Color32(140, 82, 36, 110);
            stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 18f);

            Image icon = CreateIconImage(btnObj.transform, "Icon", null, new Vector2(0.5f, 0.5f),
                new Vector2(0f, 23f), new Vector2(72f, 72f));
            icon.sprite = glyphs[(int)tab];
            tabIcons[tab] = icon;

            if (tab == MainMenuTab.Event)
            {
                Image dot = CreateIconImage(icon.transform, "ClaimableRewardDot", "Sprites/UI/circle_white",
                    new Vector2(1f, 1f), new Vector2(3f, -2f), new Vector2(22f, 22f));
                dot.color = new Color32(255, 91, 105, 255);
                eventRewardDot = dot.gameObject;
            }

            Text caption = CreateLabel(btnObj.transform, label, 28, Color.white, TextAnchor.MiddleCenter);
            caption.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            caption.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            caption.rectTransform.anchoredPosition = new Vector2(0f, -37f);
            caption.rectTransform.sizeDelta = new Vector2(-4f, 46f);
            caption.GetComponent<Shadow>().enabled = false;
            tabLabelTexts[tab] = caption;

            btnObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (CatFightAudioManager.Instance != null) CatFightAudioManager.Instance.PlayUiClick();
                SwitchMainMenuTab(tab);
            });
        }

        public void SwitchMainMenuTab(MainMenuTab tab)
        {
            activeMenuTab = tab;
            if (petInspector != null) petInspector.SetActive(false);
            foreach (var kv in tabPanels)
            {
                if (kv.Value != null)
                    kv.Value.SetActive(kv.Key == tab);
            }

            foreach (var entry in tabButtonImages)
            {
                bool active = entry.Key == tab;
                entry.Value.gameObject.SetActive(active);
                Color ink = active ? new Color32(64, 43, 35, 255) : new Color32(229, 227, 239, 255);
                tabIcons[entry.Key].color = ink;
                tabLabelTexts[entry.Key].color = ink;
            }

            if (GameManager.Instance != null)
                GameManager.Instance.SetCombatActorsVisible(GameManager.Instance.CurrentAppState == GameAppState.Battle);

            RefreshAllUI();
        }

        // --------------------------------------------------------------------
        // TAB 1: BATTLE HOME TAB (Interactive Main Menu Cat Playground)
        // --------------------------------------------------------------------
        private GameObject BuildBattleHomeTab(Transform parent)
        {
            GameObject panel = CreateEmptyTabContainer(parent, "BattleHomeTab");

            // 0. Interactive Living Room Cat Playground (Rendered behind UI buttons)
            BuildMenuCatShowcaseStage(panel.transform);

            // Floating AdMob Free Supply Button on Top-Right
            Button adBonusBtn = Create3DButton(panel.transform, "QuickAdMobBonusBtn", new Vector2(1f, 1f), new Vector2(-135f, -92f), new Vector2(236f, 112f), btnPurpleSprite, "", 20, () =>
            {
                if (GoogleAdMobManager.Instance != null)
                {
                    GoogleAdMobManager.Instance.ShowRewardedAd("Quà Tiếp Tế Sảnh", "+300 Kim Cương & +500 Vàng", () =>
                    {
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.gems += 300;
                            GameManager.Instance.gold += 500;
                            RefreshAllUI();
                        }
                    });
                }
            });
            CreateIconImage(adBonusBtn.transform, "AdVideoIcon", "Sprites/UI/icon_ad_video", new Vector2(0f, 0.5f), new Vector2(44f, 4f), new Vector2(64f, 64f));
            CreateLabel(adBonusBtn.transform, "XEM AD\n<size=23><color=#FFE57F>+300 KC</color></size>", 22, Color.white, TextAnchor.MiddleLeft, new Vector2(82f, 8f), new Vector2(-8f, -6f));

            // Standalone 3D Start Battle Button (Dynamically displays Current Stage 1-1 -> 1-2 ... & Recommended Power)
            Button startBtn = Create3DButton(panel.transform, "StartBattleBtn", new Vector2(0.5f, 0f), new Vector2(0f, 74f), new Vector2(680f, 124f), btnAmberSprite,
                "", 30, () =>
                {
                    if (CatFightAudioManager.Instance != null) CatFightAudioManager.Instance.PlayUiClick();
                    if (GameManager.Instance != null)
                        GameManager.Instance.StartBattleFromMainMenu();
                });
            startBattleBtnTitleText = CreateLabel(startBtn.transform, "⚔ CHIẾN ĐẤU: MÀN 1-1 ⚔", 29, Color.white, TextAnchor.UpperCenter, new Vector2(14f, 16f), new Vector2(-14f, -14f));
            startBattleBtnSubText = CreateLabel(startBtn.transform, "PHÒNG BẾP • 4 Đợt Quái • LC Đề Xuất: 170", 20, new Color(1f, 0.96f, 0.78f), TextAnchor.LowerCenter, new Vector2(14f, 18f), new Vector2(-14f, -12f));

            return panel;
        }

        private void BuildMenuCatShowcaseStage(Transform parent)
        {
            GameObject stageGo = new GameObject("MenuCatStage", typeof(RectTransform));
            stageGo.transform.SetParent(parent, false);
            menuCatStageRt = stageGo.GetComponent<RectTransform>();
            StretchFull(menuCatStageRt);

            // 1. Cozy Plush Cat Bed ("Ổ Mèo") placed right in the center of the pink living-room rug
            Image bedImg = CreateIconImage(menuCatStageRt, "MenuCenterCatBed", "Sprites/Environment/cat_bed", new Vector2(0.5f, 0.5f), new Vector2(0f, -385f), new Vector2(316f, 214f));
            menuCatBedRt = bedImg.GetComponent<RectTransform>();

            // 2. Soft Oval Ground Shadow beneath the Showcase Cat
            Image shadowImg = CreateIconImage(menuCatStageRt, "MenuCatShadow", "Sprites/UI/circle_white", new Vector2(0.5f, 0.5f), new Vector2(0f, -405f), new Vector2(150f, 44f));
            shadowImg.preserveAspect = false;
            shadowImg.color = new Color(0.12f, 0.08f, 0.05f, 0.34f);
            menuCatShadowRt = shadowImg.GetComponent<RectTransform>();

            // 3. Food Bowl Kibble Props (Shown when Cat is eating at the right-side bowl)
            Image kib1 = CreateIconImage(menuCatStageRt, "MenuFoodKibble1", "Sprites/Projectiles/hero_bullet", new Vector2(0.5f, 0.5f), new Vector2(420f, -6f), new Vector2(48f, 48f));
            menuFoodKibbleRt1 = kib1.GetComponent<RectTransform>();
            Image kib2 = CreateIconImage(menuCatStageRt, "MenuFoodKibble2", "Sprites/Projectiles/hero_bullet", new Vector2(0.5f, 0.5f), new Vector2(448f, -12f), new Vector2(42f, 42f));
            menuFoodKibbleRt2 = kib2.GetComponent<RectTransform>();

            // 4. Animated Showcase Cat
            GameObject catGo = new GameObject("MenuShowcaseCat", typeof(RectTransform), typeof(Image));
            catGo.transform.SetParent(menuCatStageRt, false);
            menuShowcaseCatRt = catGo.GetComponent<RectTransform>();
            menuShowcaseCatRt.anchorMin = new Vector2(0.5f, 0.5f);
            menuShowcaseCatRt.anchorMax = new Vector2(0.5f, 0.5f);
            menuShowcaseCatRt.anchoredPosition = new Vector2(0f, -342f);
            menuShowcaseCatRt.sizeDelta = new Vector2(228f, 228f);

            menuShowcaseCatImg = catGo.GetComponent<Image>();
            menuShowcaseCatImg.preserveAspect = true;
            menuShowcaseCatImg.raycastTarget = true;
            Button petCat = catGo.AddComponent<Button>();
            petCat.transition = Selectable.Transition.None;
            petCat.onClick.AddListener(() => PetCat(GameManager.Instance?.GetMenuShowcaseCat(), menuShowcaseCatRt.position));

            // 5. Rolling Yarn Ball Toy Prop (Shown when Cat is running & playing with yarn)
            Image yarnImg = CreateIconImage(menuCatStageRt, "MenuToyYarnBall", "Sprites/Projectiles/yarn_ball", new Vector2(0.5f, 0.5f), new Vector2(120f, -350f), new Vector2(82f, 82f));
            menuToyYarnRt = yarnImg.GetComponent<RectTransform>();
            menuToyYarnImg = yarnImg;

            RollRandomMenuCatActivity(false);
        }

        public void RollRandomMenuCatActivity(bool avoidSame)
        {
            int totalActivities = 7;
            MenuCatActivity next = (MenuCatActivity)Random.Range(0, totalActivities);
            if (avoidSame && totalActivities > 1)
            {
                int safety = 0;
                while (next == currentMenuCatActivity && safety < 10)
                {
                    next = (MenuCatActivity)Random.Range(0, totalActivities);
                    safety++;
                }
            }

            currentMenuCatActivity = next;
            menuActivityElapsed = 0f;
            lastPounceCycle = 0;
            menuPounceStartPos = new Vector2(-180f, -355f);
            menuPounceTargetPos = new Vector2(180f, -335f);
            menuNextYarnPos = new Vector2(-175f, -265f);
            menuCatRunPos = new Vector2(-95f, -345f);
            menuYarnCurrentPos = (next == MenuCatActivity.PouncePractice)
                ? (menuPounceTargetPos + new Vector2(0f, -24f))
                : new Vector2(85f, -345f);
            menuYarnVelocity = new Vector2(260f, 65f);
            menuYarnBatCooldown = 0f;
            if (menuToyYarnRt != null)
                menuToyYarnRt.anchoredPosition = menuYarnCurrentPos;

            UpdateMenuCatActivityObjectsVisibility();
            RefreshMenuCatShowcaseUI();
        }

        private void UpdateMenuCatActivityObjectsVisibility()
        {
            if (menuToyYarnRt != null)
                menuToyYarnRt.gameObject.SetActive(currentMenuCatActivity == MenuCatActivity.RunningChaseYarn || currentMenuCatActivity == MenuCatActivity.PouncePractice);

            bool showKibble = currentMenuCatActivity == MenuCatActivity.EatingAtBowl;
            if (menuFoodKibbleRt1 != null) menuFoodKibbleRt1.gameObject.SetActive(showKibble);
            if (menuFoodKibbleRt2 != null) menuFoodKibbleRt2.gameObject.SetActive(showKibble);
        }

        private void RefreshMenuCatShowcaseUI()
        {
            if (GameManager.Instance == null || menuShowcaseCatImg == null) return;
            var catDef = GameManager.Instance.GetMenuShowcaseCat();
            if (catDef == null) return;

            if (loadedMenuCatId != catDef.id || menuCatFrames == null || menuCatFrames.Length == 0)
            {
                loadedMenuCatId = catDef.id;
                menuCatFrames = RuntimeSpriteBank.GetHorizontalSheet(catDef.sheetResourcePath, 4, 100f);
                menuAnimFrameIdx = 0;
                if (menuCatFrames != null && menuCatFrames.Length > 0)
                    menuShowcaseCatImg.sprite = menuCatFrames[0];
            }

            if (loadedMenuCatSkinIdx != catDef.selectedSkinIndex)
            {
                loadedMenuCatSkinIdx = catDef.selectedSkinIndex;
            }
            menuShowcaseCatImg.color = catDef.GetActiveSkinTint();
        }

        private void UpdateMenuCatShowcase(float dt)
        {
            if (menuShowcaseCatRt == null || GameManager.Instance == null) return;

            var catDef = GameManager.Instance.GetMenuShowcaseCat();
            if (catDef != null && (loadedMenuCatId != catDef.id || loadedMenuCatSkinIdx != catDef.selectedSkinIndex))
            {
                RefreshMenuCatShowcaseUI();
            }

            // Keep repeating the single chosen random behavior continuously until the next time the player exits to menu
            menuActivityElapsed += dt;

            float t = menuActivityElapsed;
            Vector2 catBasePos = new Vector2(0f, -342f);
            float hopOffset = 0f;
            float facingDir = 1f;
            float rotZ = 0f;
            float scaleX = 1f;
            float scaleY = 1f;
            float animFps = 5.5f;

            switch (currentMenuCatActivity)
            {
                case MenuCatActivity.SleepingInBed:
                {
                    // Curled up right in the middle of the Cat Bed ("Nằm ở giữa ổ")
                    animFps = 1.6f;
                    catBasePos = new Vector2(0f, -342f);
                    float breath = Mathf.Sin(t * 2.4f);
                    scaleX = 1.06f + breath * 0.045f;
                    scaleY = 0.88f - breath * 0.035f;
                    rotZ = -9f + Mathf.Sin(t * 1.2f) * 2.5f;
                    break;
                }

                case MenuCatActivity.RunningChaseYarn:
                {
                    // Continuous physics-based yarn rolling & cat chasing (Zero teleportation!)
                    animFps = 9.0f;
                    Vector2 prevYarn = menuYarnCurrentPos;

                    // 1. Move yarn ball smoothly with floor friction & soft bounds bounce
                    menuYarnCurrentPos += menuYarnVelocity * dt;
                    float currentSpeed = menuYarnVelocity.magnitude;
                    if (currentSpeed > 155f)
                    {
                        menuYarnVelocity = menuYarnVelocity.normalized * Mathf.MoveTowards(currentSpeed, 155f, dt * 190f);
                    }
                    else if (currentSpeed < 120f)
                    {
                        Vector2 dir = currentSpeed > 1f ? menuYarnVelocity.normalized : new Vector2(1f, 0.25f).normalized;
                        menuYarnVelocity = dir * 155f;
                    }

                    if (menuYarnCurrentPos.x > 335f)
                    {
                        menuYarnCurrentPos.x = 335f;
                        menuYarnVelocity.x = -Mathf.Abs(menuYarnVelocity.x);
                    }
                    else if (menuYarnCurrentPos.x < -335f)
                    {
                        menuYarnCurrentPos.x = -335f;
                        menuYarnVelocity.x = Mathf.Abs(menuYarnVelocity.x);
                    }

                    if (menuYarnCurrentPos.y > -235f)
                    {
                        menuYarnCurrentPos.y = -235f;
                        menuYarnVelocity.y = -Mathf.Abs(menuYarnVelocity.y);
                    }
                    else if (menuYarnCurrentPos.y < -415f)
                    {
                        menuYarnCurrentPos.y = -415f;
                        menuYarnVelocity.y = Mathf.Abs(menuYarnVelocity.y);
                    }

                    // 2. Cat runs smoothly after the rolling yarn ball
                    Vector2 chaseTarget = new Vector2(menuYarnCurrentPos.x, menuYarnCurrentPos.y + 22f);
                    Vector2 toYarn = chaseTarget - menuCatRunPos;
                    float distToYarn = toYarn.magnitude;

                    float catRunSpeed = 290f;
                    Vector2 nextCatPos = Vector2.MoveTowards(menuCatRunPos, chaseTarget, catRunSpeed * dt);
                    float catDx = nextCatPos.x - menuCatRunPos.x;
                    if (Mathf.Abs(catDx) > 0.05f)
                        facingDir = catDx >= 0f ? 1f : -1f;
                    else
                        facingDir = (menuYarnCurrentPos.x >= menuCatRunPos.x) ? 1f : -1f;

                    menuCatRunPos = nextCatPos;
                    catBasePos = menuCatRunPos;

                    // 3. When the cat catches up to the yarn ball, it bats/dribbles it so the ball rolls forward!
                    if (menuYarnBatCooldown > 0f)
                        menuYarnBatCooldown -= dt;

                    if (distToYarn < 86f && menuYarnBatCooldown <= 0f)
                    {
                        menuYarnBatCooldown = 0.55f;
                        float kickSignX = (Mathf.Abs(menuYarnCurrentPos.x) > 250f)
                            ? -Mathf.Sign(menuYarnCurrentPos.x)
                            : facingDir;
                        float kickY = Random.Range(-110f, 110f);
                        Vector2 kickDir = new Vector2(kickSignX * 340f, kickY).normalized;
                        menuYarnVelocity = kickDir * 395f;
                    }

                    float stride = Mathf.Abs(Mathf.Sin(t * 11.5f));
                    hopOffset = stride * 26f + (menuYarnBatCooldown > 0.35f ? 22f : 0f);
                    scaleX = 1f + (0.5f - stride) * 0.12f;
                    scaleY = 1f + (stride - 0.5f) * 0.12f;
                    rotZ = -facingDir * Mathf.Sin(t * 11.5f) * 7f;

                    // 4. Roll the yarn ball visually according to exact distance traveled (dx)
                    if (menuToyYarnRt != null)
                    {
                        float dx = menuYarnCurrentPos.x - prevYarn.x;
                        float dy = menuYarnCurrentPos.y - prevYarn.y;
                        float rollDelta = (Mathf.Abs(dx) >= Mathf.Abs(dy)) ? dx : dy * Mathf.Sign(dx >= 0f ? 1f : -1f);
                        menuYarnRollAngle -= rollDelta * 1.55f;
                        float rollBounce = Mathf.Abs(Mathf.Sin(menuYarnRollAngle * Mathf.Deg2Rad * 1.5f)) * 6f;
                        menuToyYarnRt.anchoredPosition = new Vector2(menuYarnCurrentPos.x, menuYarnCurrentPos.y + rollBounce);
                        menuToyYarnRt.localEulerAngles = new Vector3(0f, 0f, menuYarnRollAngle);
                    }
                    break;
                }

                case MenuCatActivity.EatingAtBowl:
                {
                    // Standing right beside the blue food bowl on the right, munching fish kibbles happily
                    animFps = 7.5f;
                    catBasePos = new Vector2(302f, 18f);
                    facingDir = 1f;
                    float chew = Mathf.Sin(t * 13.5f);
                    hopOffset = Mathf.Max(0f, chew) * 10f;
                    rotZ = -10f + chew * 8f;
                    scaleX = 1.0f - chew * 0.06f;
                    scaleY = 1.0f + chew * 0.06f;

                    if (menuFoodKibbleRt1 != null)
                    {
                        float b1 = Mathf.Abs(Mathf.Sin(t * 8.5f)) * 26f;
                        menuFoodKibbleRt1.anchoredPosition = new Vector2(422f + Mathf.Sin(t * 4f) * 10f, -8f + b1);
                        menuFoodKibbleRt1.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * 7f) * 25f);
                    }
                    if (menuFoodKibbleRt2 != null)
                    {
                        float b2 = Mathf.Abs(Mathf.Cos(t * 9.2f)) * 22f;
                        menuFoodKibbleRt2.anchoredPosition = new Vector2(452f + Mathf.Cos(t * 5f) * 8f, -14f + b2);
                        menuFoodKibbleRt2.localEulerAngles = new Vector3(0f, 0f, -Mathf.Cos(t * 8f) * 25f);
                    }
                    break;
                }

                case MenuCatActivity.ScratchingCatTree:
                {
                    // Standing by the pink scratching post on the left, jumping & batting the dangling ball
                    animFps = 8.5f;
                    catBasePos = new Vector2(-248f, 22f);
                    facingDir = -1f;
                    float swatPhase = (t * 2.2f) % 1f;
                    if (swatPhase < 0.48f)
                    {
                        float jumpNorm = Mathf.Sin((swatPhase / 0.48f) * Mathf.PI);
                        hopOffset = jumpNorm * 78f;
                        scaleX = 0.92f;
                        scaleY = 1.12f;
                        rotZ = 14f * jumpNorm;
                    }
                    else
                    {
                        float w = Mathf.Sin(t * 16f);
                        hopOffset = 0f;
                        scaleX = 1.05f;
                        scaleY = 0.95f;
                        rotZ = w * 6f;
                    }
                    break;
                }

                case MenuCatActivity.SunbathingGrooming:
                {
                    // Sitting in the warm sunbeam under the window, swaying & grooming paws
                    animFps = 4.2f;
                    catBasePos = new Vector2(0f, -96f);
                    float sway = Mathf.Sin(t * 3.2f);
                    float groomNod = Mathf.Abs(Mathf.Sin(t * 6.4f));
                    rotZ = sway * 8.5f;
                    scaleX = 1.02f + groomNod * 0.05f;
                    scaleY = 0.98f - groomNod * 0.04f;
                    hopOffset = groomNod * 8f;
                    break;
                }

                case MenuCatActivity.ZoomiesAndBackflip:
                {
                    // High-energy figure-8 zoomies around the floor + periodic 360° mid-air flip!
                    animFps = 11.5f;
                    float w = 2.1f;
                    float xPos = Mathf.Sin(t * w) * 320f;
                    float yPos = -315f + Mathf.Sin(t * w * 2f) * 105f;
                    float vx = Mathf.Cos(t * w);
                    facingDir = vx >= 0f ? 1f : -1f;
                    catBasePos = new Vector2(xPos, yPos);

                    float flipCycle = t % 2.6f;
                    if (flipCycle > 1.95f)
                    {
                        float flipProgress = (flipCycle - 1.95f) / 0.65f;
                        hopOffset = Mathf.Sin(flipProgress * Mathf.PI) * 115f;
                        rotZ = flipProgress * 360f * -facingDir;
                    }
                    else
                    {
                        hopOffset = Mathf.Abs(Mathf.Sin(t * 15f)) * 32f;
                        rotZ = -facingDir * 10f;
                    }
                    break;
                }

                case MenuCatActivity.PouncePractice:
                {
                    // Wiggles low on the floor then leaps in a high arc to pounce on the yarn ball;
                    // upon landing, the cat bats the yarn ball so it rolls smoothly to its next spot!
                    animFps = 7.0f;
                    float cycleDur = 3.2f;
                    int curCycle = Mathf.FloorToInt(t / cycleDur);
                    float phase = (t % cycleDur) / cycleDur;

                    if (curCycle != lastPounceCycle)
                    {
                        lastPounceCycle = curCycle;
                        menuPounceStartPos = menuPounceTargetPos;
                        menuPounceTargetPos = menuNextYarnPos;
                        float nextX = (menuPounceTargetPos.x >= 0f) ? Random.Range(-290f, -95f) : Random.Range(95f, 290f);
                        float nextY = Random.Range(-390f, -225f);
                        menuNextYarnPos = new Vector2(nextX, nextY);
                    }

                    Vector2 prevYarn = menuYarnCurrentPos;

                    if (phase < 0.44f)
                    {
                        // Crouching & butt-wiggling while eyeing the yarn ball at menuPounceTargetPos
                        facingDir = (menuPounceTargetPos.x >= menuPounceStartPos.x) ? 1f : -1f;
                        catBasePos = menuPounceStartPos;
                        scaleX = 1.18f;
                        scaleY = 0.78f;
                        rotZ = Mathf.Sin(t * 28f) * 7.5f;
                        menuYarnCurrentPos = Vector2.Lerp(menuYarnCurrentPos, menuPounceTargetPos + new Vector2(0f, -24f), dt * 10f);
                    }
                    else if (phase < 0.74f)
                    {
                        // Mid-air pounce arc toward the yarn ball!
                        facingDir = (menuPounceTargetPos.x >= menuPounceStartPos.x) ? 1f : -1f;
                        float u = (phase - 0.44f) / 0.30f;
                        catBasePos = Vector2.Lerp(menuPounceStartPos, menuPounceTargetPos, u);
                        hopOffset = Mathf.Sin(u * Mathf.PI) * 135f;
                        scaleX = 0.88f;
                        scaleY = 1.16f;
                        rotZ = Mathf.Lerp(16f, -16f, u) * facingDir;
                        menuYarnCurrentPos = menuPounceTargetPos + new Vector2(0f, -24f);
                    }
                    else
                    {
                        // Cat lands on the yarn ball and knocks it rolling smoothly across the floor to menuNextYarnPos!
                        facingDir = (menuNextYarnPos.x >= menuPounceTargetPos.x) ? 1f : -1f;
                        catBasePos = menuPounceTargetPos;
                        float landBounce = Mathf.Abs(Mathf.Sin((phase - 0.74f) * 16f)) * 18f;
                        hopOffset = landBounce;

                        float uRoll = Mathf.Clamp01((phase - 0.74f) / 0.26f);
                        float easedRoll = 1f - (1f - uRoll) * (1f - uRoll);
                        Vector2 rollFrom = menuPounceTargetPos + new Vector2(0f, -24f);
                        Vector2 rollTo = menuNextYarnPos + new Vector2(0f, -24f);
                        menuYarnCurrentPos = Vector2.Lerp(rollFrom, rollTo, easedRoll);
                    }

                    if (menuToyYarnRt != null)
                    {
                        float dx = menuYarnCurrentPos.x - prevYarn.x;
                        menuYarnRollAngle -= dx * 1.55f;
                        float rollHop = Mathf.Abs(dx) > 0.2f ? Mathf.Abs(Mathf.Sin(menuYarnRollAngle * Mathf.Deg2Rad * 1.6f)) * 8f : 0f;
                        menuToyYarnRt.anchoredPosition = new Vector2(menuYarnCurrentPos.x, menuYarnCurrentPos.y + rollHop);
                        menuToyYarnRt.localEulerAngles = new Vector3(0f, 0f, menuYarnRollAngle);
                    }
                    break;
                }
            }

            // Advance 4-frame sprite sheet animation
            if (menuCatFrames != null && menuCatFrames.Length > 0)
            {
                menuAnimFrameTimer += dt * animFps;
                if (menuAnimFrameTimer >= 1f)
                {
                    menuAnimFrameTimer -= 1f;
                    menuAnimFrameIdx = (menuAnimFrameIdx + 1) % menuCatFrames.Length;
                    menuShowcaseCatImg.sprite = menuCatFrames[menuAnimFrameIdx];
                }
            }

            if (GameManager.Instance != null)
            {
                var cat = GameManager.Instance.GetMenuShowcaseCat();
                CatExpression expression = CatExpression.Happy;
                switch (currentMenuCatActivity)
                {
                    case MenuCatActivity.SleepingInBed: expression = CatExpression.Sleepy; break;
                    case MenuCatActivity.ZoomiesAndBackflip: expression = CatExpression.Surprised; break;
                    case MenuCatActivity.PouncePractice:
                    case MenuCatActivity.ScratchingCatTree: expression = CatExpression.Determined; break;
                }
                Sprite face = cat == null ? null : CharacterSpriteBank.CatFace(cat.sheetResourcePath, expression);
                if (face != null) menuShowcaseCatImg.sprite = face;
            }

            // Apply position, shadow, and scale
            menuShowcaseCatRt.anchoredPosition = new Vector2(catBasePos.x, catBasePos.y + hopOffset);
            menuShowcaseCatRt.localScale = new Vector3(facingDir * scaleX, scaleY, 1f);
            menuShowcaseCatRt.localEulerAngles = new Vector3(0f, 0f, rotZ);

            if (menuCatShadowRt != null)
            {
                menuCatShadowRt.anchoredPosition = new Vector2(catBasePos.x, catBasePos.y - 68f);
                float shadowScale = Mathf.Clamp(1f - (hopOffset / 240f), 0.55f, 1.05f);
                menuCatShadowRt.localScale = new Vector3(shadowScale, shadowScale, 1f);
            }
        }

        // --------------------------------------------------------------------
        // TAB 2: SHOP TAB (Full-Screen 3 Gacha Banners + Google AdMob Rewarded Video Roll!)
        // --------------------------------------------------------------------
        private GameObject BuildShopGachaTab(Transform parent)
        {
            GameObject panel = CreateFullScreenTabContainer(parent, "ShopGachaTab", new Color(0.10f, 0.13f, 0.24f, 1f), out GameObject bgCard, "bg_shop");

            var scroll = bgCard.GetComponentInParent<ScrollRect>();
            AddShopScrollbar(panel.transform, scroll);

            BuildRollSection(bgCard.transform, ShopGachaCategory.CatSummon, "Triệu hồi mèo",
                "Sưu tầm 7 chiến binh mèo và nhận mảnh hồn để nâng kỹ năng.", new Color32(111, 64, 52, 247));
            BuildRollSection(bgCard.transform, ShopGachaCategory.WeaponArsenal, "Kho vũ khí",
                "Mở súng mới cho chỉ huy. Súng trùng sẽ tăng cấp vũ khí.", new Color32(44, 85, 89, 247));
            BuildRollSection(bgCard.transform, ShopGachaCategory.SkinWardrobe, "Tủ trang phục",
                "Mở trang phục cho mèo và chỉ huy để tăng sát thương.", new Color32(103, 62, 99, 247));
            BuildShopSupplies(bgCard.transform);
            return panel;
        }

        private void AddShopScrollbar(Transform panel, ScrollRect scroll)
        {
            var view = scroll.viewport;
            var track = CreateCardPanel(panel, "ShopScrollbar", new Vector2(1f, 0f), Vector2.one,
                Vector2.zero, Vector2.zero, new Color32(56, 39, 40, 170));
            var rail = (RectTransform)track.transform;
            rail.offsetMin = new Vector2(-30f, view.offsetMin.y);
            rail.offsetMax = new Vector2(-5f, view.offsetMax.y);
            view.offsetMax -= new Vector2(30f, 0f);

            var thumb = CreateCardPanel(rail, "Thumb", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color32(255, 215, 150, 255));
            var bar = track.AddComponent<Scrollbar>();
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.handleRect = (RectTransform)thumb.transform;
            bar.targetGraphic = thumb.GetComponent<Image>();
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scroll.verticalScrollbar = bar;
        }

        private void BuildRollSection(Transform content, ShopGachaCategory category, string title, string description, Color theme)
        {
            var section = CreateCardPanel(content, $"RollSection_{category}", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, theme);
            var group = section.AddComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(22, 22, 12, 20);
            group.spacing = 10f;
            group.childControlWidth = group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;

            var showcase = new GameObject("RewardsShowcase", typeof(RectTransform));
            showcase.transform.SetParent(section.transform, false);
            SetTabSectionHeight(showcase, 370f);
            CreateLabel(showcase.transform, title, 43, Color.white, TextAnchor.UpperCenter,
                new Vector2(16f, 0f), new Vector2(-16f, -4f));
            BuildShopRewardPreview(showcase.transform, category);
            CreateLabel(showcase.transform, description, 29, new Color32(255, 242, 218, 255), TextAnchor.MiddleCenter,
                new Vector2(16f, 0f), new Vector2(-16f, -246f));

            var row = new GameObject("RollButtonsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(section.transform, false);
            SetTabSectionHeight(row, 132f);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            Create3DTwoLineAdButton(row.transform, "AdFreeRollBtn", Vector2.zero, Vector2.zero, Vector2.zero, btnPurpleSprite,
                "Quay miễn phí", "Xem quảng cáo", () => RequestFreeRoll(category));
            Create3DTwoLineButton(row.transform, "Roll1Btn", Vector2.zero, Vector2.zero, Vector2.zero, btnBlueSprite,
                "Quay ×1", "1 vé / 100 KC", () => ExecuteShopRoll(category, 1));
            Create3DTwoLineButton(row.transform, "Roll5Btn", Vector2.zero, Vector2.zero, Vector2.zero, btnAmberSprite,
                "Quay ×5", "400 KC (−20%)", () => ExecuteShopRoll(category, 5));

            var results = CreateCurrencyPill(section.transform, "RollResultBox", Vector2.zero, Vector2.zero, Vector2.zero);
            SetTabSectionHeight(results, 120f);
            shopResults[category] = CreateLabel(results.transform, "", 27, Color.white, TextAnchor.MiddleCenter,
                new Vector2(28f, 12f), new Vector2(-28f, -12f));
            results.SetActive(false);
        }

        private void BuildShopRewardPreview(Transform showcase, ShopGachaCategory category)
        {
            var gm = GameManager.Instance;
            for (int i = 0; i < 3; i++)
            {
                float size = i == 1 ? 151f : 122f;
                var anchor = new Vector2(0.5f, 0.5f);
                var position = new Vector2((i - 1) * 214f, i == 1 ? 10f : 0f);
                var disc = CreateIconImage(showcase, "Spotlight", "Sprites/UI/circle_white", anchor, position, Vector2.one * (size + 26f));
                disc.color = new Color32(255, 235, 201, 240);
                Image preview = CreateIconImage(disc.transform, "Reward", null, anchor, Vector2.zero, Vector2.one * size);
                if (category == ShopGachaCategory.WeaponArsenal)
                {
                    preview.sprite = RuntimeSpriteBank.GetSingle("Sprites/Characters/human_gun");
                    preview.color = gm.allWeapons[i + 1].tintColor;
                    preview.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -28f);
                }
                else if (category == ShopGachaCategory.SkinWardrobe && i == 1)
                {
                    preview.sprite = RuntimeSpriteBank.GetHorizontalSheet("Sprites/Characters/human_hero_sheet", 4)[0];
                    preview.color = gm.heroSkins[1].tintColor;
                }
                else
                {
                    var cat = gm.allCatDefinitions[i];
                    preview.sprite = RuntimeSpriteBank.GetHorizontalSheet(cat.sheetResourcePath, 4)[0];
                    if (category == ShopGachaCategory.SkinWardrobe) preview.color = cat.skins[2].tintColor;
                }
            }
        }

        private void BuildShopSupplies(Transform parent)
        {
            // 5. Free Supplies & Google AdMob Rewarded Video Row (Full-Width at Bottom)
            GameObject packRow = CreateCardPanel(parent, "FreeSupplyRow", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, Vector2.zero, new Color32(65, 81, 70, 246));
            RectTransform packRt = packRow.GetComponent<RectTransform>();
            packRt.offsetMin = new Vector2(16f, 12f);
            packRt.offsetMax = new Vector2(-16f, 210f);
            SetTabSectionHeight(packRow, 232f);

            CreateLabel(packRow.transform, "QUẦY TIẾP TẾ", 23, new Color(1f, 0.88f, 0.35f), TextAnchor.UpperCenter, new Vector2(20f, 10f), new Vector2(-20f, -14f));

            Create3DTwoLineButton(packRow.transform, "BuyGoldPack", new Vector2(0.5f, 0f), new Vector2(-248f, 82f), new Vector2(468f, 132f), btnGreenSprite,
                "ĐỔI +600 VÀNG", "Giá: 50 Kim Cương", () =>
                {
                    if (GameManager.Instance == null) return;
                    if (GameManager.Instance.gems < 50)
                    {
                        ShowNotification("Cần 50 Kim Cương để đổi 600 Vàng!", true);
                        return;
                    }
                    GameManager.Instance.gems -= 50;
                    GameManager.Instance.gold += 600;
                    ShowNotification("Đổi thành công! +600 Vàng");
                    RefreshAllUI();
                });

            Create3DTwoLineAdButton(packRow.transform, "AdMobGemPack", new Vector2(0.5f, 0f), new Vector2(248f, 82f), new Vector2(468f, 132f), btnPurpleSprite,
                "XEM QUẢNG CÁO", "+250 KC & +1 vé", () =>
                {
                    if (GoogleAdMobManager.Instance != null)
                    {
                        GoogleAdMobManager.Instance.ShowRewardedAd("Cửa Hàng Tiếp Tế", "+250 Kim Cương & +1 Vé Triệu Hồi", () =>
                        {
                            if (GameManager.Instance != null)
                            {
                                GameManager.Instance.gems += 250;
                                GameManager.Instance.summonTickets += 1;
                                ShowNotification("Đã nhận +250 Kim Cương và +1 Vé Quay!");
                                RefreshAllUI();
                            }
                        });
                    }
                });

        }

        private void RequestFreeRoll(ShopGachaCategory category)
        {
            GoogleAdMobManager.Instance?.ShowRewardedAd("Quay Gacha Miễn Phí", "1 lượt quay miễn phí",
                () => CompleteShopRoll(category, 1, true));
        }

        private void ExecuteShopRoll(ShopGachaCategory category, int count)
        {
            if (GameManager.Instance == null) return;
            var gm = GameManager.Instance;

            if (count == 1)
            {
                if (gm.summonTickets >= 1) gm.summonTickets--;
                else if (gm.gems >= 100) gm.gems -= 100;
                else
                {
                    ShowNotification("Cần 1 Vé hoặc 100 Kim Cương để quay. Bạn có thể xem quảng cáo để nhận thêm!", true);
                    return;
                }
            }
            else
            {
                if (gm.gems >= 400) gm.gems -= 400;
                else
                {
                    ShowNotification("Cần 400 Kim Cương để quay x5. Bạn có thể xem quảng cáo để nhận thêm!", true);
                    return;
                }
            }

            CompleteShopRoll(category, count, false);
        }

        private void CompleteShopRoll(ShopGachaCategory category, int count, bool fromAd)
        {
            if (GameManager.Instance == null) return;
            var rewards = GameManager.Instance.PerformGachaRollDetailed(category, count);
            var resultText = shopResults[category];
            resultText.text = string.Join("\n", rewards.ConvertAll(item => item.logLine));
            var box = resultText.transform.parent.gameObject;
            box.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)box.transform.parent);
            box.GetComponent<LayoutElement>().preferredHeight = Mathf.Max(120f, resultText.preferredHeight + 40f);
            RefreshAllUI();
            ShowGachaRollAnimation(category, rewards, fromAd);
        }

        // --------------------------------------------------------------------
        // TAB 3: PET TAB (Thú Cưng Bão Đạn 4-Stat Dashboard Grid + Range Preview + Skin Switcher)
        // --------------------------------------------------------------------
        private GameObject BuildPetSkinAndUpgradeTab(Transform parent)
        {
            GameObject panel = CreateFullScreenTabContainer(parent, "PetManagementTab", new Color(0.10f, 0.13f, 0.24f, 1f), out GameObject bgCard, "bg_pet_room");

            // Selected Cat Inspector at Top
            petInspector = CreateFullScreenDimmer(parent, "PetDetailsPopup");
            petInspector.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            petInspector.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            GameObject inspector = CreateCardPanel(petInspector.transform, "SelectedCatInspector", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 1120f), new Color(0.18f, 0.22f, 0.38f, 1f));
            Create3DButton(inspector.transform, "ClosePetDetails", new Vector2(1f, 1f), new Vector2(-42f, -38f), new Vector2(64f, 56f), btnBlueSprite, "×", 28, () => petInspector.SetActive(false));
            petInspector.SetActive(false);

            // Left Portrait + Skin Arrows
            GameObject portBox = CreateCurrencyPill(inspector.transform, "PortraitFrame", new Vector2(0f, 1f), new Vector2(146f, -142f), new Vector2(216f, 216f));
            petDetailPortrait = CreateIconImage(portBox.transform, "PetSprite", "Sprites/UI/icon_cat_stomp", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(182f, 182f));

            petDetailPortrait.raycastTarget = true;
            Button petPortrait = petDetailPortrait.gameObject.AddComponent<Button>();
            petPortrait.transition = Selectable.Transition.None;
            petPortrait.onClick.AddListener(() => PetCat(GameManager.Instance.allCatDefinitions[selectedPetIndex], petDetailPortrait.rectTransform.position));

            petSkinLabelText = CreateLabel(inspector.transform, "Skin:\nMặc Định", 19, new Color(1f, 0.88f, 0.4f), TextAnchor.MiddleCenter);
            RectTransform skRt = petSkinLabelText.GetComponent<RectTransform>();
            skRt.anchorMin = new Vector2(0f, 1f);
            skRt.anchorMax = new Vector2(0f, 1f);
            skRt.anchoredPosition = new Vector2(146f, -350f);
            skRt.sizeDelta = new Vector2(240f, 52f);

            petPreviousSkinButton = Create3DButton(inspector.transform, "PrevSkinBtn", new Vector2(0f, 1f), new Vector2(74f, -430f), new Vector2(96f, 84f), btnBlueSprite, "◀", 24, () => CycleSelectedPetSkin(-1));
            petNextSkinButton = Create3DButton(inspector.transform, "NextSkinBtn", new Vector2(0f, 1f), new Vector2(218f, -430f), new Vector2(96f, 84f), btnBlueSprite, "▶", 24, () => CycleSelectedPetSkin(1));

            petDetailStars = CreatePetStars(inspector.transform, 0, new Vector2(146f, -280f), new Vector2(260f, 52f));

            // Right Header + 4-Cell Stat Grid (Sát Thương | Hồi Chiêu | Tầm Đánh | Buff Skin)
            petDetailTitleText = CreateLabel(inspector.transform, "[SSR] Mèo Mập", 34, new Color(1f, 0.88f, 0.35f), TextAnchor.UpperLeft, new Vector2(284f, 983f), new Vector2(-80f, -14f));
            petDetailStatsText = CreateLabel(inspector.transform, "Tuyệt kỹ...", 30, new Color(0.88f, 0.94f, 1f), TextAnchor.UpperLeft, new Vector2(284f, 620f), new Vector2(-20f, -146f));

            // 2x2 High-Contrast Stat Pills inside Inspector (Width 920 -> Right area 284..864)
            GameObject st1 = CreateCurrencyPill(inspector.transform, "PetStatDmgPill", new Vector2(0f, 1f), new Vector2(250f, -610f), new Vector2(392f, 120f));
            petStatDmgText = CreateLabel(st1.transform, "SÁT THƯƠNG\n<size=34><color=#FFE066>68</color></size>", 18, Color.white, TextAnchor.MiddleCenter);

            GameObject st2 = CreateCurrencyPill(inspector.transform, "PetStatCdPill", new Vector2(0f, 1f), new Vector2(670f, -610f), new Vector2(392f, 120f));
            petStatCdText = CreateLabel(st2.transform, "HỒI CHIÊU\n<size=34><color=#80E5FF>4.0s</color></size>", 18, Color.white, TextAnchor.MiddleCenter);

            GameObject st3 = CreateCurrencyPill(inspector.transform, "PetStatRangePill", new Vector2(0f, 1f), new Vector2(250f, -760f), new Vector2(392f, 120f));
            petStatRangeText = CreateLabel(st3.transform, "TẦM ĐÁNH & NỔ\n<size=32><color=#6BFF8B>3.6m (Nổ 2.2m)</color></size>", 18, Color.white, TextAnchor.MiddleCenter);

            GameObject st4 = CreateCurrencyPill(inspector.transform, "PetStatSkinPill", new Vector2(0f, 1f), new Vector2(670f, -760f), new Vector2(392f, 120f));
            petStatSkinBuffText = CreateLabel(st4.transform, "BUFF SKIN\n<size=32><color=#FF99CC>+0% Công</color></size>", 18, Color.white, TextAnchor.MiddleCenter);

            CreateProgressBar(inspector.transform, "PetShardProgressBar", new Vector2(0f, 0f), new Vector2(568f, 128f), new Vector2(568f, 46f),
                new Color(0.32f, 0.82f, 1f, 1f), out petShardFillRt, out petShardBarText);

            Button setMenuCatBtn = Create3DButton(inspector.transform, "SetMenuShowcaseCatBtn", new Vector2(0f, 0f), new Vector2(394f, 54f), new Vector2(220f, 96f), btnAmberSprite, "ĐẶT Ở SẢNH", 20, () =>
            {
                if (GameManager.Instance != null)
                {
                    if (!GameManager.Instance.allCatDefinitions[selectedPetIndex].unlocked)
                    {
                        ShowNotification("Hãy mở khóa mèo trước khi đặt ở sảnh!", true);
                        return;
                    }
                    GameManager.Instance.SetMenuShowcaseCatIndex(selectedPetIndex);
                    ShowNotification("Đã đặt mèo ở sảnh!");
                    if (CatFightAudioManager.Instance != null)
                    {
                        CatFightAudioManager.Instance.PlayUiClick();
                        CatFightAudioManager.Instance.PlayMenuShowcaseCatVoice(0.85f);
                    }
                    RollRandomMenuCatActivity(true);
                    RefreshPetTabUI();
                    RefreshMenuCatShowcaseUI();
                }
            });
            petSetMenuShowcaseButton = setMenuCatBtn;
            petSetMenuShowcaseBtnText = setMenuCatBtn.GetComponentInChildren<Text>();
            petSetMenuShowcaseBtnImg = setMenuCatBtn.GetComponent<Image>();

            Button upgBtn = Create3DButton(inspector.transform, "UpgradePetBtn", new Vector2(0f, 0f), new Vector2(684f, 54f), new Vector2(336f, 96f), btnGreenSprite, "NÂNG CẤP (180 Vàng)", 20, () => UpgradeSelectedPet());
            petUpgradeButton = upgBtn;
            petUpgradeBtnText = upgBtn.GetComponentInChildren<Text>();

            var roomWindow = new GameObject("RoomWindow", typeof(RectTransform));
            roomWindow.transform.SetParent(bgCard.transform, false);
            SetTabSectionHeight(roomWindow, 220f);

            // Three-column collection, with space above to see the room.
            GameObject gridObj = new GameObject("PetRosterList", typeof(RectTransform));
            gridObj.transform.SetParent(bgCard.transform, false);
            RectTransform grt = gridObj.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0.5f, 0f);
            grt.anchorMax = new Vector2(0.5f, 0f);
            grt.anchoredPosition = new Vector2(0f, 405f);
            grt.sizeDelta = new Vector2(920f, 765f);

            GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(292f, 292f);
            grid.spacing = new Vector2(22f, 22f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;

            petGridContainer = gridObj.transform;
            return panel;
        }

        private Transform CreatePetStars(Transform parent, int halves, Vector2 position, Vector2 size)
        {
            GameObject row = new GameObject("FiveStars", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            RectTransform rt = row.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            float width = size.x / 5f;
            for (int i = 0; i < 5; i++)
            {
                GameObject star = new GameObject("Star", typeof(RectTransform), typeof(PetStarGraphic));
                star.transform.SetParent(row.transform, false);
                RectTransform starRt = star.GetComponent<RectTransform>();
                starRt.anchorMin = starRt.anchorMax = new Vector2(0f, 0.5f);
                starRt.anchoredPosition = new Vector2((i + 0.5f) * width, 0f);
                starRt.sizeDelta = new Vector2(width - 4f, size.y);
                star.GetComponent<PetStarGraphic>().raycastTarget = false;
            }
            UpdatePetStars(row.transform, halves);
            return row.transform;
        }

        private void UpdatePetStars(Transform row, int halves)
        {
            for (int i = 0; i < 5; i++)
                row.GetChild(i).GetComponent<PetStarGraphic>().SetHalves(halves - i * 2);
        }

        private void CycleSelectedPetSkin(int delta)
        {
            if (GameManager.Instance == null) return;
            var cats = GameManager.Instance.allCatDefinitions;
            if (selectedPetIndex < 0 || selectedPetIndex >= cats.Count) return;

            var cat = cats[selectedPetIndex];
            if (!cat.unlocked || cat.skins == null || cat.skins.Count == 0) return;

            cat.selectedSkinIndex = (cat.selectedSkinIndex + delta + cat.skins.Count) % cat.skins.Count;
            cat.skins[cat.selectedSkinIndex].unlocked = true;
            if (CatFightAudioManager.Instance != null) CatFightAudioManager.Instance.PlayUiClick();
            RefreshPetTabUI();
        }

        private void UpgradeSelectedPet()
        {
            if (GameManager.Instance == null) return;
            var gm = GameManager.Instance;
            var cat = gm.allCatDefinitions[selectedPetIndex];
            int cost = 120 + cat.permanentLevel * 60;
            bool wasUnlocked = cat.unlocked;
            if (wasUnlocked && cat.permanentLevel >= 11) return;
            if (gm.gold < cost)
            {
                ShowNotification($"Cần {cost} Vàng để {(wasUnlocked ? "nâng cấp" : "mở khóa")} mèo!", true);
                return;
            }

            if (gm.gold >= cost)
            {
                gm.gold -= cost;
                cat.unlocked = true;
                if (wasUnlocked) cat.permanentLevel++;
                ShowNotification(wasUnlocked ? $"{cat.displayName} đã được nâng thêm nửa sao!" : $"Đã mở khóa {cat.displayName}! Có thể chọn mèo trong trận.");
                gm.UpdateMissionProgress("ev_upgrade_hero", 1, false);
                if (CatFightAudioManager.Instance != null) CatFightAudioManager.Instance.PlayUiClick();
                RefreshAllUI();
            }
        }

        private void RefreshPetTabUI()
        {
            if (GameManager.Instance == null || petGridContainer == null) return;
            var cats = GameManager.Instance.allCatDefinitions;
            if (selectedPetIndex < 0 || selectedPetIndex >= cats.Count) selectedPetIndex = 0;

            var sel = cats[selectedPetIndex];
            if (petDetailPortrait != null)
            {
                petDetailPortrait.sprite = RuntimeSpriteBank.GetSingle(sel.iconResourcePath, 100f);
                petDetailPortrait.color = sel.unlocked ? sel.GetActiveSkinTint() : new Color(0.45f, 0.45f, 0.50f);
            }

            if (petSkinLabelText != null)
                petSkinLabelText.text = $"Skin: <color=#FFE066>{sel.GetActiveSkinName()}</color>";

            float dmg = sel.baseDamage * (1f + 0.28f * (sel.permanentLevel - 1)) * sel.GetSkinDamageMultiplier();
            float nextDmg = sel.baseDamage * (1f + 0.28f * sel.permanentLevel) * sel.GetSkinDamageMultiplier();
            float cd = Mathf.Max(1.0f, sel.baseCooldown * Mathf.Pow(0.92f, sel.permanentLevel - 1));
            float nextCd = Mathf.Max(1.0f, sel.baseCooldown * Mathf.Pow(0.92f, sel.permanentLevel));
            if (sel.permanentLevel >= 11) { nextDmg = dmg; nextCd = cd; }
            int skinBonusPct = Mathf.RoundToInt((sel.GetSkinDamageMultiplier() - 1f) * 100f);

            if (petDetailTitleText != null)
                petDetailTitleText.text = $"[{sel.rarity}] {sel.displayName} {(sel.unlocked ? "<color=#6BFF8B>(Đã Mở)</color>" : "<color=#FF8888>(Chưa Mở)</color>")}";

            if (petDetailStatsText != null)
                petDetailStatsText.text = $"Tuyệt Kỹ: <color=#FFE066>{sel.title}</color> — {sel.description}";

            if (petStatDmgText != null)
                petStatDmgText.text = $"⚔ SÁT THƯƠNG CHIÊU\n<size=34><color=#FFE066>{Mathf.RoundToInt(dmg)}</color> ▶ <color=#6BFF8B>{Mathf.RoundToInt(nextDmg)}</color></size>";

            if (petStatCdText != null)
                petStatCdText.text = $"⚡ HỒI CHIÊU\n<size=34><color=#80E5FF>{cd:0.0}s</color> ▶ <color=#6BFF8B>{nextCd:0.0}s</color></size>";

            if (petStatRangeText != null)
                petStatRangeText.text = $"★ TẦM ĐÁNH & VÙNG NỔ\n<size=32><color=#6BFF8B>Tầm {sel.attackRange:0.0}m • Nổ {sel.effectRadius:0.0}m</color></size>";

            if (petStatSkinBuffText != null)
                petStatSkinBuffText.text = $"♦ BUFF TRANG PHỤC\n<size=32><color=#FF99CC>+{skinBonusPct}% Sát Thương</color></size>";

            SetProgressBarRatio(petShardFillRt, Mathf.Clamp01(sel.shards / 25f));
            if (petShardBarText != null)
                petShardBarText.text = $"Mảnh Đột Phá: {sel.shards} / 25";

            int cost = 120 + sel.permanentLevel * 60;
            if (petUpgradeBtnText != null)
                petUpgradeBtnText.text = sel.unlocked
                    ? $"NÂNG NỬA SAO\n<size=26>({cost} Vàng)</size>"
                    : $"MỞ KHÓA MÈO\n<size=26>({cost} Vàng)</size>";

            bool maxStars = sel.unlocked && sel.permanentLevel >= 11;
            petUpgradeButton.interactable = !maxStars;
            if (maxStars) petUpgradeBtnText.text = "ĐÃ ĐỦ 5 SAO";
            UpdatePetStars(petDetailStars, sel.unlocked ? sel.permanentLevel - 1 : 0);

            bool isMenuCat = sel.unlocked && GameManager.Instance.menuShowcaseCatIndex == selectedPetIndex;
            if (petSetMenuShowcaseButton != null) petSetMenuShowcaseButton.interactable = sel.unlocked;
            if (petPreviousSkinButton != null) petPreviousSkinButton.interactable = sel.unlocked;
            if (petNextSkinButton != null) petNextSkinButton.interactable = sel.unlocked;
            if (petSetMenuShowcaseBtnText != null)
                petSetMenuShowcaseBtnText.text = !sel.unlocked ? "CHƯA MỞ KHÓA" : (isMenuCat ? "★ ĐANG Ở SẢNH" : "ĐẶT Ở SẢNH");
            if (petSetMenuShowcaseBtnImg != null)
                petSetMenuShowcaseBtnImg.sprite = isMenuCat ? btnAmberSprite : btnPurpleSprite;

            foreach (Transform c in petGridContainer)
                Destroy(c.gameObject);

            for (int i = 0; i < cats.Count; i++)
            {
                int idx = i;
                var cDef = cats[i];
                bool isSel = idx == selectedPetIndex;

                GameObject row = CreateCardPanel(petGridContainer, $"PetRow_{cDef.id}", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                    isSel ? new Color32(255, 225, 156, 250) : (cDef.unlocked ? new Color32(255, 245, 218, 242) : new Color32(212, 229, 218, 238)));
                
                Button b = row.AddComponent<Button>();
                b.onClick.AddListener(() =>
                {
                    selectedPetIndex = idx;
                    petInspector.transform.SetAsLastSibling();
                    petInspector.SetActive(true);
                    if (cDef.unlocked) PetCat(cDef, petDetailPortrait.rectTransform.position);
                    else CatFightAudioManager.Instance?.PlayUiClick();
                    RefreshPetTabUI();
                });

                Image icon = CreateIconImage(row.transform, "Icon", cDef.iconResourcePath, new Vector2(0.5f, 1f), new Vector2(0f, cDef.unlocked ? -105f : -117f), cDef.unlocked ? new Vector2(184f, 184f) : new Vector2(156f, 156f));
                icon.color = cDef.unlocked ? cDef.GetActiveSkinTint() : new Color(0.45f, 0.45f, 0.50f);
                CreatePetStars(row.transform, cDef.unlocked ? cDef.permanentLevel - 1 : 0, new Vector2(146f, -220f), new Vector2(270f, 51f));
                if (!cDef.unlocked)
                    CreateLabel(row.transform, "ĐÃ KHÓA", 19, new Color32(84, 64, 62, 255), TextAnchor.UpperCenter, new Vector2(4f, 0f), new Vector2(-4f, -8f)).GetComponent<Shadow>().enabled = false;
                CreateLabel(row.transform, cDef.displayName, 23, new Color32(44, 64, 59, 255), TextAnchor.MiddleCenter, new Vector2(8f, 6f), new Vector2(-8f, -240f)).GetComponent<Shadow>().enabled = false;
            }
        }

        // --------------------------------------------------------------------
        // TAB 4: CHARACTER TAB (Thú Cưng Bão Đạn 4-Stat Grid + Current ▶ Next Upgrade Rows)
        // --------------------------------------------------------------------
        private GameObject BuildHeroCharacterTab(Transform parent)
        {
            GameObject panel = CreateFullScreenTabContainer(parent, "HeroCharacterTab", new Color(0.10f, 0.13f, 0.24f, 1f), out GameObject bgCard);

            GameObject heroTop = CreateCardPanel(bgCard.transform, "HeroShowcase", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color32(36, 74, 80, 255));
            SetTabSectionHeight(heroTop, 806f, 1f);
            CreateLabel(heroTop.transform, "Chỉ huy", 43, Color.white, TextAnchor.UpperLeft, new Vector2(36f, 0f), new Vector2(-24f, -24f));
            heroStatBarrelBoxText = CreateLabel(heroTop.transform, "", 26, new Color32(175, 234, 220, 255),
                TextAnchor.UpperRight, new Vector2(250f, 0f), new Vector2(-36f, -38f));

            var stage = CreateIconImage(heroTop.transform, "ArmedHeroPreview", "Sprites/UI/circle_white",
                new Vector2(0.5f, 0.55f), new Vector2(0f, 12f), new Vector2(512f, 512f));
            stage.color = new Color32(158, 207, 187, 255);
            heroPreviewIcon = CreateIconImage(stage.transform, "HeroIcon", "Sprites/UI/tab_hero",
                new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(478f, 478f));
            Sprite[] heroFrames = RuntimeSpriteBank.GetHorizontalSheet("Sprites/Characters/human_hero_sheet", 4, 100f);
            if (heroFrames.Length > 0) heroPreviewIcon.sprite = heroFrames[0];
            weaponPreviewIcon = CreateIconImage(stage.transform, "HeldGun", "Sprites/Characters/human_gun",
                new Vector2(0.5f, 0.5f), new Vector2(54f, -88f), new Vector2(218f, 255f));

            heroSkinNameText = CreateLoadoutSelector(heroTop.transform, "SwitchHeroSkinBtn", "Trang phục", -220f, () =>
            {
                if (GameManager.Instance == null) return;
                var gm = GameManager.Instance;
                gm.equippedHeroSkinIndex = (gm.equippedHeroSkinIndex + 1) % gm.heroSkins.Count;
                gm.heroSkins[gm.equippedHeroSkinIndex].unlocked = true;
                gm.ApplyHeroMetaStats();
                if (CatFightAudioManager.Instance != null) CatFightAudioManager.Instance.PlayUiClick();
                RefreshAllUI();
            });
            heroWeaponNameText = CreateLoadoutSelector(heroTop.transform, "SwitchWeaponBtn", "Vũ khí", 220f, () =>
            {
                if (GameManager.Instance == null) return;
                var gm = GameManager.Instance;
                gm.equippedWeaponIndex = (gm.equippedWeaponIndex + 1) % gm.allWeapons.Count;
                gm.allWeapons[gm.equippedWeaponIndex].unlocked = true;
                gm.ApplyHeroMetaStats();
                if (CatFightAudioManager.Instance != null) CatFightAudioManager.Instance.PlayUiClick();
                RefreshAllUI();
            });

            CreateUpgradeStatRow(bgCard.transform, "UpgAtkRow", "Sát thương", RuntimeSpriteBank.GetSingle("Sprites/Characters/human_gun"), out heroAtkLvText, () =>
            {
                if (GameManager.Instance == null) return;
                if (GameManager.Instance.gold < 150)
                {
                    ShowNotification("Cần 150 Vàng để nâng cấp!", true);
                    return;
                }
                GameManager.Instance.gold -= 150;
                GameManager.Instance.heroAtkUpgradeLv++;
                ShowNotification("Nâng cấp hỏa lực thành công!");
                GameManager.Instance.ApplyHeroMetaStats();
                GameManager.Instance.UpdateMissionProgress("ev_upgrade_hero", 1, false);
                if (CatFightAudioManager.Instance != null) CatFightAudioManager.Instance.PlayUiClick();
                RefreshAllUI();
            });

            CreateUpgradeStatRow(bgCard.transform, "UpgSpdRow", "Tốc độ bắn", RuntimeSpriteBank.GetSingle("Sprites/UI/icon_speed_2"), out heroSpdLvText, () =>
            {
                if (GameManager.Instance == null) return;
                if (GameManager.Instance.gold < 150)
                {
                    ShowNotification("Cần 150 Vàng để nâng cấp!", true);
                    return;
                }
                GameManager.Instance.gold -= 150;
                GameManager.Instance.heroSpeedUpgradeLv++;
                ShowNotification("Nâng cấp tốc độ bắn thành công!");
                GameManager.Instance.ApplyHeroMetaStats();
                GameManager.Instance.UpdateMissionProgress("ev_upgrade_hero", 1, false);
                if (CatFightAudioManager.Instance != null) CatFightAudioManager.Instance.PlayUiClick();
                RefreshAllUI();
            });

            CreateUpgradeStatRow(bgCard.transform, "UpgWallRow", "Hàng rào", RuntimeSpriteBank.GetHorizontalSheet("Sprites/UI/navigation_glyphs", 5)[2], out wallHpLvText, () =>
            {
                if (GameManager.Instance == null) return;
                if (GameManager.Instance.gold < 150)
                {
                    ShowNotification("Cần 150 Vàng để nâng cấp!", true);
                    return;
                }
                GameManager.Instance.gold -= 150;
                GameManager.Instance.wallHpUpgradeLv++;
                ShowNotification("Nâng cấp hàng rào thành công!");
                GameManager.Instance.ApplyHeroMetaStats();
                GameManager.Instance.UpdateMissionProgress("ev_upgrade_hero", 1, false);
                if (CatFightAudioManager.Instance != null) CatFightAudioManager.Instance.PlayUiClick();
                RefreshAllUI();
            });

            return panel;
        }

        private Text CreateLoadoutSelector(Transform parent, string name, string label, float x, UnityEngine.Events.UnityAction cycle)
        {
            var button = Create3DButton(parent, name, new Vector2(0.5f, 0f), new Vector2(x, 86f),
                new Vector2(420f, 128f), tintableTileSprite, "", 24, cycle);
            button.image.color = new Color32(27, 52, 65, 255);
            CreateLabel(button.transform, label, 22, new Color32(169, 222, 211, 255), TextAnchor.UpperLeft,
                new Vector2(20f, 0f), new Vector2(-34f, -10f));
            CreateLabel(button.transform, "›", 38, Color.white, TextAnchor.MiddleRight, Vector2.zero, new Vector2(-12f, 0f));
            return CreateLabel(button.transform, "", 23, Color.white, TextAnchor.MiddleLeft, new Vector2(20f, 6f), new Vector2(-42f, -42f));
        }

        private void CreateUpgradeStatRow(Transform parent, string name, string title, Sprite icon, out Text infoLabel, UnityEngine.Events.UnityAction onUpgrade)
        {
            var row = CreateCardPanel(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color32(46, 59, 80, 255));
            SetTabSectionHeight(row, 156f);
            CreateIconImage(row.transform, "StatIcon", null, new Vector2(0f, 0.5f), new Vector2(68f, 0f), new Vector2(82f, 82f)).sprite = icon;
            CreateLabel(row.transform, title, 28, Color.white, TextAnchor.UpperLeft, new Vector2(133f, 0f), new Vector2(-245f, -20f));
            infoLabel = CreateLabel(row.transform, "", 26, new Color32(206, 221, 230, 255), TextAnchor.LowerLeft,
                new Vector2(133f, 22f), new Vector2(-218f, -58f));
            var button = Create3DButton(row.transform, "UpgBtn", new Vector2(1f, 0.5f), new Vector2(-115f, 0f),
                new Vector2(194f, 108f), tintableTileSprite, "", 24, onUpgrade);
            button.image.color = new Color32(37, 110, 98, 255);
            CreateLabel(button.transform, "Nâng cấp", 23, Color.white, TextAnchor.UpperCenter, Vector2.zero, new Vector2(0f, -11f));
            CreateIconImage(button.transform, "Gold", "Sprites/UI/icon_coin", new Vector2(0.5f, 0f), new Vector2(-38f, 31f), new Vector2(32f, 32f));
            CreateLabel(button.transform, "150", 26, new Color32(255, 224, 156, 255), TextAnchor.LowerCenter, new Vector2(38f, 10f), Vector2.zero);
        }

        private void RefreshHeroTabUI()
        {
            if (GameManager.Instance == null) return;
            var gm = GameManager.Instance;
            var wpn = gm.GetEquippedWeapon();
            var skin = gm.GetEquippedHeroSkin();

            if (heroPreviewIcon != null) heroPreviewIcon.color = skin.tintColor;
            if (weaponPreviewIcon != null) weaponPreviewIcon.color = wpn.tintColor;

            heroStatBarrelBoxText.text = $"{1 + wpn.bonusBarrels} nòng";
            int suffix = skin.skinName.IndexOf(" (", System.StringComparison.Ordinal);
            heroSkinNameText.text = suffix >= 0 ? skin.skinName.Substring(0, suffix) : skin.skinName;
            heroWeaponNameText.text = $"{wpn.displayName} · Cấp {wpn.level}";
            heroAtkLvText.text = $"Cấp {gm.heroAtkUpgradeLv}   {gm.GetHeroDamage(gm.heroAtkUpgradeLv):0.#} → <color=#A7F4CC>{gm.GetHeroDamage(gm.heroAtkUpgradeLv + 1):0.#}</color>";
            heroSpdLvText.text = $"Cấp {gm.heroSpeedUpgradeLv}   {gm.GetHeroFireRate(gm.heroSpeedUpgradeLv):0.##} → <color=#A7F4CC>{gm.GetHeroFireRate(gm.heroSpeedUpgradeLv + 1):0.##}/s</color>";
            wallHpLvText.text = $"Cấp {gm.wallHpUpgradeLv}   {gm.GetWallHealth(gm.wallHpUpgradeLv)} → <color=#A7F4CC>{gm.GetWallHealth(gm.wallHpUpgradeLv + 1)} HP</color>";
        }

        // --------------------------------------------------------------------
        // TAB 5: EVENT TAB (Redesigned Rich Event Center + Visual Reward Pills & Milestone Banner!)
        // --------------------------------------------------------------------
        private GameObject BuildEventMissionsTab(Transform parent)
        {
            GameObject panel = CreateFullScreenTabContainer(parent, "EventMissionsTab", new Color(0.10f, 0.13f, 0.24f, 1f), out GameObject bgCard, "bg_event_festival");

            var festivalSky = new GameObject("FestivalSky", typeof(RectTransform));
            festivalSky.transform.SetParent(bgCard.transform, false);
            SetTabSectionHeight(festivalSky, 170f);

            GameObject header = new GameObject("EventRibbon", typeof(RectTransform), typeof(Image));
            header.transform.SetParent(bgCard.transform, false);
            SetTabSectionHeight(header, 204f);
            var backdrop = header.GetComponent<Image>();
            backdrop.sprite = RuntimeSpriteBank.GetSingle("Sprites/UI/event_campaign_banner");
            backdrop.raycastTarget = false;

            Sprite[] bossFrames = CharacterSpriteBank.BossFrames(1);
            if (bossFrames.Length > 0)
            {
                Image boss = CreateIconImage(header.transform, "ChefMouse", null, new Vector2(1f, 0.5f),
                    new Vector2(-119f, -4f), new Vector2(195f, 178f));
                boss.sprite = bossFrames[0];
            }

            CreateEventHeadingLine(header.transform, "SỰ KIỆN", 20, new Color32(255, 216, 154, 255), 22f, 32f);
            CreateEventHeadingLine(header.transform, "Đại chiến diệt chuột", 43, Color.white, 60f, 66f);
            CreateEventHeadingLine(header.transform, "Làm nhiệm vụ, nhận vàng và kim cương.", 24,
                new Color32(249, 228, 239, 255), 138f, 40f);

            // 2. Event Milestone Progress & AdMob Bonus Strip
            GameObject summaryBanner = CreateCardPanel(bgCard.transform, "EventSummaryBanner", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -238f), new Vector2(920f, 200f), new Color(0.28f, 0.17f, 0.46f, 1f));
            SetTabSectionHeight(summaryBanner, 200f);

            GameObject chestBadge = CreateCurrencyPill(summaryBanner.transform, "ChestBadge", new Vector2(0f, 0.5f), new Vector2(76f, 0f), new Vector2(114f, 114f));
            CreateIconImage(chestBadge.transform, "ChestIcon", "Sprites/UI/icon_gacha_capsule", new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(92f, 92f));

            eventHeaderProgressText = CreateLabel(summaryBanner.transform,
                "TIẾN ĐỘ SỰ KIỆN: <color=#6BFF8B>1 / 5 NHIỆM VỤ</color>\n<size=32><color=#D8E8FF>Hoàn thành các mốc để nhận thưởng lớn!</color></size>",
                22, new Color(1f, 0.90f, 0.35f), TextAnchor.UpperLeft, new Vector2(150f, 56f), new Vector2(-264f, -12f));

            CreateProgressBar(summaryBanner.transform, "EventTotalProgBar", new Vector2(0f, 0f), new Vector2(396f, 32f), new Vector2(492f, 44f),
                new Color(1f, 0.78f, 0.22f, 1f), out eventHeaderFillRt, out eventHeaderBarLabel);

            Create3DTwoLineAdButton(summaryBanner.transform, "EventAdBonusBtn", new Vector2(1f, 0.5f), new Vector2(-128f, 0f), new Vector2(228f, 154f), btnPurpleSprite,
                "QUÀ SỰ KIỆN", "+150 KC & +300 Vàng", () =>
                {
                    if (GoogleAdMobManager.Instance != null)
                    {
                        GoogleAdMobManager.Instance.ShowRewardedAd("Thưởng Sự Kiện", "+150 Kim Cương & +300 Vàng", () =>
                        {
                            if (GameManager.Instance != null)
                            {
                                GameManager.Instance.gems += 150;
                                GameManager.Instance.gold += 300;
                                ShowNotification("Đã nhận +150 Kim Cương và +300 Vàng!");
                                if (CatFightAudioManager.Instance != null) CatFightAudioManager.Instance.PlayGacha();
                                RefreshAllUI();
                            }
                        });
                    }
                });

            // 3. Mission Cards Vertical List
            GameObject listObj = new GameObject("EventMissionsList", typeof(RectTransform));
            listObj.transform.SetParent(bgCard.transform, false);
            RectTransform lrt = listObj.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.5f, 0f);
            lrt.anchorMax = new Vector2(0.5f, 0f);
            lrt.anchoredPosition = new Vector2(0f, 504f);
            lrt.sizeDelta = new Vector2(920f, 956f);

            VerticalLayoutGroup vlg = listObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            eventMissionListContainer = listObj.transform;
            return panel;
        }

        private void CreateEventHeadingLine(Transform header, string text, int fontSize, Color ink, float top, float height)
        {
            Text line = CreateLabel(header, text, fontSize, ink, TextAnchor.MiddleLeft);
            RectTransform rt = line.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(36f, -top - height);
            rt.offsetMax = new Vector2(-243f, -top);
            line.GetComponent<Shadow>().enabled = false;
        }

        private void RefreshEventTabUI()
        {
            if (GameManager.Instance == null || eventMissionListContainer == null) return;
            foreach (Transform c in eventMissionListContainer)
                Destroy(c.gameObject);

            string[] missionIcons = new string[]
            {
                "Sprites/UI/icon_cat_royal",
                "Sprites/UI/icon_cat_ninja",
                "Sprites/UI/icon_gacha_capsule",
                "Sprites/UI/icon_cat_mage",
                "Sprites/UI/icon_cat_stomp"
            };

            int totalMissions = GameManager.Instance.eventMissions.Count;
            int completedMissions = 0;
            for (int i = 0; i < totalMissions; i++)
            {
                var m = GameManager.Instance.eventMissions[i];
                if (m.claimed || m.currentProgress >= m.targetProgress)
                    completedMissions++;
            }

            if (eventHeaderProgressText != null)
            {
                eventHeaderProgressText.text = $"TIẾN ĐỘ SỰ KIỆN: <color=#6BFF8B>{completedMissions} / {totalMissions} NHIỆM VỤ</color>\n<size=32><color=#D8E8FF>Hoàn thành các mốc để nhận kho báu Mèo!</color></size>";
            }
            if (eventHeaderFillRt != null)
            {
                float totalRatio = Mathf.Clamp01((float)completedMissions / Mathf.Max(1, totalMissions));
                SetProgressBarRatio(eventHeaderFillRt, totalRatio);
                if (eventHeaderBarLabel != null)
                    eventHeaderBarLabel.text = $"Hoàn thành {Mathf.RoundToInt(totalRatio * 100f)}% ({completedMissions}/{totalMissions})";
            }

            for (int idx = 0; idx < totalMissions; idx++)
            {
                var captured = GameManager.Instance.eventMissions[idx];
                int missionIndex = idx;
                bool canClaim = !captured.claimed && captured.currentProgress >= captured.targetProgress;

                Color cardBgColor = canClaim
                    ? new Color(0.16f, 0.36f, 0.28f, 1f)
                    : (captured.claimed ? new Color(0.14f, 0.17f, 0.28f, 0.92f) : new Color32(67, 54, 94, 233));

                GameObject row = CreateCardPanel(eventMissionListContainer, $"Mission_{captured.id}",
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, cardBgColor);
                SetTabSectionHeight(row, 328f);

                // Left Icon Badge Frame
                GameObject iconFrame = CreateCurrencyPill(row.transform, "MissionBadge", new Vector2(0f, 0.5f), new Vector2(76f, 0f), new Vector2(120f, 128f));
                Image frameImg = iconFrame.GetComponent<Image>();
                if (frameImg != null)
                    frameImg.color = canClaim ? new Color(1f, 0.95f, 0.72f, 1f) : Color.white;

                string iconPath = missionIcons[missionIndex % missionIcons.Length];
                CreateIconImage(iconFrame.transform, "MissionIcon", iconPath, new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(96f, 96f));
                CreateLabel(iconFrame.transform, $"MỐC {missionIndex + 1}", 16, new Color(1f, 0.88f, 0.35f),
                    TextAnchor.LowerCenter, new Vector2(4f, 6f), new Vector2(-4f, -84f));

                // Center Title & Description
                string titleHex = canClaim ? "#6BFF8B" : (captured.claimed ? "#B0C0D8" : "#FFE066");
                CreateLabel(row.transform, $"<color={titleHex}>★ {captured.title}</color>",
                    24, Color.white, TextAnchor.UpperLeft, new Vector2(150f, 239f), new Vector2(-248f, -12f));

                CreateLabel(row.transform, captured.description,
                    24, new Color(0.90f, 0.95f, 1f), TextAnchor.UpperLeft, new Vector2(150f, 112f), new Vector2(-248f, -91f));

                // Visual Reward Pills Row (Gold Pill + Gem Pill)
                GameObject goldRewardPill = CreateCurrencyPill(row.transform, "RewardGoldPill", new Vector2(0f, 0.5f), new Vector2(236f, -86f), new Vector2(172f, 46f));
                CreateIconImage(goldRewardPill.transform, "CoinIcon", "Sprites/UI/icon_coin", new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(30f, 30f));
                CreateLabel(goldRewardPill.transform, $"+{captured.rewardGold}", 19, new Color(1f, 0.88f, 0.32f),
                    TextAnchor.MiddleLeft, new Vector2(46f, 0f), new Vector2(-8f, 0f));

                GameObject gemRewardPill = CreateCurrencyPill(row.transform, "RewardGemPill", new Vector2(0f, 0.5f), new Vector2(420f, -86f), new Vector2(172f, 46f));
                CreateIconImage(gemRewardPill.transform, "GemIcon", "Sprites/UI/icon_gem", new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(30f, 30f));
                CreateLabel(gemRewardPill.transform, $"+{captured.rewardGems}", 19, new Color(0.50f, 0.90f, 1f),
                    TextAnchor.MiddleLeft, new Vector2(46f, 0f), new Vector2(-8f, 0f));

                // Bottom Mission Progress Bar
                float ratio = Mathf.Clamp01((float)captured.currentProgress / Mathf.Max(1, captured.targetProgress));
                Color progFillCol = (ratio >= 1f)
                    ? new Color(0.28f, 0.90f, 0.50f, 1f)
                    : new Color(0.24f, 0.78f, 1f, 1f);

                CreateProgressBar(row.transform, "MissionProgBar", new Vector2(0f, 0f), new Vector2(396f, 26f), new Vector2(492f, 40f),
                    progFillCol, out RectTransform fillRt, out Text progTxt);
                SetProgressBarRatio(fillRt, ratio);
                GameTypography.Apply(progTxt, 24);
                progTxt.text = $"Tiến độ: {Mathf.Min(captured.currentProgress, captured.targetProgress)} / {captured.targetProgress}";

                // Right Action Button
                if (canClaim)
                {
                    Create3DTwoLineButton(row.transform, "ClaimBtn", new Vector2(1f, 0.5f), new Vector2(-124f, 0f), new Vector2(216f, 132f),
                        btnGreenSprite, "★ NHẬN QUÀ", "Hoàn thành!", () =>
                        {
                            captured.claimed = true;
                            GameManager.Instance.gold += captured.rewardGold;
                            GameManager.Instance.gems += captured.rewardGems;
                            ShowNotification($"Nhận quà thành công! +{captured.rewardGold} Vàng và +{captured.rewardGems} Kim Cương");
                            if (CatFightAudioManager.Instance != null) CatFightAudioManager.Instance.PlayGacha();
                            RefreshAllUI();
                        });
                }
                else if (captured.claimed)
                {
                    Button doneB = Create3DButton(row.transform, "ClaimBtn", new Vector2(1f, 0.5f), new Vector2(-124f, 0f), new Vector2(216f, 132f),
                        glossyBtnSprite, "✓ ĐÃ NHẬN", 23, () => { });
                    doneB.GetComponent<Image>().color = new Color(0.55f, 0.60f, 0.72f, 0.90f);
                }
                else
                {
                    Create3DTwoLineButton(row.transform, "GoMissionBtn", new Vector2(1f, 0.5f), new Vector2(-124f, 0f), new Vector2(216f, 132f),
                        btnAmberSprite, "LÀM NGAY ▶", "Chuyển tới", () =>
                        {
                            if (CatFightAudioManager.Instance != null) CatFightAudioManager.Instance.PlayUiClick();
                            if (captured.id.Contains("gacha"))
                                SwitchMainMenuTab(MainMenuTab.Shop);
                            else if (captured.id.Contains("upgrade"))
                                SwitchMainMenuTab(MainMenuTab.Character);
                            else
                                SwitchMainMenuTab(MainMenuTab.Battle);
                        });
                }
            }
        }

        // ====================================================================
        // PART 2: IN-BATTLE HUD (Thú Cưng Bão Đạn Style — Enlarged Top Controls & Speed Icon)
        // ====================================================================
        private void BuildBattleHudLayer(Transform parent)
        {
            battleHudLayer = new GameObject("BattleHudLayer", typeof(RectTransform));
            battleHudLayer.transform.SetParent(parent, false);
            StretchFull(battleHudLayer.GetComponent<RectTransform>());

            // Frameless Floating Top HUD (Enlarged for crisp mobile readability & touch comfort!)
            // Row 1 (Top-most at y = -36f): Larger, Chunky Visual EXP Progress Bar (Height 64f, No numbers inside!)
            GameObject expBarGo = CreateProgressBar(battleHudLayer.transform, "ExpProgressBar", new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(900f, 64f),
                new Color(0.22f, 0.86f, 1f, 1f), out expFillRt, out levelText);
            RectTransform expRt = expBarGo.GetComponent<RectTransform>();
            expRt.anchorMin = new Vector2(0f, 1f);
            expRt.anchorMax = new Vector2(1f, 1f);
            expRt.anchoredPosition = new Vector2(0f, -36f);
            expRt.sizeDelta = new Vector2(-20f, 64f);
            if (levelText != null) levelText.text = "";

            // Row 2 (Enlarged Floating Top Controls at y = -118f):
            Create3DButton(battleHudLayer.transform, "BackMenuBtn", new Vector2(0f, 1f), new Vector2(114f, -118f), new Vector2(204f, 86f), btnRedSprite, "◀ SẢNH", 27, () =>
            {
                if (CatFightAudioManager.Instance != null) CatFightAudioManager.Instance.PlayUiClick();
                if (GameManager.Instance != null) GameManager.Instance.ReturnToMainMenu();
            });

            Vector2 topCenter = new Vector2(0.5f, 1f);
            waveTitleText = CreateOverlayText(battleHudLayer.transform, "WaveTitle", 33,
                new Color32(255, 224, 127, 255), topCenter, -99f, new Vector2(600f, 56f));
            killCountText = CreateOverlayText(battleHudLayer.transform, "WaveKills", 26,
                Color.white, topCenter, -151f, new Vector2(600f, 48f));

            autoBtnBg = null;
            autoBtnText = null;

            Button spdBtn = Create3DButton(battleHudLayer.transform, "SpeedBtn", new Vector2(1f, 1f), new Vector2(-96f, -118f), new Vector2(168f, 86f), btnBlueSprite, "", 26, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.ToggleSpeedMultiplier();
            });
            speedBtnText = null;
            speedBtnIconImg = CreateIconImage(spdBtn.transform, "SpeedIcon", "Sprites/UI/icon_speed_1", new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(96f, 66f));

            battleLiveStatsText = CreateOverlayText(battleHudLayer.transform, "BattleResources", 26,
                Color.white, topCenter, -200f, new Vector2(660f, 48f));

            Button bSetBtn = Create3DButton(battleHudLayer.transform, "BattleSettingsBtn", new Vector2(1f, 1f), new Vector2(-66f, -220f), new Vector2(104f, 104f), btnPurpleSprite, "", 20, () => ShowSettingsModal());
            CreateIconImage(bSetBtn.transform, "Gear", "Sprites/UI/icon_settings", new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(66f, 66f));

            // Standalone Defense Fence HP Bar anchored at bottom (Enlarged height & font!)
            GameObject wallBarGo = CreateProgressBar(battleHudLayer.transform, "WallHpProgressBar", new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(900f, 52f),
                new Color(0.26f, 0.88f, 0.45f, 1f), out wallHpFillRt, out wallHpText);
            RectTransform wbRt = wallBarGo.GetComponent<RectTransform>();
            wbRt.anchorMin = new Vector2(0f, 0f);
            wbRt.anchorMax = new Vector2(1f, 0f);
            wbRt.anchoredPosition = new Vector2(0f, 40f);
            wbRt.sizeDelta = new Vector2(-32f, 52f);
            if (wallHpText != null) GameTypography.Apply(wallHpText, 25);

            if (wallHpFillRt != null)
                wallHpFillImg = wallHpFillRt.GetComponent<Image>();

            skillSlotsContainer = null;
        }

        public void RefreshSkillSlotBar()
        {
            if (skillSlotsContainer == null || GameManager.Instance == null) return;

            foreach (Transform child in skillSlotsContainer)
                Destroy(child.gameObject);
            skillWidgets.Clear();

            var cats = GameManager.Instance.deployedCats;
            if (cats.Count == 0)
            {
                GameObject emptyBanner = CreateCurrencyPill(skillSlotsContainer, "SoloHintBanner", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                CreateLabel(emptyBanner.transform,
                    "CHỈ HUY ĐANG CHIẾN ĐẤU ĐƠN ĐỘC!\n<size=24><color=#FFE066>Bắn hạ chuột để lên cấp & Triệu hồi 4 Chiến Binh Mèo vào 4 Ổ Mèo phía trước hàng rào!</color></size>",
                    22, Color.white, TextAnchor.MiddleCenter, new Vector2(18f, 10f), new Vector2(-18f, -10f));
                return;
            }

            for (int i = 0; i < cats.Count; i++)
            {
                CatCompanionController cat = cats[i];
                if (cat == null || cat.definition == null) continue;

                GameObject cardObj = CreateCardPanel(skillSlotsContainer, $"SkillSlot_{cat.definition.id}", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.18f, 0.22f, 0.38f, 1f));
                Button btn = cardObj.AddComponent<Button>();
                CatCompanionController capturedCat = cat;
                btn.onClick.AddListener(() =>
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.ShowCatAttackRangeForController(capturedCat);
                    capturedCat.TryTriggerSkill();
                });

                Image iconImg = CreateIconImage(cardObj.transform, "Portrait", cat.definition.iconResourcePath, new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(72f, 72f));
                iconImg.color = cat.definition.GetActiveSkinTint();

                GameObject cdObj = new GameObject("CooldownOverlay", typeof(RectTransform), typeof(Image));
                cdObj.transform.SetParent(iconImg.transform, false);
                StretchFull(cdObj.GetComponent<RectTransform>());
                Image cdImg = cdObj.GetComponent<Image>();
                cdImg.sprite = whitePixelSprite;
                cdImg.color = new Color(0f, 0f, 0f, 0.68f);
                cdImg.type = Image.Type.Filled;
                cdImg.fillMethod = Image.FillMethod.Vertical;
                cdImg.fillOrigin = 0;

                string rank = cat.definition.IsAwakened ? "✦ THỨC TỈNH" : $"[{cat.definition.rarity}] Bậc {cat.definition.battleStageLevel}";
                Text lvText = CreateLabel(cardObj.transform, rank, 16, new Color(1f, 0.9f, 0.3f), TextAnchor.UpperCenter, new Vector2(8f, 0f), new Vector2(-8f, -10f));
                CreateLabel(cardObj.transform, $"{cat.definition.displayName}\n<size=18><color=#80E5FF>Công {Mathf.RoundToInt(cat.CurrentDamage)} • Tầm {cat.CurrentAttackRange:0.0}m</color></size>",
                    17, Color.white, TextAnchor.MiddleCenter, new Vector2(6f, 34f), new Vector2(-6f, -104f));
                Text cdTxt = CreateLabel(cardObj.transform, "SẴN SÀNG", 16, new Color(0.45f, 1f, 0.58f), TextAnchor.LowerCenter, new Vector2(8f, 10f), new Vector2(-8f, -152f));

                skillWidgets.Add(new SkillSlotWidget
                {
                    linkedCat = cat,
                    rootObj = cardObj,
                    iconImg = iconImg,
                    cooldownOverlay = cdImg,
                    cooldownText = cdTxt,
                    levelBadgeText = lvText
                });
            }
        }

        // ====================================================================
        // PART 3: FULL-BLEED MODALS (Settings, Level-Up + AdMob Reroll, Game Over + AdMob Revive!)
        // ====================================================================
        private void BuildSettingsModal(Transform canvasRoot)
        {
            settingsModalObj = CreateFullScreenDimmer(canvasRoot, "SettingsModal");
            GameObject box = CreateGoldNavyCard(settingsModalObj.transform, "SettingsBox", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, 840f));

            CreateLabel(box.transform, "⚙ CÀI ĐẶT ÂM THANH & ADMOB", 32, new Color(1f, 0.88f, 0.35f), TextAnchor.UpperCenter, new Vector2(20f, 20f), new Vector2(-20f, -32f));

            CreateSliderRow(box.transform, "BgmRow", new Vector2(0f, 155f), "Nhạc Nền (BGM)", 0.65f, out bgmSlider, out bgmValText, (val) =>
            {
                if (CatFightAudioManager.Instance != null)
                    CatFightAudioManager.Instance.SetBgmVolume(val);
                if (bgmValText != null)
                    bgmValText.text = $"{Mathf.RoundToInt(val * 100f)}%";
            });

            CreateSliderRow(box.transform, "SfxRow", new Vector2(0f, 10f), "Hiệu Ứng Âm Thanh (SFX)", 0.85f, out sfxSlider, out sfxValText, (val) =>
            {
                if (CatFightAudioManager.Instance != null)
                    CatFightAudioManager.Instance.SetSfxVolume(val);
                if (sfxValText != null)
                    sfxValText.text = $"{Mathf.RoundToInt(val * 100f)}%";
            });

            Button mBtn = Create3DButton(box.transform, "MuteToggleBtn", new Vector2(0.5f, 0.5f), new Vector2(0f, -145f), new Vector2(540f, 92f), btnGreenSprite, "ÂM THANH: ĐANG BẬT", 26, () =>
            {
                if (CatFightAudioManager.Instance != null)
                {
                    bool nextMute = !CatFightAudioManager.Instance.IsMuted;
                    CatFightAudioManager.Instance.SetMuted(nextMute);
                    CatFightAudioManager.Instance.PlayUiClick();
                    RefreshSettingsUI();
                }
            });
            muteBtnBg = mBtn.GetComponent<Image>();
            muteBtnText = mBtn.GetComponentInChildren<Text>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Create3DButton(box.transform, "OpenDeveloperTools", new Vector2(0.5f, 0.5f), new Vector2(0f, -240f), new Vector2(540f, 78f), btnBlueSprite, "DEBUG & PROFILER", 27, OpenDeveloperTools);
#endif
            Create3DButton(box.transform, "CloseSettingsBtn", new Vector2(0.5f, 0f), new Vector2(0f, 82f), new Vector2(400f, 92f), btnRedSprite, "ĐÓNG & TIẾP TỤC", 26, () =>
            {
                settingsModalObj.SetActive(false);
                if (GameManager.Instance != null && GameManager.Instance.CurrentAppState == GameAppState.Battle)
                    GameManager.Instance.SetPaused(false);
            });

            settingsModalObj.SetActive(false);
        }

        private void CreateSliderRow(Transform parent, string name, Vector2 offset, string title, float initialVal, out Slider sliderOut, out Text valueLabelOut, UnityEngine.Events.UnityAction<float> onValueChanged)
        {
            GameObject row = CreateCurrencyPill(parent, name, new Vector2(0.5f, 0.5f), offset, new Vector2(780f, 122f));
            CreateLabel(row.transform, title, 24, Color.white, TextAnchor.UpperLeft, new Vector2(28f, 12f), new Vector2(-140f, -14f));
            valueLabelOut = CreateLabel(row.transform, $"{Mathf.RoundToInt(initialVal * 100f)}%", 24, new Color(0.45f, 0.92f, 1f), TextAnchor.UpperRight, new Vector2(28f, 12f), new Vector2(-28f, -14f));

            GameObject sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(row.transform, false);
            RectTransform srt = sliderGo.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 0f);
            srt.anchorMax = new Vector2(0.5f, 0f);
            srt.anchoredPosition = new Vector2(0f, 36f);
            srt.sizeDelta = new Vector2(720f, 38f);

            GameObject bg = CreateCardPanel(sliderGo.transform, "Background", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
            bg.GetComponent<Image>().sprite = barTrackSprite;

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            RectTransform fart = fillArea.GetComponent<RectTransform>();
            fart.anchorMin = Vector2.zero;
            fart.anchorMax = Vector2.one;
            fart.offsetMin = new Vector2(6f, 6f);
            fart.offsetMax = new Vector2(-6f, -6f);

            GameObject fill = CreateCardPanel(fillArea.transform, "Fill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.28f, 0.82f, 1f, 1f));
            fill.GetComponent<Image>().sprite = barFillSprite;

            Slider sl = sliderGo.GetComponent<Slider>();
            sl.minValue = 0f;
            sl.maxValue = 1f;
            sl.value = initialVal;
            sl.fillRect = fill.GetComponent<RectTransform>();
            sl.targetGraphic = fill.GetComponent<Image>();
            sl.onValueChanged.AddListener(onValueChanged);

            sliderOut = sl;
        }

        public void ShowSettingsModal()
        {
            if (settingsModalObj == null) return;
            if (CatFightAudioManager.Instance != null) CatFightAudioManager.Instance.PlayUiClick();
            if (GameManager.Instance != null && GameManager.Instance.CurrentAppState == GameAppState.Battle)
                GameManager.Instance.SetPaused(true);

            settingsModalObj.SetActive(true);
            RefreshSettingsUI();
        }

        private void RefreshSettingsUI()
        {
            if (CatFightAudioManager.Instance == null) return;
            var am = CatFightAudioManager.Instance;
            if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(am.BgmVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(am.SfxVolume);
            if (bgmValText != null) bgmValText.text = $"{Mathf.RoundToInt(am.BgmVolume * 100f)}%";
            if (sfxValText != null) sfxValText.text = $"{Mathf.RoundToInt(am.SfxVolume * 100f)}%";

            if (muteBtnText != null)
                muteBtnText.text = am.IsMuted ? "ÂM THANH: ĐANG TẮT" : "ÂM THANH: ĐANG BẬT";
            if (muteBtnBg != null)
                muteBtnBg.sprite = am.IsMuted ? btnRedSprite : btnGreenSprite;
        }

        private void BuildLevelUpModal(Transform canvasRoot)
        {
            upgradeModalObj = CreateFullScreenDimmer(canvasRoot, "LevelUpUpgradeModal");
            GameObject box = CreateGoldNavyCard(upgradeModalObj.transform, "ModalBox", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 1320f));

            CreateLabel(box.transform, "★ THĂNG CẤP CHIẾN ĐẤU! ★", 34, new Color(1f, 0.88f, 0.35f), TextAnchor.UpperCenter, new Vector2(20f, 20f), new Vector2(-20f, -28f));
            CreateLabel(box.transform, "Chọn 1 Chiến Binh Mèo vào Ổ Mèo hoặc Cường Hóa Vũ Khí", 22, new Color(0.82f, 0.92f, 1f), TextAnchor.UpperCenter, new Vector2(20f, 20f), new Vector2(-20f, -74f));

            GameObject cardsCol = new GameObject("CardsContainer", typeof(RectTransform));
            cardsCol.transform.SetParent(box.transform, false);
            RectTransform colRt = cardsCol.GetComponent<RectTransform>();
            colRt.anchorMin = new Vector2(0.5f, 0.5f);
            colRt.anchorMax = new Vector2(0.5f, 0.5f);
            colRt.anchoredPosition = new Vector2(0f, 12f);
            colRt.sizeDelta = new Vector2(890f, 940f);

            VerticalLayoutGroup vlg = cardsCol.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            upgradeCardsContainer = cardsCol.transform;

            // AdMob Bonus Button: Watch Ad to Claim ALL 3 Upgrades at once! (Includes icon_ad_video)
            Create3DAdButton(box.transform, "AdClaimAllThreeBtn", new Vector2(0.5f, 0f), new Vector2(0f, 82f), new Vector2(720f, 104f), btnPurpleSprite,
                "XEM QUẢNG CÁO: NHẬN CẢ 3 KỸ NĂNG!", 24, () =>
                {
                    if (GoogleAdMobManager.Instance != null && GameManager.Instance != null)
                    {
                        var currentOptions = displayedUpgradeChoices;
                        if (currentOptions == null) return;
                        GoogleAdMobManager.Instance.ShowRewardedAd("Nhận Cả 3 Kỹ Năng", "Kích hoạt đồng thời cả 3 thẻ nâng cấp!", () =>
                        {
                            foreach (var opt in currentOptions)
                                opt.onSelect?.Invoke();
                            upgradeModalObj.SetActive(false);
                            GameManager.Instance.SetPaused(false);
                            RefreshSkillSlotBar();
                            RefreshAllUI();
                        });
                    }
                });

            upgradeModalObj.SetActive(false);
        }

        public void ShowLevelUpUpgradeModal()
        {
            if (GameManager.Instance == null || upgradeModalObj == null) return;

            GameManager.Instance.SetPaused(true);
            upgradeModalObj.SetActive(true);

            foreach (Transform c in upgradeCardsContainer)
                Destroy(c.gameObject);

            displayedUpgradeChoices = GameManager.Instance.GenerateThreeUpgradeChoices();
            foreach (var opt in displayedUpgradeChoices)
            {
                var capturedOpt = opt;
                GameObject card = CreateCardPanel(upgradeCardsContainer, "UpgradeCard", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.22f, 0.26f, 0.44f, 1f));
                Button b = card.AddComponent<Button>();

                CreateIconImage(card.transform, "Icon", null, new Vector2(0f, 0.5f), new Vector2(118f, 0f), new Vector2(158f, 158f)).sprite = capturedOpt.icon;

                CreateLabel(card.transform, $"{capturedOpt.title}\n<size=24><color=#80E5FF>{capturedOpt.subtitle}</color></size>",
                    26, capturedOpt.cardColor, TextAnchor.UpperLeft, new Vector2(220f, 18f), new Vector2(-24f, -20f));

                CreateLabel(card.transform, capturedOpt.description,
                    21, Color.white, TextAnchor.LowerLeft, new Vector2(220f, 22f), new Vector2(-24f, -92f));

                b.onClick.AddListener(() =>
                {
                    if (CatFightAudioManager.Instance != null) CatFightAudioManager.Instance.PlayUiClick();
                    capturedOpt.onSelect?.Invoke();
                    upgradeModalObj.SetActive(false);
                    GameManager.Instance.SetPaused(false);
                    RefreshSkillSlotBar();
                    RefreshAllUI();
                });
            }
        }

        private void BuildGameOverModal(Transform canvasRoot)
        {
            gameOverModalObj = CreateFullScreenDimmer(canvasRoot, "GameOverModal");
            GameObject box = CreateGoldNavyCard(gameOverModalObj.transform, "GameOverBox", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 360f));

            gameOverSummaryText = CreateLabel(box.transform, "PHÒNG TUYẾN THẤT THỦ!", 30, Color.white, TextAnchor.UpperCenter, new Vector2(30f, 276f), new Vector2(-30f, -28f));

            // 1. Defeat Mode Buttons: AdMob Revive + Return to Menu + Retry Stage
            Button reviveBtn = Create3DAdButton(box.transform, "AdReviveBtn", new Vector2(0.5f, 0f), new Vector2(0f, 208f), new Vector2(780f, 104f), btnPurpleSprite,
                "XEM QUẢNG CÁO: HỒI SINH 100% TƯỜNG!", 25, () =>
                {
                    if (GoogleAdMobManager.Instance != null && GameManager.Instance != null)
                    {
                        GoogleAdMobManager.Instance.ShowRewardedAd("Hồi Sinh Phòng Tuyến", "Khôi phục 100% Máu Tường & Làm Choáng Đàn Chuột", () =>
                        {
                            gameOverModalObj.SetActive(false);
                            GameManager.Instance.ReviveBattleFromAdMob();
                            RefreshAllUI();
                        });
                    }
                });
            adReviveBtnObj = reviveBtn.gameObject;

            defeatButtonsGroupObj = new GameObject("DefeatButtonsGroup", typeof(RectTransform));
            defeatButtonsGroupObj.transform.SetParent(box.transform, false);
            StretchFull(defeatButtonsGroupObj.GetComponent<RectTransform>());

            Create3DButton(defeatButtonsGroupObj.transform, "ReturnMenuBtn", new Vector2(0.5f, 0f), new Vector2(-200f, 84f), new Vector2(360f, 96f), btnBlueSprite, "VỀ SẢNH NÂNG CẤP", 24, () =>
            {
                gameOverModalObj.SetActive(false);
                if (GameManager.Instance != null)
                    GameManager.Instance.ReturnToMainMenu();
            });

            Create3DButton(defeatButtonsGroupObj.transform, "RestartBtn", new Vector2(0.5f, 0f), new Vector2(200f, 84f), new Vector2(360f, 96f), btnGreenSprite, "THỬ LẠI MÀN NÀY", 24, () =>
            {
                gameOverModalObj.SetActive(false);
                if (GameManager.Instance != null)
                    GameManager.Instance.StartBattleFromMainMenu();
            });

            // 2. Victory Mode Buttons: AdMob Double Reward + Claim & Return to Main Menu (to Upgrade & Press Battle for Next Stage!)
            Button adDoubleBtn = Create3DAdButton(box.transform, "AdDoubleRewardBtn", new Vector2(0.5f, 0f), new Vector2(0f, 208f), new Vector2(780f, 104f), btnPurpleSprite,
                "XEM QUẢNG CÁO: NHẬN X2 THƯỞNG MÀN!", 25, () =>
                {
                    if (GoogleAdMobManager.Instance != null && GameManager.Instance != null)
                    {
                        var gm = GameManager.Instance;
                        GoogleAdMobManager.Instance.ShowRewardedAd("Nhận x2 Thưởng Vượt Màn", $"+{gm.lastStageRewardGold:N0} Vàng & +{gm.lastStageRewardGems:N0} Kim Cương", () =>
                        {
                            gm.gold += gm.lastStageRewardGold;
                            gm.gems += gm.lastStageRewardGems;
                            gameOverModalObj.SetActive(false);
                            gm.ReturnToMainMenu();
                        });
                    }
                });
            adDoubleStageRewardBtnObj = adDoubleBtn.gameObject;

            Button claimMenuBtn = Create3DButton(box.transform, "VictoryClaimMenuBtn", new Vector2(0.5f, 0f), new Vector2(0f, 84f), new Vector2(780f, 98f), btnGreenSprite,
                "VỀ SẢNH", 26, () =>
                {
                    if (CatFightAudioManager.Instance != null) CatFightAudioManager.Instance.PlayUiClick();
                    gameOverModalObj.SetActive(false);
                    if (GameManager.Instance != null)
                        GameManager.Instance.ReturnToMainMenu();
                });
            victoryClaimMenuBtnObj = claimMenuBtn.gameObject;

            gameOverModalObj.SetActive(false);
        }

        public void ShowStageVictoryModal()
        {
            if (gameOverModalObj == null || GameManager.Instance == null) return;
            gameOverModalObj.SetActive(true);
            gameOverModalObj.transform.SetAsLastSibling();

            if (adReviveBtnObj != null) adReviveBtnObj.SetActive(false);
            if (defeatButtonsGroupObj != null) defeatButtonsGroupObj.SetActive(false);
            if (adDoubleStageRewardBtnObj != null) adDoubleStageRewardBtnObj.SetActive(true);
            if (victoryClaimMenuBtnObj != null) victoryClaimMenuBtnObj.SetActive(true);

            gameOverSummaryText.text = "<size=49><color=#6BFF8B>ĐÃ QUÉT SẠCH CHUỘT!</color></size>";
            FitGameOverCard();

        }

        public void ShowGameOverModal()
        {
            if (gameOverModalObj == null || GameManager.Instance == null) return;
            var gm = GameManager.Instance;
            gameOverModalObj.SetActive(true);
            gameOverModalObj.transform.SetAsLastSibling();

            if (adReviveBtnObj != null) adReviveBtnObj.SetActive(true);
            if (defeatButtonsGroupObj != null) defeatButtonsGroupObj.SetActive(true);
            if (adDoubleStageRewardBtnObj != null) adDoubleStageRewardBtnObj.SetActive(false);
            if (victoryClaimMenuBtnObj != null) victoryClaimMenuBtnObj.SetActive(false);

            int curPower = gm.GetCurrentPowerScore();
            int recPower = gm.GetRecommendedPowerForStage(gm.currentChapter, gm.currentStage);
            string powerColor = curPower >= recPower ? "#6BFF8B" : "#FF6B6B";

            gameOverSummaryText.text =
                $"<size=41><color=#FF6B6B>PHÒNG TUYẾN THẤT THỦ!</color></size>\n\n" +
                $"Màn hiện tại: <color=#FFE066>{gm.GetStageCode()}</color> (Đợt {gm.currentWave}/{gm.totalWavesInStage}) • Diệt: {gm.totalKills} chuột\n" +
                $"Lực Chiến Của Bạn: <color={powerColor}>{curPower:N0}</color>  /  LC Đề Xuất Màn {gm.GetStageCode()}: <color=#FFE066>{recPower:N0}</color>\n" +
                $"Thưởng tích lũy trận này: <color=#FFE066>+{gm.lastStageRewardGold:N0} Vàng</color> • <color=#5CE1E6>+{gm.lastStageRewardGems:N0} KC</color>\n\n" +
                $"<size=26><color=#80E5FF>Mẹo: Hãy về Sảnh nâng cấp Nhân Vật, Tường Rào hoặc Mèo Cưng để đủ Lực Chiến vượt qua màn này!</color></size>";
            FitGameOverCard();
        }

        private void FitGameOverCard()
        {
            RectTransform label = gameOverSummaryText.rectTransform;
            RectTransform card = (RectTransform)label.parent;
            card.ForceUpdateRectTransforms();
            float verticalMargins = label.offsetMin.y - label.offsetMax.y;
            card.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                verticalMargins + Mathf.Ceil(gameOverSummaryText.preferredHeight));
        }

        // ====================================================================
        // PART 4: GACHA ROLL ANIMATION & REVEAL MODAL
        // ====================================================================
        private void BuildGachaAnimationModal(Transform canvasRoot)
        {
            gachaAnimModalObj = CreateFullScreenDimmer(canvasRoot, "GachaRollAnimationModal");
            GameObject box = CreateGoldNavyCard(gachaAnimModalObj.transform, "GachaModalBox", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 1340f));

            // Golden Header Ribbon
            GameObject ribbon = new GameObject("GachaHeaderRibbon", typeof(RectTransform), typeof(Image));
            ribbon.transform.SetParent(box.transform, false);
            RectTransform rrt = ribbon.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.5f, 1f);
            rrt.anchorMax = new Vector2(0.5f, 1f);
            rrt.anchoredPosition = new Vector2(0f, -86f);
            rrt.sizeDelta = new Vector2(880f, 118f);
            Image rImg = ribbon.GetComponent<Image>();
            rImg.sprite = ribbonHeaderSprite;
            rImg.type = Image.Type.Sliced;
            CreateLabel(ribbon.transform, "★ VÒNG QUAY TRIỆU HỒI ★", 32, Color.white, TextAnchor.MiddleCenter, new Vector2(20f, 12f), new Vector2(-20f, -6f));

            // Stage 1: Shaking Gacha Capsule / Chest & Spinning Magic Circle
            gachaStageChestGroup = new GameObject("Stage1_ChestShaker", typeof(RectTransform));
            gachaStageChestGroup.transform.SetParent(box.transform, false);
            StretchFull(gachaStageChestGroup.GetComponent<RectTransform>());

            Image magicCircle = CreateIconImage(gachaStageChestGroup.transform, "SpinningMagicCircle", "Sprites/Effects/fx_magic_circle", new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(480f, 480f));
            magicCircle.color = new Color(1f, 0.86f, 0.35f, 0.9f);
            gachaSpinCircleRt = magicCircle.GetComponent<RectTransform>();

            gachaChestIconImg = CreateIconImage(gachaStageChestGroup.transform, "ShakingCapsule", "Sprites/UI/icon_gacha_capsule", new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(275f, 275f));
            gachaChestIconRt = gachaChestIconImg.GetComponent<RectTransform>();

            gachaStatusText = CreateLabel(gachaStageChestGroup.transform, "✨ ĐANG MỞ PHONG ẤN RƯƠNG... ✨", 30, new Color(1f, 0.92f, 0.42f), TextAnchor.MiddleCenter);
            RectTransform stRt = gachaStatusText.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0.5f, 0.5f);
            stRt.anchorMax = new Vector2(0.5f, 0.5f);
            stRt.anchoredPosition = new Vector2(0f, -245f);
            stRt.sizeDelta = new Vector2(840f, 90f);

            // Stage 2: Revealed Reward Cards Container
            gachaCardsGroupObj = new GameObject("Stage2_RewardCardsGroup", typeof(RectTransform));
            gachaCardsGroupObj.transform.SetParent(box.transform, false);
            StretchFull(gachaCardsGroupObj.GetComponent<RectTransform>());

            GameObject cardsCol = new GameObject("RolledCardsContainer", typeof(RectTransform));
            cardsCol.transform.SetParent(gachaCardsGroupObj.transform, false);
            RectTransform cRt = cardsCol.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = new Vector2(0f, 18f);
            cRt.sizeDelta = new Vector2(890f, 890f);

            VerticalLayoutGroup vlg = cardsCol.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            gachaCardsContainer = cardsCol.transform;

            // Bottom Action Buttons (Shown after cards reveal)
            gachaCloseButtonsGroup = new GameObject("GachaBottomActions", typeof(RectTransform));
            gachaCloseButtonsGroup.transform.SetParent(gachaCardsGroupObj.transform, false);
            RectTransform actRt = gachaCloseButtonsGroup.GetComponent<RectTransform>();
            actRt.anchorMin = new Vector2(0.5f, 0f);
            actRt.anchorMax = new Vector2(0.5f, 0f);
            actRt.anchoredPosition = new Vector2(0f, 88f);
            actRt.sizeDelta = new Vector2(890f, 118f);

            Create3DAdButton(gachaCloseButtonsGroup.transform, "GachaModalAdRollAgainBtn", new Vector2(0f, 0.5f), new Vector2(215f, 0f), new Vector2(420f, 102f), btnPurpleSprite,
                "XEM AD QUAY TIẾP", 23, () => RequestFreeRoll(rewardCategory));

            Create3DButton(gachaCloseButtonsGroup.transform, "GachaModalCloseBtn", new Vector2(1f, 0.5f), new Vector2(-215f, 0f), new Vector2(420f, 102f), btnGreenSprite,
                "NHẬN THƯỞNG (ĐÓNG)", 24, () =>
                {
                    if (CatFightAudioManager.Instance != null) CatFightAudioManager.Instance.PlayUiClick();
                    gachaAnimModalObj.SetActive(false);
                });

            // Supernova White/Gold Flash Overlay
            GameObject flashGo = new GameObject("SupernovaFlash", typeof(RectTransform), typeof(Image));
            flashGo.transform.SetParent(box.transform, false);
            StretchFull(flashGo.GetComponent<RectTransform>());
            gachaFlashImg = flashGo.GetComponent<Image>();
            gachaFlashImg.sprite = whitePixelSprite;
            gachaFlashImg.color = new Color(1f, 0.96f, 0.78f, 0f);
            gachaFlashImg.raycastTarget = false;

            gachaAnimModalObj.SetActive(false);
        }

        public void ShowGachaRollAnimation(ShopGachaCategory category, List<GachaRollResultItem> items, bool isFromAd)
        {
            if (gachaAnimModalObj == null || items == null || items.Count == 0) return;

            rewardCategory = category;
            gachaAnimModalObj.SetActive(true);
            if (activeGachaAnimCoroutine != null)
                StopCoroutine(activeGachaAnimCoroutine);

            activeGachaAnimCoroutine = StartCoroutine(PlayGachaRollAnimationRoutine(category, items, isFromAd));
        }

        private IEnumerator PlayGachaRollAnimationRoutine(ShopGachaCategory category, List<GachaRollResultItem> items, bool isFromAd)
        {
            gachaStageChestGroup.SetActive(true);
            gachaCardsGroupObj.SetActive(false);
            gachaCloseButtonsGroup.SetActive(false);
            gachaFlashImg.color = new Color(1f, 0.96f, 0.78f, 0f);

            string cratePath = "Sprites/UI/icon_gacha_capsule";
            if (category == ShopGachaCategory.WeaponArsenal) cratePath = "Sprites/UI/icon_gacha_weapon";
            else if (category == ShopGachaCategory.SkinWardrobe) cratePath = "Sprites/UI/icon_gacha_skin";
            gachaChestIconImg.sprite = RuntimeSpriteBank.GetSingle(cratePath, 100f);

            gachaStatusText.text = isFromAd
                ? "✨ ĐANG MỞ RƯƠNG QUẢNG CÁO MIỄN PHÍ... ✨"
                : $"✨ ĐANG KHAI MỞ RƯƠNG ({items.Count} VẬT PHẨM)... ✨";

            // Stage 1: Capsule Shake, Charge & Sparkle Eruption (0.85s)
            float chargeTime = 0.85f;
            float elapsed = 0f;
            float nextSparkleTick = 0f;

            while (elapsed < chargeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(elapsed / chargeTime);

                // Rotate magic circle faster as p increases
                gachaSpinCircleRt.Rotate(0f, 0f, -Time.unscaledDeltaTime * Mathf.Lerp(140f, 520f, p));
                float circleScale = Mathf.Lerp(0.75f, 1.35f, p) + Mathf.Sin(elapsed * 24f) * 0.05f;
                gachaSpinCircleRt.localScale = Vector3.one * circleScale;

                // Vigorous shake + swell on the capsule
                float shakeAmp = Mathf.Lerp(4f, 26f, p * p);
                float shakeX = Mathf.Sin(elapsed * 65f) * shakeAmp;
                float shakeY = Mathf.Cos(elapsed * 52f) * (shakeAmp * 0.45f);
                gachaChestIconRt.anchoredPosition = new Vector2(shakeX, 40f + shakeY);
                gachaChestIconRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(elapsed * 48f) * Mathf.Lerp(4f, 18f, p));

                float swell = Mathf.Lerp(0.95f, 1.38f, p * p);
                gachaChestIconRt.localScale = new Vector3(
                    swell + Mathf.Sin(elapsed * 32f) * 0.08f,
                    swell - Mathf.Sin(elapsed * 32f) * 0.08f,
                    1f);

                if (elapsed >= nextSparkleTick)
                {
                    nextSparkleTick = elapsed + 0.11f;
                    StartCoroutine(SpawnUiSparkleBurstRoutine(gachaStageChestGroup.transform, new Vector2(0f, 40f), 4, 240f, 0.45f));
                }

                yield return null;
            }

            // Stage 2: Supernova Flash & Sparkle Explosion!
            gachaChestIconRt.anchoredPosition = new Vector2(0f, 40f);
            gachaChestIconRt.localRotation = Quaternion.identity;
            gachaChestIconRt.localScale = Vector3.one;

            gachaFlashImg.color = new Color(1f, 0.97f, 0.82f, 0.95f);
            if (CatFightAudioManager.Instance != null)
                CatFightAudioManager.Instance.PlayGacha();

            StartCoroutine(SpawnUiSparkleBurstRoutine(gachaAnimModalObj.transform, Vector2.zero, 16, 480f, 0.7f));

            gachaStageChestGroup.SetActive(false);
            gachaCardsGroupObj.SetActive(true);

            foreach (Transform c in gachaCardsContainer)
                Destroy(c.gameObject);

            // Fade out the flash smoothly in background
            StartCoroutine(FadeOutGachaFlashRoutine());

            // Stage 3: Pop in each rolled item card sequentially with springy bounce!
            bool singleItem = items.Count == 1;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                GameObject card = CreateCardPanel(gachaCardsContainer, $"RolledCard_{i}", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                    new Color(0.18f, 0.22f, 0.38f, 1f));

                // Left Rarity Glow Halo + Item Sprite
                float iconBoxSize = singleItem ? 240f : 124f;
                float iconOffsetX = singleItem ? 165f : 92f;

                Image halo = CreateIconImage(card.transform, "RarityHalo", "Sprites/Effects/fx_magic_circle", new Vector2(0f, 0.5f), new Vector2(iconOffsetX, 0f), new Vector2(iconBoxSize * 1.18f, iconBoxSize * 1.18f));
                halo.color = new Color(item.rarityColor.r, item.rarityColor.g, item.rarityColor.b, 0.65f);

                Image itemImg = CreateIconImage(card.transform, "ItemSprite", null, new Vector2(0f, 0.5f), new Vector2(iconOffsetX, 0f), new Vector2(iconBoxSize, iconBoxSize));
                itemImg.sprite = item.iconSprite;
                itemImg.color = item.iconTint;

                float textLeft = singleItem ? 310f : 178f;
                int titleFont = singleItem ? 34 : 25;
                int subFont = singleItem ? 26 : 21;

                string newBadge = item.isNewUnlock ? " <color=#6BFF8B>[MỚI!]</color>" : "";
                CreateLabel(card.transform, $"[{item.rarity}] {item.itemName}{newBadge}", titleFont, item.rarityColor, TextAnchor.UpperLeft,
                    new Vector2(textLeft, 12f), new Vector2(-24f, singleItem ? -85f : -16f));

                CreateLabel(card.transform, item.subText, subFont, Color.white, TextAnchor.LowerLeft,
                    new Vector2(textLeft, singleItem ? 85f : 16f), new Vector2(-24f, -12f));

                if (CatFightAudioManager.Instance != null)
                    CatFightAudioManager.Instance.PlayUiClick();

                // Elastic Pop-In Animation for this card
                RectTransform cardRt = card.GetComponent<RectTransform>();
                float popDur = 0.18f;
                float pt = 0f;
                while (pt < popDur)
                {
                    pt += Time.unscaledDeltaTime;
                    float u = Mathf.Clamp01(pt / popDur);
                    float scale = u < 0.7f
                        ? Mathf.Lerp(0.2f, 1.12f, u / 0.7f)
                        : Mathf.Lerp(1.12f, 1.0f, (u - 0.7f) / 0.3f);
                    cardRt.localScale = Vector3.one * scale;
                    yield return null;
                }
                cardRt.localScale = Vector3.one;
            }

            gachaCloseButtonsGroup.SetActive(true);
            activeGachaAnimCoroutine = null;
        }

        private IEnumerator FadeOutGachaFlashRoutine()
        {
            float dur = 0.35f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(0.92f, 0f, t / dur);
                if (gachaFlashImg != null)
                    gachaFlashImg.color = new Color(1f, 0.97f, 0.82f, a);
                yield return null;
            }
            if (gachaFlashImg != null)
                gachaFlashImg.color = new Color(1f, 0.97f, 0.82f, 0f);
        }

        private IEnumerator SpawnUiSparkleBurstRoutine(Transform parent, Vector2 centerPos, int count, float speed, float duration)
        {
            List<RectTransform> stars = new List<RectTransform>();
            List<Image> imgs = new List<Image>();
            List<Vector2> velocities = new List<Vector2>();

            Sprite sp = RuntimeSpriteBank.GetSingle("Sprites/Effects/fx_sparkle", 100f);
            Color[] palette = new Color[]
            {
                new Color(1f, 0.92f, 0.35f, 1f),
                new Color(0.45f, 0.92f, 1f, 1f),
                new Color(1f, 0.55f, 0.82f, 1f),
                new Color(0.55f, 1f, 0.65f, 1f)
            };

            for (int i = 0; i < count; i++)
            {
                GameObject sGo = new GameObject("UiGachaSparkle", typeof(RectTransform), typeof(Image));
                sGo.transform.SetParent(parent, false);
                RectTransform rt = sGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = centerPos;
                float sz = Random.Range(36f, 68f);
                rt.sizeDelta = new Vector2(sz, sz);

                Image img = sGo.GetComponent<Image>();
                img.sprite = sp;
                img.color = palette[i % palette.Length];
                img.raycastTarget = false;

                float angle = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                velocities.Add(dir * Random.Range(speed * 0.55f, speed * 1.15f));
                stars.Add(rt);
                imgs.Add(img);
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                for (int i = 0; i < stars.Count; i++)
                {
                    if (stars[i] == null) continue;
                    stars[i].anchoredPosition += velocities[i] * Time.unscaledDeltaTime;
                    stars[i].Rotate(0f, 0f, 260f * Time.unscaledDeltaTime);
                    stars[i].localScale = Vector3.one * (1f - p * 0.7f);
                    Color c = imgs[i].color;
                    imgs[i].color = new Color(c.r, c.g, c.b, 1f - p);
                }
                yield return null;
            }

            for (int i = 0; i < stars.Count; i++)
            {
                if (stars[i] != null)
                    Destroy(stars[i].gameObject);
            }
        }

        // ====================================================================
        // REFRESH ALL UI
        // ====================================================================
        private int lastHudChapter = -1;
        private int lastHudStage = -1;
        private int lastHudWave = -1;
        private int lastHudTotalWavesInStage = -1;
        private int lastHudSpawned = -1;
        private int lastHudTotalWave = -1;
        private int lastHudKills = -1;
        private int lastHudWallHp = -1;
        private int lastHudWallMaxHp = -1;
        private int lastHudSpeed = -1;
        private float lastHudExpRatio = -1f;
        private int lastHudGold = -1;
        private int lastHudDeployedCount = -1;

        public void RefreshAllUI()
        {
            using var sample = RefreshUIMarker.Auto();
            if (GameManager.Instance == null) return;
            var gm = GameManager.Instance;

            if (eventRewardDot != null)
                eventRewardDot.SetActive(gm.eventMissions.Exists(m => !m.claimed && m.currentProgress >= m.targetProgress));

            if (gm.CurrentAppState == GameAppState.MainMenu)
            {
                if (menuGoldText != null) menuGoldText.text = gm.gold.ToString("N0");
                if (menuGemText != null) menuGemText.text = gm.gems.ToString("N0");
                if (menuTicketText != null) menuTicketText.text = gm.summonTickets.ToString();

                int powerScore = gm.GetCurrentPowerScore();
                if (menuPowerText != null)
                    menuPowerText.text = $"CHỈ HUY\n<color=#6BFF8B>LC: {powerScore:N0}</color>";

                if (startBattleBtnTitleText != null)
                {
                    bool isMilestone = gm.IsKeyMilestoneStage(gm.currentChapter, gm.currentStage);
                    startBattleBtnTitleText.text = isMilestone
                        ? $"★ MÀN THEN CHỐT {gm.GetStageCode()} (BOSS) ★"
                        : $"⚔ CHIẾN ĐẤU: MÀN {gm.GetStageCode()} ⚔";
                }

                if (startBattleBtnSubText != null)
                {
                    int wavesCount = gm.GetTotalWavesForStage(gm.currentChapter, gm.currentStage);
                    int recPower = gm.GetRecommendedPowerForStage(gm.currentChapter, gm.currentStage);
                    string lcColor = powerScore >= recPower ? "#6BFF8B" : "#FFD166";
                    startBattleBtnSubText.text = $"{gm.GetChapterShortTitle(gm.currentChapter)} • {wavesCount} Đợt Quái • <color={lcColor}>LC Đề Xuất: {recPower:N0}</color>";
                }

                RefreshMenuCatShowcaseUI();

                if (activeMenuTab == MainMenuTab.Pet) RefreshPetTabUI();
                else if (activeMenuTab == MainMenuTab.Character) RefreshHeroTabUI();
                else if (activeMenuTab == MainMenuTab.Event) RefreshEventTabUI();
            }
            else
            {
                if (waveTitleText != null && (lastHudWave != gm.currentWave || lastHudStage != gm.currentStage || lastHudChapter != gm.currentChapter || lastHudTotalWavesInStage != gm.totalWavesInStage))
                {
                    lastHudWave = gm.currentWave;
                    lastHudStage = gm.currentStage;
                    lastHudChapter = gm.currentChapter;
                    lastHudTotalWavesInStage = gm.totalWavesInStage;
                    waveTitleText.text = $"MÀN {gm.GetStageCode()} • ĐỢT {gm.currentWave}/{gm.totalWavesInStage}";
                }

                if (killCountText != null && (lastHudSpawned != gm.enemiesSpawnedInWave || lastHudTotalWave != gm.totalEnemiesInWave || lastHudKills != gm.totalKills))
                {
                    lastHudSpawned = gm.enemiesSpawnedInWave;
                    lastHudTotalWave = gm.totalEnemiesInWave;
                    lastHudKills = gm.totalKills;
                    killCountText.text = $"Đợt này: {gm.enemiesSpawnedInWave}/{gm.totalEnemiesInWave} • Diệt: {gm.totalKills}";
                }

                if (battleLiveStatsText != null && (lastHudGold != gm.gold || lastHudDeployedCount != gm.deployedCats.Count))
                {
                    lastHudGold = gm.gold;
                    lastHudDeployedCount = gm.deployedCats.Count;
                    battleLiveStatsText.text = $"Vàng <color=#FFE066>{gm.gold:N0}</color>   •   Ổ mèo <color=#FFD2AC>{gm.deployedCats.Count}/4</color>";
                }

                float expRatio = Mathf.Clamp01((float)gm.currentExp / Mathf.Max(1, gm.expToNextLevel));
                if (Mathf.Abs(expRatio - lastHudExpRatio) > 0.002f)
                {
                    lastHudExpRatio = expRatio;
                    SetProgressBarRatio(expFillRt, expRatio);
                }

                int curHpInt = Mathf.CeilToInt(gm.wallCurrentHp);
                int maxHpInt = Mathf.CeilToInt(gm.wallMaxHp);
                if (lastHudWallHp != curHpInt || lastHudWallMaxHp != maxHpInt)
                {
                    lastHudWallHp = curHpInt;
                    lastHudWallMaxHp = maxHpInt;
                    float wallRatio = Mathf.Clamp01(gm.wallCurrentHp / Mathf.Max(1f, gm.wallMaxHp));
                    SetProgressBarRatio(wallHpFillRt, wallRatio);
                    if (wallHpFillImg != null)
                    {
                        wallHpFillImg.color = wallRatio > 0.5f
                            ? new Color(0.26f, 0.88f, 0.45f, 1f)
                            : (wallRatio > 0.25f ? new Color(1f, 0.72f, 0.22f, 1f) : new Color(1f, 0.28f, 0.35f, 1f));
                    }
                    if (wallHpText != null)
                    {
                        wallHpText.text = $"{curHpInt} / {maxHpInt}";
                    }
                }

                if (lastHudSpeed != gm.GameSpeedMultiplier)
                {
                    lastHudSpeed = gm.GameSpeedMultiplier;
                    if (speedBtnIconImg != null)
                    {
                        if (gm.GameSpeedMultiplier >= 4 && speed4Sprite != null) speedBtnIconImg.sprite = speed4Sprite;
                        else if (gm.GameSpeedMultiplier == 2 && speed2Sprite != null) speedBtnIconImg.sprite = speed2Sprite;
                        else if (speed1Sprite != null) speedBtnIconImg.sprite = speed1Sprite;
                    }
                }

                if (skillWidgets.Count != gm.deployedCats.Count)
                    RefreshSkillSlotBar();
            }
        }

        // ====================================================================
        // PART 7: SPLASH LOADING SCREEN (GAME IMAGE + APP ICON + FAKE PROGRESS BAR)
        // ====================================================================
        private void BuildSplashLoadingScreen(Transform canvasRoot)
        {
            splashLoadingModalObj = new GameObject("SplashLoadingScreen", typeof(RectTransform), typeof(Image));
            splashLoadingModalObj.transform.SetParent(canvasRoot, false);
            StretchFull(splashLoadingModalObj.GetComponent<RectTransform>());

            Image bgImg = splashLoadingModalObj.GetComponent<Image>();
            Sprite bgSp = RuntimeSpriteBank.GetSingle("Sprites/Environment/bg_splash_loading", 100f);
            if (bgSp != null)
            {
                bgImg.sprite = bgSp;
                bgImg.color = Color.white;
            }
            else
            {
                bgImg.color = new Color(0.10f, 0.18f, 0.42f, 1f);
            }

            // Center Game App Icon Artwork (580x580)
            Image appIconImg = CreateIconImage(splashLoadingModalObj.transform, "GameAppIcon", "Sprites/UI/game_app_icon",
                new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(580f, 580f));
            if (appIconImg != null)
                splashLogoRt = appIconImg.GetComponent<RectTransform>();

            // Clean Game Title below the Icon
            CreateLabel(splashLoadingModalObj.transform, "THÚ CƯNG BÃO ĐẠN", 54, new Color(1f, 0.90f, 0.28f),
                TextAnchor.MiddleCenter, new Vector2(20f, 640f), new Vector2(-20f, -1160f));

            // Fake Loading Progress Bar at the bottom — automatically enters game at 100%
            splashBarTrackObj = CreateProgressBar(splashLoadingModalObj.transform, "SplashLoadingBar",
                new Vector2(0.5f, 0f), new Vector2(0f, 220f), new Vector2(840f, 68f),
                new Color(0.28f, 0.92f, 0.52f, 1f), out splashBarFillRt, out splashBarLabelText);
            if (splashBarLabelText != null)
            {
                GameTypography.Apply(splashBarLabelText, 26);
                splashBarLabelText.text = "Đang tải... 0%";
            }

            splashEnterBtnObj = null;
        }

        public void ShowSplashLoadingScreen()
        {
            if (splashLoadingModalObj == null) return;
            isSplashLoadingActive = true;
            splashLoadingProgress = 0f;
            splashLoadingModalObj.SetActive(true);
            splashLoadingModalObj.transform.SetAsLastSibling();

            if (splashBarTrackObj != null) splashBarTrackObj.SetActive(true);
            SetProgressBarRatio(splashBarFillRt, 0f);
        }

        private void UpdateSplashLoadingScreen(float dt)
        {
            if (!isSplashLoadingActive || splashLoadingModalObj == null || !splashLoadingModalObj.activeSelf) return;

            if (splashLogoRt != null)
            {
                float bob = Mathf.Sin(Time.unscaledTime * 2.8f) * 8f;
                splashLogoRt.anchoredPosition = new Vector2(0f, 110f + bob);
            }

            splashLoadingProgress = Mathf.MoveTowards(splashLoadingProgress, 1f, dt * 0.62f);
            SetProgressBarRatio(splashBarFillRt, splashLoadingProgress);

            if (splashBarLabelText != null)
            {
                int pct = Mathf.RoundToInt(splashLoadingProgress * 100f);
                splashBarLabelText.text = $"Đang tải dữ liệu... {pct}%";
            }

            if (splashLoadingProgress >= 1f)
            {
                isSplashLoadingActive = false;
                splashLoadingModalObj.SetActive(false);
                if (CatFightAudioManager.Instance != null)
                {
                    CatFightAudioManager.Instance.PlayMenuShowcaseCatVoice(0.88f);
                }
            }
        }

        // ====================================================================
        // PART 8: BATTLE STAGE LOADING SCREEN (WHEN ENTERING OR RELOADING A STAGE)
        // ====================================================================
        private void BuildStageLoadingScreen(Transform canvasRoot)
        {
            stageLoadingModalObj = new GameObject("StageLoadingScreen", typeof(RectTransform), typeof(Image));
            stageLoadingModalObj.transform.SetParent(canvasRoot, false);
            StretchFull(stageLoadingModalObj.GetComponent<RectTransform>());

            stageLoadingBgImg = stageLoadingModalObj.GetComponent<Image>();
            Sprite bgSp = RuntimeSpriteBank.GetSingle("Sprites/Environment/bg_chap1_kitchen", 100f);
            if (bgSp != null)
            {
                stageLoadingBgImg.sprite = bgSp;
                stageLoadingBgImg.color = new Color(0.62f, 0.68f, 0.80f, 1f);
            }
            else
            {
                stageLoadingBgImg.color = new Color(0.10f, 0.16f, 0.34f, 1f);
            }

            Image iconImg = CreateIconImage(stageLoadingModalObj.transform, "StageLoadingAppIcon", "Sprites/UI/game_app_icon",
                new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(320f, 320f));
            if (iconImg != null)
                stageLoadingIconRt = iconImg.GetComponent<RectTransform>();

            CreateProgressBar(stageLoadingModalObj.transform, "StageLoadingBar",
                new Vector2(0.5f, 0f), new Vector2(0f, 220f), new Vector2(840f, 68f),
                new Color(0.28f, 0.92f, 0.52f, 1f), out stageLoadingBarFillRt, out Text loadingLabel);
            loadingLabel.gameObject.SetActive(false);

            stageLoadingModalObj.SetActive(false);
        }

        public void ShowStageLoadingScreen()
        {
            if (stageLoadingModalObj == null || GameManager.Instance == null) return;
            var gm = GameManager.Instance;

            isStageLoadingActive = true;
            stageLoadingProgress = 0f;
            stageLoadingModalObj.SetActive(true);
            stageLoadingModalObj.transform.SetAsLastSibling();

            if (stageLoadingBgImg != null)
            {
                Sprite chapBg = RuntimeSpriteBank.GetSingle(gm.GetChapterBackgroundPath(gm.currentChapter), 100f);
                if (chapBg != null)
                    stageLoadingBgImg.sprite = chapBg;
            }

            SetProgressBarRatio(stageLoadingBarFillRt, 0f);
        }

        private void UpdateStageLoadingScreen(float dt)
        {
            if (!isStageLoadingActive || stageLoadingModalObj == null || !stageLoadingModalObj.activeSelf) return;

            if (stageLoadingIconRt != null)
            {
                float bob = Mathf.Sin(Time.unscaledTime * 3.4f) * 7f;
                stageLoadingIconRt.anchoredPosition = new Vector2(0f, 70f + bob);
            }

            stageLoadingProgress = Mathf.MoveTowards(stageLoadingProgress, 1f, dt * 1.15f);
            SetProgressBarRatio(stageLoadingBarFillRt, stageLoadingProgress);

            if (stageLoadingProgress >= 1f)
            {
                isStageLoadingActive = false;
                stageLoadingModalObj.SetActive(false);
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.FinishStageLoadingAndStartWave1();
                }
            }
        }

        // ====================================================================
        // HELPER UI CONSTRUCTORS
        // ====================================================================
        private GameObject CreateFullScreenDimmer(Transform canvasRoot, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvasRoot, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-200f, -200f);
            rt.offsetMax = new Vector2(200f, 200f);

            Image img = go.GetComponent<Image>();
            img.sprite = whitePixelSprite;
            img.type = Image.Type.Simple;
            img.color = new Color(0.03f, 0.04f, 0.08f, 0.88f);
            return go;
        }

        private GameObject CreateProgressBar(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos, Vector2 size, Color fillColor, out RectTransform fillRectOut, out Text labelOut)
        {
            GameObject track = new GameObject(name, typeof(RectTransform), typeof(Image));
            track.transform.SetParent(parent, false);
            RectTransform trt = track.GetComponent<RectTransform>();
            trt.anchorMin = anchor;
            trt.anchorMax = anchor;
            trt.anchoredPosition = anchoredPos;
            trt.sizeDelta = size;

            Image tImg = track.GetComponent<Image>();
            tImg.sprite = barTrackSprite;
            tImg.type = Image.Type.Sliced;
            tImg.color = Color.white;
            tImg.raycastTarget = false;

            GameObject fillContainer = new GameObject("FillMaskArea", typeof(RectTransform));
            fillContainer.transform.SetParent(track.transform, false);
            RectTransform fcrt = fillContainer.GetComponent<RectTransform>();
            fcrt.anchorMin = Vector2.zero;
            fcrt.anchorMax = Vector2.one;
            fcrt.offsetMin = new Vector2(5f, 5f);
            fcrt.offsetMax = new Vector2(-5f, -5f);

            GameObject fill = new GameObject("FillBar", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillContainer.transform, false);
            fillRectOut = fill.GetComponent<RectTransform>();
            fillRectOut.anchorMin = Vector2.zero;
            fillRectOut.anchorMax = Vector2.one;
            fillRectOut.offsetMin = Vector2.zero;
            fillRectOut.offsetMax = Vector2.zero;

            Image fImg = fill.GetComponent<Image>();
            fImg.sprite = barFillSprite;
            fImg.type = Image.Type.Sliced;
            fImg.color = fillColor;
            fImg.raycastTarget = false;

            labelOut = CreateLabel(track.transform, "", 21, Color.white, TextAnchor.MiddleCenter);
            return track;
        }

        private void SetProgressBarRatio(RectTransform fillRt, float ratio)
        {
            if (fillRt == null) return;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
        }

        private void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private GameObject CreateEmptyTabContainer(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0f, 174f);
            rt.offsetMax = new Vector2(0f, -144f);
            return go;
        }

        private GameObject CreateFullScreenTabContainer(Transform parent, string name, Color bgColor, out GameObject contentRoot, string illustration = null)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            StretchFull(panel.GetComponent<RectTransform>());

            // Full-bleed edge-to-edge backdrop covering 100% of the screen (including behind SafeArea, Top Header & Bottom Dock)
            GameObject backdrop = new GameObject("FullScreenBackdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(panel.transform, false);
            RectTransform bgRt = backdrop.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(-180f, -260f);
            bgRt.offsetMax = new Vector2(180f, 260f);

            Image bgImg = backdrop.GetComponent<Image>();
            bgImg.sprite = whitePixelSprite;
            bgImg.type = Image.Type.Simple;
            bgImg.color = bgColor;
            bgImg.raycastTarget = true;

            if (illustration != null)
            {
                // Extend the illustration past the safe area without magnifying its subjects.
                bgRt.offsetMin = new Vector2(-54f, -110f);
                bgRt.offsetMax = new Vector2(54f, 110f);
                var art = new GameObject("TabIllustration", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
                art.transform.SetParent(backdrop.transform, false);
                Image image = art.GetComponent<Image>();
                image.sprite = Resources.Load<Sprite>("Sprites/Environment/" + illustration);
                image.raycastTarget = false;
                var fit = art.GetComponent<AspectRatioFitter>();
                fit.aspectRatio = image.sprite.rect.width / image.sprite.rect.height;
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            }

            // Keep the header and navigation fixed while enlarged content scrolls between them.
            GameObject viewport = new GameObject("TabViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(panel.transform, false);
            RectTransform crt = viewport.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = new Vector2(12f, 190f);
            crt.offsetMax = new Vector2(-12f, -170f);
            viewport.GetComponent<Image>().color = Color.clear;

            float designWidth = name == "ShopGachaTab" ? 1048f : 920f;
            contentRoot = new GameObject("FullScreenContentArea", typeof(RectTransform));
            contentRoot.transform.SetParent(viewport.transform, false);
            RectTransform contentRt = contentRoot.GetComponent<RectTransform>();
            contentRt.anchorMin = contentRt.anchorMax = new Vector2(0.5f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(designWidth, 1500f);
            VerticalLayoutGroup layout = contentRoot.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.padding = new RectOffset(0, 0, 8, 12);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ScrollRect scroll = viewport.GetComponent<ScrollRect>();
            scroll.viewport = crt;
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 55f;
            viewport.AddComponent<ExpandedMenuTabLayout>().Initialize(scroll, designWidth);

            return panel;
        }

        private void SetTabSectionHeight(GameObject section, float height, float flexibleHeight = 0f)
        {
            LayoutElement layout = section.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
            layout.flexibleHeight = flexibleHeight;
        }

        private GameObject CreateGoldNavyCard(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            Image img = go.GetComponent<Image>();
            img.sprite = cardGoldNavySprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.raycastTarget = false;
            return go;
        }

        private GameObject CreateCurrencyPill(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            Image img = go.GetComponent<Image>();
            img.sprite = currencyPillSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.raycastTarget = false;
            return go;
        }

        private GameObject CreateCardPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            Image img = go.GetComponent<Image>();
            img.sprite = roundedPanelSprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            return go;
        }

        private Image CreateIconImage(Transform parent, string name, string resourcePath, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            Image img = go.GetComponent<Image>();
            if (!string.IsNullOrEmpty(resourcePath))
                img.sprite = RuntimeSpriteBank.GetSingle(resourcePath, 100f);
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        private Text CreateOverlayText(Transform parent, string name, int fontSize, Color ink, Vector2 anchor, float y, Vector2 size)
        {
            Text text = CreateLabel(parent, "", fontSize, ink, TextAnchor.MiddleCenter, outlined: true);
            text.gameObject.name = name;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = size;
            return text;
        }

        private Text CreateLabel(Transform parent, string content, int fontSize, Color color, TextAnchor alignment, Vector2 offsetMin = default, Vector2 offsetMax = default, bool outlined = false)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text), outlined ? typeof(Outline) : typeof(Shadow));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            Text t = go.GetComponent<Text>();
            t.font = defaultFont;
            t.text = content;
            float panelScale = parent.GetComponentInParent<ExpandedMenuTabLayout>() != null ? ExpandedMenuTabLayout.TextScale : 1f;
            GameTypography.Apply(t, Mathf.RoundToInt(fontSize * panelScale));
            t.fontStyle = FontStyle.Bold;
            t.supportRichText = true;
            t.color = color;
            t.alignment = alignment;
            t.raycastTarget = false;

            Shadow sh = go.GetComponent<Shadow>();
            sh.effectColor = new Color(0.03f, 0.05f, 0.12f, 0.88f);
            sh.effectDistance = outlined ? new Vector2(2.6f, -2.6f) : new Vector2(1.4f, -1.4f);
            return t;
        }

        private Button Create3DButton(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos, Vector2 size, Sprite buttonSprite, string label, int fontSize, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = go.GetComponent<Image>();
            img.sprite = buttonSprite != null ? buttonSprite : glossyBtnSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            Button btn = go.GetComponent<Button>();
            btn.onClick.AddListener(onClick);
            if (!string.IsNullOrEmpty(label))
                CreateLabel(go.transform, label, fontSize, Color.white, TextAnchor.MiddleCenter, new Vector2(10f, 10f), new Vector2(-10f, -6f));
            return btn;
        }

        private Button Create3DTwoLineButton(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos, Vector2 size, Sprite buttonSprite, string mainTitle, string subTitle, UnityEngine.Events.UnityAction onClick)
        {
            Button btn = Create3DButton(parent, name, anchor, anchoredPos, size, buttonSprite, "", 24, onClick);
            AddButtonCaptions(btn, mainTitle, subTitle, 10f);
            return btn;
        }

        private Button Create3DAdButton(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos, Vector2 size, Sprite buttonSprite, string label, int fontSize, UnityEngine.Events.UnityAction onClick)
        {
            Button btn = Create3DButton(parent, name, anchor, anchoredPos, size, buttonSprite, "", fontSize, onClick);
            float iconSize = Mathf.Min(56f, size.y - 28f);
            CreateIconImage(btn.transform, "AdVideoIcon", "Sprites/UI/icon_ad_video", new Vector2(0f, 0.5f), new Vector2(iconSize * 0.5f + 16f, 2f), new Vector2(iconSize, iconSize));
            CreateLabel(btn.transform, label, fontSize, Color.white, TextAnchor.MiddleCenter, new Vector2(iconSize + 22f, 10f), new Vector2(-14f, -6f));
            return btn;
        }

        private Button Create3DTwoLineAdButton(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos, Vector2 size, Sprite buttonSprite, string mainTitle, string subTitle, UnityEngine.Events.UnityAction onClick)
        {
            Button btn = Create3DButton(parent, name, anchor, anchoredPos, size, buttonSprite, "", 22, onClick);
            const float iconSize = 42f;
            CreateIconImage(btn.transform, "AdVideoIcon", "Sprites/UI/icon_ad_video", new Vector2(0f, 0.72f), new Vector2(32f, 0f), Vector2.one * iconSize);
            AddButtonCaptions(btn, mainTitle, subTitle, 60f);
            return btn;
        }

        private void AddButtonCaptions(Button button, string title, string detail, float titleInset)
        {
            var heading = CreateLabel(button.transform, title, 25, Color.white, TextAnchor.MiddleCenter,
                new Vector2(titleInset, 0f), new Vector2(-10f, -5f));
            heading.rectTransform.anchorMin = new Vector2(0f, 0.50f);
            var subtitle = CreateLabel(button.transform, detail, 23, new Color(1f, 0.96f, 0.78f), TextAnchor.MiddleCenter,
                new Vector2(8f, 6f), new Vector2(-8f, 0f));
            subtitle.rectTransform.anchorMax = new Vector2(1f, 0.50f);
        }
    }
}

