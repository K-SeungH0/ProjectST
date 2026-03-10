using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DataManager : MonoSingleton_Global<DataManager>
{
    // --- ScriptableObject 데이터 ---
    public Dictionary<int, WeaponData> Weapons { get; private set; }
    public Dictionary<int, MonsterData> Monsters { get; private set; }
    public Dictionary<int, PlayerData> Players { get; private set; }
    public ConstData ConstData { get; private set; }

    //Unity Method=============================================================================================================
    #region Unity Method
    #endregion
    //=============================================================================================================Unity Method


    //Public Method============================================================================================================
    #region Public Method
    public void LoadAllData()
    {
        LoadTable();
        LoadScriptableObjects();
    }

    public WeaponData GetWeaponData(GameDefine.eWeaponType type)
    {
        // TODO KSH 이거 enum과 ID가 똑같아야 쓸수있음...
        // 나중에 개선 필요할듯..
        return GetWeaponData((int)type);
    }

    public WeaponData GetWeaponData(int id)
    {
        if (Weapons.ContainsKey(id))
            return Weapons[id];

        return null;
    }

    public MonsterData GetMonsterData(int id)
    {
        if (Monsters.ContainsKey(id))
            return Monsters[id];

        LogManager.LogError($"GetMonsterData Error : {id}");
        return null;
    }

    public PlayerData GetPlayerData(int id)
    {
        if (Players.ContainsKey(id))
            return Players[id];

        return null;
    }

    public long RealValue(long baseValue)
    {
        return baseValue / 1000;
    }

    #endregion
    //============================================================================================================Public Method


    //Private Method===========================================================================================================
    #region Private Method

    private void LoadTable()
    {
        TableTextKey.Instance.FileName = "TextKey";
        TableRequiredExp.Instance.FileName = "NEED_EXP";
		TableUpgradeInfo.Instance.FileName = "UPGRADE_DATA";
		TableTowerUpgradeData.Instance.FileName = "UPGRADE_DATA_FOR_TOWER";
		TableUpgradeProbabilityInfo.Instance.FileName = "UPGRADE_GRADE_PROBABILITY_DATA";
        TableRoundSystem.Instance.FileName = "ROUND_SYSTEM";
        TableMissionInfo.Instance.FileName = "MISSION_INFO";
        TablePickupItemInfo.Instance.FileName = "PICKUP_ITEM";
        TableOutGameDataInfo.Instance.FileName = "OUTGAME_UPGRADE";
		TableMonsterProjectileInfo.Instance.FileName = "Monster_Projectile_Data";
		TableStageRewardInfo.Instance.FileName = "STAGE_REWARD";
        TableItemInfo.Instance.FileName = "ItemInfo";
        TableEquipUpgradeInfo.Instance.FileName = "Equip_Upgrade";
		TableRelicsInfo.Instance.FileName = "Relics_Info";
        TableStageInfo.Instance.FileName = "STAGE_INFO";
        TableQuestInfo.Instance.FileName = "QUEST_INFO";
        TableFeatureGating.Instance.FileName = "GatedContentData";
        TableProfileInfo.Instance.FileName = "Profile_IMG";
        TableGlobalIconInfo.Instance.FileName = "Global_Icon";
        TableTowerContents.Instance.FileName = "TowerContents";
        TableShopInfo.Instance.FileName = "SHOP";
        TablePackageInfo.Instance.FileName = "Package_Info";
        TableBossInfo.Instance.FileName = "BOSS_INFO";
        TableBlacklistInfo.Instance.FileName = "nickname_blocklist";
        TablePatrolRewardInfo.Instance.FileName = "PATROL_REWARD_INFO";
        TableNotificationInfo.Instance.FileName = "NOTIFICATION_INFO";
	}

    private void LoadScriptableObjects()
    {
        Weapons = LoadAllData<WeaponData>();
        Monsters = LoadAllData<MonsterData>();
        Players = LoadAllData<PlayerData>();
        ConstData = LoadConstData();
    }

    /// <summary> 특정 타입의 ScriptableObject를 'Resources/Data/[타입이름]' 폴더에서 모두 로드하여 딕셔너리로 반환합니다. </summary>
    private Dictionary<int, T> LoadAllData<T>() where T : ScriptableObject, IDataID
    {
        string path = $"Data/{typeof(T).Name}";

        T[] assets = Resources.LoadAll<T>(path);

        if (assets == null || assets.Length == 0)
        {
            LogManager.LogWarning($"[DataManager] '{path}' 경로에 에셋이 없거나, 경로를 찾을 수 없습니다.");
            return new Dictionary<int, T>();
        }

        // 각 에셋의 'Id' 속성을 Key로 사용하는 딕셔너리로 변환하여 반환합니다.
        // ToDictionary가 중복된 ID를 감지하면 에러를 발생시키므로, 안전하게 처리합니다.
        try
        {
            return assets.ToDictionary(asset => asset.ID, asset => asset);
        }
        catch (System.ArgumentException e)
        {
            LogManager.LogError($"[DataManager] '{typeof(T).Name}' 데이터에 중복된 ID가 존재합니다! 에러: {e.Message}");
            // 중복이 있을 경우, 첫 번째 데이터만 남기고 나머지는 무시하는 방식으로 처리할 수도 있습니다.
            return assets.GroupBy(asset => asset.ID).ToDictionary(group => group.Key, group => group.First());
        }
    }

    private ConstData LoadConstData()
    {
        string path = $"Data/ConstData";
        ConstData asset = Resources.Load<ConstData>(path);

        if (asset == null)
        {
            LogManager.LogWarning($"[DataManager] '{path}' 경로에 에셋이 없거나, 경로를 찾을 수 없습니다.");
            return new ConstData();
        }

        return asset.Copy();
    }

    private Dictionary<int, List<WaveData>> GroupWavesByRound(List<WaveData> waveDatas)
    {
        var groupedWaves = new Dictionary<int, List<WaveData>>();
        foreach (var wave in waveDatas)
        {
            if (groupedWaves.ContainsKey(wave.Round) == false)
                groupedWaves[wave.Round] = new List<WaveData>();

            groupedWaves[wave.Round].Add(wave);
        }
        return groupedWaves;
    }

    #endregion
    //===========================================================================================================Private Method
}
