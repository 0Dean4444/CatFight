using System.Collections.Generic;
using UnityEngine;

namespace CatFight.Data
{
    public static class CatFightDatabase
    {
        private static List<CatSkinData> CreateDefaultSkins(string baseSkin, string skin2, Color col2, string skin3, Color col3)
        {
            return new List<CatSkinData>
            {
                new CatSkinData { skinName = baseSkin, tintColor = Color.white, damageBonusPct = 0f, unlocked = true },
                new CatSkinData { skinName = skin2, tintColor = col2, damageBonusPct = 0.15f, unlocked = false },
                new CatSkinData { skinName = skin3, tintColor = col3, damageBonusPct = 0.35f, unlocked = false }
            };
        }

        public static List<CatCompanionDefinition> CreateDefaultCats()
        {
            return new List<CatCompanionDefinition>
            {
                new CatCompanionDefinition
                {
                    id = "cat_stomp",
                    displayName = "Mèo Mập",
                    title = "Bàn Chân Ngàn Cân",
                    rarity = "SSR",
                    description = "Gọi bàn chân mèo khổng lồ dậm xuống đám chuột, gây nổ AoE, làm choáng và IN HÌNH DẤU CHÂN làm chậm 50%.",
                    skillType = CatSkillType.GiantPawStomp,
                    sheetResourcePath = "Sprites/Cats/cat_stomp_sheet",
                    iconResourcePath = "Sprites/UI/icon_cat_stomp",
                    themeColor = new Color(1.0f, 0.62f, 0.22f),
                    baseCooldown = 5.6f,
                    baseDamage = 68f,
                    attackRange = 3.9f,
                    effectRadius = 2.15f,
                    effectDuration = 6.0f,
                    permanentLevel = 1,
                    battleStageLevel = 1,
                    unlocked = true,
                    skins = CreateDefaultSkins(
                        "Mèo Mướp Cam",
                        "Mèo Hoàng Kim (+15% Công)", new Color(1f, 0.92f, 0.45f),
                        "Mèo Hỏa Long (+35% Công)", new Color(1f, 0.55f, 0.52f))
                },
                new CatCompanionDefinition
                {
                    id = "cat_thrower",
                    displayName = "Mèo Nghịch Ngợm",
                    title = "Thích Thì Gạt!",
                    rarity = "SR",
                    description = "Hất tung chậu cây, cốc sứ, đồng hồ từ trên kệ rơi thẳng xuống đầu đàn chuột, gây sát thương mảnh vỡ và đẩy lùi.",
                    skillType = CatSkillType.ShelfKnockdown,
                    sheetResourcePath = "Sprites/Cats/cat_thrower_sheet",
                    iconResourcePath = "Sprites/UI/icon_cat_thrower",
                    themeColor = new Color(0.45f, 0.72f, 1.0f),
                    baseCooldown = 4.8f,
                    baseDamage = 50f,
                    attackRange = 4.2f,
                    effectRadius = 1.45f,
                    effectDuration = 1.2f,
                    permanentLevel = 1,
                    battleStageLevel = 1,
                    unlocked = true,
                    skins = CreateDefaultSkins(
                        "Mèo Tuxedo Lịch Lãm",
                        "Mèo Băng Tuyết (+15% Công)", new Color(0.68f, 0.92f, 1f),
                        "Mèo Ác Ma Đêm (+35% Công)", new Color(0.82f, 0.58f, 1f))
                },
                new CatCompanionDefinition
                {
                    id = "cat_yarn",
                    displayName = "Mèo Len Đỏ",
                    title = "Cuộn Len Bão Tố",
                    rarity = "SR",
                    description = "Lăn cuộn len khổng lồ nảy qua lại giữa các mép tường, cán xuyên qua hàng ngũ chuột và làm choáng nhẹ.",
                    skillType = CatSkillType.GiantYarnBall,
                    sheetResourcePath = "Sprites/Cats/cat_yarn_sheet",
                    iconResourcePath = "Sprites/UI/icon_cat_yarn",
                    themeColor = new Color(0.98f, 0.38f, 0.48f),
                    baseCooldown = 6.4f,
                    baseDamage = 40f,
                    attackRange = 4.3f,
                    effectRadius = 0.88f,
                    effectDuration = 4.6f,
                    permanentLevel = 1,
                    battleStageLevel = 1,
                    unlocked = true,
                    skins = CreateDefaultSkins(
                        "Mèo Họa Sĩ Đỏ",
                        "Mèo Kẹo Ngọt (+15% Công)", new Color(1f, 0.72f, 0.92f),
                        "Mèo Nắng Hạ (+35% Công)", new Color(1f, 0.95f, 0.55f))
                },
                new CatCompanionDefinition
                {
                    id = "cat_ninja",
                    displayName = "Mèo Ninja",
                    title = "Tuyệt Kỹ Cào Xé",
                    rarity = "SSR",
                    description = "Tốc biến liên hoàn qua các mục tiêu nguy hiểm nhất, tung những nhát cào ánh sáng gây sát thương chí mạng.",
                    skillType = CatSkillType.NinjaClawFlurry,
                    sheetResourcePath = "Sprites/Cats/cat_ninja_sheet",
                    iconResourcePath = "Sprites/UI/icon_cat_ninja",
                    themeColor = new Color(0.42f, 0.92f, 1.0f),
                    baseCooldown = 5.2f,
                    baseDamage = 62f,
                    attackRange = 4.0f,
                    effectRadius = 1.35f,
                    effectDuration = 0.6f,
                    permanentLevel = 1,
                    battleStageLevel = 1,
                    unlocked = false, // Unlockable via Shop Gacha or Pet Shards!
                    skins = CreateDefaultSkins(
                        "Ninja Ảnh Vũ",
                        "Ninja Lôi Điện (+15% Công)", new Color(0.58f, 0.95f, 1f),
                        "Ninja Huyết Nguyệt (+35% Công)", new Color(1f, 0.48f, 0.58f))
                },
                new CatCompanionDefinition
                {
                    id = "cat_mage",
                    displayName = "Mèo Phù Thủy",
                    title = "Lốc Xoáy Cỏ Mèo",
                    rarity = "SSR",
                    description = "Triệu hồi cơn lốc bạc hà mèo hút gom toàn bộ chuột xung quanh về tâm bão và rút máu liên tục.",
                    skillType = CatSkillType.CatnipTornado,
                    sheetResourcePath = "Sprites/Cats/cat_mage_sheet",
                    iconResourcePath = "Sprites/UI/icon_cat_mage",
                    themeColor = new Color(0.45f, 0.95f, 0.58f),
                    baseCooldown = 7.0f,
                    baseDamage = 26f,
                    attackRange = 4.1f,
                    effectRadius = 2.35f,
                    effectDuration = 3.8f,
                    permanentLevel = 1,
                    battleStageLevel = 1,
                    unlocked = false,
                    skins = CreateDefaultSkins(
                        "Phù Thủy Rừng Xanh",
                        "Phù Thủy Tinh Tú (+15% Công)", new Color(0.75f, 0.68f, 1f),
                        "Phù Thủy Lục Bảo (+35% Công)", new Color(0.52f, 1f, 0.75f))
                },
                new CatCompanionDefinition
                {
                    id = "cat_laser",
                    displayName = "Mèo Bác Học",
                    title = "Tia Laser Chấm Đỏ",
                    rarity = "UR",
                    description = "Quét chùm tia Laser công suất cao ngang chiến trường, thiêu đốt mọi con chuột trên đường quét.",
                    skillType = CatSkillType.RedDotLaser,
                    sheetResourcePath = "Sprites/Cats/cat_laser_sheet",
                    iconResourcePath = "Sprites/UI/icon_cat_laser",
                    themeColor = new Color(1.0f, 0.28f, 0.35f),
                    baseCooldown = 6.5f,
                    baseDamage = 78f,
                    attackRange = 4.5f,
                    effectRadius = 1.25f,
                    effectDuration = 1.4f,
                    permanentLevel = 1,
                    battleStageLevel = 1,
                    unlocked = false,
                    skins = CreateDefaultSkins(
                        "Tiến Sĩ Xiêm",
                        "Bác Học Cyber (+15% Công)", new Color(0.55f, 0.95f, 1f),
                        "Bác Học Vũ Trụ (+35% Công)", new Color(1f, 0.72f, 0.42f))
                },
                new CatCompanionDefinition
                {
                    id = "cat_royal",
                    displayName = "Mèo Hoàng Thượng",
                    title = "Mưa Xương Cá Vàng",
                    rarity = "UR",
                    description = "Phóng loạt tên lửa xương cá vàng truy kích chuột và ban phước hồi phục máu cho Tường Thành.",
                    skillType = CatSkillType.RoyalFishBarrage,
                    sheetResourcePath = "Sprites/Cats/cat_royal_sheet",
                    iconResourcePath = "Sprites/UI/icon_cat_royal",
                    themeColor = new Color(1.0f, 0.84f, 0.25f),
                    baseCooldown = 6.8f,
                    baseDamage = 48f,
                    attackRange = 4.6f,
                    effectRadius = 1.40f,
                    effectDuration = 1.5f,
                    permanentLevel = 1,
                    battleStageLevel = 1,
                    unlocked = false,
                    skins = CreateDefaultSkins(
                        "Ba Tư Hoàng Gia",
                        "Nữ Hoàng Hồng Ngọc (+15% Công)", new Color(1f, 0.75f, 0.88f),
                        "Đế Vương Kim Cương (+35% Công)", new Color(0.75f, 0.96f, 1f))
                }
            };
        }

