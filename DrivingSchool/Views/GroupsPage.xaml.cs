using DrivingSchool.Models;
using DrivingSchool.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DrivingSchool.Views
{
    public partial class GroupsPage : Page
    {
        private readonly SqlDataService _dataService;
        private StudyGroupCollection _groups;
        private StudentCollection _students;
        private readonly ReportService _reportService;

        public GroupsPage(SqlDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            _reportService = new ReportService(dataService);
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _groups = _dataService.LoadStudyGroups();
                _students = _dataService.LoadStudents();

                foreach (var group in _groups.Groups)
                {
                    group.StudentCount = _students.Students.Count(s => s.GroupId == group.Id);
                }

                ApplyFilter();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _groups = new StudyGroupCollection { Groups = new List<StudyGroup>() };
                _students = new StudentCollection { Students = new List<Student>() };
                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            try
            {
                var searchText = SearchTextBox?.Text?.ToLower() ?? string.Empty;

                string statusFilter = "Все группы";
                if (StatusFilterComboBox?.SelectedItem is ComboBoxItem selectedStatus)
                {
                    statusFilter = selectedStatus.Content.ToString();
                }

                var filtered = _groups.Groups.AsEnumerable();

                // Фильтр по статусу
                if (statusFilter == "Активные")
                {
                    filtered = filtered.Where(g => g.Status == "Активна");
                }
                else if (statusFilter == "Завершенные")
                {
                    filtered = filtered.Where(g => g.Status == "Завершена");
                }

                // Поиск в реальном времени
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    filtered = filtered.Where(g =>
                        (g.Name ?? "").ToLower().Contains(searchText) ||
                        (g.Status ?? "").ToLower().Contains(searchText) ||
                        (g.Category ?? "").ToLower().Contains(searchText));
                }

                GroupsGrid.ItemsSource = filtered.ToList();
                UpdateButtonsAvailability();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка фильтрации: {ex.Message}");
            }
        }

        private void UpdateStatus()
        {
            var activeGroups = _groups.Groups.Count(g => g.Status == "Активна");
            var totalStudents = _groups.Groups.Sum(g => g.StudentCount);

            StatusTextBlock.Text = $"Групп: {_groups.Groups.Count} | Активных: {activeGroups} | Студентов: {totalStudents}";
        }

        private void UpdateButtonsAvailability()
        {
            var selectedGroups = GroupsGrid.SelectedItems.Cast<StudyGroup>().ToList();
            var singleSelected = selectedGroups.Count == 1;
            var multipleSelected = selectedGroups.Count >= 2;

            EditGroupButton.IsEnabled = singleSelected;
            DeleteGroupButton.IsEnabled = selectedGroups.Any(); // Доступна при выборе 1 или нескольких
            ViewStudentsButton.IsEnabled = singleSelected;
            AddStudentButton.IsEnabled = singleSelected;
            RemoveStudentButton.IsEnabled = singleSelected;
            MoveStudentButton.IsEnabled = singleSelected;
            MergeGroupsButton.IsEnabled = multipleSelected;
            ExportGroupButton.IsEnabled = singleSelected;

            if (singleSelected)
            {
                var group = selectedGroups[0];
                InfoTextBlock.Text = $"Группа: {group.Name} | Категория: {group.Category} | Студентов: {group.StudentCount} | " +
                                    $"Период: {group.StartDate:dd.MM.yyyy} - {group.EndDate:dd.MM.yyyy}";
            }
            else if (multipleSelected)
            {
                var categories = selectedGroups.Select(g => g.Category).Distinct().ToList();
                var totalStudents = selectedGroups.Sum(g => g.StudentCount);
                InfoTextBlock.Text = $"Выбрано групп: {selectedGroups.Count} | Всего студентов: {totalStudents} | " +
                                    $"Категории: {string.Join(", ", categories)} | (для отмены выбора кликните еще раз)";
            }
            else
            {
                InfoTextBlock.Text = "Выберите группу (для выбора нескольких зажмите Ctrl)";
            }
        }

        private void GroupsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsAvailability();
        }

        private void GroupsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (GroupsGrid.SelectedItem != null)
            {
                EditGroup_Click(sender, e);
            }
        }

        private void AddGroup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new GroupEditDialog(_dataService);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    LoadData();
                    MessageBox.Show("Группа успешно создана!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании группы: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditGroup_Click(object sender, RoutedEventArgs e)
        {
            if (!(GroupsGrid.SelectedItem is StudyGroup selectedGroup))
            {
                MessageBox.Show("Выберите группу для редактирования", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dialog = new GroupEditDialog(_dataService, selectedGroup, selectedGroup.StudentCount);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    LoadData();
                    MessageBox.Show("Группа успешно обновлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteGroup_Click(object sender, RoutedEventArgs e)
        {
            var selectedGroups = GroupsGrid.SelectedItems.Cast<StudyGroup>().ToList();

            if (!selectedGroups.Any())
            {
                MessageBox.Show("Выберите группу для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Если выбрано несколько групп
            if (selectedGroups.Count > 1)
            {
                var totalStudents = selectedGroups.Sum(g => g.StudentCount);
                var activeGroups = selectedGroups.Count(g => g.Status == "Активна");
                var completedGroups = selectedGroups.Count(g => g.Status == "Завершена");

                var msg = $"Вы действительно хотите удалить {selectedGroups.Count} групп?\n\n" +
                          $"Из них:\n" +
                          $"• Активных: {activeGroups}\n" +
                          $"• Завершенных: {completedGroups}\n" +
                          $"Всего студентов в этих группах: {totalStudents}\n\n" +
                          "Студенты будут откреплены от групп (останутся в системе).\n\n" +
                          "Это действие нельзя отменить!";

                var result = MessageBox.Show(msg, "Подтверждение массового удаления",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                try
                {
                    int deletedCount = 0;
                    int detachedStudents = 0;

                    foreach (var group in selectedGroups)
                    {
                        var studentsInGroup = _students.Students.Where(s => s.GroupId == group.Id).ToList();
                        foreach (var student in studentsInGroup)
                        {
                            student.GroupId = 0;
                            _dataService.SaveStudent(student);
                        }
                        detachedStudents += studentsInGroup.Count;

                        if (_dataService.DeleteStudyGroup(group.Id))
                        {
                            deletedCount++;
                        }
                    }

                    LoadData();
                    MessageBox.Show($"Удалено групп: {deletedCount} из {selectedGroups.Count}\n" +
                                   $"Откреплено студентов: {detachedStudents}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return;
            }

            // Существующий код для ОДНОЙ группы
            var selectedGroup = selectedGroups[0];

            // Формируем сообщение в зависимости от статуса и наличия студентов
            string msgText = "";
            string title = "Подтверждение удаления";
            MessageBoxButton buttons = MessageBoxButton.YesNo;
            MessageBoxImage icon = MessageBoxImage.Warning;

            bool hasStudents = selectedGroup.StudentCount > 0;
            bool isCompleted = selectedGroup.Status == "Завершена";

            if (isCompleted && hasStudents)
            {
                msgText = $"Группа '{selectedGroup.Name}' ЗАВЕРШЕНА, но в ней есть {selectedGroup.StudentCount} студентов.\n\n" +
                          "Студенты будут откреплены от группы (останутся в системе).\n\n" +
                          "Вы уверены, что хотите удалить группу?";
                icon = MessageBoxImage.Question;
            }
            else if (isCompleted && !hasStudents)
            {
                msgText = $"Группа '{selectedGroup.Name}' ЗАВЕРШЕНА и не содержит студентов.\n\n" +
                          "Вы уверены, что хотите удалить группу?";
                icon = MessageBoxImage.Question;
            }
            else if (!isCompleted && hasStudents)
            {
                msgText = $"ВНИМАНИЕ! Группа '{selectedGroup.Name}' АКТИВНА и содержит {selectedGroup.StudentCount} студентов.\n\n" +
                          "Студенты будут откреплены от группы (останутся в системе).\n\n" +
                          "Вы действительно хотите удалить группу?";
                icon = MessageBoxImage.Error;
            }
            else
            {
                msgText = $"Удалить группу '{selectedGroup.Name}'?\n\nЭто действие нельзя отменить!";
                icon = MessageBoxImage.Warning;
            }

            if (MessageBox.Show(msgText, title, buttons, icon) == MessageBoxResult.Yes)
            {
                try
                {
                    if (hasStudents)
                    {
                        var studentsInGroup = _students.Students.Where(s => s.GroupId == selectedGroup.Id).ToList();
                        foreach (var student in studentsInGroup)
                        {
                            student.GroupId = 0;
                            _dataService.SaveStudent(student);
                        }
                    }

                    bool deleted = _dataService.DeleteStudyGroup(selectedGroup.Id);

                    if (deleted)
                    {
                        LoadData();
                        MessageBox.Show($"Группа '{selectedGroup.Name}' успешно удалена." +
                                       (hasStudents ? $"\n\n{selectedGroup.StudentCount} студентов откреплены от группы." : ""),
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Не удалось удалить группу.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewStudents_Click(object sender, RoutedEventArgs e)
        {
            if (!(GroupsGrid.SelectedItem is StudyGroup selectedGroup))
            {
                MessageBox.Show("Выберите группу для просмотра студентов", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ShowStudentsInGroup(selectedGroup);
        }

        private void ShowStudentsInGroup(StudyGroup group)
        {
            var studentsInGroup = _students.Students
                .Where(s => s.GroupId == group.Id)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToList();

            if (studentsInGroup.Any())
            {
                var studentList = string.Join("\n", studentsInGroup.Select(s =>
                    $"• {s.FullName} (тел: {s.Phone}, категория: {s.CategoryCode})"));

                MessageBox.Show($"Студенты в группе {group.Name}:\n\n{studentList}",
                    $"Студенты группы ({studentsInGroup.Count} чел.)",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("В выбранной группе нет студентов", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AddStudentToGroup_Click(object sender, RoutedEventArgs e)
        {
            if (!(GroupsGrid.SelectedItem is StudyGroup selectedGroup))
            {
                MessageBox.Show("Выберите группу для добавления студента", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var availableStudents = _students.Students
                    .Where(s => s.CategoryCode == selectedGroup.Category &&
                               (s.GroupId == 0 || s.GroupId != selectedGroup.Id))
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .ToList();

                if (!availableStudents.Any())
                {
                    MessageBox.Show($"Нет доступных студентов категории {selectedGroup.Category} для добавления",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                ShowStudentSelectionDialog(selectedGroup, availableStudents);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении студента: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowStudentSelectionDialog(StudyGroup group, List<Student> availableStudents)
        {
            var dialog = new Window
            {
                Title = $"Добавление студентов в группу {group.Name} (категория {group.Category})",
                Width = 600,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this)
            };

            var grid = new Grid();
            dialog.Content = grid;

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var listBox = new ListBox
            {
                Margin = new Thickness(10),
                SelectionMode = SelectionMode.Multiple,
                DisplayMemberPath = "FullName"
            };
            listBox.ItemsSource = availableStudents;

            var searchPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10) };
            var searchBox = new TextBox
            {
                Width = 200,
                Margin = new Thickness(0, 0, 10, 0),
                ToolTip = "Поиск по фамилии, имени или телефону"
            };

            var countLabel = new TextBlock
            {
                Text = $"Всего: {availableStudents.Count}",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = System.Windows.Media.Brushes.Gray
            };

            searchBox.TextChanged += (textSender, textArgs) =>
            {
                var searchText = searchBox.Text.ToLower();
                List<Student> filtered;

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    filtered = availableStudents;
                }
                else
                {
                    filtered = availableStudents
                        .Where(student => student.FullName.ToLower().Contains(searchText) ||
                                         student.Phone.Contains(searchText))
                        .ToList();
                }

                listBox.ItemsSource = filtered;
                countLabel.Text = $"Найдено: {filtered.Count} из {availableStudents.Count}";
            };

            searchPanel.Children.Add(searchBox);
            searchPanel.Children.Add(countLabel);
            Grid.SetRow(searchPanel, 0);
            Grid.SetRow(listBox, 1);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };

            var addButton = new Button
            {
                Content = "Добавить выбранных",
                Width = 150,
                Height = 35,
                Background = System.Windows.Media.Brushes.Green,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var cancelButton = new Button
            {
                Content = "Отмена",
                Width = 100,
                Height = 35
            };

            buttonPanel.Children.Add(addButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);

            grid.Children.Add(searchPanel);
            grid.Children.Add(listBox);
            grid.Children.Add(buttonPanel);

            addButton.Click += (clickSender, clickArgs) =>
            {
                var selectedStudents = listBox.SelectedItems.Cast<Student>().ToList();

                if (!selectedStudents.Any())
                {
                    MessageBox.Show("Выберите хотя бы одного студента", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var invalidStudents = selectedStudents.Where(st => st.CategoryCode != group.Category).ToList();
                if (invalidStudents.Any())
                {
                    var invalidNames = string.Join("\n", invalidStudents.Select(st => $"• {st.FullName} (категория {st.CategoryCode})"));
                    MessageBox.Show($"Следующие студенты не могут быть добавлены в группу {group.Name}:\n\n{invalidNames}\n\n" +
                                   $"Категория группы: {group.Category}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var studentsFromOtherGroups = selectedStudents.Where(st => st.GroupId != 0 && st.GroupId != group.Id).ToList();

                if (studentsFromOtherGroups.Any())
                {
                    var msg = "Следующие студенты уже находятся в других группах:\n";
                    foreach (var st in studentsFromOtherGroups)
                    {
                        var oldGroup = _groups.Groups.FirstOrDefault(g => g.Id == st.GroupId);
                        msg += $"\n• {st.FullName} - группа {(oldGroup?.Name ?? "Неизвестная")}";
                    }
                    msg += "\n\nПри добавлении они будут перемещены в текущую группу. Продолжить?";

                    if (MessageBox.Show(msg, "Подтверждение перемещения",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                foreach (var st in selectedStudents)
                {
                    st.GroupId = group.Id;
                    _dataService.SaveStudent(st);
                }

                LoadData();
                MessageBox.Show($"Добавлено студентов: {selectedStudents.Count}", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                dialog.Close();
            };

            cancelButton.Click += (cancelSender, cancelArgs) => dialog.Close();

            dialog.ShowDialog();
        }

        private void RemoveStudentFromGroup_Click(object sender, RoutedEventArgs e)
        {
            if (!(GroupsGrid.SelectedItem is StudyGroup selectedGroup))
            {
                MessageBox.Show("Выберите группу", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var studentsInGroup = _students.Students
                .Where(s => s.GroupId == selectedGroup.Id)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToList();

            if (!studentsInGroup.Any())
            {
                MessageBox.Show("В группе нет студентов", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ShowStudentRemovalDialog(selectedGroup, studentsInGroup);
        }

        private void ShowStudentRemovalDialog(StudyGroup group, List<Student> studentsInGroup)
        {
            var dialog = new Window
            {
                Title = $"Удаление студентов из группы {group.Name}",
                Width = 500,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this)
            };

            var grid = new Grid();
            dialog.Content = grid;

            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var listBox = new ListBox
            {
                Margin = new Thickness(10),
                SelectionMode = SelectionMode.Multiple,
                DisplayMemberPath = "FullName"
            };
            listBox.ItemsSource = studentsInGroup;
            Grid.SetRow(listBox, 0);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };

            var removeButton = new Button
            {
                Content = "Удалить выбранных",
                Width = 150,
                Height = 35,
                Background = System.Windows.Media.Brushes.Red,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var cancelButton = new Button
            {
                Content = "Отмена",
                Width = 100,
                Height = 35
            };

            buttonPanel.Children.Add(removeButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 1);

            grid.Children.Add(listBox);
            grid.Children.Add(buttonPanel);

            removeButton.Click += (removeSender, removeArgs) =>
            {
                var selectedStudents = listBox.SelectedItems.Cast<Student>().ToList();

                if (!selectedStudents.Any())
                {
                    MessageBox.Show("Выберите студентов для удаления", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var message = $"Удалить {selectedStudents.Count} студентов из группы?\n\n" +
                             "Студенты не будут удалены из системы, только исключены из группы.";

                if (MessageBox.Show(message, "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    foreach (var student in selectedStudents)
                    {
                        student.GroupId = 0;
                        _dataService.SaveStudent(student);
                    }

                    LoadData();
                    MessageBox.Show($"Студенты удалены из группы", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    dialog.Close();
                }
            };

            cancelButton.Click += (cancelSender, cancelArgs) => dialog.Close();

            dialog.ShowDialog();
        }

        private void MoveStudentToGroup_Click(object sender, RoutedEventArgs e)
        {
            if (!(GroupsGrid.SelectedItem is StudyGroup selectedGroup))
            {
                MessageBox.Show("Выберите группу", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var otherGroups = _groups.Groups
                .Where(g => g.Id != selectedGroup.Id && g.Category == selectedGroup.Category)
                .OrderBy(g => g.Name)
                .ToList();

            if (!otherGroups.Any())
            {
                MessageBox.Show($"Нет других групп категории {selectedGroup.Category} для перемещения",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var studentsInGroup = _students.Students
                .Where(s => s.GroupId == selectedGroup.Id)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToList();

            if (!studentsInGroup.Any())
            {
                MessageBox.Show("В группе нет студентов", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ShowStudentMoveDialog(selectedGroup, otherGroups, studentsInGroup);
        }

        private void ShowStudentMoveDialog(StudyGroup sourceGroup, List<StudyGroup> targetGroups, List<Student> students)
        {
            var dialog = new Window
            {
                Title = $"Перемещение студентов из группы {sourceGroup.Name} (категория {sourceGroup.Category})",
                Width = 600,
                Height = 550,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this)
            };

            var grid = new Grid();
            dialog.Content = grid;

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Выбор целевой группы
            var groupPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10) };
            groupPanel.Children.Add(new TextBlock
            {
                Text = "Целевая группа:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            });

            var groupCombo = new ComboBox
            {
                Width = 200,
                DisplayMemberPath = "Name",
                SelectedValuePath = "Id",
                Margin = new Thickness(0, 0, 10, 0)
            };
            groupCombo.ItemsSource = targetGroups;
            groupCombo.SelectedIndex = 0;
            groupPanel.Children.Add(groupCombo);

            var studentCountLabel = new TextBlock
            {
                Text = $"Студентов в группе: {((StudyGroup)groupCombo.SelectedItem)?.StudentCount ?? 0}",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = System.Windows.Media.Brushes.Gray
            };
            groupPanel.Children.Add(studentCountLabel);

            groupCombo.SelectionChanged += (comboSender, comboArgs) =>
            {
                if (groupCombo.SelectedItem is StudyGroup selectedGrp)
                {
                    studentCountLabel.Text = $"Студентов в группе: {selectedGrp.StudentCount}";
                }
            };

            Grid.SetRow(groupPanel, 0);

            // Панель поиска
            var searchPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10) };
            var searchBox = new TextBox
            {
                Width = 200,
                Margin = new Thickness(0, 0, 10, 0),
                ToolTip = "Поиск по фамилии, имени или телефону"
            };
            var searchCountLabel = new TextBlock
            {
                Text = $"Всего: {students.Count}",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = System.Windows.Media.Brushes.Gray
            };
            searchPanel.Children.Add(searchBox);
            searchPanel.Children.Add(searchCountLabel);
            Grid.SetRow(searchPanel, 1);

            // Список студентов
            var listBox = new ListBox
            {
                Margin = new Thickness(10),
                SelectionMode = SelectionMode.Multiple,
                DisplayMemberPath = "FullName"
            };
            listBox.ItemsSource = students;
            Grid.SetRow(listBox, 2);

            // Поиск в реальном времени
            searchBox.TextChanged += (textSender, textArgs) =>
            {
                var searchText = searchBox.Text.ToLower();
                List<Student> filtered;

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    filtered = students;
                }
                else
                {
                    filtered = students
                        .Where(student => student.FullName.ToLower().Contains(searchText) ||
                                         student.Phone.Contains(searchText))
                        .ToList();
                }

                listBox.ItemsSource = filtered;
                searchCountLabel.Text = $"Найдено: {filtered.Count} из {students.Count}";
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };

            var moveButton = new Button
            {
                Content = "Переместить выбранных",
                Width = 180,
                Height = 35,
                Background = System.Windows.Media.Brushes.Blue,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var cancelButton = new Button
            {
                Content = "Отмена",
                Width = 100,
                Height = 35
            };

            buttonPanel.Children.Add(moveButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 3);

            grid.Children.Add(groupPanel);
            grid.Children.Add(searchPanel);
            grid.Children.Add(listBox);
            grid.Children.Add(buttonPanel);

            moveButton.Click += (moveSender, moveArgs) =>
            {
                var selectedStudents = listBox.SelectedItems.Cast<Student>().ToList();
                var targetGroup = groupCombo.SelectedItem as StudyGroup;

                if (targetGroup == null)
                {
                    MessageBox.Show("Выберите целевую группу", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!selectedStudents.Any())
                {
                    MessageBox.Show("Выберите студентов для перемещения", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var invalidStudents = selectedStudents.Where(student => student.CategoryCode != targetGroup.Category).ToList();
                if (invalidStudents.Any())
                {
                    var invalidNames = string.Join("\n", invalidStudents.Select(student => $"• {student.FullName} (категория {student.CategoryCode})"));
                    MessageBox.Show($"Следующие студенты не могут быть перемещены в группу {targetGroup.Name}:\n\n{invalidNames}\n\n" +
                                   $"Категория группы: {targetGroup.Category}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var message = $"Переместить {selectedStudents.Count} студентов в группу {targetGroup.Name}?";

                if (MessageBox.Show(message, "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    foreach (var student in selectedStudents)
                    {
                        student.GroupId = targetGroup.Id;
                        _dataService.SaveStudent(student);
                    }

                    LoadData();
                    MessageBox.Show($"Студенты перемещены в группу {targetGroup.Name}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    dialog.Close();
                }
            };

            cancelButton.Click += (cancelSender, cancelArgs) => dialog.Close();

            dialog.ShowDialog();
        }

        private void MergeGroups_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedGroups = GroupsGrid.SelectedItems.Cast<StudyGroup>().ToList();

                if (selectedGroups.Count < 2)
                {
                    MessageBox.Show("Выберите минимум 2 группы для объединения (кликните на несколько групп)",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var categories = selectedGroups.Select(g => g.Category).Distinct().ToList();
                if (categories.Count > 1)
                {
                    MessageBox.Show($"Нельзя объединять группы разных категорий!\n\n" +
                                   $"Выбраны категории: {string.Join(", ", categories)}\n\n" +
                                   $"Объединять можно только группы ОДНОЙ категории.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dialog = new GroupEditDialog(_dataService);
                dialog.Title = "Создание группы для объединения";
                dialog.Owner = Window.GetWindow(this);

                var names = selectedGroups.Select(g => g.Name).ToList();
                dialog.GroupData.Name = $"Объединение: {string.Join(" + ", names)}";
                dialog.GroupData.Category = categories[0];
                dialog.GroupData.StartDate = selectedGroups.Min(g => g.StartDate);
                dialog.GroupData.EndDate = selectedGroups.Max(g => g.EndDate);

                if (dialog.ShowDialog() == true)
                {
                    var newGroup = dialog.GroupData;

                    if (newGroup.Id == 0)
                    {
                        MessageBox.Show("Ошибка: группа не была сохранена в базе данных", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var totalStudents = selectedGroups.Sum(g => g.StudentCount);
                    var message = $"Будут объединены группы категории {newGroup.Category}:\n";
                    foreach (var g in selectedGroups)
                    {
                        message += $"• {g.Name} ({g.StudentCount} студентов)\n";
                    }
                    message += $"\nБудет создана новая группа: {newGroup.Name}";
                    message += $"\nВсего студентов будет перемещено: {totalStudents}";
                    message += $"\n\nИсходные группы будут УДАЛЕНЫ. Продолжить?";

                    if (MessageBox.Show(message, "Подтверждение объединения",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        bool result = _dataService.MergeGroups(
                            newGroup.Id,
                            selectedGroups.Select(g => g.Id).ToList()
                        );

                        if (result)
                        {
                            LoadData();
                            MessageBox.Show($"Группы успешно объединены!\n\n" +
                                           $"Создана группа: {newGroup.Name}\n" +
                                           $"Категория: {newGroup.Category}\n" +
                                           $"Объединено групп: {selectedGroups.Count}\n" +
                                           $"Перемещено студентов: {totalStudents}",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Не удалось объединить группы", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при объединении групп: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            ApplyFilter();
        }

        private void ExportGroupButton_Click(object sender, RoutedEventArgs e)
        {
            var group = GroupsGrid.SelectedItem as StudyGroup;
            if (group == null)
            {
                MessageBox.Show("Выберите группу для экспорта", "Ошибка");
                return;
            }

            var dialog = new SaveFileDialog
            {
                FileName = $"Group_{group.Name}_{DateTime.Now:dd_MM_yyyy}.xls",
                Filter = "Excel Files|*.xls",
                DefaultExt = "xls"
            };

            if (dialog.ShowDialog() == true)
            {
                _reportService.ExportGroupStudentsToExcel(group.Id, dialog.FileName);
            }
        }

        // Выделение нескольких групп без зажатия Ctrl
        private void GroupsGrid_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var row = FindVisualParent<DataGridRow>(e.OriginalSource as DependencyObject);
            if (row != null && row.Item is StudyGroup clickedGroup)
            {
                // Если зажат Ctrl - стандартное поведение
                if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) ||
                    System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl))
                {
                    return; // Пусть DataGrid сам обрабатывает
                }

                // Без Ctrl - переключаем выделение
                if (GroupsGrid.SelectedItems.Contains(clickedGroup))
                {
                    GroupsGrid.SelectedItems.Remove(clickedGroup);
                }
                else
                {
                    GroupsGrid.SelectedItems.Add(clickedGroup);
                }

                e.Handled = true;
            }
        }

        // Вспомогательный метод
        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }
    }
}