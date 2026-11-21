using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using CatFight.UI;
using CatFight.Visuals;

namespace CatFight.Managers
{
    [Serializable]
    public class AdMobConfigData
    {
        public string appName = "CatFight - Đại Chiến Mèo Chuột";
        public string packageName = "com.catfight.defense.vn";
        public bool isListedOnGooglePlay = false;
        public bool useTestAdsWhileUnpublished = true;
        public string androidAppId = "ca-app-pub-3940256099942544~3347511713";
        public string androidRewardedUnitId = "ca-app-pub-3940256099942544/5224354917";
        public string androidInterstitialUnitId = "ca-app-pub-3940256099942544/1033173712";
        public string androidBannerUnitId = "ca-app-pub-3940256099942544/6300978111";
        public string iosAppId = "ca-app-pub-3940256099942544~1458002511";
        public string iosRewardedUnitId = "ca-app-pub-3940256099942544/1712485313";
    }

    /// <summary>
    /// Google AdMob Integration Manager for CatFight.
    /// - Loads pre-Google-Play AdMob configuration from Assets/Resources/AdMobConfig.json.
    /// - Uses official Google Mobile Ads test Unit IDs when unpublished / testing, or custom Ad Unit IDs from AdMob Console.
    /// - Automatically links to GoogleMobileAds.Api if the Google Mobile Ads Unity Plugin is installed.
    /// </summary>
    public class GoogleAdMobManager : MonoBehaviour
    {
        public static GoogleAdMobManager Instance { get; private set; }

        // Official Google AdMob Test Ad Unit IDs
        public const string DefaultTestAndroidAppId = "ca-app-pub-3940256099942544~3347511713";
        public const string AndroidRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
        public const string AndroidInterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
        public const string AndroidBannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";

        public AdMobConfigData Config { get; private set; } = new AdMobConfigData();

        public string ActiveRewardedAdUnitId
        {
            get
            {
                if (Config == null) return AndroidRewardedAdUnitId;
                if (!Config.isListedOnGooglePlay && Config.useTestAdsWhileUnpublished)
                    return AndroidRewardedAdUnitId;
                return string.IsNullOrEmpty(Config.androidRewardedUnitId) ? AndroidRewardedAdUnitId : Config.androidRewardedUnitId;
            }
        }

        public bool IsAdWatching { get; private set; }
        public int TotalRewardedAdsWatched { get; private set; }

        // Interactive AdMob Overlay UI
        private GameObject adOverlayRoot;
        private Text adPlacementTitleText;
        private Text adUnitIdSubtitleText;
        private Text adRewardPreviewText;
        private Text adCountdownBadgeText;
        private RectTransform adProgressFillRt;
        private Button closeClaimButton;
        private Text closeClaimBtnText;

        private Action pendingRewardCallback;
        private bool wasPausedBeforeAd;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LoadAdMobConfigFromResources();
            InitializeAdMobSdk();
        }

        public void LoadAdMobConfigFromResources()
        {
            TextAsset jsonAsset = Resources.Load<TextAsset>("AdMobConfig");
            if (jsonAsset != null && !string.IsNullOrEmpty(jsonAsset.text))
            {
                try
                {
                    Config = JsonUtility.FromJson<AdMobConfigData>(jsonAsset.text);
                }
                catch
                {
                    Config = new AdMobConfigData();
                }
            }
            if (Config == null)
                Config = new AdMobConfigData();
        }

        private void InitializeAdMobSdk()
        {
#if GOOGLE_MOBILE_ADS
            GoogleMobileAds.Api.MobileAds.Initialize(initStatus =>
            {
                Debug.Log("[GoogleAdMobManager] Google Mobile Ads SDK Initialized.");
            });
#else
            string modeNote = (!Config.isListedOnGooglePlay) ? "Unpublished Pre-GooglePlay Setup" : "Google Play Published";
            Debug.Log($"[GoogleAdMobManager] Initialized ({modeNote}) | AppId: {Config.androidAppId} | Active Rewarded Unit: {ActiveRewardedAdUnitId}");
#endif
        }

