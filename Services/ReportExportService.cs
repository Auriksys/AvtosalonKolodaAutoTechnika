using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using FDNJ_CSharp.Models;

namespace FDNJ_CSharp.Services;

public class ReportExportService
{
    private static ReportExportService? _instance;
    public static ReportExportService Instance => _instance ??= new ReportExportService();

    #region Excel Exports (ClosedXML)
    public void ExportCarsToExcel(IEnumerable<Car> cars, string filePath)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Автомобили");

        // Title Header
        ws.Cell("A1").Value = "АВТОСАЛОН «DRIVE» — КАТАЛОГ АВТОМОБИЛЕЙ";
        ws.Range("A1:N1").Merge();
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 15;
        ws.Cell("A1").Style.Font.FontColor = XLColor.White;
        ws.Cell("A1").Style.Fill.BackgroundColor = XLColor.FromArgb(15, 23, 42); // #0f172a
        ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(1).Height = 32;

        // Date generated
        ws.Cell("A2").Value = $"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm} | Всего автомобилей: {cars.Count()}";
        ws.Range("A2:N2").Merge();
        ws.Cell("A2").Style.Font.Italic = true;
        ws.Cell("A2").Style.Font.FontColor = XLColor.FromArgb(100, 116, 139);
        ws.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(2).Height = 20;

        // Table Headers
        string[] headers =
        {
            "ID", "VIN", "Марка", "Модель", "Год", "Цвет", "Кузов",
            "Мощность (л.с.)", "КПП", "Привод", "Пробег (км)", "Цена (₽)", "Статус", "Поставщик"
        };

        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(3, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(59, 130, 246); // #3b82f6
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        ws.Row(3).Height = 24;

        int row = 4;
        foreach (var car in cars)
        {
            ws.Cell(row, 1).Value = car.Id;
            ws.Cell(row, 2).Value = car.Vin;
            ws.Cell(row, 3).Value = car.Brand;
            ws.Cell(row, 4).Value = car.Model;
            ws.Cell(row, 5).Value = car.Year;
            ws.Cell(row, 6).Value = car.Color;
            ws.Cell(row, 7).Value = car.BodyType;
            ws.Cell(row, 8).Value = car.Power;
            ws.Cell(row, 9).Value = car.Transmission;
            ws.Cell(row, 10).Value = car.Drive;
            ws.Cell(row, 11).Value = car.Mileage;
            ws.Cell(row, 12).Value = (double)car.Price;
            ws.Cell(row, 13).Value = car.Status;
            ws.Cell(row, 14).Value = car.SupplierName ?? "—";

            // Formatting
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 11).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 12).Style.NumberFormat.Format = "#,##0 ₽";
            ws.Cell(row, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Zebra striping
            if (row % 2 == 0)
            {
                ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromArgb(248, 250, 252);
            }

            row++;
        }

        // Summary row
        int lastRow = row - 1;
        if (cars.Any())
        {
            ws.Cell(row, 3).Value = "ИТОГО:";
            ws.Cell(row, 3).Style.Font.Bold = true;
            ws.Cell(row, 12).FormulaA1 = $"SUM(L4:L{lastRow})";
            ws.Cell(row, 12).Style.Font.Bold = true;
            ws.Cell(row, 12).Style.NumberFormat.Format = "#,##0 ₽";
            ws.Range(row, 1, row, headers.Length).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            ws.Range(row, 1, row, headers.Length).Style.Border.BottomBorder = XLBorderStyleValues.Double;
        }

