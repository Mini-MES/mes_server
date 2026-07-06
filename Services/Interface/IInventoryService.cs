<<<<<<< HEAD
﻿namespace mes_server.Services.Interface
{
=======
﻿using mes_server.Models.MasterData;

namespace mes_server.Services.Interface
{
    public interface IInventoryService
    {
>>>>>>> 02a2052 (feat: inventoryService)
        public interface IInventoryService
        {
            Task ConsumeMaterialByProcessAsync(int workOrderId, int processId, int productionQty);

            Task ReceiveFinishedProductAsync(int workOrderId, int productionQty);

<<<<<<< HEAD
            Task<bool> CheckMaterialAvailabilityAsync(string productId, int targetQty);

            Task ShipFinishedProductAsync(string productId, int workOrderId, int quantity, string destination);
        }
=======
            Task<bool> CheckMaterialAvailability(string productId, int targetQty);

            Task ShipFinishedProductAsync(string productId, int quantity);
        }
    }
>>>>>>> 02a2052 (feat: inventoryService)
}
