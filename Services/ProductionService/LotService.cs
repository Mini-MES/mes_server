using mes_server.Models.Enum;
using mes_server.Models.Production;
using mes_server.Repositories.Interface.Production;
using Microsoft.EntityFrameworkCore;

namespace mes_server.Services.ProductionService
{
    public class LotService : ILotService
    {
        private readonly ILotRepository _lotRepository;

        public LotService(ILotRepository lotRepository)
        {
            _lotRepository = lotRepository;
        }

        public async Task ChangeLotProcessAsync(string lotId, int nextProcessId)
        {
            var lot = await _lotRepository.GetByIdAsync(lotId);

            if (lot == null)
            {
                throw new KeyNotFoundException("존재하지 않는 Lot입니다.");
            }

            if (lot.Status == LotStatus.HOLD)
            {
                throw new InvalidOperationException("보류(HOLD) 상태의 Lot은 공정을 이동할 수 없습니다. 보류 해제 또는 재작업 처리가 필요합니다.");
            }

            //if (!await IsOrderValid(lot.CurrentProcessID, nextProcessId))
            //{
            //    throw new InvalidOperationException("잘못된 공정 순서입니다.");
            //}

            lot.CurrentProcessID = nextProcessId;
            await _lotRepository.SaveChangesAsync();
        }

        public async Task<Lot> GetLotByIdAsync(string lotId)
        {
            var lot = await _lotRepository.GetByIdAsync(lotId);

            if (lot == null)
            {
                throw new KeyNotFoundException($"LOT ID: {lotId}를 찾을 수 없습니다.");
            }

            return lot;
        }

        public async Task<string> GenerateUniqueLotIdAsync()
        {
            string lotId;
            do
            {
                lotId = GenerateLotId();
            } while (await _lotRepository.GetByIdAsync(lotId) != null);

            return lotId;
        }

        public async Task<IEnumerable<Lot>> GetAllLotsAsync()
        {
            return await _lotRepository.GetAllAsync();
        }

        private string GenerateLotId()
        {
            var random = new Random();

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            var stringPart = new string(Enumerable.Repeat(chars, 4)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            var numberPart = random.Next(100).ToString("D2");

            return stringPart + numberPart;
        }

    }
}
