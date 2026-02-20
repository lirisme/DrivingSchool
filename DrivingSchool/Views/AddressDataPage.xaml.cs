using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Models;
using DrivingSchool.Services;

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
                _students = _dataService.LoadStudents();
                // TODO: LoadAddresses нужно добавить в SqlDataService
                _addresses = new StudentRegistrationAddressCollection { Addresses = new System.Collections.Generic.List<StudentRegistrationAddress>() };

                // Пока заглушка
                // _addresses = _dataService.LoadAddresses();

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _students = new StudentCollection { Students = new System.Collections.Generic.List<Student>() };
                _addresses = new StudentRegistrationAddressCollection { Addresses = new System.Collections.Generic.List<StudentRegistrationAddress>() };
            }
        }

        private void ApplyFilter()
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

        private string GetStudentName(int studentId)
        {
            var student = _students.Students.FirstOrDefault(s => s.Id == studentId);
            return student?.FullName ?? "Неизвестный студент";
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
            var hasStudent = _selectedStudent != null;
            var hasAddress = hasStudent && _addresses.Addresses.Any(a => a.StudentId == _selectedStudent.Id);
            var hasSelection = AddressGrid.SelectedItem != null;

            AddAddressButton.IsEnabled = hasStudent && !hasAddress;
            EditAddressButton.IsEnabled = hasAddress && hasSelection;
            DeleteAddressButton.IsEnabled = hasAddress && hasSelection;
            ViewAddressButton.IsEnabled = hasAddress && hasSelection;
            PrintAddressButton.IsEnabled = hasAddress && hasSelection;
        }

        private void AddressGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsAvailability();
        }

        private void AddAddress_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new AddressEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    // TODO: Обновить данные
                    // LoadData();
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

            try
            {
                var dialog = new AddressEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName, selectedAddress);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    // TODO: Обновить данные
                    // LoadData();
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

            if (MessageBox.Show($"Удалить адрес регистрации студента {_selectedStudent.FullName}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    // TODO: Удалить через сервис
                    // _dataService.DeleteAddressData(selectedAddress.Id);

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

            // TODO: Реализовать печать
            MessageBox.Show($"Печать адреса регистрации:\n\n{selectedAddress.FullAddress}", "Печать",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            SearchResultsListBox.Visibility = Visibility.Collapsed;
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