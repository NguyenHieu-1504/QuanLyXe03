using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using LibVLCSharp.Shared;
using QuanLyXe03.Helpers;
using QuanLyXe03.Models;
using QuanLyXe03.Repositories;
using QuanLyXe03.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using QuanLyXe03.Services.Camera;
using System.Runtime.InteropServices;

namespace QuanLyXe03.ViewModels
{

    public class PerformanceTimer : IDisposable
    {
        private readonly Stopwatch _stopwatch;
        private readonly string _operationName;
        private readonly Action<string>? _logAction;

        
        public PerformanceTimer(string operationName, Action<string> logAction)
        {
            _operationName = operationName;
            _logAction = logAction;
            _stopwatch = Stopwatch.StartNew();

            var startMsg = $"⏱️ START: {_operationName}";
            Console.WriteLine(startMsg);
            Debug.WriteLine(startMsg);
            SafeLog(startMsg);
        }

        // Thêm constructor 1 tham số 
        public PerformanceTimer(string operationName) : this(operationName, null!)
        {
        }

        private void SafeLog(string message)
        {
            try { _logAction?.Invoke(message); } catch { /* tránh crash nếu AppendLog ném lỗi */ }
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            var endMsg = $"✅ END: {_operationName} | Time: {_stopwatch.ElapsedMilliseconds}ms";
            Console.WriteLine(endMsg);
            Debug.WriteLine(endMsg);
            SafeLog(endMsg);
        }
    }

    public class MainWindowViewModel : ReactiveObject, IDisposable
    {
        private readonly CameraService _cameraService;
        private readonly PlateRecognitionService _plateRecognitionService;
        private readonly CardEventRepository _cardEventRepo;
        private readonly CardRepository _cardRepo;
        private string _currentCardNumberIn = "";  // Lưu mã thẻ vừa quẹt VÀO
        private string _currentCardNumberOut = ""; // Lưu mã thẻ vừa quẹt RA
        private readonly Window _mainWindow;
        private ICameraProvider? _cameraProviderIn;
        private ICameraProvider? _cameraProviderOut;
        private bool _isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public bool IsLinux => _isLinux;

        // = CAMERA PHỤ - WINDOWS =
        private MediaPlayer? _mediaPlayerInExtra1;
        private MediaPlayer? _mediaPlayerInExtra2;
        
        private MediaPlayer? _mediaPlayerOutExtra1;
        private MediaPlayer? _mediaPlayerOutExtra2;
        

        public MediaPlayer? MediaPlayerInExtra1
        {
            get => _mediaPlayerInExtra1;
            set => this.RaiseAndSetIfChanged(ref _mediaPlayerInExtra1, value);
        }
        public MediaPlayer? MediaPlayerInExtra2
        {
            get => _mediaPlayerInExtra2;
            set => this.RaiseAndSetIfChanged(ref _mediaPlayerInExtra2, value);
        }
        
        public MediaPlayer? MediaPlayerOutExtra1
        {
            get => _mediaPlayerOutExtra1;
            set => this.RaiseAndSetIfChanged(ref _mediaPlayerOutExtra1, value);
        }
        public MediaPlayer? MediaPlayerOutExtra2
        {
            get => _mediaPlayerOutExtra2;
            set => this.RaiseAndSetIfChanged(ref _mediaPlayerOutExtra2, value);
        }
        

        //= CAMERA PHỤ - LINUX =
        private ICameraProvider? _cameraProviderInExtra1;
        private ICameraProvider? _cameraProviderInExtra2;
        
        private ICameraProvider? _cameraProviderOutExtra1;
        private ICameraProvider? _cameraProviderOutExtra2;
        

        private Bitmap? _frameInExtra1;
        private Bitmap? _frameInExtra2;
        
        private Bitmap? _frameOutExtra1;
        private Bitmap? _frameOutExtra2;
        

        public Bitmap? FrameInExtra1
        {
            get => _frameInExtra1;
            set => this.RaiseAndSetIfChanged(ref _frameInExtra1, value);
        }
        public Bitmap? FrameInExtra2
        {
            get => _frameInExtra2;
            set => this.RaiseAndSetIfChanged(ref _frameInExtra2, value);
        }
       
        public Bitmap? FrameOutExtra1
        {
            get => _frameOutExtra1;
            set => this.RaiseAndSetIfChanged(ref _frameOutExtra1, value);
        }
        public Bitmap? FrameOutExtra2
        {
            get => _frameOutExtra2;
            set => this.RaiseAndSetIfChanged(ref _frameOutExtra2, value);
        }
       


        private MediaPlayer? _mediaPlayerIn;
        public MediaPlayer? MediaPlayerIn
        {
            get => _mediaPlayerIn;
            set => this.RaiseAndSetIfChanged(ref _mediaPlayerIn, value);
        }

        private MediaPlayer? _mediaPlayerOut;
        public MediaPlayer? MediaPlayerOut
        {
            get => _mediaPlayerOut;
            set => this.RaiseAndSetIfChanged(ref _mediaPlayerOut, value);
        }

        private bool _isCheckInMode;
        public bool IsCheckInMode
        {
            get => _isCheckInMode;
            set => this.RaiseAndSetIfChanged(ref _isCheckInMode, value);
        }

        private Bitmap? _frameIn;
        public Bitmap? FrameIn
        {
            get => _frameIn;
            set => this.RaiseAndSetIfChanged(ref _frameIn, value);
        }

        private Bitmap? _frameOut;
        public Bitmap? FrameOut
        {
            get => _frameOut;
            set => this.RaiseAndSetIfChanged(ref _frameOut, value);
        }

        private KzE02NetService? _kzService;

        // COMMANDS
        public ReactiveCommand<Unit, Unit> ManualOpenInCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> ManualOpenOutCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> CaptureSnapshotCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> CheckInCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> CheckOutCommand { get; private set; } = null!;

        private string _lastManualOpen = "";
        public string LastManualOpen
        {
            get => _lastManualOpen;
            set => this.RaiseAndSetIfChanged(ref _lastManualOpen, value);
        }
        // Thông tin thẻ VÀO
        public int VehiclesInLot { get; set; } = 46;

        private string _plateNumberIn = "---";
        public string PlateNumberIn
        {
            get => _plateNumberIn;
            set => this.RaiseAndSetIfChanged(ref _plateNumberIn, value);
        }

        private string _vehicleDateTimeIn = "---";
        public string VehicleDateTimeIn
        {
            get => _vehicleDateTimeIn;
            set => this.RaiseAndSetIfChanged(ref _vehicleDateTimeIn, value);
        }

        
        private string _cardInfoIn = "---";
        public string CardInfoIn
        {
            get => _cardInfoIn;
            set => this.RaiseAndSetIfChanged(ref _cardInfoIn, value);
        }

        private string _customerNameIn = "---";
        public string CustomerNameIn
        {
            get => _customerNameIn;
            set => this.RaiseAndSetIfChanged(ref _customerNameIn, value);
        }

        private string _cardGroupIn = "---";
        public string CardGroupIn
        {
            get => _cardGroupIn;
            set => this.RaiseAndSetIfChanged(ref _cardGroupIn, value);
        }

        private string _expireDateIn = "---";
        public string ExpireDateIn
        {
            get => _expireDateIn;
            set => this.RaiseAndSetIfChanged(ref _expireDateIn, value);
        }

        //  THÔNG TIN THẺ RA

        private string _cardInfoOut = "---";
        public string CardInfoOut
        {
            get => _cardInfoOut;
            set => this.RaiseAndSetIfChanged(ref _cardInfoOut, value);
        }

        private string _customerNameOut = "---";
        public string CustomerNameOut
        {
            get => _customerNameOut;
            set => this.RaiseAndSetIfChanged(ref _customerNameOut, value);
        }

        private string _cardGroupOut = "---";
        public string CardGroupOut
        {
            get => _cardGroupOut;
            set => this.RaiseAndSetIfChanged(ref _cardGroupOut, value);
        }

