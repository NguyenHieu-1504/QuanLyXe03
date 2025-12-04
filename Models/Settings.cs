
using System.Collections.Generic;

namespace QuanLyXe03.Models
{
    public class AppSettings
    {
        public KzE02Settings? KzE02Controller { get; set; }
        public CameraSettings? CameraIn { get; set; }
        public CameraSettings? CameraOut { get; set; }
        public PlateRecognitionSettings? PlateRecognition { get; set; }
        public bool RequireLogin { get; set; } = true;
        public string LoginPassword { get; set; } = "123456"; // mặc định
    }
    public class KzE02Settings
    {
        public bool Enabled { get; set; } = false;
        public string Ip { get; set; } = "192.168.1.250";
        public int Port { get; set; } = 100;
        public int PollingIntervalMs { get; set; } = 500;
        public int OpenDurationMs { get; set; } = 1500;
        public int RelayIn { get; set; } = 1;
        public int RelayOut { get; set; } = 2;
        public bool ManualOpenLog { get; set; } = true;
    }

    public class CameraSettings
    {
        public string Url { get; set; } = "";
        public string SnapshotPath { get; set; } = "Snapshots/In";

        // Mảng URL phụ (extras) — tối đa 3 (có thể thay đổi)
        public List<string>? Extras { get; set; } = new List<string>();
    }

    public class PlateRecognitionSettings
    {
        public string ApiUrl { get; set; } = "http://127.0.0.1:8000/predict";
    }
}
