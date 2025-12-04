
//FFmpegCameraProvider.cs

//    using Avalonia;
//using Avalonia.Media.Imaging;
//using Avalonia.Platform;
//using Avalonia.Threading;
//using SkiaSharp;
//using System;
//using System.Diagnostics;
//using System.IO;
//using System.Threading;
//using System.Threading.Tasks;

//namespace QuanLyXe03.Services.Camera
//{
//    public class FFmpegCameraProvider : ICameraProvider
//    {
//        private Process? _ffmpegProcess;
//        private Thread? _grabThread;
//        private volatile bool _running;
//        private readonly object _frameLock = new object();

//        private WriteableBitmap? _latestBitmap;
//        private byte[]? _latestFrameBytes;

//        public bool IsInitialized { get; private set; }
//        public string? InitializationError { get; private set; }
//        public bool IsPlaying => _running && _ffmpegProcess != null && !_ffmpegProcess.HasExited;

//        public event Action<Bitmap>? OnFrameReceived;
//        public event Action<string>? OnError;

//        public FFmpegCameraProvider()
//        {
//            Console.WriteLine("📹 FFmpegCameraProvider created (Linux mode - low latency)");
//            IsInitialized = true;
//        }

//        public async Task<bool> StartAsync(string rtspUrl)
//        {
//            try
//            {
//                Console.WriteLine($"📹 FFmpeg: Opening {rtspUrl}");

//                _ffmpegProcess = new Process
//                {
//                    StartInfo = new ProcessStartInfo
//                    {
//                        FileName = "ffmpeg",
//                        Arguments = $"-rtsp_transport tcp -fflags nobuffer -flags low_delay -analyzeduration 500000 -probesize 500000 -max_delay 0 -i \"{rtspUrl}\" -vf fps=25 -f mjpeg -q:v 5 -",
//                        UseShellExecute = false,
//                        RedirectStandardOutput = true,
//                        RedirectStandardError = true,
//                        CreateNoWindow = true
//                    }
//                };

//                _ffmpegProcess.Start();

//                // Log error stream
//                _ = Task.Run(() =>
//                {
//                    string? line;
//                    while ((line = _ffmpegProcess.StandardError.ReadLine()) != null)
//                    {
//                        Console.WriteLine($"FFmpeg ERR: {line}");
//                    }
//                });

//                // Start grab thread
//                _running = true;
//                _grabThread = new Thread(GrabLoop)
//                {
//                    IsBackground = true,
//                    Name = "FFmpegGrabThread",
//                    Priority = ThreadPriority.AboveNormal
//                };
//                _grabThread.Start();

//                await Task.Delay(1000);
//                Console.WriteLine("✅ FFmpeg: Stream opened successfully");
//                return true;
//            }
//            catch (Exception ex)
//            {
//                InitializationError = ex.Message;
//                OnError?.Invoke($"FFmpeg Error: {ex.Message}");
//                Console.WriteLine($"❌ FFmpeg Error: {ex.Message}");
//                return false;
//            }
//        }

//        private void GrabLoop()
//        {
//            Console.WriteLine("🔄 FFmpeg GrabLoop started");
//            var buffer = new byte[1024 * 1024];
//            var frameBuffer = new MemoryStream();

//            try
//            {
//                using var output = _ffmpegProcess!.StandardOutput.BaseStream;
//                while (_running && output.CanRead)
//                {
//                    int bytesRead = output.Read(buffer, 0, buffer.Length);
//                    if (bytesRead <= 0) break;

//                    frameBuffer.Write(buffer, 0, bytesRead);

//                    var data = frameBuffer.ToArray();
//                    int start = 0;
//                    while (start < data.Length - 2)
//                    {
//                        if (data[start] == 0xFF && data[start + 1] == 0xD8) // JPEG start
//                        {
//                            int end = start + 2;
//                            while (end < data.Length - 1 && !(data[end] == 0xFF && data[end + 1] == 0xD9))
//                            {
//                                end++;
//                            }
//                            if (end < data.Length - 1)
//                            {
//                                var jpegBytes = new byte[end - start + 2];
//                                Array.Copy(data, start, jpegBytes, 0, jpegBytes.Length);

