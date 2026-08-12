using mes_server.Data;
using mes_server.Models.DTOs.Analytics;
using mes_server.Services.Interface;

namespace mes_server.Services
{
    public class ExcelImportService : IExcelImportSerivce
    {
        private readonly MESDbContext _dbContext;

        public ExcelImportService(MESDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<ExcelImportResultDto> ImportDetailEquipmentOEEAsync(Stream fileStream, string sourceFileName, bool replaceExisting = false)
        {
            var result = new ExcelImportResultDto
            {
                ImportBatchID = Guid.NewGuid().ToString(),
            };

            using var workbook = new ClosedXML.Excel.XLWorkbook(fileStream);
            var worksheet = workbook.Worksheets.FirstOrDefault() ?? throw new InvalidOperationException("No worksheet found in the Excel file.");

            await Task.CompletedTask;

            return result;
        }
    }
}
