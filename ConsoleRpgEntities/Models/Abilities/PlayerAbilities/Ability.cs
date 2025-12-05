using ConsoleRpgEntities.Models.Attributes;
using ConsoleRpgEntities.Models.Characters;

namespace ConsoleRpgEntities.Models.Abilities.PlayerAbilities
{
    public abstract class Ability : IAbility
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string AbilityType { get; set; }
        public int Damage { get; set; }

        public virtual ICollection<Player> Players { get; set; }

        protected Ability()
        {
            Players = new List<Player>();
        }

        public abstract void Activate(IPlayer user, ITargetable target);
    }
}
