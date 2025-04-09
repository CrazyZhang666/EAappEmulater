namespace EAappEmulater.Helper;

public static class FileHelper
{
    /// <summary>
    /// 创建文件夹
    /// </summary>
    public static void CreateDirectory(string dirPath)
    {
        if (!Directory.Exists(dirPath))
            Directory.CreateDirectory(dirPath);
    }

    /// <summary>
    /// 创建文件
    /// </summary>
    public static void CreateFile(string filePath)
    {
        if (!File.Exists(filePath))
            File.Create(filePath).Close();
    }

    /// <summary>
    /// 创建文件
    /// </summary>
    public static void CreateFile(string dirPath, string fileName)
    {
        var path = Path.Combine(dirPath, fileName);

        if (!File.Exists(path))
            File.Create(path).Close();
    }

    /// <summary>
    /// 获取嵌入资源流（自动添加前缀）
    /// </summary>
    public static Stream GetEmbeddedResourceStream(string resPath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream($"EAappEmulater.Assets.Files.{resPath}");
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

            LoggerHelper.Info(I18nHelper.I18n._("Helper.FileHelper.ClearDirectorySuccess", dirPath));
        }
        catch (Exception ex)
        {
            LoggerHelper.Error(I18nHelper.I18n._("Helper.FileHelper.ClearDirectoryError", dirPath, ex));
        }
    }

    /// <summary>
    /// 从资源文件中抽取资源文件（默认覆盖源文件）
    /// </summary>
    public static void ExtractResFile(string resPath, string outputPath, bool isOverride = true)
    {
        // 如果输出文件存在，并且不覆盖文件，则退出
        if (!isOverride && File.Exists(outputPath))
            return;

        var stream = GetEmbeddedResourceStream(resPath);
        if (stream is null)
            return;

        BufferedStream inStream = null;
        FileStream outStream = null;

        try
        {
            inStream = new BufferedStream(stream);
            outStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);

            var buffer = new byte[1024];
            int length;

            while ((length = inStream.Read(buffer, 0, buffer.Length)) > 0)
                outStream.Write(buffer, 0, length);

            outStream.Flush();

            LoggerHelper.Info(I18nHelper.I18n._("Helper.FileHelper.ExtractResFileSuccess", outputPath));
        }
        catch (Exception ex)
        {
            LoggerHelper.Error(I18nHelper.I18n._("Helper.FileHelper.ExtractResFileError", outputPath, ex));
        }
        finally
        {
            outStream?.Close();
            inStream?.Close();
        }
    }
    /// <summary>
    /// 从游戏文件读取installerdata.xml版本号
    /// </summary>
    public static string GetGameVersion(string contentID)
    {
        try
        {
            string xmlFilePath = RegistryHelper.GetInstallDirByContentId(contentID) + "__Installer\\installerdata.xml";
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlFilePath);
            XmlNode gameVersionNode = doc.SelectSingleNode("//gameVersion");

            if (gameVersionNode != null && gameVersionNode.Attributes["version"] != null)
            {
                return gameVersionNode.Attributes["version"].Value;
            }
            else
            {
                return "1.0.0.0";
            }
        }
        catch (Exception)
        {

            return "1.0.0.0";
        }
    }
}
