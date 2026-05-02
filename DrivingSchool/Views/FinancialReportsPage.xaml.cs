using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Services;
using Microsoft.Win32;

namespace DrivingSchool.Views
{
    public partial class FinancialReportsPage : Page
    {
        private readonly ReportService _reportService;

        public FinancialReportsPage(SqlDataService dataService)
        {
            InitializeComponent();
            _reportService = new ReportService(dataService);

            StartDatePicker.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            EndDatePicker.SelectedDate = DateTime.Now;

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var startDate = StartDatePicker.SelectedDate ?? DateTime.Now.AddMonths(-1);
                var endDate = EndDatePicker.SelectedDate ?? DateTime.Now;

                var summary = _reportService.GetFinancialSummary(startDate, endDate);
                TotalIncomeText.Text = summary.TotalIncomeFormatted;
                TotalPaymentsText.Text = summary.PaymentsCount.ToString();
                AveragePaymentText.Text = summary.AveragePaymentFormatted;
                TotalDebtText.Text = summary.TotalDebtFormatted;

                PaymentsGrid.ItemsSource = _reportService.GetPaymentDetails(startDate, endDate);
                DebtorsGrid.ItemsSource = _reportService.GetDebtors();
                MonthlyBreakdownGrid.ItemsSource = _reportService.GetMonthlyBreakdown(startDate, endDate);
                GroupsGrid.ItemsSource = _reportService.GetGroupsFinancialReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        private void GenerateReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите период", "Ошибка");
                return;
            }
            if (StartDatePicker.SelectedDate > EndDatePicker.SelectedDate)
            {
                MessageBox.Show("Дата начала не может быть больше даты окончания", "Ошибка");
                return;
            }
            LoadData();
        }

        private void ExportDebtorsBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog { FileName = $"Debtors_{DateTime.Now:dd_MM_yyyy}.xls", Filter = "Excel|*.xls" };
            if (dialog.ShowDialog() == true)
            {
                var data = DebtorsGrid.ItemsSource as System.Collections.Generic.List<DebtorInfo>;
                if (data != null && data.Any())
                    _reportService.ExportDebtorsToExcel(data, dialog.FileName);
            }
        }

        private void ExportPaymentsBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog { FileName = $"Payments_{DateTime.Now:dd_MM_yyyy}.xls", Filter = "Excel|*.xls" };
            if (dialog.ShowDialog() == true)
            {
                var data = PaymentsGrid.ItemsSource as System.Collections.Generic.List<PaymentDetail>;
                if (data != null && data.Any())
                    _reportService.ExportPaymentsToExcel(data, dialog.FileName);
            }
        }

        private void ExportMonthlyBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog { FileName = $"Monthly_{DateTime.Now:dd_MM_yyyy}.xls", Filter = "Excel|*.xls" };
            if (dialog.ShowDialog() == true)
            {
                var data = MonthlyBreakdownGrid.ItemsSource as System.Collections.Generic.List<MonthlyBreakdown>;
                if (data != null && data.Any())
                    _reportService.ExportMonthlyToExcel(data, dialog.FileName);
            }
        }
    }
}