        public static Dictionary<MouseEnemyType, MouseEnemyDefinition> CreateEnemyDefinitions()
        {
            return new Dictionary<MouseEnemyType, MouseEnemyDefinition>
            {
                {
                    MouseEnemyType.Normal,
                    new MouseEnemyDefinition
                    {
                        type = MouseEnemyType.Normal,
                        displayName = "Chuột Cống",
                        sheetResourcePath = "Sprites/Mice/mouse_normal_sheet",
                        maxHp = 50f,
                        moveSpeed = 0.55f,
                        attackDamage = 8f,
                        expReward = 20,
                        goldReward = 8,
                        scale = 0.62f
                    }
                },
                {
                    MouseEnemyType.Runner,
                    new MouseEnemyDefinition
                    {
                        type = MouseEnemyType.Runner,
                        displayName = "Chuột Đạo Chích",
                        sheetResourcePath = "Sprites/Mice/mouse_runner_sheet",
                        maxHp = 35f,
                        moveSpeed = 0.92f,
                        attackDamage = 6f,
                        expReward = 22,
                        goldReward = 10,
                        scale = 0.54f
                    }
                },
                {
                    MouseEnemyType.Tank,
                    new MouseEnemyDefinition
                    {
                        type = MouseEnemyType.Tank,
                        displayName = "Chuột Nồi Giáp",
                        sheetResourcePath = "Sprites/Mice/mouse_tank_sheet",
                        maxHp = 155f,
                        moveSpeed = 0.38f,
                        attackDamage = 15f,
                        expReward = 35,
                        goldReward = 18,
                        scale = 0.76f
                    }
                },
                {
                    MouseEnemyType.Bomber,
                    new MouseEnemyDefinition
                    {
                        type = MouseEnemyType.Bomber,
                        displayName = "Chuột Cảm Tử",
                        sheetResourcePath = "Sprites/Mice/mouse_bomber_sheet",
                        maxHp = 65f,
                        moveSpeed = 0.72f,
                        attackDamage = 28f,
                        expReward = 28,
                        goldReward = 14,
                        scale = 0.65f
                    }
                },
                {
                    MouseEnemyType.Boss,
                    new MouseEnemyDefinition
                    {
                        type = MouseEnemyType.Boss,
                        displayName = "Vua Chuột Cống",
                        sheetResourcePath = "Sprites/Mice/mouse_boss_sheet",
                        maxHp = 880f,
                        moveSpeed = 0.28f,
                        attackDamage = 45f,
                        expReward = 120,
                        goldReward = 95,
                        scale = 1.05f
                    }
                }
            };
        }

