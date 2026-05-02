using DrivingSchool.Services;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DrivingSchool.Views
{
    public partial class CreateUserWindow : Window
    {
        private readonly AuthService _authService;

        public CreateUserWindow()
        {
            InitializeComponent();
            var cs = ConfigurationManager.ConnectionStrings["DrivingSchoolDB"].ConnectionString;
            _authService = new AuthService(cs);
        }

        private async void Create_Click(object sender, RoutedEventArgs e)
        {
            var fullName = FullNameBox.Text.Trim();
            var login = LoginBox.Text.Trim();
            var password = PasswordBox.Password;
            var roleItem = RoleBox.SelectedItem as ComboBoxItem;
            var role = roleItem?.Tag?.ToString() ?? "User";

            if (string.IsNullOrEmpty(fullName)) { MessageBox.Show("Введите ФИО"); return; }
            if (string.IsNullOrEmpty(login)) { MessageBox.Show("Введите логин"); return; }
            if (string.IsNullOrEmpty(password)) { MessageBox.Show("Введите пароль"); return; }
            if (password.Length < 4) { MessageBox.Show("Пароль не менее 4 символов"); return; }

            var result = await _authService.CreateUser(login, password, fullName, role);
            bool success = result.Success;
            string msg = result.Message;

            MessageBox.Show(msg, success ? "Успех" : "Ошибка");
            if (success) DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}