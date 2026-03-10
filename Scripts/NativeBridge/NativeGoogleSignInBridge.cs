using System;
using UnityEngine;

public class NativeGoogleSignInBridge : MonoBehaviour
{
    public Action<string> OnTokenReceived;

    public void StartSignIn()
    {
        LogManager.Log("C# -> Java: StartGoogleSignIn() 호출 시도...");

        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    // CustomUnityActivity.java 파일에 있는 'StartGoogleSignIn' 메소드를 호출합니다.
                    currentActivity.Call("StartGoogleSignIn");
                    LogManager.Log("C# -> Java: StartGoogleSignIn() 호출 성공.");
                }
            }
        }
        catch (System.Exception e)
        {
            LogManager.LogError($"C# -> Java 호출 실패: {e.Message}");
        }
    }

    // Java의 UnitySendMessage가 호출하는 함수입니다.
    // 이 부분은 CustomUnityActivity.java의 UnitySendMessage 호출과 일치하므로
    // 수정할 필요가 없습니다.
    void OnNativeTokenReceived(string idToken)
    {
        LogManager.Log("Java -> C#: OnNativeTokenReceived 수신. 토큰 전달.");
        OnTokenReceived?.Invoke(idToken);
    }
}