        public static List<EventMissionItem> CreateDefaultEventMissions()
        {
            return new List<EventMissionItem>
            {
                new EventMissionItem
                {
                    id = "ev_login",
                    title = "Quà Tân Thủ Khế Ước Mèo",
                    description = "Đăng nhập lần đầu nhận ngay Kim Cương & Vàng để quay Gacha triệu hồi Mèo!",
                    targetProgress = 1,
                    currentProgress = 1,
                    rewardGold = 500,
                    rewardGems = 300,
                    claimed = false
                },
                new EventMissionItem
                {
                    id = "ev_kill_20",
                    title = "Chiến Dịch Diệt Chuột I",
                    description = "Tiêu diệt tổng cộng 20 con chuột xâm lăng phòng tuyến.",
                    targetProgress = 20,
                    currentProgress = 0,
                    rewardGold = 400,
                    rewardGems = 150,
                    claimed = false
                },
                new EventMissionItem
                {
                    id = "ev_roll_gacha",
                    title = "Thử Vận May Triệu Hồi",
                    description = "Thực hiện ít nhất 1 lần Quay Gacha Mèo trong tab Cửa Hàng.",
                    targetProgress = 1,
                    currentProgress = 0,
                    rewardGold = 350,
                    rewardGems = 200,
                    claimed = false
                },
                new EventMissionItem
                {
                    id = "ev_upgrade_hero",
                    title = "Huấn Luyện Chỉ Huy",
                    description = "Nâng cấp bất kỳ chỉ số nào của Nhân Vật Chính hoặc Mèo.",
                    targetProgress = 1,
                    currentProgress = 0,
                    rewardGold = 300,
                    rewardGems = 120,
                    claimed = false
                },
                new EventMissionItem
                {
                    id = "ev_wave_3",
                    title = "Bảo Vệ Cứ Điểm Vững Chắc",
                    description = "Chiến đấu vượt qua Đợt tấn công thứ 3 của đàn chuột.",
                    targetProgress = 3,
                    currentProgress = 1,
                    rewardGold = 800,
                    rewardGems = 400,
                    claimed = false
                }
            };
        }

