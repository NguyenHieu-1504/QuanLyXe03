using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using QuanLyXe03.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.IO;

namespace QuanLyXe03.Views
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel? _vm;
        private System.Threading.Timer? windowHiderTimer;

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public MainWindow()
        {
            InitializeComponent();

            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
            if (File.Exists(iconPath))
            {
                this.Icon = new WindowIcon(iconPath);
            }
            // Nếu không có file → để trống → KHÔNG CRASH, Avalonia tự dùng icon mặc định
            this.KeyDown += MainWindow_KeyDown;
            _vm = new MainWindowViewModel(this);
            DataContext = _vm;

            Debug.WriteLine("========================================");
            Debug.WriteLine($"🪟 MainWindow Constructor");
            Debug.WriteLine($"   DataContext = {DataContext?.GetType().Name ?? "null"}");
            Debug.WriteLine($"   _vm = {(_vm != null ? "OK" : "NULL")}");
            Debug.WriteLine("========================================");

            if (_vm == null)
            {
                Debug.WriteLine("⚠️ DataContext chưa gán MainWindowViewModel.");
                return;
            }

            //  Gán MediaPlayer SAU KHI UI loaded
            this.Loaded += MainWindow_Loaded;
            this.Opened += MainWindow_Opened;

            StartWindowHiderTimer();
        }

        private void MainWindow_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key != Avalonia.Input.Key.Space) return;
            if (this.FocusManager?.GetFocusedElement() is TextBox) return;

            var vm = DataContext as MainWindowViewModel;
            if (vm == null) return;

            if (!string.IsNullOrEmpty(vm.PlateNumberIn) && vm.PlateNumberIn != "---")
                vm.ManualOpenInCommand.Execute(Unit.Default);
            else if (!string.IsNullOrEmpty(vm.PlateNumberOut) && vm.PlateNumberOut != "---")
                vm.ManualOpenOutCommand.Execute(Unit.Default);

            e.Handled = true;
        }
        private void OpenSettings_Click(object? sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            settingsWindow.ShowDialog(this);
        }



        //  Xử lý click menu Danh sách thẻ
        private void OpenCardManagement_Click(object? sender, RoutedEventArgs e)
        {
            Debug.WriteLine("📋 Mở Danh sách thẻ...");

            // Tạo cửa sổ mới cho Card Management
            var cardWindow = new Window
            {
                Title = "Quản lý thẻ",
                Width = 1400,
                Height = 800,
                Content = new CardManagementView(),
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            cardWindow.Show();
        }

        //  Gán MediaPlayer khi UI đã sẵn sàng
        private void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Debug.WriteLine("🔧 MainWindow Loaded - Đang gán MediaPlayer...");

            if (_vm == null) return;

            // Delay một chút để VideoView khởi tạo xong
            _ = Task.Run(async () =>
            {
                await Task.Delay(500);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        // ===== GÁN CAMERA CHÍNH =====
                        if (CameraInView != null && _vm.MediaPlayerIn != null)
                        {
                            CameraInView.MediaPlayer = _vm.MediaPlayerIn;
                            Debug.WriteLine("✅ Đã gán MediaPlayerIn vào CameraInView");
                        }

                        if (CameraOutView != null && _vm.MediaPlayerOut != null)
                        {
                            CameraOutView.MediaPlayer = _vm.MediaPlayerOut;
                            Debug.WriteLine("✅ Đã gán MediaPlayerOut vào CameraOutView");
                        }

                        // ===== GÁN CAMERA PHỤ VÀO =====
                        if (CameraInExtra1View != null && _vm.MediaPlayerInExtra1 != null)
                        {
                            CameraInExtra1View.MediaPlayer = _vm.MediaPlayerInExtra1;
                            Debug.WriteLine("✅ Đã gán MediaPlayerInExtra1");
                        }
                        if (CameraInExtra2View != null && _vm.MediaPlayerInExtra2 != null)
                        {
                            CameraInExtra2View.MediaPlayer = _vm.MediaPlayerInExtra2;
                            Debug.WriteLine("✅ Đã gán MediaPlayerInExtra2");
                        }
                        

                        // ===== GÁN CAMERA PHỤ RA =====
                        if (CameraOutExtra1View != null && _vm.MediaPlayerOutExtra1 != null)
                        {
                            CameraOutExtra1View.MediaPlayer = _vm.MediaPlayerOutExtra1;
                            Debug.WriteLine("✅ Đã gán MediaPlayerOutExtra1");
                        }
                        if (CameraOutExtra2View != null && _vm.MediaPlayerOutExtra2 != null)
                        {
                            CameraOutExtra2View.MediaPlayer = _vm.MediaPlayerOutExtra2;
                            Debug.WriteLine("✅ Đã gán MediaPlayerOutExtra2");
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Lỗi gán MediaPlayer: {ex.Message}");
                    }
                });
            });
        }

        private void OpenCardEventManagement_Click(object? sender, RoutedEventArgs e)
        {
            Debug.WriteLine("📋 Mở Lịch sử xe ra vào...");

            var cardEventWindow = new Window
            {
                Title = "Lịch sử xe ra vào",
                Width = 1400,
                Height = 800,
                Content = new CardEventManagementView(),
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            cardEventWindow.Show();
        }


        private void MainWindow_Opened(object? sender, EventArgs e)
        {
            Debug.WriteLine("========================================");
            Debug.WriteLine($"🪟 Window Opened Event");

            var grid = this.FindControl<DataGrid>("CardEventsGrid");

            if (grid != null)
            {
                Debug.WriteLine($"📊 DataGrid FOUND!");
                Debug.WriteLine($"   ItemsSource Type = {grid.ItemsSource?.GetType().Name ?? "null"}");

                if (grid.ItemsSource is System.Collections.IEnumerable items)
                {
                    var count = items.Cast<object>().Count();
                    Debug.WriteLine($"   Items Count = {count}");
                }
            }
            else
            {
                Debug.WriteLine("❌ DataGrid NOT FOUND!");
            }
            Debug.WriteLine("========================================");
        }

        private void StartWindowHiderTimer()
        {
            windowHiderTimer = new System.Threading.Timer(HideVLCWindows, null, 2000, 1000);
        }

        private void HideVLCWindows(object? state)
        {
            try
            {
                var processes = System.Diagnostics.Process.GetProcessesByName("vlc");

                foreach (var process in processes)
                {
                    if (process.MainWindowHandle != IntPtr.Zero &&
                        !string.IsNullOrEmpty(process.MainWindowTitle))
                    {
                        if (process.MainWindowTitle.Contains("VLC") ||
                            process.MainWindowTitle.Contains("output") ||
                            process.MainWindowTitle.Contains("Direct3D"))
                        {
                            ShowWindow(process.MainWindowHandle, 0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Window hider error: {ex.Message}");
            }
        }

        // Thoát ứng dụng với confirmation dialog
        private async void Exit_Click(object? sender, RoutedEventArgs e)
        {
            Debug.WriteLine("🚪 Yêu cầu thoát ứng dụng...");


            var msgBox = new Window
            {
                Title = "Xác nhận thoát",
                Width = 400,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Background = Avalonia.Media.Brushes.DarkGray
            };

            var panel = new StackPanel
            {
                Margin = new Avalonia.Thickness(30),
                Spacing = 25,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            panel.Children.Add(new TextBlock
            {
                Text = "Bạn có chắc muốn thoát ứng dụng?",
                FontSize = 16,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            });

            var buttons = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Spacing = 15
            };

            var yesBtn = new Button
            {
                Content = "✅ Có",
                Width = 100,
                Height = 40,
                FontSize = 14
            };
            yesBtn.Click += (s, ev) => msgBox.Close(true);

            var noBtn = new Button
            {
                Content = "❌ Không",
                Width = 100,
                Height = 40,
                FontSize = 14
            };
            noBtn.Click += (s, ev) => msgBox.Close(false);

            buttons.Children.Add(yesBtn);
            buttons.Children.Add(noBtn);
            panel.Children.Add(buttons);
            msgBox.Content = panel;

            var result = await msgBox.ShowDialog<bool>(this);

            if (result)
            {
                Debug.WriteLine(" Xác nhận thoát - Bắt đầu cleanup...");

                try
                {
                    //  1. Dừng timer trước
                    windowHiderTimer?.Dispose();
                    windowHiderTimer = null;
                    Debug.WriteLine(" Đã dispose timer");

                    //  2. Stop MediaPlayers CHÍNH
                    if (_vm?.MediaPlayerIn != null && _vm.MediaPlayerIn.IsPlaying)
                    {
                        _vm.MediaPlayerIn.Stop();
                        Debug.WriteLine(" Đã stop MediaPlayerIn");
                    }

                    if (_vm?.MediaPlayerOut != null && _vm.MediaPlayerOut.IsPlaying)
                    {
                        _vm.MediaPlayerOut.Stop();
                        Debug.WriteLine(" Đã stop MediaPlayerOut");
                    }

                    //  3. Stop MediaPlayers PHỤ
                    if (_vm?.MediaPlayerInExtra1 != null && _vm.MediaPlayerInExtra1.IsPlaying)
                    {
                        _vm.MediaPlayerInExtra1.Stop();
                        Debug.WriteLine(" Đã stop MediaPlayerInExtra1");
                    }
                    if (_vm?.MediaPlayerInExtra2 != null && _vm.MediaPlayerInExtra2.IsPlaying)
                    {
                        _vm.MediaPlayerInExtra2.Stop();
                        Debug.WriteLine(" Đã stop MediaPlayerInExtra2");
                    }
                    
                    if (_vm?.MediaPlayerOutExtra1 != null && _vm.MediaPlayerOutExtra1.IsPlaying)
                    {
                        _vm.MediaPlayerOutExtra1.Stop();
                        Debug.WriteLine(" Đã stop MediaPlayerOutExtra1");
                    }
                    if (_vm?.MediaPlayerOutExtra2 != null && _vm.MediaPlayerOutExtra2.IsPlaying)
                    {
                        _vm.MediaPlayerOutExtra2.Stop();
                        Debug.WriteLine(" Đã stop MediaPlayerOutExtra2");
                    }
                    

                    //  4. Đợi VLC cleanup
                    await Task.Delay(500);

                    //  5. Dispose ViewModel (sẽ dispose tất cả camera providers)
                    _vm?.Dispose();
                    _vm = null;
                    Debug.WriteLine("✅ Đã dispose ViewModel");

                    //  6. Kill VLC processes còn sót
                    var vlcProcesses = System.Diagnostics.Process.GetProcessesByName("vlc");
                    foreach (var process in vlcProcesses)
                    {
                        try
                        {
                            if (!process.HasExited)
                            {
                                process.Kill();
                                await process.WaitForExitAsync();
                            }
                        }
                        catch { }
                    }
                    Debug.WriteLine("✅ Đã kill VLC processes");

                    //  7. Đợi thêm chút
                    await Task.Delay(200);

                    //  8. Thoát ứng dụng
                    Debug.WriteLine("🚪 Đang thoát...");

                    var lifetime = Avalonia.Application.Current?.ApplicationLifetime
                        as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;

                    if (lifetime != null)
                    {
                        lifetime.Shutdown(0);
                    }
                    else
                    {
                        // Fallback: đóng window
                        Close();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"⚠️ Lỗi khi cleanup: {ex.Message}");
                    // Vẫn cố gắng thoát
                    Close();
                }
            }
            else
            {
                Debug.WriteLine("❌ Hủy thoát");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            windowHiderTimer?.Dispose();
            _vm?.Dispose();

            try
            {
                var vlcProcesses = System.Diagnostics.Process.GetProcessesByName("vlc");
                foreach (var process in vlcProcesses)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void TextBlock_ActualThemeVariantChanged(object? sender, EventArgs e)
        {
        }

        private void TextBlock_ActualThemeVariantChanged_1(object? sender, EventArgs e)
        {
        }
    }
}