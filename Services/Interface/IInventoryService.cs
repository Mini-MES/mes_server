using mes_server.Models.MasterData;

namespace mes_server.Services.Interface
{
        public interface IInventoryService
        {
            Task UpdateStockAsync(RawMaterial material);
            Task ConsumeMaterialByProcessAsync(int workOrderId, int processId, int productionQty);

            Task ReceiveFinishedProductAsync(int workOrderId, int productionQty);
            Task<IEnumerable<RawMaterial>> GetLowStockMaterialsAsync();

            Task<bool> CheckMaterialAvailabilityAsync(string productId, int targetQty);
            Task<IEnumerable<RawMaterial>> SearchMaterialsAsync(string keyword);
            
            Task ShipFinishedProductAsync(string productId, int workOrderId, int quantity, string destination);
        }
}
