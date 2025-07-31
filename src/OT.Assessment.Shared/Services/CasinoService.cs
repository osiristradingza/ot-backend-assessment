using Microsoft.Extensions.Logging;
using OT.Assessment.Shared.DTOs;
using OT.Assessment.Shared.Models;
using OT.Assessment.Shared.Repositories;

namespace OT.Assessment.Shared.Services;

public class CasinoService : ICasinoService
{
    private readonly ICasinoRepository _repository;
    private readonly ILogger<CasinoService> _logger;

    public CasinoService(ICasinoRepository repository, ILogger<CasinoService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<long> ProcessCasinoWagerAsync(CasinoWagerDto wagerDto)
    {
        try
        {
            _logger.LogDebug("Processing casino wager: {WagerId}", wagerDto.WagerId);

            // Parse GUIDs
            if (!Guid.TryParse(wagerDto.WagerId, out var wagerId))
                throw new ArgumentException("Invalid WagerId format");
            if (!Guid.TryParse(wagerDto.AccountId, out var accountId))
                throw new ArgumentException("Invalid AccountId format");
            if (!Guid.TryParse(wagerDto.TransactionId, out var transactionId))
                throw new ArgumentException("Invalid TransactionId format");
            if (!Guid.TryParse(wagerDto.BrandId, out var brandId))
                throw new ArgumentException("Invalid BrandId format");
            if (!Guid.TryParse(wagerDto.ExternalReferenceId, out var externalReferenceId))
                throw new ArgumentException("Invalid ExternalReferenceId format");
            if (!Guid.TryParse(wagerDto.TransactionTypeId, out var transactionTypeId))
                throw new ArgumentException("Invalid TransactionTypeId format");

            // Get or create player
            await _repository.GetOrCreatePlayerAsync(accountId, wagerDto.Username);

            // Get or create provider and game
            var providerId = await _repository.GetOrCreateProviderAsync(wagerDto.Provider);
            var gameId = await _repository.GetOrCreateGameAsync(wagerDto.GameName, wagerDto.Theme, providerId);

            // Create casino wager entity
            var wager = new CasinoWager
            {
                WagerId = wagerId,
                AccountId = accountId,
                GameId = gameId,
                TransactionId = transactionId,
                BrandId = brandId,
                ExternalReferenceId = externalReferenceId,
                TransactionTypeId = transactionTypeId,
                Amount = (decimal)wagerDto.Amount,
                CreatedDateTime = wagerDto.CreatedDateTime,
                NumberOfBets = wagerDto.NumberOfBets,
                CountryCode = wagerDto.CountryCode,
                SessionData = wagerDto.SessionData,
                Duration = wagerDto.Duration,
                ProcessedDate = DateTime.UtcNow
            };

            // Save the wager
            var id = await _repository.SaveCasinoWagerAsync(wager);
            
            _logger.LogDebug("Successfully processed casino wager: {WagerId}, Database ID: {Id}", wagerDto.WagerId, id);
            
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process casino wager: {WagerId}", wagerDto.WagerId);
            throw;
        }
    }

    public async Task<PlayerWagersResponseDto> GetPlayerWagersAsync(Guid accountId, int page, int pageSize)
    {
        try
        {
            _logger.LogDebug("Getting player wagers for account: {AccountId}, Page: {Page}, PageSize: {PageSize}", 
                accountId, page, pageSize);
                
            return await _repository.GetPlayerWagersAsync(accountId, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get player wagers for account: {AccountId}", accountId);
            throw;
        }
    }

    public async Task<List<TopSpenderDto>> GetTopSpendersAsync(int count)
    {
        try
        {
            _logger.LogDebug("Getting top {Count} spenders", count);
            
            return await _repository.GetTopSpendersAsync(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top spenders");
            throw;
        }
    }
}