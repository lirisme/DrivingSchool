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

                // ИСПРАВЛЕНИЕ: загружаем реальные данные!
                _addresses = _dataService.LoadAddresses() ?? new StudentRegistrationAddressCollection { Addresses = new List<StudentRegistrationAddress>() };
                Debug.WriteLine($"Загружено адресов: {_addresses.Addresses.Count}");

                // Отладка: выводим все загруженные адреса
                foreach (var address in _addresses.Addresses)
                {
                    Debug.WriteLine($"  Адрес ID={address.Id}, Студент ID={address.StudentId}, {address.City}, {address.Street}");
                }

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
                Debug.WriteLine("=== ПРИМЕНЕНИЕ ФИЛЬТРА ===");

                if (_selectedStudent != null)
                {
                    Debug.WriteLine($"Выбран студент ID={_selectedStudent.Id}, Name={_selectedStudent.FullName}");

                    var filtered = _addresses.Addresses
                        .Where(a => a.StudentId == _selectedStudent.Id)
                        .Select(a =>
                        {
                            a.StudentName = _selectedStudent.FullName;
                            return a;
                        })
                        .ToList();

                    Debug.WriteLine($"Найдено адресов для студента: {filtered.Count}");

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
                    Debug.WriteLine("Студент не выбран, показываем все адреса");

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
                SelectedStudentDetails.Text = $"Телефон: {_selectedStudent.Phone} | ID: {_selectedStudent.Id}";
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

                // Проверяем наличие адреса для выбранного студента
                var hasAddress = hasStudent && _addresses.Addresses.Any(a => a.StudentId == _selectedStudent.Id);
                var hasSelection = AddressGrid.SelectedItem != null;

                Debug.WriteLine($"UpdateButtonsAvailability: hasStudent={hasStudent}, hasAddress={hasAddress}, hasSelection={hasSelection}");

                // ИСПРАВЛЕНИЕ: как в паспортах - один адрес на студента!
                AddAddressButton.IsEnabled = hasStudent && !hasAddress;
                EditAddressButton.IsEnabled = hasAddress && hasSelection;
                DeleteAddressButton.IsEnabled = hasAddress && hasSelection;
                ViewAddressButton.IsEnabled = hasAddress && hasSelection;
                PrintAddressButton.IsEnabled = hasAddress && hasSelection;

                // Визуальная индикация
                AddAddressButton.Opacity = AddAddressButton.IsEnabled ? 1.0 : 0.5;
                EditAddressButton.Opacity = EditAddressButton.IsEnabled ? 1.0 : 0.5;
                DeleteAddressButton.Opacity = DeleteAddressButton.IsEnabled ? 1.0 : 0.5;
                ViewAddressButton.Opacity = ViewAddressButton.IsEnabled ? 1.0 : 0.5;
                PrintAddressButton.Opacity = PrintAddressButton.IsEnabled ? 1.0 : 0.5;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА в UpdateButtonsAvailability: {ex.Message}");
            }
        }

        private void AddressGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsAvailability();
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
                Debug.WriteLine($"Добавление адреса для студента ID={_selectedStudent.Id}");

                var dialog = new AddressEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    Debug.WriteLine("Диалог закрыт с OK, перезагружаем данные");

                    // ИСПРАВЛЕНИЕ: перезагружаем данные после сохранения
                    _addresses = _dataService.LoadAddresses() ?? new StudentRegistrationAddressCollection { Addresses = new List<StudentRegistrationAddress>() };

                    Debug.WriteLine($"После перезагрузки адресов: {_addresses.Addresses.Count}");

                    ApplyFilter();

                    MessageBox.Show("Адрес регистрации успешно добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    Debug.WriteLine("Диалог закрыт с Cancel");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА при добавлении: {ex.Message}");
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
                Debug.WriteLine($"Редактирование адреса ID={selectedAddress.Id}");

                var dialog = new AddressEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName, selectedAddress);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    Debug.WriteLine("Диалог закрыт с OK, перезагружаем данные");

                    // ИСПРАВЛЕНИЕ: перезагружаем данные после сохранения
                    _addresses = _dataService.LoadAddresses() ?? new StudentRegistrationAddressCollection { Addresses = new List<StudentRegistrationAddress>() };

                    ApplyFilter();

                    MessageBox.Show("Адрес регистрации успешно обновлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА при редактировании: {ex.Message}");
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
                    Debug.WriteLine($"Удаление адреса ID={selectedAddress.Id}");

                    _dataService.DeleteAddressData(selectedAddress.Id);

                    // ИСПРАВЛЕНИЕ: перезагружаем данные после удаления
                    _addresses = _dataService.LoadAddresses() ?? new StudentRegistrationAddressCollection { Addresses = new List<StudentRegistrationAddress>() };

                    ApplyFilter();

                    MessageBox.Show("Адрес регистрации удален.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ОШИБКА при удалении: {ex.Message}");
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
                $"Корпус: {selectedAddress.Building}\n" +
                $"Квартира: {selectedAddress.Apartment}\n" +
                $"Почтовый индекс: {selectedAddress.PostalCode}",
                "Просмотр адреса регистрации",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void PrintAddress_Click(object sender, RoutedEventArgs e)
        {
            if (!(AddressGrid.SelectedItem is StudentRegistrationAddress selectedAddress))
            {
                MessageBox.Show("Выберите запись для печати", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedStudent == null)
            {
                MessageBox.Show("Выберите студента", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"Печать адреса регистрации:\n\n{selectedAddress.FullAddress}", "Печать",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            SearchResultsListBox.Visibility = Visibility.Collapsed;
        }

        // НОВЫЙ МЕТОД: очистка выбранного студента
        private void ClearSelectedStudent_Click(object sender, RoutedEventArgs e)
        {
            _selectedStudent = null;
            SelectedStudentPanel.Visibility = Visibility.Collapsed;
            ApplyFilter();
        }

        private void AddressGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AddressGrid.SelectedItem != null && _selectedStudent != null)
            {
                EditAddress_Click(sender, e);
            }
        }
    }
}