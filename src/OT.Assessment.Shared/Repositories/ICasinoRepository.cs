using OT.Assessment.Shared.DTOs;
using OT.Assessment.Shared.Models;

namespace OT.Assessment.Shared.Repositories;

public interface ICasinoRepository
{
    Task<Guid> GetOrCreatePlayerAsync(Guid accountId, string username);
    Task<int> GetOrCreateProviderAsync(string providerName);
    Task<int> GetOrCreateGameAsync(string gameName, string theme, int providerId);
    Task<long> SaveCasinoWagerAsync(CasinoWager wager);
    Task<PlayerWagersResponseDto> GetPlayerWagersAsync(Guid accountId, int page, int pageSize);
    Task<List<TopSpenderDto>> GetTopSpendersAsync(int count);
}