namespace BF1Chat.Helper;

public static class ChatHelper
{
    /// <summary>
    /// 美式键盘
    /// </summary>
    private static readonly CultureInfo EN_US = new("en-US");
    /// <summary>
    /// 微软拼音
    /// </summary>
    private static readonly CultureInfo ZH_CN = new("zh-CN");

    /// <summary>
    /// 设置输入法为美式键盘
    /// </summary>
    public static void SetInputLangENUS()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            InputLanguageManager.Current.CurrentInputLanguage = EN_US;
        });
    }

    /// <summary>
    /// 设置输入法为微软拼音
    /// </summary>
    public static void SetInputLangZHCN()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            InputLanguageManager.Current.CurrentInputLanguage = ZH_CN;
        });
    }
}
