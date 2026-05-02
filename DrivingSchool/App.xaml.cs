using System.Windows;
using DrivingSchool.Models;
using DrivingSchool.Views;

namespace DrivingSchool
{
    public partial class App : Application
    {
        public static User CurrentUser { get; set; }
        public static int CurrentUserId { get; set; }
        public static string CurrentUserRole { get; set; }
        public static string CurrentUserName { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}