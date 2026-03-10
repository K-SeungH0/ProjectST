using GameDefine;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Unity.Services.Authentication;
using UnityEngine;
using Firebase.Firestore;
using Unity.VisualScripting;

#region 데이터 직렬화 및 변환 클래스

// Key-Value 쌍을 직렬화하기 위한 클래스
[Serializable]
public class SerializableKV
{
    public long Key;    // enum -> int로 변환
    public long Value;
}

[Serializable]
public class SerializableStringKV
{
    public string Key;
	public long Value;
}

[Serializable]
public class SerializableClassKV
{
    public int Key;
    public string Value;
}

// PlayerPrefs 저장을 위한 직렬화 가능 데이터 구조
[Serializable]
public class UserGameDataSerializable
{
    // 약관 동의 여부
    public bool IsTerms;
    // 광고 동의
    public bool IsADConsent;
    // SaveVersion 으로 DB데이터와 로컬 데이터 무엇을 사용할지 결정
    public long SaveVersion = 1;
    
    public string NickName;

    public int Level;
    public long Gold;
    public long Stamina;
    public long Gem;
    public long TowerTicket;
    public long StaminaLastUpdateTime;
    public long TowerTicketLastUpdateTime;

    // Dictionary는 직접 직렬화가 불가능하므로 List<SerializableKV>로 변환
    public List<SerializableKV> UpgradeTokens = new List<SerializableKV>();
    public List<SerializableKV> UpgradeLevels = new List<SerializableKV>();
    public List<SerializableKV> Weapons = new List<SerializableKV>();
    public List<SerializableKV> WeaponUpgradeLevels = new List<SerializableKV>();
    public eWeaponType EquipWeapon;

    // 프로필
    public int SelectedProfileImageID;
    public int SelectedFrameID;

    // Content Gating - Unlock된 Feature들
    public List<int> UnlockedFeatures = new List<int>();
    
    // 첫 게임 플레이 완료 여부 (Content Gating 조건)
    public int HasPlayedFirstGame = 0;
    public List<SerializableKV> StageClearCount = new List<SerializableKV>();
	public List<int> FlagOptions = new List<int>();

	public List<SerializableClassKV> QuestStateData = new List<SerializableClassKV>();

    // Choice Weapon 선택 풀 (무기 ID 리스트)
    public List<int> SelectedChoiceWeapons = new List<int>();

	public List<SerializableStringKV> PurchaseCount = new List<SerializableStringKV>();

	public List<SerializableClassKV> PurchaseHistory = new List<SerializableClassKV>();

    public long CreatedDay;
	public long LastLoginDay;
    public long LoginCount;
    public int TowerFloor;
    public List<int> TowerClear = new List<int>();
    public List<int> TowerRewarded = new List<int>();
    public bool ADSkip;
    public long LastPatrolTime;
    public long FastPatrolADCount;
    public long FastPatrolStaminaCount;
    public long TowerSweepADCount;
    public long TowerSweepRemainCount;
	public long CreatedTicks;
	public long LastLoginTicks;
	public long SingleGamePlayCount;
	public long MultiGamePlayCount;
    public string SystemMessage;

    public List<SerializableKV> Inventory = new List<SerializableKV>();
	public List<long> ActiveItems = new List<long>();
}

// 실제 게임에서 사용할 데이터 구조 (Dictionary 사용)
[Serializable]
public struct UserGameData
{
    // 약관 동의 여부
    public bool IsTerms;
    // 광고 동의
    public bool IsADConsent;

    public long SaveVersion;
    public string NickName { get; set; }
    public int Level;

    // 재화
    public long Gold;
    public long Stamina;
    public long Gem;
    public long TowerTicket;
    // 스태미나 마지막 업데이트 시간 (DateTime.ToBinary() 값)
    public long StaminaLastUpdateTime;
    // 티켓 마지막 업데이트 시간 (DateTime.ToBinary() 값)
    public long TowerTicketLastUpdateTime;

    public Dictionary<eOutGameDataType, int> UpgradeLevels;
    public Dictionary<eOutGameUpgradeToken, long> UpgradeTokens;
    public Dictionary<eWeaponType, eWeaponRarity> Weapons;
    public Dictionary<eWeaponType, int> WeaponUpgradeLevels; // 무기 업그레이드 레벨
    public eWeaponType EquipWeapon;

    // 인벤토리
    // 앞으로 추가되는 아이템은 여기에 넣어서 쓰자.
    public Dictionary<long, long> Inventory;
    // 활성화 된 아이템들
    public HashSet<long> ActiveItems;

    // 프로필
    public int SelectedProfileImageID;
    public int SelectedFrameID;

    // Content Gating - Unlock된 Feature들
    public HashSet<int> UnlockedFeatures;
    
    //퀘스트 관련 정보
    public Dictionary<eQuestProgressType, QuestStateData> QuestStateData;

    // 게임 플레이 완료 횟수 (Content Gating 조건)
    public int GamePlaycount;
    // 스테이지 클리어 횟수
    public Dictionary<int, long> StageClearCount;
    // 유저 생애주기에 1번만 실행할 플래그 옵션들
    public HashSet<int> FlagOptions;

    // Choice Weapon 선택 풀 (런타임 해시셋)
    public HashSet<eWeaponType> SelectedChoiceWeapons;

	// 상점 구매 이력 정보
	public Dictionary<string, long> PurchaseCount;
	public Dictionary<eShopPurchaseResetType, PurchaseHistoryData> PurchaseHistory;

    //RU시작 시점
	public long CreatedDay;
	public long LastLoginDay;
    public long LoginCount { get; set; }

    public int TowerFloor;
    public HashSet<int> TowerClear;
    public HashSet<int> TowerRewarded;

    public bool ADSkip;

    public long LastPatrolTime;
    public long FastPatrolADCount;
	public long FastPatrolStaminaCount;

    public long TowerSweepADCount;
    public long TowerSweepRemainCount;

    public long CreatedTicks;
	public long SingleGamePlayCount;
	public long MultiGamePlayCount;

    public string SystemMessage;

    public bool CheckQuestSchedule()
	{
        bool isInitSchedule = false;

        if(QuestStateData != null)
		{
            foreach(var i in QuestStateData)
			{
                if(i.Value != null)
                    isInitSchedule |= i.Value.CheckQuestSchedule();
            }
		}

        return isInitSchedule;
    }

    public bool CheckPurchaseHistorySchedule()
    {
		bool isInitSchedule = false;

		if (PurchaseHistory != null)
		{
			foreach (var i in PurchaseHistory)
			{
				if (i.Value != null)
					isInitSchedule |= i.Value.CheckPurchaseSchedule();
			}
		}

		return isInitSchedule;
	}

	/// <summary> 매일 1회 로그인 날짜, 횟수 갱신 </summary>
	public bool CheckLoginSchedule()
    {
		var nowSchedule = TableQuestInfo.Instance.GetCurrentScheduleNumber((long)GameMain.GameTicks_RealTimeSinceStartup, 1, 0, 0);
        if(nowSchedule != LastLoginDay)
        {
            LastLoginDay = nowSchedule;
            if (CreatedDay == 0)
            {
                CreatedDay = LastLoginDay;
                CreatedTicks = (long)GameMain.GameTicks_RealTimeSinceStartup;
			}

			LoginCount++;
            FastPatrolADCount = DataManager.Instance.ConstData.PATROL_FAST_PATROL_CAN_WATCH_AD;
            FastPatrolStaminaCount = DataManager.Instance.ConstData.PATROL_FAST_PATROL_CAN_RICEPT;
			PlatformManager.Instance.SendLog(PlatformManager.LOGIN, "day", (LastLoginDay - CreatedDay).ToString());
            OnQuestProgress(eQuestType.ATTENDANCE, eQuestParamType.Set, LoginCount);

            if (TowerTicket < TableTowerContents.Instance.TowerContents_Cost_Max_Count)
            {
                // 조금 복잡하긴한데.. 회복 개수랑 Max 개수랑 데이터 나누기 위해서 이렇게 한다..
                TowerTicket += TableTowerContents.Instance.TowerContents_Cost_Regen_Count;
                if (TowerTicket > TableTowerContents.Instance.TowerContents_Cost_Max_Count)
                    TowerTicket = TableTowerContents.Instance.TowerContents_Cost_Max_Count;
            }

            TowerSweepADCount = TableTowerContents.Instance.TowerContents_SweepDailyCount_AD;
            TowerSweepRemainCount = TableTowerContents.Instance.TowerContents_SweepDailyCount;

            return true;
        }

		//혹시모르니 항상 최종값을 입력해주자..
		OnQuestProgress(eQuestType.ATTENDANCE, eQuestParamType.Set, LoginCount);
		return false;
	}

    public long GetStageClearCount()
	{
        long result = 0;

        if(StageClearCount != null)
		{
            foreach(var i in StageClearCount.Values)
			{
                result += i;
			}
		}

        return result;
	}

	public long GetStageClearCount(int stageID)
	{
		long result = 0;

		if (StageClearCount != null)
		{
			foreach (var i in StageClearCount)
			{
                if(i.Key == stageID || stageID == 0)
				    result += i.Value;
			}
		}

		return result;
	}

	public string GetStageClearCountString()
    {
        string result = "";

        if (StageClearCount != null)
        {
            foreach (var i in StageClearCount)
            {
                result += $"[StageID:{i.Key}, ClearCount:{i.Value}] ";
            }
        }
        return result;
	}

    public string GetWeaponListString()
    {
        string result = "";
        if (Weapons != null)
        {
            foreach (var i in Weapons)
            {
                result += $"[WeaponType:{i.Key}, Rarity:{i.Value}] ";
            }
        }
        return result;
	}

#if DEV_QA
	public void AttendanceTest()
    {
		LoginCount++;
		OnQuestProgress(eQuestType.ATTENDANCE, eQuestParamType.Set, LoginCount);
	}
#endif

    public void ResetQuestHoldFlag()
	{
        if (QuestStateData != null)
        {
            foreach (var i in QuestStateData)
            {
                if (i.Value != null)
                    i.Value.ResetQuestHoldFlag();
            }
        }
    }

    public void OnQuestProgress(eQuestType questType, eQuestParamType paramType, long value, long param = 0)
	{
        //TODO : 음.. 매번 순환? 최적화 고민
        if (QuestStateData != null)
        {
            foreach (var i in QuestStateData)
            {
                if (i.Value != null)
                    i.Value.OnQuestProgress(questType, paramType, value, param);
            }
        }
    }

	public bool HasReward()
	{
        //TODO : 음.. 매번 순환? 최적화 고민
        if (QuestStateData != null)
        {
            foreach (var i in QuestStateData)
            {
                if (i.Value != null && i.Value.HasReward())
                    return true;
            }
        }

        return false;
    }

	public bool SetFlagOption(eFlagOptions value)
	{
		if (FlagOptions.Contains((int)value) == false)
		{
			switch (value)
			{
				//보내야하는 로그 있으면 로그 요청
				case eFlagOptions.FIRST_QUEST_REWARD:
					PlatformManager.Instance.SendLog(PlatformManager.TUTORIAL_COMPLETE, "tutorial_step", 3.ToString());
					break;
				case eFlagOptions.FIRST_WEAPON_EQUIP:
					PlatformManager.Instance.SendLog(PlatformManager.TUTORIAL_COMPLETE, "tutorial_step", 4.ToString());
					break;
			}

			FlagOptions.Add((int)value);
            DBManager.Instance.StartSaveData();

			return true;
		}

		return false;
	}

