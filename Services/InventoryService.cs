using mes_server.Data;
using mes_server.Models.DTOs.Inventory;
using mes_server.Models.History;
using mes_server.Models.MasterData;
using mes_server.Models.Production;
using mes_server.Repositories.Interface.Generic;
using mes_server.Services.Interface;

namespace mes_server.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IGenericRepository<ProductMaster> _productRepository;
        private readonly IGenericRepository<BOM> _bomRepository;
        private readonly IGenericRepository<WorkOrder> _workOrderRepository;
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly MESDbContext _context;

        public InventoryService(
            MESDbContext context,
            IGenericRepository<ProductMaster> productRepository,
            IGenericRepository<BOM> bomRepository,
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

        public async Task ConsumeMaterialByProcessAsync(int workOrderId, int processId, int productionQty)
        {
            var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId);
            if (workOrder == null) throw new KeyNotFoundException("생산지시서를 찾을 수 없습니다.");
            
            var boms = await _bomRepository.FindAsync(b => b.ProductID == workOrder.ProductID && b.ProcessID == processId);

            foreach (var bom in boms)
            {
                var product = await _productRepository.GetByIdAsync(bom.ChildProductID);
                if (product == null) throw new KeyNotFoundException($"품목을 찾을 수 없음: {bom.ChildProductID}");

                if (product.StockQty < (bom.RequiredQty * productionQty))
                    throw new InvalidOperationException($"재고 부족: {bom.ChildProductID}");

                product.StockQty -= (bom.RequiredQty * productionQty);
            }
            await _context.SaveChangesAsync();
        }

        public async Task ReceiveFinishedProductAsync(int workOrderId, int productionQty)
        {
            var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId);
            if (workOrder == null) throw new KeyNotFoundException("생산지시서를 찾을 수 없습니다.");

            var product = await _productRepository.GetByIdAsync(workOrder.ProductID);
            if (product != null)
            {
                product.StockQty += productionQty;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ProductMaster>> GetLowStockMaterialsAsync()
        {
            var products = await _productRepository.GetAllAsync();
            return products.Where(m => m.StockQty <= m.SafetyStock);
        }

        public async Task<bool> CheckMaterialAvailabilityAsync(string productId, int targetQty)
        {
            var boms = await _bomRepository.FindAsync(b => b.ProductID == productId);
            foreach (var bom in boms)
            {
                var material = await _productRepository.GetByIdAsync(bom.ChildProductID);
                if (material == null || material.StockQty < (bom.RequiredQty * targetQty))
                    return false;
            }
            return true;
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