        public static List<HeroWeaponDefinition> CreateDefaultWeapons()
        {
            return new List<HeroWeaponDefinition>
            {
                new HeroWeaponDefinition
                {
                    id = "wpn_standard",
                    displayName = "Gatling Tiêu Chuẩn",
                    rarity = "SR",
                    description = "Súng máy 3 nòng xoay cơ bản của Chỉ Huy Phòng Tuyến.",
                    tintColor = Color.white,
                    damageMultiplier = 1.0f,
                    fireRateMultiplier = 1.0f,
                    bonusBarrels = 0,
                    level = 1,
                    unlocked = true
                },
                new HeroWeaponDefinition
                {
                    id = "wpn_flame",
                    displayName = "Gatling Hỏa Ngục",
                    rarity = "SSR",
                    description = "Nòng súng rực lửa tăng +35% Sát thương và +15% Tốc bắn.",
                    tintColor = new Color(1.0f, 0.58f, 0.42f),
                    damageMultiplier = 1.35f,
                    fireRateMultiplier = 1.15f,
                    bonusBarrels = 0,
                    level = 1,
                    unlocked = false
                },
                new HeroWeaponDefinition
                {
                    id = "wpn_plasma",
                    displayName = "Gatling Điện Tử",
                    rarity = "SSR",
                    description = "Nòng súng năng lượng xanh tăng +25% Sát thương và +35% Tốc bắn.",
                    tintColor = new Color(0.45f, 0.92f, 1.0f),
                    damageMultiplier = 1.25f,
                    fireRateMultiplier = 1.35f,
                    bonusBarrels = 0,
                    level = 1,
                    unlocked = false
                },
                new HeroWeaponDefinition
                {
                    id = "wpn_dragon_gold",
                    displayName = "Thần Pháo Bão Đạn",
                    rarity = "UR",
                    description = "Vũ khí tối thượng tăng +65% Sát thương, +25% Tốc bắn và bắn kèm +1 Tia Đạn ngay từ đầu!",
                    tintColor = new Color(1.0f, 0.88f, 0.35f),
                    damageMultiplier = 1.65f,
                    fireRateMultiplier = 1.25f,
                    bonusBarrels = 1,
                    level = 1,
                    unlocked = false
                }
            };
        }

        public static List<CatSkinData> CreateHeroSkins()
        {
            return new List<CatSkinData>
            {
                new CatSkinData { skinName = "Chỉ Huy Xanh (Mặc Định)", tintColor = Color.white, damageBonusPct = 0f, unlocked = true },
                new CatSkinData { skinName = "Chiến Binh Hồng Ngọc (+20% Công)", tintColor = new Color(1f, 0.72f, 0.85f), damageBonusPct = 0.20f, unlocked = false },
                new CatSkinData { skinName = "Siêu Anh Hùng Hoàng Kim (+45% Công)", tintColor = new Color(1f, 0.92f, 0.48f), damageBonusPct = 0.45f, unlocked = false }
            };
        }
    }
}
