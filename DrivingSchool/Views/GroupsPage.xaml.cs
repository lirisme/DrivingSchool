using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Models;
using DrivingSchool.Services;
using System.Collections.Generic;

namespace DrivingSchool.Views
{
    public partial class GroupsPage : Page
    {
        private readonly SqlDataService _dataService;
        private StudyGroupCollection _groups;
        private StudentCollection _students;
        private StudyGroup _selectedGroup;

        public GroupsPage(SqlDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _groups = _dataService.LoadStudyGroups();
                _students = _dataService.LoadStudents();

                // Подсчитываем количество студентов в каждой группе
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
            var searchText = SearchTextBox?.Text?.ToLower() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                GroupsGrid.ItemsSource = _groups.Groups;
            }
            else
            {
                var filtered = _groups.Groups
                    .Where(g => (g.Name ?? "").ToLower().Contains(searchText) ||
                               (g.Status ?? "").ToLower().Contains(searchText) ||
                               (g.Category ?? "").ToLower().Contains(searchText))
                    .ToList();
                GroupsGrid.ItemsSource = filtered;
            }

            UpdateButtonsAvailability();
        }

        private void UpdateStatus()
        {
            var activeGroups = _groups.Groups.Count(g => g.Status == "Активна");
            var totalStudents = _groups.Groups.Sum(g => g.StudentCount);

            StatusTextBlock.Text = $"Групп: {_groups.Groups.Count} | Активных: {activeGroups} | Студентов: {totalStudents}";
        }

        private void UpdateButtonsAvailability()
        {
            var isSelected = GroupsGrid.SelectedItem != null;

            if (isSelected)
            {
                _selectedGroup = GroupsGrid.SelectedItem as StudyGroup;
            }
            else
            {
                _selectedGroup = null;
            }

            EditGroupButton.IsEnabled = isSelected;
            DeleteGroupButton.IsEnabled = isSelected && (_selectedGroup?.StudentCount == 0);
            ViewStudentsButton.IsEnabled = isSelected;
            AddStudentButton.IsEnabled = isSelected;
            RemoveStudentButton.IsEnabled = isSelected;
            MoveStudentButton.IsEnabled = isSelected;

            if (isSelected && _selectedGroup != null)
            {
                InfoTextBlock.Text = $"Группа: {_selectedGroup.Name} | Студентов: {_selectedGroup.StudentCount} | " +
                                    $"Период: {_selectedGroup.StartDate:dd.MM.yyyy} - {_selectedGroup.EndDate:dd.MM.yyyy}";
            }
            else
            {
                InfoTextBlock.Text = "Выберите группу для управления студентами";
            }
        }

