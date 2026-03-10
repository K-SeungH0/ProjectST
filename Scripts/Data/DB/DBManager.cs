using Firebase.Firestore; // Firestore 네임스페이스로 변경
using GameDefine;
using System;
using System.Collections;
using System.Collections.Generic; // Dictionary 사용을 위해 추가
using UnityEngine;
using static UserData;

/// <summary>
/// DB 로드 결과 상태
/// </summary>
public enum eLoadResult
{
    Success,    // 로드 성공
    NoData,     // DB에 데이터가 없음 (신규 유저)
    Failed      // DB 접근 실패 (타임아웃, 권한 없음 등)
}

/// <summary>
/// Firebase Firestore와의 실제 통신(읽기/쓰기)을 전담하는 싱글톤 클래스.
/// (Firestore 버전으로 수정됨)
/// </summary>
public class DBManager : MonoSingleton_Global<DBManager>
{
    public string GUEST_UID => $"0_{SystemInfo.deviceUniqueIdentifier}";

    // 게임 버전
    public const string GAME_VERSION_KEY = "GameVersion";

    // 로컬 저장용 Key
    public const string UID_KEY = "Player_UID";
    public const string NickName_KEY = "{0}_NickName";
    public const string GameData_KEY = "{0}_GameData";
    public const string LoginType_KEY = "LoginType";

    public const string SAVE_KEY_USER = "user";
    // public const string SAVE_KEY_DATA = "gameData";
    public const string SAVE_KEY_NICKNAME = "nickName";
    public const string SAVE_KEY_UID = "uid";
	public const string SAVE_KEY_NICKNAME_FRIENDID = "nicknameFriendID";
	public const string SAVE_KEY_LOGIN_TYPE = "LoginType";
	public const string SAVE_KEY_FRIEND = "friend";
    public const string SAVE_KEY_FRIEND_DATA = "friendData";
    public const string SAVE_KEY_TIME = "lastUpdateTime";

    // 공용 필드 키
    public const string SAVE_KEY_TERMS = "Terms";
    public const string SAVE_KEY_AD_CONSENT = "AdConsent";
    public const string SAVE_KEY_VERSION = "SaveVersion";

    // UserGameData 필드 키
    public const string SAVE_KEY_LEVEL = "Level";
    public const string SAVE_KEY_GOLD = "Gold";
    public const string SAVE_KEY_STAMINA = "Stamina";
    public const string SAVE_KEY_GEM = "Gem";
    public const string SAVE_KEY_TOWER_TICKET = "TowerTicket";
    public const string SAVE_KEY_STAMINA_LAST_UPDATE_TIME = "StaminaLastUpdateTime";
    public const string SAVE_KEY_TOWER_TICKET_LAST_UPDATE_TIME = "TowerTicketLastUpdateTime";
    public const string SAVE_KEY_EQUIP_WEAPON = "EquipWeapon";
    public const string SAVE_KEY_SELECTED_PROFILE_IMAGE_ID = "SelectedProfileImageID";
    public const string SAVE_KEY_SELECTED_FRAME_ID = "SelectedFrameID";
    public const string SAVE_KEY_HAS_PLAYED_FIRST_GAME = "HasPlayedFirstGame";
    public const string SAVE_KEY_FLAG_OPTIONS = "FlagOptions";
    public const string SAVE_KEY_STAGE_CLEAR_COUNT = "StageClearCount";

    // UserGameData 맵/리스트 필드 키
    public const string SAVE_KEY_UPGRADE_LEVELS = "UpgradeLevels";
    public const string SAVE_KEY_UPGRADE_TOKENS = "UpgradeTokens";
    public const string SAVE_KEY_WEAPONS = "Weapons";
    public const string SAVE_KEY_WEAPONS_UPGRADE_LEVEL = "WeaponLevel";
    public const string SAVE_KEY_UNLOCKED_FEATURES = "UnlockedFeatures";
    public const string SAVE_KEY_SELECTED_CHOICE_WEAPONS = "SelectedChoiceWeapons";
    public const string SAVE_KEY_QUEST_STATE_DATA = "QuestStateData";

    // QuestStateData 필드 키
    public const string SAVE_KEY_PROGRESS_TYPE = "ProgressType";
    public const string SAVE_KEY_QUEST_SCHEDULE_NUM = "QuestScheduleNum";
    public const string SAVE_KEY_TOTAL_POINT = "TotalPoint";
    public const string SAVE_KEY_LAST_REWARD_POINT = "LastRewardPoint";
    public const string SAVE_KEY_COMPLETE_POINT = "CompletePoint";
    public const string SAVE_KEY_QUEST_PROGRESS_DATA = "QuestProgressData";

    // QuestStateUserData 필드 키
    public const string SAVE_KEY_KIND = "KIND";
    public const string SAVE_KEY_TARGET_VALUE = "TargetValue";
    public const string SAVE_KEY_TARGET_VALUE_2 = "TargetValue_2";
    public const string SAVE_KEY_CURRENT_VALUE = "CurrentValue";
    public const string SAVE_KEY_POINT = "Point";
    public const string SAVE_KEY_QUEST_TYPE = "QuestType";
    public const string SAVE_KEY_QUEST_STATE = "QuestState";

    // PurchaseCount 필드 키
    public const string SAVE_KEY_PURCHASE_COUNT = "PurchaseCount";

	// PurchaseHistory 필드 키
	public const string SAVE_KEY_PURCHASE_HISTORY = "PurchaseHistory";
	public const string SAVE_KEY_PURCHASE_HISTORY_RESET_TYPE = "PurchaseHistoryResetType";
	public const string SAVE_KEY_PURCHASE_HISTORY_SCHEDULE_NUM = "PurchaseHistoryScheduleNum";
	public const string SAVE_KEY_PURCHASE_HISTORY_DATA = "PurchaseHistoryData";
	public const string SAVE_KEY_PURCHASE_HISTORY_PRODUCT_ID = "PurchaseHistoryProductID";
	public const string SAVE_KEY_PURCHASE_HISTORY_PURCHASE_COUNT = "PurchaseHistoryPurchaseCount";