	/// <summary>
	/// Firestore에 저장하기 위해 구조체를 Dictionary로 변환합니다.
	/// </summary>
	public Dictionary<string, object> GetSaveDataDictionary()
    {
        var dataToSave = new Dictionary<string, object>();

		// 1. 단순 값 필드
		// NickName은 user 문서의 루트 필드로 관리되므로 여기서는 제외합니다.

		dataToSave[DBManager.SAVE_KEY_TERMS] = IsTerms;
		dataToSave[DBManager.SAVE_KEY_AD_CONSENT] = IsADConsent;
        dataToSave[DBManager.SAVE_KEY_VERSION] = SaveVersion;
		dataToSave[DBManager.SAVE_KEY_NICKNAME] = NickName;
		dataToSave[DBManager.SAVE_KEY_LEVEL] = Level;
        dataToSave[DBManager.SAVE_KEY_GOLD] = Gold;
        dataToSave[DBManager.SAVE_KEY_STAMINA] = Stamina;
        dataToSave[DBManager.SAVE_KEY_GEM] = Gem;
        dataToSave[DBManager.SAVE_KEY_TOWER_TICKET] = TowerTicket;
        dataToSave[DBManager.SAVE_KEY_STAMINA_LAST_UPDATE_TIME] = StaminaLastUpdateTime;
        dataToSave[DBManager.SAVE_KEY_TOWER_TICKET_LAST_UPDATE_TIME] = TowerTicketLastUpdateTime;
        dataToSave[DBManager.SAVE_KEY_SELECTED_PROFILE_IMAGE_ID] = SelectedProfileImageID;
        dataToSave[DBManager.SAVE_KEY_SELECTED_FRAME_ID] = SelectedFrameID;
        dataToSave[DBManager.SAVE_KEY_HAS_PLAYED_FIRST_GAME] = GamePlaycount;
		dataToSave[DBManager.SAVE_KEY_CREATED_DAY] = CreatedDay;
		dataToSave[DBManager.SAVE_KEY_LAST_LOGIN_DAY] = LastLoginDay;
		dataToSave[DBManager.SAVE_KEY_LOGIN_COUNT] = LoginCount;
		dataToSave[DBManager.SAVE_KEY_TOWER_FLOOR] = TowerFloor;
        dataToSave[DBManager.SAVE_KEY_TOWER_CLEAR] = TowerClear;
        dataToSave[DBManager.SAVE_KEY_TOWER_REWARDED] = TowerRewarded;
        dataToSave[DBManager.SAVE_KEY_AD_SKIP] = ADSkip;
		dataToSave[DBManager.SAVE_KEY_LAST_PATROL_TIME] = LastPatrolTime;
        dataToSave[DBManager.SAVE_KEY_LAST_FAST_PATROL_AD] = FastPatrolADCount;
		dataToSave[DBManager.SAVE_KEY_LAST_FAST_PATROL_STAMINA] = FastPatrolStaminaCount;
		dataToSave[DBManager.SAVE_KEY_TOWER_SWEEP_AD] = TowerSweepADCount;
		dataToSave[DBManager.SAVE_KEY_TOWER_SWEEP_REMAIN] = TowerSweepRemainCount;
		dataToSave[DBManager.SAVE_KEY_CREATED_TICKS] = CreatedTicks;
		dataToSave[DBManager.SAVE_KEY_SINGLE_GAME_PLAY_COUNT] = SingleGamePlayCount;
		dataToSave[DBManager.SAVE_KEY_MULTI_GAME_PLAY_COUNT] = MultiGamePlayCount;
		dataToSave[DBManager.SAVE_KEY_SYSTEM_MESSAGE] = SystemMessage;

		// 2. Enum 값 (string으로 변환)
		dataToSave[DBManager.SAVE_KEY_EQUIP_WEAPON] = EquipWeapon.ToString();

        // 3. Dictionary<Enum, T> -> Map (Key를 string으로 변환)
        if (UpgradeLevels != null)
            dataToSave[DBManager.SAVE_KEY_UPGRADE_LEVELS] = UpgradeLevels.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object)kvp.Value);
        else
            dataToSave[DBManager.SAVE_KEY_UPGRADE_LEVELS] = new Dictionary<string, object>();

        if (UpgradeTokens != null)
            dataToSave[DBManager.SAVE_KEY_UPGRADE_TOKENS] = UpgradeTokens.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object)kvp.Value);
        else
            dataToSave[DBManager.SAVE_KEY_UPGRADE_TOKENS] = new Dictionary<string, object>();

        if (Weapons != null)
            dataToSave[DBManager.SAVE_KEY_WEAPONS] = Weapons.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object)kvp.Value.ToString());
        else
            dataToSave[DBManager.SAVE_KEY_WEAPONS] = new Dictionary<string, object>();

        if (WeaponUpgradeLevels != null)
            dataToSave[DBManager.SAVE_KEY_WEAPONS_UPGRADE_LEVEL] = WeaponUpgradeLevels.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object)kvp.Value);
        else
            dataToSave[DBManager.SAVE_KEY_WEAPONS_UPGRADE_LEVEL] = new Dictionary<string, object>();

        // 4. HashSet<T> -> List (Array)
        if (UnlockedFeatures != null)
            dataToSave[DBManager.SAVE_KEY_UNLOCKED_FEATURES] = new List<int>(UnlockedFeatures);
        else
            dataToSave[DBManager.SAVE_KEY_UNLOCKED_FEATURES] = new List<int>();

		if (FlagOptions != null)
			dataToSave[DBManager.SAVE_KEY_FLAG_OPTIONS] = new HashSet<int>(FlagOptions);
		else
			dataToSave[DBManager.SAVE_KEY_FLAG_OPTIONS] = new HashSet<int>();

		if (SelectedChoiceWeapons != null)
            dataToSave[DBManager.SAVE_KEY_SELECTED_CHOICE_WEAPONS] = SelectedChoiceWeapons.Select(e => e.ToString()).ToList();
        else
            dataToSave[DBManager.SAVE_KEY_SELECTED_CHOICE_WEAPONS] = new List<string>();

        // 5. 중첩 클래스 Dictionary -> Map (QuestStateData)
        var questStateDictionary = new Dictionary<string, object>();
        if (QuestStateData != null)
        {
            foreach (var kvp in QuestStateData)
            {
                // QuestStateData 클래스의 ToDictionary() 호출
                questStateDictionary[kvp.Key.ToString()] = kvp.Value?.ToDictionary();
            }
        }

        dataToSave[DBManager.SAVE_KEY_QUEST_STATE_DATA] = questStateDictionary;

        if (StageClearCount != null)
            dataToSave[DBManager.SAVE_KEY_STAGE_CLEAR_COUNT] = StageClearCount.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object)kvp.Value);
        else
            dataToSave[DBManager.SAVE_KEY_STAGE_CLEAR_COUNT] = new Dictionary<int, long>();

        if (PurchaseCount != null)
            dataToSave[DBManager.SAVE_KEY_PURCHASE_COUNT] = PurchaseCount.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
        else
            dataToSave[DBManager.SAVE_KEY_PURCHASE_COUNT] = new Dictionary<string, long>();

		var purchaseHistoryDictionary = new Dictionary<string, object>();
		if (PurchaseHistory != null)
		{
			foreach (var kvp in PurchaseHistory)
			{
				// PurchaseHistoryData 클래스의 ToDictionary() 호출
				purchaseHistoryDictionary[kvp.Key.ToString()] = kvp.Value?.ToDictionary();
			}
		}
		dataToSave[DBManager.SAVE_KEY_PURCHASE_HISTORY] = purchaseHistoryDictionary;

        if (Inventory != null)
            dataToSave[DBManager.SAVE_KEY_INVENTORY] = Inventory.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object)kvp.Value);
        else
            dataToSave[DBManager.SAVE_KEY_INVENTORY] = new Dictionary<string, object>();
        
        if (ActiveItems  != null)
            dataToSave[DBManager.SAVE_KEY_ACTIVE_ITEMS] = new HashSet<long>(ActiveItems);
        else
            dataToSave[DBManager.SAVE_KEY_ACTIVE_ITEMS] = new HashSet<long>();

        return dataToSave;
    }

    /// <summary>
    /// Firestore에서 로드한 Dictionary를 UserGameData 구조체로 복원합니다.
    /// </summary>
    public void ParseFromDictionary(Dictionary<string, object> data)
    {
        if (data == null)
            data = new Dictionary<string, object>();

        IsTerms = DBHelper.GetOrDefault<bool>(data, DBManager.SAVE_KEY_TERMS);
        IsADConsent = DBHelper.GetOrDefault<bool>(data, DBManager.SAVE_KEY_AD_CONSENT);

        var saveDBVersion = DBHelper.GetOrDefault<long>(data, DBManager.SAVE_KEY_VERSION);
        if (SaveVersion > saveDBVersion)
        {
            // 현재 SaveVersion이 DB에 저장 되어있는 Version보다 높을 경우
            // 현재 버전을 DB에 저장 해야된다.
            DBManager.Instance.StartSaveData(isForce: true);
            return;
        }

        SaveVersion = saveDBVersion;

        // 1. 단순 값 필드 (헬퍼 사용)
        NickName = DBHelper.GetOrDefault<string>(data, DBManager.SAVE_KEY_NICKNAME);
		LogManager.Log($"NickName : {NickName}");
		UserData.Instance.SetUserNickname(NickName);

		Level = DBHelper.GetOrDefault<int>(data, DBManager.SAVE_KEY_LEVEL);
        Gold = DBHelper.GetOrDefault<long>(data, DBManager.SAVE_KEY_GOLD);
        Stamina = DBHelper.GetOrDefault<long>(data, DBManager.SAVE_KEY_STAMINA);
        Gem = DBHelper.GetOrDefault<long>(data, DBManager.SAVE_KEY_GEM);
        TowerTicket = DBHelper.GetOrDefault<long>(data, DBManager.SAVE_KEY_TOWER_TICKET);
        StaminaLastUpdateTime = DBHelper.GetOrDefault<long>(data, DBManager.SAVE_KEY_STAMINA_LAST_UPDATE_TIME);
        TowerTicketLastUpdateTime = DBHelper.GetOrDefault<long>(data, DBManager.SAVE_KEY_TOWER_TICKET_LAST_UPDATE_TIME);
        SelectedProfileImageID = DBHelper.GetOrDefault<int>(data, DBManager.SAVE_KEY_SELECTED_PROFILE_IMAGE_ID);
        SelectedFrameID = DBHelper.GetOrDefault<int>(data, DBManager.SAVE_KEY_SELECTED_FRAME_ID);
        GamePlaycount = DBHelper.GetOrDefault<int>(data, DBManager.SAVE_KEY_HAS_PLAYED_FIRST_GAME);
		CreatedDay = DBHelper.GetOrDefault<long>(data, DBManager.SAVE_KEY_CREATED_DAY);
		LastLoginDay = DBHelper.GetOrDefault<long>(data, DBManager.SAVE_KEY_LAST_LOGIN_DAY);
		LoginCount = DBHelper.GetOrDefault<long>(data, DBManager.SAVE_KEY_LOGIN_COUNT);
		TowerFloor = DBHelper.GetOrDefault<int>(data, DBManager.SAVE_KEY_TOWER_FLOOR);
        TowerClear = DBHelper.GetListAsHashSet<int>(data, DBManager.SAVE_KEY_TOWER_CLEAR);
        TowerRewarded = DBHelper.GetListAsHashSet<int>(data, DBManager.SAVE_KEY_TOWER_REWARDED);
        ADSkip = DBHelper.GetOrDefault<bool>(data, DBManager.SAVE_KEY_AD_SKIP);
        LastPatrolTime = DBHelper.GetOrDefault<long>(data, DBManager.SAVE_KEY_LAST_PATROL_TIME);
        FastPatrolADCount = DBHelper.GetOrDefault<long>(data, DBManager.SAVE_KEY_LAST_FAST_PATROL_AD);
        FastPatrolStaminaCount = DBHelper.GetOrDefault<long>(data, DBManager.SAVE_KEY_LAST_FAST_PATROL_STAMINA);
        TowerSweepADCount = DBHelper.GetOrDefault<long>(data, DBManager.SAVE_KEY_TOWER_SWEEP_AD);
        TowerSweepRemainCount = DBHelper.GetOrDefault<long>(data, DBManager.SAVE_KEY_TOWER_SWEEP_REMAIN);
		CreatedTicks = DBHelper.GetOrDefault<long>(data, DBManager.SAVE_KEY_CREATED_TICKS);
		SingleGamePlayCount = DBHelper.GetOrDefault<long>(data, DBManager.SAVE_KEY_SINGLE_GAME_PLAY_COUNT);
		MultiGamePlayCount = DBHelper.GetOrDefault<long>(data, DBManager.SAVE_KEY_MULTI_GAME_PLAY_COUNT);
        SystemMessage = DBHelper.GetOrDefault<string>(data, DBManager.SAVE_KEY_SYSTEM_MESSAGE);

		// 2. Enum 값 (string -> Enum 헬퍼 사용)
		EquipWeapon = DBHelper.GetEnumOrDefault<eWeaponType>(data, DBManager.SAVE_KEY_EQUIP_WEAPON);
        if (EquipWeapon == eWeaponType.BaseWeaponStart)
            EquipWeapon = eWeaponType.Sword;

		// 3. Map -> Dictionary<Enum, T>
		UpgradeLevels = DBHelper.GetDictionary(data, DBManager.SAVE_KEY_UPGRADE_LEVELS, (k, v) => DBHelper.GetEnumOrDefault<eOutGameDataType>(k), v => Convert.ToInt32(v));
        UpgradeTokens = DBHelper.GetDictionary(data, DBManager.SAVE_KEY_UPGRADE_TOKENS, (k, v) => DBHelper.GetEnumOrDefault<eOutGameUpgradeToken>(k), v => Convert.ToInt64(v));
        Weapons = DBHelper.GetDictionary(data, DBManager.SAVE_KEY_WEAPONS, (k, v) => DBHelper.GetEnumOrDefault<eWeaponType>(k), v => DBHelper.GetEnumOrDefault<eWeaponRarity>((string)v));
        WeaponUpgradeLevels = DBHelper.GetDictionary(data, DBManager.SAVE_KEY_WEAPONS_UPGRADE_LEVEL, (k, v) => DBHelper.GetEnumOrDefault<eWeaponType>(k), v => Convert.ToInt32(v));

        // 4. List -> HashSet<T>
        UnlockedFeatures = DBHelper.GetListAsHashSet<int>(data, DBManager.SAVE_KEY_UNLOCKED_FEATURES);
        FlagOptions = DBHelper.GetListAsHashSet<int>(data, DBManager.SAVE_KEY_FLAG_OPTIONS);
		SelectedChoiceWeapons = DBHelper.GetListAsEnumHashSet<eWeaponType>(data, DBManager.SAVE_KEY_SELECTED_CHOICE_WEAPONS);

        // 5. Map -> 중첩 클래스 Dictionary
        QuestStateData = DBHelper.GetDictionary(data, DBManager.SAVE_KEY_QUEST_STATE_DATA, (k, v) => DBHelper.GetEnumOrDefault<eQuestProgressType>(k), v =>
        {
            var questData = new QuestStateData();
            if (v is Dictionary<string, object> questMap)
            {
                questData.ParseFromDictionary(questMap); // QuestStateData에 ParseFromDictionary 필요
            }
            return questData;
        });

        if (LastPatrolTime == 0)
        {
            LastPatrolTime = (long)GameMain.GameTicks_RealTimeSinceStartup;
            FastPatrolADCount = DataManager.Instance.ConstData.PATROL_FAST_PATROL_CAN_WATCH_AD;
            FastPatrolStaminaCount = DataManager.Instance.ConstData.PATROL_FAST_PATROL_CAN_RICEPT;
		}

		// DB에 없는 타입만 새로 생성
		for (var i = eQuestProgressType.NONE + 1; i < eQuestProgressType.MAX; ++i)
		{
			if (QuestStateData.ContainsKey(i) == false || QuestStateData[i] == null)
			{
				QuestStateData.Add(i, new QuestStateData(i));
			}
			else
			{
				// Daily/Weekly/Achievement: 새 Quest가 추가되었는지 확인 및 기존 데이터 테이블 값으로 갱신
				var existingData = QuestStateData[i];
				var questList = TableQuestInfo.Instance.GetQuestList(i);

				if (questList != null && existingData.QuestProgressData != null)
				{
					// 기존 퀘스트 데이터를 테이블 값으로 갱신 (VALUE_1/VALUE_2 교환 대응)
					for (int j = 0; j < existingData.QuestProgressData.Count; j++)
					{
						var existingQuest = existingData.QuestProgressData[j];
						if (existingQuest != null)
						{
							var questData = TableQuestInfo.Instance.GetQuest(existingQuest.KIND);
							if (questData != null)
							{
								// 테이블 값으로 갱신 (복사 생성자 사용)
								existingData.QuestProgressData[j] = new QuestStateUserData(existingQuest);
							}
						}
					}

					foreach (var quest in questList)
					{
						// 기존에 없는 KIND이면 추가
						if (!existingData.QuestProgressData.Any(x => x != null && x.KIND == quest.KIND))
						{
							existingData.QuestProgressData.Add(new QuestStateUserData(quest));
							existingData.TotalPoint += (int)quest.GetRewardAmount();
							LogManager.Log($"[Quest {i}] 새 퀘스트 추가: KIND {quest.KIND}");
						}
					}
				}
			}
		}

        StageClearCount = DBHelper.GetDictionary(data, DBManager.SAVE_KEY_STAGE_CLEAR_COUNT, (k, v) => Convert.ToInt32(k), v => Convert.ToInt64(v));

        PurchaseCount = DBHelper.GetDictionary(data, DBManager.SAVE_KEY_PURCHASE_COUNT, (k, v) => k, v => Convert.ToInt64(v));

        PurchaseHistory = DBHelper.GetDictionary(data, DBManager.SAVE_KEY_PURCHASE_HISTORY, (k, v) => DBHelper.GetEnumOrDefault<eShopPurchaseResetType>(k), v =>
		{
			var historyData = new PurchaseHistoryData();
			if (v is Dictionary<string, object> questMap)
			{
				historyData.ParseFromDictionary(questMap); // PurchaseHistoryData ParseFromDictionary 필요
			}
			return historyData;
		});

        Inventory = DBHelper.GetDictionary(data, DBManager.SAVE_KEY_INVENTORY, (k, v) => Convert.ToInt64(k), v => Convert.ToInt64(v));
        ActiveItems = DBHelper.GetListAsHashSet<long>(data, DBManager.SAVE_KEY_ACTIVE_ITEMS);
    }

    public void OnPurchaseConfirm(string productID, bool isAD)
	{
		if (PurchaseCount != null && productID.IsNullOrWhiteSpace() == false)
        {
            if(isAD == false)
            {
				//TODO : HYC 영수증 검증, 그리고 검증이라 db에 넣는건 db function을 쓰는게 안전하긴함..
			}

			var productData = TableShopInfo.Instance.Get(productID);
            if (productData != null)
            {
				UserData.Instance.AddItem(productData.ITEM_ID, productData.ITEM_COUNT, isAD);

				if (PurchaseCount.ContainsKey(productID) == false)
                    PurchaseCount.Add(productID, 0);

                PurchaseCount[productID]++;

				switch (productData.RESET_TYPE)
                {
                    case eShopPurchaseResetType.NONE:
                        break;
                    case eShopPurchaseResetType.DAILY:
						if (PurchaseHistory.ContainsKey(productData.RESET_TYPE) == false)
							PurchaseHistory.Add(productData.RESET_TYPE, new PurchaseHistoryData(productData.RESET_TYPE));

						PurchaseHistory[productData.RESET_TYPE].OnPurchaseConfirm(productID);
						break;
                    default:
                        LogManager.LogError($"UserData OnPurchaseConfirm Error Invalid Type : {productData.RESET_TYPE}");
                        break;
                }

				DispatchHandler.Dispatch(DispatchHandler.PURCHASE_SUCCESS, productID);
			}

            if (Define.IsSpecualPurchaseID(productID) && UIFormManager.Instance.FindUIForm<UI_OVERLAY_SPECIAL_PURCHASE>())
                UIFormManager.Instance.CloseUIForm<UI_OVERLAY_SPECIAL_PURCHASE>();

			UserData.Instance.SaveUserData(true);
		}
        else
        {
            LogManager.LogError("Purchase Except Error");
        }
	}

    public long GetPurchaseCount(eShopPurchaseResetType shopPurchaseResetType, string productID)
    {
        switch(shopPurchaseResetType)
        {
            case eShopPurchaseResetType.NONE:
				if (PurchaseCount != null && PurchaseCount.ContainsKey(productID))
					return PurchaseCount[productID];
                break;

            case eShopPurchaseResetType.DAILY:
                if (PurchaseHistory != null && PurchaseHistory.ContainsKey(shopPurchaseResetType) && PurchaseHistory[shopPurchaseResetType] != null)
                    return PurchaseHistory[shopPurchaseResetType].GetPurchaseCount(productID);
                break;
			default:
                LogManager.LogError($"UserData GetPurchaseCount Error Invalid Type : {shopPurchaseResetType}");
                break;
		}

		return 0;
	}

    public void AddUpgradeLevel(int value)
    {
        if (Level == 0 && value > 0)
            PlatformManager.Instance.SendLog(PlatformManager.TUTORIAL_COMPLETE, "tutorial_step", 2.ToString());

        PlatformManager.Instance.SendLog_Upgrade("outgame", Level.ToString(), (Level + value).ToString());

        if (RedDotManager.Instance.HasUpgradeBonusNotification() == false)
        {
            foreach (var effectTable in TableOutGameDataInfo.Instance.LevelUpEffectTable)
            {
                if(Level < effectTable.LEVEL && Level + value >= effectTable.LEVEL)
				{
                    RedDotManager.Instance.SetUpgradeBonusNotification(Level + value);
                    break;
				}
            }
        }

        Level += value;

        RedDotManager.Instance.Refresh(RedDotIDs.UPGRADE_BONUS);
	}

	public void SetLastPatrolRewardTime(long lastTime)
	{
        LastPatrolTime = lastTime;
        //이 함수를 호출했다는건 보상을 받았다는거고 그쪽에서 DB저장을 시키니까 여긴 패스..
	}

    public void UseFastPatrol_AD()
    {
        if (FastPatrolADCount > 0)
            FastPatrolADCount--;
	}

	public void UseFastPatrol_Stamina()
	{
		if (FastPatrolStaminaCount > 0)
			FastPatrolStaminaCount--;
	}

    public long GetElapsedHoursSinceCreated()
    {
        DateTime createdTime = new DateTime(CreatedTicks);
        DateTime currentTime = new DateTime((long)GameMain.GameTicks_RealTimeSinceStartup);

        return (long)(currentTime - createdTime).TotalHours;
	}
}