        ws.Range(3, 1, Math.Max(row, 4), headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        ws.Range(3, 1, Math.Max(row, 4), headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    public void ExportSalesToExcel(IEnumerable<Sale> sales, string filePath)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Продажи");

        ws.Cell("A1").Value = "АВТОСАЛОН «DRIVE» — ЖУРНАЛ СОВЕРШЕННЫХ СДЕЛОК";
        ws.Range("A1:H1").Merge();
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 15;
        ws.Cell("A1").Style.Font.FontColor = XLColor.White;
        ws.Cell("A1").Style.Fill.BackgroundColor = XLColor.FromArgb(15, 23, 42);
        ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(1).Height = 32;

        ws.Cell("A2").Value = $"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm} | Сделок: {sales.Count()}";
        ws.Range("A2:H2").Merge();
        ws.Cell("A2").Style.Font.Italic = true;
        ws.Cell("A2").Style.Font.FontColor = XLColor.FromArgb(100, 116, 139);
        ws.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(2).Height = 20;

        string[] headers =
        {
            "№ Сделки", "Дата", "Автомобиль", "VIN", "Покупатель", "Телефон клиента", "Оплата", "Сумма (₽)"
        };

        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(3, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(16, 185, 129); // #10b981 Emerald
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        ws.Row(3).Height = 24;

        int row = 4;
        foreach (var sale in sales)
        {
            ws.Cell(row, 1).Value = sale.Id;
            ws.Cell(row, 2).Value = sale.SaleDate;
            ws.Cell(row, 3).Value = sale.CarTitle;
            ws.Cell(row, 4).Value = sale.CarVin;
            ws.Cell(row, 5).Value = sale.ClientName;
            ws.Cell(row, 6).Value = sale.ClientPhone;
            ws.Cell(row, 7).Value = sale.PaymentMethod;
            ws.Cell(row, 8).Value = (double)sale.Price;

            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0 ₽";

            if (row % 2 == 0)
                ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromArgb(248, 250, 252);

            row++;
        }

        int lastRow = row - 1;
        if (sales.Any())
        {
            ws.Cell(row, 2).Value = "ИТОГОВАЯ ВЫРУЧКА:";
            ws.Cell(row, 2).Style.Font.Bold = true;
            ws.Cell(row, 8).FormulaA1 = $"SUM(H4:H{lastRow})";
            ws.Cell(row, 8).Style.Font.Bold = true;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0 ₽";
            ws.Range(row, 1, row, headers.Length).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            ws.Range(row, 1, row, headers.Length).Style.Border.BottomBorder = XLBorderStyleValues.Double;
        }

        ws.Range(3, 1, Math.Max(row, 4), headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        ws.Range(3, 1, Math.Max(row, 4), headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    public void ExportClientsToExcel(IEnumerable<Client> clients, string filePath)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Клиенты");

        ws.Cell("A1").Value = "АВТОСАЛОН «DRIVE» — БАЗА КЛИЕНТОВ";
        ws.Range("A1:E1").Merge();
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 14;
        ws.Cell("A1").Style.Font.FontColor = XLColor.White;
        ws.Cell("A1").Style.Fill.BackgroundColor = XLColor.FromArgb(15, 23, 42);
        ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        string[] headers = { "ID", "ФИО Клиента", "Телефон", "Email", "Примечание" };
        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(2, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(59, 130, 246);
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 3;
        foreach (var cl in clients)
        {
            ws.Cell(row, 1).Value = cl.Id;
            ws.Cell(row, 2).Value = cl.FullName;
            ws.Cell(row, 3).Value = cl.Phone;
            ws.Cell(row, 4).Value = cl.Email;
            ws.Cell(row, 5).Value = cl.Comment;
            row++;
        }

        ws.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    public void ExportFullFinancialReportToExcel(DashboardStats stats, IEnumerable<Car> cars, IEnumerable<Sale> sales, string filePath)
    {
        using var workbook = new XLWorkbook();

        // 1. Dashboard summary
        var ws = workbook.Worksheets.Add("Сводная аналитика");
        ws.Cell("A1").Value = "ГЕНЕРАЛЬНЫЙ ФИНАНСОВЫЙ ОТЧЁТ АВТОСАЛОНА «DRIVE»";
        ws.Range("A1:D1").Merge();
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 15;
        ws.Cell("A1").Style.Fill.BackgroundColor = XLColor.FromArgb(15, 23, 42);
        ws.Cell("A1").Style.Font.FontColor = XLColor.White;
        ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell("A3").Value = "Ключевой показатель";
        ws.Cell("B3").Value = "Значение";
        ws.Range("A3:B3").Style.Font.Bold = true;
        ws.Range("A3:B3").Style.Fill.BackgroundColor = XLColor.FromArgb(226, 232, 240);

        var kpis = new (string Key, object Val, string Fmt)[]
        {
            ("Общее количество автомобилей в системе", stats.TotalCars, "#,##0 шт."),
            ("Автомобилей в наличии", stats.InStockCars, "#,##0 шт."),
            ("Автомобилей под бронью", stats.BookedCars, "#,##0 шт."),
            ("Продано автомобилей", stats.SoldCars, "#,##0 шт."),
            ("На диагностике и сервисе", stats.InDiagnosticsOrRepairCars, "#,##0 шт."),
            ("ИТОГОВАЯ ВЫРУЧКА", (double)stats.TotalRevenue, "#,##0 ₽"),
            ("Средний чек сделки", (double)stats.AverageSalePrice, "#,##0 ₽"),
            ("Количество клиентов в базе", stats.TotalClients, "#,##0 чел."),
            ("Количество партнеров-поставщиков", stats.TotalSuppliers, "#,##0")
        };

        int r = 4;
        foreach (var (k, v, fmt) in kpis)
        {
            ws.Cell(r, 1).Value = k;
            ws.Cell(r, 2).Value = (v is double d) ? d : Convert.ToInt32(v);
            ws.Cell(r, 2).Style.NumberFormat.Format = fmt;
            if (k.Contains("ВЫРУЧКА"))
            {
                ws.Range(r, 1, r, 2).Style.Font.Bold = true;
                ws.Range(r, 1, r, 2).Style.Fill.BackgroundColor = XLColor.FromArgb(220, 252, 231);
            }
            r++;
        }

        // Top brands
        r += 2;
        ws.Cell(r, 1).Value = "ТОП МАРОК";
        ws.Cell(r, 2).Value = "Кол-во";
        ws.Range(r, 1, r, 2).Style.Font.Bold = true;
        ws.Range(r, 1, r, 2).Style.Fill.BackgroundColor = XLColor.FromArgb(226, 232, 240);
        r++;
        foreach (var b in stats.TopBrands)
        {
            ws.Cell(r, 1).Value = b.Key;
            ws.Cell(r, 2).Value = b.Count;
            r++;
        }

        ws.Columns().AdjustToContents();

        // 2. Sales sheet
        var wsSales = workbook.Worksheets.Add("Продажи");
        string[] sHeaders = { "ID", "Дата", "Автомобиль", "VIN", "Клиент", "Оплата", "Цена" };
        for (int i = 0; i < sHeaders.Length; i++)
        {
            wsSales.Cell(1, i + 1).Value = sHeaders[i];
            wsSales.Cell(1, i + 1).Style.Font.Bold = true;
            wsSales.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromArgb(59, 130, 246);
            wsSales.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }
        int sr = 2;
        foreach (var sale in sales)
        {
            wsSales.Cell(sr, 1).Value = sale.Id;
            wsSales.Cell(sr, 2).Value = sale.SaleDate;
            wsSales.Cell(sr, 3).Value = sale.CarTitle;
            wsSales.Cell(sr, 4).Value = sale.CarVin;
            wsSales.Cell(sr, 5).Value = sale.ClientName;
            wsSales.Cell(sr, 6).Value = sale.PaymentMethod;
            wsSales.Cell(sr, 7).Value = (double)sale.Price;
            wsSales.Cell(sr, 7).Style.NumberFormat.Format = "#,##0 ₽";
            sr++;
        }
        wsSales.Columns().AdjustToContents();

        workbook.SaveAs(filePath);
    }
    #endregion

    #region HTML & Printable Reports (PDF-ready)
    public string GenerateSaleReceiptHtml(Sale sale, Car? car)
    {
        string carModel = car != null ? $"{car.Brand} {car.Model}" : sale.CarTitle;
        string vin = car != null ? car.Vin : sale.CarVin;
        string year = car != null ? car.Year.ToString() : "—";
        string engine = car != null ? $"{car.Engine} ({car.Power} л.с.)" : "—";
        string color = car != null ? car.Color : "—";
        string mileage = car != null ? $"{car.Mileage:N0} км" : "—";

        return $@"<!DOCTYPE html>
<html lang=""ru"">
<head>
    <meta charset=""UTF-8"">
    <title>Договор купли-продажи № {sale.Id}</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 40px; color: #1e293b; background: #fff; }}
        .header {{ display: flex; justify-content: space-between; border-bottom: 3px solid #3b82f6; padding-bottom: 20px; }}
        .logo {{ font-size: 32px; font-weight: 900; color: #1d4ed8; letter-spacing: 2px; }}
        .logo span {{ color: #0f172a; font-weight: 400; font-size: 14px; display: block; }}
        .doc-title {{ text-align: center; margin: 30px 0 15px 0; font-size: 22px; font-weight: bold; text-transform: uppercase; }}
        .meta {{ font-size: 14px; color: #64748b; text-align: center; margin-bottom: 25px; }}
        .section {{ margin-bottom: 25px; }}
        .section-title {{ font-size: 16px; font-weight: bold; background: #f1f5f9; padding: 8px 12px; border-left: 4px solid #3b82f6; margin-bottom: 12px; }}
        table {{ width: 100%; border-collapse: collapse; margin-bottom: 15px; font-size: 14px; }}
        th, td {{ padding: 10px 14px; border: 1px solid #e2e8f0; text-align: left; }}
        th {{ background: #f8fafc; font-weight: 600; width: 35%; }}
        .total-box {{ background: #eff6ff; border: 2px solid #3b82f6; border-radius: 8px; padding: 18px; margin: 30px 0; display: flex; justify-content: space-between; align-items: center; }}
        .total-label {{ font-size: 18px; font-weight: bold; color: #1e3a8a; }}
        .total-amount {{ font-size: 28px; font-weight: 900; color: #1d4ed8; }}
        .signatures {{ display: flex; justify-content: space-between; margin-top: 60px; }}
        .sign-box {{ width: 45%; border-top: 1px solid #94a3b8; padding-top: 10px; font-size: 13px; text-align: center; }}
        .print-btn {{ position: fixed; top: 20px; right: 20px; background: #3b82f6; color: white; border: none; padding: 12px 24px; font-size: 15px; font-weight: bold; border-radius: 6px; cursor: pointer; }}
        @media print {{ .print-btn {{ display: none; }} body {{ margin: 0; }} }}
    </style>
</head>
<body>
    <button class=""print-btn"" onclick=""window.print()"">🖨 Печать / Сохранить в PDF</button>

    <div class=""header"">
        <div class=""logo"">DRIVE<span>АВТОСАЛОН ПРЕМИУМ И ВТОРИЧНОГО РЫНКА</span></div>
        <div style=""text-align: right; font-size: 13px; color: #475569;"">
            <strong>ООО «ДРАЙВ МОТОРС»</strong><br>
            г. Москва, Автозаводская ул., 23<br>
            Тел: +7 (800) 555-35-35<br>
            ИНН 7701234567 / КПП 770101001
        </div>
    </div>

    <div class=""doc-title"">ДОГОВОР КУПЛИ-ПРОДАЖИ АВТОМОБИЛЯ № {sale.Id}</div>
    <div class=""meta"">г. Москва &bull; Дата заключения сделки: <strong>{sale.SaleDate}</strong></div>

    <div class=""section"">
        <div class=""section-title"">1. СВЕДЕНИЯ О СТОРОНАХ</div>
        <table>
            <tr><th>Продавец:</th><td>ООО «ДРАЙВ МОТОРС» (Менеджер: {sale.ManagerLogin})</td></tr>
            <tr><th>Покупатель (ФИО):</th><td><strong>{sale.ClientName}</strong></td></tr>
            <tr><th>Контактный телефон:</th><td>{sale.ClientPhone}</td></tr>
        </table>
    </div>

    <div class=""section"">
        <div class=""section-title"">2. ПРЕДМЕТ ДОГОВОРА (ТРАНСПОРТНОЕ СРЕДСТВО)</div>
        <table>
            <tr><th>Марка и модель:</th><td><strong>{carModel}</strong></td></tr>
            <tr><th>Идентификационный номер (VIN):</th><td><code style=""font-weight:bold; font-size:15px;"">{vin}</code></td></tr>
            <tr><th>Год выпуска:</th><td>{year} г.</td></tr>
            <tr><th>Двигатель и мощность:</th><td>{engine}</td></tr>
            <tr><th>Цвет кузова:</th><td>{color}</td></tr>
            <tr><th>Пробег на момент сделки:</th><td>{mileage}</td></tr>
        </table>
    </div>

    <div class=""section"">
        <div class=""section-title"">3. СТОИМОСТЬ И ПОРЯДОК РАСЧЕТОВ</div>
        <div class=""total-box"">
            <div class=""total-label"">Сумма сделки:<br><small style=""font-size:13px; font-weight:normal; color:#64748b;"">Способ оплаты: {sale.PaymentMethod}</small></div>
            <div class=""total-amount"">{sale.Price:N0} ₽</div>
        </div>
    </div>

    <div class=""signatures"">
        <div class=""sign-box"">
            <strong>От Продавца:</strong><br><br>
            ________________ / {sale.ManagerLogin} /<br>
            М.П.
        </div>
        <div class=""sign-box"">
            <strong>От Покупателя:</strong><br><br>
            ________________ / {sale.ClientName} /
        </div>
    </div>
</body>
</html>";
    }

    public void OpenInBrowserOrPrint(string htmlContent)
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid():N}.html");
        File.WriteAllText(tempPath, htmlContent);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = tempPath,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Failed to open report: " + ex.Message);
        }
    }
    #endregion
}
