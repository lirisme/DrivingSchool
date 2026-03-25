using System;
using System.Windows;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class CreateExamScheduleDialog : Window
    {
        private readonly ExamService _examService;
        private readonly SqlDataService _dataService;

        public ExamSchedule CreatedSchedule { get; private set; }

        public CreateExamScheduleDialog(SqlDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            _examService = new ExamService(_dataService.GetConnectionString());

            ExamDatePicker.SelectedDate = DateTime.Now;
            // Убираем ограничение на выбор дат в прошлом
            // ExamDatePicker.DisplayDateStart = DateTime.Now;
        }

        private bool TryParseTime(string timeStr, out TimeSpan time)
        {
            time = TimeSpan.Zero;
            var parts = timeStr.Split(':');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int hours) &&
                int.TryParse(parts[1], out int minutes) &&
                hours >= 0 && hours <= 23 &&
                minutes >= 0 && minutes <= 59)
            {
                time = new TimeSpan(hours, minutes, 0);
                return true;
            }
            return false;
        }

        private async void CreateBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка даты
                if (!ExamDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Выберите дату экзамена", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var examDate = ExamDatePicker.SelectedDate.Value;

                // Проверка даты в прошлом с предупреждением
                if (examDate < DateTime.Today)
                {
                    var result = MessageBox.Show(
                        $"Дата экзамена {examDate:dd.MM.yyyy} уже прошла.\n\n" +
                        "Вы действительно хотите создать экзамен с прошедшей датой?",
                        "Предупреждение",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes)
                        return;
                }

                // Проверка времени начала
                if (!TryParseTime(StartTimeTextBox.Text.Trim(), out TimeSpan startTime))
                {
                    MessageBox.Show("Неверный формат времени начала.\nИспользуйте формат ЧЧ:ММ (например 09:00 или 14:30)",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    StartTimeTextBox.Focus();
                    return;
                }

                // Проверка времени окончания
                if (!TryParseTime(EndTimeTextBox.Text.Trim(), out TimeSpan endTime))
                {
                    MessageBox.Show("Неверный формат времени окончания.\nИспользуйте формат ЧЧ:ММ (например 11:00 или 16:30)",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    EndTimeTextBox.Focus();
                    return;
                }

                // Проверка что начало раньше окончания
                if (startTime >= endTime)
                {
                    MessageBox.Show("Время начала должно быть меньше времени окончания", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    StartTimeTextBox.Focus();
                    return;
                }

                // Определяем тип экзамена
                ExamType type;
                if (InternalRadio.IsChecked == true)
                    type = ExamType.Internal;
                else
                    type = ExamType.GIBDD;

                // Определяем этап
                ExamStage stage;
                if (TheoryRadio.IsChecked == true)
                    stage = ExamStage.Theory;
                else
                    stage = ExamStage.Practice;

                // Проверка места проведения
                if (string.IsNullOrWhiteSpace(LocationTextBox.Text))
                {
                    MessageBox.Show("Укажите место проведения экзамена", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    LocationTextBox.Focus();
                    return;
                }

                CreatedSchedule = new ExamSchedule
                {
                    Type = type,
                    Stage = stage,
                    ExamDate = examDate,
                    StartTime = startTime,
                    EndTime = endTime,
                    MaxStudents = 999,
                    CurrentStudents = 0,
                    ExaminerName = "Не назначен",
                    Location = LocationTextBox.Text.Trim(),
                    IsAvailable = true
                };

                // Сохраняем в базу
                await _examService.CreateExamScheduleAsync(CreatedSchedule);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}