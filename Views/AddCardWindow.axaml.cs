using Avalonia.Controls;
using QuanLyXe03.ViewModels;

namespace QuanLyXe03.Views
{
    public partial class AddCardWindow : Window
    {
        public AddCardWindow()
        {
            InitializeComponent();

            var vm = new AddCardViewModel();
            DataContext = vm;

            // Đóng window khi ViewModel yêu cầu
            vm.CloseRequested += (sender, success) =>
            {
                Close(success); // Trả về true nếu lưu thành công
            };
        }
    }
}