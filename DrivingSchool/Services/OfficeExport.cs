using System;
using System.Data;
using System.Windows;
using Microsoft.Win32;
using Excel = Microsoft.Office.Interop.Excel;
using Word = Microsoft.Office.Interop.Word;

namespace DrivingSchool.Services
{
    public class OfficeExport
    {
        public void ExportToExcel(DataTable data, string fileName)
        {
            try
            {
                var excelApp = new Excel.Application();
                excelApp.SheetsInNewWorkbook = 1;
                excelApp.DisplayAlerts = false;

                Excel.Workbook workbook = excelApp.Workbooks.Add(Type.Missing);
                Excel.Worksheet worksheet = (Excel.Worksheet)workbook.Sheets[1];

                // Заголовки
                for (int i = 0; i < data.Columns.Count; i++)
                {
                    worksheet.Cells[1, i + 1] = data.Columns[i].ColumnName;
                    ((Excel.Range)worksheet.Cells[1, i + 1]).Font.Bold = true;
                    ((Excel.Range)worksheet.Cells[1, i + 1]).Interior.Color = System.Drawing.Color.LightGray.ToArgb();
                }

                // Данные
                for (int i = 0; i < data.Rows.Count; i++)
                {
                    for (int j = 0; j < data.Columns.Count; j++)
                    {
                        worksheet.Cells[i + 2, j + 1] = data.Rows[i][j].ToString();
                    }
                }

                // Автоширина
                worksheet.Columns.AutoFit();

                workbook.SaveAs(fileName);
                workbook.Close();
                excelApp.Quit();

                // Освобождаем ресурсы
                System.Runtime.InteropServices.Marshal.ReleaseComObject(worksheet);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);

                MessageBox.Show($"Отчет сохранен: {fileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта в Excel: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ExportToPdf(DataTable data, string fileName, string title)
        {
            try
            {
                // Сначала создаем временный Word документ, потом сохраняем как PDF
                var wordApp = new Word.Application();
                wordApp.DisplayAlerts = Word.WdAlertLevel.wdAlertsNone;
                wordApp.Visible = false;

                Word.Document doc = wordApp.Documents.Add();

                // Заголовок
                Word.Paragraph titlePara = doc.Paragraphs.Add();
                titlePara.Range.Text = title;
                titlePara.Range.Font.Size = 18;
                titlePara.Range.Font.Bold = 1;
                titlePara.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                titlePara.Range.InsertParagraphAfter();

                // Дата
                Word.Paragraph datePara = doc.Paragraphs.Add();
                datePara.Range.Text = $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}";
                datePara.Range.Font.Size = 10;
                datePara.Range.InsertParagraphAfter();

                // Таблица
                Word.Table table = doc.Tables.Add(doc.Paragraphs.Add().Range, data.Rows.Count + 1, data.Columns.Count);
                table.Borders.Enable = 1;
                table.Range.Font.Size = 10;

                // Заголовки таблицы
                for (int i = 0; i < data.Columns.Count; i++)
                {
                    table.Cell(1, i + 1).Range.Text = data.Columns[i].ColumnName;
                    table.Cell(1, i + 1).Range.Font.Bold = 1;
                    table.Cell(1, i + 1).Shading.BackgroundPatternColor = Word.WdColor.wdColorGray20;
                }

                // Данные
                for (int i = 0; i < data.Rows.Count; i++)
                {
                    for (int j = 0; j < data.Columns.Count; j++)
                    {
                        table.Cell(i + 2, j + 1).Range.Text = data.Rows[i][j].ToString();
                    }
                }

                table.AutoFitBehavior(Word.WdAutoFitBehavior.wdAutoFitContent);

                // Сохраняем как PDF
                doc.SaveAs2(fileName, Word.WdSaveFormat.wdFormatPDF);
                doc.Close();
                wordApp.Quit();

                MessageBox.Show($"Отчет сохранен: {fileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта в PDF: {ex.Message}\n\nТребуется установленный Microsoft Word", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}