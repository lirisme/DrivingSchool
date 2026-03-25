
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class DrivingLessonsPage : Page
    {
        private readonly SqlDataService _dataService;
        private StudentCollection _students;
        private Student _selectedStudent;
        private DateTime _currentDate = DateTime.Today;
        private List<CalendarDay> _calendarData;
        private Border _selectedSlotBorder;
        private CalendarDay _selectedSlotData;
        private List<string> _currentTimeSlots = new List<string>();
        private List<Employee> _instructors;
        private Employee _selectedInstructor;

        public DrivingLessonsPage(SqlDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            LoadData();
            LoadInstructors();

            // Автоматически отмечаем прошедшие уроки
            _dataService.AutoCompletePastLessons();

            LoadCalendar();
            MonthYearText.Text = _currentDate.ToString("MMMM yyyy", CultureInfo.CurrentCulture);
        }

        private void LoadData()
        {
            try
            {
                _students = _dataService.LoadStudents() ?? new StudentCollection { Students = new List<Student>() };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка");
            }
        }

        private void LoadInstructors()
        {
            try
            {
                _instructors = _dataService.GetInstructorsList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки инструкторов: {ex.Message}", "Ошибка");
                _instructors = new List<Employee>();
            }
        }

        private void LoadCalendar()
        {
            if (_selectedStudent == null)
            {
                CalendarGrid.ItemsSource = null;
                return;
            }

            if (_selectedStudent.InstructorId == 0 || _selectedStudent.CarId == 0)
            {
                MessageBox.Show("У студента не назначен инструктор или автомобиль!\nОбратитесь к администратору.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Автоматически отмечаем прошедшие уроки
                _dataService.AutoCompletePastLessons();

                _dataService.EnsureSlotsExist(_currentDate, _selectedStudent.InstructorId, _selectedStudent.CarId);

                var firstDayOfMonth = new DateTime(_currentDate.Year, _currentDate.Month, 1);
                int daysToSubtract = ((int)firstDayOfMonth.DayOfWeek == 0 ? 6 : (int)firstDayOfMonth.DayOfWeek - 1);
                var startDate = firstDayOfMonth.AddDays(-daysToSubtract);
                var endDate = startDate.AddDays(41);

                // Получаем реальные временные слоты из базы
                var existingSlots = _dataService.GetInstructorTimeSlots(_selectedStudent.InstructorId);
                var timeSlots = new List<dynamic>();

                foreach (var slot in existingSlots)
                {
                    var parts = slot.Split(new[] { " - " }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        int hour = int.Parse(parts[0].Split(':')[0]);
                        timeSlots.Add(new { Time = parts[0], Start = hour });
                    }
                }

                // Если слотов нет, добавляем стандартные
                if (timeSlots.Count == 0)
                {
                    timeSlots.Add(new { Time = "09:00", Start = 9 });
                    timeSlots.Add(new { Time = "11:00", Start = 11 });
                    timeSlots.Add(new { Time = "13:00", Start = 13 });
                    timeSlots.Add(new { Time = "15:00", Start = 15 });
                    timeSlots.Add(new { Time = "17:00", Start = 17 });
                }

                // Обновляем панель временных слотов
                var timeSlotsDisplay = existingSlots.Count > 0 ? existingSlots : new List<string>
                {
                    "09:00 - 11:00", "11:00 - 13:00", "13:00 - 15:00", "15:00 - 17:00", "17:00 - 19:00"
                };
                TimeSlotsPanel.ItemsSource = timeSlotsDisplay;
                _currentTimeSlots = timeSlotsDisplay.ToList();

                var allSlots = _dataService.LoadAllSlots(
                    _selectedStudent.InstructorId,
                    _selectedStudent.CarId,
                    startDate,
                    endDate);

                var lessons = _dataService.LoadStudentLessons(_selectedStudent.Id);

                var calendar = new List<CalendarRow>();

                foreach (var ts in timeSlots)
                {
                    var row = new CalendarRow { TimeSlot = ts.Time };
                    var days = new List<CalendarDay>();

                    for (int i = 0; i < 42; i++)
                    {
                        var date = startDate.AddDays(i);
                        bool isCurrentMonth = date.Month == _currentDate.Month;

                        var slot = allSlots.Slots.FirstOrDefault(s =>
                            s.LessonDate.Date == date.Date &&
                            s.StartTime.Hours == ts.Start);

                        var studentLesson = lessons.FirstOrDefault(l =>
                            l.LessonDate.Date == date.Date &&
                            l.StartTime.Hours == ts.Start);

                        bool isSlotExists = slot != null;
                        bool isSlotAvailable = slot?.IsAvailable ?? false;
                        bool hasBooking = studentLesson != null;

                        string status;
                        Brush statusColor;
                        bool canBook = false;
                        int slotId = slot?.Id ?? 0;
                        int lessonId = studentLesson?.Id ?? 0;
                        bool isExtra = studentLesson?.IsExtra ?? false;

                        if (hasBooking)
                        {
                            switch (studentLesson.Status)
                            {
                                case "Booked":
                                    status = isExtra ? "Доп. урок" : "Забронирован";
                                    statusColor = Brushes.Orange;
                                    canBook = false;
                                    break;
                                case "Completed":
                                    status = isExtra ? "Доп. проведен" : "Проведен";
                                    statusColor = Brushes.Green;
                                    canBook = false;
                                    break;
                                case "NoShow":
                                    status = isExtra ? "Доп. прогул" : "Прогул";
                                    statusColor = Brushes.Red;
                                    canBook = false;
                                    break;
                                case "Cancelled":
                                    status = isExtra ? "Доп. отменен" : "Отменен";
                                    statusColor = Brushes.Gray;
                                    canBook = isSlotAvailable;
                                    break;
                                default:
                                    status = isExtra ? "Доп. урок" : "Забронирован";
                                    statusColor = Brushes.Orange;
                                    canBook = false;
                                    break;
                            }
                        }
                        else if (isSlotExists && isSlotAvailable)
                        {
                            status = "Свободно";
                            statusColor = Brushes.Green;
                            canBook = true;
                        }
                        else if (isSlotExists && !isSlotAvailable)
                        {
                            status = "Занято";
                            statusColor = Brushes.LightCoral;
                            canBook = false;
                        }
                        else
                        {
                            status = "Нет слота";
                            statusColor = Brushes.LightGray;
                            canBook = false;
                        }

                        var dayCell = new CalendarDay
                        {
                            Date = date.Day.ToString(),
                            FullDate = date,
                            SlotId = slotId,
                            LessonId = lessonId,
                            IsAvailable = canBook,
                            HasBooking = hasBooking && studentLesson?.Status != "Cancelled",
                            Status = status,
                            StatusColor = statusColor,
                            IsCurrentMonth = isCurrentMonth,
                            IsToday = date.Date == DateTime.Today.Date,
                            CanBook = canBook,
                            HasActiveBooking = hasBooking && studentLesson?.Status == "Booked",
                            IsExtra = isExtra,
                            IsSunday = date.DayOfWeek == DayOfWeek.Sunday
                        };

                        days.Add(dayCell);
                    }

                    row.Days = days;
                    calendar.Add(row);
                }

                _calendarData = calendar.SelectMany(r => r.Days).ToList();
                CalendarGrid.ItemsSource = calendar;
                UpdateStudentInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки календаря: {ex.Message}", "Ошибка");
            }
        }

        private void UpdateStudentInfo()
        {
            if (_selectedStudent == null) return;

            var lessons = _dataService.LoadStudentLessons(_selectedStudent.Id);
            var totalLessons = _dataService.GetLessonsCountByCategory(_selectedStudent.Id);
            var completed = lessons.Count(l => l.Status == "Completed");
            var noShow = lessons.Count(l => l.Status == "NoShow");
            var booked = lessons.Count(l => l.Status == "Booked");
            var extra = lessons.Count(l => l.IsExtra);
            var left = totalLessons - completed - noShow;

            LessonsStatsText.Text = $"Категория: {_selectedStudent.CategoryName ?? "Не указана"} | " +
                                   $"Всего уроков: {totalLessons} | " +
                                   $"Проведено: {completed} | " +
                                   $"Прогулы: {noShow} | " +
                                   $"Осталось: {left} | " +
                                   $"Забронировано: {booked} | " +
                                   $"Доп. уроки: {extra}";
        }

        private void BookSlot(CalendarDay day)
        {
            if (_selectedStudent == null)
            {
                MessageBox.Show("Сначала выберите студента", "Предупреждение");
                return;
            }

            try
            {
                CheckAndHandlePayments();
                int totalLessons = _dataService.GetLessonsCountByCategory(_selectedStudent.Id);
                var lessons = _dataService.LoadStudentLessons(_selectedStudent.Id);
                var completed = lessons.Count(l => l.Status == "Completed");
                var noShow = lessons.Count(l => l.Status == "NoShow");
                var booked = lessons.Count(l => l.Status == "Booked");

                int used = completed + noShow;
                int remaining = totalLessons - used;

                if (remaining > 0 && booked < remaining)
                {
                    var result = MessageBox.Show(
                        $"Забронировать урок на {day.FullDate:dd.MM.yyyy}?\n\n" +
                        $"Осталось уроков по категории: {remaining}",
                        "Подтверждение",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        _dataService.BookLesson(_selectedStudent.Id, day.SlotId);
                        MessageBox.Show("Урок успешно забронирован!", "Успех");
                        LoadCalendar();
                    }
                }
                else
                {
                    var result = MessageBox.Show(
                        $"У студента закончились уроки по категории '{_selectedStudent.CategoryName}' (всего {totalLessons} уроков).\n\n" +
                        $"Проведено: {completed}, Прогулов: {noShow}\n\n" +
                        $"Забронировать как ДОПОЛНИТЕЛЬНЫЙ урок?",
                        "Внимание",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        _dataService.BookExtraLesson(_selectedStudent.Id, day.SlotId);
                        MessageBox.Show("Дополнительный урок забронирован!", "Успех");
                        LoadCalendar();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        private void ResetLesson_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSlotData == null) return;

            var result = MessageBox.Show(
                $"Удалить отметку с урока на {_selectedSlotData.FullDate:dd.MM.yyyy}?\n\n" +
                $"Текущий статус: {_selectedSlotData.Status}\n\n" +
                "Слот станет свободным, урок будет удален из расписания.",
                "Исправление ошибки",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dataService.ResetLessonStatus(_selectedSlotData.LessonId);
                    MessageBox.Show("Отметка удалена, слот стал свободным!", "Успех");
                    LoadCalendar();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
                }
            }
        }

        private void CancelBooking(CalendarDay day)
        {
            var result = MessageBox.Show(
                $"Отменить бронь на {day.FullDate:dd.MM.yyyy}?\n\n" +
                "Слот станет свободным, урок не будет списан.",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dataService.CancelLessonOnly(day.LessonId);
                    MessageBox.Show("Бронь отменена", "Успех");
                    LoadCalendar();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
                }
            }
        }

        private void MarkAsCompleted(CalendarDay day)
        {
            var result = MessageBox.Show($"Отметить урок на {day.FullDate:dd.MM.yyyy} как ПРОВЕДЕННЫЙ?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dataService.MarkLessonAsCompleted(day.LessonId);
                    MessageBox.Show("Урок отмечен как проведенный", "Успех");
                    LoadCalendar();

                    // Проверяем платежи ПОСЛЕ отметки урока
                    CheckAndHandlePayments();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
                }
            }
        }

        private void MarkAsNoShow(CalendarDay day)
        {
            var result = MessageBox.Show($"Отметить урок на {day.FullDate:dd.MM.yyyy} как ПРОГУЛ?\n\n" +
                "Урок будет списан из общего количества!",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dataService.MarkLessonAsNoShow(day.LessonId);
                    MessageBox.Show("Урок отмечен как прогул", "Успех");
                    LoadCalendar();

                    // Проверяем платежи ПОСЛЕ отметки прогула
                    CheckAndHandlePayments();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
                }
            }
        }

        private void StudentSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = StudentSearchBox.Text?.ToLower() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                StudentResultsList.Visibility = Visibility.Collapsed;
                return;
            }

            var results = _students.Students
                .Where(s => s.LastName.ToLower().Contains(searchText) ||
                           s.FirstName.ToLower().Contains(searchText) ||
                           s.Phone.Contains(searchText))
                .Take(10)
                .ToList();

            if (results.Any())
            {
                StudentResultsList.ItemsSource = results;
                StudentResultsList.Visibility = Visibility.Visible;
            }
            else
            {
                StudentResultsList.Visibility = Visibility.Collapsed;
            }
        }

        private void StudentResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StudentResultsList.SelectedItem is Student selected)
            {
                _selectedStudent = _dataService.LoadStudent(selected.Id);
                _selectedInstructor = null;
                SelectedStudentPanel.Visibility = Visibility.Visible;
                SelectedStudentName.Text = _selectedStudent.FullName;
                StudentDetails.Text = $"Инструктор: {_selectedStudent.InstructorName ?? "не назначен"} | Авто: {_selectedStudent.CarInfo ?? "не назначено"}";
                StudentResultsList.Visibility = Visibility.Collapsed;
                StudentSearchBox.Text = "";
                SelectedInstructorPanel.Visibility = Visibility.Collapsed;

                LoadCalendar();
                Dispatcher.BeginInvoke(new Action(() => ScrollToToday()), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        private void ClearStudent_Click(object sender, RoutedEventArgs e)
        {
            _selectedStudent = null;
            SelectedStudentPanel.Visibility = Visibility.Collapsed;
            if (_selectedInstructor == null)
            {
                CalendarGrid.ItemsSource = null;
                LessonsStatsText.Text = "";
            }
            else
            {
                LoadInstructorCalendar();
            }
        }

        private void Slot_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.Tag == null) return;

            var slotId = (int)border.Tag;
            var day = _calendarData.FirstOrDefault(d => d.SlotId == slotId);

            if (day == null) return;

            if (day.CanBook)
            {
                BookSlot(day);
            }
            else if (day.HasBooking)
            {
                ShowLessonActions(day);
            }
        }

        private void Slot_RightClick(object sender, MouseButtonEventArgs e)
        {
            _selectedSlotBorder = sender as Border;
            if (_selectedSlotBorder?.Tag != null)
            {
                var slotId = (int)_selectedSlotBorder.Tag;
                _selectedSlotData = _calendarData.FirstOrDefault(d => d.SlotId == slotId);
            }
            e.Handled = true;
        }

        private void BookLesson_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSlotData != null && _selectedSlotData.CanBook)
            {
                BookSlot(_selectedSlotData);
            }
        }

        private void CancelBooking_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSlotData != null && _selectedSlotData.HasActiveBooking)
            {
                CancelBooking(_selectedSlotData);
            }
        }

        private void MarkAsCompleted_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSlotData != null && _selectedSlotData.HasBooking)
            {
                MarkAsCompleted(_selectedSlotData);
            }
        }

        private void MarkAsNoShow_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSlotData != null && _selectedSlotData.HasBooking)
            {
                MarkAsNoShow(_selectedSlotData);
            }
        }

        private void ManualBooking_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStudent == null)
            {
                MessageBox.Show("Сначала выберите студента", "Предупреждение");
                return;
            }

            var datePicker = new DatePicker { SelectedDate = DateTime.Today };
            var timeCombo = new ComboBox();

            var existingSlots = _dataService.GetInstructorTimeSlots(_selectedStudent.InstructorId);
            if (existingSlots.Count > 0)
            {
                foreach (var slot in existingSlots)
                {
                    var parts = slot.Split(new[] { " - " }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        timeCombo.Items.Add(parts[0]);
                    }
                }
            }
            else
            {
                timeCombo.Items.Add("09:00");
                timeCombo.Items.Add("11:00");
                timeCombo.Items.Add("13:00");
                timeCombo.Items.Add("15:00");
                timeCombo.Items.Add("17:00");
            }
            timeCombo.SelectedIndex = 0;

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = "Выберите дату:", Margin = new Thickness(5) });
            stack.Children.Add(datePicker);
            stack.Children.Add(new TextBlock { Text = "Выберите время:", Margin = new Thickness(5) });
            stack.Children.Add(timeCombo);

            var window = new Window
            {
                Title = "Ручное бронирование",
                Content = stack,
                Width = 300,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var okButton = new Button { Content = "ОК", Width = 80, Margin = new Thickness(5) };
            var cancelButton = new Button { Content = "Отмена", Width = 80, Margin = new Thickness(5) };
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            stack.Children.Add(buttonPanel);

            bool result = false;
            okButton.Click += (s, args) => { result = true; window.Close(); };
            cancelButton.Click += (s, args) => window.Close();

            window.ShowDialog();

            if (result && datePicker.SelectedDate.HasValue)
            {
                var selectedDate = datePicker.SelectedDate.Value;
                var selectedTime = timeCombo.SelectedItem.ToString();
                int hour = int.Parse(selectedTime.Split(':')[0]);

                try
                {
                    _dataService.ManualBookLesson(
                        _selectedStudent.Id,
                        _selectedStudent.InstructorId,
                        _selectedStudent.CarId,
                        selectedDate,
                        new TimeSpan(hour, 0, 0),
                        new TimeSpan(hour + 2, 0, 0));

                    MessageBox.Show("Урок успешно забронирован!", "Успех");
                    LoadCalendar();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
                }
            }
        }

        private void ManualBookingForSlot_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStudent == null)
            {
                MessageBox.Show("Сначала выберите студента", "Предупреждение");
                return;
            }

            if (_selectedSlotData == null) return;

            var timeCombo = new ComboBox();

            var existingSlots = _dataService.GetInstructorTimeSlots(_selectedStudent.InstructorId);
            if (existingSlots.Count > 0)
            {
                foreach (var slot in existingSlots)
                {
                    var parts = slot.Split(new[] { " - " }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        timeCombo.Items.Add(parts[0]);
                    }
                }
            }
            else
            {
                timeCombo.Items.Add("09:00");
                timeCombo.Items.Add("11:00");
                timeCombo.Items.Add("13:00");
                timeCombo.Items.Add("15:00");
                timeCombo.Items.Add("17:00");
            }
            timeCombo.SelectedIndex = 0;

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = $"Дата: {_selectedSlotData.FullDate:dd.MM.yyyy}", Margin = new Thickness(5) });
            stack.Children.Add(new TextBlock { Text = "Выберите время:", Margin = new Thickness(5) });
            stack.Children.Add(timeCombo);

            var window = new Window
            {
                Title = "Ручное бронирование",
                Content = stack,
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var okButton = new Button { Content = "ОК", Width = 80, Margin = new Thickness(5) };
            var cancelButton = new Button { Content = "Отмена", Width = 80, Margin = new Thickness(5) };
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            stack.Children.Add(buttonPanel);

            bool result = false;
            okButton.Click += (s, args) => { result = true; window.Close(); };
            cancelButton.Click += (s, args) => window.Close();

            window.ShowDialog();

            if (result)
            {
                var selectedTime = timeCombo.SelectedItem.ToString();
                int hour = int.Parse(selectedTime.Split(':')[0]);

                try
                {
                    _dataService.ManualBookLesson(
                        _selectedStudent.Id,
                        _selectedStudent.InstructorId,
                        _selectedStudent.CarId,
                        _selectedSlotData.FullDate,
                        new TimeSpan(hour, 0, 0),
                        new TimeSpan(hour + 2, 0, 0));

                    MessageBox.Show("Урок успешно забронирован!", "Успех");
                    LoadCalendar();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
                }
            }
        }

        private void ShowLessonActions(CalendarDay day)
        {
            var actions = new List<string>();

            if (day.Status == "Проведен" || day.Status == "Доп. проведен" ||
                day.Status == "Прогул" || day.Status == "Доп. прогул")
            {
                actions.Add("Исправить (снять отметку)");
            }
            else if (day.HasActiveBooking)
            {
                actions.Add("Отметить как проведенный");
                actions.Add("Отметить как прогул");
                actions.Add("Отменить бронь");
            }

            if (actions.Count == 0) return;

            var combo = new ComboBox();
            foreach (var action in actions)
            {
                combo.Items.Add(action);
            }
            combo.SelectedIndex = 0;

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = $"Урок на {day.FullDate:dd.MM.yyyy}", Margin = new Thickness(5), FontWeight = FontWeights.Bold });
            stack.Children.Add(new TextBlock { Text = "Выберите действие:", Margin = new Thickness(5) });
            stack.Children.Add(combo);

            var window = new Window
            {
                Title = "Управление уроком",
                Content = stack,
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var okButton = new Button { Content = "ОК", Width = 80, Margin = new Thickness(5) };
            var cancelButton = new Button { Content = "Отмена", Width = 80, Margin = new Thickness(5) };
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            stack.Children.Add(buttonPanel);

            bool result = false;
            okButton.Click += (s, args) => { result = true; window.Close(); };
            cancelButton.Click += (s, args) => window.Close();

            window.ShowDialog();

            if (result)
            {
                string selectedAction = combo.SelectedItem.ToString();

                if (selectedAction.Contains("Исправить"))
                {
                    _selectedSlotData = day;
                    ResetLesson_Click(this, new RoutedEventArgs());
                }
                else if (selectedAction.Contains("проведенный"))
                {
                    MarkAsCompleted(day);
                }
                else if (selectedAction.Contains("прогул"))
                {
                    MarkAsNoShow(day);
                }
                else if (selectedAction.Contains("Отменить"))
                {
                    CancelBooking(day);
                }
            }
        }

        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _currentDate.AddMonths(-1);
            MonthYearText.Text = _currentDate.ToString("MMMM yyyy", CultureInfo.CurrentCulture);
            if (_selectedStudent != null)
                LoadCalendar();
            else if (_selectedInstructor != null)
                LoadInstructorCalendar();
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _currentDate.AddMonths(1);
            MonthYearText.Text = _currentDate.ToString("MMMM yyyy", CultureInfo.CurrentCulture);
            if (_selectedStudent != null)
                LoadCalendar();
            else if (_selectedInstructor != null)
                LoadInstructorCalendar();
        }

        private void Today_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = DateTime.Today;
            MonthYearText.Text = _currentDate.ToString("MMMM yyyy", CultureInfo.CurrentCulture);
            if (_selectedStudent != null)
                LoadCalendar();
            else if (_selectedInstructor != null)
                LoadInstructorCalendar();
            ScrollToToday();
        }

        private void ScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            ScrollToToday();
        }

        private void ScrollToToday()
        {
            try
            {
                if (MainScrollViewer != null && _calendarData != null)
                {
                    var today = DateTime.Today;

                    if (CalendarGrid.ItemsSource is List<CalendarRow> rows && rows.Any())
                    {
                        var firstRow = rows.FirstOrDefault();
                        if (firstRow?.Days != null)
                        {
                            int todayIndex = firstRow.Days.FindIndex(d => d.FullDate.Date == today.Date);

                            if (todayIndex >= 0)
                            {
                                double scrollPosition = todayIndex * 104;
                                MainScrollViewer.ScrollToHorizontalOffset(Math.Max(0, scrollPosition - MainScrollViewer.ActualWidth / 2 + 50));

                                var now = DateTime.Now;
                                int timeSlotIndex = -1;

                                if (now.Hour >= 9 && now.Hour < 11) timeSlotIndex = 0;
                                else if (now.Hour >= 11 && now.Hour < 13) timeSlotIndex = 1;
                                else if (now.Hour >= 13 && now.Hour < 15) timeSlotIndex = 2;
                                else if (now.Hour >= 15 && now.Hour < 17) timeSlotIndex = 3;
                                else if (now.Hour >= 17 && now.Hour < 19) timeSlotIndex = 4;

                                if (timeSlotIndex >= 0)
                                {
                                    double verticalPosition = timeSlotIndex * 80;
                                    MainScrollViewer.ScrollToVerticalOffset(Math.Max(0, verticalPosition - 100));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка прокрутки: {ex.Message}");
            }
        }

        private void MainScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                var headerScrollViewer = FindVisualChild<ScrollViewer>(this, "DaysHeaderScrollViewer");
                if (headerScrollViewer != null && headerScrollViewer != scrollViewer)
                {
                    headerScrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset);
                }
            }
        }

        private T FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T && ((T)child).Name == name)
                    return (T)child;

                var result = FindVisualChild<T>(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void ManageTimeSlots_Click(object sender, RoutedEventArgs e)
        {
            int instructorId;
            int carId;
            string instructorName;

            if (_selectedStudent != null && _selectedStudent.InstructorId > 0)
            {
                instructorId = _selectedStudent.InstructorId;
                carId = _selectedStudent.CarId;
                instructorName = _selectedStudent.InstructorName ?? "Инструктор";
                ShowTimeSlotsEditor(instructorId, instructorName, carId);
                return;
            }

            var instructors = _dataService.GetInstructors();
            if (instructors.Count == 0)
            {
                MessageBox.Show("В базе нет инструкторов", "Ошибка");
                return;
            }

            var listBox = new ListBox { Height = 150, Margin = new Thickness(5) };
            foreach (var inst in instructors)
            {
                listBox.Items.Add(inst.Name);
            }
            listBox.SelectedIndex = 0;

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = "Выберите инструктора:", Margin = new Thickness(5), FontWeight = FontWeights.Bold });
            stack.Children.Add(listBox);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(5) };
            var okBtn = new Button { Content = "ОК", Width = 70, Margin = new Thickness(5) };
            var cancelBtn = new Button { Content = "Отмена", Width = 70, Margin = new Thickness(5) };
            buttonPanel.Children.Add(okBtn);
            buttonPanel.Children.Add(cancelBtn);
            stack.Children.Add(buttonPanel);

            var selectWindow = new Window
            {
                Title = "Выбор инструктора",
                Content = stack,
                Width = 350,
                Height = 280,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            bool selected = false;
            okBtn.Click += (s, args) => { selected = true; selectWindow.Close(); };
            cancelBtn.Click += (s, args) => selectWindow.Close();

            selectWindow.ShowDialog();

            if (!selected || listBox.SelectedIndex < 0) return;

            var selectedInstructor = instructors[listBox.SelectedIndex];
            instructorId = selectedInstructor.Id;
            carId = selectedInstructor.CarId;
            instructorName = selectedInstructor.Name;

            ShowTimeSlotsEditor(instructorId, instructorName, carId);
        }

        private void ShowTimeSlotsEditor(int instructorId, string instructorName, int carId)
        {
            var window = new Window
            {
                Title = $"Управление слотами - {instructorName}",
                Width = 350,
                Height = 380,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize
            };

            var mainPanel = new StackPanel { Margin = new Thickness(10) };

            var listBox = new ListBox { Height = 180, Margin = new Thickness(0, 0, 0, 10) };
            var currentSlots = _dataService.GetInstructorTimeSlots(instructorId);
            foreach (var slot in currentSlots)
            {
                listBox.Items.Add(slot);
            }
            mainPanel.Children.Add(listBox);

            var addPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };

            var startCombo = new ComboBox { Width = 55 };
            var endCombo = new ComboBox { Width = 55 };

            for (int i = 6; i <= 22; i++)
            {
                startCombo.Items.Add($"{i:00}:00");
                endCombo.Items.Add($"{i:00}:00");
            }
            startCombo.SelectedIndex = 0;
            endCombo.SelectedIndex = 2;

            addPanel.Children.Add(new TextBlock { Text = "Начало:", Margin = new Thickness(0, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center });
            addPanel.Children.Add(startCombo);
            addPanel.Children.Add(new TextBlock { Text = "Конец:", Margin = new Thickness(10, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center });
            addPanel.Children.Add(endCombo);

            var addBtn = new Button { Content = "Добавить", Width = 70, Margin = new Thickness(10, 0, 0, 0) };
            addPanel.Children.Add(addBtn);
            mainPanel.Children.Add(addPanel);

            var delBtn = new Button { Content = "Удалить выбранный", Height = 30, Margin = new Thickness(0, 5, 0, 0) };
            mainPanel.Children.Add(delBtn);

            window.Content = mainPanel;

            addBtn.Click += (s, args) =>
            {
                string start = startCombo.SelectedItem.ToString();
                string end = endCombo.SelectedItem.ToString();

                if (start == end)
                {
                    MessageBox.Show("Начало и конец не могут совпадать", "Ошибка");
                    return;
                }

                string newSlot = $"{start} - {end}";
                if (listBox.Items.Contains(newSlot))
                {
                    MessageBox.Show("Такой слот уже существует", "Ошибка");
                    return;
                }

                try
                {
                    _dataService.AddTimeSlot(instructorId, carId, start, end);
                    listBox.Items.Add(newSlot);
                    MessageBox.Show("Слот добавлен", "Успех");
                    if (_selectedStudent != null && _selectedStudent.InstructorId == instructorId)
                        LoadCalendar();
                    else if (_selectedInstructor != null && _selectedInstructor.Id == instructorId)
                        LoadInstructorCalendar();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
                }
            };

            delBtn.Click += (s, args) =>
            {
                if (listBox.SelectedItem == null)
                {
                    MessageBox.Show("Выберите слот для удаления", "Предупреждение");
                    return;
                }

                string slot = listBox.SelectedItem.ToString();
                var parts = slot.Split(new[] { " - " }, StringSplitOptions.None);

                var result = MessageBox.Show($"Удалить слот {slot}?\nВсе бронирования на это время будут удалены!",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _dataService.DeleteTimeSlot(instructorId, carId, parts[0], parts[1]);
                        listBox.Items.Remove(slot);
                        MessageBox.Show("Слот удален", "Успех");
                        if (_selectedStudent != null && _selectedStudent.InstructorId == instructorId)
                            LoadCalendar();
                        else if (_selectedInstructor != null && _selectedInstructor.Id == instructorId)
                            LoadInstructorCalendar();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
                    }
                }
            };

            window.ShowDialog();
        }

        private void TimeSlot_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.Tag == null) return;

            string timeSlot = border.Tag.ToString();
            var parts = timeSlot.Split(new[] { " - " }, StringSplitOptions.None);
            if (parts.Length != 2) return;

            string startTime = parts[0];
            string endTime = parts[1];

            var actions = new List<string> { "Изменить время", "Удалить слот" };
            var combo = new ComboBox();
            foreach (var action in actions) combo.Items.Add(action);
            combo.SelectedIndex = 0;

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = $"Слот: {timeSlot}", Margin = new Thickness(5), FontWeight = FontWeights.Bold });
            stack.Children.Add(new TextBlock { Text = "Выберите действие:", Margin = new Thickness(5) });
            stack.Children.Add(combo);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(5) };
            var okBtn = new Button { Content = "ОК", Width = 70, Margin = new Thickness(5) };
            var cancelBtn = new Button { Content = "Отмена", Width = 70, Margin = new Thickness(5) };
            buttonPanel.Children.Add(okBtn);
            buttonPanel.Children.Add(cancelBtn);
            stack.Children.Add(buttonPanel);

            var window = new Window
            {
                Title = "Управление слотом",
                Content = stack,
                Width = 280,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            bool selected = false;
            okBtn.Click += (s, args) => { selected = true; window.Close(); };
            cancelBtn.Click += (s, args) => window.Close();

            window.ShowDialog();

            if (!selected) return;

            string selectedAction = combo.SelectedItem.ToString();

            int instructorId;
            int carId;

            if (_selectedStudent != null && _selectedStudent.InstructorId > 0)
            {
                instructorId = _selectedStudent.InstructorId;
                carId = _selectedStudent.CarId;
            }
            else if (_selectedInstructor != null)
            {
                instructorId = _selectedInstructor.Id;
                carId = _selectedInstructor.CarId;
            }
            else
            {
                MessageBox.Show("Сначала выберите студента или инструктора", "Предупреждение");
                return;
            }

            if (selectedAction == "Удалить слот")
            {
                var result = MessageBox.Show($"Удалить слот {timeSlot}?\nВсе бронирования на это время будут удалены!",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _dataService.DeleteTimeSlot(instructorId, carId, startTime, endTime);
                        if (_selectedStudent != null)
                            LoadCalendar();
                        else if (_selectedInstructor != null)
                            LoadInstructorCalendar();
                        MessageBox.Show("Слот удален", "Успех");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
                    }
                }
            }
            else if (selectedAction == "Изменить время")
            {
                EditTimeSlot(instructorId, carId, startTime, endTime);
            }
        }

        private void AddTimeSlot_Click(object sender, RoutedEventArgs e)
        {
            int instructorId;
            int carId;

            if (_selectedStudent != null && _selectedStudent.InstructorId > 0)
            {
                instructorId = _selectedStudent.InstructorId;
                carId = _selectedStudent.CarId;
            }
            else if (_selectedInstructor != null)
            {
                instructorId = _selectedInstructor.Id;
                carId = _selectedInstructor.CarId;
            }
            else
            {
                MessageBox.Show("Сначала выберите студента или инструктора", "Предупреждение");
                return;
            }

            var startTextBox = new TextBox { Width = 70, Text = "09:00", FontSize = 14, TextAlignment = TextAlignment.Center };
            var endTextBox = new TextBox { Width = 70, Text = "11:00", FontSize = 14, TextAlignment = TextAlignment.Center };

            startTextBox.ToolTip = "Введите время в формате ЧЧ:ММ (например, 08:00, 09:30)";
            endTextBox.ToolTip = "Введите время в формате ЧЧ:ММ (например, 10:00, 12:30)";

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = "Добавление нового временного слота:", Margin = new Thickness(5), FontWeight = FontWeights.Bold });
            stack.Children.Add(new TextBlock { Text = "Формат: ЧЧ:ММ (например, 08:00, 09:30)", Margin = new Thickness(5), FontSize = 10, Foreground = Brushes.Gray });

            var timePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Center };
            timePanel.Children.Add(new TextBlock { Text = "Начало:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) });
            timePanel.Children.Add(startTextBox);
            timePanel.Children.Add(new TextBlock { Text = "Конец:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 5, 0) });
            timePanel.Children.Add(endTextBox);
            stack.Children.Add(timePanel);

            var quickButtons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Center };
            var quickTimes = new[] { "08:00-10:00", "09:00-11:00", "10:00-12:00", "11:00-13:00", "13:00-15:00", "15:00-17:00", "17:00-19:00", "18:00-20:00" };

            foreach (var qt in quickTimes)
            {
                var parts = qt.Split('-');
                var btn = new Button { Content = qt, Width = 80, Height = 25, Margin = new Thickness(2) };
                btn.Click += (s, args) =>
                {
                    startTextBox.Text = parts[0];
                    endTextBox.Text = parts[1];
                };
                quickButtons.Children.Add(btn);
            }

            stack.Children.Add(new TextBlock { Text = "Быстрый выбор:", Margin = new Thickness(5), FontSize = 10 });
            stack.Children.Add(quickButtons);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(5) };
            var okBtn = new Button { Content = "Добавить", Width = 80, Margin = new Thickness(5), Background = Brushes.LightGreen };
            var cancelBtn = new Button { Content = "Отмена", Width = 80, Margin = new Thickness(5) };
            buttonPanel.Children.Add(okBtn);
            buttonPanel.Children.Add(cancelBtn);
            stack.Children.Add(buttonPanel);

            var window = new Window
            {
                Title = "Добавление слота",
                Content = stack,
                Width = 450,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            bool added = false;
            okBtn.Click += (s, args) =>
            {
                added = true;
                window.Close();
            };
            cancelBtn.Click += (s, args) => window.Close();

            window.ShowDialog();

            if (!added) return;

            string start = startTextBox.Text.Trim();
            string end = endTextBox.Text.Trim();

            if (!IsValidTime(start) || !IsValidTime(end))
            {
                MessageBox.Show("Неверный формат времени. Используйте ЧЧ:ММ (например, 09:00)", "Ошибка");
                return;
            }

            if (start == end)
            {
                MessageBox.Show("Начало и конец не могут совпадать", "Ошибка");
                return;
            }

            var startTime = TimeSpan.Parse(start);
            var endTime = TimeSpan.Parse(end);

            if (startTime >= endTime)
            {
                MessageBox.Show("Время начала должно быть меньше времени окончания", "Ошибка");
                return;
            }

            string newSlot = $"{start} - {end}";
            if (_currentTimeSlots.Contains(newSlot))
            {
                MessageBox.Show("Такой слот уже существует", "Ошибка");
                return;
            }

            try
            {
                _dataService.AddTimeSlot(instructorId, carId, start, end);
                if (_selectedStudent != null)
                    LoadCalendar();
                else if (_selectedInstructor != null)
                    LoadInstructorCalendar();
                MessageBox.Show("Слот добавлен", "Успех");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        private void EditTimeSlot(int instructorId, int carId, string oldStart, string oldEnd)
        {
            var startTextBox = new TextBox { Width = 70, Text = oldStart, FontSize = 14, TextAlignment = TextAlignment.Center };
            var endTextBox = new TextBox { Width = 70, Text = oldEnd, FontSize = 14, TextAlignment = TextAlignment.Center };

            startTextBox.ToolTip = "Введите время в формате ЧЧ:ММ (например, 08:00, 09:30)";
            endTextBox.ToolTip = "Введите время в формате ЧЧ:ММ (например, 10:00, 12:30)";

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = $"Редактирование слота:", Margin = new Thickness(5), FontWeight = FontWeights.Bold });
            stack.Children.Add(new TextBlock { Text = "Формат: ЧЧ:ММ (например, 08:00, 09:30)", Margin = new Thickness(5), FontSize = 10, Foreground = Brushes.Gray });

            var timePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Center };
            timePanel.Children.Add(new TextBlock { Text = "Начало:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) });
            timePanel.Children.Add(startTextBox);
            timePanel.Children.Add(new TextBlock { Text = "Конец:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 5, 0) });
            timePanel.Children.Add(endTextBox);
            stack.Children.Add(timePanel);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(5) };
            var okBtn = new Button { Content = "Сохранить", Width = 80, Margin = new Thickness(5), Background = Brushes.LightGreen };
            var cancelBtn = new Button { Content = "Отмена", Width = 80, Margin = new Thickness(5) };
            buttonPanel.Children.Add(okBtn);
            buttonPanel.Children.Add(cancelBtn);
            stack.Children.Add(buttonPanel);

            var window = new Window
            {
                Title = "Изменение времени",
                Content = stack,
                Width = 350,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            bool saved = false;
            okBtn.Click += (s, args) => { saved = true; window.Close(); };
            cancelBtn.Click += (s, args) => window.Close();

            window.ShowDialog();

            if (!saved) return;

            string newStart = startTextBox.Text.Trim();
            string newEnd = endTextBox.Text.Trim();

            if (!IsValidTime(newStart) || !IsValidTime(newEnd))
            {
                MessageBox.Show("Неверный формат времени. Используйте ЧЧ:ММ (например, 09:00)", "Ошибка");
                return;
            }

            if (newStart == newEnd)
            {
                MessageBox.Show("Начало и конец не могут совпадать", "Ошибка");
                return;
            }

            var newStartTime = TimeSpan.Parse(newStart);
            var newEndTime = TimeSpan.Parse(newEnd);

            if (newStartTime >= newEndTime)
            {
                MessageBox.Show("Время начала должно быть меньше времени окончания", "Ошибка");
                return;
            }

            try
            {
                _dataService.DeleteTimeSlot(instructorId, carId, oldStart, oldEnd);
                _dataService.AddTimeSlot(instructorId, carId, newStart, newEnd);
                if (_selectedStudent != null)
                    LoadCalendar();
                else if (_selectedInstructor != null)
                    LoadInstructorCalendar();
                MessageBox.Show("Слот изменен", "Успех");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        private bool IsValidTime(string time)
        {
            if (string.IsNullOrWhiteSpace(time)) return false;

            string[] parts = time.Split(':');
            if (parts.Length != 2) return false;

            if (!int.TryParse(parts[0], out int hours)) return false;
            if (!int.TryParse(parts[1], out int minutes)) return false;

            return hours >= 0 && hours <= 23 && minutes >= 0 && minutes <= 59;
        }

        private void InstructorSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = InstructorSearchBox.Text?.ToLower() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                InstructorResultsList.Visibility = Visibility.Collapsed;
                return;
            }

            if (_instructors == null || _instructors.Count == 0) return;

            var results = _instructors
                .Where(i => i.Name.ToLower().Contains(searchText) ||
                           i.Position.ToLower().Contains(searchText))
                .Take(10)
                .ToList();

            if (results.Any())
            {
                InstructorResultsList.ItemsSource = results;
                InstructorResultsList.Visibility = Visibility.Visible;
            }
            else
            {
                InstructorResultsList.Visibility = Visibility.Collapsed;
            }
        }

        private void InstructorResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InstructorResultsList.SelectedItem is Employee selected)
            {
                _selectedInstructor = selected;
                _selectedStudent = null;
                SelectedInstructorPanel.Visibility = Visibility.Visible;
                SelectedInstructorName.Text = selected.Name;
                InstructorDetails.Text = $"Должность: {selected.Position} | Машина: {(selected.CarId > 0 ? "назначена" : "не назначена")}";
                InstructorResultsList.Visibility = Visibility.Collapsed;
                InstructorSearchBox.Text = "";
                SelectedStudentPanel.Visibility = Visibility.Collapsed;

                LoadInstructorCalendar();
                Dispatcher.BeginInvoke(new Action(() => ScrollToToday()), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        private void ClearInstructor_Click(object sender, RoutedEventArgs e)
        {
            _selectedInstructor = null;
            SelectedInstructorPanel.Visibility = Visibility.Collapsed;
            if (_selectedStudent != null)
            {
                LoadCalendar();
            }
            else
            {
                CalendarGrid.ItemsSource = null;
                LessonsStatsText.Text = "";
            }
        }

        private void LoadInstructorCalendar()
        {
            if (_selectedInstructor == null)
            {
                CalendarGrid.ItemsSource = null;
                return;
            }

            if (_selectedInstructor.CarId == 0)
            {
                MessageBox.Show("У инструктора не назначена машина!\nОбратитесь к администратору.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _dataService.EnsureSlotsExist(_currentDate, _selectedInstructor.Id, _selectedInstructor.CarId);

                var firstDayOfMonth = new DateTime(_currentDate.Year, _currentDate.Month, 1);
                int daysToSubtract = ((int)firstDayOfMonth.DayOfWeek == 0 ? 6 : (int)firstDayOfMonth.DayOfWeek - 1);
                var startDate = firstDayOfMonth.AddDays(-daysToSubtract);
                var endDate = startDate.AddDays(41);

                var existingSlots = _dataService.GetInstructorTimeSlots(_selectedInstructor.Id);
                var timeSlots = new List<dynamic>();

                foreach (var slot in existingSlots)
                {
                    var parts = slot.Split(new[] { " - " }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        int hour = int.Parse(parts[0].Split(':')[0]);
                        timeSlots.Add(new { Time = parts[0], Start = hour });
                    }
                }

                if (timeSlots.Count == 0)
                {
                    timeSlots.Add(new { Time = "09:00", Start = 9 });
                    timeSlots.Add(new { Time = "11:00", Start = 11 });
                    timeSlots.Add(new { Time = "13:00", Start = 13 });
                    timeSlots.Add(new { Time = "15:00", Start = 15 });
                    timeSlots.Add(new { Time = "17:00", Start = 17 });
                }

                var timeSlotsDisplay = existingSlots.Count > 0 ? existingSlots : new List<string>
                {
                    "09:00 - 11:00", "11:00 - 13:00", "13:00 - 15:00", "15:00 - 17:00", "17:00 - 19:00"
                };
                TimeSlotsPanel.ItemsSource = timeSlotsDisplay;
                _currentTimeSlots = timeSlotsDisplay.ToList();

                var allSlots = _dataService.LoadAllSlots(
                    _selectedInstructor.Id,
                    _selectedInstructor.CarId,
                    startDate,
                    endDate);

                var lessons = _dataService.LoadInstructorLessons(_selectedInstructor.Id);

                var calendar = new List<CalendarRow>();

                foreach (var ts in timeSlots)
                {
                    var row = new CalendarRow { TimeSlot = ts.Time };
                    var days = new List<CalendarDay>();

                    for (int i = 0; i < 42; i++)
                    {
                        var date = startDate.AddDays(i);
                        bool isCurrentMonth = date.Month == _currentDate.Month;

                        var slot = allSlots.Slots.FirstOrDefault(s =>
                            s.LessonDate.Date == date.Date &&
                            s.StartTime.Hours == ts.Start);

                        var lesson = lessons.FirstOrDefault(l =>
                            l.LessonDate.Date == date.Date &&
                            l.StartTime.Hours == ts.Start);

                        bool isSlotExists = slot != null;
                        bool isSlotAvailable = slot?.IsAvailable ?? false;
                        bool hasBooking = lesson != null;

                        string status;
                        Brush statusColor;
                        bool canBook = false;
                        int slotId = slot?.Id ?? 0;
                        int lessonId = lesson?.Id ?? 0;
                        bool isExtra = lesson?.IsExtra ?? false;

                        if (hasBooking)
                        {
                            switch (lesson.Status)
                            {
                                case "Booked":
                                    status = isExtra ? "Доп. урок" : $"Забронирован ({lesson.StudentName})";
                                    statusColor = Brushes.Orange;
                                    canBook = false;
                                    break;
                                case "Completed":
                                    status = isExtra ? "Доп. проведен" : $"Проведен ({lesson.StudentName})";
                                    statusColor = Brushes.Green;
                                    canBook = false;
                                    break;
                                case "NoShow":
                                    status = isExtra ? "Доп. прогул" : $"Прогул ({lesson.StudentName})";
                                    statusColor = Brushes.Red;
                                    canBook = false;
                                    break;
                                case "Cancelled":
                                    status = isExtra ? "Доп. отменен" : $"Отменен ({lesson.StudentName})";
                                    statusColor = Brushes.Gray;
                                    canBook = isSlotAvailable;
                                    break;
                                default:
                                    status = isExtra ? "Доп. урок" : $"Забронирован ({lesson.StudentName})";
                                    statusColor = Brushes.Orange;
                                    canBook = false;
                                    break;
                            }
                        }
                        else if (isSlotExists && isSlotAvailable)
                        {
                            status = "Свободно";
                            statusColor = Brushes.Green;
                            canBook = true;
                        }
                        else if (isSlotExists && !isSlotAvailable)
                        {
                            status = "Занято";
                            statusColor = Brushes.LightCoral;
                            canBook = false;
                        }
                        else
                        {
                            status = "Нет слота";
                            statusColor = Brushes.LightGray;
                            canBook = false;
                        }

                        var dayCell = new CalendarDay
                        {
                            Date = date.Day.ToString(),
                            FullDate = date,
                            SlotId = slotId,
                            LessonId = lessonId,
                            IsAvailable = canBook,
                            HasBooking = hasBooking,
                            Status = status,
                            StatusColor = statusColor,
                            IsCurrentMonth = isCurrentMonth,
                            IsToday = date.Date == DateTime.Today.Date,
                            CanBook = canBook,
                            HasActiveBooking = hasBooking && lesson?.Status == "Booked",
                            IsExtra = isExtra,
                            IsSunday = date.DayOfWeek == DayOfWeek.Sunday
                        };

                        days.Add(dayCell);
                    }

                    row.Days = days;
                    calendar.Add(row);
                }

                _calendarData = calendar.SelectMany(r => r.Days).ToList();
                CalendarGrid.ItemsSource = calendar;

                LessonsStatsText.Text = $"Инструктор: {_selectedInstructor.Name} | Слотов: {_currentTimeSlots.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки календаря инструктора: {ex.Message}", "Ошибка");
            }
        }

        private void CheckAndHandlePayments()
        {
            if (_selectedStudent == null) return;

            var payments = _dataService.LoadStudentPayments(_selectedStudent.Id);
            var lessons = _dataService.LoadStudentLessons(_selectedStudent.Id);
            var usedLessons = lessons.Count(l => l.Status == "Completed" || l.Status == "NoShow");

            // Проверяем первый платеж (при первом бронировании)
            if (!payments.Any())
            {
                var result = MessageBox.Show(
                    $"Для начала обучения необходимо внести первый платеж.\n\n" +
                    $"Перейти к оплате?",
                    "Первый платеж",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    NavigateToPaymentPage(0, "Первый платеж");
                }
                return;
            }

            // Проверяем необходимость второго платежа после 10 уроков (включая прогулы)
            if (usedLessons >= 10 && !_dataService.HasSecondPayment(_selectedStudent.Id))
            {
                var totalPaid = payments.Sum(p => p.Amount);
                var remainingAmount = _selectedStudent.FinalAmount - totalPaid;

                if (remainingAmount > 0)
                {
                    var result = MessageBox.Show(
                        $"Поздравляем! Вы прошли {usedLessons} уроков вождения.\n\n" +
                        $"Осталось оплатить: {remainingAmount:N2} руб.\n\n" +
                        $"Перейти к оплате?",
                        "Оплата оставшейся суммы",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        NavigateToPaymentPage(remainingAmount, "Основной платеж");
                    }
                }
            }
        }

        private void NavigateToPaymentPage(decimal amount, string paymentType)
        {
            try
            {
                var dialog = new PaymentEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName);
                dialog.Owner = Window.GetWindow(this);

                if (amount > 0)
                {
                    dialog.SetPaymentData(amount, paymentType);
                }

                if (dialog.ShowDialog() == true)
                {
                    MessageBox.Show($"Платеж успешно проведен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadCalendar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при переходе к оплате: {ex.Message}", "Ошибка");
            }
        }
    }

    public class CalendarRow
    {
        public string TimeSlot { get; set; }
        public List<CalendarDay> Days { get; set; }
    }

    public class CalendarDay
    {
        public string Date { get; set; }
        public DateTime FullDate { get; set; }
        public int SlotId { get; set; }
        public int LessonId { get; set; }
        public bool IsAvailable { get; set; }
        public bool HasBooking { get; set; }
        public bool HasActiveBooking { get; set; }
        public string Status { get; set; }
        public Brush StatusColor { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool IsToday { get; set; }
        public bool CanBook { get; set; }
        public bool IsExtra { get; set; }
        public bool IsSunday { get; set; }

        public Brush Background
        {
            get
            {
                if (IsSunday && !IsCurrentMonth)
                    return Brushes.LavenderBlush;

                if (IsSunday && IsCurrentMonth && Status == "Свободно")
                    return Brushes.LightCyan;

                if (IsToday && IsCurrentMonth)
                    return Brushes.LightYellow;

                if (!IsCurrentMonth)
                    return Brushes.WhiteSmoke;

                if (Status == "Свободно")
                    return Brushes.LightGreen;

                if (Status == "Забронирован" || Status == "Доп. урок")
                    return Brushes.Orange;

                if (Status == "Проведен" || Status == "Доп. проведен")
                    return Brushes.Green;

                if (Status == "Прогул" || Status == "Доп. прогул")
                    return Brushes.Red;

                if (Status == "Занято")
                    return Brushes.LightCoral;

                return Brushes.LightGray;
            }
        }
    }
}