    //Friend Service를 위한 별도 테이블
    public const string SAVE_KEY_FRIEND_SERVICE_UID = "FriendServiceUID";
	public const string SAVE_KEY_FRIEND_SERVICE_NICKNAME = "FriendServiceNickname";

    public const string SAVE_KEY_CREATED_DAY = "CreatedDay";
    public const string SAVE_KEY_LAST_LOGIN_DAY = "LastLoginDay";
    public const string SAVE_KEY_LOGIN_COUNT = "LoginCount";
    public const string SAVE_KEY_AD_SKIP = "ADSkip";
    public const string SAVE_KEY_LAST_PATROL_TIME = "LastPatrolTime";
	public const string SAVE_KEY_LAST_FAST_PATROL_AD = "FastPatrolAD";
	public const string SAVE_KEY_LAST_FAST_PATROL_STAMINA = "FastPatrolStamina";
    public const string SAVE_KEY_TOWER_SWEEP_AD = "TowerSweepADCount";
    public const string SAVE_KEY_TOWER_SWEEP_REMAIN = "TowerSweepRemainCount";
	public const string SAVE_KEY_CREATED_TICKS = "CreatedTicks";
	public const string SAVE_KEY_SINGLE_GAME_PLAY_COUNT = "SingleGamePlayCount";
	public const string SAVE_KEY_MULTI_GAME_PLAY_COUNT = "MultiGamePlayCount";
    public const string SAVE_KEY_SYSTEM_MESSAGE = "SystemMessage";
    public const string SAVE_KEY_INVENTORY = "Inventory";
    public const string SAVE_KEY_ACTIVE_ITEMS = "ActiveItems";

    public const string SAVE_KEY_TOWER_FLOOR = "TowerFloor";
    public const string SAVE_KEY_TOWER_CLEAR = "TowerClear";
    public const string SAVE_KEY_TOWER_REWARDED = "TowerRewarded";
    public const string SAVE_KEY_TOWER_RANK = "towerRank";
    public const string SAVE_KEY_TOWER_SOCRE = "Score";
    public const string SAVE_KEY_TOWER_WEAPON = "Weapon";
    public const string SAVE_KEY_TOWER_WEAPON_RARITY = "WeaponRarity";


    private const float TIMEOUT_SECONDS = 10f;
    // 데이터 변경 감지 되면 2분 뒤에 DB에 저장
    private const float SAVE_DELAY_TIME = 120f;

    // Firestore 인스턴스
    private FirebaseFirestore _firestoreDB;

    protected override void Awake()
    {
        base.Awake();
        // Firestore 인스턴스를 한 번만 가져옵니다.
        _firestoreDB = FirebaseFirestore.DefaultInstance;
    }

    #region Version Check

    public void GetGameVersion(Action<bool> onComplete)
    {
        StartCoroutine(GameVersion(onComplete));
    }

    private IEnumerator GameVersion(Action<bool> onComplete = null)
    {
        if (_firestoreDB == null)
        {
            LogManager.LogError("FirebaseFirestore 인스턴스를 가져올 수 없습니다. Firebase 설정 파일을 확인하세요.");
            onComplete?.Invoke(false);
            yield break;
        }

        // Firestore 경로: user (컬렉션) -> uid (문서)
        DocumentReference docRef = _firestoreDB.Collection(GAME_VERSION_KEY).Document(GAME_VERSION_KEY);
        var loadTask = docRef.GetSnapshotAsync();
        float startTime = Time.time;
        yield return new WaitUntil(() => loadTask.IsCompleted || (Time.time - startTime > TIMEOUT_SECONDS));

        if (loadTask.IsFaulted || loadTask.IsCanceled || !loadTask.IsCompleted)
        {
            LogManager.LogError($"[DBManager] Firestore 데이터 로드 중 오류 발생: {loadTask.Exception}");
            onComplete?.Invoke(false);
            yield break;
        }

        DocumentSnapshot snapshot = loadTask.Result;

        if (!snapshot.Exists)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        var dataMap = snapshot.ToDictionary();
        if (dataMap != null && dataMap.Count > 0)
        {
            if (snapshot.TryGetValue(GAME_VERSION_KEY, out string gameVersion))
            {
                var currentVersion = new Version(Application.version);
                var latestVersion = new Version(gameVersion);

                if (currentVersion.CompareTo(latestVersion) < 0)
                {
                    // 업데이트 필요!
                    onComplete?.Invoke(false);
                }
                else
                {
                    LogManager.Log($"GameVersion Check Success! {currentVersion}");
                    onComplete?.Invoke(true);
                }
            }
        }
        else
        {
            // 문서는 존재하지만 데이터가 비어있는 경우
            LogManager.Log($"Firebase(Firestore) 문서{GAME_VERSION_KEY}은 있으나 데이터 {GAME_VERSION_KEY}이 비어있습니다.");
            onComplete?.Invoke(false);
        }
    }

    #endregion

    private bool _saveFlag = false;
    private Action<bool> _onComplete;

	public void StartSaveData(Action<bool> onComplete = null, bool isForce = false)
    {
		if (UserData.Instance.IsNullUID())
        {
            LogManager.LogError("UID가 null이므로 저장을 스킵합니다. LoadUserData 또는 ResetData가 먼저 호출되어야 합니다.");
            onComplete?.Invoke(false);
            return;
        }

        switch (UserData.Instance.LoginType)
        {
            case eLoginType.None:
            case eLoginType.Guest:
                if (UserData.Instance.UID != GUEST_UID)
                {
					//NOTE : HYC 26-01-07 Legacy
					SaveDataFromLocal();
                    onComplete?.Invoke(true);
                    break;
                }
                else
                {
					_saveFlag = true;
					_onComplete += onComplete;
					break;
                }

            case eLoginType.Google:
            case eLoginType.Editor:
                _saveFlag = true;
                _onComplete += onComplete;
                break;
        }
    }

