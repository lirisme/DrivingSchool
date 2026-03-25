using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class ExamManagementPage : Page
    {
        private readonly SqlDataService _dataService;
        private readonly ExamService _examService;
        private List<ExamSchedule> _allActiveSchedules;
        private List<ExamSchedule> _allPastSchedules;
        private List<StudentExamSummary> _allStudents;
        private List<StudentExamSummary> _filteredStudents;

        public ExamManagementPage(SqlDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            _examService = new ExamService(_dataService.GetConnectionString());
            Loaded += async (s, ev) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            await LoadSchedulesAsync();
            await LoadStudentsAsync();
        }

        private async Task LoadSchedulesAsync()
        {
            try
            {
                var allSchedules = new List<ExamSchedule>();

                var internalTheory = await _examService.GetAllSchedulesAsync(ExamType.Internal, ExamStage.Theory);
                var internalPractice = await _examService.GetAllSchedulesAsync(ExamType.Internal, ExamStage.Practice);
                var gibddTheory = await _examService.GetAllSchedulesAsync(ExamType.GIBDD, ExamStage.Theory);
                var gibddPractice = await _examService.GetAllSchedulesAsync(ExamType.GIBDD, ExamStage.Practice);

                if (internalTheory != null) allSchedules.AddRange(internalTheory);
                if (internalPractice != null) allSchedules.AddRange(internalPractice);
                if (gibddTheory != null) allSchedules.AddRange(gibddTheory);
                if (gibddPractice != null) allSchedules.AddRange(gibddPractice);

                // Активные экзамены - дата сегодня или позже
                _allActiveSchedules = allSchedules
                    .Where(s => s.ExamDate.Date >= DateTime.Today)
                    .OrderBy(s => s.ExamDate).ThenBy(s => s.StartTime)
                    .ToList();

                // Проведенные экзамены - дата вчера или раньше
                _allPastSchedules = allSchedules
                    .Where(s => s.ExamDate.Date < DateTime.Today)
                    .OrderByDescending(s => s.ExamDate).ThenBy(s => s.StartTime)
                    .ToList();

                // Устанавливаем источник данных
                SchedulesGrid.ItemsSource = _allActiveSchedules;

                // Применяем фильтр к проведенным
                var filtered = _allPastSchedules.AsEnumerable();
                if (PastExamTypeFilter?.SelectedItem != null)
                {
                    var filterText = (PastExamTypeFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
                    if (filterText == "Внутренние")
                        filtered = filtered.Where(s => s.Type == ExamType.Internal);
                    else if (filterText == "ГИБДД")
                        filtered = filtered.Where(s => s.Type == ExamType.GIBDD);
                }
                PastSchedulesGrid.ItemsSource = filtered.ToList();

                // Обновляем статус
                if (ActiveStatusText != null)
                    ActiveStatusText.Text = $"Активных экзаменов: {_allActiveSchedules.Count}";
                if (PastStatusText != null)
                    PastStatusText.Text = $"Проведенных экзаменов: {_allPastSchedules.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки расписания: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                SchedulesGrid.ItemsSource = new List<ExamSchedule>();
                PastSchedulesGrid.ItemsSource = new List<ExamSchedule>();
            }
        }

        private void ApplyPastFilter()
        {
            if (_allPastSchedules == null) return;

            var filtered = _allPastSchedules.AsEnumerable();

            if (PastExamTypeFilter?.SelectedItem != null)
            {
                var filterText = (PastExamTypeFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (filterText == "Внутренние")
                    filtered = filtered.Where(s => s.Type == ExamType.Internal);
                else if (filterText == "ГИБДД")
                    filtered = filtered.Where(s => s.Type == ExamType.GIBDD);
            }

            PastSchedulesGrid.ItemsSource = filtered.ToList();
        }

        private async Task LoadStudentsAsync()
        {
            try
            {
                var students = (await Task.Run(() => _dataService.LoadStudents())).Students;
                _allStudents = new List<StudentExamSummary>();

                if (students == null || !students.Any())
                {
                    StudentsGrid.ItemsSource = new List<StudentExamSummary>();
                    return;
                }

                foreach (var student in students)
                {
                    var examStatus = await _examService.GetStudentExamStatusAsync(student.Id);

                    // Убираем await, так как метод возвращает int, а не Task
                    int totalLessons = _dataService.GetLessonsCountByCategory(student.Id);
                    int completedLessons = student.CompletedLessons ?? 0;

                    var summary = new StudentExamSummary
                    {
                        StudentId = student.Id,
                        StudentName = student.FullName ?? $"{student.LastName} {student.FirstName}",
                        CategoryCode = student.CategoryCode ?? "B",
                        CompletedLessons = completedLessons,
                        TotalLessons = totalLessons,
                        Phone = student.Phone ?? "",
                        InternalTheoryPassed = examStatus.InternalTheoryPassed,
                        InternalPracticePassed = examStatus.InternalPracticePassed,
                        OverallStatus = GetOverallStatus(examStatus, completedLessons, totalLessons)
                    };
                    _allStudents.Add(summary);
                }

                // Сортируем по количеству уроков (сверху те, у кого больше)
                _allStudents = _allStudents.OrderByDescending(s => s.CompletedLessons).ToList();
                StudentsGrid.ItemsSource = _allStudents;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки учеников: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StudentsGrid.ItemsSource = new List<StudentExamSummary>();
            }
        }

        private string GetOverallStatus(StudentExamStatus status, int completedLessons, int totalLessons)
        {
            if (status.GIBDDTheoryPassed && status.GIBDDPracticePassed)
                return "🎓 Выпущен";
            if (status.InternalTheoryPassed && status.InternalPracticePassed)
                return "✅ Готов к ГИБДД";
            if (completedLessons >= totalLessons)
                return "🚗 Готов к практике";
            if (completedLessons >= totalLessons / 2)
                return "📚 Готов к теории";
            return $"📖 В обучении ({completedLessons}/{totalLessons})";
        }

        private string GetOverallStatus(StudentExamStatus status, int completedLessons)
        {
            if (status.GIBDDTheoryPassed && status.GIBDDPracticePassed)
                return "🎓 Выпущен";
            if (status.InternalTheoryPassed && status.InternalPracticePassed)
                return "✅ Готов к ГИБДД";
            if (completedLessons >= 28)
                return "🚗 Готов к практике";
            if (completedLessons >= 14)
                return "📚 Готов к теории";
            return $"📖 В обучении ({completedLessons}/28)";
        }

        // Поиск в реальном времени
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_allStudents == null) return;

            var searchText = SearchTextBox.Text?.ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredStudents = _allStudents;
            }
            else
            {
                _filteredStudents = _allStudents
                    .Where(s => s.StudentName.ToLower().Contains(searchText) ||
                                s.Phone.ToLower().Contains(searchText))
                    .ToList();
            }

            StudentsGrid.ItemsSource = _filteredStudents;
        }

        private void PastExamTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyPastFilter();
        }

        private async void CreateExamBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CreateExamScheduleDialog(_dataService);
            if (dialog.ShowDialog() == true && dialog.CreatedSchedule != null)
            {
                try
                {
                    MessageBox.Show("Экзамен успешно создан!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadSchedulesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void RegisterStudentBtn_Click(object sender, RoutedEventArgs args)
        {
            var button = sender as Button;
            var student = button?.Tag as StudentExamSummary;
            if (student == null) return;

            var schedule = SchedulesGrid.SelectedItem as ExamSchedule;
            if (schedule == null)
            {
                MessageBox.Show("Сначала выберите экзамен в расписании", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка даты экзамена
            if (schedule.ExamDate < DateTime.Today)
            {
                var result = MessageBox.Show(
                    $"Внимание! Дата экзамена {schedule.ExamDate:dd.MM.yyyy} уже прошла.\n\n" +
                    $"Вы действительно хотите записать ученика {student.StudentName} на этот экзамен?",
                    "Дата экзамена в прошлом",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            // Проверка для ГИБДД
            if (schedule.Type == ExamType.GIBDD)
            {
                if (!student.InternalTheoryPassed || !student.InternalPracticePassed)
                {
                    var result = MessageBox.Show(
                        $"Ученик {student.StudentName} НЕ СДАЛ внутренний экзамен!\n\n" +
                        $"Внутренний теория: {(student.InternalTheoryPassed ? "✅" : "❌")}\n" +
                        $"Внутренний практика: {(student.InternalPracticePassed ? "✅" : "❌")}\n\n" +
                        $"Записать его на экзамен в ГИБДД?",
                        "Предупреждение",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes)
                        return;
                }
            }

            var confirm = MessageBox.Show($"Записать {student.StudentName} на экзамен?\n" +
                $"{schedule.ExamDate:dd.MM.yyyy} {schedule.StartTime:hh\\:mm}\n{schedule.Location}",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var success = await _examService.RegisterForExamAsync(student.StudentId, schedule.Id);
                if (success)
                {
                    MessageBox.Show("Ученик успешно записан!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadSchedulesAsync();
                    await LoadStudentsAsync();
                }
                else
                {
                    MessageBox.Show("Ученик уже записан на этот экзамен", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при записи: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteExam_Click(object sender, RoutedEventArgs e)
        {
            var schedule = SchedulesGrid.SelectedItem as ExamSchedule;
            if (schedule == null) return;

            var result = MessageBox.Show($"Удалить экзамен?\n{schedule.ExamDate:dd.MM.yyyy} {schedule.StartTime:hh\\:mm}",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await _examService.DeleteExamScheduleAsync(schedule.Id);
                MessageBox.Show("Экзамен удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadSchedulesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeletePastExam_Click(object sender, RoutedEventArgs e)
        {
            var schedule = PastSchedulesGrid.SelectedItem as ExamSchedule;
            if (schedule == null) return;

            var result = MessageBox.Show($"Удалить проведенный экзамен?\n{schedule.ExamDate:dd.MM.yyyy} {schedule.StartTime:hh\\:mm}",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await _examService.DeleteExamScheduleAsync(schedule.Id);
                MessageBox.Show("Экзамен удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadSchedulesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ConductExam_Click(object sender, RoutedEventArgs e)
        {
            var schedule = SchedulesGrid.SelectedItem as ExamSchedule;
            if (schedule == null) return;

            if (schedule.ExamDate > DateTime.Today)
            {
                MessageBox.Show("Нельзя провести экзамен, который еще не наступил", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Отметить экзамен как проведенный?\n{schedule.ExamDate:dd.MM.yyyy}",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await _examService.MarkExamAsConductedAsync(schedule.Id);
                MessageBox.Show("Экзамен отмечен как проведенный", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadSchedulesAsync();
            }
        }

        private async void SchedulesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var schedule = SchedulesGrid.SelectedItem as ExamSchedule;
            if (schedule == null) return;

            var registeredCount = await _examService.GetRegisteredStudentsCountAsync(schedule.Id);

            MessageBox.Show($"📋 ИНФОРМАЦИЯ ОБ ЭКЗАМЕНЕ\n\n" +
                $"📅 Дата: {schedule.ExamDate:dd.MM.yyyy}\n" +
                $"⏰ Время: {schedule.StartTime:hh\\:mm} - {schedule.EndTime:hh\\:mm}\n" +
                $"📌 Тип: {(schedule.Type == ExamType.Internal ? "Внутренний" : "ГИБДД")}\n" +
                $"📖 Этап: {(schedule.Stage == ExamStage.Theory ? "Теория" : "Практика")}\n" +
                $"📍 Место: {schedule.Location}\n" +
                $"👥 Записано: {registeredCount}/{schedule.MaxStudents}",
                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void PastSchedulesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var schedule = PastSchedulesGrid.SelectedItem as ExamSchedule;
            if (schedule == null) return;

            var registeredCount = await _examService.GetRegisteredStudentsCountAsync(schedule.Id);

            MessageBox.Show($"📋 ИНФОРМАЦИЯ О ПРОВЕДЕННОМ ЭКЗАМЕНЕ\n\n" +
                $"📅 Дата: {schedule.ExamDate:dd.MM.yyyy}\n" +
                $"⏰ Время: {schedule.StartTime:hh\\:mm} - {schedule.EndTime:hh\\:mm}\n" +
                $"📌 Тип: {(schedule.Type == ExamType.Internal ? "Внутренний" : "ГИБДД")}\n" +
                $"📖 Этап: {(schedule.Stage == ExamStage.Theory ? "Теория" : "Практика")}\n" +
                $"📍 Место: {schedule.Location}\n" +
                $"👥 Было записано: {registeredCount}",
                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }
    }
}