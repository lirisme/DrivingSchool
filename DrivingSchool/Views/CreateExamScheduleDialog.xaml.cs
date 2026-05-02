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

                // Определяем тип экзамена
                ExamType type = InternalRadio.IsChecked == true ? ExamType.Internal : ExamType.GIBDD;

                // Убираем этап - по умолчанию теория (значение не важно, так как при создании не делим)
                // При отметке результатов будет отдельный выбор теории/практики
                ExamStage stage = ExamStage.Theory; // Значение по умолчанию

                CreatedSchedule = new ExamSchedule
                {
                    Type = type,
                    Stage = stage,
                    ExamDate = examDate,
                    StartTime = TimeSpan.FromHours(9),
                    EndTime = TimeSpan.FromHours(17),
                    MaxStudents = 999,
                    CurrentStudents = 0,
                    ExaminerName = "Не назначен",
                    Location = "Автошкола",
                    IsAvailable = true,
                    IsConducted = false
                };

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