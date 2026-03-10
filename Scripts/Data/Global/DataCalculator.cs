public static class DataCalculator
{
    public static readonly float DATA_SCALE = 10000f;
    public static readonly int DATA_SCALE_UI = 100;
    public static readonly char[] ARRAY_SPLITS = new char[] { '(', ':', ')' };

	/// <summary> 시트에서 가져온 long 타입 데이터를 실제 게임 값(float)으로 변환합니다. </summary>
	public static float ToGameValue(long sheetValue)
    {
        return sheetValue / DATA_SCALE;
    }

    /// <summary> 시트에서 가져온 float 타입 데이터를 실제 게임 값(float)으로 변환합니다. </summary>
    public static float ToGameValue(float sheetValue)
    {
        return sheetValue / DATA_SCALE;
    }
}
