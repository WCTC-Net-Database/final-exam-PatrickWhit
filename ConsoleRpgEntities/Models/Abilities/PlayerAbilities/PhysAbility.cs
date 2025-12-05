using ConsoleRpgEntities.Models.Attributes;
using ConsoleRpgEntities.Models.Characters;

namespace ConsoleRpgEntities.Models.Abilities.PlayerAbilities
{
    public class PhysAbility : Ability
    {
        public override void Activate(IPlayer user, ITargetable target)
        {
            // Shove ability logic
            Console.WriteLine($"{user.Name} attacks {target.Name}, dealing {Damage} damage!");
        }
    }
}
