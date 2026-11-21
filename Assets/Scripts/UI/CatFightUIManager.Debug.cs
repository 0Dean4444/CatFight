#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CatFight.Controllers;
using CatFight.Data;
using CatFight.Managers;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace CatFight.UI
{
    public partial class CatFightUIManager
    {
        private GameObject developerPanel;
        private InGameProfiler profiler;
        private Text debugStatus;
        private Text stageSelection;
        private int debugChapter;
        private int debugStage;

        private void OpenDeveloperTools()
        {
            if (developerPanel != null) return;
            var gm = GameManager.Instance;
            bool battle = gm.CurrentAppState == GameAppState.Battle;
            developerPanel = CreateFullScreenDimmer(transform, "DeveloperTools");
            var center = new Vector2(0.5f, 0.5f);
            var card = CreateGoldNavyCard(developerPanel.transform, "DeveloperCard", center, center,
                Vector2.zero, new Vector2(904f, battle ? 692f : 888f));
            CreateLabel(card.transform, "CÔNG CỤ KIỂM THỬ", 38, new Color(1f, 0.87f, 0.4f), TextAnchor.UpperCenter,
                new Vector2(24f, 0f), new Vector2(-24f, -30f));

            Text profilerTitle = null;
            Button profilerButton = DeveloperButton(card.transform, "Profiler", 0, 148, 812, ProfilerCaption(), () =>
            {
                ToggleProfiler();
                profilerTitle.text = ProfilerCaption();
            });
            profilerTitle = profilerButton.GetComponentInChildren<Text>();

            if (battle)
            {
                DeveloperButton(card.transform, "Boss", -208, 256, 396, "Gọi boss chương", () =>
                    RunBattleCommand(gm.DebugSummonBoss)).interactable = !MouseEnemyController.IsAnyBossAlive();
                DeveloperButton(card.transform, "LevelUp", 208, 256, 396, "Lên cấp", () =>
                    RunBattleCommand(() => gm.AddExperience(gm.expToNextLevel - gm.currentExp)));
                DeveloperButton(card.transform, "Heal", -208, 362, 396, "Hồi đầy tường", () =>
                    RunBattleCommand(() => gm.HealWall(gm.wallMaxHp)));
                DeveloperButton(card.transform, "ClearWave", 208, 362, 396, "Qua đợt hiện tại", () =>
                    RunBattleCommand(gm.DebugClearWave));
                DeveloperButton(card.transform, "Victory", -208, 468, 396, "Thử chiến thắng", () =>
                    RunBattleCommand(gm.DebugFinishStage));
                DeveloperButton(card.transform, "Defeat", 208, 468, 396, "Thử thất thủ", () =>
                    RunBattleCommand(() => gm.DamageWall(gm.wallMaxHp)));
            }
            else
            {
                DeveloperButton(card.transform, "Currency", -208, 256, 396, "Thêm tài nguyên", () =>
                {
                    gm.BeginDebugSession();
                    gm.gold += 10000;
                    gm.gems += 5000;
                    gm.summonTickets += 50;
                    DebugFeedback("+10.000 vàng  •  +5.000 KC  •  +50 vé");
                });
                DeveloperButton(card.transform, "Collection", 208, 256, 396, "Mở bộ sưu tập", () =>
                {
                    gm.BeginDebugSession();
                    foreach (var cat in gm.allCatDefinitions)
                    {
                        cat.unlocked = true;
                        foreach (var skin in cat.skins) skin.unlocked = true;
                    }
                    foreach (var weapon in gm.allWeapons) weapon.unlocked = true;
                    foreach (var skin in gm.heroSkins) skin.unlocked = true;
                    DebugFeedback("Đã mở toàn bộ mèo, vũ khí và trang phục.");
                });
                DeveloperButton(card.transform, "Events", 0, 362, 812, "Hoàn thành nhiệm vụ sự kiện", () =>
                {
                    gm.BeginDebugSession();
                    foreach (var mission in gm.eventMissions)
                    {
                        mission.claimed = false;
                        mission.currentProgress = mission.targetProgress;
                    }
                    DebugFeedback("Có thể nhận thưởng nhiệm vụ ở tab Sự kiện.");
                });
                debugChapter = gm.currentChapter;
                debugStage = gm.currentStage;
                stageSelection = CreateLabel(card.transform, "", 32, Color.white, TextAnchor.UpperCenter,
                    new Vector2(20f, 0f), new Vector2(-20f, -436f));
                UpdateDebugStage(0, 0);
                DeveloperButton(card.transform, "PrevChapter", -312, 546, 186, "Chương −", () => UpdateDebugStage(-1, 0));
                DeveloperButton(card.transform, "NextChapter", -104, 546, 186, "Chương +", () => UpdateDebugStage(1, 0));
                DeveloperButton(card.transform, "PrevStage", 104, 546, 186, "Màn −", () => UpdateDebugStage(0, -1));
                DeveloperButton(card.transform, "NextStage", 312, 546, 186, "Màn +", () => UpdateDebugStage(0, 1));
                DeveloperButton(card.transform, "PlayStage", 0, 650, 812, "Chơi thử màn đã chọn", () =>
                {
                    gm.BeginDebugSession();
                    CloseDeveloperTools();
                    settingsModalObj.SetActive(false);
                    gm.SetStageProgression(debugChapter, debugStage);
                    gm.StartBattleFromMainMenu();
                });
            }

            debugStatus = CreateLabel(card.transform, "Lệnh thử chỉ có hiệu lực trong phiên chơi này.", 25,
                new Color(0.66f, 0.92f, 1f), TextAnchor.LowerCenter, new Vector2(22, 130), new Vector2(-22, 0));
            DeveloperButton(card.transform, "Back", 0, battle ? 622 : 818, 400, "Về cài đặt", CloseDeveloperTools);
        }

        private Button DeveloperButton(Transform card, string name, float x, float y, float width, string caption, UnityAction click)
        {
            return Create3DButton(card, name, new Vector2(0.5f, 1f), new Vector2(x, -y),
                new Vector2(width, 84f), btnBlueSprite, caption, 28, click);
        }

        private void RunBattleCommand(UnityAction command)
        {
            var gm = GameManager.Instance;
            gm.BeginDebugSession();
            CloseDeveloperTools();
            settingsModalObj.SetActive(false);
            gm.SetPaused(false);
            command();
            RefreshAllUI();
        }

        private void CloseDeveloperTools()
        {
            developerPanel.SetActive(false);
            Destroy(developerPanel);
            developerPanel = null;
        }

        private void DebugFeedback(string message)
        {
            debugStatus.text = message;
            RefreshAllUI();
        }

        private void UpdateDebugStage(int chapterDelta, int stageDelta)
        {
            debugChapter = Mathf.Clamp(debugChapter + chapterDelta, 1, GameManager.TotalChapters);
            debugStage = Mathf.Clamp(debugStage + stageDelta, 1, GameManager.StagesPerChapter);
            stageSelection.text = $"Màn {debugChapter}-{debugStage}  •  {GameManager.Instance.GetChapterName(debugChapter)}";
        }

        private string ProfilerCaption() => profiler != null && profiler.isActiveAndEnabled ? "Profiler đang bật  •  Tắt" : "Bật Profiler";

        private void ToggleProfiler()
        {
            if (profiler != null)
            {
                profiler.gameObject.SetActive(!profiler.gameObject.activeSelf);
                return;
            }

            var hud = CreateCurrencyPill(safeAreaRoot, "PerformanceHUD", new Vector2(0.5f, 1f),
                new Vector2(0f, -54f), new Vector2(864f, 104f));
            Canvas overlay = hud.AddComponent<Canvas>();
            overlay.overrideSorting = true;
            overlay.sortingOrder = GetComponent<Canvas>().sortingOrder + 1;
            var label = CreateLabel(hud.transform, "Đang đo hiệu năng…", 30, Color.white, TextAnchor.MiddleCenter);
            profiler = hud.AddComponent<InGameProfiler>();
            profiler.Label = label;
        }
    }
}
#endif
