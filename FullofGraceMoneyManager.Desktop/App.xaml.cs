using System.Windows;
using FullofGraceMoneyManager.Desktop.Data;

namespace FullofGraceMoneyManager.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DatabaseInitializer.Initialize();
    }
}
