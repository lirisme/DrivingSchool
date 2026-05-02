using DrivingSchool.Services;
using DrivingSchool.Views;
using Microsoft.Win32;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Linq;

namespace DrivingSchool
{
    public partial class MainWindow : Window
    {
        private SqlDataService _dataService;
        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                _dataService = new SqlDataService();

                // Запускаем таймер для обновления времени
                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromSeconds(1);
                _timer.Tick += Timer_Tick;
                _timer.Start();

                // Проверяем подключение к БД
                if (_dataService.TestConnection())
                {
                    StatusText.Text = "Подключено к базе данных";

                    // Загружаем статистику
                    try
                    {
                        var stats = _dataService.GetDatabaseStatistics();
                        StatusText.Text = $"Подключено. Студентов: {stats.StudentsCount}, Автомобилей: {stats.CarsCount}";
                    }
                    catch
                    {
                        // Игнорируем ошибку статистики
                    }

                    // Открываем страницу студентов по умолчанию
                    ShowStudentsPage();
                }
                else
                {
                    StatusText.Text = "Ошибка подключения к БД";
                    MessageBox.Show("Не удалось подключиться к базе данных. Проверьте строку подключения в App.config",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ошибка инициализации";
                MessageBox.Show($"Ошибка при запуске: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            DateTimeText.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
        }

        #region Навигация

        private void ShowStudentsPage()
        {
            try
            {
                var page = new StudentsPage(_dataService);
                MainFrame.Navigate(page);
                StatusText.Text = "Учащиеся";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы учащихся: {ex.Message}", "Ошибка");
            }
        }

        private void ShowEmployeesPage()
        {
            try
            {
                var page = new EmployeesPage(_dataService);
                MainFrame.Navigate(page);
                StatusText.Text = "Сотрудники";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы сотрудников: {ex.Message}", "Ошибка");
            }
        }

        private void ShowCarsPage()
        {
            try
            {
                var page = new CarsPage(_dataService);
                MainFrame.Navigate(page);
                StatusText.Text = "Автомобили";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы автомобилей: {ex.Message}", "Ошибка");
            }
        }

        private void ShowGroupsPage()
        {
            try
            {
                var page = new GroupsPage(_dataService);
                MainFrame.Navigate(page);
                StatusText.Text = "Учебные группы";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы групп: {ex.Message}", "Ошибка");
            }
        }

        private void ShowCategoriesPage()
        {
            try
            {
                var page = new CategoriesPage(_dataService);
                MainFrame.Navigate(page);
                StatusText.Text = "Категории транспортных средств";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы категорий: {ex.Message}", "Ошибка");
            }
        }

        private void ShowTemplatesPage()
        {
            try
            {
                MessageBox.Show("Страница шаблонов в разработке", "Информация");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        private void ShowPaymentsPage()
        {
            try
            {
                var page = new PaymentsPage(_dataService);
                MainFrame.Navigate(page);
                StatusText.Text = "Платежи";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы платежей: {ex.Message}", "Ошибка");
            }
        }

        private void ShowPassportDataPage()
        {
            try
            {
                var page = new PassportDataPage(_dataService);
                MainFrame.Navigate(page);
                StatusText.Text = "Паспортные данные";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы паспортных данных: {ex.Message}", "Ошибка");
            }
        }

        private void ShowSNILSPage()
        {
            try
            {
                var page = new SNILSPage(_dataService);
                MainFrame.Navigate(page);
                StatusText.Text = "Данные СНИЛС";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы СНИЛС: {ex.Message}", "Ошибка");
            }
        }

        private void ShowAddressDataPage()
        {
            try
            {
                var page = new AddressDataPage(_dataService);
                MainFrame.Navigate(page);
                StatusText.Text = "Адреса регистрации";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы адресов регистрации: {ex.Message}", "Ошибка");
            }
        }

        private void ShowMedicalPage()
        {
            try
            {
                var page = new MedicalPage(_dataService);
                MainFrame.Navigate(page);
                StatusText.Text = "Медицинские справки";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы медицинских справок: {ex.Message}", "Ошибка");
            }
        }

        private void ShowCertificateDataPage()
        {
            try
            {
                var page = new CertificateDataPage(_dataService);
                MainFrame.Navigate(page);
                StatusText.Text = "Свидетельства об окончании";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы свидетельств: {ex.Message}", "Ошибка");
            }
        }

        private void ShowDrivingLicenseDataPage()
        {
            try
            {
                var page = new DrivingLicenseDataPage(_dataService);
                MainFrame.Navigate(page);
                StatusText.Text = "Водительские удостоверения";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы водительских удостоверений: {ex.Message}", "Ошибка");
            }
        }

        private void ShowFinancialReportsPage()
        {
            try
            {
                var page = new FinancialReportsPage(_dataService);
                MainFrame.Navigate(page);
                StatusText.Text = "Финансовые отчеты";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы финансовых отчетов: {ex.Message}", "Ошибка");
            }
        }

        private void ShowStudentReportsPage()
        {
            try
            {
                var page = new StudentReportsPage(_dataService);
                MainFrame.Navigate(page);
                StatusText.Text = "Отчеты по учащимся";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы отчетов по учащимся: {ex.Message}", "Ошибка");
            }
        }

        #endregion

        #region Обработчики меню

        private void Students_Click(object sender, RoutedEventArgs e)
        {
            ShowStudentsPage();
        }

        private void Employees_Click(object sender, RoutedEventArgs e)
        {
            ShowEmployeesPage();
        }

        private void Cars_Click(object sender, RoutedEventArgs e)
        {
            ShowCarsPage();
        }

        private void Groups_Click(object sender, RoutedEventArgs e)
        {
            ShowGroupsPage();
        }

        private void Categories_Click(object sender, RoutedEventArgs e)
        {
            ShowCategoriesPage();
        }

        private void Templates_Click(object sender, RoutedEventArgs e)
        {
            ShowTemplatesPage();
        }

        private void Payments_Click(object sender, RoutedEventArgs e)
        {
            ShowPaymentsPage();
        }

        private void PassportData_Click(object sender, RoutedEventArgs e)
        {
            ShowPassportDataPage();
        }

        private void SNILSData_Click(object sender, RoutedEventArgs e)
        {
            ShowSNILSPage();
        }

        private void AddressData_Click(object sender, RoutedEventArgs e)
        {
            ShowAddressDataPage();
        }

        private void MedicalData_Click(object sender, RoutedEventArgs e)
        {
            ShowMedicalPage();
        }

        private void CertificateData_Click(object sender, RoutedEventArgs e)
        {
            ShowCertificateDataPage();
        }

        private void DrivingLicenseData_Click(object sender, RoutedEventArgs e)
        {
            ShowDrivingLicenseDataPage();
        }

        private void FinancialReports_Click(object sender, RoutedEventArgs e)
        {
            ShowFinancialReportsPage();
        }

        private void StudentReports_Click(object sender, RoutedEventArgs e)
        {
            ShowStudentReportsPage();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Вы уверены, что хотите выйти?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        #endregion

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Автошкола 'Элита Авто'\n" +
                "Версия 1.0\n\n" +
                "Автоматизированная система управления учебным центром",
                "О программе",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void Tariffs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var page = new TariffsPage(_dataService);
                MainFrame.Navigate(page);
                StatusText.Text = "Тарифы обучения";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы тарифов: {ex.Message}", "Ошибка");
            }
        }

        private void DrivingLessons_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var page = new DrivingLessonsPage(_dataService);
                MainFrame.Navigate(page);
                StatusText.Text = "Бронирование уроков вождения";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы: {ex.Message}", "Ошибка");
            }
        }

        private void OpenExamManagement_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var page = new ExamManagementPage(_dataService);
                MainFrame.Navigate(page);
                StatusText.Text = "Экзамены";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы: {ex.Message}", "Ошибка");
            }
        }

