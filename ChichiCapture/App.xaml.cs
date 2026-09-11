using System.Windows;
namespace ChichiCapture;
public partial class App : System.Windows.Application
{
 protected override void OnStartup(StartupEventArgs e){base.OnStartup(e);new MainWindow().Show();}
}
