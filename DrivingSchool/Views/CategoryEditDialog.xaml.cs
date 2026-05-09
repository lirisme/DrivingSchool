using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class CategoryEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        public VehicleCategory CategoryData { get; private set; }
        private bool _isDirty = false;

        public CategoryEditDialog(SqlDataService dataService, VehicleCategory categoryData = null)
        {
            InitializeComponent();
            _dataService = dataService;

            if (categoryData != null)
            {
                CategoryData = categoryData;
                Title = "Редактирование категории";
            }
            else
            {
                CategoryData = new VehicleCategory();
                Title = "Добавление категории";
            }

            DataContext = CategoryData;
            SubscribeToChanges();
        }

        private void SubscribeToChanges()
        {
            CodeTextBox.TextChanged += (s, e) => _isDirty = true;
            FullNameTextBox.TextChanged += (s, e) => _isDirty = true;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_isDirty)
            {
                var result = MessageBox.Show(
                    "Есть несохраненные изменения. Сохранить перед закрытием?",
                    "Подтверждение закрытия",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (SaveCategory())
                    {
                        DialogResult = true;
                        base.OnClosing(e);
                    }
                    else
                    {
                        e.Cancel = true;
                    }
                }
                else if (result == MessageBoxResult.No)
                {
                    DialogResult = false;
                    base.OnClosing(e);
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else
            {
                base.OnClosing(e);
            }
        }

        private bool ValidateCategory()
        {
            if (string.IsNullOrWhiteSpace(CategoryData.Code))
            {
                MessageBox.Show("Введите код категории", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CodeTextBox.Focus();
                return false;
            }

            // Проверка формата кода (заглавные буквы и цифры)
            if (!Regex.IsMatch(CategoryData.Code, @"^[A-ZА-Я0-9]+$"))
            {
                MessageBox.Show("Код категории должен содержать только заглавные буквы и цифры", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CodeTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(CategoryData.FullName))
            {
                MessageBox.Show("Введите название категории", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                FullNameTextBox.Focus();
                return false;
            }

            return true;
        }

        private bool SaveCategory()
        {
            try
            {
                _dataService.SaveVehicleCategory(CategoryData);
                _isDirty = false;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateCategory())
                return;

            if (SaveCategory())
            {
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isDirty)
            {
                var result = MessageBox.Show(
                    "Есть несохраненные изменения. Закрыть без сохранения?",
                    "Подтверждение закрытия",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            DialogResult = false;
            Close();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SaveButton_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                CancelButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private void CodeTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Автоматическое преобразование в верхний регистр
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox != null)
            {
                int caret = textBox.CaretIndex;
                textBox.Text = textBox.Text.ToUpper();
                textBox.CaretIndex = Math.Min(caret, textBox.Text.Length);
            }
        }
    }
}