        private string _expireDateOut = "---";
        public string ExpireDateOut
        {
            get => _expireDateOut;
            set => this.RaiseAndSetIfChanged(ref _expireDateOut, value);
        }

        private string _plateNumberOut = "---";
        public string PlateNumberOut
        {
            get => _plateNumberOut;
            set => this.RaiseAndSetIfChanged(ref _plateNumberOut, value);
        }

        private string _vehicleDateTimeOut = "---";
        public string VehicleDateTimeOut
        {
            get => _vehicleDateTimeOut;
            set => this.RaiseAndSetIfChanged(ref _vehicleDateTimeOut, value);
        }

        private Bitmap? _snapshotIn;
        public Bitmap? SnapshotIn
        {
            get => _snapshotIn;
            set => this.RaiseAndSetIfChanged(ref _snapshotIn, value);
        }

        private Bitmap? _snapshotOut;
        public Bitmap? SnapshotOut
        {
            get => _snapshotOut;
            set => this.RaiseAndSetIfChanged(ref _snapshotOut, value);
        }

        private Bitmap? _snapshot1;
        public Bitmap? Snapshot1
        {
            get => _snapshot1;
            set => this.RaiseAndSetIfChanged(ref _snapshot1, value);
        }

        private Bitmap? _snapshot2;
        public Bitmap? Snapshot2
        {
            get => _snapshot2;
            set => this.RaiseAndSetIfChanged(ref _snapshot2, value);
        }

        private Bitmap? _snapshot3;
        public Bitmap? Snapshot3
        {
            get => _snapshot3;
            set => this.RaiseAndSetIfChanged(ref _snapshot3, value);
        }


