using EdiyaGameWebsocket.Entities;

namespace EdiyaGameWebsocket.Data.Repository
{
    public interface IPlayerRepository
    {
        Task<Player> GetPlayerAsync(int playerId);
        Task<List<Player>> GetAllPlayersAsync();
        Task<Player> AddOrUpdatePlayerAsync(Player player);
    }
}
