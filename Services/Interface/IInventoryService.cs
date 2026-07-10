using mes_server.Models.DTOs.Inventory;
using mes_server.Models.MasterData;

namespace mes_server.Services.Interface
{
        public interface IInventoryService
        {
            Task UpdateStockAsync(string productId, StockUpdateDto dto);
            Task ConsumeMaterialByProcessAsync(int workOrderId, int processId, int productionQty);

            Task ReceiveFinishedProductAsync(int workOrderId, int productionQty);
            Task<IEnumerable<ProductMaster>> GetLowStockMaterialsAsync();

            Task<bool> CheckMaterialAvailabilityAsync(string productId, int targetQty);
            
            Task ShipFinishedProductAsync(string productId, int workOrderId, int quantity, string destination);

            Task AdjustProductStockAsync(string productId, int amount);
        }
}
