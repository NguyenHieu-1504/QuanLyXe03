using QuanLyXe03.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuanLyXe03.Services
{
    public class KzE02NetService : IDisposable
    {
        private readonly Bdk _config;
        private bool _isRunning = false;
        private Task? _pollingTask;
        private CancellationTokenSource? _cancellationTokenSource;
        private int _consecutiveErrors = 0;
        private const int MAX_CONSECUTIVE_ERRORS = 5;

        public event Action<Dictionary<string, string>>? OnCardEvent;
        public event Action<Dictionary<string, string>>? OnLoopInEvent;
        public event Action<Dictionary<string, string>>? OnLoopOutEvent;
        public event Action<string>? OnError;
        public event Action<bool>? OnConnectionStatusChanged;

        public KzE02NetService(Bdk config)
        {
            _config = config;
            Debug.WriteLine($"KzE02NetService created: {config.Comport}:{config.Baudrate}");
        }

        public async Task StartAsync()
        {
            if (_isRunning)
            {
                Debug.WriteLine(" Service đã chạy, dừng instance cũ trước");
                Stop();
                await Task.Delay(500); // Đợi stop xong
            }

            _isRunning = true;
            _consecutiveErrors = 0;
            _cancellationTokenSource = new CancellationTokenSource();

            Debug.WriteLine($"▶Bắt đầu polling KZ-E02: {_config.Comport}:{_config.Baudrate}");
            OnConnectionStatusChanged?.Invoke(false); // Chưa kết nối

            _pollingTask = Task.Run(() => PollingLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            await Task.CompletedTask;
        }

        private async Task PollingLoop(CancellationToken cancellationToken)
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    //  LẤY THÔNG SỐ TỪ CONFIG
                    var ip = _config.Comport;
                    var port = int.Parse(_config.Baudrate);
                    var pollingInterval = SettingsManager.Settings.KzE02Controller?.PollingIntervalMs ?? 500;

                    //  GỌI ĐÚNG HÀM ExecuteCommand (ip, port, command, timeout)
                    var cmd = KZTEK_CMD.GetEvent();
                    var res = UdpTools.ExecuteCommand(ip, port, cmd, 1000);

                    //  KIỂM TRA RESPONSE
                    if (!string.IsNullOrEmpty(res) && !res.Contains("UDP Error"))
                    {
                        // Reset error counter khi kết nối OK
                        if (_consecutiveErrors > 0)
                        {
                            _consecutiveErrors = 0;
                            OnConnectionStatusChanged?.Invoke(true);
                            Debug.WriteLine("Kết nối lại thành công");
                        }

                        // Parse response - CHECK BOTH "Event=" AND "Style="
                        if (res.Contains("Event=") || res.Contains("event=") ||
                            res.Contains("Style=") || res.Contains("style="))
                        {
                            var data = ParseEvent(res);

                            //  HỖ TRỢ CẢ "Event" VÀ "Style"
                            var evt = data.GetValueOrDefault("event", "");
                            if (string.IsNullOrEmpty(evt))
                                evt = data.GetValueOrDefault("style", "");

                            evt = evt.ToLower();

                            if (!string.IsNullOrEmpty(evt))
                            {
                                Debug.WriteLine($"📨 Event: {evt} | Card: {data.GetValueOrDefault("card", "N/A")}");

                                switch (evt)
                                {
                                    case "card":
                                        OnCardEvent?.Invoke(data);
                                        Debug.WriteLine($"CARD Event: {data.GetValueOrDefault("card", "N/A")}");
                                        break;
                                    case "loopin":
                                        OnLoopInEvent?.Invoke(data);
                                        Debug.WriteLine("LOOP IN Event");
                                        break;
                                    case "loopout":
                                        OnLoopOutEvent?.Invoke(data);
                                        Debug.WriteLine("LOOP OUT Event");
                                        break;
                                    default:
                                        Debug.WriteLine($"Unknown event type: {evt}");
                                        break;
                                }

                                //  XÓA EVENT SAU KHI XỬ LÝ
                                DeleteEvent();
                            }
                        }
                        else if (!res.Contains("NotEvent"))
                        {
                            // Chỉ log nếu không phải là NotEvent
                            if (!string.IsNullOrEmpty(res))
                                Debug.WriteLine($"Unknown response: {res.Substring(0, Math.Min(100, res.Length))}");
                        }

                        // Ping để check connection (không cần quá thường xuyên)
                        if (_consecutiveErrors == 0) // Chỉ ping khi đang OK
                        {
                            var pingOk = NetWorkTools.IsPingSuccess(ip, 500);
                            if (pingOk != (_consecutiveErrors == 0))
                            {
                                OnConnectionStatusChanged?.Invoke(pingOk);
                            }
                        }
                    }
                    else
                    {
                        //  XỬ LÝ LỖI KẾT NỐI
                        _consecutiveErrors++;

                        if (_consecutiveErrors == 1)
                        {
                            Debug.WriteLine($"Mất kết nối với controller: {res}");
                            OnConnectionStatusChanged?.Invoke(false);
                        }

                        if (_consecutiveErrors >= MAX_CONSECUTIVE_ERRORS)
                        {
                            OnError?.Invoke($"Mất kết nối sau {MAX_CONSECUTIVE_ERRORS} lần thử");
                            Debug.WriteLine($"Dừng polling sau {MAX_CONSECUTIVE_ERRORS} lỗi liên tiếp");

                            // Tăng delay để không spam
                            await Task.Delay(5000, cancellationToken);
                            _consecutiveErrors = 0; // Reset để thử lại
                        }
                    }

                    //  DELAY TRƯỚC KHI POLL TIẾP
                    await Task.Delay(pollingInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("⏹️ Polling bị hủy");
                    break;
                }
                catch (Exception ex)
                {
                    _consecutiveErrors++;
                    Debug.WriteLine($"❌ Lỗi polling: {ex.Message}");

                    // Chỉ báo lỗi qua UI nếu lỗi không phải network timeout
                    if (!ex.Message.Contains("timeout") && !ex.Message.Contains("SocketException"))
                    {
                        OnError?.Invoke(ex.Message);
                    }

                    OnConnectionStatusChanged?.Invoke(false);

                    // Delay lâu hơn khi có lỗi
                    await Task.Delay(2000, cancellationToken);
                }
            }

            Debug.WriteLine(" Polling loop kết thúc");
        }

        private Dictionary<string, string> ParseEvent(string raw)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Response format: GetEvent?/Style=Card/Card=329D446C/Reader=04/...
                // Hoặc: GetEvent?/Event=Card/Card=7C19F640/Reader=1/CardState=R/Door=01
                // Hoặc: GetEvent?/Event=LoopIn
                // Hoặc: GetEvent?/NotEvent

                var parts = raw.Split('/');
                foreach (var part in parts)
                {
                    if (part.Contains("="))
                    {
                        var kv = part.Split('=');
                        if (kv.Length == 2)
                        {
                            dict[kv[0].Trim().ToLower()] = kv[1].Trim();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($" Lỗi parse event: {ex.Message} | Raw: {raw}");
            }

            return dict;
        }

        /// <summary>
        /// Xóa event khỏi controller memory sau khi đã xử lý
        /// </summary>
        private void DeleteEvent()
        {
            try
            {
                var ip = _config.Comport;
                var port = int.Parse(_config.Baudrate);
                var cmd = KZTEK_CMD.DeleteEvent();
                var res = UdpTools.ExecuteCommand(ip, port, cmd, 500);

                if (res.Contains("OK"))
                {
                    Debug.WriteLine(" Đã xóa event khỏi controller");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($" Không thể xóa event: {ex.Message}");
            }
        }

        public bool OpenBarrier(int relayIndex, int durationMs = 1500)
        {
            try
            {
                var ip = _config.Comport;
                var port = int.Parse(_config.Baudrate);

                Debug.WriteLine($" Mở barrier Relay {relayIndex} trong {durationMs}ms");

                //  MỞ RELAY
                var cmdOn = KZTEK_CMD.SetRelay(relayIndex, "ON");
                var resOn = UdpTools.ExecuteCommand(ip, port, cmdOn, 1000);

                if (string.IsNullOrEmpty(resOn) || !resOn.Contains("OK"))
                {
                    Debug.WriteLine($" Không thể mở relay: {resOn}");
                    OnError?.Invoke($"Lỗi mở relay {relayIndex}");
                    return false;
                }

                Debug.WriteLine($" Relay {relayIndex} ON");

                //  TỰ ĐỘNG TẮT SAU {durationMs}
                _ = Task.Run(async () =>
                {
                    await Task.Delay(durationMs);

                    var cmdOff = KZTEK_CMD.SetRelay(relayIndex, "OFF");
                    var resOff = UdpTools.ExecuteCommand(ip, port, cmdOff, 1000);

                    if (resOff.Contains("OK"))
                    {
                        Debug.WriteLine($" Relay {relayIndex} OFF");
                    }
                    else
                    {
                        Debug.WriteLine($" Không thể tắt relay: {resOff}");
                    }
                });

                LogManualOpen(relayIndex);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($" Exception mở barrier: {ex.Message}");
                OnError?.Invoke(ex.Message);
                return false;
            }
        }

        private void LogManualOpen(int relay)
        {
            try
            {
                if (SettingsManager.Settings.KzE02Controller?.ManualOpenLog != true)
                    return;

                var logPath = "Logs/ManualOpen.log";
                var logDir = Path.GetDirectoryName(logPath);

                if (!string.IsNullOrEmpty(logDir))
                    Directory.CreateDirectory(logDir);

                var relayIn = SettingsManager.Settings.KzE02Controller.RelayIn;
                var gate = relay == relayIn ? "IN" : "OUT";
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | Gate: {gate} | Relay: {relay} | User: {Environment.UserName}\r\n";

                File.AppendAllText(logPath, line);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($" Không thể ghi log: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;

            Debug.WriteLine(" Dừng KzE02NetService...");
            _isRunning = false;
            _cancellationTokenSource?.Cancel();

            try
            {
                _pollingTask?.Wait(2000);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($" Lỗi khi dừng: {ex.Message}");
            }

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            OnConnectionStatusChanged?.Invoke(false);
            Debug.WriteLine(" KzE02NetService đã dừng");
        }

        public void Dispose()
        {
            Stop();
        }
    }
}