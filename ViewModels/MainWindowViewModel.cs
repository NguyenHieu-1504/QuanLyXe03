using ReactiveUI;
using ReactiveUI.Avalonia;
using System;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using QuanLyXe03.Services;
using Avalonia.Threading;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using QuanLyXe03.Models;
using QuanLyXe03.Repositories;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
using Avalonia.Media.Imaging;
using System.Collections.Generic;

namespace QuanLyXe03.ViewModels
{
    public class MainWindowViewModel : ReactiveObject, IDisposable
    {
        private readonly CameraService _cameraService;
        private readonly PlateRecognitionService _plateRecognitionService;

        private MediaPlayer _mediaPlayerIn = null!;
        private MediaPlayer _mediaPlayerOut = null!;



        // THÊM: Thông tin xe
        private string _plateNumber = "---";
        public string PlateNumber
        {
            get => _plateNumber;
            set => this.RaiseAndSetIfChanged(ref _plateNumber, value);
        }

        private string _vehicleDateTime = "---";
        public string VehicleDateTime
        {
            get => _vehicleDateTime;
            set => this.RaiseAndSetIfChanged(ref _vehicleDateTime, value);
        }

        private string _parkingFee = "---";
        public string ParkingFee
        {
            get => _parkingFee;
            set => this.RaiseAndSetIfChanged(ref _parkingFee, value);
        }

        // THÊM: 3 ảnh snapshot
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

        //  THÊM: Lưu đường dẫn 3 ảnh gần nhất
        private readonly Queue<string> _recentSnapshots = new Queue<string>(3);

        

        

        public MediaPlayer MediaPlayerIn
        {
            get => _mediaPlayerIn;
            set => this.RaiseAndSetIfChanged(ref _mediaPlayerIn, value);
        }

        public MediaPlayer MediaPlayerOut
        {
            get => _mediaPlayerOut;
            set => this.RaiseAndSetIfChanged(ref _mediaPlayerOut, value);
        }

        public ReactiveCommand<Unit, Unit>? CaptureSnapshotCommand { get; private set; }

        public MainWindowViewModel()
        {
            RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;

            _cameraService = new CameraService();
            _plateRecognitionService = new PlateRecognitionService();
            _cardEventRepo = new CardEventRepository();

            //  Sử dụng camera IP RTSP
            string rtspUrlIn = "rtsp://192.168.1.30:554/stream1";
            Debug.WriteLine($"🎥 Đang kết nối camera VÀO: {rtspUrlIn}");

            MediaPlayerIn = _cameraService.CreatePlayer(rtspUrlIn);

            if (MediaPlayerIn == null)
            {
                Debug.WriteLine("❌ Không thể tạo MediaPlayer cho camera VÀO");
            }

            //camera RA
             string rtspUrlOut = "rtsp://admin:123456@192.168.1.100:554/Streaming/Channels/101";
            MediaPlayerOut = _cameraService.CreatePlayer(rtspUrlOut);

            if (MediaPlayerOut == null)
            {
                Debug.WriteLine("❌ Không thể tạo MediaPlayer cho camera VÀO");
            }

            CaptureSnapshotCommand = ReactiveCommand.CreateFromTask(CaptureSnapshotAsync);


            CheckInCommand = ReactiveCommand.CreateFromTask(CheckInAsync); //  THÊM
            CheckOutCommand = ReactiveCommand.CreateFromTask(CheckOutAsync);


        }


       




