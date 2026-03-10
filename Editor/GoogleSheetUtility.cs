using UnityEngine;
using UnityEditor;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Services;
using System.IO;

public static class GoogleSheetUtility
{
    public const string credentialsPath = "client_secrets.json";

    public static bool IsReady(string sheetID)
    {
        if (string.IsNullOrEmpty(sheetID))
        {
            EditorUtility.DisplayDialog("Error", "선택된 데이터 타입의 Spreadsheet ID가 코드에 정의되지 않았습니다.", "OK");
            return false;
        }

        string fullCredentialsPath = Path.Combine(Application.dataPath, credentialsPath);
        if (!File.Exists(fullCredentialsPath))
        {
            EditorUtility.DisplayDialog("Error", $"인증 파일({credentialsPath})을 Assets 폴더에 넣어주세요.", "OK");
            return false;
        }
        return true;
    }

    public static SheetsService GetService(string scope)
    {
        return new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = GoogleCredential.FromFile(Path.Combine(Application.dataPath, credentialsPath)).CreateScoped(scope),
            ApplicationName = "Unity Game Data Manager",
        });
    }
    public static ServiceAccountCredential GetCredential(string[] scopes)
    {
        return GoogleCredential.FromFile(Path.Combine(Application.dataPath, GoogleSheetUtility.credentialsPath))
                               .CreateScoped(scopes)
                               .UnderlyingCredential as ServiceAccountCredential;
    }
}

