using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatFight.Data
{
    public enum GameAppState
    {
        MainMenu = 0,
        Battle = 1
    }

    public enum MainMenuTab
    {
        Shop = 0,       // Cửa Hàng Gacha Roll Mèo, Vũ Khí & Skin
        Pet = 1,        // Đội Hình Mèo, Nâng Cấp & Đổi Skin
        Battle = 2,     // Trang Chủ Sảnh Chính & Nút Bắt Đầu Chiến Đấu
        Character = 3,  // Nhân Vật Chính: Nâng Cấp Chỉ Số & Vũ Khí Gatling
        Event = 4       // Sự Kiện Săn Chuột & Nhiệm Vụ Nhận Quà
    }

    public enum ShopGachaCategory
    {
        CatSummon = 0,      // Quay Hộp Quà Mèo
        WeaponArsenal = 1,  // Quay Rương Vũ Khí Gatling
        SkinWardrobe = 2    // Quay Tủ Đồ Trang Phục Skin
    }

    [Serializable]
    public class HeroWeaponDefinition
    {
        public string id;
        public string displayName;
        public string rarity; // "SR", "SSR", "UR"
        public string description;
        public Color tintColor;
        public float damageMultiplier = 1f;
        public float fireRateMultiplier = 1f;
        public int bonusBarrels = 0;
        public int level = 1;
        public bool unlocked = false;
    }

    public enum CatSkillType
    {
        GiantPawStomp = 0,     // 1. Mèo Mập: Gọi bàn chân mèo khổng lồ dậm xuống gây sát thương AoE & in hình dấu chân làm chậm
        ShelfKnockdown = 1,    // 2. Mèo Nghịch Ngợm: Hất đồ đạc trên kệ rơi xuống gây sát thương nổ & đẩy lùi
        GiantYarnBall = 2,     // 3. Mèo Len Đỏ: Lăn cuộn len khổng lồ nảy qua lại màn hình cán xuyên chuột
        NinjaClawFlurry = 3,   // 4. Mèo Ninja: Lao vút chém liên hoàn vết cào chí mạng vào mục tiêu nguy hiểm
        CatnipTornado = 4,     // 5. Mèo Phù Thủy: Lốc xoáy cỏ mèo gom chuột lại một chỗ và rút máu liên tục
        RedDotLaser = 5,       // 6. Mèo Bác Học: Tia laser đỏ quét ngang đốt cháy hàng ngũ chuột
        RoyalFishBarrage = 6   // 7. Mèo Hoàng Thượng: Mưa xương cá vàng oanh tạc & hồi phục giáp tường thành
    }

    public enum MouseEnemyType
    {
        Normal = 0,    // Chuột Cống Thường
        Runner = 1,    // Chuột Đạo Chích (Chạy cực nhanh)
        Tank = 2,      // Chuột Nồi Giáp (Máu trâu, đội nồi cầm khiên)
        Bomber = 3,    // Chuột Cảm Tử (Cầm bom lao vào nổ mạnh)
        Boss = 4       // Vua Chuột Cống Khổng Lồ
    }

    [Serializable]
    public class CatSkinData
    {
        public string skinName;
        public Color tintColor;
        public float damageBonusPct;
        public bool unlocked;
    }

    [Serializable]
    public class CatCompanionDefinition
    {
        public string id;
        public string displayName;
        public string title;
        public string rarity; // "SR", "SSR", "UR"
        public string description;
        public CatSkillType skillType;
        public string sheetResourcePath;
        public string iconResourcePath;
        public Color themeColor;
        public float baseCooldown;
        public float baseDamage;
        public float attackRange = 3.8f;
        public float effectRadius;
        public float effectDuration;

        // Meta-progression state (persisted in session)
        public int permanentLevel = 1;
        public const int MaxBattleStageLevel = 5;
        public int battleStageLevel = 1;
        public bool IsAwakened => battleStageLevel >= MaxBattleStageLevel;

        public string AwakeningDescription => skillType switch
        {
            CatSkillType.GiantPawStomp => "Bàn chân giáng thêm cú dậm thứ hai tại điểm trúng.",
            CatSkillType.ShelfKnockdown => "Cú ném cuối hất thêm một món đồ từ kệ đối diện.",
            CatSkillType.GiantYarnBall => "Mỗi cuộn len tách thành hai cuộn nhỏ khi nảy lần đầu.",
            CatSkillType.NinjaClawFlurry => "Nhát chém cuối tạo vết cào chữ X và làm choáng chuột.",
            CatSkillType.CatnipTornado => "Lốc tan thành một vòng xung kích đánh bật chuột ra ngoài.",
            CatSkillType.RedDotLaser => "Tia laser đổi màu và quét ngược thêm một lượt.",
            CatSkillType.RoyalFishBarrage => "Kết thúc loạt bắn bằng năm xương cá xuyên mục tiêu theo hình quạt.",
            _ => string.Empty
        };
        public int shards = 0;
        public bool unlocked = true;
        public int selectedSkinIndex = 0;
        public List<CatSkinData> skins = new List<CatSkinData>();

        public Color GetActiveSkinTint()
        {
            if (skins != null && selectedSkinIndex >= 0 && selectedSkinIndex < skins.Count)
                return skins[selectedSkinIndex].tintColor;
            return Color.white;
        }

        public string GetActiveSkinName()
        {
            if (skins != null && selectedSkinIndex >= 0 && selectedSkinIndex < skins.Count)
                return skins[selectedSkinIndex].skinName;
            return "Mặc Định";
        }

        public float GetSkinDamageMultiplier()
        {
            if (skins != null && selectedSkinIndex >= 0 && selectedSkinIndex < skins.Count)
                return 1f + skins[selectedSkinIndex].damageBonusPct;
            return 1f;
        }
    }

    [Serializable]
    public class MouseEnemyDefinition
    {
        public MouseEnemyType type;
        public string displayName;
        public string sheetResourcePath;
        public float maxHp;
        public float moveSpeed;
        public float attackDamage;
        public int expReward;
        public int goldReward;
        public float scale;
    }

    [Serializable]
    public class RogueliteUpgradeOption
    {
        public string title;
        public string subtitle;
        public string description;
        public Color cardColor;
        public Sprite icon;
        public Action onSelect;
    }

    [Serializable]
    public class EventMissionItem
    {
        public string id;
        public string title;
        public string description;
        public int targetProgress;
        public int currentProgress;
        public int rewardGold;
        public int rewardGems;
        public bool claimed;
    }

    [Serializable]
    public class GachaRollResultItem
    {
        public string rarity;
        public string itemName;
        public string subText;
        public string logLine;
        public Sprite iconSprite;
        public Color iconTint = Color.white;
        public Color rarityColor = Color.white;
        public bool isNewUnlock;
    }
}
