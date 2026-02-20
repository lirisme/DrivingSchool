using System;
using System.Windows;
using System.Windows.Threading;
using DrivingSchool.Services;
using DrivingSchool.Views;

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
                // TODO: Создать TemplatesPage
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
    }
}