#region

using CurveTool.View;
using CurveTool.ViewModel;
using System.Windows;

#endregion

namespace CurveTool;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        var mainView = new MainView();
        mainView.DataContext = new MainViewModel();
        mainView!.Show();
    }
}