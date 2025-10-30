using LibVLCSharp.Shared;
using System;
using System.Diagnostics;
using System.IO;

namespace QuanLyXe03.Services
{
    public class CameraService : IDisposable
    {
        private LibVLC? _libVLC;
        public bool IsInitialized { get; private set; }
        public string? InitializationError { get; private set; }

        public CameraService()
        {
            try
            {


                var vlcPath = Path.Combine(AppContext.BaseDirectory, "Libs");

                // Kiểm tra thư mục VLC có tồn tại không
                if (!Directory.Exists(vlcPath))
                {
                    throw new DirectoryNotFoundException($"Không tìm thấy thư mục VLC tại: {vlcPath}");
                }
                var options = new string[]
               {
                    "--drawable-hwnd=0",         // Không tự tạo window
                     "--vout=direct3d11",         // Video output qua Direct3D11
                    "--intf=dummy",              // Không dùng interface
                    "--no-video-title-show",     // Không hiển thị title
                    "--no-osd",                  // Không hiển thị OSD
                    "--quiet",                   // Chế độ im lặng
                    "--no-spu",                  // Không subtitle
                    "--no-audio",                // Không âm thanh
                    "--aout=dummy",              // Audio output dummy
                    "--no-video-deco",           // Không decoration
                    "--no-embedded-video",       // Không embedded
                  
                    "--no-video-on-top",         // Không luôn ở trên
                    "--no-video-wallpaper",      // Không dùng làm wallpaper
                    "--no-disable-screensaver",  // Không disable screensaver
                    "--no-one-instance",         // Cho phép nhiều instance
                    "--no-playlist-enqueue",    // Không enqueue playlist
                    "--no-video-title"
               };

                _libVLC = new LibVLC(options);
                IsInitialized = true;
                Debug.WriteLine(" CameraService khởi tạo thành công");
            }
            catch (Exception ex)
            {
                IsInitialized = false;
                InitializationError = ex.Message;
                Debug.WriteLine($"❌ Lỗi khởi tạo CameraService: {ex.Message}");
                Debug.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Tạo MediaPlayer cho camera RTSP
        /// </summary>
        public MediaPlayer? CreatePlayer(string rtspUrl)
        {
            if (!IsInitialized)
            {
                Debug.WriteLine($"❌ Không thể tạo player - CameraService chưa được khởi tạo: {InitializationError}");
                return null;
            }

            if (_libVLC == null)
            {
                Debug.WriteLine("❌ _libVLC is null");
                return null;
            }

            MediaPlayer? player = null;

            try
            {
                var media = new Media(_libVLC, rtspUrl, FromType.FromLocation);

                //  Cấu hình cho RTSP stream
                media.AddOption("--network-caching=1000");
                media.AddOption("--rtsp-tcp"); // Dùng TCP thay vì UDP cho ổn định hơn
                media.AddOption("--no-video-title-show");
                media.AddOption("--live-caching=300");

                player = new MediaPlayer(_libVLC)
                {
                    EnableHardwareDecoding = false
                };

                // Đăng ký các event handlers để theo dõi trạng thái
                player.Playing += (s, e) =>
                {
                    Debug.WriteLine($"▶️ Camera bắt đầu phát: {rtspUrl}");
                };

                player.Stopped += (s, e) =>
                {
                    Debug.WriteLine($"⏹️ Camera dừng: {rtspUrl}");
                };

                player.EncounteredError += (s, e) =>
                {
                    Debug.WriteLine($"❌ Lỗi phát camera: {rtspUrl}");
                    Debug.WriteLine($"   Chi tiết: Không thể kết nối hoặc stream bị lỗi");
                };

                player.EndReached += (s, e) =>
                {
                    Debug.WriteLine($" Stream kết thúc: {rtspUrl}");
                };

                player.Buffering += (s, e) =>
                {
                    if (e.Cache < 100)
                    {
                        Debug.WriteLine($"⏳ Đang buffer: {e.Cache}%");
                    }
                };

                // Bắt đầu phát
                bool playResult = player.Play(media);

                if (!playResult)
                {
                    Debug.WriteLine($" Không thể bắt đầu phát: {rtspUrl}");
                }

                return player;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi tạo MediaPlayer cho {rtspUrl}: {ex.Message}");
                player?.Dispose();
                return null;
            }
        }

        /// <summary>
        /// Chụp ảnh với callback để xử lý kết quả
        /// </summary>
        public bool TakeSnapshot(MediaPlayer player, string filename, Action<bool, string>? onComplete = null)
        {
            if (player == null)
            {
                string error = "MediaPlayer null - không thể chụp ảnh";
                Debug.WriteLine($"❌ {error}");
                onComplete?.Invoke(false, error);
                return false;
            }

            if (!player.IsPlaying)
            {
                string error = "Camera chưa phát - không thể chụp ảnh";
                Debug.WriteLine($"⚠️ {error}");
                onComplete?.Invoke(false, error);
                return false;
            }

            try
            {
                // Đảm bảo thư mục tồn tại
                string? directory = Path.GetDirectoryName(filename);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Chụp ảnh (width=0, height=0 nghĩa là dùng kích thước gốc)
                bool result = player.TakeSnapshot(0, filename, 0, 0);

                if (result)
                {
                    // Đợi một chút để file được ghi xong
                    System.Threading.Thread.Sleep(500);

                    // Kiểm tra file có tồn tại và có kích thước hợp lệ
                    if (File.Exists(filename))
                    {
                        var fileInfo = new FileInfo(filename);
                        if (fileInfo.Length > 0)
                        {
                            Debug.WriteLine($"✅ Chụp ảnh thành công: {filename} ({fileInfo.Length} bytes)");
                            onComplete?.Invoke(true, filename);
                            return true;
                        }
                        else
                        {
                            string error = "File ảnh rỗng";
                            Debug.WriteLine($"⚠️ {error}");
                            onComplete?.Invoke(false, error);
                            return false;
                        }
                    }
                    else
                    {
                        string error = "File ảnh không được tạo";
                        Debug.WriteLine($"⚠️ {error}");
                        onComplete?.Invoke(false, error);
                        return false;
                    }
                }
                else
                {
                    string error = "TakeSnapshot trả về false";
                    Debug.WriteLine($"❌ {error}");
                    onComplete?.Invoke(false, error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                string error = $"Exception khi chụp ảnh: {ex.Message}";
                Debug.WriteLine($"❌ {error}");
                Debug.WriteLine($"   Stack trace: {ex.StackTrace}");
                onComplete?.Invoke(false, error);
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra MediaPlayer có sẵn sàng chụp ảnh không
        /// </summary>
        public bool CanTakeSnapshot(MediaPlayer player)
        {
            if (player == null) return false;
            if (!player.IsPlaying) return false;

            // Kiểm tra thêm: có video track không
            try
            {
                return player.VideoTrack >= 0 || player.Media?.Tracks?.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            try
            {
                _libVLC?.Dispose();
                Debug.WriteLine("✅ CameraService đã dispose");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Lỗi khi dispose CameraService: {ex.Message}");
            }
        }
    }
}