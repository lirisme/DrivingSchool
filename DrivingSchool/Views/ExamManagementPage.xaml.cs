using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DrivingSchool.Models;
using DrivingSchool.Services;
using DrivingSchool.AI;  // <-- ДОБАВЛЕНО

namespace DrivingSchool.Views
{
    public partial class ExamManagementPage : Page
    {
        private readonly SqlDataService _dataService;
        private readonly ExamService _examService;
        private AIClient _aiClient;  // <-- ДОБАВЛЕНО
        private List<ExamSchedule> _allActiveSchedules;
        private List<ExamSchedule> _allPastSchedules;
        private List<StudentExamSummary> _allStudents;
        private List<StudentExamSummary> _filteredStudents;

        public ExamManagementPage(SqlDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            _examService = new ExamService(_dataService.GetConnectionString());
            _aiClient = new AIClient();  // <-- ДОБАВЛЕНО
            Loaded += async (s, ev) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            await LoadSchedulesAsync();
            await LoadStudentsAsync();
        }

        /// <summary>
        /// Загрузка расписания
        /// </summary>
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

                // АКТИВНЫЕ: все НЕ проведенные экзамены
                _allActiveSchedules = allSchedules
                    .Where(s => !s.IsConducted)
                    .OrderBy(s => s.ExamDate).ThenBy(s => s.StartTime)
                    .ToList();

                // ПРОВЕДЕННЫЕ: только IsConducted = true
                _allPastSchedules = allSchedules
                    .Where(s => s.IsConducted)
                    .OrderByDescending(s => s.ExamDate).ThenBy(s => s.StartTime)
                    .ToList();

                SchedulesGrid.ItemsSource = _allActiveSchedules;
                PastSchedulesGrid.ItemsSource = _allPastSchedules;

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


                    int totalLessons = _dataService.GetLessonsCountByCategory(student.Id);
                    int completedLessons = (student.CompletedLessons ?? 0) + (student.MissedLessons ?? 0);                    // ========== ДАННЫЕ ДЛЯ ИИ ==========
                    int maxGap = await _dataService.GetMaxGapDaysAsync(student.Id);
                    double avgGap = await _dataService.GetAvgGapDaysAsync(student.Id);
                    int lastGap = await _dataService.GetLastGapDaysAsync(student.Id);

                    if (student.LastName.Contains("Корецк"))
                    {
                        System.Windows.MessageBox.Show($"maxGap: {maxGap}, avgGap: {avgGap}, lastGap: {lastGap}");
                    } 

                    var aiData = new StudentDataForAI
                    {
                        CompletedLessons = completedLessons,
                        TotalRequiredLessons = totalLessons,
                        MissedLessons = student.MissedLessons ?? 0,
                        MaxGapDays = maxGap,
                        AvgGapDays = avgGap,
                        LastGapDays = lastGap,
                        TheoryAttempts = 0,
                        PracticeAttempts = 0
                    };

                    AIRecommendation aiRecommendation = null;
                    try
                    {
                        aiRecommendation = await _aiClient.GetRecommendationAsync(aiData);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"AI Error: {ex.Message}");
                    }
                    // ===================================

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
                        OverallStatus = GetOverallStatus(examStatus, completedLessons, totalLessons),

