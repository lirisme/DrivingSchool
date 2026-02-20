using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class TariffsPage : Page
    {
        private readonly SqlDataService _dataService;
        private TariffCollection _tariffs;

        public TariffsPage(SqlDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _tariffs = _dataService.LoadTariffs();
                TariffsGrid.ItemsSource = _tariffs.Tariffs;
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _tariffs = new TariffCollection { Tariffs = new System.Collections.Generic.List<Tariff>() };
            }
        }

        private void UpdateStatus()
        {
            var totalCount = _tariffs?.Tariffs?.Count ?? 0;
            StatusTextBlock.Text = $"Всего тарифов: {totalCount}";
            UpdateButtonsAvailability();
        }

        private void UpdateButtonsAvailability()
        {
            var isSelected = TariffsGrid.SelectedItem != null;
            EditTariffButton.IsEnabled = isSelected;
            DeleteTariffButton.IsEnabled = isSelected;
        }

        private void TariffsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsAvailability();
        }

        private void AddTariff_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new TariffEditDialog(_dataService);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    LoadData();
                    MessageBox.Show("Тариф успешно добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditTariff_Click(object sender, RoutedEventArgs e)
        {
            if (!(TariffsGrid.SelectedItem is Tariff selectedTariff))
            {
                MessageBox.Show("Выберите тариф для редактирования", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dialog = new TariffEditDialog(_dataService, selectedTariff);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    LoadData();
                    MessageBox.Show("Тариф успешно обновлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteTariff_Click(object sender, RoutedEventArgs e)
        {
            if (!(TariffsGrid.SelectedItem is Tariff selectedTariff))
            {
                MessageBox.Show("Выберите тариф для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Удалить тариф '{selectedTariff.Name}'?\n\nЭто действие нельзя отменить!",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    bool deleted = _dataService.DeleteTariff(selectedTariff.Id);

                    if (deleted)
                    {
                        LoadData();
                        MessageBox.Show("Тариф удален.", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Не удалось удалить тариф. Возможно, он используется в стоимости обучения.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void TariffsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (TariffsGrid.SelectedItem != null)
            {
                EditTariff_Click(sender, e);
            }
        }
    }
}