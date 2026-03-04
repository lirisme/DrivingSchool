using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DrivingSchool.Models;
using DrivingSchool.Services;
using System.Collections.Generic;

namespace DrivingSchool.Views
{
    public partial class StudentsPage : Page
    {
        private readonly SqlDataService _dataService;
        private StudentCollection _students;
        private StudentCollection _filteredStudents;

        // Данные документов
        private StudentPassportDataCollection _passports;
        private StudentSNILSCollection _snilsList;
        private StudentMedicalCertificateCollection _medicalList;
        private StudentRegistrationAddressCollection _addresses;
        private StudentCertificateCollection _certificates;
        private StudentDrivingLicenseCollection _licenses;

        public StudentsPage(SqlDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            LoadStudents();
            LoadDocuments();
        }

        private void LoadStudents()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Загрузка студентов...");
                _students = _dataService.LoadStudents();

                if (_students == null)
                {
                    System.Diagnostics.Debug.WriteLine("_students == null, создаем новую коллекцию");
                    _students = new StudentCollection { Students = new List<Student>() };
                }

                if (_students.Students == null)
                {
                    System.Diagnostics.Debug.WriteLine("_students.Students == null, создаем новый список");
                    _students.Students = new List<Student>();
                }

                System.Diagnostics.Debug.WriteLine($"Загружено студентов: {_students.Students.Count}");

                // Загружаем документы
                LoadDocuments();