        public void ShowRewardedAd(string placementTitle, string rewardSummary, Action onRewardEarned)
        {
            if (IsAdWatching) return;

            pendingRewardCallback = onRewardEarned;
            if (CatFightAudioManager.Instance != null)
                CatFightAudioManager.Instance.PlayUiClick();

            EnsureAdOverlayBuilt();
            StartCoroutine(SimulateAdMobRewardedVideoRoutine(placementTitle, rewardSummary));
        }

        private IEnumerator SimulateAdMobRewardedVideoRoutine(string placementTitle, string rewardSummary)
        {
            IsAdWatching = true;
            float prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            adOverlayRoot.SetActive(true);
            adOverlayRoot.transform.SetAsLastSibling();

            adPlacementTitleText.text = $"GOOGLE ADMOB • {placementTitle.ToUpper()}";
            if (adUnitIdSubtitleText != null)
            {
                string statusTag = (!Config.isListedOnGooglePlay) ? "Chưa lên Google Play • Test Mode" : "Google Play Live";
                adUnitIdSubtitleText.text = $"AdUnit ID: {ActiveRewardedAdUnitId} ({statusTag})";
            }
            adRewardPreviewText.text = $"Phần thưởng sau khi xem xong:\n<color=#FFE066>{rewardSummary}</color>";
            closeClaimButton.interactable = false;
            closeClaimButton.GetComponent<Image>().color = new Color(0.45f, 0.48f, 0.58f, 1f);

            float duration = 3.0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float remaining = Mathf.Max(0f, duration - elapsed);
                float ratio = Mathf.Clamp01(elapsed / duration);

                if (adProgressFillRt != null)
                {
                    adProgressFillRt.anchorMin = Vector2.zero;
                    adProgressFillRt.anchorMax = new Vector2(ratio, 1f);
                    adProgressFillRt.offsetMin = Vector2.zero;
                    adProgressFillRt.offsetMax = Vector2.zero;
                }

                adCountdownBadgeText.text = $"Quảng cáo Google AdMob • Còn {Mathf.CeilToInt(remaining)} giây...";
                closeClaimBtnText.text = $"ĐỢI {Mathf.CeilToInt(remaining)}s...";
                yield return null;
            }

            // Ad finished! Unlock reward claim
            adCountdownBadgeText.text = "✓ Đã hoàn thành xem Quảng Cáo Google AdMob!";
            closeClaimButton.interactable = true;
            closeClaimButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
            closeClaimBtnText.text = "NHẬN THƯỞNG & ĐÓNG (✕)";

            if (CatFightAudioManager.Instance != null)
                CatFightAudioManager.Instance.PlayGacha();

            // Wait until user clicks Claim or auto-claim after 0.8s for smooth UX
            float autoTimer = 0f;
            while (adOverlayRoot.activeSelf && autoTimer < 4.0f)
            {
                autoTimer += Time.unscaledDeltaTime;
                yield return null;
            }

