using System.Windows;
namespace ChichiCapture;
public partial class App : Application
{
 protected override void OnStartup(StartupEventArgs e){base.OnStartup(e);new MainWindow().Show();}
}
