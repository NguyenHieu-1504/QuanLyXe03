#  QuanLyXe03 - Quản lý Bãi xe 

Quản lý bãi xe tự động với nhận diện biển số, quẹt thẻ RFID và điều khiển barrier.

##  Tính năng

###  Hệ thống Camera (6 camera)
- **2 camera chính** (VÀO/RA): Nhận diện biển số xe tự động
- **4 camera phụ**: Giám sát góc rộng
- Hỗ trợ **Windows** (VLC) và **Linux** (FFmpeg/OpenCV)
- Layout mirror: VÀO (chính trái + phụ phải), RA (phụ trái + chính phải)
- Chụp ảnh tự động khi xe vào/ra

###  Quản lý Thẻ RFID
- Quẹt thẻ tự động qua KZ-E02 Controller
- Quản lý thẻ: thêm/sửa/xóa, phân nhóm, hạn dùng
- Liên kết thẻ với biển số xe
- Xác thực thẻ hợp lệ trước khi mở barrier

###  Điều khiển Barrier
- Tự động mở barrier khi:
  - Nhận diện biển số thành công
  - Thẻ RFID hợp lệ
- Mở thủ công bằng nút hoặc phím Space
- Xử lý cảnh báo khi biển số không khớp với thẻ

###  Nhận diện Biển số
- API nhận diện biển số (Python FastAPI)
- Hỗ trợ biển số Việt Nam
- Độ chính xác cao với camera HD

###  Báo cáo & Lịch sử
- Lịch sử xe ra vào chi tiết
- Tìm kiếm theo biển số, thời gian, loại thẻ
- Phân trang, export dữ liệu
- Tính phí đỗ xe tự động

##  Công nghệ

- **Frontend**: Avalonia UI (cross-platform)
- **Backend**: .NET 8.0
- **Database**: SQL Server
- **Camera**: LibVLCSharp (Windows), FFmpeg/OpenCV (Linux)
- **Nhận diện**: Python + YOLOv8/PaddleOCR
- **Hardware**: KZ-E02 Controller, RFID Reader, ip Cameras

##  Yêu cầu hệ thống

### Windows
- .NET 8.0 SDK
- VLC Media Player (libraries)
- SQL Server 2019+

### Linux
- .NET 8.0 SDK
- FFmpeg hoặc OpenCV
- SQL Server (Docker hoặc remote)

##  Cài đặt

### 1. Clone repository
```bash
git clone https://github.com/NguyenHieu-1504/QuanLyXe03.git
cd QuanLyXe03
```

### 2. Cấu hình appsettings.json
```bash
cp appsettings.example.json appsettings.json
notepad appsettings.json  # Sửa thông tin DB, camera, controller
```

### 3. Restore dependencies
```bash
dotnet restore
```

### 4. Chạy ứng dụng
```bash
dotnet run
```

## ⚙️ Cấu hình

File `appsettings.json`:

- **ConnectionStrings**: Kết nối SQL Server
- **KzE02Controller**: IP và cổng của bộ điều khiển barrier
- **CameraIn/CameraOut**: RTSP URLs của camera
- **PlateRecognition**: API endpoint nhận diện biển số
- **ImagePaths**: Đường dẫn lưu ảnh xe vào/ra

##  Screenshots

(Thêm ảnh sau)


Nguyễn Hiếu - [@NguyenHieu-1504](https://github.com/NguyenHieu-1504)

Project Link: [https://github.com/NguyenHieu-1504/QuanLyXe03](https://github.com/NguyenHieu-1504/QuanLyXe03)
