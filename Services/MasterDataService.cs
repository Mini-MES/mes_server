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
    }
}
