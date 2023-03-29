using Volo.Abp.Domain.Entities;

namespace EdiyaGameWebsocket.Entities
{
    public class Player : Entity<int>
    {
        public string Name { get; set; }
        public bool IsOnline { get; set; }
    }
}
