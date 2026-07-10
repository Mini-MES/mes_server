using mes_server.Data;
using mes_server.Models.DTOs.MasterData;
using mes_server.Models.Enum;
using mes_server.Models.MasterData;
using mes_server.Repositories.Interface.Generic;
using mes_server.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace mes_server.Services
{
    public class MasterDataService : IMasterDataService
    {
        private readonly IGenericRepository<BOM> _bomRepository;
        private readonly IGenericRepository<ProcessMaster> _processMasterRepository;
        private readonly IGenericRepository<BadReasonMaster> _badReasonMasterRepository;
        private readonly IGenericRepository<ProductMaster> _productRepository;
        private readonly MESDbContext _context;

        public MasterDataService(
            MESDbContext context,
            IGenericRepository<BOM> bomRepository,
            IGenericRepository<ProcessMaster> processMasterRepository,
            IGenericRepository<BadReasonMaster> badReasonMasterRepository,
            IGenericRepository<ProductMaster> productRepository)
        {
            _context = context;
            _bomRepository = bomRepository;
            _processMasterRepository = processMasterRepository;
            _badReasonMasterRepository = badReasonMasterRepository;
            _productRepository = productRepository;
        }


        public async Task<IEnumerable<BadReasonMaster>> GetBadReasonListAsync()
        {
            return await _badReasonMasterRepository.GetAllAsync();
        }

        public async Task<IEnumerable<BadReasonMaster>> GetBadReasonByCodeAsync(ReasonCode code)
        {
            return await _badReasonMasterRepository.FindAsync(b => b.ReasonCode == code);
        }

        public async Task<ProcessMaster?> GetProcessBySequenceAsync(int sequence)
        {
            var process = await _processMasterRepository.FindAsync(p => p.SequenceOrder == sequence);
            return process?.SingleOrDefault();
        }

        public async Task<IEnumerable<ProcessMaster>> GetProcessListAsync()
        {
            var processes = await _processMasterRepository.GetAllAsync();
            return processes.OrderBy(p => p.SequenceOrder);
        }

        public async Task<IEnumerable<BOM>> GetProductBOMAsync(string productId)
        {
            return await _bomRepository.FindAsync(b => b.ProductID.Equals(productId));
        }

        public async Task<bool> ValidateBOMAsync(string productId)
        {
            var boms = await GetProductBOMAsync(productId);
            return boms != null && boms.Any();
        }
        public async Task<bool> DeleteBomAsync(string productId, string childProductId, int processId)
        {
            var bom = await _bomRepository.FindAsync(b =>
                b.ProductID == productId &&
                b.ChildProductID == childProductId &&
                b.ProcessID == processId);

            var entity = bom.SingleOrDefault();
            if (entity == null) return false;

            await _bomRepository.DeleteAsync(entity);
            await _bomRepository.SaveChangesAsync();
            return true;
        }


        public async Task<ProcessMaster> CreateProcessAsync(ProcessCreateDto dto)
        {
            var process = new ProcessMaster
            {
                ProcessName = dto.ProcessName,
                SequenceOrder = dto.SequenceOrder
            };
            await _processMasterRepository.CreateAsync(process);
            await _processMasterRepository.SaveChangesAsync();

            return process;
        }

        public async Task<BadReasonMaster> CreateBadReasonAsync(BadReasonCreateDto dto)
        {
            var reason = new BadReasonMaster
            {
                ReasonCode = dto.ReasonCode,
                ReasonDescription = dto.Description
            };
            await _badReasonMasterRepository.CreateAsync(reason);
            await _badReasonMasterRepository.SaveChangesAsync();

            return reason;
        }

        public async Task<BOM> AddBomAsync(BOMCreateDto dto)
        {
            var bom = new BOM
            {
                ProductID = dto.ProductID,
                ChildProductID = dto.ChildProductID,
                ProcessID = dto.ProcessID,
                RequiredQty = dto.RequiredQty
            };
            await _bomRepository.CreateAsync(bom);
            await _bomRepository.SaveChangesAsync();
            return bom;

        }

        public async Task<ProcessMaster> UpdateProcessAsync(int id, ProcessUpdateDto dto)
        {
            var process = await _processMasterRepository.GetByIdAsync(id);
            if (process == null) throw new KeyNotFoundException("공정을 찾을 수 없습니다.");

            process.ProcessName = dto.ProcessName;
            process.SequenceOrder = dto.SequenceOrder;

            await _processMasterRepository.UpdateAsync(process);
            await _processMasterRepository.SaveChangesAsync();

            return process;
        }

        public async Task<ProductMaster> CreateProductAsync(ProductCreateDto dto)
        {
            if(await _productRepository.GetByIdAsync(dto.ProductID) != null)
            {
                throw new InvalidOperationException($"Product with ID '{dto.ProductID}' already exists.");
            }

            var product = new ProductMaster
            {
                ProductID = dto.ProductID,
                ProductName = dto.ProductName,
                ItemType = dto.ItemType,
                StockQty = dto.StockQty,
                SafetyStock = dto.SafetyStock
            };

            await _productRepository.CreateAsync(product);
            await _productRepository.SaveChangesAsync();
            return product;
        }

        public async Task<ProductMaster> UpdateProductAsync(string productId, ProductUpdateDto dto)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) throw new KeyNotFoundException("제품을 찾을 수 없습니다.");
            product.ProductName = dto.ProductName;
            product.ItemType = dto.ItemType;
            product.StockQty = dto.StockQty;
            product.SafetyStock = dto.SafetyStock;
            await _productRepository.UpdateAsync(product);
            await _productRepository.SaveChangesAsync();
            return product;
        }   

        private ProductResponseDto MapToProductResponseDto(ProductMaster p)
        {
            return new ProductResponseDto
            {
                ProductID = p.ProductID,
                ProductName = p.ProductName,
                StockQty = p.StockQty,
                SafetyStock = p.SafetyStock,
                ItemType = p.ItemType,
                BOMs = p.BOMs.Select(b => new BOMResponseDto
                {
                    ProductID = b.ProductID,
                    ChildProductID = b.ChildProductID,
                    ChildProductName = b.ChildProduct?.ProductName ?? string.Empty,
                    RequiredQty = b.RequiredQty,
                    ProcessID = b.ProcessID,
                    ProcessName = b.Process?.ProcessName
                }).ToList()
            };
        }

        public async Task<IEnumerable<ProductResponseDto>> GetProductsWithBOMAsync()
        {
            var products = await _context.ProductMasters
                .Include(p => p.BOMs)
                    .ThenInclude(b => b.ChildProduct)
                .Include(p => p.BOMs)
                    .ThenInclude(b => b.Process)
                .ToListAsync();

            return products.Select(MapToProductResponseDto);
        }

        public async Task<ProductResponseDto?> GetProductWithBOMAsync(string productId)
        {
            var product = await _context.ProductMasters
                .Include(p => p.BOMs)
                    .ThenInclude(b => b.ChildProduct)
                .Include(p => p.BOMs)
                    .ThenInclude(b => b.Process)
                .FirstOrDefaultAsync(p => p.ProductID == productId);

            return product != null ? MapToProductResponseDto(product) : null;
        }
    }
}