        /// <summary>
        ///  CHỤP ẢNH VÀ NHẬN DIỆN BIỂN SỐ
        /// </summary>
        private async Task CaptureSnapshotAsync()
        {
            if (MediaPlayerIn == null)
            {
                Debug.WriteLine("⚠️ Không thể chụp — MediaPlayerIn bằng null.");
                return;
            }

            if (!MediaPlayerIn.IsPlaying)
            {
                Debug.WriteLine("⚠️ Camera chưa phát — không thể chụp ảnh.");
                return;
            }

            try
            {
                Debug.WriteLine("📸 Bắt đầu chụp ảnh từ camera VÀO...");

                // Tạo thư mục nếu chưa tồn tại
                string pictureInPath = @"D:\Pic\PicIn";
                if (!Directory.Exists(pictureInPath))
                {
                    Directory.CreateDirectory(pictureInPath);
                    Debug.WriteLine($"✅ Đã tạo thư mục: {pictureInPath}");
                }

                // Tạo tên file theo timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string snapshotPath = Path.Combine(pictureInPath, $"camera_in_{timestamp}.jpg");

                // Xóa file cũ nếu tồn tại
                if (File.Exists(snapshotPath))
                {
                    File.Delete(snapshotPath);
                }

                // Chụp ảnh
                await Task.Run(() =>
                {
                    bool result = MediaPlayerIn.TakeSnapshot(0, snapshotPath, 0, 0);
                    Debug.WriteLine($"TakeSnapshot result: {result}");
                });

                // Đợi file được tạo
                int maxWaitTime = 5000;
                int waited = 0;
                while (!File.Exists(snapshotPath) && waited < maxWaitTime)
                {
                    await Task.Delay(200);
                    waited += 200;
                }

                if (!File.Exists(snapshotPath))
                {
                    Debug.WriteLine("❌ Không thể chụp ảnh từ camera.");
                    return;
                }

                Debug.WriteLine($"✅ Ảnh đã lưu tại: {snapshotPath}");

                // Đọc ảnh để gửi API
                byte[] imageBytes = await File.ReadAllBytesAsync(snapshotPath);

                Debug.WriteLine("🔄 Đang gửi ảnh đến API nhận diện...");

                // Gọi API nhận diện
                var (plateText, vehicleClass, success, errorMessage) = await _plateRecognitionService.RecognizePlate(imageBytes);

                if (success)
                {
                    Debug.WriteLine($"✅ Nhận diện thành công: {plateText} - {vehicleClass}");

                    // Cập nhật thông tin xe
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        PlateNumber = plateText;
                        VehicleDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                        // Tính tiền theo loại xe
                        if (vehicleClass.ToLower().Contains("motor") || vehicleClass.ToLower().Contains("xe máy"))
                        {
                            ParkingFee = "5,000 VNĐ";
                        }
                        else if (vehicleClass.ToLower().Contains("car") || vehicleClass.ToLower().Contains("ô tô"))
                        {
                            ParkingFee = "15,000 VNĐ";
                        }
                        else
                        {
                            ParkingFee = "0 VNĐ";
                        }
                    });
                }
                else
                {
                    Debug.WriteLine($"❌ Nhận diện thất bại: {errorMessage}");

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        PlateNumber = "Không nhận diện được";
                        VehicleDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        ParkingFee = "---";
                    });
                }

                // Cập nhật 3 ảnh snapshot (ảnh mới nhất ở ô 1)
                await UpdateSnapshotImages(snapshotPath);

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi khi chụp ảnh: {ex.Message}");
                Debug.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Cập nhật 3 ảnh snapshot, ảnh mới nhất hiển thị ở ô 1
        /// </summary>
        private async Task UpdateSnapshotImages(string newImagePath)
        {
            try
            {
                // Thêm ảnh mới vào queue
                _recentSnapshots.Enqueue(newImagePath);

                // Giữ tối đa 3 ảnh
                while (_recentSnapshots.Count > 3)
                {
                    _recentSnapshots.Dequeue();
                }

                // Lấy danh sách ảnh (mới nhất → cũ nhất)
                var snapshotList = _recentSnapshots.Reverse().ToList();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // Ảnh 1: Mới nhất
                    if (snapshotList.Count > 0 && File.Exists(snapshotList[0]))
                    {
                        using var stream1 = File.OpenRead(snapshotList[0]);
                        Snapshot1 = new Bitmap(stream1);
                    }

                    // Ảnh 2: Thứ 2
                    if (snapshotList.Count > 1 && File.Exists(snapshotList[1]))
                    {
                        using var stream2 = File.OpenRead(snapshotList[1]);
                        Snapshot2 = new Bitmap(stream2);
                    }
                    else
                    {
                        Snapshot2 = null;
                    }

                    // Ảnh 3: Thứ 3
                    if (snapshotList.Count > 2 && File.Exists(snapshotList[2]))
                    {
                        using var stream3 = File.OpenRead(snapshotList[2]);
                        Snapshot3 = new Bitmap(stream3);
                    }
                    else
                    {
                        Snapshot3 = null;
                    }
                });

                Debug.WriteLine($"✅ Đã cập nhật {snapshotList.Count} ảnh snapshot");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi cập nhật snapshot images: {ex.Message}");
            }
        }


        /// <summary>
        /// Ghi vào - Chụp camera VÀO + Lưu vào DB
        /// </summary>
        private async Task CheckInAsync()
        {
            if (MediaPlayerIn == null)
            {
                Debug.WriteLine("⚠️ MediaPlayerIn null - không thể chụp.");
                StatusMessage = "❌ Camera không sẵn sàng";
                return;
            }

            if (!MediaPlayerIn.IsPlaying)
            {
                Debug.WriteLine("⚠️ Camera chưa phát.");
                StatusMessage = "❌ Camera chưa kết nối";
                return;
            }

            try
            {
                Debug.WriteLine("📸 Bắt đầu Ghi vào...");
                StatusMessage = "⏳ Đang chụp ảnh...";

                // Tạo thư mục
                string pictureInPath = @"D:\Pic\PicIn";
                if (!Directory.Exists(pictureInPath))
                {
                    Directory.CreateDirectory(pictureInPath);
                }

                // Tạo tên file
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string snapshotPath = Path.Combine(pictureInPath, $"camera_in_{timestamp}.jpg");

                // Xóa file cũ nếu tồn tại
                if (File.Exists(snapshotPath))
                {
                    File.Delete(snapshotPath);
                }

                // Chụp ảnh
                await Task.Run(() =>
                {
                    MediaPlayerIn.TakeSnapshot(0, snapshotPath, 0, 0);
                });

                // Đợi file được tạo
                int maxWaitTime = 5000;
                int waited = 0;
                while (!File.Exists(snapshotPath) && waited < maxWaitTime)
                {
                    await Task.Delay(200);
                    waited += 200;
                }

                if (!File.Exists(snapshotPath))
                {
                    Debug.WriteLine("❌ Không thể chụp ảnh từ camera.");
                    StatusMessage = "❌ Chụp ảnh thất bại";
                    return;
                }

                Debug.WriteLine($"✅ Ảnh đã lưu tại: {snapshotPath}");
                StatusMessage = "⏳ Đang nhận diện biển số...";

                // Đọc ảnh để gửi API
                byte[] imageBytes = await File.ReadAllBytesAsync(snapshotPath);

                // Gọi API nhận diện
                var (plateText, vehicleClass, success, errorMessage) = await _plateRecognitionService.RecognizePlate(imageBytes);

                if (success && !string.IsNullOrEmpty(plateText))
                {
                    Debug.WriteLine($"✅ Nhận diện thành công: {plateText} - {vehicleClass}");

                    // Cập nhật UI
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        PlateNumber = plateText;
                        VehicleDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        ParkingFee = "---"; // Chưa hiện tiền
                        StatusMessage = $"✅ Xe vào: {plateText}";
                    });

                    // Lưu vào database
                    var eventId = _cardEventRepo.InsertCardEventIn(plateText, DateTime.Now);

                    if (eventId.HasValue)
                    {
                        Debug.WriteLine($"✅ Đã lưu vào database: {plateText}");
                    }
                    else
                    {
                        Debug.WriteLine("⚠️ Không thể lưu vào database");
                    }

                    // Cập nhật snapshot
                    await UpdateSnapshotImages(snapshotPath);
                }
                else
                {
                    Debug.WriteLine($"❌ Nhận diện thất bại: {errorMessage}");

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        PlateNumber = "Không nhận diện được";
                        VehicleDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        ParkingFee = "---";
                        StatusMessage = "❌ Không nhận diện được biển số";
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi CheckInAsync: {ex.Message}");
                StatusMessage = $"❌ Lỗi: {ex.Message}";
            }
        }



        /// <summary>
        /// Ghi ra - Chụp camera RA + So sánh + Tính tiền
        /// </summary>
        private async Task CheckOutAsync()
        {
            if (MediaPlayerIn == null)
            {
                Debug.WriteLine("⚠️ MediaPlayerIn null - không thể chụp.");
                StatusMessage = "❌ Camera không sẵn sàng";
                return;
            }

            if (!MediaPlayerIn.IsPlaying)
            {
                Debug.WriteLine("⚠️ Camera chưa phát.");
                StatusMessage = "❌ Camera chưa kết nối";
                return;
            }

            try
            {
                Debug.WriteLine("📸 Bắt đầu Ghi ra...");
                StatusMessage = "⏳ Đang chụp ảnh...";

                // Tạo thư mục
                string pictureOutPath = @"D:\Pic\PicOut";
                if (!Directory.Exists(pictureOutPath))
                {
                    Directory.CreateDirectory(pictureOutPath);
                }

                // Tạo tên file
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string snapshotPath = Path.Combine(pictureOutPath, $"camera_out_{timestamp}.jpg");

                if (File.Exists(snapshotPath))
                {
                    File.Delete(snapshotPath);
                }

                // Chụp ảnh (tạm dùng camera VÀO, sau này sẽ dùng camera RA)
                await Task.Run(() =>
                {
                    MediaPlayerIn.TakeSnapshot(0, snapshotPath, 0, 0);
                });

                // Đợi file
                int maxWaitTime = 5000;
                int waited = 0;
                while (!File.Exists(snapshotPath) && waited < maxWaitTime)
                {
                    await Task.Delay(200);
                    waited += 200;
                }

                if (!File.Exists(snapshotPath))
                {
                    Debug.WriteLine("❌ Không thể chụp ảnh.");
                    StatusMessage = "❌ Chụp ảnh thất bại";
                    return;
                }

                Debug.WriteLine($"✅ Ảnh đã lưu tại: {snapshotPath}");
                StatusMessage = "⏳ Đang nhận diện biển số...";

                // Đọc ảnh
                byte[] imageBytes = await File.ReadAllBytesAsync(snapshotPath);

                // Gọi API nhận diện
                var (plateText, vehicleClass, success, errorMessage) = await _plateRecognitionService.RecognizePlate(imageBytes);

                if (success && !string.IsNullOrEmpty(plateText))
                {
                    Debug.WriteLine($"✅ Nhận diện thành công: {plateText} - {vehicleClass}");

                    // Tìm trong database
                    var cardEvent = _cardEventRepo.FindCardEventByPlate(plateText);

                    if (cardEvent != null)
                    {
                        // ✅ Tìm thấy - Biển số khớp
                        Debug.WriteLine($"✅ Tìm thấy xe trong database: {plateText}");

                        // Tính tiền theo loại xe
                        decimal parkingFee = 0;
                        if (vehicleClass.ToLower().Contains("motor") || vehicleClass.ToLower().Contains("xe máy"))
                        {
                            parkingFee = 5000;
                        }
                        else if (vehicleClass.ToLower().Contains("car") || vehicleClass.ToLower().Contains("ô tô"))
                        {
                            parkingFee = 15000;
                        }

                        Debug.WriteLine($"💰 Loại xe: {vehicleClass} → Tiền: {parkingFee:N0} VNĐ");

                        // Update database
                        bool updated = _cardEventRepo.UpdateCardEventOut(cardEvent.Id, DateTime.Now, parkingFee);

                        if (updated)
                        {
                            // Cập nhật UI
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                PlateNumber = plateText;
                                VehicleDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                ParkingFee = $"{parkingFee:N0} VNĐ";
                                StatusMessage = "✅ Xin mời ra";
                            });

                            Debug.WriteLine("✅ Đã cập nhật database");
                        }
                        else
                        {
                            StatusMessage = "⚠️ Lỗi cập nhật database";
                        }
                    }
                    else
                    {
                        // ❌ Không tìm thấy - Biển số không khớp
                        Debug.WriteLine($"⚠️ KHÔNG tìm thấy xe trong database: {plateText}");

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            PlateNumber = plateText;
                            VehicleDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            ParkingFee = "---";
                            StatusMessage = "⚠️ Cảnh báo biển số";
                        });
                    }
                }
                else
                {
                    Debug.WriteLine($"❌ Nhận diện thất bại: {errorMessage}");

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        PlateNumber = "Không nhận diện được";
                        VehicleDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        ParkingFee = "---";
                        StatusMessage = "❌ Không nhận diện được biển số";
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi CheckOutAsync: {ex.Message}");
                StatusMessage = $"❌ Lỗi: {ex.Message}";
            }
        }



        //  Repository cho CardEvent
        private readonly CardEventRepository _cardEventRepo;

        //  Message hiển thị trạng thái
        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        //  Command cho Ghi vào và Ghi ra
        public ReactiveCommand<Unit, Unit> CheckInCommand { get; }
        public ReactiveCommand<Unit, Unit> CheckOutCommand { get; }





        public void Dispose()
        {
            MediaPlayerIn?.Dispose();
            MediaPlayerOut?.Dispose();
            _cameraService?.Dispose();
            _plateRecognitionService?.Dispose();
        }
    }
}