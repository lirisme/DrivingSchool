using DrivingSchool.Models;
using DrivingSchool.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DrivingSchool.Views
{
    public partial class GroupEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        public StudyGroup GroupData { get; private set; }
        private bool _isDirty = false;
        private VehicleCategoryCollection _categories;
        private int _originalStudentCount = 0;

        public GroupEditDialog(SqlDataService dataService, StudyGroup groupData = null, int studentCount = 0)
        {
            InitializeComponent();
            _dataService = dataService;
            _originalStudentCount = studentCount;

            if (groupData != null)
            {
                GroupData = groupData;
                Title = "Редактирование группы";
            }
            else
            {
                GroupData = new StudyGroup
                {
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddMonths(3),
                    Status = "Активна"
                };
                Title = "Добавление группы";
            }

            DataContext = GroupData;
            LoadCategories();
            StartDatePicker.SelectedDateChanged += DateChanged;
            EndDatePicker.SelectedDateChanged += DateChanged;
            UpdateDuration();
            SubscribeToChanges();

            // Если в группе есть студенты - блокируем выбор категории
            if (_originalStudentCount > 0)
            {
                CategoryComboBox.IsEnabled = false;
                WarningTextBlock.Visibility = Visibility.Visible;
                InfoTextBlock.Text = $"В группе {_originalStudentCount} студентов. Категорию изменить нельзя.";
            }
        }

        private void LoadCategories()
        {
            try
            {
                _categories = _dataService.LoadVehicleCategories();
                CategoryComboBox.ItemsSource = _categories.Categories;
                CategoryComboBox.DisplayMemberPath = "DisplayText";
                CategoryComboBox.SelectedValuePath = "Code";

                if (!string.IsNullOrEmpty(GroupData.Category))
                {
                    CategoryComboBox.SelectedValue = GroupData.Category;
                }
                else if (_categories.Categories.Any())
                {
                    CategoryComboBox.SelectedIndex = 0;
                    GroupData.Category = _categories.Categories[0].Code;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки категорий: {ex.Message}");
                CategoryComboBox.ItemsSource = new[] { new { Code = "B", DisplayText = "B - Легковые автомобили" } };
            }
        }

        private void SubscribeToChanges()
        {
            NameTextBox.TextChanged += (s, e) => _isDirty = true;
            CategoryComboBox.SelectionChanged += (s, e) => _isDirty = true;
            StatusComboBox.SelectionChanged += (s, e) => _isDirty = true;
            StartDatePicker.SelectedDateChanged += (s, e) => _isDirty = true;
            EndDatePicker.SelectedDateChanged += (s, e) => _isDirty = true;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_isDirty)
            {
                var result = MessageBox.Show(
                    "Есть несохраненные изменения. Сохранить перед закрытием?",
                    "Подтверждение закрытия",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (SaveGroup())
                    {
                        DialogResult = true;
                        base.OnClosing(e);
                    }
                    else
                    {
                        e.Cancel = true;
                    }
                }
                else if (result == MessageBoxResult.No)
                {
                    DialogResult = false;
                    base.OnClosing(e);
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else
            {
                base.OnClosing(e);
            }
        }

        private void DateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDuration();
        }

        private void UpdateDuration()
        {
            if (GroupData.StartDate != default && GroupData.EndDate != default)
            {
                var days = (GroupData.EndDate - GroupData.StartDate).Days;
                var months = days / 30;
                var remainingDays = days % 30;

                if (days <= 0)
                {
                    InfoTextBlock.Text = "Дата окончания должна быть позже даты начала";
                    InfoTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    GroupData.Duration = "Ошибка";
                }
                else if (months > 0 && remainingDays > 0)
                {
                    GroupData.Duration = $"{months} мес. {remainingDays} дн. ({days} дней)";
                    InfoTextBlock.Text = $"Длительность обучения: {months} месяцев и {remainingDays} дней";
                    InfoTextBlock.Foreground = System.Windows.Media.Brushes.Black;
                }
                else if (months > 0)
                {
                    GroupData.Duration = $"{months} мес. ({days} дней)";
                    InfoTextBlock.Text = $"Длительность обучения: {months} месяцев";
                    InfoTextBlock.Foreground = System.Windows.Media.Brushes.Black;
                }
                else
                {
                    GroupData.Duration = $"{days} дней";
                    InfoTextBlock.Text = $"Длительность обучения: {days} дней";
                    InfoTextBlock.Foreground = System.Windows.Media.Brushes.Black;
                }
            }
        }

        private bool ValidateGroup()
        {
            if (string.IsNullOrWhiteSpace(GroupData.Name))
            {
                MessageBox.Show("Введите название группы", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(GroupData.Category))
            {
                MessageBox.Show("Выберите категорию группы", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CategoryComboBox.Focus();
                return false;
            }

            if (GroupData.StartDate >= GroupData.EndDate)
            {
                MessageBox.Show("Дата окончания должна быть позже даты начала", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EndDatePicker.Focus();
                return false;
            }

            if (GroupData.StartDate < new DateTime(2000, 1, 1))
            {
                MessageBox.Show("Дата начала должна быть не ранее 2000 года", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                StartDatePicker.Focus();
                return false;
            }

            return true;
        }

        private bool SaveGroup()
        {
            try
            {
                // Получаем статус как строку
                if (StatusComboBox.SelectedItem is ComboBoxItem selectedStatus)
                {
                    GroupData.Status = selectedStatus.Content.ToString();
                }

                int newId = _dataService.SaveStudyGroup(GroupData);

                if (GroupData.Id == 0)
                {
                    GroupData.Id = newId;
                }

                _isDirty = false;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateGroup())
                return;

            if (SaveGroup())
            {
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isDirty)
            {
                var result = MessageBox.Show(
                    "Есть несохраненные изменения. Закрыть без сохранения?",
                    "Подтверждение закрытия",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            DialogResult = false;
            Close();
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SaveButton_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                CancelButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private void LoadStatus()
        {
            // Устанавливаем выбранное значение статуса
            foreach (ComboBoxItem item in StatusComboBox.Items)
            {
                if (item.Content.ToString() == GroupData.Status)
                {
                    StatusComboBox.SelectedItem = item;
                    break;
                }
            }
        }
    }
}