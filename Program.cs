using Avalonia;
using LibVLCSharp.Shared;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace QuanLyXe03
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {

            Console.WriteLine(" APP BẮT ĐẦU CHẠY!");
            Console.WriteLine("========================================");

            // ============== FIX LINUX  ==============
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Chỉ Windows mới cần chỉ đường dẫn VLC thủ công
                var vlcPath = Path.Combine(AppContext.BaseDirectory, "Libs");
                Core.Initialize(vlcPath);
            }
            else
            {
                // Linux / macOS → KHÔNG truyền đường dẫn → để hệ thống tự tìm trong /usr/lib
                Core.Initialize();
            }
            // =============================================
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
