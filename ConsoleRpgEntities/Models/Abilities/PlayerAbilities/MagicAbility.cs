using ConsoleRpgEntities.Models.Attributes;
using ConsoleRpgEntities.Models.Characters;

namespace ConsoleRpgEntities.Models.Abilities.PlayerAbilities
{
    public class MagicAbility : Ability
    {
        public override void Activate(IPlayer user, ITargetable target)
        {
            // Shove ability logic
            Console.WriteLine($"{user.Name} targets {target.Name}, dealing {Damage} damage!");
        }
    }
}
