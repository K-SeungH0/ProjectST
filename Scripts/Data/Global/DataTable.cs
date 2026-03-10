using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public abstract class DataTable<TClass, TData>
    where TClass : class, new()
    where TData : new()
{
    private static TClass _instance;
    public static TClass Instance
    {
        get
        {
            if (_instance == null)
                _instance = new TClass();

            return _instance;
        }
    }

    protected List<TData> DataList { get; private set; } = new List<TData>();

    private Dictionary<string, List<string>> _rawSections;

    private string _fileName;
    public string FileName
    {
        get => _fileName;
        set
        {
            _fileName = value;
            Load();
        }
    }

    protected DataTable() { }

    public void Load()
    {
        TextAsset csvFile = Resources.Load<TextAsset>($"Table/{_fileName}");
        if (csvFile != null)
        {
            _rawSections = CSVParser.SplitBySection(csvFile.text);

            var allParsedSections = new Dictionary<string, List<TData>>();
            var section = _rawSections.FirstOrDefault();
            allParsedSections[section.Key] = CSVParser.ParseSectionContent<TData>(section.Value);

            if (allParsedSections.ContainsKey("Table"))
            {
                DataList = allParsedSections["Table"];
                allParsedSections.Remove("Table");
            }
            else if (allParsedSections.Count > 0)
                DataList = allParsedSections.Values.FirstOrDefault() ?? new List<TData>();

            OnComplete();
        }
        else
        {
            LogManager.LogError($"[DataTable] 파일을 찾을 수 없습니다: Resources/Table/{_fileName}.csv");
        }
    }

    protected List<TSection> ParseSection<TSection>(string sectionName) where TSection : new()
    {
        if (_rawSections.ContainsKey(sectionName))
        {
            return CSVParser.ParseSectionContent<TSection>(_rawSections[sectionName]);
        }
        return new List<TSection>();
    }

    protected void ParseConstSection<TConst>(string sectionName, TConst targetObject) where TConst : new()
    {
        if (_rawSections.ContainsKey(sectionName))
            CSVParser.ParseConstSection(_rawSections[sectionName], targetObject);
    }

    /// <summary> 마지막에 실행 해줘야함 </summary>
    protected virtual void OnComplete()
    {
        DataList.Clear();
        DataList = null;

        _rawSections.Clear();
        _rawSections = null;
    }
}