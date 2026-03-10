using Firebase.Auth;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using System;

// NativeGoogleSignInBridge가 같은 GameObject에 있도록 강제합니다.
[RequireComponent(typeof(NativeGoogleSignInBridge))]
public class GoogleAuthManager : MonoSingleton_Global<GoogleAuthManager>
{
    private FirebaseAuth auth;
    private NativeGoogleSignInBridge bridge;

    public Action<string> OnSuccessCallback;
    public Action OnFailedCallback;

    protected override void Awake()
    {
        base.Awake();
        bridge = GetComponent<NativeGoogleSignInBridge>();
        if (bridge)
            bridge.OnTokenReceived += OnTokenReceived;
    }

    /// <summary> Android로 구글 로그인 호출 </summary>
    public void Login(Action<string> successCallback, Action failCallback)
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Firebase SDK가 준비되었습니다.
                // 여기에 초기화 후 필요한 로직을 추가할 수 있습니다.
                LogManager.Log("Firebase is ready to use!");
                auth = FirebaseAuth.DefaultInstance;
                
                if (bridge)
                    bridge.StartSignIn();
            }
            else
            {
                // Firebase SDK 초기화에 실패했습니다.
                LogManager.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
                OnFailedCallback?.Invoke();
                ResetCallback();
            }
        });

        OnSuccessCallback = successCallback;
        OnFailedCallback = failCallback;
    }


    /// <summary> 현재 로그인 중인가? </summary>
    public bool IsLogin()
    {
        return auth != null && auth.CurrentUser != null;
    }

    public void LogOut()
    {
        if (IsLogin())
        {
            auth.SignOut();
            LogManager.Log("Firebase 로그아웃 완료.");
        }
    }

    public void OnTokenReceived(string idToken)
    {
        if (string.IsNullOrEmpty(idToken))
        {
            LogManager.LogError("Google 로그인 실패 또는 취소됨.");
            OnFailedCallback?.Invoke();
            ResetCallback();
            return;
        }

        LogManager.NetworkLog($"Receive Google Token : {idToken}");
        UserData.Instance.TokenID = idToken;
        AuthenticateWithFirebase(idToken);
    }

    private void AuthenticateWithFirebase(string idToken)
    {
        Credential credential = GoogleAuthProvider.GetCredential(idToken, null);

        auth.SignInWithCredentialAsync(credential).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                LogManager.LogError($"Firebase 로그인 실패: {task.Exception}");
                OnFailedCallback?.Invoke();
                ResetCallback();
                return;
            }

            FirebaseUser user = task.Result;
            LogManager.Log($"Firebase 로그인 성공! 사용자 UID: {user.UserId}");

            OnSuccessCallback?.Invoke(user.UserId);
            ResetCallback();
        });
    }

    private void ResetCallback()
    {
        OnSuccessCallback = null;
        OnFailedCallback = null;
    }
}