        private void CreateBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings["DrivingSchoolDB"].ConnectionString;
                var backupService = new BackupService(connectionString);
                var backupPath = backupService.CreateBackup();

                MessageBox.Show($"Резервная копия создана!\n\n{backupPath}\n\nПапка с бэкапами: {backupService.GetBackupFolder()}",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenBackupFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings["DrivingSchoolDB"].ConnectionString;
                var backupService = new BackupService(connectionString);
                Process.Start("explorer.exe", backupService.GetBackupFolder());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings["DrivingSchoolDB"].ConnectionString;
                var backupService = new BackupService(connectionString);
                var backups = backupService.GetBackupFiles();

                if (!backups.Any())
                {
                    MessageBox.Show("Нет резервных копий для восстановления", "Информация");
                    return;
                }

                var dialog = new OpenFileDialog
                {
                    Title = "Выберите файл резервной копии",
                    Filter = "Backup files (*.bak)|*.bak",
                    InitialDirectory = backupService.GetBackupFolder()
                };

                if (dialog.ShowDialog() == true)
                {
                    var result = MessageBox.Show(
                        "ВНИМАНИЕ! Восстановление заменит текущую базу данных.\n" +
                        "Все несохраненные изменения будут потеряны.\n\n" +
                        "Продолжить?",
                        "Подтверждение",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        backupService.RestoreBackup(dialog.FileName);
                        MessageBox.Show("База данных восстановлена!\nПерезапустите приложение.", "Успех");
                        Process.Start(Application.ResourceAssembly.Location);
                        Application.Current.Shutdown();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка восстановления: {ex.Message}", "Ошибка");
            }
        }

        private void CreateUser_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUserRole != "Admin")
            {
                MessageBox.Show("Доступ запрещен", "Ошибка");
                return;
            }

            var dialog = new CreateUserWindow();
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        // ========== ГЛАВНЫЙ МЕТОД - ВЫЗЫВАЕТСЯ ПРИ ЗАГРУЗКЕ ОКНА ==========
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Вызываем метод разграничения прав
            SetAccessRights();

            // Обновляем статусную строку
            UserInfo.Text = $"Пользователь: {App.CurrentUserName} ({App.CurrentUserRole})";

            string roleName = App.CurrentUserRole == "Admin" ? "Администратор" :
                              App.CurrentUserRole == "Accountant" ? "Бухгалтер" :
                              App.CurrentUserRole == "Registrar" ? "Регистратор" : "Пользователь";

            StatusText.Text = $"Роль: {roleName}";
        }

        // ========== РАЗГРАНИЧЕНИЕ ПРАВ ДОСТУПА ==========
        private void SetAccessRights()
        {
            bool isAdmin = App.CurrentUserRole == "Admin";
            bool isAccountant = App.CurrentUserRole == "Accountant";
            bool isRegistrar = App.CurrentUserRole == "Registrar";

            // Администрирование - только админ
            AdminMenu.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

            // Финансы - админ и бухгалтер
            FinanceMenu.Visibility = (isAdmin || isAccountant) ? Visibility.Visible : Visibility.Collapsed;

            // Сервис (бэкапы) - только админ
            ServiceMenu.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

            // Справочники - админ и регистратор
            ReferencesMenu.Visibility = (isAdmin || isRegistrar) ? Visibility.Visible : Visibility.Collapsed;

            // Документы - все три роли
            DocumentsMenu.Visibility = Visibility.Visible;

            // Вождение - админ
            DrivingMenu.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

            // Экзамены - админ
            ExamMenu.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

            // Отчеты - админ и бухгалтер
            ReportsMenu.Visibility = (isAdmin || isAccountant) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}