namespace mes_server.Services.Interface
{


    public interface IInventoryService
    {
        public interface IInventoryService
        {
            Task ConsumeMaterialByProcessAsync(int workOrderId, int processId, int productionQty);

            Task ReceiveFinishedProductAsync(int workOrderId, int productionQty);

            Task<bool> CheckMaterialAvailabilityAsync(string productId, int targetQty);

            Task ShipFinishedProductAsync(string productId, int workOrderId, int quantity, string destination);
        }
    }
}
