using OT.Assessment.Shared.DTOs;
using OT.Assessment.Shared.Models;

namespace OT.Assessment.Shared.Services;

public interface ICasinoService
{
    Task<long> ProcessCasinoWagerAsync(CasinoWagerDto wagerDto);
    Task<PlayerWagersResponseDto> GetPlayerWagersAsync(Guid accountId, int page, int pageSize);
    Task<List<TopSpenderDto>> GetTopSpendersAsync(int count);
}