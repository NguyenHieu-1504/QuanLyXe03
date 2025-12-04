using Avalonia.Media.Imaging;
using System;
using System.Threading.Tasks;

namespace QuanLyXe03.Services.Camera
{
    public interface ICameraProvider : IDisposable
    {
        bool IsInitialized { get; }
        string? InitializationError { get; }
        bool IsPlaying { get; }

        /// <summary>
        /// Bắt đầu stream camera
        /// </summary>
        Task<bool> StartAsync(string rtspUrl);

        /// <summary>
        /// Dừng stream
        /// </summary>
        void Stop();

        /// <summary>
        /// Lấy frame hiện tại (low latency)
        /// </summary>
        Bitmap? GetCurrentFrame();

        /// <summary>
        /// Lấy frame dưới dạng byte[] để xử lý (nhận diện biển số)
        /// </summary>
        byte[]? GetCurrentFrameBytes();

        /// <summary>
        /// Chụp snapshot và lưu file
        /// </summary>
        Task<bool> TakeSnapshotAsync(string filename);

        /// <summary>
        /// Event khi có frame mới
        /// </summary>
        event Action<Bitmap>? OnFrameReceived;

        /// <summary>
        /// Event khi có lỗi
        /// </summary>
        event Action<string>? OnError;
    }
}