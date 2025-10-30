using Avalonia.Controls;
using Avalonia.Interactivity;
using QuanLyXe03.ViewModels;
using System.Diagnostics;
using System.Reactive;

namespace QuanLyXe03.Views
{
    public partial class CardManagementView : UserControl
    {
        private CardManagementViewModel _vm;

        public CardManagementView()
        {
            InitializeComponent();

            _vm = new CardManagementViewModel();
            DataContext = _vm;

            Debug.WriteLine($"📋 CardManagementView Constructor: Cards.Count = {_vm.Cards.Count}");

            // ✅ Subscribe để theo dõi Cards thay đổi
            _vm.Cards.CollectionChanged += (s, e) =>
            {
                Debug.WriteLine($"🔔 Cards CollectionChanged: Action={e.Action}, Count={_vm.Cards.Count}");
            };
        }

        // ✅ THÊM: Xử lý click nút Thêm mới
        private async void AddCard_Click(object? sender, RoutedEventArgs e)
        {
            Debug.WriteLine("➕ Mở cửa sổ Thêm thẻ mới...");

            var addWindow = new AddCardWindow();

            // Lấy window cha để làm owner
            var parentWindow = this.VisualRoot as Window;

            // ShowDialog = modal window (phải đóng mới dùng window khác)
            var result = await addWindow.ShowDialog<bool>(parentWindow);

            Debug.WriteLine($"📋 Kết quả: {result}");

            // Nếu thêm thành công, refresh danh sách
            if (result)
            {
                Debug.WriteLine("✅ Thêm thẻ thành công, đang refresh danh sách...");
                _vm.RefreshCards(); // ✅ Gọi method refresh
            }
        }


    }
}