// 친구 요청, 수락용 데이터
[Serializable]
public struct UserFriendData
{
    public string UID;
    public string Nickname;

	public Dictionary<string, object> ToDictionary()
    {
		var result = new Dictionary<string, object>
			{
				{ DBManager.SAVE_KEY_FRIEND_SERVICE_UID, UID },
				{ DBManager.SAVE_KEY_FRIEND_SERVICE_NICKNAME, Nickname },
			};

		return result;
	}
}

// OutGameData와 OutGameDataSerializable 간의 변환을 담당하는 유틸리티 클래스
public static class UserGameDataConverter
{
    public static UserGameDataSerializable ToSerializable(UserGameData src)
    {
        var dst = new UserGameDataSerializable
        {
            IsTerms = src.IsTerms,
            IsADConsent = src.IsADConsent,
            SaveVersion = src.SaveVersion,
            NickName = src.NickName,
            Level = src.Level,
            Gold = src.Gold,
            Stamina = src.Stamina,
            Gem = src.Gem,
            TowerTicket = src.TowerTicket,
            StaminaLastUpdateTime = src.StaminaLastUpdateTime,
            TowerTicketLastUpdateTime = src.TowerTicketLastUpdateTime,
            EquipWeapon = src.EquipWeapon,
            SelectedProfileImageID = src.SelectedProfileImageID,
            SelectedFrameID = src.SelectedFrameID,
            UnlockedFeatures = src.UnlockedFeatures != null ? src.UnlockedFeatures.ToList() : new List<int>(),
            HasPlayedFirstGame = src.GamePlaycount,
            FlagOptions = src.FlagOptions != null ? src.FlagOptions.ToList() : new List<int>(),
            CreatedDay = src.CreatedDay,
            LastLoginDay = src.LastLoginDay,
            LoginCount = src.LoginCount,
            TowerFloor = src.TowerFloor,
            TowerClear = src.TowerClear != null ? src.TowerClear.ToList() : new List<int>(),
            TowerRewarded = src.TowerRewarded != null ? src.TowerRewarded.ToList() : new List<int>(),
            ADSkip = src.ADSkip,
            LastPatrolTime = src.LastPatrolTime,
            FastPatrolADCount = src.FastPatrolADCount,
            FastPatrolStaminaCount = src.FastPatrolStaminaCount,
            TowerSweepADCount = src.TowerSweepADCount,
            TowerSweepRemainCount = src.TowerSweepRemainCount,
            CreatedTicks = src.CreatedTicks,
			SingleGamePlayCount = src.SingleGamePlayCount,
            MultiGamePlayCount = src.MultiGamePlayCount,
            SystemMessage = src.SystemMessage,
        };

        if(src.FlagOptions != null)
        {
			foreach (var flag in src.FlagOptions)
			{
				dst.FlagOptions.Add(flag);
			}
		}

        if (src.UpgradeTokens != null)
        {
            foreach (var kv in src.UpgradeTokens)
            {
                dst.UpgradeTokens.Add(new SerializableKV { Key = (int)kv.Key, Value = kv.Value });
            }
        }

        if (src.UpgradeLevels != null)
        {
            foreach (var kv in src.UpgradeLevels)
            {
                // UpgradeLevels는 int 값(레벨)을 가지지만, SerializableKV는 long을 사용하므로 형 변환 필요
                dst.UpgradeLevels.Add(new SerializableKV { Key = (int)kv.Key, Value = kv.Value });
            }
        }

        if (src.Weapons != null)
        {
            foreach (var kv in src.Weapons)
            {
                // eWeaponRarity는 enum이므로 long으로 캐스팅하여 저장
                dst.Weapons.Add(new SerializableKV { Key = (int)kv.Key, Value = (long)kv.Value });
            }
        }

        if (src.WeaponUpgradeLevels != null)
        {
            foreach (var kv in src.WeaponUpgradeLevels)
            {
                dst.WeaponUpgradeLevels.Add(new SerializableKV { Key = (int)kv.Key, Value = kv.Value });
            }
        }

        if (src.QuestStateData != null)
		{
            foreach (var kv in src.QuestStateData)
            {
                if(kv.Value != null)
                    dst.QuestStateData.Add(new SerializableClassKV { Key = (int)kv.Key, Value = JsonUtility.ToJson(kv.Value)});
            }
        }

        // SelectedChoiceWeapons 직렬화
        if (src.SelectedChoiceWeapons != null)
        {
            foreach (var wt in src.SelectedChoiceWeapons)
                dst.SelectedChoiceWeapons.Add((int)wt);
        }

        if (src.StageClearCount != null)
        {
            foreach (var kv in src.StageClearCount)
            {
                dst.StageClearCount.Add(new SerializableKV { Key = kv.Key, Value = kv.Value });
            }
        }
        else
		{
            dst.StageClearCount = new List<SerializableKV>();
		}

        if (src.PurchaseCount != null)
		{
			foreach (var kv in src.PurchaseCount)
			{
				dst.PurchaseCount.Add(new SerializableStringKV { Key = kv.Key, Value = kv.Value });
			}
		}
        else
        {
            dst.PurchaseCount = new List<SerializableStringKV>();
        }

        if (src.PurchaseHistory != null)
        {
            foreach (var kv in src.PurchaseHistory)
            {
                dst.PurchaseHistory.Add(new SerializableClassKV { Key = (int)kv.Key, Value = JsonUtility.ToJson(kv.Value) });
            }
        }
        else
        {
            dst.PurchaseHistory = new List<SerializableClassKV>();
        }

        if (src.Inventory != null)
        {
            foreach (var kv in src.Inventory)
            {
                dst.Inventory.Add(new SerializableKV { Key = kv.Key, Value = kv.Value });
            }
        }
        else
        {
            dst.Inventory = new List<SerializableKV>();
        }

        if (src.ActiveItems != null)
        {
            foreach (var item in src.ActiveItems)
            {
                dst.ActiveItems.Add(item);
            }
        }
        return dst;
    }

    public static UserGameData FromSerializable(UserGameDataSerializable src)
    {
        var dst = new UserGameData
        {
            IsTerms = src.IsTerms,
            IsADConsent = src.IsADConsent,
            SaveVersion = src.SaveVersion,
            NickName = src.NickName,
            Level = src.Level,
            Gold = src.Gold,
            Stamina = src.Stamina,
            Gem = src.Gem,
            TowerTicket = src.TowerTicket,
            StaminaLastUpdateTime = src.StaminaLastUpdateTime,
            TowerTicketLastUpdateTime = src.TowerTicketLastUpdateTime,
            UpgradeLevels = new Dictionary<eOutGameDataType, int>(),
            UpgradeTokens = new Dictionary<eOutGameUpgradeToken, long>(),
            Weapons = new Dictionary<eWeaponType, eWeaponRarity>(),
            WeaponUpgradeLevels = new Dictionary<eWeaponType, int>(),
            EquipWeapon = src.EquipWeapon,
            SelectedProfileImageID = src.SelectedProfileImageID,
            SelectedFrameID = src.SelectedFrameID,
            UnlockedFeatures = src.UnlockedFeatures != null ? new HashSet<int>(src.UnlockedFeatures) : new HashSet<int>(),
            QuestStateData = new Dictionary<eQuestProgressType, QuestStateData>(),
            GamePlaycount = src.HasPlayedFirstGame,
            FlagOptions = src.FlagOptions != null ? new HashSet<int>(src.FlagOptions) : new HashSet<int>(),
            SelectedChoiceWeapons = new HashSet<eWeaponType>(),
            StageClearCount = new Dictionary<int, long>(),
            PurchaseCount = new Dictionary<string, long>(),
            PurchaseHistory = new Dictionary<eShopPurchaseResetType, PurchaseHistoryData>(),
            CreatedDay = src.CreatedDay,
            LastLoginDay = src.LastLoginDay,
            LoginCount = src.LoginCount,
            TowerFloor = src.TowerFloor,
            TowerClear = src.TowerClear != null ? new HashSet<int>(src.TowerClear) : new HashSet<int>(),
            TowerRewarded = src.TowerRewarded != null ? new HashSet<int>(src.TowerRewarded) : new HashSet<int>(),
            ADSkip = src.ADSkip,
            LastPatrolTime = src.LastPatrolTime,
            FastPatrolADCount = src.FastPatrolADCount,
            FastPatrolStaminaCount = src.FastPatrolStaminaCount,
            TowerSweepADCount = src.TowerSweepADCount,
            TowerSweepRemainCount = src.TowerSweepRemainCount,
            CreatedTicks = src.CreatedTicks,
			SingleGamePlayCount = src.SingleGamePlayCount,
            MultiGamePlayCount = src.MultiGamePlayCount,
            SystemMessage = src.SystemMessage,
            Inventory = new Dictionary<long, long>(),
            ActiveItems = src.ActiveItems != null ? new HashSet<long>(src.ActiveItems) : new HashSet<long>(),
        };

        if (dst.LastPatrolTime == 0)
        {
            dst.LastPatrolTime = (long)GameMain.GameTicks_RealTimeSinceStartup;
			dst.FastPatrolADCount = DataManager.Instance.ConstData.PATROL_FAST_PATROL_CAN_WATCH_AD;
            dst.FastPatrolStaminaCount = DataManager.Instance.ConstData.PATROL_FAST_PATROL_CAN_RICEPT;
		}

		foreach (var kv in src.UpgradeLevels)
        {
            var key = (eOutGameDataType)kv.Key;
            dst.UpgradeLevels[key] = (int)kv.Value; // long에서 int로 역변환
        }
        foreach (var kv in src.UpgradeTokens)
        {
            var key = (eOutGameUpgradeToken)kv.Key;
            dst.UpgradeTokens[key] = kv.Value;
        }
        foreach (var kv in src.Weapons)
        {
            var key = (eWeaponType)kv.Key;
            var value = (eWeaponRarity)kv.Value;
            
            // DB 중복 키 방어 (손상된 데이터 감지)
            if (dst.Weapons.ContainsKey(key))
            {
                LogManager.LogWarning($"[DB] 중복 무기 키 감지 및 덮어쓰기: {key}, 기존={dst.Weapons[key]}, 새값={value}");
            }
            
            dst.Weapons[key] = value; // long에서 eWeaponRarity로 역변환
        }

        if (src.WeaponUpgradeLevels != null)
        {
            foreach (var kv in src.WeaponUpgradeLevels)
            {
                var key = (eWeaponType)kv.Key;
                dst.WeaponUpgradeLevels[key] = (int)kv.Value; // long에서 int로 역변환
            }
        }

        foreach (var kv in src.QuestStateData)
            {
                if (kv.Value.IsNullOrWhiteSpace() == false)
                    dst.QuestStateData.Add((eQuestProgressType)kv.Key, JsonUtility.FromJson<QuestStateData>(kv.Value));
            }

		// SelectedChoiceWeapons 역직렬화
		if (src.SelectedChoiceWeapons != null)
        {
            foreach (var id in src.SelectedChoiceWeapons)
                dst.SelectedChoiceWeapons.Add((eWeaponType)id);
            
            LogManager.Log($"[ChoicePool] 로드 완료: {dst.SelectedChoiceWeapons.Count}개 복원됨");
        }

        if (src.StageClearCount.Count > 0)
        {
            foreach (var kv in src.StageClearCount)
            {
                dst.StageClearCount.Add((int)kv.Key, kv.Value);
            }
        }

        if (src.PurchaseCount.Count > 0)
        {
            foreach (var kv in src.PurchaseCount)
            {
                dst.PurchaseCount.Add(kv.Key, kv.Value);
            }
        }

        if (src.PurchaseHistory.Count > 0)
        {
            foreach (var kv in src.PurchaseHistory)
            {
                dst.PurchaseHistory.Add((eShopPurchaseResetType)kv.Key, JsonUtility.FromJson<PurchaseHistoryData>(kv.Value));
            }
        }

        // DB에 없는 타입만 새로 생성
        for (var i = eQuestProgressType.NONE + 1; i < eQuestProgressType.MAX; ++i)
        {
            if (dst.QuestStateData.ContainsKey(i) == false || dst.QuestStateData[i] == null)
            {
                dst.QuestStateData.Add(i, new QuestStateData(i));
            }
            else
            {
                // Daily/Weekly/Achievement: 새 Quest가 추가되었는지 확인 및 기존 데이터 테이블 값으로 갱신
                var existingData = dst.QuestStateData[i];
                var questList = TableQuestInfo.Instance.GetQuestList(i);

                if (questList != null && existingData.QuestProgressData != null)
                {
                    // 기존 퀘스트 데이터를 테이블 값으로 갱신 (VALUE_1/VALUE_2 교환 대응)
                    for (int j = 0; j < existingData.QuestProgressData.Count; j++)
                    {
                        var existingQuest = existingData.QuestProgressData[j];
                        if (existingQuest != null)
                        {
                            var questData = TableQuestInfo.Instance.GetQuest(existingQuest.KIND);
                            if (questData != null)
                            {
                                // 테이블 값으로 갱신 (복사 생성자 사용)
                                existingData.QuestProgressData[j] = new QuestStateUserData(existingQuest);
                            }
                        }
                    }

                    foreach (var quest in questList)
                    {
                        // 기존에 없는 KIND이면 추가
                        if (!existingData.QuestProgressData.Any(x => x != null && x.KIND == quest.KIND))
                        {
                            existingData.QuestProgressData.Add(new QuestStateUserData(quest));
                            existingData.TotalPoint += (int)quest.GetRewardAmount();
                            LogManager.Log($"[Quest {i}] 새 퀘스트 추가: KIND {quest.KIND}");
                        }
                    }
                }
            }
        }

        // 인벤토리
        if (src.Inventory != null && src.Inventory.Count > 0)
        {
            foreach (var kv in src.Inventory)
            {
                dst.Inventory.Add(kv.Key, kv.Value);
            }
        }

        return dst;
    }
}

// AES 암호화를 처리하는 정적 클래스
public static class SimpleEncryption
{
    // 16바이트(128비트) Key와 IV를 사용하여 AES 암호화/복호화
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("Happy_Happy_1d1p");
    private static readonly byte[] IV = Encoding.UTF8.GetBytes("Happy_Happy_1d1p");
    
    public static string Encrypt(string plainText)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Key;
            aesAlg.IV = IV;
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }
    }

    public static string Decrypt(string cipherText)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Key;
            aesAlg.IV = IV;
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
            try
            {
                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            catch (CryptographicException)
            {
                // 복호화 실패 시 (예: 데이터 손상, 키/IV 불일치, Base64 형식 오류)
                LogManager.LogErrorNoti("데이터 복호화에 실패했습니다. 데이터가 손상되었거나 변조되었을 수 있습니다.");
                return string.Empty; // 안전을 위해 빈 문자열 반환
            }
        }
    }
}

