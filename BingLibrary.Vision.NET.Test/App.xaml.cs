using BingLibrary.Vision.NET.Test.Views;
using Prism.Ioc;
using System.Windows;

namespace BingLibrary.Vision.NET.Test
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {

        }
    }
}