            if (adOverlayRoot.activeSelf)
                CompleteAndCloseAd(prevTimeScale);
        }

        private void CompleteAndCloseAd(float restoreTimeScale)
        {
            if (!IsAdWatching) return;
            IsAdWatching = false;
            TotalRewardedAdsWatched++;

            if (adOverlayRoot != null)
                adOverlayRoot.SetActive(false);

            if (GameManager.Instance != null && GameManager.Instance.CurrentAppState == Data.GameAppState.Battle && GameManager.Instance.IsPlaying)
                Time.timeScale = GameManager.Instance.GameSpeedMultiplier;
            else
                Time.timeScale = restoreTimeScale > 0f ? restoreTimeScale : 1f;

            var cb = pendingRewardCallback;
            pendingRewardCallback = null;
            cb?.Invoke();
        }

        private void EnsureAdOverlayBuilt()
        {
            if (adOverlayRoot != null) return;
            if (CatFightUIManager.Instance == null) return;

            Transform canvasRoot = CatFightUIManager.Instance.transform;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 24);

            Sprite whitePx = RuntimeSpriteBank.GetSingle("Sprites/UI/ui_white_pixel", 32f);
            Sprite cardNavy = CreateSlicedSprite("Sprites/UI/ui_card_gold_navy", 34);
            Sprite pillSprite = CreateSlicedSprite("Sprites/UI/ui_currency_pill", 36);
            Sprite panelSprite = CreateSlicedSprite("Sprites/UI/ui_rounded_panel", 34);
            Sprite btnGreen = CreateSlicedSprite("Sprites/UI/ui_btn_green", 30);
            Sprite barTrack = CreateSlicedSprite("Sprites/UI/ui_bar_track", 20);
            Sprite barFill = CreateSlicedSprite("Sprites/UI/ui_bar_fill", 16);

            // Full-bleed dark backdrop
            adOverlayRoot = new GameObject("GoogleAdMob_RewardedVideoModal", typeof(RectTransform), typeof(Image));
            adOverlayRoot.transform.SetParent(canvasRoot, false);
            RectTransform rootRt = adOverlayRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = new Vector2(-200f, -200f);
            rootRt.offsetMax = new Vector2(200f, 200f);
            Image rootImg = adOverlayRoot.GetComponent<Image>();
            rootImg.sprite = whitePx;
            rootImg.color = new Color(0.02f, 0.03f, 0.07f, 0.94f);

            // Center Video Player Card (Properly 9-Sliced Gold-Navy Frame with generous inner margins)
            GameObject card = new GameObject("AdPlayerCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(adOverlayRoot.transform, false);
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta = new Vector2(880f, 780f);
            Image cardImg = card.GetComponent<Image>();
            cardImg.sprite = cardNavy;
            cardImg.type = Image.Type.Sliced;

            // Top Header Pill inside AdPlayerCard (44px horizontal margin, 36px top margin from gold border)
            GameObject headerPill = new GameObject("AdHeaderPill", typeof(RectTransform), typeof(Image));
            headerPill.transform.SetParent(card.transform, false);
            RectTransform hpRt = headerPill.GetComponent<RectTransform>();
            hpRt.anchorMin = new Vector2(0.5f, 1f);
            hpRt.anchorMax = new Vector2(0.5f, 1f);
            hpRt.anchoredPosition = new Vector2(0f, -82f);
            hpRt.sizeDelta = new Vector2(792f, 92f);
            Image hpImg = headerPill.GetComponent<Image>();
            hpImg.sprite = pillSprite;
            hpImg.type = Image.Type.Sliced;

            adPlacementTitleText = CreateAdLabel(headerPill.transform, font, "GOOGLE ADMOB REWARDED VIDEO", 24, new Color(1f, 0.88f, 0.35f), TextAnchor.UpperCenter, new Vector2(18f, 38f), new Vector2(-18f, -10f));
            adUnitIdSubtitleText = CreateAdLabel(headerPill.transform, font, $"AdUnit ID: {ActiveRewardedAdUnitId} (Chưa lên Google Play • Test Mode)", 16, new Color(0.68f, 0.86f, 1f), TextAnchor.LowerCenter, new Vector2(18f, 10f), new Vector2(-18f, -46f));

            // Center Rounded Video Screen Box (44px horizontal margin from gold border)
            GameObject videoScreen = new GameObject("VideoScreen", typeof(RectTransform), typeof(Image));
            videoScreen.transform.SetParent(card.transform, false);
            RectTransform vsRt = videoScreen.GetComponent<RectTransform>();
            vsRt.anchorMin = new Vector2(0.5f, 0.5f);
            vsRt.anchorMax = new Vector2(0.5f, 0.5f);
            vsRt.anchoredPosition = new Vector2(0f, 24f);
            vsRt.sizeDelta = new Vector2(792f, 356f);
            Image vsImg = videoScreen.GetComponent<Image>();
            vsImg.sprite = panelSprite;
            vsImg.type = Image.Type.Sliced;
            vsImg.color = new Color(0.09f, 0.12f, 0.22f, 1f);

            GameObject adIcon = new GameObject("AdIcon", typeof(RectTransform), typeof(Image));
            adIcon.transform.SetParent(videoScreen.transform, false);
            RectTransform aiRt = adIcon.GetComponent<RectTransform>();
            aiRt.anchorMin = new Vector2(0.5f, 0.5f);
            aiRt.anchorMax = new Vector2(0.5f, 0.5f);
            aiRt.anchoredPosition = new Vector2(0f, 44f);
            aiRt.sizeDelta = new Vector2(132f, 132f);
            Image aiImg = adIcon.GetComponent<Image>();
            aiImg.sprite = RuntimeSpriteBank.GetSingle("Sprites/UI/icon_ad_video", 100f);
            aiImg.preserveAspect = true;

            adRewardPreviewText = CreateAdLabel(videoScreen.transform, font, "Phần thưởng...", 24, Color.white, TextAnchor.MiddleCenter, new Vector2(28f, 22f), new Vector2(-28f, -186f));

            // 9-Sliced Progress Bar with inner mask padding
            GameObject track = new GameObject("AdProgressBar", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(card.transform, false);
            RectTransform trt = track.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.5f, 0f);
            trt.anchorMax = new Vector2(0.5f, 0f);
            trt.anchoredPosition = new Vector2(0f, 172f);
            trt.sizeDelta = new Vector2(792f, 44f);
            Image tImg = track.GetComponent<Image>();
            tImg.sprite = barTrack;
            tImg.type = Image.Type.Sliced;

            GameObject fillMask = new GameObject("FillMask", typeof(RectTransform));
            fillMask.transform.SetParent(track.transform, false);
            RectTransform fmRt = fillMask.GetComponent<RectTransform>();
            fmRt.anchorMin = Vector2.zero;
            fmRt.anchorMax = Vector2.one;
            fmRt.offsetMin = new Vector2(5f, 5f);
            fmRt.offsetMax = new Vector2(-5f, -5f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillMask.transform, false);
            adProgressFillRt = fill.GetComponent<RectTransform>();
            adProgressFillRt.anchorMin = Vector2.zero;
            adProgressFillRt.anchorMax = Vector2.one;
            adProgressFillRt.offsetMin = Vector2.zero;
            adProgressFillRt.offsetMax = Vector2.zero;
            Image fImg = fill.GetComponent<Image>();
            fImg.sprite = barFill;
            fImg.type = Image.Type.Sliced;
            fImg.color = new Color(0.28f, 0.88f, 0.48f, 1f);

            adCountdownBadgeText = CreateAdLabel(track.transform, font, "Đang phát...", 20, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);

            // 9-Sliced Claim & Close Button
            GameObject btnGo = new GameObject("ClaimCloseAdBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(card.transform, false);
            RectTransform brt = btnGo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0f);
            brt.anchorMax = new Vector2(0.5f, 0f);
            brt.anchoredPosition = new Vector2(0f, 82f);
            brt.sizeDelta = new Vector2(520f, 92f);
            Image bImg = btnGo.GetComponent<Image>();
            bImg.sprite = btnGreen;
            bImg.type = Image.Type.Sliced;

            closeClaimButton = btnGo.GetComponent<Button>();
            closeClaimButton.onClick.AddListener(() => CompleteAndCloseAd(1f));
            closeClaimBtnText = CreateAdLabel(btnGo.transform, font, "NHẬN THƯỞNG", 24, Color.white, TextAnchor.MiddleCenter, new Vector2(16f, 12f), new Vector2(-16f, -8f));

            adOverlayRoot.SetActive(false);
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

        private Text CreateAdLabel(Transform parent, Font font, string content, int size, Color col, TextAnchor align, Vector2 minOff, Vector2 maxOff)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = minOff;
            rt.offsetMax = maxOff;

            Text t = go.GetComponent<Text>();
            t.font = font;
            t.text = content;
            GameTypography.Apply(t, size);
            t.fontStyle = FontStyle.Bold;
            t.supportRichText = true;
            t.color = col;
            t.alignment = align;

            Outline ol = go.GetComponent<Outline>();
            ol.effectColor = new Color(0.03f, 0.05f, 0.12f, 0.82f);
            ol.effectDistance = new Vector2(1.2f, -1.2f);
            return t;
        }
    }
}
