using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using QuanLyXe03.ViewModels;

namespace QuanLyXe03.Views
{
    public partial class CardEventManagementView : UserControl
    {
        public CardEventManagementView()
        {
            AvaloniaXamlLoader.Load(this);
            DataContext = new CardEventManagementViewModel();
        }
    }
}