	private void Update()
	{
		if(_saveFlag)
        {
			//한틱에 한번만 저장 시도
			LogManager.Log("StartSave");
			var dataToSave = UserData.Instance.Data.GetSaveDataDictionary();
            // 비동기로 작동하기 떄문에 콜백을 로컬 변수에 복사
            var completeCallback = _onComplete;

            switch (UserData.Instance.LoginType)
            {
				case eLoginType.None:
				case eLoginType.Guest:
					StartSaveData(GUEST_UID, SAVE_KEY_USER, dataToSave, completeCallback);
					break;
				case eLoginType.Google:
				case eLoginType.Editor:
					StartSaveData(UserData.Instance.UID, SAVE_KEY_USER, dataToSave, completeCallback);
					break;
                default:
                    LogManager.LogError($"DBManager Save User Data Error Invalid Login Type : {UserData.Instance.LoginType}");
					StartSaveData(GUEST_UID, SAVE_KEY_USER, dataToSave, completeCallback);
					break;
			}

            _saveFlag = false;
            _onComplete = null;
		}
	}

    /// <summary>
    /// 데이터 즉시 강제로 저장하는 함수
    /// </summary>
    public void StartSaveData(string uid, string collection, Dictionary<string, object> dataToSave, Action<bool> onComplete = null)
    {
        StartCoroutine(SaveData(uid, collection, dataToSave, onComplete));
    }

    /// <summary>
    /// 실제 Firebase Firestore에 데이터를 저장하는 코루틴입니다.
    /// </summary>
    private IEnumerator SaveData(string uid, string collection, Dictionary<string, object> dataToSave, Action<bool> onComplete = null)
    {
        if (string.IsNullOrEmpty(uid) || dataToSave == null)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        if (_firestoreDB == null)
        {
            LogManager.LogError("FirebaseFirestore 인스턴스를 가져올 수 없습니다. Firebase 설정 파일을 확인하세요.");
            onComplete?.Invoke(false);
            yield break;
        }

        // Firestore 경로: user (컬렉션) -> uid (문서)
        DocumentReference docRef = _firestoreDB.Collection(collection).Document(uid);

        // [중요] SetOptions.MergeAll
        // 이 옵션을 사용해야 기존 필드(예: nickName)를 덮어쓰지 않고,
        // dataToSave에 포함된 필드(예: level, gold, lastUpdated)만 병합/업데이트합니다.
        var task = docRef.SetAsync(dataToSave, SetOptions.MergeAll);

        float startTime = Time.time;
        yield return new WaitUntil(() => task.IsCompleted || (Time.time - startTime > TIMEOUT_SECONDS));

        bool success = task.IsCompleted && !task.IsFaulted && !task.IsCanceled;
        if (success)
        {
            LogManager.Log($"[Firestore] key : {collection} 저장 완료.");
            onComplete?.Invoke(true);
        }
        else
        {
            if (task.IsCompleted)
                LogManager.LogError($"[Firestore] 데이터 저장 중 오류 발생: {task.Exception}");
            else
                LogManager.LogError($"[Firestore] 데이터 저장 작업 타임아웃({TIMEOUT_SECONDS}s).");

            onComplete?.Invoke(false);
        }
    }

	/// <summary> 로그인씬에서 로그인 타입에 맞게 이전 데이터 불러오기 </summary>
	public void StartLoadDataByLoginType(Action<bool> onComplete)
    {
        switch (UserData.Instance.LoginType)
        {
            case eLoginType.None:
            case eLoginType.Guest:
                // 게스트 정보가 PlayerPfefs -> DB에 DeviceID를 Key로하는 문서 방식으로 변경됨. 그에 따른 분기 처리
				UserData.Instance.UID = PlayerPrefs.GetString(UID_KEY, string.Empty);
				string encryptedData = PlayerPrefs.GetString(string.Format(GameData_KEY, UserData.Instance.UID), string.Empty);
				if (string.IsNullOrEmpty(UserData.Instance.UID) || string.IsNullOrEmpty(encryptedData))
				{
					//신규 게스트 데이터 저장 로직
					UserData.Instance.UID = GUEST_UID;
					StartLoadUserData(UserData.Instance.UID, (result, data) => UserData.Instance.OnLoadComplete(result, data, onComplete));
				}
                else
                {
					//NOTE : HYC 26-01-07 Legacy
					// 닉네임
					var nickName = PlayerPrefs.GetString(string.Format(NickName_KEY, UserData.Instance.UID), string.Empty);
					if (string.IsNullOrEmpty(nickName) == false)
						UserData.Instance.SetUserNickname(nickName);

					UserData.Instance.OnLoadComplete(eLoadResult.Success, encryptedData, onComplete);
				}
                break;

            case eLoginType.Google:
            case eLoginType.Editor:
                StartLoadUserData(UserData.Instance.UID, (result, data) => UserData.Instance.OnLoadComplete(result, data, onComplete));
                break;
        }
    }

    private void StartLoadUserData(string uid, Action<eLoadResult, Dictionary<string, object>> onComplete)
    {
        StartCoroutine(LoadData(uid, SAVE_KEY_USER, onComplete));
    }

    public void LoadUserData(string uid, string collectionKey, Action<eLoadResult, Dictionary<string, object>> onComplete)
    {
        StartCoroutine(LoadData(uid, collectionKey, onComplete));
    }

