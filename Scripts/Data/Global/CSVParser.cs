using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class CSVParser
{
    private static readonly Dictionary<Type, Dictionary<string, MemberInfo>> _memberCache = new Dictionary<Type, Dictionary<string, MemberInfo>>();
 
    public static Dictionary<string, List<string>> SplitBySection(string csvText)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        List<string> currentSectionLines = new List<string>();
        string currentSectionName = "Table"; // 섹션 없는 데이터를 위한 기본 키 이름

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine))
                continue;

            var firstColumn = trimmedLine.Split(',')[0].Trim();

            if (firstColumn.StartsWith("[") && firstColumn.EndsWith("]"))
            {
                if (currentSectionLines.Count > 0)
                {
                    result[currentSectionName] = currentSectionLines;
                }
                currentSectionName = firstColumn.Substring(1, firstColumn.Length - 2).Trim();
                currentSectionLines = new List<string>();
            }
            else
            {
                currentSectionLines.Add(line);
            }
        }

        if (currentSectionLines.Count > 0)
        {
            result[currentSectionName] = currentSectionLines;
        }

        return result;
    }

    public static Dictionary<string, List<T>> ParseBySection<T>(string csvText) where T : new()
    {
        var result = new Dictionary<string, List<T>>(StringComparer.OrdinalIgnoreCase);
        var rawSections = SplitBySection(csvText);

        foreach (var section in rawSections)
        {
            result[section.Key] = ParseSectionContent<T>(section.Value);
        }

        return result;
    }

    public static void ParseConstSection<T>(List<string> sectionLines, T targetObject) where T : new()
    {
        if (sectionLines == null)
            return;

        CacheMemberInfo<T>();
        var memberMap = _memberCache[typeof(T)];

        foreach (var line in sectionLines)
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";"))
                continue;

            var columns = trimmedLine.Split(',');
            if (columns.Length < 2 || string.IsNullOrWhiteSpace(columns[0]))
                continue;

            string key = columns[0].Trim();
            string value = columns[1].Trim();

            if (memberMap.TryGetValue(key, out MemberInfo member))
            {
                SetMemberValue(targetObject, member, value);
            }
        }
    }
    public static List<T> Parse<T>(string csvText) where T : new()
    {
        var lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        return ParseSectionContent<T>(lines);
    }

    public static List<T> ParseSectionContent<T>(List<string> sectionLines) where T : new()
    {
        var list = new List<T>();
        if (sectionLines.Count == 0)
            return list;

        int headerRowIndex = FindHeaderRowIndex(sectionLines.ToArray());
        if (headerRowIndex == -1)
            return list;

        CacheMemberInfo<T>();
        var memberMap = _memberCache[typeof(T)];
        var headers = ParseLine(sectionLines[headerRowIndex]);

        MemberInfo listMember = null;
        string listPrefix = null;
        foreach (var member in memberMap.Values)
        {
            var attribute = member.GetCustomAttribute<CsvListParser>();
            if (attribute != null)
            {
                listMember = member;
                listPrefix = attribute.Prefix;
                break;
            }
        }

        List<int> lvColumnIndices = new List<int>();
        if (listPrefix != null)
        {
            lvColumnIndices = headers
                .Select((header, index) => new { header, index })
                .Where(h => !string.IsNullOrEmpty(h.header) && h.header.StartsWith(listPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(h => h.index)
                .ToList();
        }

        for (int i = headerRowIndex + 1; i < sectionLines.Count; i++)
        {
            var values = ParseLine(sectionLines[i]);
            if (values.Count == 0 || string.IsNullOrWhiteSpace(values.FirstOrDefault()))
                continue;

            T entry = new T();
            for (int j = 0; j < headers.Count; j++)
            {
                // 헤더가 비어있거나 인덱스가 범위를 벗어나면 스킵
                if (string.IsNullOrWhiteSpace(headers[j]) || j >= values.Count)
                    continue;

                // 빈 값도 처리 (기본값으로 설정되도록)
                string cellValue = values[j];

                if (lvColumnIndices.Contains(j))
                {
                    // LV_ 열인 경우
                    if (listMember != null)
                    {
                        var listObject = GetMemberValue<T>(entry, listMember);
                        if (listObject is IList dynamicList)
                        {
                            try
                            {
                                var convertedValue = ConvertValue(cellValue, typeof(long));
                                dynamicList.Add(convertedValue);
                            }
                            catch (Exception e)
                            {
                                LogManager.LogError($"[CSVParser] LV_ value conversion failed. Value: '{cellValue}'. Error: {e.Message}");
                            }
                        }
                    }
                }
                else
                {
                    // LV_가 아닌 일반 열인 경우 (빈 값도 처리하여 기본값 설정)
                    if (memberMap.TryGetValue(headers[j], out MemberInfo member))
                    {
                        SetMemberValue(entry, member, cellValue);
                    }
                }
            }
            list.Add(entry);
        }
        return list;
    }

    public static object ConvertValue(string value, Type targetType)
    {
        if (targetType == typeof(string))
            return value;

        string trimmedValue = value.Trim();
        if (string.IsNullOrEmpty(trimmedValue))
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

        if (targetType.IsEnum)
            return Enum.Parse(targetType, trimmedValue, true);
        if (targetType == typeof(bool))
            return trimmedValue == "1" || trimmedValue.Equals("true", StringComparison.OrdinalIgnoreCase);
        if (targetType == typeof(Color))
        {
            if (ColorUtility.TryParseHtmlString("#" + trimmedValue, out Color color))
                return color;
            else
                LogManager.LogError($"[Converter] Color 변환 실패: '{trimmedValue}'는 유효한 HEX 코드가 아닙니다.");
        }

        return Convert.ChangeType(trimmedValue, targetType);
    }

    private static int FindHeaderRowIndex(string[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            var columns = ParseLine(lines[i]);
            if (columns.Count > 1 && !string.IsNullOrWhiteSpace(columns.FirstOrDefault()) && !int.TryParse(columns[0], out _))
            {
                return i;
            }
        }
        return -1;
    }

    private static List<string> ParseLine(string line)
    {
        int commentIndex = line.IndexOf(';');
        if (commentIndex != -1)
        {
            line = line.Substring(0, commentIndex);
        }
        return line.Split(',').Select(s => s.Trim()).ToList();
    }

    private static void CacheMemberInfo<T>() where T : new()
    {
        if (!_memberCache.ContainsKey(typeof(T)))
        {
            var members = new Dictionary<string, MemberInfo>(StringComparer.OrdinalIgnoreCase);
            var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
                members[field.Name] = field;

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite);
            foreach (var prop in properties)
                members[prop.Name] = prop;

            _memberCache[typeof(T)] = members;
        }
    }

    private static void SetMemberValue<T>(T targetObject, MemberInfo member, string value)
    {
        try
        {
            Type memberType = member is FieldInfo f ? f.FieldType : ((PropertyInfo)member).PropertyType;
            var convertedValue = ConvertValue(value, memberType);

            if (member is FieldInfo field)
                field.SetValue(targetObject, convertedValue);
            else if (member is PropertyInfo prop)
                prop.SetValue(targetObject, convertedValue, null);
        }
        catch (Exception e)
        {
            LogManager.LogError($"[CSVParser] 데이터 변환 실패. Member: '{member.Name}', Value: '{value}'. 에러: {e.Message}");
        }
    }
    private static object GetMemberValue<T>(T targetObject, MemberInfo member)
    {
        if (member is FieldInfo field)
            return field.GetValue(targetObject);
        if (member is PropertyInfo prop)
            return prop.GetValue(targetObject, null);
        return null;
    }

    /// <summary>
    /// 데어터 여러개 넣을 경우 Split 하기 위해 사용
    /// 구분은 '/'로 한다.
    /// </summary>
    public static List<long[]> SplitString(string str, int splitCount)
    {
        List<long[]> resultList = new List<long[]>();

        if (string.IsNullOrEmpty(str))
            return resultList;

        // 괄호 제거
        string content = str.Trim('(', ')');

        // '/' 기준으로 각 행(Row)을 분리
        string[] rows = content.Split('/');

        foreach (string row in rows)
        {
            // ':' 기준으로 각 요소(Element)를 분리
            string[] elements = row.Split(':');

            // 지정된 splitCount만큼의 크기를 가진 배열 생성
            long[] dataRow = new long[splitCount];

            for (int i = 0; i < splitCount; i++)
            {
                // 실제 데이터가 splitCount보다 적을 경우를 대비한 안전 처리
                if (i < elements.Length)
                {
                    long.TryParse(elements[i], out dataRow[i]);
                }
            }

            resultList.Add(dataRow);
        }

        return resultList;
    }
}