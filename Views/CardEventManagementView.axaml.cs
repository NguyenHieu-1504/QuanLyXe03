using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using QuanLyXe03.ViewModels;
using System.Diagnostics;

namespace QuanLyXe03.Views
{
    public partial class CardEventManagementView : UserControl
    {
        public CardEventManagementView()
        {
            Debug.WriteLine("🏗️ CardEventManagementView Constructor START");

            //AvaloniaXamlLoader.Load(this);
            InitializeComponent();

            // CHỈ TẠO ViewModel 1 LẦN
            if (DataContext == null)
            {
                Debug.WriteLine("   → Creating new ViewModel");
                DataContext = new CardEventManagementViewModel();
            }

            Debug.WriteLine("🏗️ CardEventManagementView Constructor END");
        }
    }
}