                ApplyFilter();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки данных: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                _students = new StudentCollection { Students = new List<Student>() };
                LoadDocuments(); // Все равно загружаем документы (они будут пустыми)
                ApplyFilter();
            }
        }

        private void LoadDocuments()
        {
            try
            {
                // Загружаем все документы с защитой от null
                _passports = _dataService.LoadPassportData() ?? new StudentPassportDataCollection { Passports = new List<StudentPassportData>() };
                _snilsList = _dataService.LoadSNILSData() ?? new StudentSNILSCollection { SNILSList = new List<StudentSNILS>() };
                _medicalList = _dataService.LoadMedicalData() ?? new StudentMedicalCertificateCollection { Certificates = new List<StudentMedicalCertificate>() };
                _addresses = _dataService.LoadAddresses() ?? new StudentRegistrationAddressCollection { Addresses = new List<StudentRegistrationAddress>() };
                _certificates = _dataService.LoadCertificates() ?? new StudentCertificateCollection { Certificates = new List<StudentCertificate>() };
                _licenses = _dataService.LoadDrivingLicenses() ?? new StudentDrivingLicenseCollection { Licenses = new List<StudentDrivingLicense>() };

                System.Diagnostics.Debug.WriteLine("Документы загружены успешно");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки документов: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                // Создаем пустые коллекции
                _passports = new StudentPassportDataCollection { Passports = new List<StudentPassportData>() };
                _snilsList = new StudentSNILSCollection { SNILSList = new List<StudentSNILS>() };
                _medicalList = new StudentMedicalCertificateCollection { Certificates = new List<StudentMedicalCertificate>() };
                _addresses = new StudentRegistrationAddressCollection { Addresses = new List<StudentRegistrationAddress>() };
                _certificates = new StudentCertificateCollection { Certificates = new List<StudentCertificate>() };
                _licenses = new StudentDrivingLicenseCollection { Licenses = new List<StudentDrivingLicense>() };
            }
        }

        private void ApplyFilter()
        {
            try
            {
                if (_students?.Students == null)
                {
                    _filteredStudents = new StudentCollection { Students = new List<Student>() };
                }
                else
                {
                    var searchText = SearchTextBox?.Text?.ToLower() ?? string.Empty;

                    // Сначала получаем всех студентов
                    IEnumerable<Student> query = _students.Students;

                    // Применяем поиск
                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        query = query.Where(s => s != null && (
                            (s.LastName ?? "").ToLower().Contains(searchText) ||
                            (s.FirstName ?? "").ToLower().Contains(searchText) ||
                            (s.MiddleName ?? "").ToLower().Contains(searchText) ||
                            (s.Phone ?? "").Contains(searchText) ||
                            (s.Email ?? "").ToLower().Contains(searchText)));
                    }

                    // Обновляем статусы документов для каждого студента
                    var studentsList = query.Where(s => s != null).ToList();
                    foreach (var student in studentsList)
                    {
                        UpdateStudentDocumentsStatus(student);
                    }

                    _filteredStudents = new StudentCollection { Students = studentsList };
                }

                StudentsGrid.ItemsSource = _filteredStudents?.Students ?? new List<Student>();
                UpdateButtonsAvailability();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в ApplyFilter: {ex.Message}");
            }
        }

        private void UpdateStudentDocumentsStatus(Student student)
        {
            if (student == null) return;

            try
            {
                student.HasPassport = _passports?.Passports?.Any(p => p.StudentId == student.Id) ?? false;
                student.HasSNILS = _snilsList?.SNILSList?.Any(s => s.StudentId == student.Id) ?? false;
                student.HasMedical = _medicalList?.Certificates?.Any(m => m.StudentId == student.Id) ?? false;
                student.HasAddress = _addresses?.Addresses?.Any(a => a.StudentId == student.Id) ?? false;
                student.HasCertificate = _certificates?.Certificates?.Any(c => c.StudentId == student.Id) ?? false;
                student.HasDrivingLicense = _licenses?.Licenses?.Any(l => l.StudentId == student.Id) ?? false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления статуса документов для студента {student.Id}: {ex.Message}");
            }
        }

        private void UpdateStatus()
        {
            var totalCount = _students?.Students?.Count ?? 0;
            var filteredCount = _filteredStudents?.Students?.Count ?? 0;

            // Подсчет студентов с полным комплектом документов
            var fullDocs = _filteredStudents?.Students?.Count(s =>
                s.HasPassport && s.HasSNILS && s.HasMedical && s.HasAddress &&
                s.HasCertificate && s.HasDrivingLicense) ?? 0;

            if (filteredCount == totalCount)
            {
                StatusTextBlock.Text = $"Всего учащихся: {totalCount} | Полный комплект документов: {fullDocs}";
            }
            else
            {
                StatusTextBlock.Text = $"Найдено учащихся: {filteredCount} из {totalCount} | Полный комплект: {fullDocs}";
            }
        }
        private void UpdateButtonsAvailability()
        {
            try
            {
                var isSelected = StudentsGrid.SelectedItem != null;
                EditStudentButton.IsEnabled = isSelected;
                DeleteStudentButton.IsEnabled = isSelected;
                ViewStudentButton.IsEnabled = isSelected;
                DocumentsButton.IsEnabled = isSelected;
                PaymentsButton.IsEnabled = isSelected;

                if (isSelected && StudentsGrid.SelectedItem is Student selectedStudent && selectedStudent != null)
                {
                    var docsStatus = $"Документы: {selectedStudent.DocumentsStatus ?? "0/6"}";
                    InfoTextBlock.Text = $"Выбран: {selectedStudent.FullName} | Возраст: {selectedStudent.Age} | Категория: {selectedStudent.CategoryCode ?? "?"} | {docsStatus}";
                }
                else
                {
                    InfoTextBlock.Text = "Выберите учащегося для просмотра документов";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в UpdateButtonsAvailability: {ex.Message}");
            }
        }

        private void StudentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsAvailability();
        }

        private void AddStudent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new StudentEditDialog(_dataService);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    LoadStudents();
                    MessageBox.Show("Учащийся успешно добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditStudent_Click(object sender, RoutedEventArgs e)
        {
            if (!(StudentsGrid.SelectedItem is Student selectedStudent))
            {
                MessageBox.Show("Выберите учащегося для редактирования", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dialog = new StudentEditDialog(_dataService, selectedStudent);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    LoadStudents();
                    LoadDocuments(); // Перезагружаем документы
                    MessageBox.Show("Данные учащегося обновлены!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteStudent_Click(object sender, RoutedEventArgs e)
        {
            if (!(StudentsGrid.SelectedItem is Student selectedStudent))
            {
                MessageBox.Show("Выберите учащегося для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Удалить учащегося {selectedStudent.FullName}?\n\n" +
                               "Будут удалены все связанные данные (паспорт, СНИЛС, мед. справка, адрес, платежи).\n\n" +
                               "Это действие нельзя отменить!",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    _dataService.DeleteStudent(selectedStudent.Id);
                    LoadStudents();
                    LoadDocuments();
                    MessageBox.Show("Учащийся удален.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewStudent_Click(object sender, RoutedEventArgs e)
        {
            if (!(StudentsGrid.SelectedItem is Student selectedStudent))
            {
                MessageBox.Show("Выберите учащегося для просмотра", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            UpdateStudentDocumentsStatus(selectedStudent);

            var passport = _passports.Passports.FirstOrDefault(p => p.StudentId == selectedStudent.Id);
            var snils = _snilsList.SNILSList.FirstOrDefault(s => s.StudentId == selectedStudent.Id);
            var medical = _medicalList.Certificates.FirstOrDefault(m => m.StudentId == selectedStudent.Id);
            var address = _addresses.Addresses.FirstOrDefault(a => a.StudentId == selectedStudent.Id);
            var certificate = _certificates.Certificates.FirstOrDefault(c => c.StudentId == selectedStudent.Id);
            var license = _licenses.Licenses.FirstOrDefault(l => l.StudentId == selectedStudent.Id);

            var message = $"📋 ПОЛНЫЕ ДАННЫЕ УЧАЩЕГОСЯ:\n\n" +
                         $"👤 ФИО: {selectedStudent.FullName}\n" +
                         $"📞 Телефон: {selectedStudent.Phone}\n" +
                         $"📧 Email: {selectedStudent.Email ?? "не указан"}\n" +
                         $"🎂 Дата рождения: {selectedStudent.BirthDate:dd.MM.yyyy} (Возраст: {selectedStudent.Age})\n" +
                         $"🏠 Место рождения: {selectedStudent.BirthPlace}\n" +
                         $"🌍 Гражданство: {selectedStudent.Citizenship}\n" +
                         $"🚗 Категория: {selectedStudent.CategoryCode}\n\n" +
                         $"📋 ДОКУМЕНТЫ:\n" +
                         $"{(passport != null ? $"✅ Паспорт: {passport.Series} {passport.Number}" : "❌ Паспорт: не заполнен")}\n" +
                         $"{(snils != null ? $"✅ СНИЛС: {snils.Number}" : "❌ СНИЛС: не заполнен")}\n" +
                         $"{(medical != null ? $"✅ Мед. справка: {medical.Series} {medical.Number} (до {medical.ValidUntil:dd.MM.yyyy})" : "❌ Мед. справка: не заполнена")}\n" +
                         $"{(address != null ? $"✅ Адрес: {address.FullAddress}" : "❌ Адрес: не заполнен")}\n" +
                         $"{(certificate != null ? $"✅ Свидетельство: {certificate.CertificateSeries} {certificate.CertificateNumber}" : "❌ Свидетельство: не заполнено")}\n" +
                         $"{(license != null ? $"✅ Вод. удостоверение: {license.Series} {license.Number}" : "❌ Вод. удостоверение: не заполнено")}";

            MessageBox.Show(message, $"Данные учащегося: {selectedStudent.FullName}",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DocumentsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(StudentsGrid.SelectedItem is Student selectedStudent))
            {
                MessageBox.Show("Выберите учащегося для работы с документами", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ShowDocumentsDialog(selectedStudent);
        }

        private void ShowDocumentsDialog(Student student)
        {
            var dialog = new Window
            {
                Title = $"Документы: {student.FullName}",
                Width = 500,
                Height = 450,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this)
            };

            var grid = new Grid();
            dialog.Content = grid;

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Заголовок
            var headerText = new TextBlock
            {
                Text = $"Управление документами: {student.FullName}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10)
            };
            Grid.SetRow(headerText, 0);

            // Список документов
            var listBox = new ListBox
            {
                Margin = new Thickness(10),
                Background = Brushes.White
            };

            UpdateStudentDocumentsStatus(student);

            var items = new List<DocumentMenuItem>
            {
                new DocumentMenuItem {
                    Title = "Паспортные данные",
                    Status = student.HasPassport ? "✅" : "❌",
                    Type = "Passport",
                    Color = student.HasPassport ? "Green" : "Red"
                },
                new DocumentMenuItem {
                    Title = "СНИЛС",
                    Status = student.HasSNILS ? "✅" : "❌",
                    Type = "SNILS",
                    Color = student.HasSNILS ? "Green" : "Red"
                },
                new DocumentMenuItem {
                    Title = "Медицинская справка",
                    Status = student.HasMedical ? "✅" : "❌",
                    Type = "Medical",
                    Color = student.HasMedical ? "Green" : "Red"
                },
                new DocumentMenuItem {
                    Title = "Адрес регистрации",
                    Status = student.HasAddress ? "✅" : "❌",
                    Type = "Address",
                    Color = student.HasAddress ? "Green" : "Red"
                },
                new DocumentMenuItem {
                    Title = "Свидетельство об окончании",
                    Status = student.HasCertificate ? "✅" : "❌",
                    Type = "Certificate",
                    Color = student.HasCertificate ? "Green" : "Red"
                },
                new DocumentMenuItem {
                    Title = "Водительское удостоверение",
                    Status = student.HasDrivingLicense ? "✅" : "❌",
                    Type = "License",
                    Color = student.HasDrivingLicense ? "Green" : "Red"
                }
            };

            listBox.ItemsSource = items;
            listBox.DisplayMemberPath = "DisplayText";

            Grid.SetRow(listBox, 1);

            // Кнопки
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };

            var editButton = new Button
            {
                Content = "Редактировать выбранный документ",
                Width = 250,
                Height = 35,
                Background = Brushes.Blue,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var closeButton = new Button
            {
                Content = "Закрыть",
                Width = 100,
                Height = 35
            };

            buttonPanel.Children.Add(editButton);
            buttonPanel.Children.Add(closeButton);
            Grid.SetRow(buttonPanel, 2);

            grid.Children.Add(headerText);
            grid.Children.Add(listBox);
            grid.Children.Add(buttonPanel);

            editButton.Click += (editSender, editArgs) =>
            {
                if (listBox.SelectedItem is DocumentMenuItem selectedItem)
                {
                    OpenDocumentEditor(student, selectedItem.Type);
                    dialog.Close();
                }
                else
                {
                    MessageBox.Show("Выберите документ для редактирования", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            closeButton.Click += (closeSender, closeArgs) => dialog.Close();

            dialog.ShowDialog();
        }

        private void OpenDocumentEditor(Student student, string documentType)
        {
            switch (documentType)
            {
                case "Passport":
                    var passport = _passports.Passports.FirstOrDefault(p => p.StudentId == student.Id);
                    var passportDialog = new PassportEditDialog(_dataService, student.Id, student.FullName, passport);
                    passportDialog.Owner = Window.GetWindow(this);
                    if (passportDialog.ShowDialog() == true)
                    {
                        LoadDocuments();
                        ApplyFilter();
                    }
                    break;

                case "SNILS":
                    var snils = _snilsList.SNILSList.FirstOrDefault(s => s.StudentId == student.Id);
                    var snilsDialog = new SNILSEditDialog(_dataService, student.Id, student.FullName, snils);
                    snilsDialog.Owner = Window.GetWindow(this);
                    if (snilsDialog.ShowDialog() == true)
                    {
                        LoadDocuments();
                        ApplyFilter();
                    }
                    break;

                case "Medical":
                    var medical = _medicalList.Certificates.FirstOrDefault(m => m.StudentId == student.Id);
                    var medicalDialog = new MedicalEditDialog(_dataService, student.Id, student.FullName, medical);
                    medicalDialog.Owner = Window.GetWindow(this);
                    if (medicalDialog.ShowDialog() == true)
                    {
                        LoadDocuments();
                        ApplyFilter();
                    }
                    break;

                case "Address":
                    var address = _addresses.Addresses.FirstOrDefault(a => a.StudentId == student.Id);
                    var addressDialog = new AddressEditDialog(_dataService, student.Id, student.FullName, address);
                    addressDialog.Owner = Window.GetWindow(this);
                    if (addressDialog.ShowDialog() == true)
                    {
                        LoadDocuments();
                        ApplyFilter();
                    }
                    break;

                case "Certificate":
                    var certificate = _certificates.Certificates.FirstOrDefault(c => c.StudentId == student.Id);
                    var certificateDialog = new CertificateEditDialog(_dataService, student.Id, student.FullName, certificate);
                    certificateDialog.Owner = Window.GetWindow(this);
                    if (certificateDialog.ShowDialog() == true)
                    {
                        LoadDocuments();
                        ApplyFilter();
                    }
                    break;

                case "License":
                    var license = _licenses.Licenses.FirstOrDefault(l => l.StudentId == student.Id);
                    var licenseDialog = new DrivingLicenseEditDialog(_dataService, student.Id, student.FullName, license);
                    licenseDialog.Owner = Window.GetWindow(this);
                    if (licenseDialog.ShowDialog() == true)
                    {
                        LoadDocuments();
                        ApplyFilter();
                    }
                    break;
            }
        }

        private void PaymentsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(StudentsGrid.SelectedItem is Student selectedStudent))
            {
                MessageBox.Show("Выберите учащегося для просмотра оплат", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Открываем страницу платежей для этого студента
            var paymentsPage = new PaymentsPage(_dataService);
            (Window.GetWindow(this) as MainWindow)?.MainFrame.Navigate(paymentsPage);
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

        private void StudentsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (StudentsGrid.SelectedItem != null)
            {
                EditStudent_Click(sender, e);
            }
        }
    }

    public class DocumentMenuItem
    {
        public string Title { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string Color { get; set; }

        public string DisplayText => $"{Status} {Title}";
    }
}