                        // ========== ИИ ПОЛЯ ==========
                        AIRecommendationText = aiRecommendation?.Recommendation ?? "Загрузка...",
                        AIReadinessScore = aiRecommendation?.Score ?? 0,
                        AIReadinessColor = aiRecommendation?.Score >= 80 ? "Green" :
                                           (aiRecommendation?.Score >= 60 ? "Orange" : "Red")
                    };
                    _allStudents.Add(summary);
                }

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
                return "🚗 Готов к внутреннему экзамену";
            return $"📖 В обучении ({completedLessons}/{totalLessons})";
        }

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

        /// <summary>
        /// Запись студента на экзамен
        /// </summary>
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
                $"{schedule.ExamDate:dd.MM.yyyy}",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var (success, message) = await _examService.RegisterForExamAsync(student.StudentId, schedule.Id);
                if (success)
                {
                    MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadSchedulesAsync();
                    await LoadStudentsAsync();
                }
                else
                {
                    MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        /// <summary>
        /// Проведение экзамена
        /// </summary>
        private async void ConductExam_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var schedule = SchedulesGrid.SelectedItem as ExamSchedule;
                if (schedule == null)
                {
                    MessageBox.Show("Выберите экзамен", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Проведение экзамена: Id={schedule.Id}, Type={schedule.Type}");

                // Получаем студентов, записанных на экзамен (без привязки к этапу)
                var registeredStudents = await _examService.GetRegisteredStudentsWithStatusForGeneralExamAsync(schedule.Id, schedule.Type);

                if (!registeredStudents.Any())
                {
                    MessageBox.Show("На этот экзамен никто не записан", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new MarkPassedDialog(registeredStudents, schedule.Type); dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    int savedCount = 0;
                    int failedCount = 0;

                    foreach (var student in registeredStudents)
                    {
                        try
                        {
                            // Сохраняем результат по теории
                            if (student.TheoryEditable)
                            {
                                int theoryAttempt = student.TheoryAttempts + 1;
                                await _examService.SaveExamResultAsync(new ExamRecord
                                {
                                    StudentId = student.StudentId,
                                    ScheduleId = schedule.Id,
                                    Type = schedule.Type,
                                    Stage = ExamStage.Theory,
                                    ExamDate = DateTime.Now,
                                    Result = student.TheoryPassed ? ExamResult.Passed : ExamResult.Failed,
                                    AttemptNumber = theoryAttempt,
                                    ExaminerName = "Экзаменатор",
                                    Notes = student.TheoryPassed ? "Теория сдана" : "Теория не сдана"
                                });
                                System.Diagnostics.Debug.WriteLine($"Студент {student.StudentName}: Теория - {(student.TheoryPassed ? "Сдан" : "Не сдан")}, попытка {theoryAttempt}");
                            }

                            // Сохраняем результат по практике
                            if (student.PracticeEditable)
                            {
                                int practiceAttempt = student.PracticeAttempts + 1;
                                await _examService.SaveExamResultAsync(new ExamRecord
                                {
                                    StudentId = student.StudentId,
                                    ScheduleId = schedule.Id,
                                    Type = schedule.Type,
                                    Stage = ExamStage.Practice,
                                    ExamDate = DateTime.Now,
                                    Result = student.PracticePassed ? ExamResult.Passed : ExamResult.Failed,
                                    AttemptNumber = practiceAttempt,
                                    ExaminerName = "Экзаменатор",
                                    Notes = student.PracticePassed ? "Практика сдана" : "Практика не сдана"
                                });
                                System.Diagnostics.Debug.WriteLine($"Студент {student.StudentName}: Практика - {(student.PracticePassed ? "Сдан" : "Не сдан")}, попытка {practiceAttempt}");
                            }

                            savedCount++;
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            System.Diagnostics.Debug.WriteLine($"Ошибка сохранения для {student.StudentName}: {ex.Message}");
                            MessageBox.Show($"Ошибка при сохранении результата для {student.StudentName}: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"Сохранено: {savedCount}, ошибок: {failedCount}");

                    // Отмечаем экзамен как проведенный
                    bool conducted = await _examService.MarkExamAsConductedAsync(schedule.Id);
                    System.Diagnostics.Debug.WriteLine($"Экзамен отмечен как проведенный: {conducted}");

                    var theoryPassedCount = registeredStudents.Count(s => s.TheoryPassed);
                    var practicePassedCount = registeredStudents.Count(s => s.PracticePassed);

                    MessageBox.Show($"Экзамен проведен!\n" +
                        $"Теорию сдало: {theoryPassedCount} из {registeredStudents.Count}\n" +
                        $"Практику сдало: {practicePassedCount} из {registeredStudents.Count}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    await LoadSchedulesAsync();
                    await LoadStudentsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Общая ошибка: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Стек: {ex.StackTrace}");
                MessageBox.Show($"Ошибка при проведении экзамена: {ex.Message}\n\n{ex.StackTrace}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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