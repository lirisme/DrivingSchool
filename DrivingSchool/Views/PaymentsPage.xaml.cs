using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Models;
using DrivingSchool.Services;
using System.Collections.Generic;
using System.Diagnostics;

namespace DrivingSchool.Views
{
    public partial class PaymentsPage : Page
    {
        private readonly SqlDataService _dataService;
        private StudentCollection _students;
        private Student _selectedStudent;
        private List<Payment> _payments;
        private decimal _tuitionAmount;
        private decimal _discountAmount;
        private decimal _finalAmount;
        private decimal _paidAmount;

        public PaymentsPage(SqlDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                Debug.WriteLine("=== ЗАГРУЗКА ДАННЫХ ПЛАТЕЖЕЙ ===");
                _students = _dataService.LoadStudents() ?? new StudentCollection { Students = new List<Student>() };
                Debug.WriteLine($"Загружено студентов: {_students.Students.Count}");

                // Если есть выбранный студент из предыдущего сеанса, обновляем его данные
                if (_selectedStudent != null)
                {
                    _selectedStudent = _dataService.LoadStudent(_selectedStudent.Id);
                    UpdateSelectedStudentPanel();
                    LoadStudentTuitionInfo();
                    LoadPaymentsForStudent();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА загрузки данных: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _students = new StudentCollection { Students = new List<Student>() };
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var searchText = SearchTextBox.Text?.Trim().ToLower() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    SearchResultsListBox.Visibility = Visibility.Collapsed;
                    return;
                }

                var results = _students?.Students?
                    .Where(s =>
                        (s.LastName ?? "").ToLower().Contains(searchText) ||
                        (s.FirstName ?? "").ToLower().Contains(searchText) ||
                        NormalizePhone(s.Phone ?? "").Contains(searchText) ||
                        (s.Email ?? "").ToLower().Contains(searchText))
                    .Take(10)
                    .ToList() ?? new List<Student>();

                if (results.Any())
                {
                    SearchResultsListBox.ItemsSource = results;
                    SearchResultsListBox.Visibility = Visibility.Visible;
                }
                else
                {
                    SearchResultsListBox.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка поиска: {ex.Message}");
            }
        }

        private string NormalizePhone(string phone)
        {
            return new string(phone.Where(c => char.IsDigit(c)).ToArray());
        }

