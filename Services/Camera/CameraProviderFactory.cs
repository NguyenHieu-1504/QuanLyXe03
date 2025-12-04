using System;
using System.Runtime.InteropServices;

namespace QuanLyXe03.Services.Camera
{
    public static class CameraProviderFactory
    {
        public static ICameraProvider Create()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("🪟 Detected Windows - Using VLC provider");
                return new VlcCameraProvider();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Console.WriteLine("🐧 Linux detected - Using FFmpeg provider (ổn định, low latency)");
                return new FFmpegCameraProvider();  // <<<--- DÒNG NÀY QUAN TRỌNG NHẤT
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Console.WriteLine("🍎 Detected macOS - Using OpenCV provider");
                return new OpenCvCameraProvider();
            }

            Console.WriteLine("❓ Unknown OS - Fallback to FFmpeg provider");
            return new FFmpegCameraProvider();
        }

        public static ICameraProvider Create(CameraProviderType type)
        {
            return type switch
            {
                CameraProviderType.VLC => new VlcCameraProvider(),
                CameraProviderType.OpenCV => new OpenCvCameraProvider(),
                CameraProviderType.FFmpeg => new FFmpegCameraProvider(),  // Thêm enum nếu cần
                _ => Create()
            };
        }
    }

    public enum CameraProviderType
    {
        Auto,
        VLC,
        OpenCV,
        FFmpeg
    }
}