using UnityEngine;
using UnityEditor;
using Google.Apis.Drive.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using System.IO;
using System.Reflection;
using System.Linq;
using System;
using System.Collections.Generic;
using Color = UnityEngine.Color;
using GameDefine;

public class CSVDataParser : EditorWindow
{
    private enum DataType
    {
        TableData,
        StageData,

        SO_Start,
        WeaponData,
        MonsterData,
        PlayerData,
        WaveData,
        ConstData,
        SO_End,
    }

    private const string saveRootPath = "Assets/Resources/Table";
    private const string soRootPath = "Assets/Resources/Data";

    private const string tableDataSheetID        = "구글 시트 ID";
    private const string notificationInfoSheetID = "구글 시트 ID";
    private const string stageDataSheetID        = "구글 시트 ID";
    private const string weaponDataSheetID       = "구글 시트 ID";
    private const string monsterDataSheetID      = "구글 시트 ID";
    private const string playerDataSheetID       = "구글 시트 ID";
    private const string waveDataSheetID         = "구글 시트 ID";
    private const string constDataSheetID        = "구글 시트 ID";

    private DataType selectedDataType;

    // GUI에 표시할 enum 값들의 리스트와 문자열 이름
    private List<DataType> displayableDataTypes;
    private string[] displayableDataTypeNames;

    [MenuItem("Data Manager/Google Sheet Data Manager")]
    public static void ShowWindow()
    {
        GetWindow<CSVDataParser>("Sheets Manager");
    }