        private void GroupsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsAvailability();
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
                var dialog = new GroupEditDialog(_dataService, selectedGroup);
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
            if (!(GroupsGrid.SelectedItem is StudyGroup selectedGroup))
            {
                MessageBox.Show("Выберите группу для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (selectedGroup.StudentCount > 0)
            {
                MessageBox.Show($"Невозможно удалить группу. В группе {selectedGroup.StudentCount} студентов.\n\n" +
                               "Сначала переместите или удалите студентов из группы.",
                    "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Удалить группу {selectedGroup.Name}?\n\nЭто действие нельзя отменить!",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    bool deleted = _dataService.DeleteStudyGroup(selectedGroup.Id);

                    if (deleted)
                    {
                        LoadData();
                        MessageBox.Show("Группа удалена.", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
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
                // Получаем студентов без группы или из других групп
                var availableStudents = _students.Students
                    .Where(s => s.GroupId == 0 || s.GroupId != selectedGroup.Id)
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .ToList();

                if (!availableStudents.Any())
                {
                    MessageBox.Show("Нет доступных студентов для добавления", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
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
                Title = $"Добавление студентов в группу {group.Name}",
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

            // Поиск
            var searchPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10) };
            var searchBox = new TextBox { Width = 200, Margin = new Thickness(0, 0, 10, 0) };
            var searchButton = new Button { Content = "Поиск", Width = 80 };
            searchPanel.Children.Add(searchBox);
            searchPanel.Children.Add(searchButton);
            Grid.SetRow(searchPanel, 0);

            // Список студентов с чекбоксами
            var listBox = new ListBox
            {
                Margin = new Thickness(10),
                SelectionMode = SelectionMode.Multiple
            };

            // Используем простой шаблон без сложной привязки
            listBox.DisplayMemberPath = "Student.FullName";

            var studentItems = availableStudents.Select(s => new StudentSelectionItem
            {
                Student = s,
                IsSelected = false
            }).ToList();

            listBox.ItemsSource = studentItems;
            Grid.SetRow(listBox, 1);

            // Кнопки
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

            // Обработчики
            searchButton.Click += (searchSender, searchArgs) =>
            {
                var searchText = searchBox.Text.ToLower();
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    listBox.ItemsSource = studentItems;
                }
                else
                {
                    var filtered = studentItems
                        .Where(item => item.Student.FullName.ToLower().Contains(searchText) ||
                                      item.Student.Phone.Contains(searchText))
                        .ToList();
                    listBox.ItemsSource = filtered;
                }
            };

            addButton.Click += (addSender, addArgs) =>
            {
                // Получаем выбранные элементы через коллекцию
                var selectedItems = new List<Student>();
                foreach (var item in listBox.SelectedItems)
                {
                    if (item is StudentSelectionItem selectionItem)
                    {
                        selectedItems.Add(selectionItem.Student);
                    }
                }

                if (!selectedItems.Any())
                {
                    // Также проверяем через свойство IsSelected
                    var checkedItems = studentItems.Where(i => i.IsSelected).Select(i => i.Student).ToList();
                    if (checkedItems.Any())
                    {
                        selectedItems = checkedItems;
                    }
                }

                if (!selectedItems.Any())
                {
                    MessageBox.Show("Выберите хотя бы одного студента", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверяем, есть ли студенты из других групп
                var studentsFromOtherGroups = selectedItems.Where(student => student.GroupId != 0 && student.GroupId != group.Id).ToList();

                if (studentsFromOtherGroups.Any())
                {
                    var message = "Следующие студенты уже находятся в других группах:\n";
                    foreach (var student in studentsFromOtherGroups)
                    {
                        var oldGroup = _groups.Groups.FirstOrDefault(g => g.Id == student.GroupId);
                        message += $"\n• {student.FullName} - группа {(oldGroup?.Name ?? "Неизвестная")}";
                    }
                    message += "\n\nПри добавлении они будут перемещены в текущую группу. Продолжить?";

                    if (MessageBox.Show(message, "Подтверждение перемещения",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                // Добавляем студентов в группу
                foreach (var student in selectedItems)
                {
                    student.GroupId = group.Id;
                    _dataService.SaveStudent(student);
                }

                LoadData();
                MessageBox.Show($"Добавлено студентов: {selectedItems.Count}", "Успех",
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
                .Where(g => g.Id != selectedGroup.Id)
                .OrderBy(g => g.Name)
                .ToList();

            if (!otherGroups.Any())
            {
                MessageBox.Show("Нет других групп для перемещения", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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
                Title = $"Перемещение студентов из группы {sourceGroup.Name}",
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
                SelectedValuePath = "Id"
            };
            groupCombo.ItemsSource = targetGroups;
            groupCombo.SelectedIndex = 0;
            groupPanel.Children.Add(groupCombo);
            Grid.SetRow(groupPanel, 0);

            // Список студентов с чекбоксами
            var listBox = new ListBox
            {
                Margin = new Thickness(10),
                SelectionMode = SelectionMode.Multiple,
                DisplayMemberPath = "FullName"
            };
            listBox.ItemsSource = students;
            Grid.SetRow(listBox, 1);

            // Кнопки
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
            Grid.SetRow(buttonPanel, 2);

            grid.Children.Add(groupPanel);
            grid.Children.Add(listBox);
            grid.Children.Add(buttonPanel);

            moveButton.Click += (s, args) =>
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

            cancelButton.Click += (s, args) => dialog.Close();

            dialog.ShowDialog();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            ApplyFilter();
        }

        private void GroupsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (GroupsGrid.SelectedItem != null)
            {
                EditGroup_Click(sender, e);
            }
        }
    }

    // Вспомогательный класс для выбора студентов
    public class StudentSelectionItem
    {
        public Student Student { get; set; }
        public bool IsSelected { get; set; }
    }
}