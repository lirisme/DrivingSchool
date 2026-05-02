using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using DrivingSchool.Models;

namespace DrivingSchool.Views
{
    public partial class MarkPassedDialog : Window
    {
        public List<StudentExamMark> Students { get; private set; }
        public ExamType ExamType { get; private set; }
        public ExamStage ExamStage { get; private set; }

        public MarkPassedDialog(List<StudentExamMark> students, ExamType examType)
        {
            InitializeComponent();
            Students = students;
            ExamType = examType;

            foreach (var student in students)
            {
                // 1. Базовые проверки уже сданных экзаменов
                if (student.TheoryAlreadyPassed)
                {
                    student.TheoryPassed = true;
                    student.TheoryEditable = false;
                }

                if (student.PracticeAlreadyPassed)
                {
                    student.PracticePassed = true;
                    student.PracticeEditable = false;
                }

                // 2. Проверки для ГИБДД
                if (examType == ExamType.GIBDD)
                {
                    // Проверка попыток для теории (только если еще не сдана)
                    if (!student.TheoryAlreadyPassed && student.TheoryAttempts >= 3)
                    {
                        student.TheoryEditable = false;
                    }

                    // Проверка попыток для практики (только если еще не сдана)
                    if (!student.PracticeAlreadyPassed && student.PracticeAttempts >= 3)
                    {
                        student.PracticeEditable = false;
                    }

                    // Проверка допуска к практике ГИБДД
                    if (!student.InternalTheoryPassed || !student.InternalPracticePassed)
                    {
                        student.PracticeEditable = false;
                    }
                }

                // 3. Установка количества попыток для отображения
                if (examType == ExamType.GIBDD)
                {
                    student.AttemptsCount = Math.Max(student.TheoryAttempts, student.PracticeAttempts);
                }
                else
                {
                    student.AttemptsCount = student.AttemptsCount; // или другая логика для внутренних экзаменов
                }

                // 4. Обновление статуса отображения
                UpdateStatusDisplay(student);
            }

            StudentsGrid.ItemsSource = Students;

            // Отладка: проверка состояния после загрузки
            Loaded += (s, e) =>
            {
                foreach (var student in Students)
                {
                    System.Diagnostics.Debug.WriteLine($"Студент: {student.StudentName}");
                    System.Diagnostics.Debug.WriteLine($"  Теория: сдана={student.TheoryPassed}, доступна={student.TheoryEditable}");
                    System.Diagnostics.Debug.WriteLine($"  Практика: сдана={student.PracticePassed}, доступна={student.PracticeEditable}");
                    System.Diagnostics.Debug.WriteLine($"  Статус: {student.StatusDisplay}");
                }
            };
        }

        private void UpdateStatusDisplay(StudentExamMark student)
        {
            if (student.TheoryAlreadyPassed && student.PracticeAlreadyPassed)
                student.StatusDisplay = "✅ Уже сданы оба этапа";
            else if (student.TheoryAlreadyPassed)
                student.StatusDisplay = "✅ Теория сдана, нужно сдать практику";
            else if (student.PracticeAlreadyPassed)
                student.StatusDisplay = "✅ Практика сдана, нужно сдать теорию";
            else if (ExamType == ExamType.GIBDD)
            {
                if (!student.InternalTheoryPassed)
                    student.StatusDisplay = "❌ Нет допуска (не сдана внутренняя теория)";
                else if (!student.InternalPracticePassed)
                    student.StatusDisplay = "❌ Нет допуска (не сдана внутренняя практика)";
                else if (student.TheoryAttempts >= 3 && !student.TheoryAlreadyPassed)
                    student.StatusDisplay = "⚠️ Превышены попытки теории (3/3)";
                else if (student.PracticeAttempts >= 3 && !student.PracticeAlreadyPassed)
                    student.StatusDisplay = "⚠️ Превышены попытки практики (3/3)";
                else
                    student.StatusDisplay = "Ожидает сдачи";
            }
            else
            {
                student.StatusDisplay = "Ожидает сдачи";
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, что было отмечено
            foreach (var student in Students)
            {
                System.Diagnostics.Debug.WriteLine($"{student.StudentName}: Теория={student.TheoryPassed}, Практика={student.PracticePassed}");
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
        private bool _theoryPassed;
        private bool _practicePassed;
        private bool _alreadyPassed;
        private bool _theoryAlreadyPassed;
        private bool _practiceAlreadyPassed;
        private bool _theoryEditable = true;
        private bool _practiceEditable = true;
        private int _theoryAttempts;
        private int _practiceAttempts;
        private int _attemptsCount;
        private bool _internalTheoryPassed;
        private bool _internalPracticePassed;
        private string _statusDisplay = "Ожидает";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public int StudentId
        {
            get => _studentId;
            set
            {
                if (_studentId != value)
                {
                    _studentId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StudentName
        {
            get => _studentName;
            set
            {
                if (_studentName != value)
                {
                    _studentName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CategoryCode
        {
            get => _categoryCode;
            set
            {
                if (_categoryCode != value)
                {
                    _categoryCode = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Phone
        {
            get => _phone;
            set
            {
                if (_phone != value)
                {
                    _phone = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool TheoryPassed
        {
            get => _theoryPassed;
            set
            {
                if (_theoryPassed != value)
                {
                    _theoryPassed = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool PracticePassed
        {
            get => _practicePassed;
            set
            {
                if (_practicePassed != value)
                {
                    _practicePassed = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool AlreadyPassed
        {
            get => _alreadyPassed;
            set
            {
                if (_alreadyPassed != value)
                {
                    _alreadyPassed = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool TheoryAlreadyPassed
        {
            get => _theoryAlreadyPassed;
            set
            {
                if (_theoryAlreadyPassed != value)
                {
                    _theoryAlreadyPassed = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool PracticeAlreadyPassed
        {
            get => _practiceAlreadyPassed;
            set
            {
                if (_practiceAlreadyPassed != value)
                {
                    _practiceAlreadyPassed = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool TheoryEditable
        {
            get => _theoryEditable;
            set
            {
                if (_theoryEditable != value)
                {
                    _theoryEditable = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool PracticeEditable
        {
            get => _practiceEditable;
            set
            {
                if (_practiceEditable != value)
                {
                    _practiceEditable = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TheoryAttempts
        {
            get => _theoryAttempts;
            set
            {
                if (_theoryAttempts != value)
                {
                    _theoryAttempts = value;
                    OnPropertyChanged();
                }
            }
        }

        public int PracticeAttempts
        {
            get => _practiceAttempts;
            set
            {
                if (_practiceAttempts != value)
                {
                    _practiceAttempts = value;
                    OnPropertyChanged();
                }
            }
        }

        public int AttemptsCount
        {
            get => _attemptsCount;
            set
            {
                if (_attemptsCount != value)
                {
                    _attemptsCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool InternalTheoryPassed
        {
            get => _internalTheoryPassed;
            set
            {
                if (_internalTheoryPassed != value)
                {
                    _internalTheoryPassed = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool InternalPracticePassed
        {
            get => _internalPracticePassed;
            set
            {
                if (_internalPracticePassed != value)
                {
                    _internalPracticePassed = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusDisplay
        {
            get => _statusDisplay;
            set
            {
                if (_statusDisplay != value)
                {
                    _statusDisplay = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}