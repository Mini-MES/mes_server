using mes_server.Models.Production;
using mes_server.Repositories.Interface.Generic;

namespace mes_server.Repositories.Interface.Production
{
    public interface ILotRepository : IGenericRepository<Lot> 
    {
        Task<Lot?> GetLotWithDetailsAsync(string lotId);
    }
}