        private string _statusMessage = "Sẵn sàng";
        public string StatusMessage
        {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        private string _statusMessageIn = "Chờ xe vào...";
        public string StatusMessageIn
        {
            get => _statusMessageIn;
            set => this.RaiseAndSetIfChanged(ref _statusMessageIn, value);
        }

        private string _statusMessageOut = "Chờ xe ra...";
        public string StatusMessageOut
        {
            get => _statusMessageOut;
            set => this.RaiseAndSetIfChanged(ref _statusMessageOut, value);
        }

        private string _parkingFeeOut = "0 VNĐ";
        public string ParkingFeeOut
        {
            get => _parkingFeeOut;
            set => this.RaiseAndSetIfChanged(ref _parkingFeeOut, value);
        }

        private readonly ObservableCollection<string> _logEntries = new();
        public IReadOnlyCollection<string> LogEntries => _logEntries;

        private void AppendLog(string message)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _logEntries.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                if (_logEntries.Count > 500)
                    _logEntries.RemoveAt(0);
            });
        }

        // New: prevent concurrent checkout
        private bool _isProcessingCheckout = false;
        public bool IsProcessingCheckout
        {
            get => _isProcessingCheckout;
            private set => this.RaiseAndSetIfChanged(ref _isProcessingCheckout, value);
        }

        // ---------------------------

        // Constructor này chỉ dành cho Designer (Preview)
        public MainWindowViewModel()
        {
            if (Design.IsDesignMode)
            {
                // Khởi tạo dữ liệu giả để nhìn thấy trên màn hình thiết kế cho đẹp
                PlateNumberIn = "30A-123.45";
                VehicleDateTimeIn = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
                StatusMessageIn = "Chế độ thiết kế (Design Mode)";

                PlateNumberOut = "29B-999.99";
                StatusMessageOut = "Sẵn sàng";
                ParkingFeeOut = "50,000 VNĐ";
                return;
            }
            throw new InvalidOperationException("Phải dùng constructor có tham số!");
            //  Không khởi tạo CameraService hay Repository ở đây 
            // để tránh lỗi kết nối DB/Phần cứng khi đang Design.
        }

        // ---------------------------

        public MainWindowViewModel(Window mainWindow)
        {
            Debug.WriteLine(" MainWindowViewModel Constructor");
            Debug.WriteLine($"   Input Path: '{SettingsManager.Settings.ImagePaths.Input}'");
            Debug.WriteLine($"   Output Path: '{SettingsManager.Settings.ImagePaths.Output}'");
           
            //  CHỈ KHỞI TẠO CameraService TRÊN WINDOWS
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Console.WriteLine("Windows detected - Initializing CameraService (VLC)");
                _cameraService = new CameraService();
            }
            else
            {
                Console.WriteLine("Linux detected - CameraService will NOT be initialized");
                // _cameraService = null; // Để null, không khởi tạo VLC
            }

            _plateRecognitionService = new PlateRecognitionService();
            _cardEventRepo = new CardEventRepository();
            _cardRepo = new CardRepository();

            ManualOpenInCommand = ReactiveCommand.Create(() => ManualOpenBarrier(true));
            ManualOpenOutCommand = ReactiveCommand.Create(() => ManualOpenBarrier(false));

            CaptureSnapshotCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await CaptureSnapshotOnlyAsync();
            });

            CheckInCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                IsCheckInMode = true;
                AppendLog("Nhấn nút GHI VÀO");
                await CaptureAndCheckInAsync();
            });

            CheckOutCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                IsCheckInMode = false;
                AppendLog("Nhấn nút GHI RA");
                await CheckOutAsync();
            });

            //  InitializeCameras() sẽ tự quyết định dùng VLC hay FFmpeg/OpenCV
            InitializeCameras();
            LoadKzController();
            _mainWindow = mainWindow;
        }
        private void InitializeCameras()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("InitializeCameras() started");

            var settings = SettingsManager.Settings;
            Console.WriteLine($"   Camera IN Enabled: {settings.CameraIn.Enabled}");
            Console.WriteLine($"   Camera IN URL: {settings.CameraIn.RtspUrl}");
            Console.WriteLine($"   Camera OUT Enabled: {settings.CameraOut.Enabled}");
            Console.WriteLine($"   Camera OUT URL: {settings.CameraOut.RtspUrl}");

            if (_isLinux)
            {
                Console.WriteLine("Linux detected - Using Factory to create provider");

                // ===== CAMERA IN =====
                if (settings.CameraIn.Enabled && !string.IsNullOrEmpty(settings.CameraIn.RtspUrl))
                {
                    Console.WriteLine($"📹 Creating Camera IN provider...");
                    _cameraProviderIn = CameraProviderFactory.Create(); // ← DÙNG FACTORY

                    Console.WriteLine($"   Provider type: {_cameraProviderIn.GetType().Name}");

                    _cameraProviderIn.OnFrameReceived += bitmap =>
                    {
                        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            FrameIn = bitmap;
                            Console.WriteLine(" Frame IN set on UI thread");  // Log để check
                        });
                    };

                    _cameraProviderIn.OnError += (err) =>
                    {
                        Console.WriteLine($"Camera IN Error: {err}");
                        AppendLog($" Camera IN: {err}");
                    };

                    Console.WriteLine($"   Starting Camera IN stream: {settings.CameraIn.RtspUrl}");

                    // Start async
                    _ = Task.Run(async () =>
                    {
                        var success = await _cameraProviderIn.StartAsync(settings.CameraIn.RtspUrl);
                        Console.WriteLine($"   Camera IN start result: {success}");
                        if (!success)
                        {
                            Console.WriteLine($" Failed to start Camera IN");
                            Console.WriteLine($"   Error: {_cameraProviderIn.InitializationError}");
                        }
                    });
                }
                else
                {
                    Console.WriteLine(" Camera IN is disabled or URL is empty");
                }

                // ===== CAMERA OUT =====
                if (settings.CameraOut.Enabled && !string.IsNullOrEmpty(settings.CameraOut.RtspUrl))
                {
                    Console.WriteLine($" Creating Camera OUT provider...");
                    _cameraProviderOut = CameraProviderFactory.Create(); // ← DÙNG FACTORY

                    Console.WriteLine($"   Provider type: {_cameraProviderOut.GetType().Name}");

                    _cameraProviderOut.OnFrameReceived += bitmap =>
                    {
                        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            FrameOut = bitmap;
                            Console.WriteLine(" Frame OUT set on UI thread");
                        });
                    };

                    _cameraProviderOut.OnError += (err) =>
                    {
                        Console.WriteLine($" Camera OUT Error: {err}");
                        AppendLog($" Camera OUT: {err}");
                    };

                    Console.WriteLine($"   Starting Camera OUT stream: {settings.CameraOut.RtspUrl}");

                    // Start async
                    _ = Task.Run(async () =>
                    {
                        var success = await _cameraProviderOut.StartAsync(settings.CameraOut.RtspUrl);
                        Console.WriteLine($"   Camera OUT start result: {success}");
                        if (!success)
                        {
                            Console.WriteLine($" Failed to start Camera OUT");
                            Console.WriteLine($"   Error: {_cameraProviderOut.InitializationError}");
                        }
                    });
                }
                else
                {
                    Console.WriteLine(" Camera OUT is disabled or URL is empty");
                }
            }
            else
            {
                // WINDOWS: Dùng VLC 
                Console.WriteLine(" Windows detected - Using VLC");

                if (settings.CameraIn.Enabled && !string.IsNullOrEmpty(settings.CameraIn.RtspUrl))
                {
                    MediaPlayerIn = _cameraService.CreatePlayer(settings.CameraIn.RtspUrl);
                }
                if (settings.CameraOut.Enabled && !string.IsNullOrEmpty(settings.CameraOut.RtspUrl))
                {
                    MediaPlayerOut = _cameraService.CreatePlayer(settings.CameraOut.RtspUrl);
                }
            }

            // ========== KHỞI TẠO CAMERA PHỤ VÀO ==========
            if (settings.CameraIn.Extras != null && settings.CameraIn.Extras.Count > 0)
            {
                Console.WriteLine($"📹 Tìm thấy {settings.CameraIn.Extras.Count} camera phụ VÀO");

                for (int i = 0; i < Math.Min(3, settings.CameraIn.Extras.Count); i++)
                {
                    if (!string.IsNullOrEmpty(settings.CameraIn.Extras[i]))
                    {
                        InitializeExtraCamera(settings.CameraIn.Extras[i], true, i + 1);
                    }
                }
            }

            // ========== KHỞI TẠO CAMERA PHỤ RA ==========
            if (settings.CameraOut.Extras != null && settings.CameraOut.Extras.Count > 0)
            {
                Console.WriteLine($"📹 Tìm thấy {settings.CameraOut.Extras.Count} camera phụ RA");

                for (int i = 0; i < Math.Min(3, settings.CameraOut.Extras.Count); i++)
                {
                    if (!string.IsNullOrEmpty(settings.CameraOut.Extras[i]))
                    {
                        InitializeExtraCamera(settings.CameraOut.Extras[i], false, i + 1);
                    }
                }
            }

            Console.WriteLine(" InitializeCameras() completed");
            Console.WriteLine("========================================");
        }

        // Existing CaptureSnapshotOnlyAsync unchanged
        private async Task CaptureSnapshotOnlyAsync()
        {
            var totalTimer = new PerformanceTimer("CAPTURE_SNAPSHOT_ONLY", AppendLog);

            try
            {
                byte[]? imageBytes = null;

                //  CHỤP ẢNH
                using (new PerformanceTimer("GET_FRAME_BYTES", AppendLog))
                {
                    if (_isLinux)
                    {
                        var provider = IsCheckInMode ? _cameraProviderIn : _cameraProviderOut;
                        if (provider == null || !provider.IsPlaying)
                        {
                            AppendLog(" Camera không khả dụng");
                            return;
                        }
                        imageBytes = provider.GetCurrentFrameBytes();
                    }
                    else
                    {
                        MediaPlayer? player = IsCheckInMode ? MediaPlayerIn : MediaPlayerOut;
                        if (player == null)
                        {
                            AppendLog(" Camera không khả dụng");
                            return;
                        }

                        string tempFile = Path.GetTempFileName() + ".jpg";
                        bool success = _cameraService.TakeSnapshot(player, tempFile, (ok, path) =>
                        {
                            if (!ok) AppendLog($"Chụp ảnh thất bại: {path}");
                        });

                        if (!success || !File.Exists(tempFile))
                        {
                            AppendLog(" Không thể chụp ảnh");
                            return;
                        }

                        imageBytes = await File.ReadAllBytesAsync(tempFile);
                        File.Delete(tempFile);
                    }
                }

                if (imageBytes == null || imageBytes.Length == 0)
                {
                    AppendLog(" Không có dữ liệu ảnh");
                    return;
                }

                //  NHẬN DIỆN 
                string plateText;
                string vehicleClass;
                bool recogSuccess;
                string errorMessage;

                using (new PerformanceTimer("PLATE_RECOGNITION_SNAPSHOT", AppendLog))
                {
                    (plateText, vehicleClass, recogSuccess, errorMessage) =
                        await _plateRecognitionService.RecognizePlateAsync(imageBytes);
                }

                //  CẬP NHẬT UI 
                using (new PerformanceTimer("UPDATE_UI_SNAPSHOT", AppendLog))
                {
                    using var ms = new MemoryStream(imageBytes);
                    var avaloniaBitmap = new Bitmap(ms);

                    if (IsCheckInMode)
                    {
                        SnapshotIn = avaloniaBitmap;
                        PlateNumberIn = plateText;
                        StatusMessageIn = recogSuccess ? "✓ Nhận dạng OK" : "⚠ Không rõ biển số";
                    }
                    else
                    {
                        SnapshotOut = avaloniaBitmap;
                        PlateNumberOut = plateText;
                        StatusMessageOut = recogSuccess ? "✓ Nhận dạng OK" : "⚠ Không rõ biển số";
                    }

                    Snapshot3 = Snapshot2;
                    Snapshot2 = Snapshot1;
                    ms.Position = 0;
                    Snapshot1 = new Bitmap(ms);
                }

                var gate = IsCheckInMode ? "VÀO" : "RA";
                AppendLog($"Chụp ảnh cổng {gate} - Biển: {plateText} ({vehicleClass})");
            }
            catch (Exception ex)
            {
                AppendLog($"Lỗi xử lý ảnh: {ex.Message}");
            }
            finally
            {
                totalTimer.Dispose();
            }
        }

        // Existing CaptureAndCheckInAsync 
        private async Task CaptureAndCheckInAsync()
        {
            var totalTimer = new PerformanceTimer("CHECK_IN_NO_CARD", AppendLog);

            try
            {
                byte[]? imageBytes = null;

                //  KIỂM TRA CAMERA
                using (new PerformanceTimer("CHECK_CAMERA_IN", AppendLog))
                {
                    if (_isLinux)
                    {
                        if (_cameraProviderIn == null || !_cameraProviderIn.IsPlaying)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() => StatusMessageIn = " Camera không khả dụng");
                            AppendLog(" Camera VÀO không khả dụng");
                            return;
                        }
                    }
                    else
                    {
                        if (MediaPlayerIn == null)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() => StatusMessageIn = " Camera không khả dụng");
                            AppendLog(" Camera VÀO không khả dụng");
                            return;
                        }

                        if (!MediaPlayerIn.IsPlaying)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() => StatusMessageIn = " Camera chưa phát, đang kích hoạt...");
                            AppendLog(" Camera chưa phát, thử play lại...");

                            try
                            {
                                MediaPlayerIn.Play();
                                await Task.Delay(2000);

                                if (!MediaPlayerIn.IsPlaying)
                                {
                                    await Dispatcher.UIThread.InvokeAsync(() => StatusMessageIn = " Không thể khởi động camera");
                                    AppendLog(" Camera không thể khởi động");
                                    return;
                                }
                            }
                            catch (Exception ex)
                            {
                                await Dispatcher.UIThread.InvokeAsync(() => StatusMessageIn = " Lỗi khởi động camera");
                                AppendLog($" Lỗi khởi động camera: {ex.Message}");
                                return;
                            }
                        }
                    }
                }

                await Dispatcher.UIThread.InvokeAsync(() => StatusMessageIn = " Đang chụp ảnh...");

                //  CHỤP ẢNH
                using (new PerformanceTimer("CAPTURE_IMAGE_IN_NO_CARD", AppendLog))
                {
                    if (_isLinux)
                    {
                        imageBytes = _cameraProviderIn.GetCurrentFrameBytes();
                    }
                    else
                    {
                        string tempFile = Path.GetTempFileName() + ".jpg";
                        bool success = _cameraService.TakeSnapshot(MediaPlayerIn, tempFile);

                        if (!success || !File.Exists(tempFile))
                        {
                            await Dispatcher.UIThread.InvokeAsync(() => StatusMessageIn = " Chụp ảnh thất bại");
                            AppendLog(" Chụp ảnh VÀO thất bại");
                            return;
                        }

                        imageBytes = await File.ReadAllBytesAsync(tempFile);
                        File.Delete(tempFile);
                    }
                }

                if (imageBytes == null || imageBytes.Length == 0)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => StatusMessageIn = " Không có dữ liệu ảnh");
                    return;
                }

                await Dispatcher.UIThread.InvokeAsync(() => StatusMessageIn = " Đang nhận diện...");

                //  NHẬN DIỆN BIỂN SỐ 
                string plateText;
                string vehicleClass;
                bool recogSuccess;
                string errorMessage;

                using (new PerformanceTimer("PLATE_RECOGNITION_IN_NO_CARD", AppendLog))
                {
                    (plateText, vehicleClass, recogSuccess, errorMessage) =
                        await _plateRecognitionService.RecognizePlateAsync(imageBytes);
                }

                if (!recogSuccess || string.IsNullOrEmpty(plateText) || plateText == "N/A")
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        StatusMessageIn = $"⚠ {errorMessage}";
                        PlateNumberIn = "N/A";
                    });
                    AppendLog($" Nhận diện thất bại (VÀO): {errorMessage}");
                    return;
                }

                //  CẬP NHẬT UI VỚI ẢNH VÀ BIỂN SỐ 
                using (new PerformanceTimer("UPDATE_UI_IMAGE_IN_NO_CARD", AppendLog))
                {
                    using var ms = new MemoryStream(imageBytes);
                    var avaloniaBitmap = new Bitmap(ms);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        SnapshotIn = avaloniaBitmap;
                        PlateNumberIn = plateText;

                        Snapshot3 = Snapshot2;
                        Snapshot2 = Snapshot1;
                        ms.Position = 0;
                        Snapshot1 = new Bitmap(ms);
                    });
                }

                //  LƯU ẢNH
                using (new PerformanceTimer("SAVE_IMAGE_IN_NO_CARD", AppendLog))
                {
                    SaveVehicleImage(imageBytes, true, plateText);
                }

                await Dispatcher.UIThread.InvokeAsync(() => StatusMessageIn = " Đang lưu dữ liệu...");

                //  LƯU VÀO DATABASE
                var datetimeIn = DateTime.Now;
                Guid? newId;

                using (new PerformanceTimer("DB_INSERT_CHECK_IN_NO_CARD", AppendLog))
                {
                    newId = _cardEventRepo.InsertCardEventIn(plateText, datetimeIn, _currentCardNumberIn);
                    _currentCardNumberIn = "";
                }

                if (newId == null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => StatusMessageIn = " Lỗi lưu DB");
                    AppendLog($" CHECK-IN FAIL | {plateText} | DB lỗi");
                    return;
                }

                //  CẬP NHẬT UI KẾT QUẢ
                using (new PerformanceTimer("UPDATE_UI_RESULT_IN_NO_CARD", AppendLog))
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        VehicleDateTimeIn = datetimeIn.ToString("HH:mm:ss dd/MM/yyyy");
                        StatusMessageIn = " Đã ghi nhận xe vào";
                    });
                }

                AppendLog($" CHECK-IN OK | {plateText} | {datetimeIn:HH:mm:ss} | ID: {newId}");

                //  MỞ BARRIER 
                using (new PerformanceTimer("OPEN_BARRIER_IN_NO_CARD", AppendLog))
                {
                    ManualOpenBarrier(true);
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() => StatusMessageIn = " Lỗi hệ thống");
                AppendLog($" CHECK-IN EXCEPTION | {ex.Message}");
                Debug.WriteLine($" Exception: {ex.StackTrace}");
            }
            finally
            {
                totalTimer.Dispose();
            }
        }

        // Optimized CHECK-OUT flow
        private async Task CheckOutAsync()
        {
            var totalTimer = new PerformanceTimer("CHECK_OUT_NO_CARD", AppendLog);

            try
            {
                //KIỂM TRA CONCURRENT
                if (IsProcessingCheckout)
                {
                    AppendLog(" Đang xử lý check-out, vui lòng chờ...");
                    return;
                }

                IsProcessingCheckout = true;

                await Dispatcher.UIThread.InvokeAsync(() => StatusMessageOut = " Đang xử lý Check-out...");

                //  GỌI PROCESS CHECKOUT 
                CheckoutResult result;
                using (new PerformanceTimer("PROCESS_CHECKOUT_NO_CARD", AppendLog))
                {
                    result = await ProcessCheckoutAsync();
                }

                // XỬ LÝ KẾT QUẢ 
                using (new PerformanceTimer("HANDLE_CHECKOUT_RESULT", AppendLog))
                {
                    if (result.Status == CheckoutStatus.Success)
                    {
                        AppendLog($" CHECK-OUT OK | {result.Plate}");

                        using (new PerformanceTimer("OPEN_BARRIER_OUT_NO_CARD", AppendLog))
                        {
                            ManualOpenBarrier(false);
                        }

                        await Dispatcher.UIThread.InvokeAsync(() => StatusMessageOut = " Xe đã ra thành công");
                    }
                    else if (result.Status == CheckoutStatus.NotFound)
                    {
                        AppendLog(" CHECK-OUT FAIL | Không tìm thấy lượt vào");
                        await Dispatcher.UIThread.InvokeAsync(() => StatusMessageOut = " Không tìm thấy lượt vào");
                    }
                    else if (result.Status == CheckoutStatus.DetectionFailed)
                    {
                        AppendLog(" CHECK-OUT FAIL | Nhận diện thất bại");
                        await Dispatcher.UIThread.InvokeAsync(() => StatusMessageOut = " Không nhận diện được biển số");
                    }
                    else
                    {
                        AppendLog(" CHECK-OUT ERROR");
                        await Dispatcher.UIThread.InvokeAsync(() => StatusMessageOut = " Lỗi hệ thống (Check-out)");
                    }
                }

                //  XÓA UI
                using (new PerformanceTimer("CLEAR_OUT_UI", AppendLog))
                {
                    await ClearOutUI();
                }
            }
            catch (Exception ex)
            {
                AppendLog($" CheckOutAsync exception: {ex.Message}");
            }
            finally
            {
                IsProcessingCheckout = false;
                totalTimer.Dispose();
            }
        }

        private async Task<CheckoutResult> ProcessCheckoutAsync()
        {
            var totalTimer = new PerformanceTimer("PROCESS_CHECKOUT_INTERNAL", AppendLog);

            try
            {
                //  CHỤP ẢNH
                byte[]? imageBytes;
                using (new PerformanceTimer("CAPTURE_SNAPSHOT_OUT", AppendLog))
                {
                    imageBytes = await CaptureSnapshotOutAsync(null);
                }

                if (imageBytes == null)
                {
                    return CheckoutResult.Failed(CheckoutStatus.DetectionFailed);
                }

                // NHẬN DIỆN BIỂN SỐ
                string plateText;
                string vehicleClass;
                bool recogSuccess;
                string errorMessage;

                using (new PerformanceTimer("PLATE_RECOGNITION_OUT_PROCESS", AppendLog))
                {
                    (plateText, vehicleClass, recogSuccess, errorMessage) =
                        await _plateRecognitionService.RecognizePlateAsync(imageBytes);
                }

                if (!recogSuccess || string.IsNullOrEmpty(plateText) || plateText == "N/A")
                {
                    AppendLog($" Nhận diện thất bại (RA): {errorMessage}");
                    return CheckoutResult.Failed(CheckoutStatus.DetectionFailed);
                }

                //  CẬP NHẬT UI VỚI ẢNH
                using (new PerformanceTimer("UPDATE_UI_SNAPSHOT_OUT_PROCESS", AppendLog))
                {
                    using var ms = new MemoryStream(imageBytes);
                    var avaloniaBitmap = new Bitmap(ms);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        SnapshotOut = avaloniaBitmap;
                        PlateNumberOut = plateText;
                        Snapshot3 = Snapshot2;
                        Snapshot2 = Snapshot1;
                        ms.Position = 0;
                        Snapshot1 = new Bitmap(ms);
                    });
                }

                AppendLog($" Biển OUT: {plateText} ({vehicleClass})");

                // TÌM LƯỢT VÀO 
                CardEventModel? existingEvent;
                using (new PerformanceTimer("DB_FIND_EXISTING_EVENT", AppendLog))
                {
                    existingEvent = _cardEventRepo.FindCardEventByPlate(plateText);
                }

                if (existingEvent == null)
                {
                    return CheckoutResult.Failed(CheckoutStatus.NotFound);
                }

                // TÍNH PHÍ
                var datetimeOut = DateTime.Now;
                var timeParked = datetimeOut - existingEvent.DatetimeIn.GetValueOrDefault();
                var hours = Math.Max(1, Math.Ceiling(timeParked.TotalHours));
                var fee = (decimal)hours * 5000m;

                //  LƯU ẢNH 
                using (new PerformanceTimer("SAVE_IMAGE_OUT_PROCESS", AppendLog))
                {
                    SaveVehicleImage(imageBytes, false, plateText);
                }

                //  CẬP NHẬT DATABASE 
                bool updated;
                using (new PerformanceTimer("DB_UPDATE_CHECK_OUT_PROCESS", AppendLog))
                {
                    updated = _cardEventRepo.UpdateCardEventOut(existingEvent.Id, datetimeOut, fee);
                }

                if (!updated)
                {
                    AppendLog(" Lỗi cập nhật DB khi check-out");
                    return CheckoutResult.Failed(CheckoutStatus.Error);
                }

                //  CẬP NHẬT UI KẾT QUẢ
                using (new PerformanceTimer("UPDATE_UI_RESULT_OUT_PROCESS", AppendLog))
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        VehicleDateTimeOut = datetimeOut.ToString("HH:mm:ss dd/MM/yyyy");
                        ParkingFeeOut = $"{fee:N0} VNĐ";
                        StatusMessageOut = " Đã ghi nhận xe ra";
                    });
                }

                return CheckoutResult.Success(plateText);
            }
            catch (Exception ex)
            {
                AppendLog($"❌ ProcessCheckoutAsync exception: {ex.Message}");
                return CheckoutResult.Failed(CheckoutStatus.Error);
            }
            finally
            {
                totalTimer.Dispose();
            }
        }

        // Capture snapshot from OUT player and return bytes
        private async Task<byte[]?> CaptureSnapshotOutAsync(string? plateCandidate)
        {
            var timer = new PerformanceTimer("CAPTURE_SNAPSHOT_OUT_ASYNC", AppendLog);

            try
            {
                //  LINUX: Dùng ICameraProvider
                if (_isLinux)
                {
                    if (_cameraProviderOut == null || !_cameraProviderOut.IsPlaying)
                    {
                        AppendLog(" Camera OUT không hoạt động");
                        return null;
                    }
                    return _cameraProviderOut.GetCurrentFrameBytes();
                }
                //  WINDOWS: Dùng MediaPlayer/VLC
                else
                {
                    if (MediaPlayerOut == null)
                    {
                        AppendLog(" Camera OUT null");
                        return null;
                    }

                    if (!MediaPlayerOut.IsPlaying)
                    {
                        AppendLog(" Camera OUT chưa phát, thử play...");
                        try
                        {
                            MediaPlayerOut.Play();
                            await Task.Delay(1200);
                        }
                        catch { }

                        if (!MediaPlayerOut.IsPlaying)
                        {
                            AppendLog(" Camera OUT không thể play");
                            return null;
                        }
                    }

                    string tempFile = Path.GetTempFileName() + ".jpg";
                    bool success = _cameraService.TakeSnapshot(MediaPlayerOut, tempFile);
                    if (!success || !File.Exists(tempFile))
                    {
                        AppendLog(" Chụp ảnh OUT thất bại");
                        return null;
                    }

                    var bytes = await File.ReadAllBytesAsync(tempFile);
                    File.Delete(tempFile);
                    return bytes;
                }
            }
            catch (Exception ex)
            {
                AppendLog($" CaptureSnapshotOutAsync exception: {ex.Message}");
                return null;
            }
            finally
            {
                timer.Dispose();
            }
        }

        private async Task ClearOutUI()
        {
            try
            {
                await Task.Delay(1000);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    PlateNumberOut = "";
                    VehicleDateTimeOut = "";
                    ParkingFeeOut = "";
                    SnapshotOut = null;
                });
            }
            catch { }
        }

        private async Task<byte[]?> CaptureSnapshotInAsync(string? plateCandidate)
        {
            try
            {
                if (_isLinux)
                {
                    if (_cameraProviderIn == null || !_cameraProviderIn.IsPlaying)
                    {
                        AppendLog(" Camera IN không hoạt động");
                        return null;
                    }
                    return _cameraProviderIn.GetCurrentFrameBytes();
                }
                else
                {
                    if (MediaPlayerIn == null || !MediaPlayerIn.IsPlaying)
                    {
                        AppendLog(" Camera IN không hoạt động");
                        return null;
                    }

                    string tempFile = Path.GetTempFileName() + ".jpg";
                    bool success = _cameraService.TakeSnapshot(MediaPlayerIn, tempFile);
                    if (!success || !File.Exists(tempFile))
                    {
                        return null;
                    }

                    var bytes = await File.ReadAllBytesAsync(tempFile);
                    File.Delete(tempFile);
                    return bytes;
                }
            }
            catch (Exception ex)
            {
                AppendLog($" CaptureSnapshotInAsync exception: {ex.Message}");
                return null;
            }
        }

        private void LoadKzController()
        {
            var s = SettingsManager.Settings.KzE02Controller;
            if (s == null || !s.Enabled)
            {
                AppendLog(" KZ-E02 Controller bị tắt trong settings");
                return;
            }

            if (_kzService != null)
            {
                Debug.WriteLine(" Đã có KzService, dispose trước");
                _kzService.Dispose();
                _kzService = null;
            }

            var config = new Bdk { Comport = s.Ip, Baudrate = s.Port.ToString() };
            _kzService = new KzE02NetService(config);

            _kzService.OnLoopInEvent += async (_) =>
            {
                await Dispatcher.UIThread.InvokeAsync(async () => await HandleLoopIn());
            };

            _kzService.OnLoopOutEvent += async (_) =>
            {
                await Dispatcher.UIThread.InvokeAsync(async () => await HandleLoopOut());
            };

            _kzService.OnCardEvent += async (d) =>
            {
                // Sử dụng PerformanceTimer để đo delay handler
                using var timer = new PerformanceTimer("HANDLE_CARD_EVENT_TOTAL", AppendLog);

                // Post lên UI thread chỉ cho phần cần UI (như cập nhật StatusMessage), còn xử lý nặng chạy async riêng
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    try
                    {
                        var cardNumber = d?.GetValueOrDefault("card") ?? "";
                        var reader = d?.GetValueOrDefault("reader") ?? "";

                        AppendLog($" Nhận thẻ: {cardNumber}");
                        AppendLog($" READER RAW = {reader}");

                        // XÁC ĐỊNH VÀO / RA theo reader - thêm check lỗi
                        bool isCheckIn;
                        if (reader == "03")
                        {
                            isCheckIn = true;
                        }
                        else if (reader == "04")
                        {
                            isCheckIn = false;
                        }
                        else
                        {
                            AppendLog($"❌ Reader không hợp lệ: {reader} - Bỏ qua event");
                            return;  // Không xử lý nếu reader sai
                        }

                        AppendLog($" Xác định cổng: {(isCheckIn ? "VÀO (03)" : "RA (04)")}");

                        // Gọi HandleCardSwipe async, không await ở đây để không block UI thread lâu
                        // Thay vào đó, chạy như task background
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await HandleCardSwipe(cardNumber, isCheckIn);
                            }
                            catch (Exception ex)
                            {
                                AppendLog($"❌ Lỗi xử lý thẻ: {ex.Message}");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"❌ Lỗi OnCardEvent: {ex.Message}");
                    }
                });
            };
                                   
            _kzService.OnError += (msg) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    StatusMessage = $"KZ-E02: {msg}";
                    AppendLog($" Lỗi KZ-E02: {msg}");
                });
            };

            _kzService.OnConnectionStatusChanged += (ok) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var status = ok ? " Online" : " Offline";
                    StatusMessage = $"Controller {status}";
                    AppendLog($" KZ-E02: {status}");
                });
            };

            AppendLog($" Khởi tạo KZ-E02: {s.Ip}:{s.Port}");
            _ = _kzService.StartAsync();
        }

        private async Task HandleLoopIn()
        {
            using var timer = new PerformanceTimer("HANDLE_LOOP_IN", AppendLog);
            IsCheckInMode = true;
            await Dispatcher.UIThread.InvokeAsync(() => CheckInCommand.Execute(Unit.Default));
        }

        private async Task HandleLoopOut()
        {
            using var timer = new PerformanceTimer("HANDLE_LOOP_OUT", AppendLog);
            IsCheckInMode = false;
            await Dispatcher.UIThread.InvokeAsync(() => CheckOutCommand.Execute(Unit.Default));
        }

        private async Task HandleCardSwipe(string cardNumber, bool isCheckIn)
        {
            var totalTimer = new PerformanceTimer("TOTAL_CARD_SWIPE", AppendLog);

            try
            {
                AppendLog($" Quẹt thẻ: {cardNumber} | Cổng: {(isCheckIn ? "VÀO" : "RA")}");
                AppendLog($" Đang tìm thẻ {cardNumber} trong DB...");

                CardModel? card;

                // TÌM THẺ TRONG DB 
               using (new PerformanceTimer("DB_FIND_CARD", AppendLog))
                {
                    card = _cardRepo.FindCardByNumber(cardNumber);
                }

                if (card != null)
                {
                    // Lưu mã thẻ
                    if (isCheckIn)
                        _currentCardNumberIn = cardNumber;
                    else
                        _currentCardNumberOut = cardNumber;

                    AppendLog($" Tìm thấy thẻ: {card.CardNumber} - {card.Plate1}");

                    //  VALIDATE THẺ 
                    (bool isValid, string errorMessage) validationResult;
                    using (new PerformanceTimer("VALIDATE_CARD", AppendLog))
                    {
                        validationResult = _cardRepo.ValidateCard(card);
                    }

                    if (!validationResult.isValid)
                    {
                        // Thẻ không hợp lệ
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (isCheckIn)
                            {
                                PlateNumberIn = card.Plate1;
                                CardGroupIn = card.CardGroupName;
                                ExpireDateIn = card.ExpireDate?.ToString("dd/MM/yyyy") ?? "---";
                                VehicleDateTimeIn = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
                                StatusMessageIn = $"❌ {validationResult.errorMessage}";
                            }
                            else
                            {
                                PlateNumberOut = card.Plate1;
                                CardGroupOut = card.CardGroupName;
                                ExpireDateOut = card.ExpireDate?.ToString("dd/MM/yyyy") ?? "---";
                                VehicleDateTimeOut = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
                                StatusMessageOut = $" {validationResult.errorMessage}";
                            }
                        });

                        AppendLog($"❌ Thẻ không hợp lệ: {validationResult.errorMessage}");
                        return;
                    }

                    //  CẬP NHẬT UI 
                    using (new PerformanceTimer("UPDATE_UI_CARD_INFO", AppendLog))
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (isCheckIn)
                            {
                                PlateNumberIn = card.Plate1;
                                CardGroupIn = card.CardGroupName;
                                ExpireDateIn = card.ExpireDate?.ToString("dd/MM/yyyy") ?? "---";
                                VehicleDateTimeIn = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
                                StatusMessageIn = " Thẻ hợp lệ";
                            }
                            else
                            {
                                PlateNumberOut = card.Plate1;
                                CardGroupOut = card.CardGroupName;
                                ExpireDateOut = card.ExpireDate?.ToString("dd/MM/yyyy") ?? "---";
                                VehicleDateTimeOut = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
                                StatusMessageOut = " Thẻ hợp lệ";
                            }
                        });
                    }

                    AppendLog($" Biển số: {card.Plate1}");
                    AppendLog($" Nhóm thẻ: {card.CardGroupName}");

                    //  CHECK-IN/OUT
                    if (isCheckIn)
                        await CaptureAndCheckInWithCard(card);
                    else
                        await CaptureAndCheckOutWithCard(card);
                }
                else
                {
                    // Không tìm thấy thẻ
                    AppendLog($" Thẻ {cardNumber} không có trong DB");

                    if (isCheckIn)
                    {
                        await CaptureAndCheckInAsync();
                    }
                    else
                    {
                        await CheckOutAsync();
                    }
                }
            }
            finally
            {
                totalTimer.Dispose();
            }
        }

        private async Task CaptureAndCheckInWithCard(CardModel card)
        {
            var totalTimer = new PerformanceTimer("CHECK_IN_WITH_CARD", AppendLog);

            try
            {
                var datetimeIn = DateTime.Now;
                byte[]? imageBytes = null;
                string plateFromImage = "N/A";
                bool plateMatch = true;

                //  CHỤP ẢNH 
                using (new PerformanceTimer("CAPTURE_IMAGE_IN", AppendLog))
                {
                    if (_isLinux)
                    {
                        if (_cameraProviderIn != null && _cameraProviderIn.IsPlaying)
                        {
                            imageBytes = _cameraProviderIn.GetCurrentFrameBytes();
                        }
                    }
                    else
                    {
                        if (MediaPlayerIn != null && MediaPlayerIn.IsPlaying)
                        {
                            string tempFile = Path.GetTempFileName() + ".jpg";
                            bool success = _cameraService.TakeSnapshot(MediaPlayerIn, tempFile);
                            if (success && File.Exists(tempFile))
                            {
                                imageBytes = await File.ReadAllBytesAsync(tempFile);
                                File.Delete(tempFile);
                            }
                        }
                    }
                }

                //  NHẬN DIỆN BIỂN SỐ 
                if (imageBytes != null)
                {
                    using (new PerformanceTimer("PLATE_RECOGNITION_IN", AppendLog))
                    {
                        var (plateText, vehicleClass, recogSuccess, errorMessage) =
                            await _plateRecognitionService.RecognizePlateAsync(imageBytes);

                        plateFromImage = plateText;

                        if (recogSuccess && !string.IsNullOrEmpty(plateText) &&
                            !string.Equals(plateText.Trim().ToUpper(), card.Plate1.Trim().ToUpper()))
                        {
                            plateMatch = false;
                            AppendLog($" WARNING: Biển số không khớp! Thẻ: {card.Plate1}, Ảnh: {plateText}");
                        }
                    }

                    // LƯU ẢNH 
                    using (new PerformanceTimer("SAVE_IMAGE_IN", AppendLog))
                    {
                        SaveVehicleImage(imageBytes, true, card.Plate1, !plateMatch);
                    }

                    // CẬP NHẬT UI
                    using (new PerformanceTimer("UPDATE_UI_SNAPSHOT_IN", AppendLog))
                    {
                        using var ms = new MemoryStream(imageBytes);
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            SnapshotIn = new Bitmap(ms);
                        });
                    }
                }

                // LƯU VÀO DATABASE
                Guid? newId;
                using (new PerformanceTimer("DB_INSERT_CHECK_IN", AppendLog))
                {
                    newId = _cardEventRepo.InsertCardEventIn(card.Plate1, datetimeIn, card.CardNumber);
                }

                if (newId != null)
                {
                    //  CẬP NHẬT UI KẾT QUẢ 
                    using (new PerformanceTimer("UPDATE_UI_RESULT_IN", AppendLog))
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            VehicleDateTimeIn = datetimeIn.ToString("HH:mm:ss dd/MM/yyyy");
                            if (plateMatch) StatusMessageIn = " Đã ghi nhận xe vào";
                        });
                    }

                    AppendLog($" CHECK-IN OK | {card.Plate1} | {card.CustomerName}");
                    if (!plateMatch) AppendLog("   (Với cảnh báo biển số không khớp)");

                    //  MỞ BARRIER 
                    bool openBarrier = plateMatch;
                    if (!plateMatch)
                    {
                        using (new PerformanceTimer("SHOW_BARRIER_CONFIRM", AppendLog))
                        {
                            openBarrier = await ShowBarrierConfirmAsync(true);
                        }
                    }

                    if (openBarrier)
                    {
                        using (new PerformanceTimer("OPEN_BARRIER_IN", AppendLog))
                        {
                            ManualOpenBarrier(true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi check-in: {ex.Message}");
            }
            finally
            {
                totalTimer.Dispose();
            }
        }
        private async Task CaptureAndCheckOutWithCard(CardModel card)
        {
            var totalTimer = new PerformanceTimer("CHECK_OUT_WITH_CARD", AppendLog);

            try
            {
                var datetimeOut = DateTime.Now;

                // TÌM LƯỢT VÀO
                CardEventModel? existingEvent;
                using (new PerformanceTimer("DB_FIND_CHECK_IN_EVENT", AppendLog))
                {
                    existingEvent = _cardEventRepo.FindCardEventByPlate(card.Plate1);
                }

                if (existingEvent == null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        StatusMessageOut = " Không tìm thấy lượt vào";
                    });
                    AppendLog($" Không tìm thấy lượt vào cho {card.Plate1}");
                    return;
                }

                decimal fee = 0m;
                byte[]? imageBytes = null;
                bool plateMatch = true;

                // CHỤP ẢNH 
                using (new PerformanceTimer("CAPTURE_IMAGE_OUT", AppendLog))
                {
                    if (_isLinux)
                    {
                        if (_cameraProviderOut != null && _cameraProviderOut.IsPlaying)
                        {
                            imageBytes = _cameraProviderOut.GetCurrentFrameBytes();
                        }
                    }
                    else
                    {
                        if (MediaPlayerOut != null && MediaPlayerOut.IsPlaying)
                        {
                            string tempFile = Path.GetTempFileName() + ".jpg";
                            bool success = _cameraService.TakeSnapshot(MediaPlayerOut, tempFile);
                            if (success && File.Exists(tempFile))
                            {
                                imageBytes = await File.ReadAllBytesAsync(tempFile);
                                File.Delete(tempFile);
                            }
                        }
                    }
                }

                // NHẬN DIỆN BIỂN SỐ 
                if (imageBytes != null)
                {
                    using (new PerformanceTimer("PLATE_RECOGNITION_OUT", AppendLog))
                    {
                        var (plateText, vehicleClass, recogSuccess, errorMessage) =
                            await _plateRecognitionService.RecognizePlateAsync(imageBytes);

                        if (recogSuccess && !string.IsNullOrEmpty(plateText) &&
                            !string.Equals(plateText.Trim().ToUpper(), card.Plate1.Trim().ToUpper()))
                        {
                            plateMatch = false;
                            AppendLog($" WARNING: Biển số không khớp! Thẻ: {card.Plate1}, Ảnh: {plateText}");
                        }
                    }

                    //  LƯU ẢNH 
                    using (new PerformanceTimer("SAVE_IMAGE_OUT", AppendLog))
                    {
                        SaveVehicleImage(imageBytes, false, card.Plate1, !plateMatch);
                    }

                    //  CẬP NHẬT UI 
                    using (new PerformanceTimer("UPDATE_UI_SNAPSHOT_OUT", AppendLog))
                    {
                        using var ms = new MemoryStream(imageBytes);
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            SnapshotOut = new Bitmap(ms);
                        });
                    }
                }

                //  CẬP NHẬT DATABASE 
                bool updated;
                using (new PerformanceTimer("DB_UPDATE_CHECK_OUT", AppendLog))
                {
                    updated = _cardEventRepo.UpdateCardEventOut(existingEvent.Id, datetimeOut, fee);
                }

                if (updated)
                {
                    // CẬP NHẬT UI KẾT QUẢ 
                    using (new PerformanceTimer("UPDATE_UI_RESULT_OUT", AppendLog))
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            VehicleDateTimeOut = datetimeOut.ToString("HH:mm:ss dd/MM/yyyy");
                            ParkingFeeOut = "---";
                            if (plateMatch) StatusMessageOut = " Đã ghi nhận xe ra";
                        });
                    }

                    AppendLog($" CHECK-OUT OK | {card.Plate1} | Phí: {fee:N0} VNĐ");
                    if (!plateMatch) AppendLog("   (Với cảnh báo biển số không khớp)");

                    // MỞ BARRIER 
                    bool openBarrier = plateMatch;
                    if (!plateMatch)
                    {
                        using (new PerformanceTimer("SHOW_BARRIER_CONFIRM", AppendLog))
                        {
                            openBarrier = await ShowBarrierConfirmAsync(false);
                        }
                    }

                    if (openBarrier)
                    {
                        using (new PerformanceTimer("OPEN_BARRIER_OUT", AppendLog))
                        {
                            ManualOpenBarrier(false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Lỗi check-out: {ex.Message}");
            }
            finally
            {
                totalTimer.Dispose();
            }
        }
        private void ManualOpenBarrier(bool isIn)
        {
            var s = SettingsManager.Settings.KzE02Controller;
            if (s == null || _kzService == null) return;

            var relay = isIn ? s.RelayIn : s.RelayOut;
            var gate = isIn ? "VÀO" : "RA";
            var ok = _kzService.OpenBarrier(relay, s.OpenDurationMs);

            LastManualOpen = $"{DateTime.Now:HH:mm:ss} - Mở tay cổng {gate}";
            StatusMessage = ok ? $"Đã mở cổng {gate}" : $"Lỗi mở cổng {gate}";
            if (s.ManualOpenLog) AppendLog($"MỞ TAY | Cổng {gate} | {(ok ? "OK" : "FAIL")}");
        }
        private void SaveVehicleImage(byte[] imageBytes, bool isIn, string plate, bool warning = false)
        {
            try
            {
                var path = isIn ? SettingsManager.Settings.ImagePaths.Input : SettingsManager.Settings.ImagePaths.Output;

                Debug.WriteLine($" SaveVehicleImage:");
                Debug.WriteLine($"   Gate: {(isIn ? "VÀO" : "RA")}");
                Debug.WriteLine($"   Path từ Settings: '{path}'");
                Debug.WriteLine($"   Plate: {plate}");
                Debug.WriteLine($"   Warning: {warning}");

                if (string.IsNullOrEmpty(path))
                {
                    AppendLog($" Chưa cấu hình đường dẫn lưu ảnh {(isIn ? "VÀO" : "RA")}");
                    Debug.WriteLine($" PATH NULL/EMPTY!");
                    return;
                }

                Directory.CreateDirectory(path);
                var tag = warning ? "_WARNING" : "";
                var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{plate.Replace("/", "-")}{tag}.jpg";
                var fullPath = Path.Combine(path, fileName);

                Debug.WriteLine($"   Full Path: {fullPath}");

                File.WriteAllBytes(fullPath, imageBytes);

                AppendLog($" Lưu ảnh: {fullPath}");
                Debug.WriteLine($" Đã lưu ảnh thành công!");
            }
            catch (Exception ex)
            {
                AppendLog($" Lỗi lưu ảnh: {ex.Message}");
                Debug.WriteLine($" Exception: {ex.Message}");
                Debug.WriteLine($"   StackTrace: {ex.StackTrace}");
            }
        }
        private async Task<bool> ShowBarrierConfirmAsync(bool isIn)
        {
            return await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var msgBox = new Window
                {
                    Title = "Xác nhận mở barrier",
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

                var gateText = isIn ? "vào" : "ra";
                panel.Children.Add(new TextBlock
                {
                    Text = $"Biển số không khớp. Có mở barrier {gateText} không?",
                    FontSize = 16,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
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
                var noBtn = new Button
                {
                    Content = "❌ Không",
                    Width = 100,
                    Height = 40,
                    FontSize = 14
                };

                var tcs = new TaskCompletionSource<bool>();
                yesBtn.Click += (s, ev) => { msgBox.Close(); tcs.SetResult(true); };
                noBtn.Click += (s, ev) => { msgBox.Close(); tcs.SetResult(false); };

                buttons.Children.Add(yesBtn);
                buttons.Children.Add(noBtn);
                panel.Children.Add(buttons);
                msgBox.Content = panel;

                //  Await dialog để chờ hoàn tất
                await msgBox.ShowDialog(_mainWindow);

                return await tcs.Task;
            });
        }

        /// <summary>
        /// Khởi tạo 1 camera phụ (VÀO hoặc RA)
        /// </summary>
        private void InitializeExtraCamera(string rtspUrl, bool isIn, int extraNumber)
        {
            var gateName = isIn ? "VÀO" : "RA";
            Console.WriteLine($"📹 Init Camera {gateName} Extra {extraNumber}: {rtspUrl}");

            if (_isLinux)
            {
                // LINUX: Dùng ICameraProvider (FFmpeg/OpenCV)
                var provider = CameraProviderFactory.Create();

                provider.OnFrameReceived += bitmap =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        SetExtraFrame(isIn, extraNumber, bitmap);
                    });
                };

                provider.OnError += (err) =>
                {
                    Console.WriteLine($"❌ Camera {gateName} Extra {extraNumber} Error: {err}");
                };

                // Start async
                _ = Task.Run(async () =>
                {
                    var success = await provider.StartAsync(rtspUrl);
                    Console.WriteLine($"   Camera {gateName} Extra {extraNumber} start: {success}");
                });

                // Lưu reference
                StoreExtraProvider(isIn, extraNumber, provider);
            }
            else
            {
                // WINDOWS: Dùng VLC MediaPlayer
                if (_cameraService != null)
                {
                    var player = _cameraService.CreatePlayer(rtspUrl);
                    StoreExtraMediaPlayer(isIn, extraNumber, player);
                    Console.WriteLine($"   ✅ Camera {gateName} Extra {extraNumber} created (VLC)");
                }
            }
        }

        /// <summary>
        /// Set frame cho camera phụ (Linux)
        /// </summary>
        private void SetExtraFrame(bool isIn, int number, Bitmap bitmap)
        {
            if (isIn)
            {
                switch (number)
                {
                    case 1: FrameInExtra1 = bitmap; break;
                    case 2: FrameInExtra2 = bitmap; break;
                   
                }
            }
            else
            {
                switch (number)
                {
                    case 1: FrameOutExtra1 = bitmap; break;
                    case 2: FrameOutExtra2 = bitmap; break;
                    
                }
            }
        }

        /// <summary>
        /// Lưu ICameraProvider cho camera phụ (Linux)
        /// </summary>
        private void StoreExtraProvider(bool isIn, int number, ICameraProvider provider)
        {
            if (isIn)
            {
                switch (number)
                {
                    case 1: _cameraProviderInExtra1 = provider; break;
                    case 2: _cameraProviderInExtra2 = provider; break;
                   
                }
            }
            else
            {
                switch (number)
                {
                    case 1: _cameraProviderOutExtra1 = provider; break;
                    case 2: _cameraProviderOutExtra2 = provider; break;
                    
                }
            }
        }

        /// <summary>
        /// Lưu MediaPlayer cho camera phụ (Windows)
        /// </summary>
        private void StoreExtraMediaPlayer(bool isIn, int number, MediaPlayer? player)
        {
            if (isIn)
            {
                switch (number)
                {
                    case 1: MediaPlayerInExtra1 = player; break;
                    case 2: MediaPlayerInExtra2 = player; break;
                    
                }
            }
            else
            {
                switch (number)
                {
                    case 1: MediaPlayerOutExtra1 = player; break;
                    case 2: MediaPlayerOutExtra2 = player; break;
                    
                }
            }
        }

        public void Dispose()
        {
            Console.WriteLine("🧹 MainWindowViewModel.Dispose() called");

            // Windows: Dispose MediaPlayers
            if (MediaPlayerIn != null)
            {
                Console.WriteLine("   Disposing MediaPlayerIn...");
                MediaPlayerIn?.Dispose();
            }
            if (MediaPlayerOut != null)
            {
                Console.WriteLine("   Disposing MediaPlayerOut...");
                MediaPlayerOut?.Dispose();
            }

            // Windows: Dispose CameraService (chỉ khi có khởi tạo)
            if (_cameraService != null)
            {
                Console.WriteLine("   Disposing CameraService...");
                _cameraService?.Dispose();
            }

            // Linux: Dispose ICameraProvider
            if (_cameraProviderIn != null)
            {
                Console.WriteLine("   Disposing CameraProviderIn...");
                _cameraProviderIn?.Dispose();
            }
            if (_cameraProviderOut != null)
            {
                Console.WriteLine("   Disposing CameraProviderOut...");
                _cameraProviderOut?.Dispose();
            }
            // = DISPOSE CAMERA PHỤ - WINDOWS =
            MediaPlayerInExtra1?.Dispose();
            MediaPlayerInExtra2?.Dispose();
            
            MediaPlayerOutExtra1?.Dispose();
            MediaPlayerOutExtra2?.Dispose();
            

            // = DISPOSE CAMERA PHỤ - LINUX ==
            _cameraProviderInExtra1?.Dispose();
            _cameraProviderInExtra2?.Dispose();
            
            _cameraProviderOutExtra1?.Dispose();
            _cameraProviderOutExtra2?.Dispose();
            

            Console.WriteLine(" Đã dispose 4 camera phụ");

            Console.WriteLine("   Disposing PlateRecognitionService...");
            _plateRecognitionService?.Dispose();

            Console.WriteLine("   Disposing KzService...");
            _kzService?.Dispose();

            Console.WriteLine(" MainWindowViewModel disposed");
        }
        private enum CheckoutStatus
        {
            Success,
            NotFound,
            DetectionFailed,
            Error
        }
        private class CheckoutResult
        {
            public CheckoutStatus Status { get; set; }
            public string? Plate { get; set; }

            public static CheckoutResult Success(string plate)
                => new CheckoutResult { Status = CheckoutStatus.Success, Plate = plate };

            public static CheckoutResult Failed(CheckoutStatus status)
                => new CheckoutResult { Status = status };
        }
    }
}
