package com.SurvivalTaktix.ProjectST;

import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

import androidx.annotation.Nullable;

import com.google.android.gms.auth.api.signin.GoogleSignIn;
import com.google.android.gms.auth.api.signin.GoogleSignInAccount;
import com.google.android.gms.auth.api.signin.GoogleSignInClient;
import com.google.android.gms.auth.api.signin.GoogleSignInOptions;
import com.google.android.gms.common.api.ApiException;
import com.google.android.gms.tasks.Task;
import com.unity3d.player.UnityPlayer;
import com.unity3d.player.UnityPlayerActivity;

// Unity의 기본 Activity를 상속받아 사용합니다.
public class CustomUnityActivity extends UnityPlayerActivity {

    // 상수 정의
    private static final int RC_SIGN_IN = 9001;
    private static final String WEB_CLIENT_ID = "WEB_CLIENT_ID";
    private static final String TAG = "JavaGoogleSignIn";
    // Unity C# 스크립트가 붙어 있는 GameObject 이름
    private static final String UNITY_RECEIVER_OBJECT = "GoogleAuthManager";
    private static final String UNITY_RECEIVER_METHOD = "OnNativeTokenReceived";


    // ==========================================================
    // 1. Unity C#에서 호출되는 함수
    // ==========================================================

    // 이 함수를 C# 코드 (NativeGoogleSignInBridge)에서 currentActivity.Call("StartGoogleSignIn")로 호출합니다.
    public void StartGoogleSignIn() {
        Log.d(TAG, "StartGoogleSignIn called from Unity.");

        // 1. Google Sign-In 옵션 설정 (ID 토큰 요청 필수)
        GoogleSignInOptions gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DEFAULT_SIGN_IN)
                .requestIdToken(WEB_CLIENT_ID) // Firebase 인증에 필요한 ID 토큰 요청
                .requestEmail()
                .build();

        // 2. 클라이언트 생성 및 로그인 Intent 실행 (계정 선택 팝업 표시)
        GoogleSignInClient signInClient = GoogleSignIn.getClient(this, gso);
        Intent signInIntent = signInClient.getSignInIntent();
        startActivityForResult(signInIntent, RC_SIGN_IN);
    }

    // ==========================================================
    // 2. 로그인 결과 처리 (Android 시스템 콜백)
    // ==========================================================

    @Override
    protected void onActivityResult(int requestCode, int resultCode, @Nullable Intent data) {
        // 다른 플러그인(Firebase 등)의 ActivityResult를 위해 super 호출을 유지합니다.
        super.onActivityResult(requestCode, resultCode, data);

        if (requestCode == RC_SIGN_IN) {
            Task<GoogleSignInAccount> task = GoogleSignIn.getSignedInAccountFromIntent(data);
            handleSignInResult(task);
        }
    }

    private void handleSignInResult(Task<GoogleSignInAccount> completedTask) {
        try {
            // 3. ID 토큰 획득
            GoogleSignInAccount account = completedTask.getResult(ApiException.class);
            String idToken = account.getIdToken();

            // 4. ID 토큰을 Unity C# 코드로 전달
            if (idToken != null) {
                // UnitySendMessage: (GameObject 이름, C# 함수 이름, 전달할 문자열)
                UnityPlayer.UnitySendMessage(UNITY_RECEIVER_OBJECT, UNITY_RECEIVER_METHOD, idToken);
                Log.d(TAG, "ID Token sent to Unity successfully.");
            } else {
                UnityPlayer.UnitySendMessage(UNITY_RECEIVER_OBJECT, UNITY_RECEIVER_METHOD, "");
                Log.e(TAG, "ID Token is null.");
            }

        } catch (ApiException e) {
            // 로그인 실패 또는 취소
            Log.e(TAG, "SignIn failed: Code = " + e.getStatusCode());
            // 실패했음을 Unity에 알림 (빈 문자열 또는 오류 코드를 전달)
            UnityPlayer.UnitySendMessage(UNITY_RECEIVER_OBJECT, UNITY_RECEIVER_METHOD, "");
        }
    }
}
