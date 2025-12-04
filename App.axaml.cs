using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;  
using QuanLyXe03.ViewModels;
using QuanLyXe03.Views;
using ReactiveUI;            
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace QuanLyXe03
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            
            // Đảm bảo RxApp dùng đúng Scheduler của Avalonia trước khi bất kỳ ViewModel nào được tạo
            RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
            RxApp.TaskpoolScheduler = TaskPoolScheduler.Default;

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var loginWindow = new LoginWindow();
                desktop.MainWindow = loginWindow;
                loginWindow.Show();

                loginWindow.Closed += (s, e) =>
                {
                    bool success = loginWindow.Tag is bool b && b;

                    if (success)
                    {
                        var mainWindow = new MainWindow();
                        desktop.MainWindow = mainWindow;
                        mainWindow.Show();
                    }
                    else
                    {
                        desktop.Shutdown();
                    }
                };
            }

            // === GỌI HÀM NÀY SAU KHI ĐÃ SET SCHEDULER (nếu vẫn muốn disable validation) ===
            DisableAvaloniaDataAnnotationValidation();

            base.OnFrameworkInitializationCompleted();
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}