#endregion

public class UserData
{
    // 외부에서 new 금지
    private UserData()
    {
        UID = string.Empty;
        TokenID = string.Empty;
    }

    public enum eLoginType
    {
        None,
        Google,
        Guest,
        Editor,
    }

    // 유일한 인스턴스를 보관하는 private static 필드
    private static readonly UserData _instance = new UserData();

	// 외부에서 접근 가능한 프로퍼티
	public static UserData Instance => _instance;

    private string _uid;
    public string UID { get => _uid; 
        set 
        {
            _uid = value;

			if (_uid.IsNullOrWhiteSpace() == false)
				ScreenPresentationManager.Instance.LoadQueue();
		}
	}
    public string TokenID;

    private UserGameData _userGameData;

    // 데이터에 접근할 수 있는 공개 속성
    public UserGameData Data => _userGameData;

    // 데이터 변경 콜백
    public Action<long> GoldChangeCallback;
    public Action<long> StaminaChangeCallback;
    public Action<long> GemChangeCallback;
    public Action<eOutGameUpgradeToken, long> TokenChangeCallback;
    public Action<eWeaponType, eWeaponRarity> WeaponChangeCallback;

    // 테이블 데이터는 DataManager를 통해 가져온다고 가정
    private long MAX_STAMINA => DataManager.Instance.ConstData.STAMINA_MAX_COUNT;
    private long STAMINA_REGEN_TIME => DataManager.Instance.ConstData.STAMINA_REGEN_TIME;

    /// <summary>
    /// 스태미나 값을 가져올 때마다 회복 로직을 적용하는 속성
    /// </summary>
    public long CurrentStamina
    {
        get
        {
            RegenStamina();
            return _userGameData.Stamina;
        }
    }

    public string NickName => _userGameData.NickName;
    
    // Quest 변경 콜백 (Red Dot 자동 갱신용)
    public Action<eQuestProgressType> QuestChangeCallback;

    public eLoginType LoginType 
    {
        get
        {
            var type = PlayerPrefs.GetInt(DBManager.LoginType_KEY, 0);
            return (eLoginType)type;
        }
        set 
        {
            PlayerPrefs.SetInt(DBManager.LoginType_KEY, (int)value);
            PlayerPrefs.Save();
        }
    }

    public void OnLoadComplete(eLoadResult result, object data, Action<bool> onSuccess)
    {
        bool needsSave = false; // 로드 후 데이터를 수정/초기화하여 저장이 필요한지 여부
        bool loadSuccess = false; // 최종 로드 성공 여부

        switch (result)
        {
            case eLoadResult.Success:
                if (data is string encryptedData)
                {
					//NOTE : HYC 26-01-07 Legacy
					string json = SimpleEncryption.Decrypt(encryptedData);

                    if (string.IsNullOrEmpty(json))
                    {
                        LogManager.LogErrorNoti("복호화 실패 또는 데이터가 비어있습니다. 새로운 데이터로 초기화합니다.");
                        InitializeData();
                        needsSave = true;
                        loadSuccess = false; // 데이터 유실로 간주
                    }
                    else
                    {
                        try
                        {
                            var ser = JsonUtility.FromJson<UserGameDataSerializable>(json);
                            if (ser == null)
                            {
                                LogManager.LogErrorNoti($"파싱된 데이터가 유효하지 않거나 핵심 필드(UID)가 손상되었습니다. 새로운 데이터로 초기화합니다.{json}");
                                InitializeData();
                                needsSave = true;
                                loadSuccess = false; // 데이터 유실로 간주
                            }
                            else
                            {
                                _userGameData = UserGameDataConverter.FromSerializable(ser);

                                LogManager.Log($"NickName : {_userGameData.NickName}");
                                SetUserNickname(_userGameData.NickName);

                                // ContentGatingManager 먼저 초기화 (CheckData가 IsFeatureUnlocked 호출하기 전!)
                                ContentGatingManager.Instance.Initialize(forceReload: true);

                                if (CheckData()) // 데이터 무결성 검사
                                    needsSave = true;

                                loadSuccess = true; // 정상 로드
                            }
                        }
                        catch (Exception e)
                        {
                            LogManager.LogErrorNoti($"데이터 파싱 중 오류 발생: {e.Message}. 새로운 데이터로 초기화합니다.{json}");
                            InitializeData();
                            needsSave = true;
                            loadSuccess = false; // 데이터 유실로 간주
                        }
                    }
                }
                else if (data is Dictionary<string, object> dataMap)
                {
                    //NOTE : HYC 26-01-07 게스트 계정도 DB방식으로 통합됨
                    LogManager.Log("[DBLoad] 구글/에디터 로그인. Firestore Map 데이터 파싱...");
                    try
                    {
                        // UserGameData.ParseFromDictionary로 데이터 복원
                        _userGameData.ParseFromDictionary(dataMap);

                        // ContentGatingManager 먼저 초기화 (CheckData가 IsFeatureUnlocked 호출하기 전!)
                        ContentGatingManager.Instance.Initialize(forceReload: true);

                        if (CheckData()) // 데이터 무결성 검사 및 마이그레이션
                            needsSave = true;

                        loadSuccess = true; // 정상 로드
                    }
                    catch (Exception e)
                    {
                        LogManager.LogErrorNoti($"[Firestore] 데이터 파싱 중 오류 발생: {e.Message}.");
                        // 파싱 오류로 새로운 데이터 초기화 하면 기존 데이터 날라감.
                        // 그래서 일단 초기화 안하고 로드 실패 처리 후 다른 방법으로 처리 해야할듯.
                        //InitializeData();
                        //needsSave = true;
                        loadSuccess = false; // 데이터 유실로 간주
                    }
                }
                // 3. 알 수 없는 타입 (오류)
                else
                {
                    LogManager.LogErrorNoti($"[DBLoad] 알 수 없는 데이터 타입({data?.GetType()}) 수신. 새로운 데이터로 초기화합니다.");
                    InitializeData();
                    needsSave = true;
                    loadSuccess = true; // 일단 다음 스텝으로 넘어가게는 하자
                }
                break;

            case eLoadResult.NoData:
                LogManager.Log("저장된 데이터가 없습니다. 새로운 데이터로 초기화합니다.");
                InitializeData();
                needsSave = true; // 새 데이터 저장 필요
                loadSuccess = true; // 신규 유저, 정상 처리
                ResetData();
                break;

            case eLoadResult.Failed:
                //게임 이용 불가수준의 실패상황임.
                LogManager.LogErrorNoti("데이터 로드 중 오류 발생. 임시 데이터로 초기화합니다.");
                loadSuccess = false; // DB 로드 실패
                break;
        }

        if (needsSave)
            SaveUserData();

        onSuccess?.Invoke(loadSuccess);
    }

    /// <summary>
    /// 아웃게임 데이터를 기본값으로 초기화합니다.
    /// </summary>
    private void InitializeData()
    {
        _userGameData = new UserGameData
        {
            IsTerms = false,
            IsADConsent = false,
            SaveVersion = 0,
            Level = 0,
            Gold = 0,
            Gem = 0,
            TowerTicket = TableTowerContents.Instance.TowerContents_Cost_Max_Count,
            Stamina = 30, // 기본 스태미나 30으로 설정
            UpgradeLevels = new Dictionary<eOutGameDataType, int>(),
            UpgradeTokens = new Dictionary<eOutGameUpgradeToken, long>(),
            Weapons = new Dictionary<eWeaponType, eWeaponRarity>(),
            WeaponUpgradeLevels = new Dictionary<eWeaponType, int>(),
            StaminaLastUpdateTime = DateTime.Now.ToBinary(),
            TowerTicketLastUpdateTime = DateTime.Now.ToBinary(),
            SelectedProfileImageID = TableProfileInfo.Instance?.GetDefaultIDByType("Profile") ?? 0,
            SelectedFrameID = TableProfileInfo.Instance?.GetDefaultIDByType("Frame") ?? 0,
            UnlockedFeatures = new HashSet<int>(),
            GamePlaycount = 0,
            StageClearCount = new Dictionary<int, long>(),
            FlagOptions = new HashSet<int>(),
            SelectedChoiceWeapons = new HashSet<eWeaponType>(),
            QuestStateData = new Dictionary<eQuestProgressType, QuestStateData>(),
            PurchaseCount = new Dictionary<string, long>(),
            PurchaseHistory = new Dictionary<eShopPurchaseResetType, PurchaseHistoryData>(),
            CreatedDay = 0,
            LastLoginDay = 0,
            LoginCount = 0,
            TowerFloor = 1,
            TowerClear = new HashSet<int>(),
            TowerRewarded = new HashSet<int>(),
            ADSkip = false,
            LastPatrolTime = (long)GameMain.GameTicks_RealTimeSinceStartup,
            FastPatrolADCount = DataManager.Instance.ConstData.PATROL_FAST_PATROL_CAN_WATCH_AD,
            FastPatrolStaminaCount = DataManager.Instance.ConstData.PATROL_FAST_PATROL_CAN_RICEPT,
            CreatedTicks = 0,
            TowerSweepADCount = 0,
            TowerSweepRemainCount = 0,
			SingleGamePlayCount = 0,
            MultiGamePlayCount = 0,
			SystemMessage = string.Empty,
            Inventory = new Dictionary<long, long>(),
            ActiveItems = new HashSet<long>(),
        };

        // 모든 업그레이드 레벨을 0으로 초기화
        for (var type = eOutGameDataType.NONE + 1; type < eOutGameDataType.MAX; type++)
            _userGameData.UpgradeLevels[type] = 0;

        // 모든 토큰을 0으로 초기화
        for (var type = eOutGameUpgradeToken.None; type < eOutGameUpgradeToken.Max; type++)
            _userGameData.UpgradeTokens[type] = 0;

        // Base Weapon 초기화 (Sword ~ ShotGun)
        for (var type = eWeaponType.BaseWeaponStart + 1; type < eWeaponType.BaseWeaponEnd; type++)
        {
            // Sword만 기본 소유, 나머지는 Empty (구매 필요)
            _userGameData.Weapons.Add(type, type == eWeaponType.Sword ? eWeaponRarity.Uncommon : eWeaponRarity.None);
            // 업그레이드 레벨 0으로 초기화
            _userGameData.WeaponUpgradeLevels.Add(type, 0);
        }
        _userGameData.EquipWeapon = eWeaponType.Sword;

        // Choice Weapon 초기화
        for (var type = eWeaponType.ChoiceWeaponStart + 1; type < eWeaponType.ChoiceWeaponEnd; type++)
        {
            var featureData = TableFeatureGating.Instance?.GetWeaponFeatureByTarget((int)type);
            if (featureData != null && !ContentGatingManager.Instance.IsFeatureUnlocked(featureData.Feature_ID))
                _userGameData.Weapons.Add(type, eWeaponRarity.None);
            else
                _userGameData.Weapons.Add(type, eWeaponRarity.Uncommon); 
        }

        // 기본 선택 풀 자동 채움 (언락 상태와 무관, 초기엔 None로 간주) - 최대 8개
        AutoFillChoicePoolIfNeeded();

        // 현재 날짜에 맞게 스케줄 번호, 미션 셋팅
        for (var i = eQuestProgressType.NONE + 1; i < eQuestProgressType.MAX; ++i)
        {
            _userGameData.QuestStateData.Add(i, new QuestStateData(i));
        }
    }

