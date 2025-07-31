using Microsoft.EntityFrameworkCore;
using OT.Assessment.Shared.Data;
using OT.Assessment.Shared.DTOs;
using OT.Assessment.Shared.Models;

namespace OT.Assessment.Shared.Repositories;

public class CasinoRepository : ICasinoRepository
{
    private readonly CasinoDbContext _context;

    public CasinoRepository(CasinoDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> GetOrCreatePlayerAsync(Guid accountId, string username)
    {
        var player = await _context.Players.FirstOrDefaultAsync(p => p.AccountId == accountId);
        
        if (player == null)
        {
            player = new Player
            {
                AccountId = accountId,
                Username = username,
                CreatedDate = DateTime.UtcNow
            };
            
            _context.Players.Add(player);
            await _context.SaveChangesAsync();
        }
        
        return player.AccountId;
    }

    public async Task<int> GetOrCreateProviderAsync(string providerName)
    {
        var provider = await _context.Providers.FirstOrDefaultAsync(p => p.Name == providerName);
        
        if (provider == null)
        {
            provider = new Provider
            {
                Name = providerName,
                CreatedDate = DateTime.UtcNow
            };
            
            _context.Providers.Add(provider);
            await _context.SaveChangesAsync();
        }
        
        return provider.Id;
    }

    public async Task<int> GetOrCreateGameAsync(string gameName, string theme, int providerId)
    {
        var game = await _context.Games.FirstOrDefaultAsync(g => g.Name == gameName && g.ProviderId == providerId);
        
        if (game == null)
        {
            game = new Game
            {
                Name = gameName,
                Theme = theme,
                ProviderId = providerId,
                CreatedDate = DateTime.UtcNow
            };
            
            _context.Games.Add(game);
            await _context.SaveChangesAsync();
        }
        
        return game.Id;
    }

    public async Task<long> SaveCasinoWagerAsync(CasinoWager wager)
    {
        _context.CasinoWagers.Add(wager);
        await _context.SaveChangesAsync();
        return wager.Id;
    }

    public async Task<PlayerWagersResponseDto> GetPlayerWagersAsync(Guid accountId, int page, int pageSize)
    {
        var offset = (page - 1) * pageSize;
        
        var totalCount = await _context.CasinoWagers
            .Where(w => w.AccountId == accountId)
            .CountAsync();
            
        var wagers = await _context.CasinoWagers
            .Include(w => w.Game)
            .ThenInclude(g => g.Provider)
            .Where(w => w.AccountId == accountId)
            .OrderByDescending(w => w.CreatedDateTime)
            .Skip(offset)
            .Take(pageSize)
            .Select(w => new PlayerWagerDto
            {
                WagerId = w.WagerId.ToString(),
                Game = w.Game.Name,
                Provider = w.Game.Provider.Name,
                Amount = w.Amount,
                CreatedDate = w.CreatedDateTime
            })
            .ToListAsync();
            
        return new PlayerWagersResponseDto
        {
            Data = wagers,
            Page = page,
            PageSize = pageSize,
            Total = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public async Task<List<TopSpenderDto>> GetTopSpendersAsync(int count)
    {
        return await _context.CasinoWagers
            .Include(w => w.Player)
            .GroupBy(w => new { w.Player.AccountId, w.Player.Username })
            .Select(g => new TopSpenderDto
            {
                AccountId = g.Key.AccountId.ToString(),
                Username = g.Key.Username,
                TotalAmountSpend = g.Sum(w => w.Amount)
            })
            .OrderByDescending(s => s.TotalAmountSpend)
            .Take(count)
            .ToListAsync();
    }
}