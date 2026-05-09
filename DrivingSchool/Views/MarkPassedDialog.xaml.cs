using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Models;

namespace DrivingSchool.Views
{
    public partial class MarkPassedDialog : Window
    {
        public List<StudentExamMark> Students { get; private set; }
        public ExamType ExamType { get; private set; }
        public ExamStage ExamStage { get; private set; }

        public MarkPassedDialog(List<StudentExamMark> students, ExamType examType, ExamStage examStage)
        {
            InitializeComponent();
            Students = students;
            ExamType = examType;
            ExamStage = examStage;

            string stageText = examStage == ExamStage.Theory ? "теорию" : "практику";
            string typeText = examType == ExamType.Internal ? "внутренний" : "ГИБДД";
            TitleText.Text = $"Отметьте, кто сдал {stageText} ({typeText} экзамен):";

            foreach (var student in students)
            {
                // Проверка: уже сдал этот экзамен ранее
                if (examStage == ExamStage.Theory && student.TheoryAlreadyPassed)
                {
                    student.ExamPassed = true;
                    student.ExamEditable = false;
                    student.StatusDisplay = "✅ Уже сдан ранее";
                }
                else if (examStage == ExamStage.Practice && student.PracticeAlreadyPassed)
                {
                    student.ExamPassed = true;
                    student.ExamEditable = false;
                    student.StatusDisplay = "✅ Уже сдан ранее";
                }
                else
                {
                    // Проверка попыток (максимум 3)
                    int attempts = examStage == ExamStage.Theory ? student.TheoryAttempts : student.PracticeAttempts;
                    if (attempts >= 3)
                    {
                        student.ExamEditable = false;
                        student.StatusDisplay = "⚠️ Превышены попытки (3/3), требуется переобучение";
                    }
                    else
                    {
                        student.StatusDisplay = "Ожидает сдачи";
                    }
                }

                // Для ГИБДД: проверка допуска (сданы внутренние экзамены)
                if (examType == ExamType.GIBDD)
                {
                    if (!student.InternalTheoryPassed)
                    {
                        student.ExamEditable = false;
                        student.StatusDisplay = "❌ Нет допуска: не сдана внутренняя теория";
                    }
                    else if (!student.InternalPracticePassed)
                    {
                        student.ExamEditable = false;
                        student.StatusDisplay = "❌ Нет допуска: не сдана внутренняя практика";
                    }
                }

                // Установка количества попыток для отображения
                student.AttemptsCount = examStage == ExamStage.Theory ? student.TheoryAttempts : student.PracticeAttempts;
            }

            StudentsGrid.ItemsSource = Students;

            // Отладка
            Loaded += (s, e) =>
            {
                foreach (var student in Students)
                {
                    System.Diagnostics.Debug.WriteLine($"Студент: {student.StudentName}");
                    System.Diagnostics.Debug.WriteLine($"  Этап: {examStage}");
                    System.Diagnostics.Debug.WriteLine($"  Сдан: {student.ExamPassed}, доступен: {student.ExamEditable}");
                    System.Diagnostics.Debug.WriteLine($"  Статус: {student.StatusDisplay}");
                }
            };
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            foreach (var student in Students)
            {
                System.Diagnostics.Debug.WriteLine($"{student.StudentName}: Результат={student.ExamPassed}");
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class StudentExamMark : INotifyPropertyChanged
    {
        private int _studentId;
        private string _studentName;
        private string _categoryCode;
        private string _phone;
        private bool _examPassed;
        private bool _examEditable = true;
        private int _theoryAttempts;
        private int _practiceAttempts;
        private int _attemptsCount;
        private bool _internalTheoryPassed;
        private bool _internalPracticePassed;
        private bool _theoryAlreadyPassed;
        private bool _practiceAlreadyPassed;
        private string _statusDisplay = "Ожидает";

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public int StudentId { get => _studentId; set { _studentId = value; OnPropertyChanged(); } }
        public string StudentName { get => _studentName; set { _studentName = value; OnPropertyChanged(); } }
        public string CategoryCode { get => _categoryCode; set { _categoryCode = value; OnPropertyChanged(); } }
        public string Phone { get => _phone; set { _phone = value; OnPropertyChanged(); } }

        public bool ExamPassed
        {
            get => _examPassed;
            set { _examPassed = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusDisplay)); }
        }

        public bool ExamEditable
        {
            get => _examEditable;
            set { _examEditable = value; OnPropertyChanged(); }
        }

        public int TheoryAttempts
        {
            get => _theoryAttempts;
            set { _theoryAttempts = value; OnPropertyChanged(); }
        }

        public int PracticeAttempts
        {
            get => _practiceAttempts;
            set { _practiceAttempts = value; OnPropertyChanged(); }
        }

        public int AttemptsCount
        {
            get => _attemptsCount;
            set { _attemptsCount = value; OnPropertyChanged(); }
        }

        public bool InternalTheoryPassed
        {
            get => _internalTheoryPassed;
            set { _internalTheoryPassed = value; OnPropertyChanged(); }
        }

        public bool InternalPracticePassed
        {
            get => _internalPracticePassed;
            set { _internalPracticePassed = value; OnPropertyChanged(); }
        }

        public bool TheoryAlreadyPassed
        {
            get => _theoryAlreadyPassed;
            set { _theoryAlreadyPassed = value; OnPropertyChanged(); }
        }

        public bool PracticeAlreadyPassed
        {
            get => _practiceAlreadyPassed;
            set { _practiceAlreadyPassed = value; OnPropertyChanged(); }
        }

        public string StatusDisplay
        {
            get => _statusDisplay;
            set { _statusDisplay = value; OnPropertyChanged(); }
        }
    }
}