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
    public partial class AddressDataPage : Page
    {
        private readonly SqlDataService _dataService;
        private StudentCollection _students;
        private StudentRegistrationAddressCollection _addresses;
        private Student _selectedStudent;

        public AddressDataPage(SqlDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                Debug.WriteLine("=== ЗАГРУЗКА АДРЕСОВ ===");

                _students = _dataService.LoadStudents() ?? new StudentCollection { Students = new List<Student>() };
                Debug.WriteLine($"Загружено студентов: {_students.Students.Count}");

                _addresses = _dataService.LoadAddresses() ?? new StudentRegistrationAddressCollection { Addresses = new List<StudentRegistrationAddress>() };
                Debug.WriteLine($"Загружено адресов: {_addresses.Addresses.Count}");

                ApplyFilter();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА загрузки данных: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _students = new StudentCollection { Students = new List<Student>() };
                _addresses = new StudentRegistrationAddressCollection { Addresses = new List<StudentRegistrationAddress>() };
            }
        }

        private void ApplyFilter()
        {
            try
            {
                if (_selectedStudent != null)
                {
                    var filtered = _addresses.Addresses
                        .Where(a => a.StudentId == _selectedStudent.Id)
                        .Select(a =>
                        {
                            a.StudentName = _selectedStudent.FullName;
                            return a;
                        })
                        .ToList();

                    AddressGrid.ItemsSource = filtered;

                    if (filtered.Any())
                    {
                        InfoTextBlock.Text = $"Адрес регистрации студента {_selectedStudent.FullName}";
                    }
                    else
                    {
                        InfoTextBlock.Text = $"У студента {_selectedStudent.FullName} нет адреса регистрации";
                    }
                }
                else
                {
                    var allAddresses = _addresses.Addresses
                        .Select(a =>
                        {
                            a.StudentName = GetStudentName(a.StudentId);
                            return a;
                        })
                        .ToList();

                    AddressGrid.ItemsSource = allAddresses;
                    InfoTextBlock.Text = $"Всего записей: {_addresses.Addresses.Count}. Выберите студента для добавления/редактирования";
                }

                UpdateButtonsAvailability();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА в ApplyFilter: {ex.Message}");
            }
        }

        private string GetStudentName(int studentId)
        {
            var student = _students?.Students?.FirstOrDefault(s => s.Id == studentId);
            return student?.FullName ?? "Неизвестный студент";
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var searchText = SearchTextBox.Text?.ToLower() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    SearchResultsListBox.Visibility = Visibility.Collapsed;
                    return;
                }

                var results = _students?.Students?
                    .Where(s => (s.LastName ?? "").ToLower().Contains(searchText) ||
                               (s.FirstName ?? "").ToLower().Contains(searchText) ||
                               (s.Phone ?? "").Contains(searchText))
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

        private void SearchResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchResultsListBox.SelectedItem is Student selectedStudent)
            {
                _selectedStudent = selectedStudent;
                UpdateSelectedStudentPanel();
                SearchResultsListBox.Visibility = Visibility.Collapsed;
                SearchTextBox.Text = string.Empty;
                ApplyFilter();
            }
        }

        private void UpdateSelectedStudentPanel()
        {
            if (_selectedStudent != null)
            {
                SelectedStudentPanel.Visibility = Visibility.Visible;
                SelectedStudentText.Text = _selectedStudent.FullName;
                SelectedStudentDetails.Text = $"Телефон: {_selectedStudent.Phone}";
            }
            else
            {
                SelectedStudentPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateButtonsAvailability()
        {
            try
            {
                var hasStudent = _selectedStudent != null;
                var hasAddress = hasStudent && _addresses.Addresses.Any(a => a.StudentId == _selectedStudent.Id);
                var hasSelection = AddressGrid.SelectedItem != null;

                AddAddressButton.IsEnabled = hasStudent && !hasAddress;
                EditAddressButton.IsEnabled = hasAddress && hasSelection;
                DeleteAddressButton.IsEnabled = hasAddress && hasSelection;
                ViewAddressButton.IsEnabled = hasAddress && hasSelection;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА в UpdateButtonsAvailability: {ex.Message}");
            }
        }

        private void AddressGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Если выбран адрес - находим студента
            if (AddressGrid.SelectedItem is StudentRegistrationAddress selectedAddress)
            {
                var student = _students?.Students?.FirstOrDefault(s => s.Id == selectedAddress.StudentId);
                if (student != null)
                {
                    _selectedStudent = student;
                    UpdateSelectedStudentPanel();
                    ApplyFilter();
                }
            }
            UpdateButtonsAvailability();
        }

        // Клик по пустому месту для сброса выбора
        private void AddressGrid_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var hit = System.Windows.Media.VisualTreeHelper.HitTest(AddressGrid, e.GetPosition(AddressGrid));
            var row = FindVisualParent<DataGridRow>(hit?.VisualHit as System.Windows.DependencyObject);

            if (row == null)
            {
                _selectedStudent = null;
                SelectedStudentPanel.Visibility = Visibility.Collapsed;
                AddressGrid.SelectedItem = null;
                ApplyFilter();
                e.Handled = true;
            }
        }

        private T FindVisualParent<T>(System.Windows.DependencyObject child) where T : System.Windows.DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;
                child = System.Windows.Media.VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        private void AddAddress_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStudent == null)
            {
                MessageBox.Show("Выберите студента", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dialog = new AddressEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    _addresses = _dataService.LoadAddresses() ?? new StudentRegistrationAddressCollection { Addresses = new List<StudentRegistrationAddress>() };
                    ApplyFilter();
                    MessageBox.Show("Адрес регистрации успешно добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditAddress_Click(object sender, RoutedEventArgs e)
        {
            if (!(AddressGrid.SelectedItem is StudentRegistrationAddress selectedAddress))
            {
                MessageBox.Show("Выберите запись для редактирования", "Предупреждение",
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
                var dialog = new AddressEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName, selectedAddress);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    _addresses = _dataService.LoadAddresses() ?? new StudentRegistrationAddressCollection { Addresses = new List<StudentRegistrationAddress>() };
                    ApplyFilter();
                    MessageBox.Show("Адрес регистрации успешно обновлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteAddress_Click(object sender, RoutedEventArgs e)
        {
            if (!(AddressGrid.SelectedItem is StudentRegistrationAddress selectedAddress))
            {
                MessageBox.Show("Выберите запись для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedStudent == null)
            {
                MessageBox.Show("Выберите студента", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Удалить адрес регистрации студента {_selectedStudent.FullName}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    _dataService.DeleteAddressData(selectedAddress.Id);
                    _addresses = _dataService.LoadAddresses() ?? new StudentRegistrationAddressCollection { Addresses = new List<StudentRegistrationAddress>() };
                    ApplyFilter();
                    MessageBox.Show("Адрес регистрации удален.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewAddress_Click(object sender, RoutedEventArgs e)
        {
            if (!(AddressGrid.SelectedItem is StudentRegistrationAddress selectedAddress))
            {
                MessageBox.Show("Выберите запись для просмотра", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedStudent == null)
            {
                MessageBox.Show("Выберите студента", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(
                $"Адрес регистрации:\n\n" +
                $"Студент: {_selectedStudent.FullName}\n" +
                $"Полный адрес: {selectedAddress.FullAddress}\n" +
                $"Регион: {selectedAddress.Region}\n" +
                $"Город: {selectedAddress.City}\n" +
                $"Улица: {selectedAddress.Street}\n" +
                $"Дом: {selectedAddress.House}\n" +
                $"Корпус: {selectedAddress.Building ?? "не указан"}\n" +
                $"Квартира: {selectedAddress.Apartment ?? "не указана"}",
                "Просмотр адреса регистрации",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            SearchResultsListBox.Visibility = Visibility.Collapsed;
        }

        private void ClearSelectedStudent_Click(object sender, RoutedEventArgs e)
        {
            _selectedStudent = null;
            SelectedStudentPanel.Visibility = Visibility.Collapsed;
            ApplyFilter();
        }

        private void AddressGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AddressGrid.SelectedItem is StudentRegistrationAddress selectedAddress)
            {
                if (_selectedStudent == null || _selectedStudent.Id != selectedAddress.StudentId)
                {
                    _selectedStudent = _students?.Students?.FirstOrDefault(s => s.Id == selectedAddress.StudentId);
                    if (_selectedStudent != null)
                    {
                        UpdateSelectedStudentPanel();
                        ApplyFilter();
                    }
                }

                if (_selectedStudent != null)
                {
                    EditAddress_Click(sender, e);
                }
            }
        }
    }
}