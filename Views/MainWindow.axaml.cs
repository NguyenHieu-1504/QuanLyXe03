using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using QuanLyXe03.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

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
            this.Icon = new WindowIcon("Assets/app.ico");   // Đặt biểu tượng cửa sổ
            _vm = new MainWindowViewModel();
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

            // ✅ QUAN TRỌNG: Gán MediaPlayer SAU KHI UI loaded
            this.Loaded += MainWindow_Loaded;
            this.Opened += MainWindow_Opened;

            StartWindowHiderTimer();
        }

        // ✅ THÊM: Xử lý click menu Danh sách thẻ
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




        // ✅ THÊM: Gán MediaPlayer khi UI đã sẵn sàng
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
                        // Gán MediaPlayer vào VideoView
                        if (CameraInView != null && _vm.MediaPlayerIn != null)
                        {
                            CameraInView.MediaPlayer = _vm.MediaPlayerIn;
                            Debug.WriteLine("✅ Đã gán MediaPlayerIn vào CameraInView");
                        }
                        else
                        {
                            Debug.WriteLine("❌ CameraInView hoặc MediaPlayerIn null");
                        }

                        if (CameraOutView != null && _vm.MediaPlayerOut != null)
                        {
                            CameraOutView.MediaPlayer = _vm.MediaPlayerOut;
                            Debug.WriteLine("✅ Đã gán MediaPlayerOut vào CameraOutView");
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
            //Debug.WriteLine($"   CardEvents.Count = {_vm?.CardEvents.Count ?? -1}");

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

                //if (_vm != null)
                //{
                //    _vm.CardEvents.CollectionChanged += (s, args) =>
                //    {
                //        Debug.WriteLine($"🔔 CollectionChanged Event! Action: {args.Action}");
                //        Debug.WriteLine($"   CardEvents.Count NOW: {_vm.CardEvents.Count}");
                //    };
                //}
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
                Debug.WriteLine("✅ Xác nhận thoát - Bắt đầu cleanup...");

                try
                {
                    //  1. Dừng timer trước
                    windowHiderTimer?.Dispose();
                    windowHiderTimer = null;
                    Debug.WriteLine("✅ Đã dispose timer");

                    //  2. Stop MediaPlayers
                    if (_vm?.MediaPlayerIn != null && _vm.MediaPlayerIn.IsPlaying)
                    {
                        _vm.MediaPlayerIn.Stop();
                        Debug.WriteLine("✅ Đã stop MediaPlayerIn");
                    }

                    if (_vm?.MediaPlayerOut != null && _vm.MediaPlayerOut.IsPlaying)
                    {
                        _vm.MediaPlayerOut.Stop();
                        Debug.WriteLine("✅ Đã stop MediaPlayerOut");
                    }

                    //  3. Đợi VLC cleanup
                    await Task.Delay(500);

                    //  4. Dispose ViewModel
                    _vm?.Dispose();
                    _vm = null;
                    Debug.WriteLine("✅ Đã dispose ViewModel");

                    //  5. Kill VLC processes còn sót
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

                    //  6. Đợi thêm chút
                    await Task.Delay(200);

                    //  7. Thoát ứng dụng
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
    }
}