    public IEnumerator IsAlreadyCreatedAccount(string uid, Action<eLoadResult> onComplete)
    {
		if (string.IsNullOrEmpty(uid))
        {
            onComplete?.Invoke(eLoadResult.Failed);
			yield break;
        }

		if (_firestoreDB == null)
		{
			LogManager.LogError("[DBManager] FirebaseFirestore 인턴스를 가져올 수 없습니다. Firebase 설정 파일을 확인하세요.");
			onComplete?.Invoke(eLoadResult.Failed);
			yield break;
		}

		// Firestore 경로: user (컬렉션) -> uid (문서)
		DocumentReference docRef = _firestoreDB.Collection(SAVE_KEY_USER).Document(uid);
		var loadTask = docRef.GetSnapshotAsync();

		float startTime = Time.time;
		yield return new WaitUntil(() => loadTask.IsCompleted || (Time.time - startTime > TIMEOUT_SECONDS));

		if (loadTask.IsFaulted || loadTask.IsCanceled || !loadTask.IsCompleted)
		{
			LogManager.LogError($"[DBManager] Firestore 데이터 로드 중 오류 발생: {loadTask.Exception}");
			onComplete?.Invoke(eLoadResult.Failed);
			yield break;
		}

		DocumentSnapshot snapshot = loadTask.Result;

		if (!snapshot.Exists)
		{
			LogManager.Log($"Firebase(Firestore)에 저장된 데이터(문서)가 없습니다. (신규 유저) : {uid}");
			onComplete?.Invoke(eLoadResult.NoData);
			yield break;
		}

        //이미 있는 계정
		onComplete?.Invoke(eLoadResult.Success);
	}

    /// <summary>
    /// Firebase Firestore에서 암호화된 문자열 데이터를 로드합니다.
    /// </summary>
    private IEnumerator LoadData(string uid, string collectionKey, Action<eLoadResult, Dictionary<string, object>> onComplete)
    {
        if (string.IsNullOrEmpty(uid))
        {
			LogManager.LogErrorNoti("[DBManager] uid 빈 문자열");
			onComplete?.Invoke(eLoadResult.Failed, null);
            yield break;
        }

        if (_firestoreDB == null)
        {
            LogManager.LogErrorNoti("[DBManager] FirebaseFirestore 인턴스를 가져올 수 없습니다. Firebase 설정 파일을 확인하세요.");
            onComplete?.Invoke(eLoadResult.Failed, null);
            yield break;
        }

        // Firestore 경로: user (컬렉션) -> uid (문서)
        DocumentReference docRef = _firestoreDB.Collection(collectionKey).Document(uid);
        var loadTask = docRef.GetSnapshotAsync();

        float startTime = Time.time;
        yield return new WaitUntil(() => loadTask.IsCompleted || (Time.time - startTime > TIMEOUT_SECONDS));

        if (loadTask.IsFaulted || loadTask.IsCanceled || !loadTask.IsCompleted)
        {
            LogManager.LogErrorNoti($"[DBManager] Firestore 데이터 로드 중 오류 발생: {uid}_{loadTask.Exception}");
            onComplete?.Invoke(eLoadResult.Failed, null);
            yield break;
        }

        DocumentSnapshot snapshot = loadTask.Result;

        if (!snapshot.Exists)
        {
            LogManager.Log($"Firebase(Firestore)에 저장된 데이터(문서)가 없습니다. (신규 유저) : {uid}");
            onComplete?.Invoke(eLoadResult.NoData, null);
            yield break;
        }

        var dataMap = snapshot.ToDictionary();
        if (dataMap != null && dataMap.Count > 0)
        {
            // [수정] 닉네임 필드를 dataMap에 수동으로 추가
            // (user 문서 루트에 있는 닉네임을 UserGameData가 파싱할 수 있도록)
            if (snapshot.TryGetValue(SAVE_KEY_NICKNAME, out string nickName))
                dataMap[SAVE_KEY_NICKNAME] = nickName;

            onComplete?.Invoke(eLoadResult.Success, dataMap);
        }
        else
        {
            // 문서는 존재하지만 데이터가 비어있는 경우
            LogManager.Log("Firebase(Firestore) 문서는 있으나 데이터가 비어있습니다.");
            onComplete?.Invoke(eLoadResult.NoData, null);
        }
    }

    private void SaveDataFromLocal()
    {
        if (UserData.Instance.UID != GUEST_UID)
        {
			//NOTE : HYC 26-01-07 Legacy
			var ser = UserGameDataConverter.ToSerializable(UserData.Instance.Data);
			// 저장할때 +1 시켜줘서 DB버전과 로컬버전 구분
			ser.SaveVersion++;
			string json = JsonUtility.ToJson(ser);
			string gameData = SimpleEncryption.Encrypt(json);

			// ID
			PlayerPrefs.SetString(UID_KEY, UserData.Instance.UID);

			// 닉네임
			PlayerPrefs.SetString(string.Format(NickName_KEY, UserData.Instance.UID), UserData.Instance.NickName);

			// 로컬에도 저장
			PlayerPrefs.SetString(string.Format(GameData_KEY, UserData.Instance.UID), gameData);

			PlayerPrefs.Save();
		}
    }

    // =================================================================================================
    // [Helper Methods] 내부 사용 함수들
    // =================================================================================================

    public void DeleteDataFromLocal()
    {
        PlayerPrefs.DeleteKey(UID_KEY);
        PlayerPrefs.DeleteKey(string.Format(NickName_KEY, UserData.Instance.UID));
        PlayerPrefs.DeleteKey(string.Format(GameData_KEY, UserData.Instance.UID));
		PlayerPrefs.DeleteKey(LoginType_KEY);
        PlayerPrefs.Save();
	}

