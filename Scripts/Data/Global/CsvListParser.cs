using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class CsvListParser : Attribute
{
    public string Prefix { get; }

    public CsvListParser(string prefix)
    {
        Prefix = prefix;
    }
}
