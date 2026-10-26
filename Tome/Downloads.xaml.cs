using System.Windows;
using Tome.Models;

namespace Tome;

public partial class Downloads : Window
{
    private readonly TomeManifest _manifest;
    private readonly ModActions _requestedActions;

    public Downloads(ModActions actions, TomeManifest manifest)
    {
        InitializeComponent();

        _manifest = manifest;
        _requestedActions = actions;
    }
}