    /// <summary>
    /// 신규 유저의 닉네임을 설정하고 데이터를 생성합니다.
    /// </summary>
    public IEnumerator Co_CreateNickname(string uid, string nickname, Action<bool> onComplete)
    {
        // 1. Firestore 트랜잭션 작업을 Task 객체로 시작합니다.
        var transactionTask = _firestoreDB.RunTransactionAsync(async transaction =>
        {
            DocumentReference nicknameDocRef = _firestoreDB.Collection(SAVE_KEY_NICKNAME).Document(nickname);
            DocumentSnapshot nicknameSnapshot = await transaction.GetSnapshotAsync(nicknameDocRef);

            if (nicknameSnapshot.Exists)
            {
				throw new FirestoreException(FirestoreError.AlreadyExists, "닉네임이 이미 존재합니다.");
            }

            var nicknameData = new Dictionary<string, object> 
            { 
                { SAVE_KEY_UID, uid }, 
                { SAVE_KEY_NICKNAME_FRIENDID, string.Empty },
                { SAVE_KEY_LOGIN_TYPE, UserData.Instance.LoginType.ToString() },
                { SAVE_KEY_TIME, FieldValue.ServerTimestamp }
            };

            transaction.Set(nicknameDocRef, nicknameData);
        });

        // 2. transactionTask가 완료될 때까지 코루틴을 여기서 잠시 멈추고 기다립니다.
        float startTime = Time.time;
        yield return new WaitUntil(() => transactionTask.IsCompleted || (Time.time - startTime > TIMEOUT_SECONDS));

        // 3. Task가 완료된 후, 그 결과를 확인합니다.
        //    이 시점부터는 메인 스레드에서 안전하게 로직을 처리할 수 있습니다.
        if (transactionTask.IsCompletedSuccessfully)
        {
            LogManager.Log($"[Firestore] 닉네임 '{nickname}' (소유자: {uid}) 선점 성공!");
            // 성공 결과를 콜백으로 전달
            onComplete?.Invoke(true);
            PlatformManager.Instance.SendLog_CreatedAccount(UserData.Instance.UID, UserData.Instance.LoginType.ToString());
		}
        else
        {
            LogManager.LogError($"[Firestore] 닉네임 선점 실패: {transactionTask.Exception}");
            // 실패 결과를 콜백으로 전달
            onComplete?.Invoke(false);
        }
    }

    /// <summary>
    /// Firestore 트랜잭션을 사용해 닉네임을 변경하는 코루틴입니다.
    /// 작업 완료 후 onComplete 콜백으로 결과를 전달합니다.
    /// </summary>
    public IEnumerator Co_ChangeNickname(string uid, string oldNickname, string newNickname, Action<bool> onComplete)
    {
        // 1. 트랜잭션 작업을 Task 객체로 시작합니다.
        var transactionTask = _firestoreDB.RunTransactionAsync(async transaction =>
        {
            DocumentReference userDocRef = _firestoreDB.Collection(SAVE_KEY_USER).Document(uid);
            DocumentReference oldNicknameDocRef = _firestoreDB.Collection(SAVE_KEY_NICKNAME).Document(oldNickname);
            DocumentReference newNicknameDocRef = _firestoreDB.Collection(SAVE_KEY_NICKNAME).Document(newNickname);

			// 새 닉네임이 중복되는지 확인
			DocumentSnapshot newNicknameSnapshot = await transaction.GetSnapshotAsync(newNicknameDocRef);
            if (newNicknameSnapshot.Exists)
                throw new FirestoreException(FirestoreError.AlreadyExists, "새 닉네임이 이미 사용 중입니다.");

            //이전에 저장된 friendID를 옮겨오기위해 old데이터 조회
			DocumentSnapshot oldNicknameSnapshot = await transaction.GetSnapshotAsync(oldNicknameDocRef);
			string oldFriendID = oldNicknameSnapshot.GetValue<string>(SAVE_KEY_NICKNAME_FRIENDID);

            // user 컬렉션에 있는 닉네임 업데이트
            transaction.Update(userDocRef, SAVE_KEY_NICKNAME, newNickname);

            // 기존 Old 닉네임 Document 삭제
            transaction.Delete(oldNicknameDocRef);

            // 새로운 닉네임 생성
            var nicknameData = new Dictionary<string, object> 
            { 
                { SAVE_KEY_UID, uid },
                { SAVE_KEY_NICKNAME_FRIENDID, oldFriendID },
                { SAVE_KEY_LOGIN_TYPE, UserData.Instance.LoginType.ToString() },
                { SAVE_KEY_TIME, FieldValue.ServerTimestamp }
            };

            transaction.Set(newNicknameDocRef, nicknameData);
		});

        // 2. [핵심] transactionTask가 완료될 때까지 코루틴을 여기서 멈추고 기다립니다.
        float startTime = Time.time;
        yield return new WaitUntil(() => transactionTask.IsCompleted || (Time.time - startTime > TIMEOUT_SECONDS));

        // 3. Task가 완료된 후, 그 결과를 확인하여 콜백을 호출합니다. (메인 스레드에서 실행)
        if (transactionTask.IsCompletedSuccessfully)
        {
            LogManager.Log($"[Firestore] 닉네임 변경 성공: {oldNickname} -> {newNickname}");
            onComplete?.Invoke(true);
        }
        else
        {
            LogManager.LogError($"[Firestore] 닉네임 변경 실패: {transactionTask.Exception}");
            onComplete?.Invoke(false);
        }
    }

    public IEnumerator Co_DeleteNickname(string nickName, Action<bool> onComplete = null)
    {
        if (string.IsNullOrEmpty(nickName))
        {
            LogManager.LogError("[Firestore] 삭제할 닉네임이 없습니다.");
            // 삭제할 닉네임 없으면 그냥 성공처리.
            onComplete?.Invoke(true);
            yield break;
        }

        if (_firestoreDB == null)
        {
            LogManager.LogError("[Firestore] DB 인스턴스가 없습니다.");
            onComplete?.Invoke(false);
            yield break;
        }

        DocumentReference docRef = _firestoreDB.Collection(SAVE_KEY_NICKNAME).Document(nickName);
        var task = docRef.DeleteAsync();

        float startTime = Time.time;
        yield return new WaitUntil(() => task.IsCompleted || (Time.time - startTime > TIMEOUT_SECONDS));

        if (task.IsFaulted || task.IsCanceled)
        {
            LogManager.LogError($"[Firestore] 닉네임 '{nickName}' 삭제 실패: {task.Exception}");
            onComplete?.Invoke(false);
        }
        else
        {
            LogManager.Log($"[Firestore] 닉네임 '{nickName}' 삭제 완료.");
            onComplete?.Invoke(true);
        }
    }

