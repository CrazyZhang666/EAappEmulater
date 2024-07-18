namespace ModernWpf.Controls;

public class Loading : Control
{
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        UpdateVisualState();
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == IsVisibleProperty)
            UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (Template == null)
            return;

        var mainGrid = GetTemplateChild("MainGrid") as FrameworkElement;
        if (mainGrid == null)
            return;

        var targetState = IsVisible ? "Active" : "Inactive";
        VisualStateManager.GoToElementState(mainGrid, targetState, true);

        Debug.WriteLine(targetState);
    }
}
