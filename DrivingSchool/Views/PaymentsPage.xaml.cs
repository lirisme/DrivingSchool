using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class PaymentsPage : Page
    {
        private readonly SqlDataService _dataService;
        private StudentCollection _students;
        private Student _selectedStudent;
        private System.Collections.Generic.List<Payment> _payments;

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
                _students = _dataService.LoadStudents();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _students = new StudentCollection { Students = new System.Collections.Generic.List<Student>() };
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text?.ToLower() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                SearchResultsListBox.Visibility = Visibility.Collapsed;
                return;
            }

            var results = _students.Students
                .Where(s => (s.LastName ?? "").ToLower().Contains(searchText) ||
                           (s.FirstName ?? "").ToLower().Contains(searchText) ||
                           (s.Phone ?? "").Contains(searchText))
                .Take(10)
                .ToList();

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

        private void SearchResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchResultsListBox.SelectedItem is Student selectedStudent)
            {
                _selectedStudent = selectedStudent;
                UpdateSelectedStudentPanel();
                SearchResultsListBox.Visibility = Visibility.Collapsed;
                SearchTextBox.Text = string.Empty;
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

        private void LoadPaymentsForStudent()
        {
            try
            {
                _payments = _dataService.LoadStudentPayments(_selectedStudent.Id);
                PaymentsGrid.ItemsSource = _payments;
                UpdateTotalAmount();
                UpdateButtonsAvailability();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки платежей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _payments = new System.Collections.Generic.List<Payment>();
                PaymentsGrid.ItemsSource = null;
            }
        }

        private void UpdateTotalAmount()
        {
            if (_selectedStudent != null)
            {
                var total = _payments?.Sum(p => p.Amount) ?? 0;
                TotalAmountText.Text = $"Общая сумма платежей: {total:N2} руб.";

                if (_payments != null && _payments.Any())
                {
                    var lastPayment = _payments.OrderByDescending(p => p.PaymentDate).First();
                    InfoTextBlock.Text = $"Последний платеж: {lastPayment.PaymentDate:dd.MM.yyyy} на сумму {lastPayment.Amount:N2} руб.";
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
            var hasStudent = _selectedStudent != null;
            var hasSelection = PaymentsGrid.SelectedItem != null;

            AddPaymentButton.IsEnabled = hasStudent;
            EditPaymentButton.IsEnabled = hasStudent && hasSelection;
            DeletePaymentButton.IsEnabled = hasStudent && hasSelection;
            ViewPaymentButton.IsEnabled = hasStudent && hasSelection;
        }

        private void PaymentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsAvailability();
        }

        private void AddPayment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new PaymentEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    LoadPaymentsForStudent();
                    MessageBox.Show("Платеж успешно добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
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

            try
            {
                var dialog = new PaymentEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName, selectedPayment);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    LoadPaymentsForStudent();
                    MessageBox.Show("Платеж успешно обновлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
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

            if (MessageBox.Show($"Удалить платеж на сумму {selectedPayment.Amount:N2} руб. от {selectedPayment.PaymentDate:dd.MM.yyyy}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    _dataService.DeletePayment(selectedPayment.Id);
                    LoadPaymentsForStudent();
                    MessageBox.Show("Платеж удален.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
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

            MessageBox.Show(
                $"Информация о платеже:\n\n" +
                $"Студент: {_selectedStudent.FullName}\n" +
                $"Дата платежа: {selectedPayment.PaymentDate:dd.MM.yyyy HH:mm}\n" +
                $"Сумма: {selectedPayment.Amount:N2} руб.\n" +
                $"Тип платежа: {selectedPayment.PaymentType}\n" +
                $"Дата создания: {selectedPayment.CreatedDate:dd.MM.yyyy HH:mm}",
                "Просмотр платежа",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            SearchResultsListBox.Visibility = Visibility.Collapsed;
        }

        private void PaymentsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PaymentsGrid.SelectedItem != null && _selectedStudent != null)
            {
                EditPayment_Click(sender, e);
            }
        }

        private void ClearSelectedStudent_Click(object sender, RoutedEventArgs e)
        {
            _selectedStudent = null;
            SelectedStudentPanel.Visibility = Visibility.Collapsed;
            PaymentsGrid.ItemsSource = null;
            UpdateTotalAmount();
            UpdateButtonsAvailability();
        }
    }
}