	public IEnumerator Co_DeleteUID(string uid, Action<bool> onComplete = null)
	{
		if (string.IsNullOrEmpty(uid))
		{
			LogManager.LogError("[Firestore] 삭제할 uid가 없습니다.");
			// 삭제할 닉네임 없으면 그냥 성공처리.
			onComplete?.Invoke(true);
			yield break;
		}

		if (_firestoreDB == null)
		{
			LogManager.LogError("[Firestore] DB 인스턴스가 없습니다.");
			onComplete?.Invoke(false);
			yield break;
		}

		DocumentReference docRef = _firestoreDB.Collection(SAVE_KEY_USER).Document(uid);
		var task = docRef.DeleteAsync();

		float startTime = Time.time;
		yield return new WaitUntil(() => task.IsCompleted || (Time.time - startTime > TIMEOUT_SECONDS));

		if (task.IsFaulted || task.IsCanceled)
		{
			LogManager.LogError($"[Firestore] UID '{uid}' 삭제 실패: {task.Exception}");
			onComplete?.Invoke(false);
		}
		else
		{
			LogManager.Log($"[Firestore] UID '{uid}' 삭제 완료.");
			onComplete?.Invoke(true);
		}
	}

	public IEnumerator Co_ChangeUID(string nickName, string oldUID, string newUID, Action<bool> onComplete = null)
    {
        if (string.IsNullOrEmpty(nickName) || string.IsNullOrEmpty(oldUID) || string.IsNullOrEmpty(newUID))
        {
            LogManager.LogError("[Firestore] Co_ChangeUID: 닉네임 또는 UID가 null이거나 비어있습니다.");
            onComplete?.Invoke(false);
            yield break;
        }

        if (_firestoreDB == null)
        {
            LogManager.LogError("[Firestore] DB 인스턴스가 없습니다.");
            onComplete?.Invoke(false);
            yield break;
        }

        DocumentReference docRef = _firestoreDB.Collection(SAVE_KEY_NICKNAME).Document(nickName);
        var transactionTask = _firestoreDB.RunTransactionAsync(async transaction =>
        {
            DocumentSnapshot snapshot = await transaction.GetSnapshotAsync(docRef);

            if (!snapshot.Exists)
                throw new FirestoreException(FirestoreError.NotFound, $"[Firestore] 닉네임 '{nickName}' 문서가 존재하지 않아 UID를 변경할 수 없습니다.");

            string currentUID;
            if (!snapshot.TryGetValue(SAVE_KEY_UID, out currentUID) || !currentUID.Equals(oldUID))
                throw new FirestoreException(FirestoreError.Aborted, $"[Firestore] 닉네임 '{nickName}'의 현재 소유자({currentUID ?? "null"})가 oldUID({oldUID})와 일치하지 않습니다.");

            transaction.Update(docRef, SAVE_KEY_UID, newUID);
            transaction.Update(docRef, SAVE_KEY_TIME, FieldValue.ServerTimestamp);
            transaction.Update(docRef, SAVE_KEY_LOGIN_TYPE, UserData.Instance.LoginType.ToString());
            // UID만 변경하기 때문에 기존 친구ID는 업데이트 필요없음
            // transaction.Update(docRef, SAVE_KEY_NICKNAME_FRIENDID);
        });

        var startTime = Time.time;
        yield return new WaitUntil(() => transactionTask.IsCompleted || (Time.time - startTime > TIMEOUT_SECONDS));

        // 8. (코루틴) 트랜잭션 결과 처리
        if (transactionTask.IsCompletedSuccessfully)
        {
            LogManager.Log($"[Firestore] 닉네임 소유권 이관 성공: '{nickName}' (소유자: {oldUID} -> {newUID})");
            onComplete?.Invoke(true);
        }
        else
        {
            // IsFaulted (예외 발생) 또는 IsCanceled
            LogManager.LogError($"[Firestore] 닉네임 소유권 이관 실패 ('{nickName}'): {transactionTask.Exception}");
            onComplete?.Invoke(false);
        }
    }

	public void CreateOrLinkToGoogle(string token, Action<eLoadResult> onComplete)
	{
		if (this.IsValid() && gameObject.activeInHierarchy)
			StartCoroutine(Co_CheckAlreadyCreatedGoogleAccount(token, onComplete));
        else
			onComplete?.Invoke(eLoadResult.Failed);
	}

	private IEnumerator Co_CheckAlreadyCreatedGoogleAccount(string token, Action<eLoadResult> onComplete)
	{
		yield return IsAlreadyCreatedAccount(token, (loadResult) =>
		{
			if (this.IsValid() && gameObject.activeInHierarchy)
			{
				switch (loadResult)
				{
					case eLoadResult.Success:
						//이미 존재하는 계정. 게스트 계정 보존처리. 로그아웃 후 구글 계정으로 로그인
						GameMain.Instance.LinkGuestToGoogle();
                        //로그아웃 시킬거라서 콜백 실행 안함
                        return;
					case eLoadResult.NoData:
						//신규 계정. 게스트 계정 삭제처리. 신규 구글 계정으로 현재 게스트 계정 복사 후 로그인
						StartLinkGuestToGoogle(token, onComplete);
						break;
					case eLoadResult.Failed:
						LogManager.LogWarning("구글 계정 불러오기 실패.");
						onComplete?.Invoke(loadResult);
						break;
				}
			}
		});
	}

	public void StartLinkGuestToGoogle(string newGoogleUID, Action<eLoadResult> onComplete)
    {
        StartCoroutine(Co_LinkGuestToGoogle(newGoogleUID, onComplete));
    }