    /// <summary>
    /// 로드된 데이터의 유효성을 검사하고, 문제가 발견되면 복구(재초기화)합니다.
    /// </summary>
    private bool CheckData()
    {
        // 딕셔너리들이 null인지 체크 (struct 기본값에 의해 발생 가능)
        bool dataModified = false;

        if (_userGameData.UpgradeLevels == null)
        {
            LogManager.LogWarning("[CheckData] UpgradeLevels가 null입니다. 초기화합니다.");
            _userGameData.UpgradeLevels = new Dictionary<eOutGameDataType, int>();
            dataModified = true;
        }

        if (_userGameData.SystemMessage == null)
        {
            _userGameData.SystemMessage = string.Empty;
            dataModified = true;
        }

        if (_userGameData.StageClearCount == null)
        {
            _userGameData.StageClearCount = new Dictionary<int, long>();
            dataModified = true;
        }

        Dictionary<eOutGameDataType, int> needFix = new Dictionary<eOutGameDataType, int>();

        if (_userGameData.UpgradeLevels != null)
        {
            foreach (var i in _userGameData.UpgradeLevels)
            {
                //예외처리. db 데이터 불러올 때 최대치 확인.
                var maxLevelData = TableOutGameDataInfo.Instance.Get(i.Key);
                if (maxLevelData != null && maxLevelData.LV_List != null)
                {
                    int maxLevel = maxLevelData.LV_List.Count;
                    if (i.Value > maxLevel)
                        needFix.Add(i.Key, maxLevel);
                }
            }
        }

        if (needFix.Count > 0)
        {
            dataModified = true;

            foreach (var i in needFix)
            {
                _userGameData.UpgradeLevels[i.Key] = i.Value;
            }
        }

        if (_userGameData.UpgradeTokens == null)
        {
            LogManager.LogWarning("[CheckData] UpgradeTokens가 null입니다. 초기화합니다.");
            _userGameData.UpgradeTokens = new Dictionary<eOutGameUpgradeToken, long>();
            dataModified = true;
        }
        if (_userGameData.Weapons == null)
        {
            LogManager.LogWarning("[CheckData] Weapons가 null입니다. 초기화합니다.");
            _userGameData.Weapons = new Dictionary<eWeaponType, eWeaponRarity>();
            dataModified = true;
        }

        //BaseWeaponStart == Sword였는데 인덱스 분리로 마이그레이션
        if (_userGameData.Weapons.ContainsKey(eWeaponType.BaseWeaponStart) && _userGameData.Weapons.ContainsKey(eWeaponType.Sword) == false)
        {
            _userGameData.Weapons.Add(eWeaponType.Sword, _userGameData.Weapons[eWeaponType.BaseWeaponStart]);
            _userGameData.Weapons.Remove(eWeaponType.BaseWeaponStart);
        }

        if (_userGameData.WeaponUpgradeLevels == null)
        {
            // 기존 유저들 업그레이드 데이터 없는 경우
            LogManager.LogWarning("[CheckData] WeaponUpgradeLevels is null !! 초기화합니다.");
            _userGameData.WeaponUpgradeLevels = new Dictionary<eWeaponType, int>();

            for (var type = eWeaponType.BaseWeaponStart + 1; type < eWeaponType.BaseWeaponEnd; type++)
            {
                _userGameData.WeaponUpgradeLevels.Add(type, 0);
            }
            dataModified = true;
        }
        else
        {
            // 업그레이드 데이터 있는데, 무기 추가된 경우
            for (var type = eWeaponType.BaseWeaponStart + 1; type < eWeaponType.BaseWeaponEnd; type++)
            {
                if (_userGameData.WeaponUpgradeLevels.ContainsKey(type) == false)
                    _userGameData.WeaponUpgradeLevels.Add(type, 0);

                dataModified = true;
            }
        }

        // --- 딕셔너리 내부 키 무결성 검사 (필수 키 누락 방지) ---

        // 업그레이드 레벨 키 무결성 검사
        for (var type = eOutGameDataType.NONE + 1; type < eOutGameDataType.MAX; type++)
        {
            if (_userGameData.UpgradeLevels.ContainsKey(type) == false)
            {
                _userGameData.UpgradeLevels[type] = 0;
                dataModified = true;
            }
        }

        // 업그레이드 토큰 키 무결성 검사
        for (var type = eOutGameUpgradeToken.None + 1; type < eOutGameUpgradeToken.Max; type++) // None 제외
        {
            if (_userGameData.UpgradeTokens.ContainsKey(type) == false)
            {
                _userGameData.UpgradeTokens[type] = 0;
                dataModified = true;
            }
        }

        // 무기 목록 무결성 검사 (기본 무기 및 선택 가능한 무기)
        // Base Weapon 초기화 (Sword ~ ShotGun)
        for (var type = eWeaponType.BaseWeaponStart + 1; type < eWeaponType.BaseWeaponEnd; type++)
        {
            if (_userGameData.Weapons.ContainsKey(type) == false)
            {
                // Sword만 기본 소유, 나머지는 Empty (구매 필요)
                _userGameData.Weapons.Add(type, type == eWeaponType.Sword ? eWeaponRarity.Uncommon : eWeaponRarity.None);
                dataModified = true;
            }
        }

        // Choice Weapon 초기화 (모두 None - Ungated, 기본 열림)
        for (var type = eWeaponType.ChoiceWeaponStart + 1; type < eWeaponType.ChoiceWeaponEnd; type++)
        {
            if (_userGameData.Weapons.ContainsKey(type) == false)
            {
                _userGameData.Weapons.Add(type, eWeaponRarity.Uncommon);
                dataModified = true;
            }
        }

        // 무기 Gating 검증: "Nothing should be unlocked over gated"
        int relockCount = 0;
        int recoveredCount = 0;

        // 긴급 복구: UnlockedFeatures에 있는데 무기가 Empty인 경우 복구
        foreach (var weaponKV in _userGameData.Weapons.ToList())
        {
            var weaponType = weaponKV.Key;
            var currentRarity = weaponKV.Value;
            int weaponID = (int)weaponType;

            var featureData = TableFeatureGating.Instance?.GetWeaponFeatureByTarget(weaponID);

            // Feature가 Unlocked인데 무기가 Empty이면 복구
            if (featureData != null && currentRarity == eWeaponRarity.None)
            {
                if (ContentGatingManager.Instance.IsFeatureUnlocked(featureData.Feature_ID))
                {
                    var weaponData = DataManager.Instance.GetWeaponData(weaponType);
                    if (weaponData != null)
                    {
                        _userGameData.Weapons[weaponType] = weaponData.Rarity;
                        recoveredCount++;
                        dataModified = true;
                    }
                }
            }
        }

        if (recoveredCount > 0)
            LogManager.LogWarning($"[WeaponGating] 무기 복구: {recoveredCount}개 (DB 손상 수정)");

        // Gating 검증 (재잠금)
        foreach (var weaponKV in _userGameData.Weapons.ToList())
        {
            var weaponType = weaponKV.Key;
            var currentRarity = weaponKV.Value;

            if (currentRarity == eWeaponRarity.None)
                continue;

            int weaponID = (int)weaponType;
            var featureData = TableFeatureGating.Instance?.GetWeaponFeatureByTarget(weaponID);

            if (featureData != null && !ContentGatingManager.Instance.IsFeatureUnlocked(featureData.Feature_ID))
            {
                _userGameData.Weapons[weaponType] = eWeaponRarity.None;
                relockCount++;
            }
        }

        if (relockCount > 0)
        {
            dataModified = true;
            LogManager.Log($"[WeaponGating] 무기 재잠금: {relockCount}개 (Feature 미언락)");
        }

        // SelectedChoiceWeapons 무결성 체크 및 정리
        if (_userGameData.SelectedChoiceWeapons == null)
        {
            _userGameData.SelectedChoiceWeapons = new HashSet<eWeaponType>();
            dataModified = true;
        }

        // SelectedChoiceWeapons Pool 정리 (Gated & 미언락 제거)
        int cleanCount = 0;
        foreach (var weaponType in _userGameData.SelectedChoiceWeapons.ToList())
        {
            int weaponID = (int)weaponType;
            var featureData = TableFeatureGating.Instance?.GetWeaponFeatureByTarget(weaponID);

            if (featureData != null && !ContentGatingManager.Instance.IsFeatureUnlocked(featureData.Feature_ID))
            {
                _userGameData.SelectedChoiceWeapons.Remove(weaponType);
                cleanCount++;
            }
        }

        if (_userGameData.SelectedChoiceWeapons.Contains(eWeaponType.ChoiceWeaponStart) && _userGameData.SelectedChoiceWeapons.Contains(eWeaponType.SolarPulse) == false)
        {
			_userGameData.SelectedChoiceWeapons.Remove(eWeaponType.ChoiceWeaponStart);
			_userGameData.SelectedChoiceWeapons.Add(eWeaponType.SolarPulse);
            dataModified = true;
		}

		if (cleanCount > 0)
            dataModified = true;

        // Pool 자동 채움
        if (AutoFillChoicePoolIfNeeded())
            dataModified = true;

        if (_userGameData.EquipWeapon == eWeaponType.None)
        {
            _userGameData.EquipWeapon = eWeaponType.Sword;
            dataModified = true;
        }

        // UnlockedFeatures 초기화 체크
        if (_userGameData.UnlockedFeatures == null)
        {
            LogManager.LogWarning("[CheckData] UnlockedFeatures가 null입니다. 초기화합니다.");
            _userGameData.UnlockedFeatures = new HashSet<int>();
            dataModified = true;
        }

        // 프로필 ID 기본값 체크 (구 계정 호환성)
        if (_userGameData.SelectedProfileImageID <= 0)
        {
            _userGameData.SelectedProfileImageID = TableProfileInfo.Instance?.GetDefaultIDByType("Profile") ?? 0;
            dataModified = true;
        }

        if (_userGameData.SelectedFrameID <= 0)
        {
            _userGameData.SelectedFrameID = TableProfileInfo.Instance?.GetDefaultIDByType("Frame") ?? 0;
            dataModified = true;
        }

        // HasPlayedFirstGame 기본값은 false이므로 별도 체크 불필요

        dataModified |= _userGameData.CheckQuestSchedule();

        dataModified |= _userGameData.CheckPurchaseHistorySchedule();

        dataModified |= _userGameData.CheckLoginSchedule();

        return dataModified;
    }
    
    public void ResetData()
    {
        // UID 이미 있을 경우 리턴함
        if (IsNullUID() == false)
            return;

        UID = DBManager.Instance.GUEST_UID;
    }

    public void SaveUserData(bool isForce = false)
    {
        DBManager.Instance.StartSaveData(isForce: isForce);
    }

    public void SetUserNickname(string nickname)
	{
        LogManager.Log($"[OutGameDataManager] SetNickName : {nickname}");
        _userGameData.NickName = nickname;
        PhotonNetwork.NickName = nickname;
    }

    public void GetOutGameData(PlayerData baseData, ref PlayerDataRunTime playerData)
    {
        foreach (var upgrade in _userGameData.UpgradeLevels)
        {
            var type = upgrade.Key;
            var level = upgrade.Value;
            var tableData = TableOutGameDataInfo.Instance.Get(type);

            if (tableData == null)
                continue;

            // 데이터 적용 로직
            ApplyData((int)tableData.GetValue(level), type, tableData.PARAM_TYPE, tableData.OPERATORS, ref playerData);
        }

        var levelUpEffects = TableOutGameDataInfo.Instance.LevelUpEffectTable.OrderBy(x => x.LEVEL).ToList();
        for (int i = 0; i < levelUpEffects.Count; i++)
        {
            if (levelUpEffects[i].LEVEL > _userGameData.Level)
                break;

            ApplyData((int)levelUpEffects[i].Value, levelUpEffects[i].TYPE, levelUpEffects[i].PARAM_TYPE, levelUpEffects[i].OPERATORS, ref playerData);
        }

        for (var weaponID = eWeaponType.BaseWeaponStart + 1; weaponID < eWeaponType.Max; weaponID++)
        {
            var equipEffectDatas = TableEquipUpgradeInfo.Instance.GetEquipEffectData(weaponID);
            if (equipEffectDatas != null && equipEffectDatas.Count > 0)
            {
                for (int i = 0; i < equipEffectDatas.Count; i++)
                {
                    var equipEffectData = equipEffectDatas[i];
                    eOutGameDataType dataType = eOutGameDataType.NONE;
                    switch (equipEffectData.upgradeType)
                    {
                        case eUpgradeType.MAX_HP_UP:
                            dataType = eOutGameDataType.MAX_HP_UP;
                            break;
                        case eUpgradeType.HP_REGEN_UP:
                            dataType = eOutGameDataType.HP_REGEN_UP;
                            break;
                        case eUpgradeType.DAMAGE_UP:
                            dataType = eOutGameDataType.DAMAGE_UP;
                            break;
                        case eUpgradeType.ATKSPEED_UP:
                            dataType = eOutGameDataType.ATK_SPEED_UP;
                            break;
                        case eUpgradeType.MOVE_SPEED_UP:
                            dataType = eOutGameDataType.MOVE_SPEED_UP;
                            break;
                        case eUpgradeType.DEF_UP:
                            dataType = eOutGameDataType.DEF_UP;
                            break;
                        case eUpgradeType.CRIT_CHANCE_UP:
                            dataType = eOutGameDataType.CRIT_CHANCE_UP;
                            break;
                        case eUpgradeType.CRIT_DAMAGE_UP:
                            dataType = eOutGameDataType.CRIT_DAMAGE_UP;
                            break;
                        case eUpgradeType.LUCK_UP:
                            dataType = eOutGameDataType.LUCK_UP;
                            break;
                        case eUpgradeType.EXP_UP:
                            dataType = eOutGameDataType.EXP_UP;
                            break;
                        case eUpgradeType.PICKUP_RAGE_UP:
                            dataType = eOutGameDataType.PICKUP_DISTANCE_UP;
                            break;
                        case eUpgradeType.RESURRECTION:
                            // TODO 부활 속도 증가 추가
                            break;

                        // 유물 효과는 일단 제외..
                        case eUpgradeType.SLOW_AND_STRONG:
                        case eUpgradeType.ABSORB_LIFE:
                        case eUpgradeType.MAX_HP_AND_ADD_DAMAGE:
                        case eUpgradeType.DECREASE_DAMAGE:
                        case eUpgradeType.LUCK_AND_LEVEL:
                        case eUpgradeType.STOP_AND_DAMAGE_UP:
                        case eUpgradeType.REWARD_UP:
                        case eUpgradeType.RESCUR_SPEED_UP:
                        case eUpgradeType.HURT_AND_SPEED_UP:
                        case eUpgradeType.POTION_EFFECT_UP:
                        case eUpgradeType.BOSS_DAMAGE_UP:
                        case eUpgradeType.AVOID_PROBABILITY:
                        case eUpgradeType.LUCK_AND_STRONG:
                            break;

                        // 얘내들은 PlayerWeapon클래스에서 적용됨
                        case eUpgradeType.None:
                        case eUpgradeType.Damage:
                        case eUpgradeType.AttackSpeed:
                        case eUpgradeType.Scale:
                        case eUpgradeType.ProjectileSpeed:
						case eUpgradeType.CriticalChance:
						case eUpgradeType.CriticalDamage:
						case eUpgradeType.Evolution:
                            break;
                    }

                    ApplyData((int)equipEffectData.value, dataType, equipEffectData.paramType, equipEffectData.operators, ref playerData);
                }
            }
        }

        return;
        void ApplyData(int value, eOutGameDataType type, PARAM_TYPE PARAM_TYPE, OPERATORS OPERATORS, ref PlayerDataRunTime playerData)
        {
            switch (type)
            {
                case eOutGameDataType.MAX_HP_UP:
                    long addedHP = (long)Define.GetUpgradeValue(value, PARAM_TYPE, OPERATORS, baseData.MaxHP);
                    playerData.MaxHP += addedHP;
                    playerData.NowHP += addedHP;
                    break;
                case eOutGameDataType.HP_REGEN_UP:
                    playerData.HPRegen += (long)Define.GetUpgradeValue(value, PARAM_TYPE, OPERATORS, baseData.HP_Regen);
                    break;
                case eOutGameDataType.DAMAGE_UP:
                    playerData.AttackDamage += Define.GetUpgradeValue(value, PARAM_TYPE, OPERATORS, baseData.AttackDamage);
                    break;
                case eOutGameDataType.ATK_SPEED_UP:
                    playerData.AttackSpeed += Define.GetUpgradeValue(value, PARAM_TYPE, OPERATORS, baseData.AttackSpeed);
                    break;
                case eOutGameDataType.MOVE_SPEED_UP:
                    playerData.Speed += Define.GetUpgradeValue(value, PARAM_TYPE, OPERATORS, baseData.Speed);
                    break;
                case eOutGameDataType.DEF_UP:
                    playerData.Def += (long)Define.GetUpgradeValue(value, PARAM_TYPE, OPERATORS, baseData.Def);
                    break;
                case eOutGameDataType.CRIT_CHANCE_UP:
                    playerData.Crit_Chance += Define.GetUpgradeValue(value, PARAM_TYPE, OPERATORS, baseData.Crit_Chance);
                    break;
                case eOutGameDataType.CRIT_DAMAGE_UP:
                    playerData.Crit_Damage += Define.GetUpgradeValue(value, PARAM_TYPE, OPERATORS, baseData.Crit_Damage);
                    break;
                case eOutGameDataType.LUCK_UP:
                    playerData.Luck += (long)Define.GetUpgradeValue(value, PARAM_TYPE, OPERATORS, baseData.Luck);
                    break;
                case eOutGameDataType.EXP_UP:
                    playerData.EXP_UP += Define.GetUpgradeValue(value, PARAM_TYPE, OPERATORS, baseData.EXP_UP);
                    break;
                case eOutGameDataType.REROLL_CHANCE_UP:
                    playerData.Reroll += (int)Define.GetUpgradeValue(value, PARAM_TYPE, OPERATORS, baseData.Reroll);
                    break;
                case eOutGameDataType.PICKUP_DISTANCE_UP:
                    playerData.PICKUP_RANGE += Define.GetUpgradeValue(value, PARAM_TYPE, OPERATORS, baseData.PICKUP_RANGE);
                    break;
                case eOutGameDataType.POTION_RECOVERY_UP:
                    playerData.PotionRecovery += Define.GetUpgradeValue(value, PARAM_TYPE, OPERATORS, baseData.PotionRecovery);
                    break;
                case eOutGameDataType.DEATH_COUNT_UP:
                    playerData.DeathCount += (int)Define.GetUpgradeValue(value, PARAM_TYPE, OPERATORS, baseData.DeathCount);
                    break;
                case eOutGameDataType.CHOICE_WEAPON_DAMAGE_UP:
                    // MULTIPLY 연산: 500 = 5% 증가, 누적 적용
                    playerData.ChoiceWeaponDamageBonus += Define.GetUpgradeValue(value, PARAM_TYPE, OPERATORS, (long)DataCalculator.DATA_SCALE);
                    break;
            }
        }
    }

    public int GetOutGameLevel(eOutGameDataType dataType)
    {
        if (_userGameData.UpgradeLevels == null || !_userGameData.UpgradeLevels.ContainsKey(dataType))
            return 0;
        return _userGameData.UpgradeLevels[dataType];
    }

    public long GetCurrentLuck()
    {
        // 데이터가 로드되기 전일 수 있으므로 기본값 0 반환
        if (IsNullUID())
            return 0;

        // 플레이어 기본 데이터 가져오기 (일단 1로 하드코딩..)
        var baseData = DataManager.Instance.GetPlayerData(1);
        if (baseData == null)
        {
            Debug.LogWarning($"[GetCurrentLuck] PlayerData not found for UID {1}, using 0 as base luck");
            return 0;
        }

        // 기본 LUCK 값
        long totalLuck = baseData.Luck;

        // 업그레이드로 인한 LUCK 증가 계산 (DB에서 로드된 값)
        int luckLevel = GetOutGameLevel(eOutGameDataType.LUCK_UP);
        var luckData = TableOutGameDataInfo.Instance.Get(eOutGameDataType.LUCK_UP);

        if (luckData != null && luckLevel > 0)
        {
            int upgradeValue = (int)luckData.GetValue(luckLevel);
            totalLuck += (long)Define.GetUpgradeValue(upgradeValue, luckData.PARAM_TYPE, luckData.OPERATORS, baseData.Luck);
        }

        return totalLuck;
    }

    public long GetOutGameUpgradeToken(eOutGameUpgradeToken tokenType)
    {
        if (_userGameData.UpgradeTokens == null || !_userGameData.UpgradeTokens.ContainsKey(tokenType))
            return 0;
        return _userGameData.UpgradeTokens[tokenType];
    }

    public eWeaponRarity GetOutGameWeaponRarity(int weaponID)
    {
        return GetOutGameWeaponRarity((eWeaponType)weaponID);
    }

