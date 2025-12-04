using Avalonia.Media.Imaging;
using LibVLCSharp.Shared;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace QuanLyXe03.Services.Camera
{
    /// <summary>
    /// Camera provider sử dụng LibVLCSharp - tối ưu cho Windows
    /// </summary>
    public class VlcCameraProvider : ICameraProvider
    {
        private LibVLC? _libVLC;
        private MediaPlayer? _mediaPlayer;
        private string? _rtspUrl;

        public bool IsInitialized { get; private set; }
        public string? InitializationError { get; private set; }
        public bool IsPlaying => _mediaPlayer?.IsPlaying == true;

        public MediaPlayer? MediaPlayer => _mediaPlayer;

        public event Action<Bitmap>? OnFrameReceived;
        public event Action<string>? OnError;

        public VlcCameraProvider()
        {
            try
            {
                Console.WriteLine("🪟 VlcCameraProvider created (Windows mode)");

                var vlcPath = Path.Combine(AppContext.BaseDirectory, "Libs");
                if (!Directory.Exists(vlcPath))
                {
                    throw new DirectoryNotFoundException($"VLC libs not found: {vlcPath}");
                }

                Core.Initialize();

                _libVLC = new LibVLC(
                    "--intf=dummy",
                    "--no-osd",
                    "--no-audio",
                    "--aout=dummy",
                    "--vout=direct3d11",
                    "--network-caching=0", // Low latency như ffplay
                    "--live-caching=0",
                    "--clock-jitter=0",
                    "--clock-synchro=0"
                );

                IsInitialized = true;
                Console.WriteLine("✅ VLC initialized");
            }
            catch (Exception ex)
            {
                IsInitialized = false;
                InitializationError = ex.Message;
                Console.WriteLine($"❌ VLC init error: {ex.Message}");
            }
        }

        public async Task<bool> StartAsync(string rtspUrl)
        {
            if (!IsInitialized || _libVLC == null)
            {
                OnError?.Invoke("VLC chưa khởi tạo");
                return false;
            }

            try
            {
                _rtspUrl = rtspUrl;

                var media = new Media(_libVLC, rtspUrl, FromType.FromLocation);
                media.AddOption(":network-caching=0");
                media.AddOption(":live-caching=0");
                media.AddOption(":rtsp-tcp");
                media.AddOption(":fflags=nobuffer"); // Thêm từ ffplay
                media.AddOption(":flags=low_delay");
                media.AddOption(":analyzeduration=100000");
                media.AddOption(":probesize=100000");
                media.AddOption(":max_delay=0");

                _mediaPlayer = new MediaPlayer(_libVLC)
                {
                    EnableHardwareDecoding = true
                };

                _mediaPlayer.Playing += (s, e) =>
                    Console.WriteLine($"▶️ VLC Playing: {rtspUrl}");
                _mediaPlayer.EncounteredError += (s, e) =>
                    OnError?.Invoke($"VLC Error: {rtspUrl}");

                bool result = await Task.Run(() => _mediaPlayer.Play(media));

                if (result)
                {
                    await Task.Delay(1000);
                }

                return result;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"VLC Error: {ex.Message}");
                return false;
            }
        }

        public void Stop()
        {
            _mediaPlayer?.Stop();
        }

        public Bitmap? GetCurrentFrame()
        {
            var bytes = GetCurrentFrameBytes();
            if (bytes == null) return null;

            using var ms = new MemoryStream(bytes);
            return new Bitmap(ms);
        }

        public byte[]? GetCurrentFrameBytes()
        {
            if (_mediaPlayer == null || !_mediaPlayer.IsPlaying)
                return null;

            try
            {
                var tempFile = Path.GetTempFileName() + ".jpg";
                bool success = _mediaPlayer.TakeSnapshot(0, tempFile, 0, 0);

                if (success)
                {
                    Thread.Sleep(200);
                    if (File.Exists(tempFile))
                    {
                        var bytes = File.ReadAllBytes(tempFile);
                        File.Delete(tempFile);
                        return bytes;
                    }
                }
            }
            catch { }

            return null;
        }

        public async Task<bool> TakeSnapshotAsync(string filename)
        {
            if (_mediaPlayer == null || !_mediaPlayer.IsPlaying)
                return false;

            return await Task.Run(() =>
            {
                try
                {
                    var dir = Path.GetDirectoryName(filename);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);

                    bool result = _mediaPlayer.TakeSnapshot(0, filename, 0, 0);

                    if (result)
                    {
                        Thread.Sleep(300);
                        return File.Exists(filename);
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            });
        }

        public void Dispose()
        {
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
            Console.WriteLine("✅ VlcCameraProvider disposed");
        }
    }
}