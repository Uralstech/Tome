using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Tome;

public class Mod : INotifyPropertyChanged
{
    public required int Id { get; init; }
    public required string Type { get; init; } = string.Empty;
    public required string Name { get; init; } = string.Empty;
    public required string Author { get; init; } = string.Empty;
    public required int Likes { get; init; }

    public bool Installed
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanUpdate));
        }
    }

    public bool Update
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            OnPropertyChanged();
        }
    }

    public bool HasUpdate
    {
        get;
        set
        {
            if (value == field)
                return;

            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanUpdate));
        }
    }

    public bool CanUpdate => Installed && HasUpdate;

    private void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;
}