    public eWeaponRarity GetOutGameWeaponRarity(eWeaponType weaponType)
    {
        if (_userGameData.Weapons == null || !_userGameData.Weapons.ContainsKey(weaponType))
            return eWeaponRarity.None;
        return _userGameData.Weapons[weaponType];
    }

    public int GetWeaponLevel(int weaponID)
    {
        return GetWeaponLevel((eWeaponType)weaponID);
    }

    public int GetWeaponLevel(eWeaponType weaponType)
    {
        if (_userGameData.WeaponUpgradeLevels == null || !_userGameData.WeaponUpgradeLevels.ContainsKey(weaponType))
            return 0;

        return _userGameData.WeaponUpgradeLevels[weaponType];
    }

    // Choice Weapon 선택 풀 최대 크기
    private const int MAX_CHOICE_POOL = 8;

    /// <summary>
    /// 선택 풀 자동 채움: 언락 가능한 ChoiceWeapon을 ID 순으로 MAX까지 채움
    /// Gated & 미언락 무기는 제외
    /// </summary>
    private bool AutoFillChoicePoolIfNeeded()
    {
        if (_userGameData.SelectedChoiceWeapons == null)
            _userGameData.SelectedChoiceWeapons = new HashSet<eWeaponType>();

        int before = _userGameData.SelectedChoiceWeapons.Count;

        if (_userGameData.SelectedChoiceWeapons.Count >= MAX_CHOICE_POOL)
            return false;

        for (var type = eWeaponType.ChoiceWeaponStart + 1; type < eWeaponType.ChoiceWeaponEnd; type++)
        {
            if (_userGameData.SelectedChoiceWeapons.Count >= MAX_CHOICE_POOL)
                break;

            if (_userGameData.SelectedChoiceWeapons.Contains(type))
                continue;

            // Gated & 미언락 무기 제외
            int weaponID = (int)type;
            var featureData = TableFeatureGating.Instance?.GetWeaponFeatureByTarget(weaponID);
            if (featureData != null && !ContentGatingManager.Instance.IsFeatureUnlocked(featureData.Feature_ID))
                continue;

            _userGameData.SelectedChoiceWeapons.Add(type);
        }

        return before != _userGameData.SelectedChoiceWeapons.Count;
    }

    public IReadOnlyCollection<eWeaponType> GetSelectedChoiceWeapons()
    {
        return _userGameData.SelectedChoiceWeapons ??= new HashSet<eWeaponType>();
    }

    /// <summary>
    /// 선택 풀 전체를 교체합니다 (에디트 UI의 일괄 저장용, AutoFill 트리거 안함)
    /// </summary>
    public void SetSelectedChoiceWeapons(HashSet<eWeaponType> newSelection)
    {
        if (_userGameData.SelectedChoiceWeapons == null)
            _userGameData.SelectedChoiceWeapons = new HashSet<eWeaponType>();

        _userGameData.SelectedChoiceWeapons.Clear();
        foreach (var wt in newSelection)
            _userGameData.SelectedChoiceWeapons.Add(wt);

        LogManager.Log($"[ChoicePool] 일괄 교체: {_userGameData.SelectedChoiceWeapons.Count}개");
    }

    public bool TrySetChoiceWeaponSelected(eWeaponType weaponType, bool isSelected)
    {
        if (_userGameData.SelectedChoiceWeapons == null)
            _userGameData.SelectedChoiceWeapons = new HashSet<eWeaponType>();

        bool changed = false;
        if (isSelected)
        {
            if (_userGameData.SelectedChoiceWeapons.Count >= MAX_CHOICE_POOL && _userGameData.SelectedChoiceWeapons.Contains(weaponType) == false)
                return false; // 용량 초과

            changed = _userGameData.SelectedChoiceWeapons.Add(weaponType);
        }
        else
        {
            changed = _userGameData.SelectedChoiceWeapons.Remove(weaponType);
        }

        if (changed)
        {
            LogManager.Log($"[ChoicePool] 변경: {weaponType} → {(isSelected ? "선택" : "해제")} (현재: {_userGameData.SelectedChoiceWeapons.Count}/{MAX_CHOICE_POOL})");
            SaveUserData();
        }

        return changed;
    }

    /// <summary>
    /// (훅) Choice 무기가 언락되었을 때 호출하여 선택 풀에 즉시 추가합니다.
    /// MAX 초과 시에는 추가하지 않습니다.
    /// </summary>
    public void OnChoiceWeaponUnlocked(eWeaponType weaponType)
    {
        if (weaponType <= eWeaponType.ChoiceWeaponStart || weaponType >= eWeaponType.ChoiceWeaponEnd)
            return;

        if (_userGameData.SelectedChoiceWeapons == null)
            _userGameData.SelectedChoiceWeapons = new HashSet<eWeaponType>();

        if (_userGameData.SelectedChoiceWeapons.Count >= MAX_CHOICE_POOL)
            return;

        if (_userGameData.SelectedChoiceWeapons.Add(weaponType))
            SaveUserData();
    }

    public int GetChoicePoolCount() => _userGameData.SelectedChoiceWeapons?.Count ?? 0;
    public int GetChoicePoolMax() => MAX_CHOICE_POOL;

    /// <summary>
    /// 언락/ungated된 Choice Weapon 개수를 반환합니다.
    /// Ungated 또는 Gated이지만 언락된 무기만 카운트합니다.
    /// </summary>
    public int GetAvailableChoiceWeaponCount()
    {
        int count = 0;

        for (var type = eWeaponType.ChoiceWeaponStart + 1; type < eWeaponType.ChoiceWeaponEnd; type++)
        {
            int weaponID = (int)type;
            var featureData = TableFeatureGating.Instance?.GetWeaponFeatureByTarget(weaponID);

            // Ungated이거나, Gated이지만 언락된 경우
            bool isAvailable = (featureData == null) ||
                              ContentGatingManager.Instance.IsFeatureUnlocked(featureData.Feature_ID);

            if (isAvailable)
                count++;
        }

        return count;
    }

    public eOutGameDataType GetRandomLevelUpDatType()
    {
        var levelUpList = TableOutGameDataInfo.Instance.GetNonMaxLevelOutGameDayType();
        var randomType = levelUpList.OrderBy(x => UnityEngine.Random.value).FirstOrDefault();
        if (randomType == eOutGameDataType.NONE)
        {
            LogManager.LogError($"[OutGameDataManager] GetRandomLevelUpIndex randomType is {randomType} !!");
            return randomType;
        }

        return randomType;
    }

    public bool LevelUpByGold(eOutGameDataType dataType)
    {
        return LevelUpByGoldWithGrade(dataType, eUpgradeGrade.NORMAL);
    }

    public bool LevelUpByGoldWithGrade(eOutGameDataType dataType, eUpgradeGrade grade)
    {
        if (!_userGameData.UpgradeLevels.ContainsKey(dataType))
            return false;

        var needGold = TableOutGameDataInfo.Instance.GetUpgradeCost(_userGameData.Level);
        if (needGold < 0)
            return false;

        if (_userGameData.Gold < needGold)
            return false;

        // 등급에 따른 증가량 결정
        int levelIncrease = grade switch
        {
            eUpgradeGrade.NORMAL => 1,
            eUpgradeGrade.RARE => 2,
            eUpgradeGrade.EPIC => 3,
            eUpgradeGrade.LEGEND => 4,
            _ => 1
        };

        // 개별 스탯 레벨도 같은 양만큼 증가
        if (_userGameData.UpgradeLevels.ContainsKey(dataType))
            _userGameData.UpgradeLevels[dataType] += levelIncrease;
        else
            _userGameData.UpgradeLevels[dataType] = levelIncrease;

        // 안전장치: 최대 레벨 초과 시 캡 적용
        var excessLevel = 0; // 초과 레벨
        var maxLevel = TableOutGameDataInfo.Instance.GetMaxLevel(dataType);
        if (_userGameData.UpgradeLevels[dataType] > maxLevel)
        {
            excessLevel = _userGameData.UpgradeLevels[dataType] - maxLevel;
            _userGameData.UpgradeLevels[dataType] = maxLevel; 
        }

        // 전체 플레이어 레벨도 같은 양만큼 증가 (모든 스탯 레벨의 합계)
        _userGameData.AddUpgradeLevel(levelIncrease - excessLevel);

        // 골드 차감 및 저장
        AddGold(-needGold);
        return true;
    }

    public bool LevelUpByIAA(eOutGameDataType dataType, eUpgradeGrade grade)
    {
        if (!_userGameData.UpgradeLevels.ContainsKey(dataType))
            return false;

        // 등급에 따른 증가량 결정
        int levelIncrease = grade switch
        {
            eUpgradeGrade.NORMAL => 1,
            eUpgradeGrade.RARE => 2,
            eUpgradeGrade.EPIC => 3,
            eUpgradeGrade.LEGEND => 4,
            _ => 1
        };

        // 전체 플레이어 레벨도 같은 양만큼 증가
        _userGameData.AddUpgradeLevel(levelIncrease);

        // 개별 스탯 레벨도 같은 양만큼 증가
        if (_userGameData.UpgradeLevels.ContainsKey(dataType))
            _userGameData.UpgradeLevels[dataType] += levelIncrease;
        else
            _userGameData.UpgradeLevels[dataType] = levelIncrease;

        // 안전장치: 최대 레벨 초과 시 캡 적용
        var maxLevelData = TableOutGameDataInfo.Instance.Get(dataType);
        if (maxLevelData != null && maxLevelData.LV_List != null)
        {
            int maxLevel = maxLevelData.LV_List.Count;
            if (_userGameData.UpgradeLevels[dataType] > maxLevel)
                _userGameData.UpgradeLevels[dataType] = maxLevel;
        }

        // IAA는 골드 차감 없음 (무료 업그레이드)
        return true;
    }

    public void WeaponLevelUp(int weaponID)
    {
        WeaponLevelUp((eWeaponType)weaponID);
    }

    public void WeaponLevelUp(eWeaponType weaponType)
    {
        if (_userGameData.WeaponUpgradeLevels == null)
            _userGameData.WeaponUpgradeLevels = new Dictionary<eWeaponType, int>();


        if (_userGameData.WeaponUpgradeLevels.ContainsKey(weaponType))
        {
            var maxLevel = TableEquipUpgradeInfo.Instance.GetMaxWeaponLevel();
            _userGameData.WeaponUpgradeLevels[weaponType] = Mathf.Min(maxLevel, _userGameData.WeaponUpgradeLevels[weaponType] + 1);
        }
        else
            _userGameData.WeaponUpgradeLevels.Add(weaponType, 1);
    }

    public eWeaponType GetEquipWeapon()
    {
        return Data.EquipWeapon;
    }

    public int GetEquipWeaponLevel()
    {
        var weaponType = GetEquipWeapon();
        return GetWeaponLevel(weaponType);
    }

    public long GetStaminaRegenRemainTime()
    {
        // 이미 최대 스태미나일 경우 0 반환
        if (_userGameData.Stamina >= MAX_STAMINA)
            return 0;

        long lastUpdateTimeBinary = _userGameData.StaminaLastUpdateTime;
        DateTime lastUpdateTime = DateTime.FromBinary(lastUpdateTimeBinary);

        TimeSpan timePassed = DateTime.Now - lastUpdateTime;

        long recoveredStaminaCount = (long)(timePassed.TotalSeconds / STAMINA_REGEN_TIME);

        // 회복할 스태미나가 있다면 RegenStamina()를 호출
        if (recoveredStaminaCount > 0)
        {
            // 이 호출 내에서 스태미나 값과 StaminaLastUpdateTime이 갱신됩니다.
            RegenStamina();

            // RegenStamina() 호출 후 최대 스태미나가 되었는지 다시 확인합니다.
            if (_userGameData.Stamina >= MAX_STAMINA)
                return 0;

            // RegenStamina()가 시간을 갱신했으므로, timePassed와 lastUpdateTime을 새로 계산합니다.
            lastUpdateTimeBinary = _userGameData.StaminaLastUpdateTime;
            lastUpdateTime = DateTime.FromBinary(lastUpdateTimeBinary);
            timePassed = DateTime.Now - lastUpdateTime;
        }

        // 다음 스태미나 회복하는 데 필요한 남은 시간을 계산
        long remainingSeconds = STAMINA_REGEN_TIME - (long)timePassed.TotalSeconds % STAMINA_REGEN_TIME;

        if (remainingSeconds < 0)
            return 0;

        return remainingSeconds;
    }

    public bool CanUpgradeWeapon(int weaponID)
    {
        return CanUpgradeWeapon((eWeaponType)weaponID);
    }

    public bool CanUpgradeWeapon(eWeaponType weaponType)
    {
        if (_userGameData.Weapons == null || !_userGameData.Weapons.ContainsKey(weaponType))
            return false;
        return _userGameData.Weapons[weaponType] != eWeaponRarity.Legendary;
    }

    public bool CanLevelUpWeapon(int weaponID, bool isNoti = false)
    {
        return CanLevelUpWeapon((eWeaponType)weaponID, isNoti);
    }

    public bool CanLevelUpWeapon(eWeaponType weaponType, bool isNoti = false)
    {
        int currentLevel = GetWeaponLevel(weaponType);
        if (TableEquipUpgradeInfo.Instance.IsWeaponMaxLevel(currentLevel))
            return false; 

        var limitGrade = TableEquipUpgradeInfo.Instance.GetNextLevelGradeLimit((int)weaponType);
        var currentRarity = GetOutGameWeaponRarity(weaponType);

        // 현재 등급보다 제한 등급이 더 높다
        if (currentRarity < limitGrade)
        {
            if (isNoti)
            {
                string message = TableTextKey.Instance.GetText("TOAST_GEAR_ENHANCE_FAILURE_RARITY");
                ToastManager.Instance.ShowWarning(message);
            }
            return false; 
        }

        return true;
    }

    public bool CanUnlockWeapon(eWeaponType weaponType)
    {
        var weaponData = DataManager.Instance.GetWeaponData(weaponType);
        if (weaponData == null)
            return false;

        var currentRarity = GetOutGameWeaponRarity(weaponType);
        if (currentRarity != eWeaponRarity.None)
            return false;

        int weaponID = (int)weaponType;

        // TableFeatureGating에서 조건 데이터 가져오기 (WeaponID로 검색)
        var featureData = TableFeatureGating.Instance?.GetWeaponFeatureByTarget(weaponID);
        if (featureData == null)
            return true; // 데이터 없으면 기본 열림

        // ContentGatingManager로 언락 여부 확인
        if (ContentGatingManager.Instance.IsFeatureUnlocked(featureData.Feature_ID))
            return true;

        // 조건 충족 여부 체크
        switch (featureData.Unlock_Type)
        {
            case eUnlockCondition.None:
                return true;
            case eUnlockCondition.Gold:
                return _userGameData.Gold >= featureData.Unlock_Value;
            case eUnlockCondition.Gem:
                return _userGameData.Gem >= featureData.Unlock_Value;
            case eUnlockCondition.Quest:
                return IsQuestCompleted(featureData.Unlock_Value);
            case eUnlockCondition.Stage:
            case eUnlockCondition.UpgradeLevel:
            case eUnlockCondition.Achievement:
            case eUnlockCondition.GamePlay:
            case eUnlockCondition.StageClearCount:
                // 다른 조건들은 FeatureGatingData.IsUnlockConditionMet()에서 체크
                return featureData.IsUnlockConditionMet();
            default:
                return false;
        }
    }

    private bool IsQuestCompleted(int questID)
    {
        if (_userGameData.QuestStateData == null)
            return false;

        foreach (var questState in _userGameData.QuestStateData.Values)
        {
            if (questState.QuestProgressData == null)
                continue;

            foreach (var userQuest in questState.QuestProgressData)
            {
                if (userQuest.KIND == questID)
                    return userQuest.State == eQuestState.Rewarded;
            }
        }

        return false;
    }

