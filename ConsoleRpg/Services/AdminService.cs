using ConsoleRpgEntities.Data;
using ConsoleRpgEntities.Models.Abilities.PlayerAbilities;
using ConsoleRpgEntities.Models.Characters;
using ConsoleRpgEntities.Models.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Spectre.Console;
using System.Numerics;

namespace ConsoleRpg.Services;

/// <summary>
/// Handles all admin/developer CRUD operations and advanced queries
/// Separated from GameEngine to follow Single Responsibility Principle
/// </summary>
public class AdminService
{
    private readonly GameContext _context;
    private readonly ILogger<AdminService> _logger;

    public AdminService(GameContext context, ILogger<AdminService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Basic CRUD Operations

    /// <summary>
    /// Add a new character to the database
    /// </summary>
    public void AddCharacter()
    {
        try
        {
            _logger.LogInformation("User selected Add Character");
            AnsiConsole.MarkupLine("[yellow]=== Add New Character ===[/]");

            var name = AnsiConsole.Ask<string>("Enter character [green]name[/]:");
            var health = AnsiConsole.Ask<int>("Enter [green]health[/]:");
            var experience = AnsiConsole.Ask<int>("Enter [green]experience[/]:");

            var player = new Player
            {
                Name = name,
                Health = health,
                Experience = experience
            };

            _context.Players.Add(player);
            _context.SaveChanges();

            _logger.LogInformation("Character {Name} added to database with Id {Id}", name, player.Id);
            AnsiConsole.MarkupLine($"[green]Character '{name}' added successfully![/]");
            Thread.Sleep(1000);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding character");
            AnsiConsole.MarkupLine($"[red]Error adding character: {ex.Message}[/]");
            PressAnyKey();
        }
    }

    /// <summary>
    /// Edit an existing character's properties
    /// </summary>
    public void EditCharacter()
    {
        try
        {
            _logger.LogInformation("User selected Edit Character");
            AnsiConsole.MarkupLine("[yellow]=== Edit Character ===[/]");

            var id = AnsiConsole.Ask<int>("Enter character [green]ID[/] to edit:");

            var player = _context.Players.Find(id);
            if (player == null)
            {
                _logger.LogWarning("Character with Id {Id} not found", id);
                AnsiConsole.MarkupLine($"[red]Character with ID {id} not found.[/]");
                return;
            }

            AnsiConsole.MarkupLine($"Editing: [cyan]{player.Name}[/]");

            if (AnsiConsole.Confirm("Update name?"))
            {
                player.Name = AnsiConsole.Ask<string>("Enter new [green]name[/]:");
            }

            if (AnsiConsole.Confirm("Update health?"))
            {
                player.Health = AnsiConsole.Ask<int>("Enter new [green]health[/]:");
            }

            if (AnsiConsole.Confirm("Update experience?"))
            {
                player.Experience = AnsiConsole.Ask<int>("Enter new [green]experience[/]:");
            }

            _context.SaveChanges();

            _logger.LogInformation("Character {Name} (Id: {Id}) updated", player.Name, player.Id);
            AnsiConsole.MarkupLine($"[green]Character '{player.Name}' updated successfully![/]");
            Thread.Sleep(1000);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing character");
            AnsiConsole.MarkupLine($"[red]Error editing character: {ex.Message}[/]");
            PressAnyKey();
        }
    }

    /// <summary>
    /// Display all characters in the database
    /// </summary>
    public void DisplayAllCharacters()
    {
        try
        {
            _logger.LogInformation("User selected Display All Characters");
            AnsiConsole.MarkupLine("[yellow]=== All Characters ===[/]");

            var players = _context.Players.Include(p => p.Room).ToList();

            if (!players.Any())
            {
                AnsiConsole.MarkupLine("[red]No characters found.[/]");
            }
            else
            {
                var table = new Table();
                table.AddColumn("ID");
                table.AddColumn("Name");
                table.AddColumn("Health");
                table.AddColumn("Experience");
                table.AddColumn("Location");

                foreach (var player in players)
                {
                    table.AddRow(
                        player.Id.ToString(),
                        player.Name,
                        player.Health.ToString(),
                        player.Experience.ToString(),
                        player.Room?.Name ?? "Unknown"
                    );
                }

                AnsiConsole.Write(table);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error displaying all characters");
            AnsiConsole.MarkupLine($"[red]Error displaying characters: {ex.Message}[/]");
        }
    }

    /// <summary>
    /// Search for characters by name
    /// </summary>
    public void SearchCharacterByName()
    {
        try
        {
            _logger.LogInformation("User selected Search Character");
            AnsiConsole.MarkupLine("[yellow]=== Search Character ===[/]");

            var searchName = AnsiConsole.Ask<string>("Enter character [green]name[/] to search:");

            var players = _context.Players
                .Include(p => p.Room)
                .Where(p => p.Name.ToLower().Contains(searchName.ToLower()))
                .ToList();

            if (!players.Any())
            {
                _logger.LogInformation("No characters found matching '{SearchName}'", searchName);
                AnsiConsole.MarkupLine($"[red]No characters found matching '{searchName}'.[/]");
            }
            else
            {
                _logger.LogInformation("Found {Count} character(s) matching '{SearchName}'", players.Count, searchName);

                var table = new Table();
                table.AddColumn("ID");
                table.AddColumn("Name");
                table.AddColumn("Health");
                table.AddColumn("Experience");
                table.AddColumn("Location");

                foreach (var player in players)
                {
                    table.AddRow(
                        player.Id.ToString(),
                        player.Name,
                        player.Health.ToString(),
                        player.Experience.ToString(),
                        player.Room?.Name ?? "Unknown"
                    );
                }

                AnsiConsole.Write(table);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for characters");
            AnsiConsole.MarkupLine($"[red]Error searching characters: {ex.Message}[/]");
        }
    }

    #endregion

    #region C-Level Requirements
    public void AddAbilityToCharacter()
    {
        try
        {
            _logger.LogInformation("User selected Add Ability to Character");
            AnsiConsole.MarkupLine("[yellow]=== Add Ability to Character ===[/]");

            // call DisplayAllCharacters() to get a character list
            DisplayAllCharacters();

            // user selects a character by their ID
            var charId = AnsiConsole.Ask<int>("Enter [green]character Id[/]: ");
            // use character id to get the player character
            var character = _context.Players.Find(charId);
            if (character == null)
            {
                AnsiConsole.MarkupLine($"[red]Character with ID {charId} not found.[/]");
                return;
            }

            // get a list of all the abilities
            var abilities = _context.Abilities;
            // check to see if there are any abilities
            if (!abilities.Any())
            {
                AnsiConsole.MarkupLine("[red]No abilities found.[/]");
            }
            else
            {
                // list all the abilities for the user to see
                var table = new Table();
                table.AddColumn("ID");
                table.AddColumn("Name");
                table.AddColumn("Description");
                table.AddColumn("AbilityType");

                foreach (var a in abilities)
                {
                    table.AddRow(
                        a.Id.ToString(),
                        a.Name,
                        a.Description.ToString(),
                        a.AbilityType.ToString()
                    );
                }

                AnsiConsole.Write(table);
            }

            // user selects an ability by it's ID
            var abilId = AnsiConsole.Ask<int>("Enter [green]ability Id[/]: ");
            // use character id to get the player character
            var ability = _context.Abilities.Find(abilId);
            if (ability == null)
            {
                AnsiConsole.MarkupLine($"[red]Ability with ID {abilId} not found.[/]");
                return;
            }

            // assign the ability to the caracter
            character.Abilities.Add(ability);
            _context.SaveChanges();

            _logger.LogInformation("Ability {Name} added to Character {name}", ability.Name, character.Name);
            AnsiConsole.MarkupLine("[green]Ability added successfully![/]");
            Thread.Sleep(1000);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding ability");
            AnsiConsole.MarkupLine($"[red]Error adding ability: {ex.Message}[/]");
            PressAnyKey();
        }
    }

    public void DisplayCharacterAbilities()
    {
        try
        {
            _logger.LogInformation("User selected Display Character Abilities");
            AnsiConsole.MarkupLine("[yellow]=== Display Character Abilities ===[/]");

            // user enters id of a character
            var id = AnsiConsole.Ask<int>("Enter character [green]ID[/] to see abilities: ");
            var player = _context.Players.Find(id);
            if (player == null)
            {
                _logger.LogWarning("Character with Id {Id} not found", id);
                AnsiConsole.MarkupLine($"[red]Character with ID {id} not found.[/]");
                return;
            }

            // Get the abilities from the character previously selected
            var abilities = player.Abilities;

            // display the character and their abilities
            AnsiConsole.MarkupLine($"Character Name: [blue]{player.Name}[/]");
            AnsiConsole.MarkupLine($"Character Experience: [blue]{player.Experience}[/]");
            AnsiConsole.MarkupLine($"Character health: [blue]{player.Health}[/]");
            AnsiConsole.MarkupLine($"Character Abilities:");

            // if selected character has abilities then they will be displayed in  table,
            // otherwise a message will be displayed saying the character has no abilities
            if (!abilities.Any())
            {
                AnsiConsole.MarkupLine($"[red]Selected character does not possess and abilities[/]");
            }
            else
            {
                var table = new Table();
                table.AddColumn("Name");
                table.AddColumn("Description");
                table.AddColumn("AbilityType");
                table.AddColumn("Damage");
                table.AddColumn("Distance");

                foreach (var ability in abilities)
                {
                    if (ability is ShoveAbility shove)
                    {
                        table.AddRow(
                            shove.Name,
                            shove.Description,
                            shove.AbilityType,
                            shove.Damage.ToString(),
                            shove.Distance.ToString()
                        );
                    }

                    if (ability is MagicAbility magic)
                    {
                        table.AddRow(
                            magic.Name,
                            magic.Description,
                            magic.AbilityType,
                            magic.Damage.ToString(),
                            "NULL"
                        );
                    }

                    if (ability is PhysAbility phys)
                    {
                        table.AddRow(
                            phys.Name,
                            phys.Description,
                            phys.AbilityType,
                            phys.Damage.ToString(),
                            "NULL"
                        );
                    }
                }

                AnsiConsole.Write(table);
            }

            _logger.LogInformation("User looked at character abilities of {character}", player.Name);
            Thread.Sleep(1000);

            PressAnyKey();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error displaying character abilities");
            AnsiConsole.MarkupLine($"[red]Error displaying character abilities: {ex.Message}[/]");
            PressAnyKey();
        }
    }

    #endregion

    #region B-Level Requirements

    public void AddRoom()
    {
        try
        {
            _logger.LogInformation("User selected Add Room");
            AnsiConsole.MarkupLine("[yellow]=== Add New Room ===[/]");

            var name = AnsiConsole.Ask<string>("Enter room [green]name[/]: ");
            var description = AnsiConsole.Ask<string>("Enter room [green]description[/]: ");

            var room = new Room
            {
                Name = name,
                Description = description
            };

            _context.Rooms.Add(room);
            _context.SaveChanges();

            _logger.LogInformation("Room {Name} added to database with Id {Id}", name, room.Id);
            AnsiConsole.MarkupLine($"[green]Room '{name}' added successfully![/]");
            Thread.Sleep(1000);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding room");
            AnsiConsole.MarkupLine($"[red]Error adding room: {ex.Message}[/]");
        }

        PressAnyKey();
    }

    public void DisplayRoomDetails()
    {
        try
        {
            _logger.LogInformation("User selected Display Room Details");
            AnsiConsole.MarkupLine("[yellow]=== Display Room Details ===[/]");

            // get a list of all the rooms
            var rooms = _context.Rooms
                .ToList();

            // if no rooms found then display message, else list all the rooms in a table
            if(!rooms.Any())
            {
                AnsiConsole.MarkupLine("[red]No rooms found.[/]");
            }
            else
            {
                var table = new Table();
                table.AddColumn("ID");
                table.AddColumn("Name");
                table.AddColumn("Description");

                foreach (var room in rooms)
                {
                    table.AddRow(
                        room.Id.ToString(),
                        room.Name,
                        room.Description
                    );
                }

                AnsiConsole.Write(table);
            }

            // ask user to enter room Id
            var roomId = AnsiConsole.Ask<string>("Enter room [green]Id[/]: ");

            // find the room using the Id, if no room found then display a message and return
            var selectedRoom = _context.Rooms
                .Include(r => r.Players)
                .Include(r => r.Monsters);
            var r = selectedRoom.ToList().Find(r => r.Id == Convert.ToInt32(roomId));
            if (r == null)
            {
                AnsiConsole.MarkupLine($"[red]Room with ID {roomId} not found.[/]");
                return;
            }

            // list room info
            AnsiConsole.MarkupLine($"Name: [Blue]{r.Name}[/]");
            AnsiConsole.MarkupLine($"Description: [Blue]{r.Description}[/]");
            AnsiConsole.MarkupLine($"North Exit: [Blue]{r.NorthRoom?.Name}[/]");
            AnsiConsole.MarkupLine($"South Exit: [Blue]{r.SouthRoom?.Name}[/]");
            AnsiConsole.MarkupLine($"East Exit: [Blue]{r.EastRoom?.Name}[/]");
            AnsiConsole.MarkupLine($"West Exit: [Blue]{r.WestRoom?.Name}[/]");

            // display all players in the room, if there are non then display a message
            var players = r.Players;
            if (players.Count() == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]There are no players in the room[/]");
            }
            else
            {
                Console.WriteLine("List of players in the room:");
                foreach (var player in players)
                {
                    AnsiConsole.MarkupLine($"Name: [blue]{player.Name}[/]");
                }
            }

            // display all monsters in the room, if there are non then display a message
            var monsters = r.Monsters;
            if (monsters.Count() == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]There are no monsters in the room[/]");
            }
            else
            {
                Console.WriteLine("List of monsters in the room:");
                foreach (var monster in monsters)
                {
                    AnsiConsole.MarkupLine($"Name: [blue]{monster.Name}[/]");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error displaying room");
            AnsiConsole.MarkupLine($"[red]Error displaying room: {ex.Message}[/]");
        }

        PressAnyKey();
    }

    #endregion

    #region A-Level Requirements

    /// <summary>
    /// TODO: Implement this method
    /// Requirements:
    /// - Display list of all rooms **
    /// - Prompt user to select a room **
    /// - Display a menu of attributes to filter by (Health, Name, Experience, etc.) **
    /// - Prompt user for filter criteria **
    /// - Query the database for characters in that room matching the criteria **
    /// - Display matching characters with relevant details in a formatted table **
    /// - Handle case where no characters match **
    /// - Log the operation **
    /// </summary>
    public void ListCharactersInRoomByAttribute()
    {
        try
        {
            _logger.LogInformation("User selected List Characters in Room by Attribute");
            AnsiConsole.MarkupLine("[yellow]=== List Characters in Room by Attribute ===[/]");

            // get rooms and put them into a variable
            var rooms = _context.Rooms;

            // display all the rooms in a table
            var table = new Table();
            table.AddColumn("ID");
            table.AddColumn("Name");
            table.AddColumn("Description");

            foreach (var room in rooms)
            {
                table.AddRow(
                    room.Id.ToString(),
                    room.Name,
                    room.Description
                );
            }

            AnsiConsole.Write(table);

            // get the user to select a room by entering an id
            var roomId = AnsiConsole.Ask<string>("Enter room [green]Id[/] of the room you wish to look at: ");

            // Display a list of attributes that the user can filter by and ask them to select one
            AnsiConsole.MarkupLine("Please enter an attribute you wish to search by:");
            AnsiConsole.MarkupLine("    [blue]Name[/]");
            AnsiConsole.MarkupLine("    [blue]Health[/]");
            AnsiConsole.MarkupLine("    [blue]Experience[/]");
            AnsiConsole.MarkupLine("    [blue]Equipment Name[/]");
            AnsiConsole.MarkupLine("    [blue]Ability Type[/]");
            var ask = AnsiConsole.Ask<string>("Enter the attribute: ");

            // convert all characters to lower case
            ask.ToLower();

            // based on which attribute the user selected, the program will use that to dertermine hot to search in the room
            switch(ask)
            {
                case "name":
                    var name = AnsiConsole.Ask<string>("Enter the name of the character you wish to search for: ");

                    // search for the character using the name
                    var findCharacterByName = _context.Players.Where(c => c.RoomId == Convert.ToInt32(roomId)
                                                    && c.Name == name);

                    // if no players are found them say so, else display players
                    if (!findCharacterByName.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No characters by that name were found in the room.[/]");
                    }
                    else
                    {
                        Console.WriteLine("List of characters that match criteria:");
                        foreach (var c in findCharacterByName)
                        {
                            AnsiConsole.MarkupLine($"   [blue]{c.Name}[/]");
                        }
                    }
                        break;
                case "health":
                    var health = AnsiConsole.Ask<string>("Enter the minimum health of the character you wish to search for: ");

                    // search for the character using the health amount
                    var findCharacterByMinHealth = _context.Players.Where(c => c.RoomId == Convert.ToInt32(roomId)
                                                    && c.Health <= Convert.ToInt32(health));

                    // if no players are found them say so, else display players
                    if (!findCharacterByMinHealth.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No characters with that health amount were found in the room.[/]");
                    }
                    else
                    {
                        Console.WriteLine("List of characters that match criteria:");
                        foreach (var c in findCharacterByMinHealth)
                        {
                            AnsiConsole.MarkupLine($"   [blue]{c.Name}[/]");
                        }
                    }
                    break;
                case "experience":
                    var exp = AnsiConsole.Ask<string>("Enter the minimum experiennce level of the character you wish to search for: ");

                    // search for the character using the experience level
                    var findCharacterByMinExperience = _context.Players.Where(c => c.RoomId == Convert.ToInt32(roomId)
                                                    && c.Experience <= Convert.ToInt32(exp));

                    // if no players are found them say so, else display players
                    if (!findCharacterByMinExperience.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No characters with that experience level were found in the room.[/]");
                    }
                    else
                    {
                        Console.WriteLine("List of characters that match criteria:");
                        foreach (var c in findCharacterByMinExperience)
                        {
                            AnsiConsole.MarkupLine($"   [blue]{c.Name}[/]");
                        }
                    }
                    break;
                case "equipment name":
                    var itemName = AnsiConsole.Ask<string>("To search for the character, enter the name of the equipment they have: ");

                    // get the item id from the name the user entered
                    var item = _context.Items.Where(i => i.Name == itemName);
                    var itemId = item.Select(i => i.Id).FirstOrDefault();

                    // search for the character using the equipment name
                    var findCharacterByEquipmentName = _context.Players.Where(c => c.RoomId == Convert.ToInt32(roomId)
                                                    && c.Equipment.Id == itemId);

                    // if no players are found them say so, else display players
                    if (!findCharacterByEquipmentName.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No characters with that experience level were found in the room.[/]");
                    }
                    else
                    {
                        Console.WriteLine("List of characters that match criteria:");
                        foreach (var c in findCharacterByEquipmentName)
                        {
                            AnsiConsole.MarkupLine($" [blue]{c.Name}[/]");
                        }
                    }
                    break;
                case "ability type":
                    // user enters the type of ability they want to search for
                    AnsiConsole.MarkupLine($"Enter the ability type of the character you wish to search for.");
                    var type = AnsiConsole.Ask<string>("([blue]ShoveAbility[/], [blue]MagicAbility[/], [blue]PhysAbility[/]): ");
                    // abilities of that type are located
                    var abilityType = _context.Abilities.Where(a => a.AbilityType == type);

                    // search for the character using the ability type of an ability they have
                    var findCharacterByAbilityType = _context.Players.Where(c => c.RoomId == Convert.ToInt32(roomId)
                                                                      && c.Abilities == abilityType)
                                               .Select( c => new
                                               {
                                                   CharacterName = c.Name,
                                                   Abilities = c.Abilities
                                               }
                                               );

                    // if no players are found them say so, else display players
                    if (!findCharacterByAbilityType.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No characters with that experience level were found in the room.[/]");
                    }
                    else
                    {
                        Console.WriteLine("List of characters that match criteria:");
                        foreach (var c in findCharacterByAbilityType)
                        {
                            AnsiConsole.MarkupLine($"   [blue]{c.CharacterName}[/]");
                        }
                    }
                    break;
                default:
                    AnsiConsole.MarkupLine("[red]Invalid option entered, try again[/]");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing characters in room by attribute");
            AnsiConsole.MarkupLine($"[red]Error listing characters in room by attribute: {ex.Message}[/]");
        }

        // TODO: Implement this method
        AnsiConsole.MarkupLine("[red]This feature is not yet implemented.[/]");
        AnsiConsole.MarkupLine("[yellow]TODO: Find characters in a room matching specific criteria.[/]");

        PressAnyKey();
    }

    /// <summary>
    /// TODO: Implement this method
    /// Requirements:
    /// - Query database for all rooms
    /// - For each room, retrieve all characters (Players) in that room
    /// - Display in a formatted list grouped by room
    /// - Show room name and description
    /// - Under each room, list all characters with their details
    /// - Handle rooms with no characters gracefully
    /// - Consider using Spectre.Console panels or tables for nice formatting
    /// - Log the operation
    /// </summary>
    public void ListAllRoomsWithCharacters()
    {
        _logger.LogInformation("User selected List All Rooms with Characters");
        AnsiConsole.MarkupLine("[yellow]=== List All Rooms with Characters ===[/]");

        // TODO: Implement this method
        AnsiConsole.MarkupLine("[red]This feature is not yet implemented.[/]");
        AnsiConsole.MarkupLine("[yellow]TODO: Group and display all characters by their rooms.[/]");

        PressAnyKey();
    }

    /// <summary>
    /// TODO: Implement this method
    /// Requirements:
    /// - Prompt user for equipment/item name to search for
    /// - Query the database to find which character has this equipment
    /// - Use Include to load Equipment -> Weapon/Armor -> Item
    /// - Also load the character's Room information
    /// - Display the character's name who has the equipment
    /// - Display the room/location where the character is located
    /// - Handle case where equipment is not found
    /// - Handle case where equipment exists but isn't equipped by anyone
    /// - Use Spectre.Console for nice formatting
    /// - Log the operation
    /// </summary>
    public void FindEquipmentLocation()
    {
        _logger.LogInformation("User selected Find Equipment Location");
        AnsiConsole.MarkupLine("[yellow]=== Find Equipment Location ===[/]");

        // TODO: Implement this method
        AnsiConsole.MarkupLine("[red]This feature is not yet implemented.[/]");
        AnsiConsole.MarkupLine("[yellow]TODO: Find which character has specific equipment and where they are located.[/]");

        PressAnyKey();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper method for user interaction consistency
    /// </summary>
    private void PressAnyKey()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Markup("[dim]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    #endregion
}
