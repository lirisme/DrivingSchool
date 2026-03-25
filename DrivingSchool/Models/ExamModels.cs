using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DrivingSchool.Models
{
    /// <summary>
    /// Тип экзамена
    /// </summary>
    public enum ExamType
    {
        [Description("Внутренний экзамен")]
        Internal = 0,

        [Description("Экзамен в ГИБДД")]
        GIBDD = 1
    }

    /// <summary>
    /// Этап экзамена
    /// </summary>
    public enum ExamStage
    {
        [Description("Теория (ПДД)")]
        Theory = 0,

        [Description("Практика (вождение)")]
        Practice = 1
    }

    /// <summary>
    /// Результат экзамена
    /// </summary>
    public enum ExamResult
    {
        [Description("Не сдавался")]
        NotAttempted = 0,

        [Description("Сдан")]
        Passed = 1,

        [Description("Не сдан")]
        Failed = 2,

        [Description("Назначен")]
        Scheduled = 3
    }

    /// <summary>
    /// Запись об экзамене
    /// </summary>
    public class ExamRecord : INotifyPropertyChanged
    {
        private int _id;
        private int _studentId;
        private ExamType _type;
        private ExamStage _stage;
        private DateTime _examDate;
        private ExamResult _result;
        private int _score;
        private int _attemptNumber;
        private string _examinerName;
        private string _notes;
        private List<string> _mistakes;
        private DateTime _createdDate;
        private DateTime? _modifiedDate;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }

        public int StudentId
        {
            get => _studentId;
            set { _studentId = value; OnPropertyChanged(nameof(StudentId)); }
        }

        public ExamType Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(nameof(Type)); OnPropertyChanged(nameof(TypeName)); }
        }

        public ExamStage Stage
        {
            get => _stage;
            set { _stage = value; OnPropertyChanged(nameof(Stage)); OnPropertyChanged(nameof(StageName)); }
        }

        public DateTime ExamDate
        {
            get => _examDate;
            set { _examDate = value; OnPropertyChanged(nameof(ExamDate)); OnPropertyChanged(nameof(ExamDateDisplay)); }
        }

        public ExamResult Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(nameof(Result)); OnPropertyChanged(nameof(ResultDisplay)); OnPropertyChanged(nameof(ResultColor)); }
        }

        public int Score
        {
            get => _score;
            set { _score = value; OnPropertyChanged(nameof(Score)); OnPropertyChanged(nameof(ScoreDisplay)); }
        }

        public int AttemptNumber
        {
            get => _attemptNumber;
            set { _attemptNumber = value; OnPropertyChanged(nameof(AttemptNumber)); }
        }

        public string ExaminerName
        {
            get => _examinerName;
            set { _examinerName = value; OnPropertyChanged(nameof(ExaminerName)); }
        }

        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(nameof(Notes)); }
        }

        public List<string> Mistakes
        {
            get => _mistakes ?? new List<string>();
            set { _mistakes = value; OnPropertyChanged(nameof(Mistakes)); OnPropertyChanged(nameof(MistakesDisplay)); }
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set { _createdDate = value; OnPropertyChanged(nameof(CreatedDate)); }
        }

        public DateTime? ModifiedDate
        {
            get => _modifiedDate;
            set { _modifiedDate = value; OnPropertyChanged(nameof(ModifiedDate)); }
        }

        // Вспомогательные свойства для отображения
        public string TypeName => GetEnumDescription(Type);
        public string StageName => GetEnumDescription(Stage);
        public string ResultDisplay => GetEnumDescription(Result);
        public string ExamDateDisplay => ExamDate.ToString("dd.MM.yyyy HH:mm");
        public string ScoreDisplay => Score > 0 ? $"{Score}%" : "—";
        public string MistakesDisplay => Mistakes.Count > 0 ? string.Join(", ", Mistakes) : "Нет";

        public string ResultColor
        {
            get
            {
                switch (Result)
                {
                    case ExamResult.Passed:
                        return "Green";
                    case ExamResult.Failed:
                        return "Red";
                    case ExamResult.Scheduled:
                        return "Orange";
                    default:
                        return "Gray";
                }
            }
        }

        public string FullDisplay => $"{TypeName} - {StageName} - {ResultDisplay} ({ExamDate:dd.MM.yyyy})";

        private string GetEnumDescription(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
            return attribute?.Length > 0 ? attribute[0].Description : value.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Сводка по экзаменам ученика
    /// </summary>
    public class StudentExamSummary
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string CategoryCode { get; set; }
        public int CompletedLessons { get; set; }
        public int TotalLessons { get; set; }  // ДОБАВЬТЕ ЭТО ПОЛЕ
        public string Phone { get; set; }

        public bool InternalTheoryPassed { get; set; }
        public bool InternalPracticePassed { get; set; }
        public bool GIBDDTheoryPassed { get; set; }
        public bool GIBDDPracticePassed { get; set; }

        public string OverallStatus { get; set; }

        public string LessonsColor => CompletedLessons >= TotalLessons ? "Green" :
                                       CompletedLessons >= TotalLessons / 2 ? "Orange" : "Gray";
    }

    /// <summary>
    /// Расписание экзамена
    /// </summary>
    public class ExamSchedule
        {
            public int Id { get; set; }
            public ExamType Type { get; set; }
            public ExamStage Stage { get; set; }
            public DateTime ExamDate { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            public int MaxStudents { get; set; }
            public int CurrentStudents { get; set; }
            public string ExaminerName { get; set; }
            public string Location { get; set; }
            public bool IsAvailable { get; set; }

            public string DisplayText => $"{GetTypeName()} - {GetStageName()} - {ExamDate:dd.MM.yyyy} ({StartTime:hh\\:mm})";
            public string StatusDisplay => IsAvailable ? $"Свободно {MaxStudents - CurrentStudents} мест" : "Занято";

            private string GetTypeName() => Type == ExamType.Internal ? "Внутренний" : "ГИБДД";
            private string GetStageName() => Stage == ExamStage.Theory ? "Теория" : "Практика";
        }

    public class StudentExamStatus
    {
        public bool InternalTheoryPassed { get; set; }
        public bool InternalPracticePassed { get; set; }
        public bool GIBDDTheoryPassed { get; set; }
        public bool GIBDDPracticePassed { get; set; }
    }
}
