using Avalonia.Media.Imaging;
using OpenCvSharp;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace QuanLyXe03.Services.Camera
{
    /// <summary>
    /// Camera provider sử dụng OpenCvSharp4 - tối ưu cho Linux
    /// Sử dụng kỹ thuật "Bufferless Capture" để giảm latency
    /// </summary>
    public class OpenCvCameraProvider : ICameraProvider
    {
        private VideoCapture? _capture;
        private Thread? _grabThread;
        private volatile bool _running;
        private readonly object _frameLock = new();

        private Mat? _latestFrame;
        private byte[]? _latestFrameBytes;
        private Bitmap? _latestBitmap;

        public bool IsInitialized { get; private set; }
        public string? InitializationError { get; private set; }
        public bool IsPlaying => _running && _capture?.IsOpened() == true;

        public event Action<Bitmap>? OnFrameReceived;
        public event Action<string>? OnError;

        public OpenCvCameraProvider()
        {
            Console.WriteLine("🐧 OpenCvCameraProvider created (Linux mode)");
            IsInitialized = true;
        }

        public async Task<bool> StartAsync(string rtspUrl)
        {
            try
            {
                Console.WriteLine($"📹 OpenCV: Opening {rtspUrl}");

                // QUAN TRỌNG: Set FFmpeg options cho low latency + TCP transport như ffplay
                Environment.SetEnvironmentVariable(
                    "OPENCV_FFMPEG_CAPTURE_OPTIONS",
                    "rtsp_transport;tcp|fflags;nobuffer|flags;low_delay|analyzeduration;100000|probesize;100000|max_delay;0"
                );

                _capture = new VideoCapture();

                // Mở với FFmpeg backend
                bool opened = await Task.Run(() =>
                    _capture.Open(rtspUrl, VideoCaptureAPIs.FFMPEG));

                if (!opened || !_capture.IsOpened())
                {
                    // Thử lại với URL có credentials nếu chưa có
                    Console.WriteLine("⚠️ Không mở được, kiểm tra URL có credentials chưa?");
                    InitializationError = "Không thể mở RTSP stream. Kiểm tra URL format: rtsp://user:pass@ip:port/stream";
                    OnError?.Invoke(InitializationError);
                    return false;
                }

                // Cấu hình low latency
                _capture.Set(VideoCaptureProperties.BufferSize, 1); // Chỉ giữ 1 frame

                var width = _capture.FrameWidth;
                var height = _capture.FrameHeight;
                var fps = _capture.Fps;

                Console.WriteLine($"✅ OpenCV: Stream opened");
                Console.WriteLine($"   Resolution: {width}x{height}");
                Console.WriteLine($"   FPS: {fps}");

                // Bắt đầu thread grab frame liên tục
                _running = true;
                _grabThread = new Thread(GrabLoop)
                {
                    IsBackground = true,
                    Name = "OpenCV_GrabThread",
                    Priority = ThreadPriority.AboveNormal
                };
                _grabThread.Start();

                return true;
            }
            catch (Exception ex)
            {
                InitializationError = ex.Message;
                OnError?.Invoke($"OpenCV Error: {ex.Message}");
                Console.WriteLine($"❌ OpenCV Error: {ex.Message}");
                Console.WriteLine($"   Stack: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Thread liên tục grab frame để buffer luôn có frame mới nhất
        /// Kỹ thuật này đảm bảo khi cần frame, ta luôn có frame gần nhất
        /// </summary>
        private void GrabLoop()
        {
            Console.WriteLine("🔄 GrabLoop started");
            var frame = new Mat();
            int errorCount = 0;
            const int maxErrors = 10;

            while (_running && _capture?.IsOpened() == true)
            {
                try
                {
                    // Grab frame - nhanh, chỉ lấy data từ buffer
                    if (!_capture.Grab())
                    {
                        errorCount++;
                        if (errorCount >= maxErrors)
                        {
                            Console.WriteLine($"❌ Quá nhiều lỗi grab ({errorCount}), dừng stream");
                            OnError?.Invoke("Mất kết nối camera");
                            break;
                        }
                        Thread.Sleep(50);
                        continue;
                    }

                    errorCount = 0; // Reset error count khi thành công

                    // Retrieve và decode frame
                    if (_capture.Retrieve(frame) && !frame.Empty())
                    {
                        lock (_frameLock)
                        {
                            // Dispose frame cũ
                            _latestFrame?.Dispose();
                            _latestFrame = frame.Clone();

                            // Convert to JPEG bytes
                            _latestFrameBytes = frame.ToBytes(".jpg",
                                new ImageEncodingParam(ImwriteFlags.JpegQuality, 85));

                            // Update Avalonia Bitmap
                            UpdateBitmap();
                        }

                        // Fire event cho UI update
                        if (_latestBitmap != null)
                        {
                            OnFrameReceived?.Invoke(_latestBitmap);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ GrabLoop error: {ex.Message}");
                    errorCount++;
                }

                // ~25-30 FPS - đồng bộ với camera, giảm delay
                Thread.Sleep(20); // Giảm từ 33ms để tăng FPS nếu cần
            }

            frame.Dispose();
            Console.WriteLine("🛑 GrabLoop stopped");
        }

        private void UpdateBitmap()
        {
            if (_latestFrameBytes == null || _latestFrameBytes.Length == 0)
                return;

            try
            {
                _latestBitmap?.Dispose();
                using var ms = new MemoryStream(_latestFrameBytes);
                _latestBitmap = new Bitmap(ms);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ UpdateBitmap error: {ex.Message}");
            }
        }

        public Bitmap? GetCurrentFrame()
        {
            lock (_frameLock)
            {
                if (_latestFrameBytes == null || _latestFrameBytes.Length == 0)
                    return null;

                try
                {
                    using var ms = new MemoryStream(_latestFrameBytes);
                    return new Bitmap(ms);
                }
                catch
                {
                    return null;
                }
            }
        }

        public byte[]? GetCurrentFrameBytes()
        {
            lock (_frameLock)
            {
                if (_latestFrameBytes == null) return null;

                // Return copy để thread-safe
                var copy = new byte[_latestFrameBytes.Length];
                Array.Copy(_latestFrameBytes, copy, _latestFrameBytes.Length);
                return copy;
            }
        }

        public async Task<bool> TakeSnapshotAsync(string filename)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (_frameLock)
                    {
                        if (_latestFrameBytes == null || _latestFrameBytes.Length == 0)
                        {
                            Console.WriteLine("⚠️ Không có frame để snapshot");
                            return false;
                        }

                        var dir = Path.GetDirectoryName(filename);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        File.WriteAllBytes(filename, _latestFrameBytes);
                        Console.WriteLine($"📸 Snapshot saved: {filename} ({_latestFrameBytes.Length} bytes)");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Snapshot error: {ex.Message}");
                    return false;
                }
            });
        }

        public void Stop()
        {
            Console.WriteLine("🛑 OpenCV: Stopping...");
            _running = false;

            // Đợi thread kết thúc
            if (_grabThread?.IsAlive == true)
            {
                _grabThread.Join(3000);
            }

            _capture?.Release();
            _capture?.Dispose();
            _capture = null;

            lock (_frameLock)
            {
                _latestFrame?.Dispose();
                _latestFrame = null;
                _latestBitmap?.Dispose();
                _latestBitmap = null;
                _latestFrameBytes = null;
            }

            Console.WriteLine("✅ OpenCV: Stopped");
        }

        public void Dispose()
        {
            Stop();
            Console.WriteLine("✅ OpenCvCameraProvider disposed");
        }
    }
}