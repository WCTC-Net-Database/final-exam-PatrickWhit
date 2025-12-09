using ConsoleRpgEntities.Models.Abilities.PlayerAbilities;
using ConsoleRpgEntities.Models.Attributes;
using System.ComponentModel.DataAnnotations;
using ConsoleRpgEntities.Models.Equipments;
using ConsoleRpgEntities.Models.Rooms;

namespace ConsoleRpgEntities.Models.Characters
{
    public class Player : ITargetable, IPlayer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Experience { get; set; }
        public int Health { get; set; }

        // Foreign keys
        public int? EquipmentId { get; set; }
        public int? RoomId { get; set; }

        // Navigation properties
        public virtual Equipment Equipment { get; set; }
        public virtual ICollection<Ability> Abilities { get; set; }
        public virtual Room Room { get; set; }

        public Player()
        {
            Abilities = new List<Ability>();
        }

        public string Attack(ITargetable target)
        {
            // Player-specific attack logic
            if (Equipment == null)
            {
                Console.WriteLine($"{Name} attacks {target.Name} with their fist dealing 1 damage!");
                target.Health -= 1;
                return ($"{target.Name} has {target.Health} health remaining.");
            }
            else
            {
                Console.WriteLine($"{Name} attacks {target.Name} with a {Equipment.Weapon.Name} dealing {Equipment.Weapon.Attack} damage!");
                target.Health -= Equipment.Weapon.Attack;
                return ($"{target.Name} has {target.Health} health remaining.");
            }
        }

        public void UseAbility(IAbility ability, ITargetable target)
        {
            if (Abilities.Contains(ability))
            {
                ability.Activate(this, target);
            }
            else
            {
                Console.WriteLine($"{Name} does not have the ability {ability.Name}!");
            }
        }
    }
}
