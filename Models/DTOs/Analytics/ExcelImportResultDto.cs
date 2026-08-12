namespace mes_server.Models.DTOs.Analytics
{
    public class ExcelImportResultDto
    {
            public int TotalRows { get; set; }
            public int ImportRows { get; set; }
            public int UpdateRows { get; set; }
            public string ImportBatchID { get; set; } = null!;
            public List<ExcelImportErrorDto> Errors { get; set; } = new List<ExcelImportErrorDto>();
    }

    public class ExcelImportErrorDto
    {
        public int RowNumber { get; set; }
        public string ErrorMessage { get; set; } = null!;
    }
}
