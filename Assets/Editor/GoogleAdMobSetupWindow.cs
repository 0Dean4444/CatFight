using System.IO;
using UnityEditor;
using UnityEngine;
using CatFight.Managers;

namespace CatFight.EditorTools
{
    public class GoogleAdMobSetupWindow : EditorWindow
    {
        private const string ConfigPath = "Assets/Resources/AdMobConfig.json";
        private AdMobConfigData config = new AdMobConfigData();
        private Vector2 scrollPos;

        [MenuItem("CatFight/Thiết Lập Google AdMob (Chưa Cần Google Play)", false, 10)]
        public static void ShowWindow()
        {
            var win = GetWindow<GoogleAdMobSetupWindow>("Cấu Hình Google AdMob");
            win.minSize = new Vector2(540f, 620f);
            win.LoadConfig();
            win.Show();
        }

        private void OnEnable()
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigPath);
                    config = JsonUtility.FromJson<AdMobConfigData>(json);
                }
                catch
                {
                    config = new AdMobConfigData();
                }
            }
            if (config == null)
                config = new AdMobConfigData();
        }

        private void SaveConfig()
        {
            string dir = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string json = JsonUtility.ToJson(config, true);
            File.WriteAllText(ConfigPath, json);
            AssetDatabase.Refresh();

            if (GoogleAdMobManager.Instance != null)
                GoogleAdMobManager.Instance.LoadAdMobConfigFromResources();

            EditorUtility.DisplayDialog(
                "Đã Lưu Cấu Hình Google AdMob",
                "Đã lưu thành công mã kết nối Google AdMob vào Assets/Resources/AdMobConfig.json!\n\nGame đã sẵn sàng chạy quảng cáo ở chế độ Chưa Up Google Play.",
                "OK");
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                wordWrap = true
            };

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("THIẾT LẬP GOOGLE ADMOB (DÀNH CHO GAME CHƯA UP GOOGLE PLAY)", headerStyle);
            EditorGUILayout.Space(4f);

            EditorGUILayout.HelpBox(
                "Bạn KHÔNG cần có tài khoản Google Play Console vẫn có thể tạo App ID & Ad Unit ID trên Google AdMob:\n\n" +
                "• Bước 1: Nhấn nút '1. Mở Trang Tạo App Google AdMob' bên dưới.\n" +
                "• Bước 2: Chọn Nền tảng: 'Android'.\n" +
                "• Bước 3: Ở câu hỏi 'Ứng dụng này đã được liệt kê trên cửa hàng ứng dụng được hỗ trợ chưa?', hãy chọn 'KHÔNG (No)' rồi bấm Tiếp tục.\n" +
                "• Bước 4: Đặt tên ứng dụng là 'CatFight - Đại Chiến Mèo Chuột' -> Bấm 'Thêm ứng dụng'.\n" +
                "• Bước 5: Vào mục 'Đơn vị quảng cáo (Ad units)' -> Bấm 'Thêm đơn vị quảng cáo' -> Chọn 'Có tặng thưởng (Rewarded)' -> Copy mã App ID và Ad Unit ID dán vào ô bên dưới!",
                MessageType.Info);

            EditorGUILayout.Space(6f);
            GUI.backgroundColor = new Color(0.28f, 0.72f, 1f);
            if (GUILayout.Button("1. MỞ TRANG TẠO APP GOOGLE ADMOB TRÊN TRÌNH DUYỆT", GUILayout.Height(36f)))
            {
                Application.OpenURL("https://apps.admob.com/v2/apps/create");
            }

            GUI.backgroundColor = new Color(0.85f, 0.92f, 1f);
            if (GUILayout.Button("Tải Google Mobile Ads Unity Plugin (.unitypackage chính thức)", GUILayout.Height(28f)))
            {
                Application.OpenURL("https://github.com/googleads/googleads-mobile-unity/releases/latest");
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("THÔNG TIN ỨNG DỤNG & TRẠNG THÁI CỬA HÀNG", EditorStyles.boldLabel);
            config.appName = EditorGUILayout.TextField("Tên Game trên AdMob", config.appName);
            config.packageName = EditorGUILayout.TextField("Package Name (Bundle ID)", config.packageName);
            config.isListedOnGooglePlay = EditorGUILayout.Toggle("Đã Up Lên Google Play?", config.isListedOnGooglePlay);
            config.useTestAdsWhileUnpublished = EditorGUILayout.Toggle("Dùng Test ID Khi Chưa Up", config.useTestAdsWhileUnpublished);

            if (!config.isListedOnGooglePlay)
            {
                EditorGUILayout.HelpBox(
                    "Lưu ý quan trọng từ Google: Khi game CHƯA được duyệt trên Google Play, bạn nên bật 'Dùng Test ID Khi Chưa Up' trong quá trình test để tránh bị Google khóa tài khoản AdMob do tự bấm quảng cáo thật.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("MÃ KẾT NỐI ANDROID (DÁN TỪ TRANG WEB ADMOB VÀO ĐÂY)", EditorStyles.boldLabel);
            config.androidAppId = EditorGUILayout.TextField("Android App ID (~)", config.androidAppId);
            config.androidRewardedUnitId = EditorGUILayout.TextField("Rewarded Ad Unit ID (/)", config.androidRewardedUnitId);
            config.androidInterstitialUnitId = EditorGUILayout.TextField("Interstitial Unit ID (/)", config.androidInterstitialUnitId);
            config.androidBannerUnitId = EditorGUILayout.TextField("Banner Unit ID (/)", config.androidBannerUnitId);

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Khôi Phục Mã Test Chuẩn Của Google", GUILayout.Height(26f)))
            {
                config.androidAppId = GoogleAdMobManager.DefaultTestAndroidAppId;
                config.androidRewardedUnitId = GoogleAdMobManager.AndroidRewardedAdUnitId;
                config.androidInterstitialUnitId = GoogleAdMobManager.AndroidInterstitialAdUnitId;
                config.androidBannerUnitId = GoogleAdMobManager.AndroidBannerAdUnitId;
            }

            EditorGUILayout.Space(14f);
            GUI.backgroundColor = new Color(0.28f, 0.88f, 0.48f);
            if (GUILayout.Button("2. LƯU CẤU HÌNH KẾT NỐI ADMOB VÀO DỰ ÁN", GUILayout.Height(42f)))
            {
                SaveConfig();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndScrollView();
        }
    }
}
