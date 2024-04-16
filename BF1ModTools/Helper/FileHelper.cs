namespace BF1ModTools.Helper;

public static class FileHelper
{
    private static readonly MD5 md5 = MD5.Create();

    /// <summary>
    /// 创建文件夹
    /// </summary>
    public static void CreateDirectory(string dirPath)
    {
        if (!Directory.Exists(dirPath))
            Directory.CreateDirectory(dirPath);
    }

    /// <summary>
    /// 获取嵌入资源流（自动添加前缀）
    /// </summary>
    public static Stream GetEmbeddedResourceStream(string resPath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream($"BF1ModTools.Assets.Files.{resPath}");
    }

    /// <summary>
    /// 获取嵌入资源文本内容
    /// </summary>
    public static string GetEmbeddedResourceText(string resPath)
    {
        var stream = GetEmbeddedResourceStream(resPath);
        if (stream is null)
            return string.Empty;

        return new StreamReader(stream).ReadToEnd();
    }

    /// <summary>
    /// 清空指定文件夹下的文件及文件夹
    /// </summary>
    public static void ClearDirectory(string dirPath)
    {
        try
        {
            var dir = new DirectoryInfo(dirPath);
            var fileInfo = dir.GetFileSystemInfos();

            foreach (var file in fileInfo)
            {
                if (file is DirectoryInfo)
                {
                    var subdir = new DirectoryInfo(file.FullName);
                    subdir.Delete(true);
                }
                else
                {
                    File.Delete(file.FullName);
                }
            }

            LoggerHelper.Info($"清空文件夹成功 {dirPath}");
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"清空文件夹异常 {dirPath}", ex);
        }
    }

    /// <summary>
    /// 获取文件MD5值
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static async Task<string> GetFileMD5(string filePath)
    {
        if (!File.Exists(filePath))
            return string.Empty;

        using var fileStream = File.OpenRead(filePath);
        var fileMD5 = await md5.ComputeHashAsync(fileStream);

        return BitConverter.ToString(fileMD5).Replace("-", "");
    }
}
