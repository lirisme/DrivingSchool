using DrivingSchool.Models;
using DrivingSchool.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DrivingSchool.Views
{
    public partial class CategoriesPage : Page
    {
        private SqlDataService _dataService;
        private VehicleCategoryCollection _categories;

        public CategoriesPage(SqlDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _categories = _dataService.LoadVehicleCategories();
                CategoriesGrid.ItemsSource = _categories.Categories;

                // Обновляем статус в главном окне
                UpdateMainWindowStatus($"Категории: всего {_categories.Categories.Count}");

                UpdateButtonsAvailability();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _categories = new VehicleCategoryCollection();
            }
        }

        private void UpdateMainWindowStatus(string status)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null && mainWindow.StatusText != null)
            {
                mainWindow.StatusText.Text = status;
            }
        }

        private void UpdateButtonsAvailability()
        {
            var isSelected = CategoriesGrid.SelectedItem != null;
            EditCategoryButton.IsEnabled = isSelected;
            DeleteCategoryButton.IsEnabled = isSelected;
            ViewCategoryButton.IsEnabled = isSelected;
        }

        private void CategoriesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsAvailability();
            UpdateInfoText();
        }

        private void UpdateInfoText()
        {
            if (CategoriesGrid.SelectedItem is VehicleCategory selected)
            {
                InfoText.Text = $"Выбрана: {selected.Code} - {selected.FullName}";
            }
            else
            {
                InfoText.Text = "Выберите категорию для просмотра или редактирования";
            }
        }

        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new CategoryEditDialog(_dataService);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    LoadData();
                    MessageBox.Show("Категория успешно добавлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            if (!(CategoriesGrid.SelectedItem is VehicleCategory selected))
            {
                MessageBox.Show("Выберите категорию для редактирования", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dialog = new CategoryEditDialog(_dataService, selected);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    LoadData();
                    MessageBox.Show("Категория успешно обновлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (!(CategoriesGrid.SelectedItem is VehicleCategory selected))
            {
                MessageBox.Show("Выберите категорию для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Проверяем, используется ли категория
                var students = _dataService.LoadStudents();
                var isUsed = students.Students.Any(s => s.VehicleCategoryId == selected.Id);

                if (isUsed)
                {
                    MessageBox.Show($"Невозможно удалить категорию '{selected.Code} - {selected.FullName}'\n\n" +
                        "Эта категория используется в карточках учащихся.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (MessageBox.Show($"Удалить категорию '{selected.Code} - {selected.FullName}'?\n\nЭто действие нельзя отменить!",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    bool deleted = _dataService.DeleteVehicleCategory(selected.Id);

                    if (deleted)
                    {
                        LoadData();
                        MessageBox.Show("Категория удалена.", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Не удалось удалить категорию. Возможно, она используется.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewCategory_Click(object sender, RoutedEventArgs e)
        {
            if (!(CategoriesGrid.SelectedItem is VehicleCategory selected))
            {
                MessageBox.Show("Выберите категорию для просмотра", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(
                $"Информация о категории:\n\n" +
                $"Код: {selected.Code}\n" +
                $"Название: {selected.FullName}\n" +
                $"ID: {selected.Id}",
                "Просмотр категории",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void CategoriesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CategoriesGrid.SelectedItem != null)
            {
                EditCategory_Click(sender, e);
            }
        }
    }
}