//                                //  Không log mỗi frame (spam quá)
//                                // Console.WriteLine($"FRAME_EXTRACTED: {jpegBytes.Length} bytes");

//                                lock (_frameLock)
//                                {
//                                    _latestFrameBytes = jpegBytes;
//                                }

//                                //  Dùng InvokeAsync thay vì Post để đảm bảo thực thi
//                                Dispatcher.UIThread.InvokeAsync(() =>
//                                {
//                                    UpdateBitmap();
//                                    if (_latestBitmap != null)
//                                    {
//                                        OnFrameReceived?.Invoke(_latestBitmap);
//                                    }
//                                }, DispatcherPriority.Render); // ← Priority cao cho rendering

//                                start = end + 2;
//                            }
//                            else
//                            {
//                                break;
//                            }
//                        }
//                        else
//                        {
//                            start++;
//                        }
//                    }

//                    // Keep remaining data in buffer
//                    var remaining = new byte[data.Length - start];
//                    Array.Copy(data, start, remaining, 0, remaining.Length);
//                    frameBuffer.SetLength(0);
//                    frameBuffer.Write(remaining, 0, remaining.Length);
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"⚠️ GrabLoop error: {ex.Message}");
//                OnError?.Invoke("GrabLoop failed");
//            }

//            Console.WriteLine("🛑 FFmpeg GrabLoop stopped");
//        }

//        private unsafe void UpdateBitmap()
//        {
//            if (_latestFrameBytes == null || _latestFrameBytes.Length < 100)
//                return;

//            try
//            {
//                byte[] frameData;
//                lock (_frameLock)
//                {
//                    frameData = new byte[_latestFrameBytes.Length];
//                    Array.Copy(_latestFrameBytes, frameData, _latestFrameBytes.Length);
//                }

//                using var skBitmap = SKBitmap.Decode(frameData);
//                if (skBitmap == null || skBitmap.Width <= 0 || skBitmap.Height <= 0)
//                    return;

//                //  TẠO MỚI thay vì reuse
//                var pixelSize = new PixelSize(skBitmap.Width, skBitmap.Height);
//                var newBitmap = new WriteableBitmap(pixelSize, new Vector(96, 96),
//                    PixelFormat.Bgra8888, AlphaFormat.Premul);

//                using (var fb = newBitmap.Lock())
//                {
//                    var rowBytes = fb.RowBytes;
//                    var skBytes = skBitmap.GetPixelSpan();

//                    for (int y = 0; y < skBitmap.Height; y++)
//                    {
//                        var srcRow = skBytes.Slice(y * skBitmap.RowBytes, skBitmap.RowBytes);
//                        var dstPtr = fb.Address + y * rowBytes;
//                        var dstRow = new Span<byte>((void*)dstPtr, rowBytes);
//                        srcRow.CopyTo(dstRow);
//                    }
//                }

//                _latestBitmap = newBitmap; // ← Reference mới
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"BITMAP_CREATE_FAILED: {ex.Message}");
//                _latestBitmap = null;
//            }
//        }

//        public unsafe Bitmap? GetCurrentFrame()
//        {
//            lock (_frameLock)
//            {
//                if (_latestBitmap == null) return null;

//                var copyBitmap = new WriteableBitmap(_latestBitmap.PixelSize, _latestBitmap.Dpi, _latestBitmap.Format, _latestBitmap.AlphaFormat);
//                using (var srcLock = _latestBitmap.Lock())
//                using (var dstLock = copyBitmap.Lock())
//                {
//                    for (int y = 0; y < _latestBitmap.PixelSize.Height; y++)
//                    {
//                        var srcRow = new Span<byte>((void*)(srcLock.Address + y * srcLock.RowBytes), srcLock.RowBytes);
//                        var dstRow = new Span<byte>((void*)(dstLock.Address + y * dstLock.RowBytes), dstLock.RowBytes);
//                        srcRow.CopyTo(dstRow);
//                    }
//                }
//                return copyBitmap;
//            }
//        }

//        public byte[]? GetCurrentFrameBytes()
//        {
//            lock (_frameLock)
//            {
//                if (_latestFrameBytes == null) return null;
//                var copy = new byte[_latestFrameBytes.Length];
//                Array.Copy(_latestFrameBytes, copy, _latestFrameBytes.Length);
//                return copy;
//            }
//        }

