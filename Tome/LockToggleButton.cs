using System.Windows.Controls.Primitives;

namespace Tome;

public sealed class LockToggleButton : ToggleButton
{
    public bool IsLocked { get; set; }

    public void LockOn(bool isChecked)
    {
        IsChecked = isChecked;
        IsLocked = true;
    }

    public void LockOff(bool isChecked)
    {
        IsChecked = isChecked;
        IsLocked = false;
    }

    protected override void OnToggle()
    {
        if (IsLocked)
            return;

        base.OnToggle();
    }
}
