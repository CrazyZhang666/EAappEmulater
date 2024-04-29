namespace BF1ModTools.Helper;

public static class JsonHelper
{
    /// <summary>
    /// 反序列化配置
    /// </summary>
    private static readonly JsonSerializerOptions OptionsDeserialize = new()
    {
        IncludeFields = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// 序列化配置
    /// </summary>
    private static readonly JsonSerializerOptions OptionsSerialize = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    /// <summary>
    /// 反序列化，将json字符串转换成json类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="result"></param>
    /// <returns></returns>
    public static T JsonDeserialize<T>(string result)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(result, OptionsDeserialize);
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("反序列化发生异常", ex);
            return default;
        }
    }

    /// <summary>
    /// 序列化，将json类转换成json字符串
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="jsonClass"></param>
    /// <returns></returns>
    public static string JsonSerialize<T>(T jsonClass)
    {
        try
        {
            return JsonSerializer.Serialize(jsonClass, OptionsSerialize);
        }
        catch (Exception ex)
        {
            LoggerHelper.Error("序列化发生异常", ex);
            return default;
        }
    }
}
