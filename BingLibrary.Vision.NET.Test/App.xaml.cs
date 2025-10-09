using BingLibrary.Vision.NET.Test.Views;
using BingLibraryLite.Log.Services;
using Prism.Ioc;
using Serilog;
using System.Windows;

namespace BingLibrary.Vision.NET.Test
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            LogService.Initialize();
            base.OnStartup(e);
        }

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance<ILogger>(LogService.Logger);
             }
    }
}