    private IEnumerator Co_LinkGuestToGoogle(string newGoogleUID, Action<eLoadResult> onComplete)
    {
        if (string.IsNullOrEmpty(newGoogleUID))
        {
            LogManager.LogError("[DBManager] UID가 없습니다.");
            // UID 에러
            ToastManager.Instance.ShowError("TOAST_ACCOUNT_LINK_GOOGLE_FAILED");
			onComplete?.Invoke(eLoadResult.Failed);
			yield break;
        }

        if (_firestoreDB == null)
        {
            LogManager.LogError("[DBManager] FirebaseFirestore 인턴스를 가져올 수 없습니다. Firebase 설정 파일을 확인하세요.");
            // Firestore 인스턴스 에러
            ToastManager.Instance.ShowError("TOAST_ACCOUNT_LINK_GOOGLE_FAILED");
			onComplete?.Invoke(eLoadResult.Failed);
			yield break;
        }

        string guestUID = UserData.Instance.UID;
        string guestNickName = UserData.Instance.NickName;

        // 유저 UID 확인
        DocumentReference docRef = _firestoreDB.Collection(SAVE_KEY_USER).Document(newGoogleUID);
        var loadTask = docRef.GetSnapshotAsync();

        float startTime = Time.time;
        yield return new WaitUntil(() => loadTask.IsCompleted || (Time.time - startTime > TIMEOUT_SECONDS));

        if (loadTask.IsFaulted || loadTask.IsCanceled || !loadTask.IsCompleted)
        {
            LogManager.LogError($"[DBManager] Firestore 데이터 로드 중 오류 발생: {loadTask.Exception}");
            // 데이터 오류 그냥 종료
            ToastManager.Instance.ShowError("TOAST_ACCOUNT_LINK_GOOGLE_FAILED");
			onComplete?.Invoke(eLoadResult.Failed);
			yield break;
        }

        DocumentSnapshot snapshot = loadTask.Result;

        if (!snapshot.Exists)
        {
            LogManager.Log("Firebase(Firestore)에 저장된 데이터(문서)가 없습니다. (신규 유저)");

            // 닉네임 UID 변경 (게스트UID -> 구글UID)
            yield return StartCoroutine(Co_ChangeUID(guestNickName, guestUID, newGoogleUID, (success) => ResultChangeUID(success, newGoogleUID, guestUID)));

			onComplete?.Invoke(eLoadResult.Success);

			// 기존 게스트 계정 삭제처리
			GameMain.Instance.StartCoroutine(Co_DeleteUID(guestUID));
            yield break;
        }

        var dataMap = snapshot.ToDictionary();
        if (dataMap != null && dataMap.Count > 0)
        {
			// DB 저장된 데이터 있음
			//로그아웃 후 원래있던 계정으로 로그인
			GameMain.Instance.LinkGuestToGoogle();
        }
        else
        {
            LogManager.Log("Firebase(Firestore) 문서는 있으나 data가 없습니다. (신규 유저)");
            // DB저장된 데이터 없음 바로 저장
            yield return StartCoroutine(Co_ChangeUID(guestNickName, guestUID, newGoogleUID, (success) => ResultChangeUID(success, newGoogleUID, guestUID)));

			onComplete?.Invoke(eLoadResult.Success);
		}
    }

    private void ResultChangeUID(bool success, string newGoogleUID, string oldUID)
    {
        if (success)
        {
            StartSaveData((success) =>
            { 
                LinkGuestToGoogleSaveComplete(success, newGoogleUID, oldUID); 
            }, true);
        }
        else
        {
			UserData.Instance.LoginType = eLoginType.Google;
			UserData.Instance.UID = newGoogleUID;
			LogManager.LogError("[DBManager] 닉네임 소유권 이전 실패. 계정 연동을 중단합니다.");
            ToastManager.Instance.ShowError("TOAST_ACCOUNT_LINK_GOOGLE_FAILED");
        }
    }

	private void LinkGuestToGoogleSaveComplete(bool success, string newGoogleUID, string oldUID)
	{
		if (success)
		{
			// 로컬 데이터는 삭제해준다.
			DeleteDataFromLocal();
			// 기존 게스트 계정 삭제처리
			GameMain.Instance.StartCoroutine(Co_DeleteUID(oldUID));
			UserData.Instance.LoginType = eLoginType.Google;
			UserData.Instance.UID = newGoogleUID;
			UserData.Instance.CheckLinkGoogleAccount();
			DispatchHandler.Dispatch(DispatchHandler.GOOGLE_ACCOUNT_LINK);
			UIHelper.OpenOneButtonWindow(TableTextKey.Instance.GetText("TOAST_ACCOUNT_LINK_GOOGLE_SUCCESSFUL"));
		}
		else
		{
			// 저장 실패
			ToastManager.Instance.ShowError("TOAST_ACCOUNT_LINK_GOOGLE_FAILED");
		}
	}

    public void StartTowerRankSave(long score, eWeaponType weaponType, eWeaponRarity weaponRarity)
    {
        StartCoroutine(Co_TowerRankSave(score, weaponType, weaponRarity));
    }

