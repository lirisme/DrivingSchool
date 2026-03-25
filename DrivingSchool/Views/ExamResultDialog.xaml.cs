using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Models;

namespace DrivingSchool.Views
{
    public partial class ExamResultDialog : Window
    {
        private readonly Student _student;
        private readonly ExamType _examType;
        private readonly ExamStage _examStage;

        public ExamRecord ExamRecord { get; private set; }

        public ExamResultDialog(Student student, ExamType examType, ExamStage examStage)
        {
            InitializeComponent();

            _student = student;
            _examType = examType;
            _examStage = examStage;

            Loaded += (s, e) => InitializeDialog();
        }

        private void InitializeDialog()
        {
            StudentNameText.Text = $"{_student.FullName}";
            ExamInfoText.Text = $"{GetExamTypeName()} - {GetExamStageName()}";

            ExamDatePicker.SelectedDate = DateTime.Today;

            ResultCombo.SelectionChanged += (s, e) => UpdateScoreSliderState();
            ScoreSlider.ValueChanged += (s, e) => ScoreDisplayText.Text = $"Баллы: {ScoreSlider.Value:F0}";

            UpdateScoreSliderState();
        }

        private void UpdateScoreSliderState()
        {
            var isPassed = ResultCombo.SelectedItem is ComboBoxItem item &&
                           item.Tag.ToString() == "Passed";

            ScoreSlider.IsEnabled = isPassed;
            ScoreDisplayText.Foreground = isPassed ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Gray;
        }

        private string GetExamTypeName()
        {
            return _examType == ExamType.Internal ? "Внутренний экзамен" : "Экзамен в ГИБДД";
        }

        private string GetExamStageName()
        {
            return _examStage == ExamStage.Theory ? "Теория (ПДД)" : "Практика (вождение)";
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ExamDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Выберите дату экзамена", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedResult = (ComboBoxItem)ResultCombo.SelectedItem;
                ExamResult result;

                switch (selectedResult.Tag.ToString())
                {
                    case "Passed":
                        result = ExamResult.Passed;
                        break;
                    case "Failed":
                        result = ExamResult.Failed;
                        break;
                    default:
                        result = ExamResult.Scheduled;
                        break;
                }

                var mistakes = MistakesTextBox.Text
                    .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(m => m.Trim())
                    .Where(m => !string.IsNullOrEmpty(m))
                    .ToList();

                ExamRecord = new ExamRecord
                {
                    StudentId = _student.Id,
                    Type = _examType,
                    Stage = _examStage,
                    ExamDate = ExamDatePicker.SelectedDate.Value,
                    Result = result,
                    Score = result == ExamResult.Passed ? (int)ScoreSlider.Value : 0,
                    AttemptNumber = 1, // Будет обновлено в сервисе
                    ExaminerName = ExaminerTextBox.Text,
                    Notes = NotesTextBox.Text,
                    Mistakes = mistakes,
                    CreatedDate = DateTime.Now
                };

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