using ConsoleRpgEntities.Data;
using ConsoleRpgEntities.Models.Characters.Monsters;

namespace ConsoleRpg.Services
{
    public class MonsterService
    {
        private readonly GameContext _context;

        public MonsterService(GameContext context)
        {
            _context = context;
        }

        public Monster? GetFirstMonsterInRoom(int roomId)
        {
            return _context.Monsters.Where(m => m.RoomId == roomId).FirstOrDefault();
        }
    }
}
