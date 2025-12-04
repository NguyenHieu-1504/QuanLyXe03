using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using QuanLyXe03.Helpers;
using QuanLyXe03.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyXe03.Views
{
    public partial class SettingsWindow : Window
    {
        private KzE02NetService? _testService;
        private readonly StringBuilder _testLog = new();

        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = this;

            AppendTestLog("Test KZ-E02 sẵn sàng. Nhấn nút để kiểm tra.");
            LoadSettings();
            LoadTestService();

            AddHoverEffect(SaveButton, new SolidColorBrush(Colors.LimeGreen), new SolidColorBrush(Color.Parse("#059669")));
            AddHoverEffect(CancelButton, new SolidColorBrush(Color.Parse("#64748B")), new SolidColorBrush(Color.Parse("#475569")));
        }

        private void LoadSettings()
        {
            var s = SettingsManager.Settings;
            RequireLoginCheck.IsChecked = s.RequireLogin;

            var inputBox = this.FindControl<TextBox>("InputPathBox");
            var outputBox = this.FindControl<TextBox>("OutputPathBox");

            if (inputBox != null) inputBox.Text = s.ImagePaths?.Input ?? "";
            if (outputBox != null) outputBox.Text = s.ImagePaths?.Output ?? "";

            Debug.WriteLine($"📂 Loaded Settings:");
            Debug.WriteLine($"   Input Path: {s.ImagePaths?.Input ?? "(empty)"}");
            Debug.WriteLine($"   Output Path: {s.ImagePaths?.Output ?? "(empty)"}");
        }

        private async void ChooseInputFolder_Click(object sender, RoutedEventArgs e)
        {
            var folder = await PickFolderAsync();
            if (folder != null)
            {
                //  LƯU VÀO SETTINGS NGAY LẬP TỨC
                SettingsManager.Settings.ImagePaths.Input = folder;

                var inputBox = this.FindControl<TextBox>("InputPathBox");
                if (inputBox != null) inputBox.Text = folder;

                StatusText.Text = $"✅ Đã chọn thư mục ảnh VÀO: {folder}";
                StatusText.Foreground = Brushes.Green;

                Debug.WriteLine($"✅ Chọn Input Path: {folder}");
            }
        }

        private async void ChooseOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            var folder = await PickFolderAsync();
            if (folder != null)
            {
                //  LƯU VÀO SETTINGS NGAY LẬP TỨC
                SettingsManager.Settings.ImagePaths.Output = folder;

                var outputBox = this.FindControl<TextBox>("OutputPathBox");
                if (outputBox != null) outputBox.Text = folder;

                StatusText.Text = $"✅ Đã chọn thư mục ảnh RA: {folder}";
                StatusText.Foreground = Brushes.Green;

                Debug.WriteLine($"✅ Chọn Output Path: {folder}");
            }
        }

        private async Task<string?> PickFolderAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider == null) return null;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Chọn thư mục lưu ảnh",
                AllowMultiple = false
            });

            return folders?.Count > 0 ? folders[0].TryGetLocalPath() : null;
        }

        private void Save_Click(object? sender, RoutedEventArgs e)
        {
            var s = SettingsManager.Settings;
            s.RequireLogin = RequireLoginCheck.IsChecked ?? false;

            if (s.RequireLogin)
            {
                var newPass = NewPasswordBox.Text;
                var confirm = ConfirmPasswordBox.Text;

                if (!string.IsNullOrEmpty(newPass))
                {
                    if (newPass != confirm)
                    {
                        StatusText.Text = "❌ Mật khẩu xác nhận không khớp!";
                        StatusText.Foreground = Brushes.Red;
                        return;
                    }
                    s.LoginPassword = newPass;
                }
            }

            //  LƯU TOÀN BỘ SETTINGS VÀO FILE JSON
            SettingsManager.SaveSettings();

            Debug.WriteLine("💾 ĐÃ LƯU SETTINGS:");
            Debug.WriteLine($"   Input Path: {s.ImagePaths?.Input ?? "(empty)"}");
            Debug.WriteLine($"   Output Path: {s.ImagePaths?.Output ?? "(empty)"}");
            Debug.WriteLine($"   RequireLogin: {s.RequireLogin}");

            StatusText.Text = "✅ ĐÃ LƯU THÀNH CÔNG! Có thể đóng cửa sổ.";
            StatusText.Foreground = Brushes.LimeGreen;
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();

        private void LoadTestService()
        {
            var cfg = SettingsManager.Settings.KzE02Controller;
            if (cfg == null || !cfg.Enabled) return;

            var config = new Bdk { Comport = cfg.Ip, Baudrate = cfg.Port.ToString() };
            _testService = new KzE02NetService(config);

            _testService.OnLoopInEvent += (_) => AppendTestLog("→ LOOP IN phát hiện xe vào");
            _testService.OnLoopOutEvent += (_) => AppendTestLog("→ LOOP OUT phát hiện xe ra");
            _testService.OnCardEvent += (d) => AppendTestLog($"→ CARD: {d?.GetValueOrDefault("card") ?? "N/A"}");
            _testService.OnError += (msg) => AppendTestLog($"❌ LỖI: {msg}");
            _testService.OnConnectionStatusChanged += (ok) =>
            {
                TestStatusText.Text = ok ? "✅ ONLINE" : "❌ OFFLINE";
                TestStatusText.Foreground = ok ? Brushes.LimeGreen : Brushes.Red;
            };
        }

        private void AppendTestLog(string msg)
        {
            //  Wrap bằng Dispatcher để chạy trên UI thread
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _testLog.AppendLine($"[{DateTime.Now:HH:mm:ss}] {msg}");
                TestLogBox.Text = _testLog.ToString();
                TestLogBox.CaretIndex = TestLogBox.Text.Length;
            });
        }
        private void TestPing_Click(object? sender, RoutedEventArgs e)
        {
            AppendTestLog("→ Ping controller...");
            var ip = SettingsManager.Settings.KzE02Controller?.Ip ?? "192.168.1.250";
            try
            {
                var ping = new Ping();
                var reply = ping.Send(ip, 3000);
                var status = reply?.Status == IPStatus.Success ? "✅ PING OK" : $"❌ PING FAIL ({reply?.Status})";
                AppendTestLog(status);
            }
            catch (Exception ex)
            {
                AppendTestLog($"❌ PING EXCEPTION: {ex.Message}");
            }
        }

        private void TestFirmware_Click(object? sender, RoutedEventArgs e)
        {
            AppendTestLog("→ Lấy Firmware...");
            var ip = SettingsManager.Settings.KzE02Controller?.Ip ?? "192.168.1.250";
            var port = SettingsManager.Settings.KzE02Controller?.Port ?? 100;
            var cmd = KZTEK_CMD.GetFirmwareVersion();
            var res = UdpTools.ExecuteCommand(ip, port, cmd, 5000);
            AppendTestLog($"Response: {res}");
            if (res.Contains("Version=")) AppendTestLog("✅ FIRMWARE OK");
        }

        private void TestMode_Click(object? sender, RoutedEventArgs e)
        {
            AppendTestLog("→ Lấy Mode...");
            var ip = SettingsManager.Settings.KzE02Controller?.Ip ?? "192.168.1.250";
            var port = SettingsManager.Settings.KzE02Controller?.Port ?? 100;
            var cmd = KZTEK_CMD.GetMode();
            var res = UdpTools.ExecuteCommand(ip, port, cmd, 5000);
            AppendTestLog($"Response: {res}");
        }

        private async void TestRelay_Click(object? sender, RoutedEventArgs e)
        {
            AppendTestLog("→ Mở Relay 1 trong 3 giây...");
            var ip = SettingsManager.Settings.KzE02Controller?.Ip ?? "192.168.1.250";
            var port = SettingsManager.Settings.KzE02Controller?.Port ?? 100;

            var on = KZTEK_CMD.SetRelay(1, "ON");
            var res1 = UdpTools.ExecuteCommand(ip, port, on, 5000);
            AppendTestLog($"ON: {res1}");

            await Task.Delay(3000);

            var off = KZTEK_CMD.SetRelay(1, "OFF");
            var res2 = UdpTools.ExecuteCommand(ip, port, off, 5000);
            AppendTestLog($"OFF: {res2}");
            AppendTestLog("✅ RELAY TEST HOÀN TẤT!");
        }

        private void StartPolling_Click(object? sender, RoutedEventArgs e)
        {
            AppendTestLog("→ BẮT ĐẦU POLLING GetEvent...");
            _testService?.StartAsync();
        }

        private void StopPolling_Click(object? sender, RoutedEventArgs e)
        {
            AppendTestLog("→ DỪNG POLLING");
            _testService?.Stop();
        }

        private void AddHoverEffect(Button button, IBrush normal, IBrush hover)
        {
            button.PointerEntered += (_, __) => button.Background = hover;
            button.PointerExited += (_, __) => button.Background = normal;
        }

        protected override void OnClosed(EventArgs e)
        {
            _testService?.Stop();
            base.OnClosed(e);
        }
    }
}