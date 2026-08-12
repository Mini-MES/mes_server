using mes_server.Models.DTOs.Analytics;

namespace mes_server.Services.Interface
{
    public interface IExcelImportSerivce
    {
        Task<ExcelImportResultDto> ImportDetailEquipmentOEEAsync(Stream fileStream, string sourceFileName, bool replaceExisting = false);
    }
}