    private void RegenStamina()
    {
        // 데이터가 아직 로드되지 않았을 수 있음
        if (IsNullUID())
            return;

        long currentStamina = _userGameData.Stamina;
        if (currentStamina >= MAX_STAMINA)
            return;

        long lastUpdateTimeBinary = _userGameData.StaminaLastUpdateTime;
        DateTime lastUpdateTime = DateTime.FromBinary(lastUpdateTimeBinary);
        TimeSpan timePassed = DateTime.Now - lastUpdateTime;

        long recoveredStamina = (long)(timePassed.TotalSeconds / STAMINA_REGEN_TIME);

        if (recoveredStamina > 0)
        {
            long newStamina = Math.Min(currentStamina + recoveredStamina, MAX_STAMINA);

            // 스태미나가 실제로 변경되었을 때만 갱신 및 저장
            if (currentStamina != newStamina)
            {
                _userGameData.Stamina = newStamina;
                StaminaChangeCallback?.Invoke(_userGameData.Stamina);

                // 회복된 시간을 제외한 나머지 시간을 기준으로 마지막 업데이트 시간 갱신
                long remainingSecondsAfterRecovery = (long)timePassed.TotalSeconds % STAMINA_REGEN_TIME;
                _userGameData.StaminaLastUpdateTime = (DateTime.Now - TimeSpan.FromSeconds(remainingSecondsAfterRecovery)).ToBinary();

                SaveUserData();
            }
        }
    }

    public static bool TryConvertTokenItemId(long itemId, out eOutGameUpgradeToken tokenType)
    {
        switch (itemId)
        {
            case Define.TOKEN_NORMAL:
                tokenType = eOutGameUpgradeToken.Normal;
                return true;
            case Define.TOKEN_RARE:
                tokenType = eOutGameUpgradeToken.Rare;
                return true;
            case Define.TOKEN_EPIC:
                tokenType = eOutGameUpgradeToken.Epic;
                return true;
            case Define.TOKEN_LEGENDARY:
                tokenType = eOutGameUpgradeToken.Legend;
                return true;
            default:
                tokenType = eOutGameUpgradeToken.None;
                return false;
        }
    }

    public static bool TryConvertItemIdToken(eOutGameUpgradeToken tokenType, out long itemId)
    {
        itemId = 0;
        switch (tokenType)
        {
            case eOutGameUpgradeToken.Normal:
                itemId = Define.TOKEN_NORMAL;
                return true;
            case eOutGameUpgradeToken.Rare:
                itemId = Define.TOKEN_RARE;
                return true;
            case eOutGameUpgradeToken.Epic:
                itemId = Define.TOKEN_EPIC;
                return true;
            case eOutGameUpgradeToken.Legend:
                itemId = Define.TOKEN_LEGENDARY;
                return true;
            default:
                itemId = 0;
                return false;
        }
    }

    public bool CanUpgradeWeaponNow(eWeaponType weaponType)
    {
        if (!CanUpgradeWeapon(weaponType))
            return false;

        var currentRarity = GetOutGameWeaponRarity(weaponType);
        var upgradeGold = TableEquipUpgradeInfo.Instance.GetCostUpgradeGold(currentRarity);
        var upgradeItem = TableEquipUpgradeInfo.Instance.GetCostUpgradeItem(currentRarity);

        if (_userGameData.Gold < upgradeGold)
            return false;

        if (upgradeItem.ItemID > 0)
        {
            if (!TryConvertTokenItemId(upgradeItem.ItemID, out var tokenType))
                return false;

            var ownedCount = GetOutGameUpgradeToken(tokenType);
            if (ownedCount < upgradeItem.Amount)
                return false;
        }

        return true;
    }

    public bool IsEquippedWeapon(int weaponID)
    {
        return IsEquippedWeapon((eWeaponType)weaponID);
    }

    public bool IsEquippedWeapon(eWeaponType weaponType)
    {
        return _userGameData.EquipWeapon == weaponType;
    }

    public bool IsCanUseStamina(long amount)
    {
        return CurrentStamina >= amount;
    }

    public bool IsCanUseTowerTicket(long amount)
    {
        return _userGameData.TowerTicket >= amount;
    }

    public bool TryUseStamina(long amount)
    {
        if (CurrentStamina >= amount)
        {
            long oldStamina = _userGameData.Stamina;
            _userGameData.Stamina -= amount;
            OnQuestProgress(eQuestType.USE_STAMIA, eQuestParamType.Add, amount);

            // 스태미나를 사용한 후, MAX_STAMINA 이상이었다가 미만이 되었을 때만 업데이트 시간을 갱신하여 회복 타이머를 시작합니다.
            if (oldStamina >= MAX_STAMINA && _userGameData.Stamina < MAX_STAMINA)
            {
                _userGameData.StaminaLastUpdateTime = DateTime.Now.ToBinary();
            }

            StaminaChangeCallback?.Invoke(_userGameData.Stamina);
            SaveUserData();
            return true;
        }
        else
            return false;
    }

    public void AddGold(long amount)
    {
        // TODO KSH 암호화? (이미 Save/Load에서 암호화되므로 개별 필드 암호화는 불필요)
        if (amount < 0)
        {
            long afterGold = _userGameData.Gold + amount;
            if (afterGold < 0)
                amount -= afterGold;

			OnQuestProgress(eQuestType.USE_GOLD, eQuestParamType.Add, Math.Abs(amount));
        }

        _userGameData.Gold += amount;
        _userGameData.Gold = Math.Max(0, _userGameData.Gold); // 골드가 음수가 되지 않도록 방지
        GoldChangeCallback?.Invoke(_userGameData.Gold);

        SaveUserData();
    }

    public void AddStamina(long amount, bool ignoreMax)
    {
        long oldStamina = _userGameData.Stamina;
        if (ignoreMax == false)
        {
            if (_userGameData.Stamina < MAX_STAMINA)
                _userGameData.Stamina = Math.Min(_userGameData.Stamina + amount, MAX_STAMINA); // 최대치 초과 방지
        }
        else
        {
            _userGameData.Stamina += amount;
        }

        _userGameData.Stamina = Math.Max(0, _userGameData.Stamina);

        if (oldStamina < MAX_STAMINA && _userGameData.Stamina >= MAX_STAMINA)
        {
			// 스태미나가 최대치 미만이었다가 이상이 되는 순간, 마지막 업데이트 시간을 현재로 설정하여 회복 타이머를 리셋(중지)합니다.
			_userGameData.StaminaLastUpdateTime = DateTime.Now.ToBinary();
        }
        else if (oldStamina >= MAX_STAMINA && _userGameData.Stamina < MAX_STAMINA)
        {
			// 스태미나가 최대치 이상이였다가 미만이 되는 순간, 마지막 업데이트 시간을 현재로 설정하여 회복 타이머를 시작합니다.
			_userGameData.StaminaLastUpdateTime = DateTime.Now.ToBinary();
        }

		StaminaChangeCallback?.Invoke(_userGameData.Stamina);

        SaveUserData();
    }

    public void AddGem(long amount)
    {
        _userGameData.Gem += amount;
        _userGameData.Gem = Math.Max(0, _userGameData.Gem);
        GemChangeCallback?.Invoke(_userGameData.Gem);

        SaveUserData();
    }

    public void AddToken(eOutGameUpgradeToken tokenType, long amount)
    {
        if (_userGameData.UpgradeTokens.ContainsKey(tokenType) == false)
            _userGameData.UpgradeTokens[tokenType] = 0;

        _userGameData.UpgradeTokens[tokenType] += amount;
        _userGameData.UpgradeTokens[tokenType] = Math.Max(0, _userGameData.UpgradeTokens[tokenType]);
        TokenChangeCallback?.Invoke(tokenType, _userGameData.UpgradeTokens[tokenType]);
        SaveUserData();
    }

    public void AddItem(Item item)
    {
        AddItem(item.ItemID, item.Amount);
    }

    public void AddItem(long itemID, long itemAmount)
    {
        if (_userGameData.Inventory == null)
            _userGameData.Inventory = new Dictionary<long, long>();

        if (_userGameData.Inventory.ContainsKey(itemID) == false)
        {
            // Item이 0개 -> N개로 바뀔때 가능하면 활성화 해준다.
            if (GetActiveItemCount() < Define.MAX_ACTIVE_CONSUMABLE_COUNT)
                AddActivetem(itemID);

            _userGameData.Inventory.Add(itemID, itemAmount);
        }
        else
        {
            if (GetActiveItemCount() < Define.MAX_ACTIVE_CONSUMABLE_COUNT && _userGameData.Inventory[itemID] <= 0)
                AddActivetem(itemID);

            _userGameData.Inventory[itemID] += itemAmount; 
        }

        SaveUserData();
    }

    public void AddTowerReward(long floor)
    {
        if (_userGameData.TowerRewarded == null)
            _userGameData.TowerRewarded = new HashSet<int>();

        _userGameData.TowerRewarded.Add((int)floor);
        // 보상 받았으면, 다음 층으로 이동
        _userGameData.TowerFloor++;
        var maxFloor = TableTowerContents.Instance.GetMaxFloor();
        if (_userGameData.TowerFloor > maxFloor)
            _userGameData.TowerFloor = maxFloor;

        SaveUserData();
    }

    public void AddTowerClear(int floor)
    {
        if (_userGameData.TowerClear == null)
            _userGameData.TowerClear = new HashSet<int>();

        _userGameData.TowerClear.Add(floor);
        SaveUserData();
    }

    public void AddTowerTicket(long amount)
    {
		if (amount < 0)
		{
			long afterTicket = _userGameData.TowerTicket + amount;
			if (afterTicket < 0)
				amount -= afterTicket;

			OnQuestProgress(eQuestType.USE_TICKET, eQuestParamType.Add, Math.Abs(amount));
		}

		_userGameData.TowerTicket += amount;
        _userGameData.TowerTicket = Math.Max(0, _userGameData.TowerTicket);
        SaveUserData();
    }

    public void UnLockWeapon(int weaponID)
    {
        UnLockWeapon((eWeaponType)weaponID);
    }

    public void UnLockWeapon(eWeaponType type)
    {
        var weaponData = DataManager.Instance.GetWeaponData(type);
        if (weaponData == null)
        {
            LogManager.LogError($"[OutGameDataManager] WeaponData is Null !! type : {type}");
            return;
        }

        // 이미 소유한 무기인지 확인 (Empty는 미소유로 간주)
        if (_userGameData.Weapons.ContainsKey(type) && _userGameData.Weapons[type] != eWeaponRarity.None)
        {
            LogManager.LogWarning($"[OutGameDataManager] 무기 이미 소유 중: {type}, Rarity={_userGameData.Weapons[type]}");
            return;
        }

        // 무기 태생 등급으로 추가 (이미 Empty로 존재하면 업데이트)
        if (_userGameData.Weapons.ContainsKey(type))
            _userGameData.Weapons[type] = weaponData.Rarity;
        else
            _userGameData.Weapons.Add(type, weaponData.Rarity);

        WeaponChangeCallback?.Invoke(type, weaponData.Rarity);
        SaveUserData();
    }

    public void EquipWeapon(int weaponID)
    {
        EquipWeapon((eWeaponType)weaponID);
        _userGameData.SetFlagOption(eFlagOptions.FIRST_WEAPON_EQUIP);
    }

    public void EquipWeapon(eWeaponType type)
    {
        // Only base weapons (ID < BaseWeaponEnd) can be equipped
        if (type >= eWeaponType.BaseWeaponEnd)
        {
            LogManager.LogError($"[OutGameDataManager] EquipWeapon Error !! {type} is not a base weapon. Only base weapons can be equipped.");
            return;
        }

        _userGameData.EquipWeapon = type;

        var dlg = UIFormManager.Instance.FindUIForm<UI_SCREEN_LOBBY>();
        if (dlg)
            dlg.UpdatePlayerLobbyInfo();

        SaveUserData();
    }

    public void UpgradeWeapon(int weaponID)
    {
        UpgradeWeapon((eWeaponType)weaponID);
    }

    public void UpgradeWeapon(eWeaponType type)
    {
        if (_userGameData.Weapons.ContainsKey(type) == false)
        {
            LogManager.LogError($"[OutGameDataManager] {type} Type Not Found Error !!");
            return;
        }

        if (_userGameData.Weapons[type] == eWeaponRarity.Legendary)
        {
            LogManager.LogError($"[OutGameDataManager] UpgradeWeapon Error !! {type} is MaxLevel !!");
            return;
        }

        PlatformManager.Instance.SendLog_Upgrade(type.ToString(), _userGameData.Weapons[type].ToString(), (_userGameData.Weapons[type] + 1).ToString());
        _userGameData.Weapons[type]++;
        WeaponChangeCallback?.Invoke(type, _userGameData.Weapons[type]);
        SaveUserData();
    }

    public void SetProfileImage(int profileImageID)
    {
        _userGameData.SelectedProfileImageID = profileImageID;
        SaveUserData();
    }

    public void SetProfileFrame(int frameID)
    {
        _userGameData.SelectedFrameID = frameID;
        SaveUserData();
    }

    public void SetUnlockedFeatures(HashSet<int> unlockedFeatures)
    {
        _userGameData.UnlockedFeatures = unlockedFeatures;
        SaveUserData();
    }

    public void AddGamePlayCount(int stageID, bool isWin, bool isMulti)
    {
		_userGameData.GamePlaycount++;
        if (_userGameData.GamePlaycount >= 1 && _userGameData.CreatedDay == 0)
        {
            PlatformManager.Instance.SendLog(PlatformManager.TUTORIAL_COMPLETE, "tutorial_step", 1.ToString());
            _userGameData.CreatedDay = TableQuestInfo.Instance.GetCurrentScheduleNumber((long)GameMain.GameTicks_RealTimeSinceStartup, 1, 0, 0);
			_userGameData.CreatedTicks = (long)GameMain.GameTicks_RealTimeSinceStartup;
		}

        if (isWin)
        {
            if (_userGameData.StageClearCount != null)
            {
                if (_userGameData.StageClearCount.ContainsKey(stageID) == false)
                    _userGameData.StageClearCount.Add(stageID, 0);

                _userGameData.StageClearCount[stageID]++;
            }
        }

        if(isMulti)
            _userGameData.MultiGamePlayCount++;
        else
            _userGameData.SingleGamePlayCount++;

		SaveUserData();
    }

    public void SaveFriendID()
    {
        if (AuthenticationService.Instance != null && AuthenticationService.Instance.PlayerId.IsNullOrWhiteSpace() == false)
        {
            string friendID = AuthenticationService.Instance.PlayerId;

            //nickName 테이블에 저장. 유저 데이터에 저장하는게 낫긴한데.. 게스트는 user테이블에 저장을 안해서..
            var result = new Dictionary<string, object>
            {
                { DBManager.SAVE_KEY_UID, UID },
                { DBManager.SAVE_KEY_NICKNAME_FRIENDID, friendID },
            };

            DBManager.Instance.StartSaveData(NickName, DBManager.SAVE_KEY_NICKNAME, result, null);

            //Friend 테이블에 저장
            UserFriendData friendData = new UserFriendData();
            friendData.UID = UID;
            friendData.Nickname = NickName;
            DBManager.Instance.StartSaveData(friendID, DBManager.SAVE_KEY_FRIEND, friendData.ToDictionary(), null);
        }
    }

    /// <summary> 단일 아이템 보상처리 </summary>
    public void AddItem(long itemID, long amount, bool isAD)
    {
        var item = TableItemInfo.Instance.Get(itemID);
        if (item != null)
        {
            if (isAD)
                PlatformManager.Instance.SendLog_ADComplete(itemID.ToString());

            List<Item> addedItem = new List<Item>();

            if (item.TYPE == eItemType.Pacakage)
            {
                for (int i = 0; i < amount; ++i)
                {
                    var packageData = TablePackageInfo.Instance.Get(itemID);
                    if (packageData != null)
                        addedItem.AddRange(packageData.Rewards);
                }
            }
            else
            {
                //단일 아이템
                addedItem.Add(new Item(itemID, amount));
            }

            if (addedItem.Count > 0)
                AddItemOnDB(addedItem);
        }
    }

    /// <summary> 2개이상 보상 일괄처리 </summary>
    public void AddItem(List<Item> itemIDs, bool rewardUI = true)
    {
        if (itemIDs == null || itemIDs.Count == 0)
            return;

        List<Item> addedItem = new List<Item>();
        foreach (var i in itemIDs)
        {
            if (i == null)
                continue;

            //음..이경우는 일단 광고일리는 없을거라 광고 로그를 체크하지는 않음. 광고보고 일괄수령 이런 기능이 없음
            var item = TableItemInfo.Instance.Get(i.ItemID);
            if (item != null && i.Amount > 0)
            {
                if (item.TYPE == eItemType.Pacakage)
                {
                    for (int j = 0; j < i.Amount; ++j)
                    {
                        var packageData = TablePackageInfo.Instance.Get(item.ItemID);
                        if (packageData != null)
                            addedItem.AddRange(packageData.Rewards);
                    }
                }
                else
                {
                    //단일 아이템
                    addedItem.Add(new Item(item.ItemID, i.Amount));
                }
            }
        }

        if (addedItem.Count > 0)
            AddItemOnDB(addedItem, rewardUI);
    }

