using System;
using System.Configuration;
using System.Windows;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _auth;

        public LoginWindow()
        {
            InitializeComponent();
            var cs = ConfigurationManager.ConnectionStrings["DrivingSchoolDB"].ConnectionString;
            _auth = new AuthService(cs);
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginBox.Text.Trim();
            string password = PasswordBox.Password;

            // ОТЛАДКА
            System.Diagnostics.Debug.WriteLine($"Login attempt: {login}");

            var user = await _auth.Authenticate(login, password);

            if (user != null)
            {
                // Успех
                App.CurrentUserId = user.Id;
                App.CurrentUserRole = user.Role;
                App.CurrentUserName = user.FullName;

                var mainWindow = new MainWindow();
                mainWindow.Show();
                Close();
            }
            else
            {
                ErrorText.Text = "Неверный логин или пароль";
                System.Diagnostics.Debug.WriteLine($"Auth failed for: {login}");
            }
        }
    }
}