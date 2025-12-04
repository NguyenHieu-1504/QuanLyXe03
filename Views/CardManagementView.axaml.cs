using Avalonia.Controls;
//using Avalonia.Markup.Xaml;
using QuanLyXe03.ViewModels;
using System.Diagnostics;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using System;
using Avalonia.Layout;

namespace QuanLyXe03.Views
{
    public partial class CardManagementView : UserControl
    {
        private CardManagementViewModel _vm;

        public CardManagementView()
        {
            //AvaloniaXamlLoader.Load(this);
            InitializeComponent();

            _vm = new CardManagementViewModel();
            DataContext = _vm;

            Debug.WriteLine($"📋 CardManagementView Constructor: Cards.Count = {_vm.Cards.Count}");

            //  BỎ CollectionChanged handler - KHÔNG CẦN!
            // Event này có thể gây loop khi DataGrid render
            /*
            _vm.Cards.CollectionChanged += (s, e) =>
            {
                Debug.WriteLine($" Cards CollectionChanged: Action={e.Action}, Count={_vm.Cards.Count}");
            };
            */

            // Subscribe event để hiển thị confirmation dialog
            _vm.DeleteConfirmationRequested += OnDeleteConfirmationRequested;
        }

        private async void AddCard_Click(object? sender, RoutedEventArgs e)
        {
            Debug.WriteLine("➕ Mở cửa sổ Thêm thẻ mới...");

            var addWindow = new AddCardWindow();
            var parentWindow = this.VisualRoot as Window;
            var result = await addWindow.ShowDialog<bool>(parentWindow);

            if (result)
            {
                Debug.WriteLine("✅ Thêm thẻ thành công, đang refresh danh sách...");
                _vm.RefreshCards();
            }
        }

        // Hiển thị confirmation dialog
        private async void OnDeleteConfirmationRequested(object? sender, (int count, Action onConfirm) args)
        {
            var (count, onConfirm) = args;

            Debug.WriteLine($"💬 Hiển thị confirmation dialog: {count} thẻ");

            var dialog = new Window
            {
                Title = "Xác nhận xóa",
                Width = 400,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Background = Avalonia.Media.Brushes.LightGray
            };

            var panel = new StackPanel
            {
                Margin = new Avalonia.Thickness(30),
                Spacing = 25,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(new TextBlock
            {
                Text = $"Bạn có chắc muốn xóa {count} thẻ?",
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            });

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 15
            };

            var yesBtn = new Button
            {
                Content = "✅ Có",
                Width = 100,
                Height = 40,
                FontSize = 14,
                Background = Avalonia.Media.Brushes.Red,
                Foreground = Avalonia.Media.Brushes.White
            };
            yesBtn.Click += (s, ev) => dialog.Close(true);

            var noBtn = new Button
            {
                Content = "❌ Không",
                Width = 100,
                Height = 40,
                FontSize = 14,
                Background = Avalonia.Media.Brushes.Gray,
                Foreground = Avalonia.Media.Brushes.White
            };
            noBtn.Click += (s, ev) => dialog.Close(false);

            buttons.Children.Add(yesBtn);
            buttons.Children.Add(noBtn);
            panel.Children.Add(buttons);
            dialog.Content = panel;

            var parentWindow = this.VisualRoot as Window;
            var result = await dialog.ShowDialog<bool>(parentWindow);

            if (result)
            {
                Debug.WriteLine(" User xác nhận xóa");
                onConfirm?.Invoke();
            }
            else
            {
                Debug.WriteLine(" User hủy xóa");
            }
        }
    }
}