    /// <summary> 실질적 보상 데이터 db에 저장, 획득 연출 </summary>
    private void AddItemOnDB(List<Item> itemIDs, bool rewardUI = true)
    {
        if (itemIDs == null || itemIDs.Count == 0)
            return;

        //습득 연출용
        List<Item> addedItem = new List<Item>();
        foreach (var i in itemIDs)
        {
            if (i == null)
                continue;

            var item = TableItemInfo.Instance.Get(i.ItemID);
            if (item != null)
            {
                if (i.Amount > 0)
                    addedItem.Add(i);

                switch (item.TYPE)
                {
                    case eItemType.Normal:
                        break;

                    case eItemType.Currency:
                        switch (i.ItemID)
                        {
                            case Define.STAMINA_ID:
                                AddStamina(i.Amount, true);
                                break;
                            case Define.GOLD_ID:
                                AddGold(i.Amount);
                                break;
                            case Define.GEM_ID:
                                AddGem(i.Amount);
                                break;
                            case Define.TOWERTICKET_ID:
                                AddTowerTicket(i.Amount);
                                break;
                            case Define.GEARDOUBLE_ID:
                                AddItem(i.ItemID, i.Amount);
                                break;
                            default:
                                LogManager.LogError($"OutGameDataManager AddItem Error Invalid Currency : {i.ItemID}");
                                break;
                        }
                        break;

                    case eItemType.Equipment:
						UnLockWeapon((int)i.ItemID);
						break;

                    case eItemType.Token:
                        switch (i.ItemID)
                        {
                            case Define.TOKEN_NORMAL:
                                AddToken(eOutGameUpgradeToken.Normal, i.Amount);
                                break;
                            case Define.TOKEN_RARE:
                                AddToken(eOutGameUpgradeToken.Rare, i.Amount);
                                break;
                            case Define.TOKEN_EPIC:
                                AddToken(eOutGameUpgradeToken.Epic, i.Amount);
                                break;
                            case Define.TOKEN_LEGENDARY:
                                AddToken(eOutGameUpgradeToken.Legend, i.Amount);
                                break;

                            //case Define.TOKEN_SICKLE_NORMAL:
                            //case Define.TOKEN_SICKLE_RARE:
                            //case Define.TOKEN_SICKLE_EPIC:
                            //case Define.TOKEN_SICKLE_LEGENDARY:
                            //case Define.TOKEN_TOYGUN_NORMAL:
                            //case Define.TOKEN_TOYGUN_RARE:
                            //case Define.TOKEN_TOYGUN_EPIC:
                            //case Define.TOKEN_TOYGUN_LEGENDARY:
                            //case Define.TOKEN_BOOMERANG_NORMAL:
                            //case Define.TOKEN_BOOMERANG_RARE:
                            //case Define.TOKEN_BOOMERANG_EPIC:
                            //case Define.TOKEN_BOOMERANG_LEGENDARY:
                            //case Define.TOKEN_SHOTGUN_NORMAL:
                            //case Define.TOKEN_SHOTGUN_RARE:
                            //case Define.TOKEN_SHOTGUN_EPIC:
                            //case Define.TOKEN_SHOTGUN_LEGENDARY:
                            //case Define.TOKEN_KUNAI_NORMAL:
                            //case Define.TOKEN_KUNAI_RARE:
                            //case Define.TOKEN_KUNAI_EPIC:
                            //case Define.TOKEN_KUNAI_LEGENDARY:
                            //case Define.TOKEN_SNIPERRIFLE_NORMAL:
                            //case Define.TOKEN_SNIPERRIFLE_RARE:
                            //case Define.TOKEN_SNIPERRIFLE_EPIC:
                            //case Define.TOKEN_SNIPERRIFLE_LEGENDARY:
                            //    break;

                            default:
                                AddItem(i.ItemID, i.Amount);
                                //LogManager.LogError($"OutGameDataManager AddItem Error Invalid Token : {i.ItemID}");
                                break;
                        }
                        break;
                    case eItemType.ADSkip:
                        if (_userGameData.ADSkip)
                        {
                            //치명적 버그. 이미 보유했으면 다시 살 수 없어야함
                            LogManager.LogErrorNoti("Already Have ADSkip Item");
                        }
                        else
                        {
                            _userGameData.ADSkip = true;
                        }
                        break;

                    case eItemType.Consumable:
                    default:
                        // LogManager.LogError($"OutGameDataManager AddItem Error Invalid Type : {item.TYPE}");
                        AddItem(i.ItemID, i.Amount);
                        break;
                }
            }
        }

        if (addedItem.Count > 0)
        {
            if (rewardUI)
            {
                ScreenPresentationManager.RewardPresentationData data = new ScreenPresentationManager.RewardPresentationData();
                foreach (var i in addedItem)
                {
                    if (i != null)
                        data.Add(new Item(i.ItemID, i.Amount));
                }
                ScreenPresentationManager.Instance.EnqueueFromReward(data);
            }

			SaveUserData(true);
            DispatchHandler.Dispatch(DispatchHandler.UPDATE_CURRENCY);
        }
    }

    public QuestStateData GetQuestStateData(eQuestProgressType progressType)
    {
        if (_userGameData.QuestStateData != null &&
            _userGameData.QuestStateData.ContainsKey(progressType))
        {
            return _userGameData.QuestStateData[progressType];
        }

        return null;
    }

    public void ResetQuestHoldFlag()
    {
        _userGameData.ResetQuestHoldFlag();
    }

    public void OnQuestProgress(eQuestType questType, eQuestParamType paramType, long value, long param = 0)
    {
        _userGameData.OnQuestProgress(questType, paramType, value, param);
    }

    public bool CheckQuestSchedule()
    {
        return _userGameData.CheckQuestSchedule();
    }

    public bool CheckPurchaseHistorySchedule()
    {
        return _userGameData.CheckPurchaseHistorySchedule();
    }

    public bool CheckLoginSchedule()
    {
        return _userGameData.CheckLoginSchedule();
    }

	/// <summary> 비정상 종료에 의한 스태미너 환급 확인 </summary>
	public void CheckStaminaForANR()
    {
		if (long.TryParse(PlayerPrefs.GetString(Define_PlayerPrefs.GAME_ENTER_FLAG, string.Empty), out long usedStamina))
		{
			if (usedStamina > 0)
			{
				//플래그값이 0이 아닌 상태로 로비에 들어온거면 비정상 종료 후 재접속이라고 판단. 스태미너 다시 돌려준다. PlayerPrefs값은 마지막에 사용한 스태미너
				UserData.Instance.AddStamina(usedStamina, true);
				ScreenPresentationManager.Instance.EnqueueMessageBox_A_AB(TableTextKey.Instance.GetText("TOAST_STAMINA_REFUND"), Define.STAMINA_ID, usedStamina);
				//TODO : HYC 메세지박스로 안내. ScreenPresentationManager에 추가
			}

			PlayerPrefs.SetString(Define_PlayerPrefs.GAME_ENTER_FLAG, string.Empty);
		}
	}

	/// <summary> 구글로 계정 연동 보상 지급 확인 </summary>
	public void CheckLinkGoogleAccount()
    {
		if (LoginType == eLoginType.Google && SetFlagOption(eFlagOptions.LINK_GOOGLE_ACCOUNT))
		{
			//구글 계정 전환 완료 후 젬 지급
			AddGem(Define.LINK_GOOGLE_ACCOUNT_REWARD_GEM);
			ScreenPresentationManager.Instance.EnqueueMessageBox_A_AB(TableTextKey.Instance.GetText("ACCOUNT_LINK_GIFT_DESC"), Define.GEM_ID, 100);
		}
	}

	/// <summary> 평점 유도 확인 </summary>
	public void CheckRequestReview()
    {
		if (_userGameData.GamePlaycount >= 5 && SetFlagOption(eFlagOptions.REQUEST_REVIEW))
		{
			//구글 계정 전환 완료 후 젬 지급
			ScreenPresentationManager.Instance.EnqueueRate();
		}
	}

    public void CheckSystemMessage()
    {
		if (_userGameData.SystemMessage.IsNullOrWhiteSpace() == false)
		{
			ScreenPresentationManager.Instance.EnqueueSystemMessage(_userGameData.SystemMessage);

			_userGameData.SystemMessage = string.Empty;
            SaveUserData();
		}
	}

    public bool SetFlagOption(eFlagOptions value)
    {
        return _userGameData.SetFlagOption(value);
    }

    public void OnPurchaseConfirm(string productID, bool isAD)
    {
        _userGameData.OnPurchaseConfirm(productID, isAD);
    }

    public void SetLastPatrolRewardTime(long lastTime)
    {
        _userGameData.SetLastPatrolRewardTime(lastTime);
    }

    public void UseFastPatrol_AD()
    {
        _userGameData.UseFastPatrol_AD();
    }

    public void UseFastPatrol_Stamina()
    {
        _userGameData.UseFastPatrol_Stamina();
    }

    public bool IsNullUID()
	{
		return string.IsNullOrEmpty(UID);
	}

    public bool IsNullNickName()
    {
        return string.IsNullOrEmpty(NickName);
    }

    public bool IsTerms()
    {
        return _userGameData.IsTerms;
    }

    public bool IsAdConsent()
    {
        return _userGameData.IsADConsent;
    }

    public void SetTerms(bool isTerms)
    {
        _userGameData.IsTerms = isTerms;
    }

    public void SetAdConsent(bool isAdConsent)
    {
        _userGameData.IsADConsent = isAdConsent;
    }

    public bool HasReward()
    {
        return _userGameData.HasReward();
    }

	public long GetPurchaseCount(eShopPurchaseResetType shopPurchaseResetType, string productID)
	{
		return _userGameData.GetPurchaseCount(shopPurchaseResetType, productID);
	}

    public int GetTowerFloor()
    {
        return _userGameData.TowerFloor;
    }

    public long GetTowerTicket()
    {
        return _userGameData.TowerTicket;
    }

    public int GetLastSelectedStageID()
    {
        int lastSelectedStageId = PlayerPrefs.GetInt($"LAST_SELECTED_STAGE_ID_{UserData.Instance.UID}", 0);
        if (lastSelectedStageId > 0)
            return lastSelectedStageId;
        else
            return StageIDUtility.ComposeStageID(1, eDifficultyType.Normal);
    }

    public void SaveLastSelectedStageID(int stageID)
    {
        PlayerPrefs.SetInt($"LAST_SELECTED_STAGE_ID_{UserData.Instance.UID}", stageID);
        PlayerPrefs.Save();
    }

    public bool IsTowerRewardReceived(int floor)
    {
        if (_userGameData.TowerRewarded == null)
            return false;
       
        return _userGameData.TowerRewarded.Contains(floor);
    }

    public bool IsTowerClear(int floor)
    {
        if (_userGameData.TowerClear == null)
            return false;
        
        return _userGameData.TowerClear.Contains(floor);
    }

    public void UseItem(List<Item> items)
    {
        foreach (var item in items)
        {
            UseItem(item.ItemID, item.Amount);
        }
    }

    public void UseItem(Item item)
    {
        UseItem(item.ItemID, item.Amount);
    }

    public void UseItem(long itemID, long itemAmount)
    {
        switch (itemID)
        {
            case Define.STAMINA_ID:
                AddStamina(-itemAmount, true);
                break;
            case Define.GOLD_ID:
                AddGold(-itemAmount);
                break;
            case Define.GEM_ID:
                AddGem(-itemAmount);
                break;
            case Define.TOWERTICKET_ID:
                AddTowerTicket(-itemAmount);
                break;
            case Define.TOKEN_NORMAL:
                AddToken(eOutGameUpgradeToken.Normal, -itemAmount);
                break;
            case Define.TOKEN_RARE:
                AddToken(eOutGameUpgradeToken.Rare, -itemAmount);
                break;
            case Define.TOKEN_EPIC:
                AddToken(eOutGameUpgradeToken.Epic, -itemAmount);
                break;
            case Define.TOKEN_LEGENDARY:
                AddToken(eOutGameUpgradeToken.Legend, -itemAmount);
                break;
        }

        if (_userGameData.Inventory != null && _userGameData.Inventory.ContainsKey(itemID))
        {
            _userGameData.Inventory[itemID] -= itemAmount;
        }

        SaveUserData();
    }

    public List<Item> GetAllItem()
    {
        List<Item> items = new List<Item>();

        if (_userGameData.UpgradeTokens != null)
        {
            foreach (var token in _userGameData.UpgradeTokens)
            {
                long itemID = 0;
                switch (token.Key)
                {
                    case eOutGameUpgradeToken.Normal:
                        itemID = Define.TOKEN_NORMAL;
                        break;
                    case eOutGameUpgradeToken.Rare:
                        itemID = Define.TOKEN_RARE;
                        break;
                    case eOutGameUpgradeToken.Epic:
                        itemID = Define.TOKEN_EPIC;
                        break;
                    case eOutGameUpgradeToken.Legend:
                        itemID = Define.TOKEN_LEGENDARY;
                        break;
                }

                if (itemID > 0 && token.Value > 0)
                    items.Add(new Item(itemID, token.Value));
            }
        }

        // 위에 것 외에는 인벤토리에 들어감
        // 나중에 마이그레이션 해야할거 같은데..
        if (_userGameData.Inventory != null)
        {
            foreach (var item in _userGameData.Inventory)
            {
                if (item.Key > 0 && item.Value > 0)
                    items.Add(new Item(item.Key, item.Value));
            }
        }
        return items;
    }

    /// <summary> 아이템 보유 개수 조회 </summary>
    public long GetItemAmount(long itemID)
    {
        switch (itemID)
        {
            case Define.STAMINA_ID:
                return CurrentStamina;
            case Define.GOLD_ID:
                return _userGameData.Gold;
            case Define.GEM_ID:
                return _userGameData.Gem;
            case Define.TOWERTICKET_ID:
                return _userGameData.TowerTicket;
            case Define.TOKEN_NORMAL:
                return GetOutGameUpgradeToken(eOutGameUpgradeToken.Normal);
            case Define.TOKEN_RARE:
                return GetOutGameUpgradeToken(eOutGameUpgradeToken.Rare);
            case Define.TOKEN_EPIC:
                return GetOutGameUpgradeToken(eOutGameUpgradeToken.Epic);
            case Define.TOKEN_LEGENDARY:
                return GetOutGameUpgradeToken(eOutGameUpgradeToken.Legend);
        }

        if (_userGameData.Inventory != null && _userGameData.Inventory.ContainsKey(itemID))
            return _userGameData.Inventory[itemID];

        return 0;
    }

    public bool IsActiveItem(long itemID)
    {
        if (_userGameData.ActiveItems == null)
            return false;

        return _userGameData.ActiveItems.Contains(itemID);
    }

    public HashSet<long> GetActiveItems()
    {
        return _userGameData.ActiveItems;
    }

    public int GetActiveItemCount()
    {
        if (_userGameData.ActiveItems == null)
            return 0;

        return _userGameData.ActiveItems.Count;
    }

    public void AddActivetem(long itemID)
    {
        if (_userGameData.ActiveItems == null)
            _userGameData.ActiveItems = new HashSet<long>();

        var itemInfo = TableItemInfo.Instance.Get(itemID);
        if (itemInfo == null)
            return;

        if (itemInfo.TYPE != eItemType.Consumable)
            return;

        _userGameData.ActiveItems.Add(itemID);
        SaveUserData();
    }

    public void RemoveActiveItem(long itemID)
    {
        if (_userGameData.ActiveItems == null)
            return;

        _userGameData.ActiveItems.Remove(itemID);
        SaveUserData();
    }

    public void CheckActiveItem()
    {
        if (_userGameData.ActiveItems == null)
            return;

        _userGameData.ActiveItems.RemoveWhere(itemID => GetItemAmount(itemID) <= 0);
    }

#if DEV_QA
    public void AttendanceTest()
    {
        _userGameData.AttendanceTest();
    }
#endif
}