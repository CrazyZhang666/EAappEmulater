using System.Runtime.InteropServices;
using System.Text;
using System.IO;

namespace EAappEmulater.Helper;

public static class IniHelper
{
    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

    /// <summary>
    /// 读取节点值
    /// </summary>
    private static string ReadValue(string section, string key, string iniPath)
    {
        var strBuilder = new StringBuilder(1024);
        _ = GetPrivateProfileString(section, key, string.Empty, strBuilder, strBuilder.Capacity, iniPath);
        return strBuilder.ToString();
    }

    /// <summary>
    /// 写入节点值
    /// </summary>
    private static void WriteValue(string section, string key, string value, string iniPath)
    {
        WritePrivateProfileString(section, key, value, iniPath);
    }

    /// <summary>
    /// 删除键（若 value 为 null，WritePrivateProfileString 会删除键）
    /// 如果系统调用未能删除（某些情况下可能只清空了值），作为回退，我们手动编辑 INI 文件以移除键行，并在节为空时移除节头。
    /// </summary>
    public static void DeleteKey(string section, string key, string iniPath)
    {
        try
        {
            // First attempt using Win32 API (should remove the key when passing null)
            WritePrivateProfileString(section, key, null, iniPath);

            // If file doesn't exist there's nothing more to do
            if (!File.Exists(iniPath)) return;

            // Read file and check whether the key still exists under the section
            var lines = File.ReadAllLines(iniPath).ToList();
            bool modified = false;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("[") && line.EndsWith("]") && string.Equals(line, "[" + section + "]", StringComparison.OrdinalIgnoreCase))
                {
                    // Found the section; remove any matching key lines until next section
                    int j = i + 1;
                    bool anyKeyRemoved = false;
                    while (j < lines.Count && !(lines[j].TrimStart().StartsWith("[") && lines[j].TrimEnd().EndsWith("]")))
                    {
                        var cur = lines[j];
                        var idx = cur.IndexOf('=');
                        if (idx > 0)
                        {
                            var k = cur.Substring(0, idx).Trim();
                            if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                            {
                                lines.RemoveAt(j);
                                anyKeyRemoved = true;
                                modified = true;
                                continue; // don't increment j, next line shifted into j
                            }
                        }
                        j++;
                    }

                    // If we removed some keys and the section now contains no key/value lines, remove the section header as well
                    if (anyKeyRemoved)
                    {
                        // Check if the section has any non-empty, non-comment lines left
                        int k = i + 1;
                        bool hasContent = false;
                        while (k < lines.Count && !(lines[k].TrimStart().StartsWith("[") && lines[k].TrimEnd().EndsWith("]")))
                        {
                            if (!string.IsNullOrWhiteSpace(lines[k]) && !lines[k].TrimStart().StartsWith(";")) { hasContent = true; break; }
                            k++;
                        }
                        if (!hasContent)
                        {
                            lines.RemoveAt(i); // remove section header
                            modified = true;
                        }
                    }

                    break; // done
                }
            }

            if (modified)
            {
                File.WriteAllLines(iniPath, lines);
            }
        }
        catch
        {
            // Swallow exceptions to keep behavior non-fatal - callers will log if needed
        }
    }

    #region 读取操作
    /// <summary>
    /// 读取字符串
    /// </summary>
    public static string ReadString(string section, string key, string iniPath)
    {
        return ReadValue(section, key, iniPath);
    }

    /// <summary>
    /// 读取布尔值
    /// </summary>
    public static bool ReadBoolean(string section, string key, string iniPath)
    {
        var value = ReadValue(section, key, iniPath);
        return value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 读取整数值
    /// </summary>
    public static int ReadInt(string section, string key, string iniPath)
    {
        var value = ReadValue(section, key, iniPath);

        if (int.TryParse(value, out int result))
            return result;

        return default;
    }
    #endregion

    #region 写入操作
    /// <summary>
    /// 写入字符串
    /// </summary>
    public static void WriteString(string section, string key, string value, string iniPath)
    {
        WriteValue(section, key, value, iniPath);
    }

    /// <summary>
    /// 写入布尔值
    /// </summary>
    public static void WriteBoolean(string section, string key, bool value, string iniPath)
    {
        WriteValue(section, key, value ? "true" : "false", iniPath);
    }

    /// <summary>
    /// 写入整数值
    /// </summary>
    public static void WriteInt(string section, string key, int value, string iniPath)
    {
        WriteValue(section, key, $"{value}", iniPath);
    }
    #endregion
}
