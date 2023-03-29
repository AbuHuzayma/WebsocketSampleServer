using EdiyaGameWebsocket.Entities;
using System.Numerics;
using Volo.Abp.Domain.Repositories;

namespace EdiyaGameWebsocket.Data.Repository
{
    public class PlayerRepository : IPlayerRepository
    {
        private readonly IRepository<Player> _playerRepository;
        public PlayerRepository(IRepository<Player> playerRepository) 
        { 
            _playerRepository = playerRepository;
        }

        public async Task<Player> AddOrUpdatePlayerAsync(Player player)
        {
            var old = await _playerRepository.FindAsync(p=>p.Id == player.Id);
            if (old != null)
            {
                old.IsOnline = player.IsOnline;
                return await _playerRepository.UpdateAsync(old, true);
            }
            else
                return await _playerRepository.InsertAsync(player,true);
        }

        public async Task<List<Player>> GetAllPlayersAsync()
        {
            return await _playerRepository.GetListAsync();
        }

        public async Task<Player> GetPlayerAsync(int playerId)
        {
            return await _playerRepository.FindAsync(p => p.Id == playerId);
        }
    }
}
