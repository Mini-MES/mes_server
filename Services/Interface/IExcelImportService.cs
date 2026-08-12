using mes_server.Models.DTOs.Analytics;

namespace mes_server.Services.Interface
{
    public interface IExcelImportService
    {
        Task<ExcelImportResultDto> ImportDailyEquipmentOeeAsync(
            Stream fileStream,
            string sourceFileName,
            bool replaceExisting = false);
    }
}
