using System;
using System.Collections.Generic;

public class DBHelper
{
    /// <summary>
    /// Dictionary에서 값을 안전하게 가져옵니다 (long/int/double 형변환 포함).
    /// </summary>
    public static T GetOrDefault<T>(Dictionary<string, object> dataMap, string key, T defaultValue = default)
    {
        try
        {
            if (dataMap.TryGetValue(key, out object value))
            {
                // Firestore는 숫자를 long이나 double로 반환할 수 있으므로, Convert로 안전하게 형변환
                return (T)Convert.ChangeType(value, typeof(T));
            }
        }
        catch (Exception ex)
        {
            LogManager.LogWarning($"[DBHelper] 필드 파싱 오류 (Key: {key}): {ex.Message}. 기본값 사용.");
        }
        return defaultValue;
    }

    /// <summary>
    /// Dictionary에서 문자열을 Enum으로 안전하게 변환합니다.
    /// </summary>
    public static TEnum GetEnumOrDefault<TEnum>(Dictionary<string, object> dataMap, string key, TEnum defaultValue = default) where TEnum : struct
    {
        if (dataMap.TryGetValue(key, out object value) && value is string strValue)
        {
            if (Enum.TryParse(strValue, out TEnum result))
            {
                return result;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// Dictionary에서 문자열을 Enum으로 안전하게 변환합니다 (오버로드).
    /// </summary>
    public static TEnum GetEnumOrDefault<TEnum>(string strValue, TEnum defaultValue = default) where TEnum : struct
    {
        if (Enum.TryParse(strValue, out TEnum result))
        {
            return result;
        }
        return defaultValue;
    }

    /// <summary>
    /// Firestore의 Map(Dict<string, object>)을 C# Dictionary<TKey, TValue>로 변환합니다.
    /// </summary>
    public static Dictionary<TKey, TValue> GetDictionary<TKey, TValue>(
        Dictionary<string, object> dataMap,
        string key,
        Func<string, object, TKey> keyConverter,
        Func<object, TValue> valueConverter)
    {
        var result = new Dictionary<TKey, TValue>();
        if (dataMap.TryGetValue(key, out object value) && value is Dictionary<string, object> map)
        {
            foreach (var kvp in map)
            {
                try
                {
                    result[keyConverter(kvp.Key, kvp.Value)] = valueConverter(kvp.Value);
                }
                catch (Exception ex)
                {
                    LogManager.LogWarning($"[DBHelper] 맵 파싱 중 오류 (Key: {key}, ItemKey: {kvp.Key}): {ex.Message}");
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Firestore의 Array(List<object>)를 C# HashSet<T>로 변환합니다.
    /// </summary>
    public static HashSet<T> GetListAsHashSet<T>(Dictionary<string, object> dataMap, string key)
    {
        var result = new HashSet<T>();
        if (dataMap.TryGetValue(key, out object value) && value is List<object> list)
        {
            foreach (var item in list)
            {
                try
                {
                    result.Add((T)Convert.ChangeType(item, typeof(T)));
                }
                catch (Exception ex)
                {
                    LogManager.LogWarning($"[DBHelper] 리스트 파싱 중 오류 (Key: {key}): {ex.Message}");
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Firestore의 Array(List<string>)를 C# HashSet<TEnum>로 변환합니다.
    /// </summary>
    public static HashSet<TEnum> GetListAsEnumHashSet<TEnum>(Dictionary<string, object> dataMap, string key) where TEnum : struct
    {
        var result = new HashSet<TEnum>();
        if (dataMap.TryGetValue(key, out object value) && value is List<object> list)
        {
            foreach (var item in list)
            {
                if (item is string strValue)
                {
                    result.Add(GetEnumOrDefault<TEnum>(strValue));
                }
            }
        }
        return result;
    }
}
