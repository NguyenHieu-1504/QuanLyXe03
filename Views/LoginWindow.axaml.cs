using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using QuanLyXe03.Services;
using System;
using System.Threading.Tasks;

namespace QuanLyXe03.Views
{
    public partial class LoginWindow : Window
    {
        private readonly Animation _shakeAnimation;

        public LoginWindow()
        {
            InitializeComponent();

            // TẠO ANIMATION RUNG TRỰC TIẾP TRONG CONSTRUCTOR
            _shakeAnimation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(400),
                Easing = new QuadraticEaseOut(),
                Children =
                {
                    new KeyFrame { Cue = new Cue(0.0), Setters = { new Setter(TranslateTransform.XProperty, -15.0) } },
                    new KeyFrame { Cue = new Cue(0.2), Setters = { new Setter(TranslateTransform.XProperty,  15.0) } },
                    new KeyFrame { Cue = new Cue(0.4), Setters = { new Setter(TranslateTransform.XProperty, -10.0) } },
                    new KeyFrame { Cue = new Cue(0.6), Setters = { new Setter(TranslateTransform.XProperty,  10.0) } },
                    new KeyFrame { Cue = new Cue(1.0), Setters = { new Setter(TranslateTransform.XProperty,   0.0) } }
                }
            };

            // TỰ ĐỘNG FOCUS + ENTER = ĐĂNG NHẬP
            this.Opened += LoginWindow_Opened;

            // HIỆU ỨNG HOVER CHO NÚT
            AddHoverEffect(LoginButton);
            AddHoverEffect(ExitButton);

            // VIỀN XANH KHI FOCUS Ô MẬT KHẨU
            PasswordBox.GotFocus += (s, e) =>
                PasswordBorder.BorderBrush = Brushes.DeepSkyBlue;
            PasswordBox.LostFocus += (s, e) =>
                PasswordBorder.BorderBrush = Brushes.Transparent;
        }

        private void LoginWindow_Opened(object? sender, EventArgs e)
        {
            var box = PasswordBox;
            if (box != null)
            {
                box.Focus();
                box.KeyDown += (s, ke) =>
                {
                    if (ke.Key == Key.Enter)
                    {
                        Login_Click(null, null);
                    }
                };
            }
        }

        private void AddHoverEffect(Button btn)
        {
            btn.PointerEntered += (s, e) =>
            {
                btn.Opacity = 0.85;
                btn.RenderTransform = new ScaleTransform(1.05, 1.05);
            };
            btn.PointerExited += (s, e) =>
            {
                btn.Opacity = 1.0;
                btn.RenderTransform = new ScaleTransform(1.0, 1.0);
            };
        }

        private async void Login_Click(object? sender, RoutedEventArgs e)
        {
            var password = PasswordBox.Text;
            var settings = SettingsManager.Settings;

            if (string.IsNullOrWhiteSpace(password))
            {
                ErrorMessage.Text = "Vui lòng nhập mật khẩu!";
                await RunShakeAnimation(PasswordBorder);
                return;
            }

            if (!settings.RequireLogin || password == settings.LoginPassword)
            {
                ErrorMessage.Text = "";
                this.Tag = true;
                Close();
            }
            else
            {
                ErrorMessage.Text = "Mật khẩu không đúng!";
                await RunShakeAnimation(PasswordBorder);
            }
        }

        private void Exit_Click(object? sender, RoutedEventArgs e)
        {
            this.Tag = false;
            Close();
        }

        private async Task RunShakeAnimation(Control control)
        {
            if (control == null) return;

            var translate = new TranslateTransform();
            control.RenderTransform = translate;

            
            await _shakeAnimation.RunAsync(control);

            control.RenderTransform = null;
        }
    }
}