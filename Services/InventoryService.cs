using mes_server.Data;
using mes_server.Models.MasterData;
using mes_server.Models.Production;
using mes_server.Repositories.Interface.Generic;
using mes_server.Services.Interface;

namespace mes_server.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IGenericRepository<RawMaterial> _materialRepository;
        private readonly IGenericRepository<ProductMaster> _productRepository;
        private readonly IGenericRepository<BOM> _bomRepository;
        private readonly IGenericRepository<WorkOrder> _workOrderRepository;
        private readonly MESDbContext _context;

        public InventoryService(
            MESDbContext context,
            IGenericRepository<RawMaterial> materialRepository,
            IGenericRepository<ProductMaster> productRepository,
            IGenericRepository<BOM> bomRepository,
            IGenericRepository<WorkOrder> workOrderRepository)
        {
            _context = context;
            _materialRepository = materialRepository;
            _productRepository = productRepository;
            _bomRepository = bomRepository;
            _workOrderRepository = workOrderRepository;
        }

        public async Task ConsumeMaterialByProcessAsync(int workOrderId, int processId, int productionQty)
        {
            var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId);
            if (workOrder == null) throw new KeyNotFoundException("생산지시서를 찾을 수 없습니다.");

            var boms = await _bomRepository.FindAsync(b => b.ProductID == workOrder.ProductID && b.ProcessID == processId);

            foreach (var bom in boms)
            {
                var material = await _materialRepository.GetByIdAsync(bom.MaterialID);
                if (material != null)
                {
                    material.StockQty -= (bom.RequiredQty * productionQty);
                }
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

        public async Task<IEnumerable<RawMaterial>> GetLowStockMaterialsAsync()
        {
            var materials = await _materialRepository.GetAllAsync();
            return materials.Where(m => m.StockQty <= m.SafetyStock);
        }

        public async Task<bool> CheckMaterialAvailability(string productId, int targetQty)
        {
            var boms = await _bomRepository.FindAsync(b => b.ProductID == productId);
            foreach (var bom in boms)
            {
                var material = await _materialRepository.GetByIdAsync(bom.MaterialID);
                if (material == null || material.StockQty < (bom.RequiredQty * targetQty))
                    return false;
            }
            return true;
        }

        public async Task ShipFinishedProductAsync(string productId, int quantity)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) throw new KeyNotFoundException("제품을 찾을 수 없습니다.");

            if (product.StockQty < quantity)
            {
                throw new InvalidOperationException($"재고 부족: 출하량({quantity})이 현재 재고({product.StockQty})보다 많습니다.");
            }

            product.StockQty -= quantity;

            //TODO : 출하 기록 저장 

            await _context.SaveChangesAsync();
        }
    }
}