    private IEnumerator Co_TowerRankSave(long score, eWeaponType weaponType, eWeaponRarity weaponRarity)
    {
        var uid = UserData.Instance.UID;
        var nickName = UserData.Instance.NickName;

        if (string.IsNullOrEmpty(uid))
        {
            LogManager.LogError("[DBManager] UID가 없습니다.");
            yield break;
        }

        DocumentReference docRef = _firestoreDB.Collection(SAVE_KEY_TOWER_RANK).Document(uid);

        // 트랜잭션 실행
        var task = _firestoreDB.RunTransactionAsync(transaction =>
        {
            return transaction.GetSnapshotAsync(docRef).ContinueWith(snapshotTask =>
            {
                DocumentSnapshot snapshot = snapshotTask.Result;
                long currentBestScore = 0;

                // 1. 기존 데이터가 있는지 확인하고 현재 점수를 가져옴
                if (snapshot.Exists)
                {
                    if (snapshot.ContainsField(SAVE_KEY_TOWER_SOCRE))
                    {
                        currentBestScore = snapshot.GetValue<long>(SAVE_KEY_TOWER_SOCRE);
                    }
                }

                // 2. 새 점수가 기존 점수보다 높을 때만 데이터 갱신
                if (score > currentBestScore)
                {
                    var rankData = new Dictionary<string, object>
                    {
                        { SAVE_KEY_NICKNAME, nickName },
                        { SAVE_KEY_TOWER_SOCRE, score },
                        { SAVE_KEY_TOWER_WEAPON, weaponType.ToString() },
                        { SAVE_KEY_TOWER_WEAPON_RARITY, weaponRarity.ToString() },
                        { SAVE_KEY_TIME, FieldValue.ServerTimestamp }
                    };

                    // 트랜잭션 내에서 Set 실행 (Merge 옵션 포함)
                    transaction.Set(docRef, rankData, SetOptions.MergeAll);
                    return "Updated";
                }

                return "LowerScore"; // 점수가 낮아서 업데이트 안 함
            });
        });

        float startTime = Time.time;
        yield return new WaitUntil(() => task.IsCompleted || (Time.time - startTime > TIMEOUT_SECONDS));

        if (task.IsFaulted || task.IsCanceled)
        {
            LogManager.LogError($"[DBManager] Firestore 트랜잭션 오류: {task.Exception}");
        }
        else if (task.Result == "Updated")
        {
            LogManager.Log($"[DBManager] 타워 랭킹 정보 저장 완료: {nickName} - {score} - {weaponType}");
        }
        else
        {
            LogManager.Log($"[DBManager] {nickName} 기존 기록 {score}이 더 높아서 저장하지 않았습니다.");
        }
    }

    public void StartTowerRankLoad(int rankCount, Action<List<TowerRankData>> onCompleted)
    {
        StartCoroutine(Co_TowerRankLoad(rankCount, onCompleted));
    }

    private IEnumerator Co_TowerRankLoad(int rankCount, Action<List<TowerRankData>> onCompleted)
    {
        if (_firestoreDB == null)
        {
            LogManager.LogError("[DBManager] Firestore 인스턴스가 없습니다.");
            yield break;
        }

        // 1. 점수(Score) 내림차순, 점수가 같으면 시간(Time) 오름차순(먼저 달성한 사람 우선)
        Query query = _firestoreDB.Collection(SAVE_KEY_TOWER_RANK)
            .OrderByDescending(SAVE_KEY_TOWER_SOCRE)
            .OrderBy(SAVE_KEY_TIME) // 동점자 처리용
            .Limit(rankCount);  // n명까지만

        var task = query.GetSnapshotAsync();
        float startTime = Time.time;

        yield return new WaitUntil(() => task.IsCompleted || (Time.time - startTime > TIMEOUT_SECONDS));

        if (task.IsFaulted || task.IsCanceled || !task.IsCompleted)
        {
            LogManager.LogError($"[DBManager] 랭킹 로드 실패: {task.Exception}");
            onCompleted?.Invoke(null);
            yield break;
        }

        QuerySnapshot snapshot = task.Result;
        List<TowerRankData> rankList = new List<TowerRankData>();

        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            if (doc.Exists)
            {
                // 데이터 추출
                var nick = doc.GetValue<string>(SAVE_KEY_NICKNAME);
                var score = doc.GetValue<long>(SAVE_KEY_TOWER_SOCRE);
                var weapon = doc.GetValue<string>(SAVE_KEY_TOWER_WEAPON);
                Enum.TryParse(weapon, out eWeaponType weaponType);

                doc.TryGetValue<string>(SAVE_KEY_TOWER_WEAPON_RARITY, out var weaponRarityData);
                var weaponRarity = eWeaponRarity.Uncommon;
                if (weaponRarityData != null)
                    Enum.TryParse(weaponRarityData, out weaponRarity);

                var timestamp = doc.GetValue<Timestamp>(SAVE_KEY_TIME);

                rankList.Add(new TowerRankData(nick, score, weaponType, weaponRarity, timestamp));
            }
        }

        LogManager.Log($"[DBManager] 랭킹 로드 완료: {rankList.Count}명");
        onCompleted?.Invoke(rankList);
    }

    public void StartTowerMySocreLoad(Action<long> onCompleted)
    {
        StartCoroutine(Co_TowerMySocreLoad(onCompleted));
    }

    private IEnumerator Co_TowerMySocreLoad(Action<long> onCompleted)
    {
        if (_firestoreDB == null)
        {
            LogManager.LogError("[DBManager] Firestore 인스턴스가 없습니다.");
            yield break;
        }

        DocumentReference docRef = _firestoreDB.Collection(SAVE_KEY_TOWER_RANK).Document(UserData.Instance.UID);
        var task = docRef.GetSnapshotAsync();
        float startTime = Time.time;
        yield return new WaitUntil(() => task.IsCompleted || (Time.time - startTime > TIMEOUT_SECONDS));

        if (task.IsFaulted || task.IsCanceled || !task.IsCompleted)
        {
            LogManager.LogError($"[DBManager] 내 점수 로드 실패: {task.Exception}");
            onCompleted?.Invoke(0);
            yield break;
        }

        DocumentSnapshot snapshot = task.Result;

        if (!snapshot.Exists)
        {
            LogManager.Log($"Firebase(Firestore)에 저장된 데이터(문서)가 없습니다. (랭킹 집계 전)");
            onCompleted?.Invoke(0);
            yield break;
        }

        var dataMap = snapshot.ToDictionary();
        if (dataMap != null && dataMap.Count > 0)
        {
            if (snapshot.TryGetValue(SAVE_KEY_NICKNAME, out string nickName))
            { 
                if (nickName.Equals(UserData.Instance.NickName))
                {
                    snapshot.TryGetValue(SAVE_KEY_TOWER_SOCRE, out long score);
                    onCompleted?.Invoke(score);
                }  
            }
        }
        else
        {
            // 문서는 존재하지만 데이터가 비어있는 경우
            LogManager.Log("Firebase(Firestore) 문서는 있으나 데이터가 비어있습니다.");
            onCompleted?.Invoke(0);
        }
    }
}