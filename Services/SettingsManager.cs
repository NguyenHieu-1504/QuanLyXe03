using QuanLyXe03.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace QuanLyXe03.Services
{
    public class AppSettings
    {
        //  Connection Strings
        public Dictionary<string, string> ConnectionStrings { get; set; } = new()
        {
            { "ParkingEventDb", "Server=192.168.0.102,1433;Database=MPARKINGEVENTTM;User Id=sa;Password=123;TrustServerCertificate=True;Encrypt=False;" },
            { "ParkingCardDb", "Server=192.168.0.102,1433;Database=MPARKINGKH;User Id=sa;Password=123;TrustServerCertificate=True;Encrypt=False;" }
        };

        public KzE02ControllerSettings KzE02Controller { get; set; } = new();
        public ImagePaths ImagePaths { get; set; } = new();

        // Camera Settings
        public CameraSettings CameraIn { get; set; } = new();
        public CameraSettings CameraOut { get; set; } = new();

        // Plate Recognition Settings
        public PlateRecognitionSettings PlateRecognition { get; set; } = new();

        // Login settings
        public bool RequireLogin { get; set; } = true;
        public string LoginPassword { get; set; } = "admin";
    }

    public class ImagePaths
    {
        public string Input { get; set; } = "";
        public string Output { get; set; } = "";
    }

    // Camera Settings Class
    public class CameraSettings
    {
        public string RtspUrl { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public string Name { get; set; } = "";
        public List<string> Extras { get; set; } = new();
    }


    public class KzE02ControllerSettings
    {
        public bool Enabled { get; set; } = true;
        public string Ip { get; set; } = "192.168.1.250";
        public int Port { get; set; } = 100;
        public int RelayIn { get; set; } = 1;
        public int RelayOut { get; set; } = 2;
        public int OpenDurationMs { get; set; } = 3000;
        public int PollingIntervalMs { get; set; } = 1000;
        public bool ManualOpenLog { get; set; } = true;
    }

    // Plate Recognition Settings Class
    public class PlateRecognitionSettings
    {
        public string ApiUrl { get; set; } = "http://127.0.0.1:8000/predict";
    }

    public static class SettingsManager
    {
        private static readonly string ConfigPath = "appsettings.json";
        private static AppSettings? _settings;

        public static AppSettings Settings => _settings ??= LoadSettings();

        /// <summary>
        ///  Lấy Connection String theo tên
        /// </summary>
        public static string GetConnectionString(string name)
        {
            try
            {
                if (Settings.ConnectionStrings.TryGetValue(name, out var connStr))
                {
                    Debug.WriteLine($"✅ Lấy connection string '{name}' từ config");
                    return connStr;
                }

                Debug.WriteLine($"⚠️ Không tìm thấy connection string '{name}'");
                return GetDefaultConnectionString(name);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi GetConnectionString '{name}': {ex.Message}");
                return GetDefaultConnectionString(name);
            }
        }

        /// <summary>
        /// Connection strings mặc định (fallback khi không có trong config)
        /// </summary>
        private static string GetDefaultConnectionString(string name)
        {
            Debug.WriteLine($"⚠️ Sử dụng connection string mặc định cho '{name}'");
            return name switch
            {
                "ParkingEventDb" => "Server=192.168.0.102,1433;Database=MPARKINGEVENTTM;User Id=sa;Password=123;TrustServerCertificate=True;Encrypt=False;",
                "ParkingCardDb" => "Server=192.168.0.102,1433;Database=MPARKINGKH;User Id=sa;Password=123;TrustServerCertificate=True;Encrypt=False;",
                _ => ""
            };
        }

        private static AppSettings LoadSettings()
        {
            // Lấy đường dẫn tuyệt đối
            var fullPath = Path.GetFullPath(ConfigPath);
            Debug.WriteLine($"📂 LoadSettings:");
            Debug.WriteLine($"   File: {fullPath}");
            Debug.WriteLine($"   Exists: {File.Exists(fullPath)}");

            if (File.Exists(fullPath))
            {
                try
                {
                    var json = File.ReadAllText(fullPath);
                    Debug.WriteLine($"   JSON Length: {json.Length} chars");

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    };

                    var settings = JsonSerializer.Deserialize<AppSettings>(json, options) ?? new AppSettings();

                    Debug.WriteLine($"   Input Path: '{settings.ImagePaths.Input}'");
                    Debug.WriteLine($"   Output Path: '{settings.ImagePaths.Output}'");
                    Debug.WriteLine($"   Plate API: '{settings.PlateRecognition.ApiUrl}'");
                    Debug.WriteLine($"   Connection Strings: {settings.ConnectionStrings.Count} found");

                    return settings;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Lỗi Load Settings: {ex.Message}");
                    return new AppSettings();
                }
            }

            Debug.WriteLine("⚠️ File không tồn tại, tạo mới AppSettings");
            return new AppSettings();
        }

        public static void SaveSettings()
        {
            var fullPath = Path.GetFullPath(ConfigPath);
            Debug.WriteLine($"💾 SaveSettings:");
            Debug.WriteLine($"   File: {fullPath}");
            Debug.WriteLine($"   Input Path: '{Settings.ImagePaths.Input}'");
            Debug.WriteLine($"   Output Path: '{Settings.ImagePaths.Output}'");

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    // Đảm bảo encoding UTF-8
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(Settings, options);

                Debug.WriteLine($"   JSON Preview: {json.Substring(0, Math.Min(200, json.Length))}...");

                // Ghi file với UTF-8
                File.WriteAllText(fullPath, json, System.Text.Encoding.UTF8);

                Debug.WriteLine("✅ Đã lưu file thành công!");

                // Verify lại ngay sau khi ghi
                if (File.Exists(fullPath))
                {
                    var verify = File.ReadAllText(fullPath);
                    Debug.WriteLine($"✅ Verify: File có {verify.Length} chars");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi SaveSettings: {ex.Message}");
                Debug.WriteLine($"   StackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Force reload settings từ file (dùng khi cần refresh)
        /// </summary>
        public static void ReloadSettings()
        {
            Debug.WriteLine(" Force Reload Settings...");
            _settings = null;
            var reloaded = Settings; // Trigger lazy load
            Debug.WriteLine($"   Input: '{reloaded.ImagePaths.Input}'");
            Debug.WriteLine($"   Output: '{reloaded.ImagePaths.Output}'");
            Debug.WriteLine($"   Connection Strings: {reloaded.ConnectionStrings.Count}");
        }
    }
}