//        public async Task<bool> TakeSnapshotAsync(string filename)
//        {
//            return await Task.Run(() =>
//            {
//                try
//                {
//                    lock (_frameLock)
//                    {
//                        if (_latestFrameBytes == null) return false;

//                        var dir = Path.GetDirectoryName(filename);
//                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
//                            Directory.CreateDirectory(dir);

//                        File.WriteAllBytes(filename, _latestFrameBytes);
//                        Console.WriteLine($"📸 Snapshot saved: {filename}");
//                        return true;
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"❌ Snapshot error: {ex.Message}");
//                    return false;
//                }
//            });
//        }

//        public void Stop()
//        {
//            Console.WriteLine("🛑 FFmpeg: Stopping...");
//            _running = false;

//            if (_grabThread?.IsAlive == true)
//                _grabThread.Join(2000);

//            if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
//            {
//                _ffmpegProcess.Kill();
//                _ffmpegProcess.WaitForExit(2000);
//            }

//            lock (_frameLock)
//            {
//                _latestBitmap = null;
//                _latestFrameBytes = null;
//            }

//            _ffmpegProcess?.Dispose();
//            Console.WriteLine("✅ FFmpeg: Stopped");
//        }

//        public void Dispose()
//        {
//            Stop();
//            Console.WriteLine("✅ FFmpegCameraProvider disposed");
//        }
//    }
//}






//PlateRecognitionService.cs

//    using System;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Text.Json;
//using System.Threading.Tasks;
//using QuanLyXe03.Helpers;

//namespace QuanLyXe03.Services
//{
//    public class PlateRecognitionService
//    {
//        private readonly HttpClient _httpClient;
//        private readonly string _apiUrl;

//        public PlateRecognitionService()
//        {
//            _httpClient = new HttpClient
//            {
//                Timeout = TimeSpan.FromSeconds(30)
//            };

//            //  Đọc URL từ appsettings.json
//            _apiUrl = SettingsManager.Settings.PlateRecognition?.ApiUrl ?? "http://127.0.0.1:8000/predict";

//            Console.WriteLine($"🔍 PlateRecognitionService initialized");
//            Console.WriteLine($"   API URL: {_apiUrl}");
//        }

//        /// <summary>
//        /// Gửi ảnh đến API nhận diện biển số
//        /// </summary>
//        public async Task<(string plateText, string vehicleClass, bool success, string errorMessage)> RecognizePlateAsync(byte[] imageBytes)
//        {
//            try
//            {
//                using var content = new MultipartFormDataContent();

//                var imageContent = new ByteArrayContent(imageBytes);
//                imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
//                content.Add(imageContent, "file", "image.jpg");

//                var response = await _httpClient.PostAsync(_apiUrl, content);
//                var responseString = await response.Content.ReadAsStringAsync();

//                if (response.IsSuccessStatusCode)
//                {
//                    var plates = JsonSerializer.Deserialize<JsonElement>(responseString);

//                    if (plates.ValueKind == JsonValueKind.Array && plates.GetArrayLength() > 0)
//                    {
//                        var firstPlate = plates[0];
//                        var plateText = firstPlate.TryGetProperty("plate_text", out var p) ? p.GetString() ?? "N/A" : "N/A";
//                        var vehicleClass = firstPlate.TryGetProperty("vehicle_class", out var c) ? c.GetString() ?? "unknown" : "unknown";
//                        return (plateText, vehicleClass, true, string.Empty);
//                    }
//                    return ("N/A", "unknown", false, "Không phát hiện biển số");
//                }
//                else
//                {
//                    return ("N/A", "unknown", false, $"Lỗi API {response.StatusCode}");
//                }
//            }
//            catch (HttpRequestException ex)
//            {
//                return ("N/A", "unknown", false, $"Không kết nối được API: {ex.Message}");
//            }
//            catch (TaskCanceledException)
//            {
//                return ("N/A", "unknown", false, "API timeout - server chậm");
//            }
//            catch (Exception ex)
//            {
//                return ("N/A", "unknown", false, $"Lỗi: {ex.Message}");
//            }
//        }

//        public void Dispose()
//        {
//            _httpClient?.Dispose();
//        }
//    }
//}





