using mes_server.Models.Enum;
using mes_server.Models.MasterData;
using mes_server.Repositories.Interface.Generic;


namespace mes_server.Repositories.Interface.MasterData
{
    public interface IBadReasonMasterRepository : IGenericRepository<BadReasonMaster>
    {
        Task<IEnumerable<BadReasonMaster>> GetAllBadReasonMasterByCodeAsync(ReasonCode code);
    }
}