    // 윈도우가 활성화될 때 한 번 호출됩니다.
    private void OnEnable()
    {
        // LINQ를 사용해 SO_Start와 SO_End를 제외한 모든 DataType 값을 가져옵니다.
        displayableDataTypes = System.Enum.GetValues(typeof(DataType))
                                      .Cast<DataType>()
                                      .Where(e => e != DataType.SO_Start && e != DataType.SO_End)
                                      .ToList();

        // 화면에 표시될 이름을 위해 문자열 배열로 변환합니다.
        displayableDataTypeNames = displayableDataTypes.Select(e => e.ToString()).ToArray();

        // 기본 선택 값을 설정합니다.
        if (displayableDataTypes.Count > 0)
        {
            selectedDataType = displayableDataTypes[0];
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Google Sheets <-> Data Manager", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("처리할 데이터 타입을 선택하면 해당 구글 시트와 연동됩니다.", MessageType.Info);

        // 현재 선택된 enum 값(selectedDataType)이 표시 목록(displayableDataTypes)에서 몇 번째에 있는지 인덱스를 찾습니다.
        int currentIndex = displayableDataTypes.IndexOf(selectedDataType);

        // 만약 현재 값이 목록에 없다면 (예: 초기값이 SO_Start였을 경우) 0으로 초기화합니다.
        if (currentIndex < 0)
            currentIndex = 0;

        // 필터링된 목록을 사용해 Popup을 만듭니다.
        int selectedIndex = EditorGUILayout.Popup("Data Type", currentIndex, displayableDataTypeNames);

        // 사용자가 다른 항목을 선택했다면, selectedDataType 값을 업데이트합니다.
        if (selectedIndex != currentIndex)
            selectedDataType = displayableDataTypes[selectedIndex];

        string sheetId = GetSheetIdForSelectedType();
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField("Spreadsheet ID", sheetId);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(10);

        if (IsOnlyImportMode())
        {
            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
            if (GUILayout.Button($"Import [Google Sheets -> '{selectedDataType}']", GUILayout.Height(40)))
            {
                if (GoogleSheetUtility.IsReady(sheetId))
                {
                    DownloadAllSheetsInFolder(sheetId);
                }
            }
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
            if (GUILayout.Button($"Import [Google Sheets -> '{selectedDataType}']", GUILayout.Height(40)))
            {
                if (GoogleSheetUtility.IsReady(sheetId))
                {
                    if (selectedDataType == DataType.ConstData)
                        ImportConstDataSheet(sheetId);
                    else
                        ImportSelectedSheet(sheetId);
                }
            }

            GUI.backgroundColor = new Color(1f, 0.8f, 0.8f);
            if (GUILayout.Button($"Export ['{selectedDataType}' -> Google Sheets]", GUILayout.Height(40)))
            {
                if (GoogleSheetUtility.IsReady(sheetId) &&
                    EditorUtility.DisplayDialog("Export Confirmation", $"정말로 현재 '{selectedDataType}' SO 데이터를 Google Sheets에 덮어쓰시겠습니까? 이 작업은 되돌릴 수 없습니다.", "예", "아니오"))
                {
                    if (selectedDataType == DataType.ConstData)
                        ExportConstDataSheet(sheetId);
                    else
                        ExportSelectedSheet(sheetId);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
            if (GUILayout.Button("Import [Google Sheets -> All]", GUILayout.Height(40)))
            {
                for (var dataType = DataType.SO_Start + 1; dataType < DataType.SO_End; dataType++)
                {
                    selectedDataType = dataType;
                    string allSheetId = GetSheetIdForSelectedType();
                    if (GoogleSheetUtility.IsReady(allSheetId))
                    {
                        if (dataType == DataType.ConstData)
                            ImportConstDataSheet(allSheetId);
                        else
                            ImportSelectedSheet(allSheetId, dataType == DataType.SO_End - 1);
                    }
                }
            }

            GUI.backgroundColor = new Color(1f, 0.8f, 0.8f);
            if (GUILayout.Button($"Export [All -> Google Sheets]", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Export Confirmation", $"정말로 현재 SO 데이터 전체를 Google Sheets에 덮어쓰시겠습니까? 이 작업은 되돌릴 수 없습니다.", "예", "아니오"))
                {
                    for (var dataType = DataType.SO_Start + 1; dataType < DataType.SO_End; dataType++)
                    {
                        selectedDataType = dataType;
                        string allSheetId = GetSheetIdForSelectedType();
                        if (GoogleSheetUtility.IsReady(allSheetId))
                        {
                            if (dataType == DataType.ConstData)
                                ExportConstDataSheet(allSheetId);
                            else
                                ExportSelectedSheet(allSheetId, dataType == DataType.SO_End - 1);
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

        }

        GUI.backgroundColor = Color.white;
    }

    private string GetSheetIdForSelectedType()
    {
        switch (selectedDataType)
        {
            case DataType.TableData:
                return tableDataSheetID;
            case DataType.StageData:
                return stageDataSheetID;
            case DataType.WeaponData:
                return weaponDataSheetID;
            case DataType.MonsterData:
                return monsterDataSheetID;
            case DataType.PlayerData:
                return playerDataSheetID;
            case DataType.WaveData:
                return waveDataSheetID;
            case DataType.ConstData:
                return constDataSheetID;
            default:
                return string.Empty;
        }
    }

    private bool IsOnlyImportMode()
    {
        return selectedDataType == DataType.TableData ||
               selectedDataType == DataType.StageData;
    }

    private void ImportSelectedSheet(string sheetID, bool showLog = true)
    {
        try
        {
            var service = GoogleSheetUtility.GetService(SheetsService.Scope.SpreadsheetsReadonly);
            var spreadsheet = service.Spreadsheets.Get(sheetID).Execute();

            string typeName = selectedDataType.ToString();

            // CSV 타입은 직접 CSV로 저장
            if (IsOnlyImportMode())
            {
                if (spreadsheet.Sheets.Count > 0)
                {
                    string firstSheetName = spreadsheet.Sheets[0].Properties.Title;
                    var request = service.Spreadsheets.Values.Get(sheetID, firstSheetName);
                    var response = request.Execute();

                    if (response.Values != null && response.Values.Count > 1)
                    {
                        ImportSheetDataToCSV(typeName, response.Values);
                    }
                }
            }
            else
            {
                // ScriptableObject 타입들
                Type soType = FindType(typeName);

                if (soType == null)
                {
                    LogManager.LogError($"[Importer] Enum '{typeName}'에 해당하는 ScriptableObject 클래스를 찾을 수 없습니다.");
                    return;
                }

                if (spreadsheet.Sheets.Count > 0)
                {
                    string firstSheetName = spreadsheet.Sheets[0].Properties.Title;
                    var request = service.Spreadsheets.Values.Get(sheetID, firstSheetName);
                    var response = request.Execute();

                    if (response.Values != null && response.Values.Count > 1)
                    {
                        MethodInfo method = GetType().GetMethod(nameof(ImportSheetData), BindingFlags.NonPublic | BindingFlags.Instance);
                        MethodInfo genericMethod = method.MakeGenericMethod(soType);
                        var result = genericMethod.Invoke(this, new object[] { typeName, response.Values });
                        if (result is bool success && success == false)
                        {
                            if (showLog)
                                EditorUtility.DisplayDialog("Import Failed", "Google Sheets로부터 Import 실패 했습니다.", "OK");
                            return;
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
        // TextKey import 시 완전한 폰트 설정 자동 실행 (아틀라스 + Material 수정)
        if (selectedDataType == DataType.TableData)
        {
            Debug.Log("[CSVDataParser] TableData import 완료! 완전한 폰트 설정을 시작합니다...");
            FontAtlasAutoGenerator.CompleteFontSetup();
            Debug.Log("[CSVDataParser] 완전한 폰트 설정 완료!");
        }
            
            if (showLog)
                EditorUtility.DisplayDialog("Import Complete", $"'{typeName}' 데이터를 Google Sheets로 성공적으로 Import 했습니다.", "OK");
        }
        catch (Exception e) { HandleException(e); }
    }

    private void ImportSheetDataToCSV(string dataName, IList<IList<object>> values)
    {
        if (values == null || values.Count == 0)
        {
            LogManager.LogError($"[CSV Importer] '{dataName}' 시트에 데이터가 없습니다.");
            return;
        }

        // CSV 파일 경로
        string csvFileName = $"{dataName}.csv";
        string csvPath = Path.Combine(saveRootPath, csvFileName);

        // 디렉토리가 없으면 생성
        if (!Directory.Exists(saveRootPath))
            Directory.CreateDirectory(saveRootPath);

        // CSV 내용 생성
        var csvLines = new List<string>();
        
        foreach (var row in values)
        {
            var cells = row.Select(cell => 
            {
                string cellValue = cell?.ToString() ?? "";
                // CSV 형식: 값에 쉼표나 따옴표가 있으면 따옴표로 감싸고, 내부 따옴표는 두 개로
                if (cellValue.Contains(",") || cellValue.Contains("\"") || cellValue.Contains("\n"))
                {
                    cellValue = "\"" + cellValue.Replace("\"", "\"\"") + "\"";
                }
                return cellValue;
            });
            
            csvLines.Add(string.Join(",", cells));
        }

        // 파일 저장
        File.WriteAllText(csvPath, string.Join("\n", csvLines), System.Text.Encoding.UTF8);
        
        LogManager.Log($"[CSV Importer] '{dataName}' 데이터를 '{csvFileName}'로 저장했습니다.");
        AssetDatabase.Refresh();
    }

    private void ImportConstDataSheet(string sheetID)
    {
        try
        {
            // 1. 구글 시트 서비스 및 데이터 가져오기
            var service = GoogleSheetUtility.GetService(SheetsService.Scope.SpreadsheetsReadonly);
            var spreadsheet = service.Spreadsheets.Get(sheetID).Execute();
            if (spreadsheet.Sheets.Count == 0)
            {
                LogManager.LogError("[Importer] ConstData 시트를 찾을 수 없습니다.");
                return;
            }

            string firstSheetName = spreadsheet.Sheets[0].Properties.Title;
            var request = service.Spreadsheets.Values.Get(sheetID, firstSheetName);
            var response = request.Execute();
            var values = response.Values;
            if (values == null || values.Count < 1) // 헤더를 포함해 최소 1줄은 있어야 함
            {
                LogManager.LogWarning("[Importer] ConstData 시트에 데이터가 없습니다.");
                return;
            }

            // 2. 헤더 행을 찾아 필수 컬럼의 인덱스 확인
            const string keyColumnName = "CONST_NAME";
            const string valueColumnName = "VALUE";
            int headerRowIndex = -1;
            int keyIndex = -1;
            int valueIndex = -1;

            for (int i = 0; i < values.Count; i++)
            {
                var rowHeaders = values[i].Select(h => h.ToString().Trim()).ToArray();
                keyIndex = Array.IndexOf(rowHeaders, keyColumnName);
                valueIndex = Array.IndexOf(rowHeaders, valueColumnName);

                if (keyIndex != -1 && valueIndex != -1)
                {
                    headerRowIndex = i; // 헤더 행의 인덱스를 찾음
                    break;
                }
            }

            if (headerRowIndex == -1)
            {
                LogManager.LogError($"[Importer] ConstData 시트에서 필수 컬럼 '{keyColumnName}' 또는 '{valueColumnName}'을 찾을 수 없습니다.");
                EditorUtility.DisplayDialog("Import Failed", $"ConstData 시트에서 필수 컬럼 '{keyColumnName}' 또는 '{valueColumnName}'을 찾을 수 없습니다.", "OK");
                return;
            }

            // 3. ConstData ScriptableObject 에셋 로드 또는 생성
            string assetName = nameof(ConstData);
            string assetPath = Path.Combine(soRootPath, $"{assetName}.asset");
            ConstData constDataAsset = AssetDatabase.LoadAssetAtPath<ConstData>(assetPath);

            if (constDataAsset == null)
            {
                constDataAsset = CreateInstance<ConstData>();
                if (!Directory.Exists(soRootPath))
                {
                    Directory.CreateDirectory(soRootPath);
                }
                AssetDatabase.CreateAsset(constDataAsset, assetPath);
                LogManager.Log($"[Importer] '{assetPath}' 에셋을 새로 생성했습니다.");
            }

            // 4. ConstData 클래스의 모든 public 필드 정보를 미리 가져오기
            var soFields = typeof(ConstData).GetFields(BindingFlags.Public | BindingFlags.Instance).ToDictionary(f => f.Name, f => f);

            // 5. 데이터 행을 순회하며 에셋에 값 채우기 (헤더 다음 줄부터 시작)
            for (int i = headerRowIndex + 1; i < values.Count; i++)
            {
                var row = values[i];

                // 키(CONST_NAME)가 비어있거나 주석(;)으로 시작하는 행은 건너뜀
                if (row.Count <= keyIndex || string.IsNullOrWhiteSpace(row[keyIndex].ToString()) || row[keyIndex].ToString().Trim().StartsWith(";"))
                    continue;

                string key = row[keyIndex].ToString().Trim();
                string val = (row.Count > valueIndex) ? row[valueIndex].ToString().Trim() : "";

                if (soFields.TryGetValue(key, out FieldInfo field))
                {
                    try
                    {
                        // 다른 임포트 함수와 동일한 ConvertValue 메소드를 사용
                        var convertedValue = ConvertValue(val, field.FieldType);
                        field.SetValue(constDataAsset, convertedValue);
                    }
                    catch (Exception e)
                    {
                        LogManager.LogWarning($"[Importer] ConstData 값 변환 실패. Key: '{key}', Value: '{val}'. 에러: {e.Message}");
                    }
                }
                else
                {
                    // 시트에 정의된 키가 실제 코드(ConstData.cs)에 없을 경우 경고
                    LogManager.LogWarning($"[Importer] ConstData.cs에 '{key}' 이름의 필드가 없습니다. 시트의 값을 무시합니다.");
                }
            }

            // 6. 변경사항 저장 및 완료 메시지 표시
            EditorUtility.SetDirty(constDataAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Import Complete", $"'{assetName}.asset' 업데이트를 완료했습니다.", "OK");
        }
        catch (Exception e)
        {
            HandleException(e); // 예외 처리 로직 통일
        }
    }

    private void ExportConstDataSheet(string sheetID)
    {
        try
        {
            // 1. 서비스 가져오기 및 로컬 에셋 로드
            var service = GoogleSheetUtility.GetService(SheetsService.Scope.Spreadsheets);
            string assetName = nameof(ConstData);
            string assetPath = Path.Combine(soRootPath, $"{assetName}.asset");
            ConstData constDataAsset = AssetDatabase.LoadAssetAtPath<ConstData>(assetPath);

            if (constDataAsset == null)
            {
                LogManager.LogError($"[Exporter] 에셋 파일을 찾을 수 없습니다: {assetPath}");
                EditorUtility.DisplayDialog("Export Failed", $"'{assetPath}'\n에셋 파일을 찾을 수 없습니다. 먼저 Import를 진행해주세요.", "OK");
                return;
            }

            // 2. 시트 정보 및 기존 데이터 전체 읽기
            var spreadsheet = service.Spreadsheets.Get(sheetID).Execute();
            if (spreadsheet.Sheets == null || spreadsheet.Sheets.Count == 0)
            {
                LogManager.LogError($"[Exporter] 시트 ID '{sheetID}'에 해당하는 스프레드시트를 찾을 수 없거나 시트가 비어있습니다.");
                return;
            }
            string targetTabName = spreadsheet.Sheets[0].Properties.Title;
            var existingValuesResponse = service.Spreadsheets.Values.Get(sheetID, targetTabName).Execute();
            var existingSheetRows = existingValuesResponse.Values ?? new List<IList<object>>();

            // 3. 로컬 에셋 데이터를 Dictionary로 변환 (업데이트 및 추가 확인용)
            var localDataMap = typeof(ConstData).GetFields(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(
                    field => field.Name,
                    field => {
                        var value = field.GetValue(constDataAsset);
                        return value is Color colorValue ? ColorUtility.ToHtmlStringRGB(colorValue) : value?.ToString() ?? "";
                    });

            // 4. 시트에 쓸 새로운 데이터 리스트 생성 (기존 데이터 기반으로 업데이트)
            var newSheetRows = new List<IList<object>>();
            bool headerFound = false;

            foreach (var row in existingSheetRows)
            {
                // 빈 행은 그대로 유지
                if (row.Count == 0 || string.IsNullOrWhiteSpace(row[0]?.ToString()))
                {
                    newSheetRows.Add(row);
                    continue;
                }

                string key = row[0].ToString().Trim();

                // 헤더, 주석, 로컬 에셋에 없는 데이터는 그대로 유지
                if (key == "CONST_NAME" || key.StartsWith(";") || !localDataMap.ContainsKey(key))
                {
                    newSheetRows.Add(row);
                    if (key == "CONST_NAME")
                        headerFound = true;
                    continue;
                }

                // 로컬 에셋에 있는 데이터면 값을 업데이트
                string newValue = localDataMap[key];
                var updatedRow = new List<object> { key, newValue };

                // 기존 행의 3번째 열부터의 데이터를 보존 (사용자가 추가한 노트 등)
                if (row.Count > 2)
                {
                    updatedRow.AddRange(row.Skip(2));
                }
                newSheetRows.Add(updatedRow);

                // 처리된 데이터는 맵에서 제거
                localDataMap.Remove(key);
            }

            // 5. 시트에 헤더가 없었다면, 맨 위에 추가
            if (!headerFound && newSheetRows.Count > 0)
            {
                newSheetRows.Insert(0, new List<object> { "CONST_NAME", "VALUE" });
            }
            else if (!headerFound) // 시트가 완전히 비어있었다면
            {
                newSheetRows.Add(new List<object> { "CONST_NAME", "VALUE" });
            }

            //// 6. 로컬에만 있는 새로운 데이터는 맨 아래에 추가
            //if (localDataMap.Count > 0)
            //{
            //    // 새 데이터를 추가하기 전에 구분을 위한 빈 줄 추가
            //    if (newSheetRows.Count > 0 && newSheetRows.Last().Any(c => !string.IsNullOrWhiteSpace(c?.ToString())))
            //    {
            //        newSheetRows.Add(new List<object>()); // Add an empty row for spacing
            //    }
            //    newSheetRows.Add(new List<object> { "; ----- New Data From Local -----" });
            //    foreach (var newData in localDataMap)
            //    {
            //        newSheetRows.Add(new List<object> { newData.Key, newData.Value });
            //    }
            //}

            // 7. 시트 업데이트
            var valueRange = new ValueRange { Values = newSheetRows };
            var clearRequest = service.Spreadsheets.Values.Clear(new ClearValuesRequest(), sheetID, targetTabName);
            clearRequest.Execute();

            if (newSheetRows.Count > 0)
            {
                var updateRequest = service.Spreadsheets.Values.Update(valueRange, sheetID, $"{targetTabName}!A1");
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                updateRequest.Execute();
            }

            LogManager.Log($"[Exporter] '{assetName}' 데이터를 '{targetTabName}' 탭으로 익스포트 완료! (주석 및 기존 데이터 유지)");
            EditorUtility.DisplayDialog("Export Complete", $"'{assetName}' 데이터를 Google Sheets로 성공적으로 Export 했습니다.", "OK");
        }
        catch (Exception e)
        {
            HandleException(e);
        }
    }


    private bool ImportSheetData<T_SO>(string dataName, IList<IList<object>> values) where T_SO : ScriptableObject, IDataID
    {
        string soFolderPath = Path.Combine(soRootPath, dataName);
        if (!Directory.Exists(soFolderPath))
            Directory.CreateDirectory(soFolderPath);

        var allAssets = AssetDatabase.FindAssets($"t:{typeof(T_SO).Name}", new[] { soFolderPath })
            .Select(guid => AssetDatabase.LoadAssetAtPath<T_SO>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(asset => asset != null)
            .ToList();

        var duplicateGroups = allAssets.GroupBy(asset => asset.ID)
                                       .Where(group => group.Count() > 1)
                                       .ToList();

        if (duplicateGroups.Any())
        {
            foreach (var group in duplicateGroups)
            {
                var filePaths = group.Select(asset => AssetDatabase.GetAssetPath(asset));
                LogManager.LogError($"[Importer] ID 중복 오류! ID '{group.Key}'가 다음 에셋들에서 중복으로 사용되었습니다:\n {string.Join("\n", filePaths)}");
            }
            EditorUtility.DisplayDialog("Import Error", "프로젝트에 ID가 중복된 데이터 에셋이 있습니다. Console 창을 확인하여 중복된 파일을 수정해주세요.", "OK");
            return false;
        }

        var existingAssets = allAssets.ToDictionary(asset => asset.ID, asset => asset);
        string[] headers;
        int objectNameColumnIndex;
        do
        {
            headers = values[0].Select(h => h.ToString()).ToArray();
            objectNameColumnIndex = Array.IndexOf(headers, "ObjectName");
            if (objectNameColumnIndex == -1)
            {
                values.RemoveAt(0);
                if (values.Count == 0)
                {
                    LogManager.LogError($"[Importer] '{dataName}' 시트에 파일명으로 사용할 'ObjectName' 컬럼이 없습니다.");
                    return false;
                }
            }

        }
        while (objectNameColumnIndex == -1);


        FieldInfo idField = null;
        var potentialIdFields = typeof(T_SO).GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.Name.EndsWith("ID", StringComparison.OrdinalIgnoreCase) && f.FieldType == typeof(int))
            .ToList();

        if (potentialIdFields.Count == 1)
        {
            idField = potentialIdFields[0];
        }
        else if (potentialIdFields.Count > 1)
        {
            LogManager.LogError($"[Importer] {typeof(T_SO).Name} 클래스에 'ID'로 끝나는 필드가 여러 개 있어 어떤 것을 고유 ID로 사용할지 알 수 없습니다. ({string.Join(", ", potentialIdFields.Select(f => f.Name))})");
            return false;
        }
        else
        {
            LogManager.LogError($"[Importer] {typeof(T_SO).Name} 클래스에서 'ID'로 끝나는 정수(int) 타입의 ID 필드를 찾을 수 없습니다.");
            return false;
        }

        int idColumnIndex = Array.IndexOf(headers, idField.Name);
        if (idColumnIndex == -1)
        {
            LogManager.LogError($"[Importer] '{dataName}' 시트에 고유 식별자로 사용할 '{idField.Name}' 컬럼이 없습니다.");
            return false;
        }

        var soFields = typeof(T_SO).GetFields(BindingFlags.Public | BindingFlags.Instance).ToDictionary(f => f.Name, f => f);

        for (int i = 1; i < values.Count; i++)
        {
            var row = values[i];

            var idCellObject = row.Count > idColumnIndex ? row[idColumnIndex] : null;
            var nameCellObject = row.Count > objectNameColumnIndex ? row[objectNameColumnIndex] : null;

            if (idCellObject == null || nameCellObject == null ||
                string.IsNullOrWhiteSpace(idCellObject.ToString()) ||
                nameCellObject.ToString().Trim().StartsWith(";"))
            {
                continue;
            }

            if (!int.TryParse(idCellObject.ToString(), out int currentId))
            {
                LogManager.LogWarning($"[Importer] ID 값을 정수로 변환할 수 없습니다. Row: {i + 1}, Value: {idCellObject}");
                continue;
            }

            string objectName = nameCellObject.ToString().Trim();
            T_SO so_asset;

            if (existingAssets.TryGetValue(currentId, out so_asset))
            {
                existingAssets.Remove(currentId);
                string existingPath = AssetDatabase.GetAssetPath(so_asset);
                if (Path.GetFileNameWithoutExtension(existingPath) != objectName)
                {
                    AssetDatabase.RenameAsset(existingPath, objectName);
                    LogManager.Log($"[Importer] 파일 이름 변경: '{Path.GetFileName(existingPath)}' -> '{objectName}.asset'");
                }
            }
            else
            {
                so_asset = CreateInstance<T_SO>();
                string assetPath = Path.Combine(soFolderPath, $"{objectName}.asset");
                AssetDatabase.CreateAsset(so_asset, assetPath);
            }

            for (int j = 0; j < headers.Length; j++)
            {
                string header = headers[j];
                if (soFields.TryGetValue(header, out FieldInfo field))
                {
                    try
                    {
                        string cellValue = j >= row.Count ? "" : row[j].ToString();

                        if (field.FieldType != typeof(string))
                        {
                            int commentIndex = cellValue.IndexOf(';');
                            if (commentIndex != -1)
                                cellValue = cellValue.Substring(0, commentIndex);
                        }

                        var convertedValue = ConvertValue(cellValue, field.FieldType);
                        field.SetValue(so_asset, convertedValue);
                    }
                    catch (Exception e) { LogManager.LogWarning($"[Importer] 데이터 변환 실패: {objectName} -> {header}. 에러: {e.Message}"); }
                }
            }
            EditorUtility.SetDirty(so_asset);
        }

        if (existingAssets.Count > 0)
        {
            if (EditorUtility.DisplayDialog("오래된 에셋 삭제",
                $"시트에 존재하지 않는 {existingAssets.Count}개의 에셋이 '{dataName}' 폴더에 있습니다. 삭제하시겠습니까?", "예, 삭제합니다", "아니오"))
            {
                foreach (var assetToDelete in existingAssets.Values)
                {
                    string path = AssetDatabase.GetAssetPath(assetToDelete);
                    AssetDatabase.DeleteAsset(path);
                    LogManager.Log($"[Importer] 오래된 에셋 삭제: {path}");
                }
            }
        }

        return true;
    }

    private void ExportSelectedSheet(string sheetID, bool showLog = true)
    {
        try
        {
            var service = GoogleSheetUtility.GetService(SheetsService.Scope.Spreadsheets);
            string typeName = selectedDataType.ToString();
            Type soType = FindType(typeName);

            if (soType == null)
            {
                LogManager.LogError($"[Exporter] Enum '{typeName}'에 해당하는 ScriptableObject 클래스를 찾을 수 없습니다.");
                return;
            }

            MethodInfo method = GetType().GetMethod(nameof(ExportSheetData), BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo genericMethod = method.MakeGenericMethod(soType);
            var result = genericMethod.Invoke(this, new object[] { service, typeName, sheetID });
            if (result is bool success && success == false)
            {
                if (showLog)
                    EditorUtility.DisplayDialog("Export Failed", "Google Sheets로 Export 실패 했습니다.", "OK");
                return;
            }

            if (showLog)
                EditorUtility.DisplayDialog("Export Complete", $"'{typeName}' 데이터를 Google Sheets로 성공적으로 Export 했습니다.", "OK");
        }
        catch (Exception e) { HandleException(e); }
    }

    private bool ExportSheetData<T_SO>(SheetsService service, string dataName, string sheetID) where T_SO : ScriptableObject, IDataID
    {
        // 1. 로컬 에셋 로드 및 ID를 키로 하는 딕셔너리 생성
        var soAssets = AssetDatabase.FindAssets($"t:{typeof(T_SO).Name}", new[] { Path.Combine(soRootPath, dataName) })
            .Select(guid => AssetDatabase.LoadAssetAtPath<T_SO>(AssetDatabase.GUIDToAssetPath(guid)))
            .ToList();

        var localDataMap = soAssets.ToDictionary(asset => asset.ID, asset => asset);
        var soFields = typeof(T_SO).GetFields(BindingFlags.Public | BindingFlags.Instance).ToDictionary(f => f.Name, f => f);

        // 2. 시트 정보 및 기존 데이터 전체 읽기
        var spreadsheet = service.Spreadsheets.Get(sheetID).Execute();
        if (spreadsheet.Sheets == null || spreadsheet.Sheets.Count == 0)
        {
            LogManager.LogError($"[Exporter] 시트 ID '{sheetID}'에 해당하는 스프레드시트를 찾을 수 없거나 시트가 비어있습니다.");
            return false;
        }
        string targetTabName = spreadsheet.Sheets[0].Properties.Title;
        var existingValuesResponse = service.Spreadsheets.Values.Get(sheetID, targetTabName).Execute();
        var existingSheetRows = existingValuesResponse.Values ?? new List<IList<object>>();

        // 3. ID 필드 정보 찾기
        FieldInfo idField = GetIdField(typeof(T_SO));
        if (idField == null)
            return false; // GetIdField 내부에서 에러 로그 처리

        // 4. 헤더 행 및 ID 컬럼 인덱스 찾기
        string[] headers = null;
        int headerRowIndex = -1;
        int idColumnIndex = -1;

        for (int i = 0; i < existingSheetRows.Count; i++)
        {
            var row = existingSheetRows[i];
            if (row.Count == 0)
                continue;

            var currentHeaders = row.Select(h => h.ToString().Trim()).ToArray();
            int foundIdIndex = Array.IndexOf(currentHeaders, idField.Name);

            if (foundIdIndex != -1)
            {
                headers = currentHeaders;
                headerRowIndex = i;
                idColumnIndex = foundIdIndex;
                break;
            }
        }

        if (headerRowIndex == -1)
        {
            LogManager.LogError($"[Exporter] '{dataName}' 시트에서 ID 컬럼 '{idField.Name}'을 포함한 헤더를 찾을 수 없습니다.");
            return false;
        }

        // 5. 헤더 업데이트: 새 필드가 추가되었는지 확인
        var allSoFields = typeof(T_SO).GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.Name != "name") // Unity 기본 name 필드 제외
            .ToList();
        
        var missingHeaders = allSoFields
            .Where(f => f.Name != "ObjectName" && !headers.Contains(f.Name))
            .Select(f => f.Name)
            .ToList();
        
        // 새 헤더가 있으면 기존 헤더 행에 추가
        if (missingHeaders.Any())
        {
            LogManager.Log($"[Exporter] '{dataName}' 시트에 새 헤더 추가: {string.Join(", ", missingHeaders)}");
            headers = headers.Concat(missingHeaders).ToArray();
        }

        // 6. 시트에 쓸 새로운 데이터 리스트 생성 (기존 데이터 기반으로 업데이트)
        var newSheetRows = new List<IList<object>>();
        for (int i = 0; i < existingSheetRows.Count; i++)
        {
            var row = existingSheetRows[i];

            // 헤더 행까지는 업데이트된 헤더로 교체
            if (i == headerRowIndex)
            {
                // 헤더 행을 새 헤더로 업데이트
                var updatedHeaderRow = new List<object>(headers);
                newSheetRows.Add(updatedHeaderRow);
                continue;
            }
            else if (i < headerRowIndex)
            {
                // 헤더 행 이전 (주석 등)은 그대로 유지
                newSheetRows.Add(row);
                continue;
            }

            // ID 값을 파싱할 수 없거나, 주석이거나, 빈 행이면 그대로 유지
            var idCell = row.Count > idColumnIndex ? row[idColumnIndex]?.ToString() : null;
            var firstCell = row.Count > 0 ? row[0]?.ToString() : null;

            if (string.IsNullOrWhiteSpace(idCell) || firstCell?.Trim().StartsWith(";") == true || !int.TryParse(idCell, out int currentId))
            {
                newSheetRows.Add(row);
                continue;
            }

            // 로컬 데이터에 ID가 존재하면, 행을 업데이트
            if (localDataMap.TryGetValue(currentId, out var asset))
            {
                var updatedRow = new List<object>(row);
                while (updatedRow.Count < headers.Length)
                    updatedRow.Add(""); // 행 길이 맞추기

                for (int j = 0; j < headers.Length; j++)
                {
                    string header = headers[j];
                    if (header == "ObjectName")
                    {
                        updatedRow[j] = asset.name;
                    }
                    else if (soFields.TryGetValue(header, out var field))
                    {
                        updatedRow[j] = ConvertFieldValueToString(field.GetValue(asset));
                    }
                }
                newSheetRows.Add(updatedRow);
                localDataMap.Remove(currentId); // 처리된 데이터는 맵에서 제거
            }
            else // 시트에는 있지만 로컬에는 없는 데이터 (삭제된 데이터) -> 새 헤더 길이에 맞춰 유지
            {
                var preservedRow = new List<object>(row);
                while (preservedRow.Count < headers.Length)
                    preservedRow.Add(""); // 새 헤더 컬럼 추가
                newSheetRows.Add(preservedRow);
            }
        }

        // 7. 로컬에만 있는 새로운 데이터는 맨 아래에 추가
        if (localDataMap.Any())
        {
            if (newSheetRows.Any() && newSheetRows.Last().Any(c => !string.IsNullOrWhiteSpace(c?.ToString())))
            {
                newSheetRows.Add(new List<object>()); // 구분선
            }
            newSheetRows.Add(new List<object> { "; ----- New Data From Local -----" });

            foreach (var asset in localDataMap.Values.OrderBy(a => a.ID))
            {
                var newRow = new object[headers.Length];
                for (int i = 0; i < headers.Length; i++)
                {
                    string header = headers[i];
                    if (header == "ObjectName")
                    {
                        newRow[i] = asset.name;
                    }
                    else if (soFields.TryGetValue(header, out var field))
                    {
                        newRow[i] = ConvertFieldValueToString(field.GetValue(asset));
                    }
                    else
                    {
                        newRow[i] = "";
                    }
                }
                newSheetRows.Add(newRow.ToList());
            }
        }

        // 8. 시트 업데이트
        var valueRange = new ValueRange { Values = newSheetRows };
        service.Spreadsheets.Values.Clear(new ClearValuesRequest(), sheetID, targetTabName).Execute();

        if (newSheetRows.Any())
        {
            var updateRequest = service.Spreadsheets.Values.Update(valueRange, sheetID, $"{targetTabName}!A1");
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            updateRequest.Execute();
        }

        LogManager.Log($"[Exporter] '{dataName}' 데이터를 '{targetTabName}' 탭으로 익스포트 완료! (주석 및 기존 데이터 유지)");
        return true;
    }

    private string ConvertFieldValueToString(object value)
    {
        if (value is GameObject go && go != null)
            return Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(go));
        if (value is Sprite sprite && sprite != null)
            return Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(sprite));
        if (value is Color colorValue)
            return ColorUtility.ToHtmlStringRGB(colorValue);

        return value?.ToString() ?? "";
    }

    private FieldInfo GetIdField(Type type)
    {
        var potentialIdFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.Name.EndsWith("ID", StringComparison.OrdinalIgnoreCase) && f.FieldType == typeof(int))
            .ToList();

        if (potentialIdFields.Count == 1)
        {
            return potentialIdFields[0];
        }

        if (potentialIdFields.Count > 1)
        {
            LogManager.LogError($"[Exporter] {type.Name} 클래스에 'ID'로 끝나는 필드가 여러 개 있어 어떤 것을 고유 ID로 사용할지 알 수 없습니다. ({string.Join(", ", potentialIdFields.Select(f => f.Name))})");
        }
        else
        {
            LogManager.LogError($"[Exporter] {type.Name} 클래스에서 'ID'로 끝나는 정수(int) 타입의 ID 필드를 찾을 수 없습니다.");
        }
        return null;
    }


    private Type FindType(string typeName)
    {
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name == typeName &&
                        t.IsSubclassOf(typeof(ScriptableObject)) &&
                        !t.IsAbstract &&
                        !t.Assembly.FullName.Contains("Editor"))
            .ToList();

        if (types.Count == 1)
            return types[0];
        if (types.Count > 1)
        {
            LogManager.LogError($"[Type Finder] '{typeName}'이라는 이름의 SO 클래스가 여러 개 발견되었습니다.");
            return null;
        }
        return null;
    }

    private object ConvertValue(string value, Type targetType)
    {
        if (targetType == typeof(GameObject))
        {
            string trimmedValue = value.Trim();
            if (string.IsNullOrEmpty(trimmedValue))
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            var go = AssetDatabase.LoadAssetAtPath<GameObject>(string.Format(ResourcesPath.WEAPON_PREFABS_PATH, trimmedValue));
            if (go.IsNull())
                go = AssetDatabase.LoadAssetAtPath<GameObject>(string.Format(ResourcesPath.PLAYER_PREFABS_PATH, trimmedValue));
            if (go.IsNull())
                go = AssetDatabase.LoadAssetAtPath<GameObject>(string.Format(ResourcesPath.MONSTER_PREFABS_PATH, trimmedValue));

            return go;
        }

        else if(targetType == typeof(Sprite))
        {
            string trimmedValue = value.Trim();
            if (string.IsNullOrEmpty(trimmedValue))
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(trimmedValue);
            return sprite;
        }

        return CSVParser.ConvertValue(value, targetType);
    }

    private void DownloadAllSheetsInFolder(string targetFolderId)
    {
        try
        {
            var credential = GoogleSheetUtility.GetCredential(new[]
            {
                DriveService.Scope.DriveReadonly,
                SheetsService.Scope.SpreadsheetsReadonly
            });

            var driveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Unity Drive Downloader"
            });

            var sheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Unity Sheets Downloader"
            });

            // 폴더 안 파일 검색 (공유 드라이브/공유 폴더도 포함)
            var request = driveService.Files.List();
            request.Q = $"'{targetFolderId}' in parents and trashed=false";
            request.Fields = "files(id, name, mimeType)";
            request.PageSize = 200;
            request.IncludeItemsFromAllDrives = true;
            request.SupportsAllDrives = true;

            var result = request.Execute();

            if (result.Files == null || result.Files.Count == 0)
            {
                EditorUtility.DisplayDialog("Info",
                    "해당 구글 드라이브 폴더에 시트 파일이 없습니다.",
                    "OK");
                return;
            }

            // 구글 시트 문서만 필터링
            var sheetsFiles = result.Files
                .Where(f => f.MimeType == "application/vnd.google-apps.spreadsheet")
                .ToList();

            if (sheetsFiles.Count == 0)
            {
                EditorUtility.DisplayDialog("Info",
                    "폴더에는 파일이 있지만 Google Sheets 문서는 없습니다.",
                    "OK");
                return;
            }

            LogManager.Log($"[Downloader] {sheetsFiles.Count}개의 시트 파일을 다운로드합니다...");

            foreach (var file in sheetsFiles)
                DownloadAndSaveSheet(driveService, file.Id, file.Name);

            // NotificationInfo 전용 시트도 다운로드
            DownloadAndSaveSheet(driveService, notificationInfoSheetID, "NOTIFICATION_INFO");

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Download Complete",
                $"{sheetsFiles.Count}개의 CSV 파일이\n{saveRootPath} 폴더에 저장되었습니다.",
                "OK");
        }
        catch (Exception e)
        {
            LogManager.LogError($"[Downloader] 에러 발생: {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("Download Failed",
                "에러가 발생했습니다. Console 창을 확인해주세요.",
                "OK");
        }
    }

    private void DownloadAndSaveSheet(DriveService driveService, string sheetID, string fileName)
    {
        try
        {
            var exportRequest = driveService.Files.Export(sheetID, "text/csv");
            using (var stream = new MemoryStream())
            {
                exportRequest.Download(stream);

                if (!Directory.Exists(saveRootPath))
                    Directory.CreateDirectory(saveRootPath);

                string csvFileName = Path.GetFileNameWithoutExtension(fileName);
                string savePath = Path.Combine(saveRootPath, $"{csvFileName}.csv");

                File.WriteAllBytes(savePath, stream.ToArray());

                LogManager.Log($"[Downloader] '{savePath}' 저장 완료 (Export API).");
            }
        }
        catch (Exception e)
        {
            LogManager.LogError($"[Downloader] 시트 '{fileName}' CSV Export 실패: {e.Message}");
        }
    }

    private void HandleException(Exception e)
    {
        LogManager.LogError($"[Data Manager] 에러 발생: {e.Message}\n{e.StackTrace}");
        EditorUtility.DisplayDialog("Request Failed", "에러가 발생했습니다. Console 창을 확인해주세요.", "OK");
    }

}
