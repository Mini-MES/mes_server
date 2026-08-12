using mes_server.Data;
using mes_server.Models.DTOs.Analytics;
using mes_server.Services.Interface;
using System.Globalization;
using mes_server.Models.Analytics;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace mes_server.Services
{
    public class ExcelImportService : IExcelImportService
    {
        private readonly MESDbContext _dbContext;
        private static readonly string[] RequiredHeaders =
        [
            "WorkDate",
            "EquipmentID",
            "PlannedProductionMinutes",
            "OperatingMinutes",
            "DowntimeMinutes",
            "TargetQty",
            "GoodQty",
            "DefectQty"
        ];

        public ExcelImportService(MESDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ExcelImportResultDto> ImportDailyEquipmentOeeAsync(Stream fileStream, string sourceFileName, bool replaceExisting = false)
        {
            var result = new ExcelImportResultDto
            {
                ImportBatchID = Guid.NewGuid().ToString(),
            };

            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheets.FirstOrDefault() ?? throw new InvalidOperationException("No worksheet found in the Excel file.");

            var headerMap = GetHeaderMap(worksheet);
            var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            var importedRows = new List<DailyEquipmentOee>();

            for(var rowNumber = 2; rowNumber <= lastRowNumber; rowNumber++)
            {
                var row = worksheet.Row(rowNumber);

                if(!row.CellsUsed().Any())
                {
                    continue; // Skip empty rows
                }

                result.TotalRows++;

                try
                {
                    var dailyOee = ParseDailyEquipmentOee(
                     row,
                     headerMap,
                     result.ImportBatchID,
                     sourceFileName);

                    importedRows.Add(dailyOee);
                }
                catch (InvalidOperationException ex)
                {
                    result.Errors.Add(new ExcelImportErrorDto
                    {
                        RowNumber = rowNumber,
                        ErrorMessage = ex.Message
                    });
                }

            }

            if (result.Errors.Any())
            {
                return result;
            }

            var duplicateRows = importedRows
                .GroupBy(x => new { x.EquipmentID, x.WorkDate })
                .Where(group => group.Count() > 1)
                .SelectMany(group => group)
                .ToList();

            foreach (var duplicateRow in duplicateRows)
            {
                result.Errors.Add(new ExcelImportErrorDto
                {
                    RowNumber = duplicateRow.SourceRowNumber,
                    ErrorMessage = $"중복 데이터입니다. EquipmentID: {duplicateRow.EquipmentID}, WorkDate: {duplicateRow.WorkDate:yyyy-MM-dd}"
                });
            }

            if (result.Errors.Any())
            {
                return result;
            }

            var equipmentIds = importedRows
                .Select(x => x.EquipmentID)
                .Distinct()
                .ToList();

            var existingEquipmentIds = (await _dbContext.Equipments
                .Where(e => equipmentIds.Contains(e.EquipmentID))
                .Select(e => e.EquipmentID)
                .ToListAsync())
                .ToHashSet();

            foreach (var row in importedRows.Where(x => !existingEquipmentIds.Contains(x.EquipmentID)))
            {
                result.Errors.Add(new ExcelImportErrorDto
                {
                    RowNumber = row.SourceRowNumber,
                    ErrorMessage = $"존재하지 않는 설비입니다: {row.EquipmentID}"
                });
            }

            if (result.Errors.Any())
            {
                return result;
            }

            var workDates = importedRows
                .Select(x => x.WorkDate)
                .Distinct()
                .ToList();

            var existingDailyOees = await _dbContext.DailyEquipmentOees
                .Where(x => equipmentIds.Contains(x.EquipmentID) && workDates.Contains(x.WorkDate))
                .ToListAsync();

            var existingByKey = existingDailyOees.ToDictionary(
                x => (x.EquipmentID, x.WorkDate));

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                foreach (var importedRow in importedRows)
                {
                    var key = (importedRow.EquipmentID, importedRow.WorkDate);

                    if (existingByKey.TryGetValue(key, out var existingRow))
                    {
                        if (!replaceExisting)
                        {
                            result.Errors.Add(new ExcelImportErrorDto
                            {
                                RowNumber = importedRow.SourceRowNumber,
                                ErrorMessage = $"이미 분석 데이터가 존재합니다. EquipmentID: {importedRow.EquipmentID}, WorkDate: {importedRow.WorkDate:yyyy-MM-dd}"
                            });

                            continue;
                        }

                        UpdateDailyEquipmentOee(existingRow, importedRow, sourceFileName, result.ImportBatchID);
                        result.UpdateRows++;
                    }
                    else
                    {
                        _dbContext.DailyEquipmentOees.Add(importedRow);
                        result.ImportRows++;
                    }
                }

                if (result.Errors.Any())
                {
                    await transaction.RollbackAsync();
                    result.ImportRows = 0;
                    result.UpdateRows = 0;
                    return result;
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return result;
         }

        private static void UpdateDailyEquipmentOee(
            DailyEquipmentOee target,
            DailyEquipmentOee source,
            string sourceFileName,
            string importBatchId)
        {
            target.PlannedProductionMinutes = source.PlannedProductionMinutes;
            target.OperatingMinutes = source.OperatingMinutes;
            target.DowntimeMinutes = source.DowntimeMinutes;
            target.TargetQty = source.TargetQty;
            target.TotalProducedQty = source.TotalProducedQty;
            target.GoodQty = source.GoodQty;
            target.DefectQty = source.DefectQty;
            target.AvailabilityRate = source.AvailabilityRate;
            target.PerformanceRate = source.PerformanceRate;
            target.QualityRate = source.QualityRate;
            target.OeePercentage = source.OeePercentage;
            target.SourceFileName = sourceFileName;
            target.ImportBatchID = importBatchId;
            target.SourceRowNumber = source.SourceRowNumber;
            target.ImportedAt = DateTime.UtcNow;
        }


        private static Dictionary<string, int> GetHeaderMap(IXLWorksheet worksheet)
        {
            var headerRow = worksheet.FirstRowUsed() ?? throw new InvalidOperationException("No header row found in the Excel file.");
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var cell in headerRow.Cells())
            {
                var headerName = cell.GetString().Trim();
                if (!string.IsNullOrWhiteSpace(headerName))
                {
                    headerMap.TryAdd(headerName, cell.Address.ColumnNumber);
                }
            }

            var missingHeaders = RequiredHeaders
                .Where(header => !headerMap.ContainsKey(header))
                .ToList();

            if (missingHeaders.Any())
            {
                throw new InvalidOperationException(
                    $"필수 헤더가 없습니다: {string.Join(", ", missingHeaders)}");
            }

            return headerMap;
        }

        private static DailyEquipmentOee ParseDailyEquipmentOee(
            IXLRow row,
            IReadOnlyDictionary<string, int> headerMap,
            string importBatchId,
            string sourceFileName)
        {
            var workDate = GetDateOnly(row, headerMap["WorkDate"], "WorkDate");
            var equipmentId = GetRequiredText(row, headerMap["EquipmentID"], "EquipmentID");

            var plannedMinutes = GetNonNegativeInt(
                row, headerMap["PlannedProductionMinutes"], "PlannedProductionMinutes");

            var operatingMinutes = GetNonNegativeInt(
                row, headerMap["OperatingMinutes"], "OperatingMinutes");

            var downtimeMinutes = GetNonNegativeInt(
                row, headerMap["DowntimeMinutes"], "DowntimeMinutes");

            var targetQty = GetNonNegativeInt(row, headerMap["TargetQty"], "TargetQty");
            var goodQty = GetNonNegativeInt(row, headerMap["GoodQty"], "GoodQty");
            var defectQty = GetNonNegativeInt(row, headerMap["DefectQty"], "DefectQty");

            if (plannedMinutes <= 0)
            {
                throw new InvalidOperationException(
                    "PlannedProductionMinutes는 0보다 커야 합니다.");
            }

            if (operatingMinutes + downtimeMinutes > plannedMinutes)
            {
                throw new InvalidOperationException(
                    "OperatingMinutes와 DowntimeMinutes의 합은 계획 생산 시간보다 클 수 없습니다.");
            }

            var totalProducedQty = goodQty + defectQty;

            var availabilityRate = Math.Round(
                (decimal)operatingMinutes / plannedMinutes * 100, 2);

            var performanceRate = targetQty > 0
                ? Math.Round((decimal)totalProducedQty / targetQty * 100, 2)
                : 0;

            var qualityRate = totalProducedQty > 0
                ? Math.Round((decimal)goodQty / totalProducedQty * 100, 2)
                : 0;

            var oeePercentage = Math.Round(
                availabilityRate * performanceRate * qualityRate / 10000, 2);

            return new DailyEquipmentOee
            {
                EquipmentID = equipmentId,
                WorkDate = workDate,
                PlannedProductionMinutes = plannedMinutes,
                OperatingMinutes = operatingMinutes,
                DowntimeMinutes = downtimeMinutes,
                TargetQty = targetQty,
                TotalProducedQty = totalProducedQty,
                GoodQty = goodQty,
                DefectQty = defectQty,
                AvailabilityRate = availabilityRate,
                PerformanceRate = performanceRate,
                QualityRate = qualityRate,
                OeePercentage = oeePercentage,
                SourceFileName = sourceFileName,
                ImportBatchID = importBatchId,
                SourceRowNumber = row.RowNumber(),
                ImportedAt = DateTime.UtcNow
            };
        }

        private static string GetRequiredText(IXLRow row, int columnNumber, string fieldName)
        {
            var value = row.Cell(columnNumber).GetString().Trim();

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"{fieldName} 값은 필수입니다.");
            }

            return value;
        }

        private static int GetNonNegativeInt(
            IXLRow row,
            int columnNumber,
            string fieldName)
        {
            var rawValue = row.Cell(columnNumber).GetFormattedString().Trim();

            var isValid = int.TryParse(
                rawValue,
                NumberStyles.Integer | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out var value);

            if (!isValid || value < 0)
            {
                throw new InvalidOperationException(
                    $"{fieldName} 값은 0 이상의 정수여야 합니다.");
            }

            return value;
        }

        private static DateOnly GetDateOnly(
            IXLRow row,
            int columnNumber,
            string fieldName)
        {
            var cell = row.Cell(columnNumber);

            if (cell.TryGetValue<DateTime>(out var dateTime))
            {
                return DateOnly.FromDateTime(dateTime);
            }

            if (DateTime.TryParse(
                cell.GetFormattedString(),
                CultureInfo.CurrentCulture,
                DateTimeStyles.None,
                out dateTime))
            {
                return DateOnly.FromDateTime(dateTime);
            }

            throw new InvalidOperationException(
                $"{fieldName} 값은 올바른 날짜여야 합니다.");
        }
    }
}
