using mes_server.Data;
using mes_server.Models.DTOs.Inventory;
using mes_server.Models.Enum;
using mes_server.Models.History;
using mes_server.Models.MasterData;
using mes_server.Models.Production;
using mes_server.Repositories.Interface.Generic;
using mes_server.Repositories.Interface.MasterData;

namespace mes_server.Services.InventoryService
{
    public class InventoryService : IInventoryService
    {
        private readonly IGenericRepository<ProductMaster> _productRepository;
        private readonly IBOMRepository _bomRepository;
        private readonly IGenericRepository<WorkOrder> _workOrderRepository;
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly MESDbContext _context;

        public InventoryService(
            MESDbContext context,
            IGenericRepository<ProductMaster> productRepository,
            IBOMRepository bomRepository,
            IGenericRepository<WorkOrder> workOrderRepository,
            IGenericRepository<Shipment> shipmentRepository)
        {
            _context = context;
            _productRepository = productRepository;
            _bomRepository = bomRepository;
            _workOrderRepository = workOrderRepository;
            _shipmentRepository = shipmentRepository;
        }

        public async Task UpdateStockAsync(string productId, StockUpdateDto dto)
        {
            var existingProduct = await _productRepository.GetByIdAsync(productId);
            if (existingProduct == null) throw new KeyNotFoundException("품목을 찾을 수 없습니다.");

            existingProduct.StockQty = dto.StockQty;
            existingProduct.ProductName = dto.MaterialName;
            existingProduct.SafetyStock = dto.SafetyStock;

            await _context.SaveChangesAsync();
        }

        public async Task ConsumeMaterialByProcessAsync(int workOrderId, int processId, int productionQty, bool autoSave = true)
        {
            var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId);
            if (workOrder == null) throw new KeyNotFoundException("생산지시서를 찾을 수 없습니다.");
            
            var allBoms = await _bomRepository.GetAllBomsByProductIdAsync(workOrder.ProductID);

            var targetBoms = allBoms.Where(b => b.ProcessID == processId).ToList();
            if (!targetBoms.Any() && (processId == 1 || processId == 2))
            {
                targetBoms = allBoms.ToList();
            }

            foreach (var bom in targetBoms)
            {
                var product = await _productRepository.GetByIdAsync(bom.ChildProductID);
                if (product == null) continue;

                int deductQty = bom.RequiredQty * productionQty;
                product.StockQty = Math.Max(0, product.StockQty - deductQty);
            }

            if (autoSave)
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task ReceiveFinishedProductAsync(int workOrderId, int productionQty, bool autoSave = true)
        {
            var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId);
            if (workOrder == null) throw new KeyNotFoundException("생산지시서를 찾을 수 없습니다.");

            var product = await _productRepository.GetByIdAsync(workOrder.ProductID);
            if (product != null)
            {
                product.StockQty += productionQty;
            }

            if (autoSave)
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task ReceiveSemiFinishedProductAsync(int workOrderId, int processId, int productionQty, bool autoSave = true)
        {
            var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId);
            if (workOrder == null) throw new KeyNotFoundException("생산지시서를 찾을 수 없습니다.");

            var boms = await _bomRepository.GetBomsByProcessIdAsync(processId);
            var currentBom = boms.FirstOrDefault();

            if (currentBom != null)
            {
                var outputProduct = await _productRepository.GetByIdAsync(currentBom.ProductID);
                if (outputProduct != null)
                {
                    outputProduct.StockQty += productionQty;
                }
            }
            else
            {
                await ReceiveFinishedProductAsync(workOrderId, productionQty, autoSave);
                return;
            }

            if (autoSave)
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<ProductMaster>> GetLowStockMaterialsAsync()
        {
            var products = await _productRepository.GetAllAsync();
            return products.Where(m => m.StockQty <= m.SafetyStock);
        }

        public async Task<bool> CheckMaterialAvailabilityAsync(string productId, int targetQty)
        {
            var leafMaterials = await GetLeafRawMaterialsAsync(productId);

            if (leafMaterials.Any())
            {
                foreach (var (materialId, requiredQty) in leafMaterials)
                {
                    var material = await _productRepository.GetByIdAsync(materialId);
                    if (material != null && material.StockQty < (requiredQty * targetQty))
                        return false;
                }
                return true;
            }

            var boms = await _bomRepository.GetAllBomsByProductIdAsync(productId);
            foreach (var bom in boms)
            {
                var material = await _productRepository.GetByIdAsync(bom.ChildProductID);
                if (material != null && material.ItemType == ItemType.RawMaterial && material.StockQty < (bom.RequiredQty * targetQty))
                    return false;
            }
            return true;
        }

        private async Task<List<(string MaterialId, int RequiredQty)>> GetLeafRawMaterialsAsync(string productId)
        {
            var result = new List<(string, int)>();
            var queue = new Queue<(string ProductId, int Qty)>();
            queue.Enqueue((productId, 1));
            var visited = new HashSet<string> { productId };

            while (queue.Count > 0)
            {
                var (currProduct, currQty) = queue.Dequeue();
                var boms = await _bomRepository.GetAllBomsByProductIdAsync(currProduct);

                foreach (var bom in boms)
                {
                    var child = await _productRepository.GetByIdAsync(bom.ChildProductID);
                    if (child != null)
                    {
                        if (child.ItemType == ItemType.RawMaterial)
                        {
                            result.Add((child.ProductID, currQty * bom.RequiredQty));
                        }
                        else if (!visited.Contains(child.ProductID))
                        {
                            visited.Add(child.ProductID);
                            queue.Enqueue((child.ProductID, currQty * bom.RequiredQty));
                        }
                    }
                }
            }

            return result;
        }

        public async Task ShipFinishedProductAsync(string productId, int workOrderId, int quantity, string destination)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) throw new KeyNotFoundException("제품을 찾을 수 없습니다.");

            if (product.StockQty < quantity)
            {
                throw new InvalidOperationException($"재고 부족: 출하량({quantity})이 현재 재고({product.StockQty})보다 많습니다.");
            }

            product.StockQty -= quantity;

            var shipment = new Shipment
            {
                ProductID = productId,
                WorkOrderID = workOrderId,
                Quantity = quantity,
                Destination = destination,
                ShipmentDate = DateTime.Now,
                Product = product.ProductName
            };

            await _shipmentRepository.CreateAsync(shipment);

            await _context.SaveChangesAsync();
        }

        public async Task AdjustProductStockAsync(string productId, int amount)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) throw new KeyNotFoundException("제품을 찾을 수 없습니다.");

            product.StockQty += amount;

            await _context.SaveChangesAsync();
        }
    }
}