        private void SearchResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchResultsListBox.SelectedItem is Student selectedStudent)
            {
                // ВАЖНО: Полностью перезагружаем данные студента из БД
                _selectedStudent = _dataService.LoadStudent(selectedStudent.Id);

                // ОБЯЗАТЕЛЬНО: Загружаем информацию о стоимости обучения СРАЗУ после выбора студента
                LoadStudentTuitionInfo();

                UpdateSelectedStudentPanel();
                SearchResultsListBox.Visibility = Visibility.Collapsed;
                SearchTextBox.Text = string.Empty;

                // Загружаем платежи для студента
                LoadPaymentsForStudent();
            }
        }

        private void UpdateSelectedStudentPanel()
        {
            if (_selectedStudent != null)
            {
                SelectedStudentPanel.Visibility = Visibility.Visible;
                SelectedStudentText.Text = _selectedStudent.FullName;
                SelectedStudentDetails.Text = $"Телефон: {_selectedStudent.Phone} | ID: {_selectedStudent.Id}";
            }
            else
            {
                SelectedStudentPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadStudentTuitionInfo()
        {
            try
            {
                Debug.WriteLine($"Загрузка информации о стоимости обучения для студента ID={_selectedStudent.Id}");

                // Получаем ВСЮ информацию одним запросом
                var paymentInfo = _dataService.GetStudentPaymentInfo(_selectedStudent.Id);

                _tuitionAmount = paymentInfo.tuition;
                _discountAmount = paymentInfo.discount;
                _paidAmount = paymentInfo.paid;
                _finalAmount = paymentInfo.final;

                Debug.WriteLine($"Загружено: сумма={_tuitionAmount}, скидка={_discountAmount}, " +
                               $"оплачено={_paidAmount}, итого={_finalAmount}");

                // Если стоимость не загрузилась (0), пробуем получить напрямую из студента
                if (_tuitionAmount == 0 && _discountAmount == 0 && _selectedStudent != null)
                {
                    Debug.WriteLine("Пробуем получить стоимость напрямую из объекта студента");
                    // Прямое присваивание без оператора ?? так как поля не nullable
                    _tuitionAmount = _selectedStudent.TuitionAmount;
                    _discountAmount = _selectedStudent.DiscountAmount;
                    _finalAmount = _tuitionAmount - _discountAmount;
                    Debug.WriteLine($"Из студента: сумма={_tuitionAmount}, скидка={_discountAmount}");
                }

                UpdateTuitionInfo();
                UpdateProgressBar();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА загрузки стоимости обучения: {ex.Message}");

                // Пробуем получить из объекта студента как запасной вариант
                if (_selectedStudent != null)
                {
                    // Прямое присваивание без оператора ?? 
                    _tuitionAmount = _selectedStudent.TuitionAmount;
                    _discountAmount = _selectedStudent.DiscountAmount;
                    _finalAmount = _tuitionAmount - _discountAmount;
                    UpdateTuitionInfo();
                    UpdateProgressBar();
                }
                else
                {
                    _tuitionAmount = 0;
                    _discountAmount = 0;
                    _finalAmount = 0;
                    _paidAmount = 0;
                }
            }
        }

        public void RefreshSelectedStudentData()
        {
            if (_selectedStudent != null)
            {
                Debug.WriteLine($"Обновление данных студента ID={_selectedStudent.Id}");
                _selectedStudent = _dataService.LoadStudent(_selectedStudent.Id);
                LoadStudentTuitionInfo();
                LoadPaymentsForStudent();
            }
        }

        private void UpdateTuitionInfo()
        {
            TuitionAmountText.Text = $"{_tuitionAmount:N2} руб.";
            DiscountAmountText.Text = $"{_discountAmount:N2} руб.";
            FinalAmountText.Text = $"{_finalAmount:N2} руб.";
            PaidAmountText.Text = $"Оплачено: {_paidAmount:N2} руб.";

            var remaining = _finalAmount - _paidAmount;
            RemainingAmountText.Text = $"Остаток: {(remaining > 0 ? remaining : 0):N2} руб.";

            if (remaining < 0)
            {
                OverpaymentText.Text = $"Переплата: {Math.Abs(remaining):N2} руб.";
                OverpaymentText.Visibility = Visibility.Visible;
            }
            else
            {
                OverpaymentText.Visibility = Visibility.Collapsed;
            }

            // Обновляем статус
            if (_finalAmount == 0)
            {
                StatusText.Text = "Статус: Не указана стоимость обучения";
                StatusText.Foreground = System.Windows.Media.Brushes.Gray;
            }
            else if (remaining <= 0)
            {
                StatusText.Text = "Статус: Оплачено полностью";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else if (_paidAmount == 0)
            {
                StatusText.Text = "Статус: Не оплачено";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
            else
            {
                StatusText.Text = "Статус: Частично оплачено";
                StatusText.Foreground = System.Windows.Media.Brushes.Orange;
            }
        }

        private void UpdateProgressBar()
        {
            if (_finalAmount > 0)
            {
                int progress = (int)((_paidAmount / _finalAmount) * 100);
                progress = Math.Min(100, Math.Max(0, progress));

                PaymentProgressBar.Value = progress;
                ProgressText.Text = $"{progress}%";

                // Меняем цвет в зависимости от прогресса
                if (progress >= 100)
                {
                    PaymentProgressBar.Foreground = System.Windows.Media.Brushes.Green;
                }
                else if (progress >= 50)
                {
                    PaymentProgressBar.Foreground = System.Windows.Media.Brushes.Orange;
                }
                else
                {
                    PaymentProgressBar.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            else
            {
                PaymentProgressBar.Value = 0;
                ProgressText.Text = "0%";
            }
        }

        private void LoadPaymentsForStudent()
        {
            try
            {
                Debug.WriteLine($"Загрузка платежей для студента ID={_selectedStudent.Id}");
                _payments = _dataService.LoadStudentPayments(_selectedStudent.Id) ?? new List<Payment>();
                Debug.WriteLine($"Загружено платежей: {_payments.Count}");

                PaymentsGrid.ItemsSource = _payments.OrderByDescending(p => p.PaymentDate).ToList();

                // Обновляем общую сумму оплаты
                _paidAmount = _payments?.Where(p => p.Amount > 0).Sum(p => p.Amount) ?? 0;
                UpdateTuitionInfo();
                UpdateProgressBar();
                UpdateTotalAmount();
                UpdateButtonsAvailability();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА загрузки платежей: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки платежей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _payments = new List<Payment>();
                PaymentsGrid.ItemsSource = null;
            }
        }

        private void UpdateTotalAmount()
        {
            if (_selectedStudent != null)
            {
                var positivePayments = _payments?.Where(p => p.Amount > 0).Sum(p => p.Amount) ?? 0;
                var negativePayments = _payments?.Where(p => p.Amount < 0).Sum(p => p.Amount) ?? 0;
                var total = positivePayments + negativePayments;

                TotalAmountText.Text = $"Общая сумма платежей: {total:N2} руб.";

                if (negativePayments != 0)
                {
                    TotalAmountText.Text += $" (включая возвраты: {negativePayments:N2} руб.)";
                }

                if (_payments != null && _payments.Any())
                {
                    var lastPayment = _payments
                        .OrderByDescending(p => p.PaymentDate)
                        .ThenByDescending(p => p.CreatedDate)
                        .First();

                    var paymentType = lastPayment.Amount > 0 ? "платеж" : "возврат";
                    InfoTextBlock.Text = $"Последний {paymentType}: {lastPayment.PaymentDate:dd.MM.yyyy HH:mm} " +
                                        $"на сумму {lastPayment.Amount:N2} руб.";
                }
                else
                {
                    InfoTextBlock.Text = $"У студента {_selectedStudent.FullName} нет платежей";
                }
            }
            else
            {
                TotalAmountText.Text = "Общая сумма: 0 руб.";
                InfoTextBlock.Text = "Выберите студента для просмотра платежей";
            }
        }

        private void UpdateButtonsAvailability()
        {
            try
            {
                var hasStudent = _selectedStudent != null;
                var hasSelection = PaymentsGrid.SelectedItem != null;

                Debug.WriteLine($"UpdateButtonsAvailability: hasStudent={hasStudent}, hasSelection={hasSelection}");

                AddPaymentButton.IsEnabled = hasStudent;
                EditPaymentButton.IsEnabled = hasStudent && hasSelection;
                DeletePaymentButton.IsEnabled = hasStudent && hasSelection;
                ViewPaymentButton.IsEnabled = hasStudent && hasSelection;
                EditTuitionButton.IsEnabled = hasStudent;

                // Визуальная индикация
                AddPaymentButton.Opacity = AddPaymentButton.IsEnabled ? 1.0 : 0.5;
                EditPaymentButton.Opacity = EditPaymentButton.IsEnabled ? 1.0 : 0.5;
                DeletePaymentButton.Opacity = DeletePaymentButton.IsEnabled ? 1.0 : 0.5;
                ViewPaymentButton.Opacity = ViewPaymentButton.IsEnabled ? 1.0 : 0.5;
                EditTuitionButton.Opacity = EditTuitionButton.IsEnabled ? 1.0 : 0.5;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА в UpdateButtonsAvailability: {ex.Message}");
            }
        }

        private void PaymentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsAvailability();
        }

        private void AddPayment_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStudent == null)
            {
                MessageBox.Show("Выберите студента", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Debug.WriteLine($"Добавление платежа для студента ID={_selectedStudent.Id}");

                var dialog = new PaymentEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    Debug.WriteLine("Диалог закрыт с OK, перезагружаем данные");
                    LoadPaymentsForStudent();
                    MessageBox.Show("Платеж успешно добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА при добавлении платежа: {ex.Message}");
                MessageBox.Show($"Ошибка при добавлении платежа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditPayment_Click(object sender, RoutedEventArgs e)
        {
            if (!(PaymentsGrid.SelectedItem is Payment selectedPayment))
            {
                MessageBox.Show("Выберите платеж для редактирования", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedStudent == null)
            {
                MessageBox.Show("Выберите студента", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Debug.WriteLine($"Редактирование платежа ID={selectedPayment.Id}");

                var dialog = new PaymentEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName, selectedPayment);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    Debug.WriteLine("Диалог закрыт с OK, перезагружаем данные");
                    LoadPaymentsForStudent();
                    MessageBox.Show("Платеж успешно обновлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА при редактировании платежа: {ex.Message}");
                MessageBox.Show($"Ошибка при редактировании платежа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeletePayment_Click(object sender, RoutedEventArgs e)
        {
            if (!(PaymentsGrid.SelectedItem is Payment selectedPayment))
            {
                MessageBox.Show("Выберите платеж для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedStudent == null)
            {
                MessageBox.Show("Выберите студента", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var paymentType = selectedPayment.Amount > 0 ? "платеж" : "возврат";
            if (MessageBox.Show($"Удалить {paymentType} на сумму {Math.Abs(selectedPayment.Amount):N2} руб. от {selectedPayment.PaymentDate:dd.MM.yyyy}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    Debug.WriteLine($"Удаление платежа ID={selectedPayment.Id}");
                    _dataService.DeletePayment(selectedPayment.Id);
                    LoadPaymentsForStudent();
                    MessageBox.Show("Платеж удален.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ОШИБКА при удалении платежа: {ex.Message}");
                    MessageBox.Show($"Ошибка при удалении платежа: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewPayment_Click(object sender, RoutedEventArgs e)
        {
            if (!(PaymentsGrid.SelectedItem is Payment selectedPayment))
            {
                MessageBox.Show("Выберите платеж для просмотра", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedStudent == null)
            {
                MessageBox.Show("Выберите студента", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var paymentType = selectedPayment.Amount > 0 ? "Платеж" : "Возврат";
            MessageBox.Show(
                $"Информация о {paymentType.ToLower()}:\n\n" +
                $"Студент: {_selectedStudent.FullName}\n" +
                $"Дата: {selectedPayment.PaymentDate:dd.MM.yyyy HH:mm}\n" +
                $"Сумма: {Math.Abs(selectedPayment.Amount):N2} руб.\n" +
                $"Тип: {selectedPayment.PaymentType ?? "Не указан"}\n" +
                $"Дата создания: {selectedPayment.CreatedDate:dd.MM.yyyy HH:mm}",
                $"Просмотр {paymentType}а",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void EditTuition_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStudent == null)
            {
                MessageBox.Show("Выберите студента", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Debug.WriteLine($"Редактирование стоимости обучения для студента ID={_selectedStudent.Id}");

                var dialog = new TuitionEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName,
                    _tuitionAmount, _discountAmount);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    Debug.WriteLine("Диалог закрыт с OK, перезагружаем данные");
                    LoadStudentTuitionInfo();
                    LoadPaymentsForStudent();
                    MessageBox.Show("Стоимость обучения обновлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА при редактировании стоимости: {ex.Message}");
                MessageBox.Show($"Ошибка при редактировании стоимости: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            SearchResultsListBox.Visibility = Visibility.Collapsed;
        }

        private void ClearSelectedStudent_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Сброс выбранного студента");
            _selectedStudent = null;
            SelectedStudentPanel.Visibility = Visibility.Collapsed;
            PaymentsGrid.ItemsSource = null;
            _payments = null;
            _tuitionAmount = 0;
            _discountAmount = 0;
            _finalAmount = 0;
            _paidAmount = 0;

            UpdateTuitionInfo();
            UpdateProgressBar();
            UpdateTotalAmount();
            UpdateButtonsAvailability();
        }

        private void PaymentsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PaymentsGrid.SelectedItem != null && _selectedStudent != null)
            {
                EditPayment_Click(sender, e);
            }
        }

        public void SelectStudent(int studentId)
        {
            try
            {
                _selectedStudent = _dataService.LoadStudent(studentId);
                if (_selectedStudent != null)
                {
                    UpdateSelectedStudentPanel();
                    LoadStudentTuitionInfo();
                    LoadPaymentsForStudent();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка выбора студента: {ex.Message}");
            }
        }

    }
}