using ConsoleRpgEntities.Data;

namespace ConsoleRpg.Services
{
    public class MonsterService
    {
        private readonly GameContext _context;

        private ConsoleRpgEntities.Models.Characters.Monsters.Monster? GetFirstMonster()
        {
            return _currentRoom.Monsters.FirstOrDefault();
        }
    }
}
