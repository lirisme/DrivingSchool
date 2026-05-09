using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

            ExamDatePicker.SelectedDate = DateTime.Now.AddDays(7);
        }

        private async void CreateBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ExamDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Выберите дату экзамена", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var examDate = ExamDatePicker.SelectedDate.Value;

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

                // Тип экзамена
                ExamType type = InternalRadio.IsChecked == true ? ExamType.Internal : ExamType.GIBDD;

                // Этап экзамена
                ExamStage stage = TheoryRadio.IsChecked == true ? ExamStage.Theory : ExamStage.Practice;

                // Время по умолчанию 09:00 - 17:00
                TimeSpan startTime = TimeSpan.FromHours(9);
                TimeSpan endTime = TimeSpan.FromHours(17);

                // Максимум студентов - большой (не ограничиваем)
                int maxStudents = 999;

                CreatedSchedule = new ExamSchedule
                {
                    Type = type,
                    Stage = stage,
                    ExamDate = examDate,
                    StartTime = startTime,
                    EndTime = endTime,
                    MaxStudents = maxStudents,
                    CurrentStudents = 0,
                    ExaminerName = "Не назначен",
                    Location = "Автошкола",
                    IsAvailable = true,
                    IsConducted = false
                };

                int newId = await _examService.CreateExamScheduleAsync(CreatedSchedule);
                CreatedSchedule.Id = newId;

                string stageText = stage == ExamStage.Theory ? "теории" : "практики";
                string typeText = type == ExamType.Internal ? "внутренний" : "ГИБДД";

                MessageBox.Show($"Экзамен по {stageText} ({typeText}) успешно создан!\n" +
                    $"Дата: {examDate:dd.MM.yyyy}",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

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

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CreateBtn_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                CancelBtn_Click(sender, e);
                e.Handled = true;
            }
        }
    }
}