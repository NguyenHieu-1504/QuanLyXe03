using LibVLCSharp.Shared;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace QuanLyXe03.Services
{
    public class CameraService : IDisposable
    {
        private LibVLC? _libVLC;
        public bool IsInitialized { get; private set; }
        public string? InitializationError { get; private set; }

        public CameraService()
        {
            try
            {
                Console.WriteLine("========================================");
                Console.WriteLine("🎬 Đang khởi tạo CameraService...");
                Console.WriteLine($"   OS: {RuntimeInformation.OSDescription}");
                Console.WriteLine($"   Platform: {GetPlatformName()}");

                // LINUX: Không cần kiểm tra thư mục Libs
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var vlcPath = Path.Combine(AppContext.BaseDirectory, "Libs");
                    if (!Directory.Exists(vlcPath))
                    {
                        throw new DirectoryNotFoundException($"Không tìm thấy thư mục VLC tại: {vlcPath}");
                    }
                    Console.WriteLine($"   VLC Path: {vlcPath}");
                }
                else
                {
                    Console.WriteLine("   VLC: Sử dụng system libvlc");
                }

                Console.WriteLine("   Đang gọi Core.Initialize()...");
                Core.Initialize();
                Console.WriteLine("   ✅ Core.Initialize() thành công");

                var options = GetPlatformSpecificOptions();
                Console.WriteLine($"   VLC Options: {string.Join(" ", options)}");

                Console.WriteLine("   Đang tạo LibVLC instance...");
                _libVLC = new LibVLC(options);
                Console.WriteLine("   ✅ LibVLC instance tạo thành công");

                IsInitialized = true;
                Console.WriteLine("✅ CameraService khởi tạo thành công");
                Console.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                IsInitialized = false;
                InitializationError = ex.Message;
                Console.WriteLine($"❌ Lỗi khởi tạo CameraService: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
        }

        private string GetPlatformName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "Windows";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "Linux";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "macOS";
            return "Unknown";
        }

        private string[] GetPlatformSpecificOptions()
        {
            var options = new List<string>
            {
                "--intf=dummy",
                "--no-osd",
                "--no-audio",
                "--aout=dummy",
                "--verbose=2", // Bật log để debug
                "--file-logging", // Ghi log ra file nếu cần
                "--logfile=vlc-log.txt"
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                options.Add("--vout=direct3d11");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Tăng buffer mạng cho Linux nhưng tối ưu low latency
                options.Add("--network-caching=0"); // Giảm buffer như ffplay nobuffer
                // Xóa --rtsp-tcp vì invalid global option
                // options.Add("--rtsp-tcp"); // Đã xóa

                // Xử lý X11
                options.Add("--no-xlib");

                // Tắt toàn bộ tăng tốc phần cứng trên Linux VM
                options.Add("--avcodec-hw=none");

                // Thêm low latency options
                options.Add("--clock-jitter=0");
                options.Add("--clock-synchro=0");
            }

            return options.ToArray();
        }

        public MediaPlayer? CreatePlayer(string rtspUrl)
        {
            Console.WriteLine($"🎥 CreatePlayer được gọi: {rtspUrl}");

            if (!IsInitialized)
            {
                Console.WriteLine($"❌ CameraService chưa khởi tạo: {InitializationError}");
                return null;
            }

            if (_libVLC == null)
            {
                Console.WriteLine("❌ _libVLC is null");
                return null;
            }

            MediaPlayer? player = null;

            try
            {
                Console.WriteLine("   Đang tạo Media...");
                var media = new Media(_libVLC, rtspUrl, FromType.FromLocation);

                // Cấu hình RTSP - dùng ":" cho media options, tối ưu low latency như ffplay
                media.AddOption(":network-caching=0"); // Nobuffer
                media.AddOption(":live-caching=0");
                media.AddOption(":rtsp-tcp"); // Force TCP
                media.AddOption(":fflags=nobuffer");
                media.AddOption(":flags=low_delay");
                media.AddOption(":analyzeduration=100000");
                media.AddOption(":probesize=100000");
                media.AddOption(":max_delay=0");

                Console.WriteLine("   Đang tạo MediaPlayer...");
                player = new MediaPlayer(_libVLC)
                {
                    EnableHardwareDecoding = false
                };

                // Event handlers
                player.Playing += (s, e) => Console.WriteLine($"▶️ Camera PLAYING: {rtspUrl}");
                player.Stopped += (s, e) => Console.WriteLine($"⏹️ Camera STOPPED: {rtspUrl}");
                player.EncounteredError += (s, e) => Console.WriteLine($"❌ Camera ERROR: {rtspUrl}");
                player.Buffering += (s, e) =>
                {
                    if (e.Cache < 100)
                        Console.WriteLine($"⏳ Buffering: {e.Cache}%");
                };

                Console.WriteLine("   Đang gọi Play()...");
                bool playResult = player.Play(media);
                Console.WriteLine($"   Play() returned: {playResult}");

                return player;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception trong CreatePlayer: {ex.Message}");
                Console.WriteLine($"   StackTrace: {ex.StackTrace}");
                player?.Dispose();
                return null;
            }
        }

        public bool TakeSnapshot(MediaPlayer player, string filename, Action<bool, string>? onComplete = null)
        {
            if (player == null)
            {
                Console.WriteLine("❌ MediaPlayer null");
                onComplete?.Invoke(false, "MediaPlayer null");
                return false;
            }

            if (!player.IsPlaying)
            {
                Console.WriteLine("⚠️ Camera chưa phát");
                onComplete?.Invoke(false, "Camera chưa phát");
                return false;
            }

            try
            {
                string? directory = Path.GetDirectoryName(filename);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                bool result = player.TakeSnapshot(0, filename, 0, 0);

                if (result)
                {
                    System.Threading.Thread.Sleep(500);

                    if (File.Exists(filename))
                    {
                        var fileInfo = new FileInfo(filename);
                        if (fileInfo.Length > 0)
                        {
                            Console.WriteLine($"✅ Snapshot: {filename} ({fileInfo.Length} bytes)");
                            onComplete?.Invoke(true, filename);
                            return true;
                        }
                    }
                }

                Console.WriteLine("❌ Snapshot thất bại");
                onComplete?.Invoke(false, "Snapshot thất bại");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.Message}");
                onComplete?.Invoke(false, ex.Message);
                return false;
            }
        }

        public bool CanTakeSnapshot(MediaPlayer player)
        {
            if (player == null) return false;
            if (!player.IsPlaying) return false;
            try
            {
                return player.VideoTrack >= 0 || player.Media?.Tracks?.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _libVLC?.Dispose();
            Console.WriteLine("✅ CameraService disposed");
        }
    }
}