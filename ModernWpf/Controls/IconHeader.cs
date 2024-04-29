﻿namespace ModernWpf.Controls;

public class IconHeader : Control
{
    /// <summary>
    /// 字体图标
    /// </summary>
    public string Icon
    {
        get { return (string)GetValue(IconProperty); }
        set { SetValue(IconProperty, value); }
    }
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register("Icon", typeof(string), typeof(IconHeader), new PropertyMetadata(default));

    /// <summary>
    /// 标题文字
    /// </summary>
    public string Title
    {
        get { return (string)GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register("Title", typeof(string), typeof(IconHeader), new PropertyMetadata(default));
}
