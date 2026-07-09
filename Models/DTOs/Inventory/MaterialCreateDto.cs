namespace mes_server.Models.DTOs.Inventory
{
    public class MaterialCreateDto
    {
        public string MaterialID { get; set; } = null!;
        public string MaterialName { get; set; } = null!;
        public int SafetyStock { get; set; }
    }
}
