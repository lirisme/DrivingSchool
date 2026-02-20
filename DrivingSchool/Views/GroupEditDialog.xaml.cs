using DrivingSchool.Models;
using DrivingSchool.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace DrivingSchool.Views
{
    public partial class GroupEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        public StudyGroup GroupData { get; private set; }

        public GroupEditDialog(SqlDataService dataService, StudyGroup groupData = null)
        {
            InitializeComponent();
            _dataService = dataService;

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
            StartDatePicker.SelectedDateChanged += DateChanged;
            EndDatePicker.SelectedDateChanged += DateChanged;
            UpdateDuration();
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

                if (months > 0 && remainingDays > 0)
                {
                    GroupData.Duration = $"{months} мес. {remainingDays} дн. ({days} дней)";
                    InfoTextBlock.Text = $"Длительность обучения: {months} месяцев и {remainingDays} дней";
                }
                else if (months > 0)
                {
                    GroupData.Duration = $"{months} мес. ({days} дней)";
                    InfoTextBlock.Text = $"Длительность обучения: {months} месяцев";
                }
                else
                {
                    GroupData.Duration = $"{days} дней";
                    InfoTextBlock.Text = $"Длительность обучения: {days} дней";
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(GroupData.Name))
            {
                MessageBox.Show("Введите номер группы", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            if (GroupData.StartDate >= GroupData.EndDate)
            {
                MessageBox.Show("Дата окончания должна быть позже даты начала", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EndDatePicker.Focus();
                return;
            }

            try
            {
                _dataService.SaveStudyGroup(GroupData);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}