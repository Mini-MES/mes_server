using mes_server.Models.DTOs.MasterData;
using mes_server.Models.Enum;
using mes_server.Models.MasterData;
using mes_server.Repositories.Interface.Generic;
using mes_server.Services.Interface;

namespace mes_server.Services
{
    public class MasterDataService : IMasterDataService
    {
        private readonly IGenericRepository<BOM> _bomRepository;
        private readonly IGenericRepository<ProcessMaster> _processMasterRepository;
        private readonly IGenericRepository<BadReasonMaster> _badReasonMasterRepository;

        public MasterDataService(
            IGenericRepository<BOM> bomRepository,
            IGenericRepository<ProcessMaster> processMasterRepository,
            IGenericRepository<BadReasonMaster> badReasonMasterRepository)
        {
            _bomRepository = bomRepository;
            _processMasterRepository = processMasterRepository;
            _badReasonMasterRepository = badReasonMasterRepository;
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

        public async Task<ProcessMaster> CreateProcessAsync(ProcessCreateDto dto)
        {
            var process = new ProcessMaster
            {
                ProcessName = dto.ProcessName,
                SequenceOrder = dto.SequenceOrder
            };
            await _processMasterRepository.CreateAsync(process);

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

            return reason;
        }

        public async Task<BOM> AddBomAsync(BOMCreateDto dto)
        {
            var bom = new BOM
            {
                ProductID = dto.ProductID,
                MaterialID = dto.MaterialID,
                ProcessID = dto.ProcessID,
                RequiredQty = dto.RequiredQty
            };
            await _bomRepository.CreateAsync(bom);
            return bom;

        }

        public async Task<ProcessMaster> UpdateProcessAsync(int id, ProcessUpdateDto dto)
        {
            var process = await _processMasterRepository.GetByIdAsync(id);
            if (process == null) throw new KeyNotFoundException("공정을 찾을 수 없습니다.");

            process.ProcessName = dto.ProcessName;
            process.SequenceOrder = dto.SequenceOrder;

            await _processMasterRepository.UpdateAsync(process);

            return process;
        }
    }
}
