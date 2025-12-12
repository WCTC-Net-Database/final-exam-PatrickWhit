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

    public void ListCharactersInRoomByAttribute()
    {
        _logger.LogInformation("User selected List Characters in Room by Attribute");
        AnsiConsole.MarkupLine("[yellow]=== List Characters in Room by Attribute ===[/]");

        try
        {
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
                        // List all the characters that match the criteria
                        var charListTable = new Table();
                        charListTable.AddColumn("ID");
                        charListTable.AddColumn("Name");
                        charListTable.AddColumn("Experience");
                        charListTable.AddColumn("Health");

                        foreach (var character in findCharacterByName)
                        {
                            charListTable.AddRow(
                                character.Id.ToString(),
                                character.Name,
                                character.Experience.ToString(),
                                character.Health.ToString()
                            );
                        }

                        AnsiConsole.Write(charListTable);
                    }
                        break;
                case "health":
                    var health = AnsiConsole.Ask<string>("Enter the minimum health of the character you wish to search for: ");

                    // search for the character using the health amount
                    var findCharacterByMinHealth = _context.Players.Where(c => c.RoomId == Convert.ToInt32(roomId)
                                                    && c.Health >= Convert.ToInt32(health));

                    // if no players are found them say so, else display players
                    if (!findCharacterByMinHealth.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No characters with that health amount were found in the room.[/]");
                    }
                    else
                    {
                        // List all the characters that match the criteria
                        var charListTable = new Table();
                        charListTable.AddColumn("ID");
                        charListTable.AddColumn("Name");
                        charListTable.AddColumn("Experience");
                        charListTable.AddColumn("Health");

                        foreach (var character in findCharacterByMinHealth)
                        {
                            charListTable.AddRow(
                                character.Id.ToString(),
                                character.Name,
                                character.Experience.ToString(),
                                character.Health.ToString()
                            );
                        }

                        AnsiConsole.Write(charListTable);
                    }
                    break;
                case "experience":
                    var exp = AnsiConsole.Ask<string>("Enter the minimum experiennce level of the character you wish to search for: ");

                    // search for the character using the experience level
                    var findCharacterByMinExperience = _context.Players.Where(c => c.RoomId == Convert.ToInt32(roomId)
                                                    && c.Experience >= Convert.ToInt32(exp));

                    // if no players are found them say so, else display players
                    if (!findCharacterByMinExperience.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No characters with that experience level were found in the room.[/]");
                    }
                    else
                    {
                        // List all the characters that match the criteria
                        var charListTable = new Table();
                        charListTable.AddColumn("ID");
                        charListTable.AddColumn("Name");
                        charListTable.AddColumn("Experience");
                        charListTable.AddColumn("Health");

                        foreach (var character in findCharacterByMinExperience)
                        {
                            charListTable.AddRow(
                                character.Id.ToString(),
                                character.Name,
                                character.Experience.ToString(),
                                character.Health.ToString()
                            );
                        }

                        AnsiConsole.Write(charListTable);
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
                        // List all the characters that match the criteria
                        var charListTable = new Table();
                        charListTable.AddColumn("ID");
                        charListTable.AddColumn("Name");
                        charListTable.AddColumn("Experience");
                        charListTable.AddColumn("Health");

                        foreach (var character in findCharacterByEquipmentName)
                        {
                            charListTable.AddRow(
                                character.Id.ToString(),
                                character.Name,
                                character.Experience.ToString(),
                                character.Health.ToString()
                            );
                        }

                        AnsiConsole.Write(charListTable);
                    }
                    break;
                case "ability type":
                    // user enters the type of ability they want to search for
                    AnsiConsole.MarkupLine($"Enter the ability type of the character you wish to search for.");
                    var abilityType = AnsiConsole.Ask<string>("([blue]ShoveAbility[/], [blue]MagicAbility[/], [blue]PhysAbility[/]): ");
                    // get a list of abilities of the type entered by the user
                    var abilities = _context.Abilities.Where(a => a.AbilityType == abilityType);

                    // if entered ability type has no matches, then state that to the user
                    if (!abilities.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No abilities of that type were found.[/]");
                        AnsiConsole.MarkupLine("[yellow]Make sure your spelling is correct.[/]");

                        PressAnyKey();
                        return;
                    }

                    // get the Ids from the abilities
                    //var abilitiIds = abilities.Select(a => a.Id).ToList();
                    // get a list of all the players in the specified room
                    var findPlayersInRoom = _context.Players
                        .Where(c => c.RoomId == Convert.ToInt32(roomId))
                        .Include(c => c.Abilities)
                        .ToList();

                    // loop over the abilities with the entered type and find the players in the room
                    // that have them and add them to a list
                    List<Player> findCharacterByAbilityType = new List<Player>();
                    foreach (var player in findPlayersInRoom)
                    {
                        foreach(var ability in abilities)
                        {
                            if (player.Abilities.Contains(ability))
                                findCharacterByAbilityType.Add(player);
                        }
                    }

                    // display the list of players found through the search, if no players are found then say so
                    if (!findCharacterByAbilityType.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No characters with that experience level were found in the room.[/]");
                    }
                    else
                    {
                        // List all the characters that match the criteria
                        var charListTable = new Table();
                        charListTable.AddColumn("ID");
                        charListTable.AddColumn("Name");
                        charListTable.AddColumn("Experience");
                        charListTable.AddColumn("Health");

                        foreach (var character in findCharacterByAbilityType)
                        {
                            charListTable.AddRow(
                                character.Id.ToString(),
                                character.Name,
                                character.Experience.ToString(),
                                character.Health.ToString()
                            );
                        }

                        AnsiConsole.Write(charListTable);
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

        PressAnyKey();
    }

    public void ListAllRoomsWithCharacters()
    {
        _logger.LogInformation("User selected List All Rooms with Characters");
        AnsiConsole.MarkupLine("[yellow]=== List All Rooms with Characters ===[/]");

        try
        {
            // get all the rooms
            var rooms = _context.Rooms
                                .Include(r => r.Players);

            // for each room, list all the player in that room
            foreach (var room in rooms)
            {
                // List room details
                AnsiConsole.MarkupLine($"\n[green]Room Name[/]: [blue]{room.Name}[/]");
                AnsiConsole.MarkupLine($"[green]Room Description[/]: {room.Description}");
                AnsiConsole.MarkupLine("[green]List Of Characters in Room[/]:");

                // get all players in the room
                var players = room.Players;

                // if there are no players in the room then print a message, else list the characters
                if (!players.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]There are no characters in this room.[/]");
                }
                else
                {
                    var charListTable = new Table();
                    charListTable.AddColumn("ID");
                    charListTable.AddColumn("Name");
                    charListTable.AddColumn("Experience");
                    charListTable.AddColumn("Health");

                    foreach (var player in players)
                    {
                        charListTable.AddRow(
                            player.Id.ToString(),
                            player.Name,
                            player.Experience.ToString(),
                            player.Health.ToString()
                        );
                    }

                    AnsiConsole.Write(charListTable);
                } 
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing rooms with characters");
            AnsiConsole.MarkupLine($"[red]Error listing rooms with characters: {ex.Message}[/]");
        }

        PressAnyKey();
    }

    public void FindEquipmentLocation()
    {
        _logger.LogInformation("User selected Find Equipment Location");
        AnsiConsole.MarkupLine("[yellow]=== Find Equipment Location ===[/]");

        try
        {
            // get all items
            var items = _context.Items;

            // list all items for user
            var listItemstable = new Table();
            listItemstable.AddColumn("Id");
            listItemstable.AddColumn("Name");
            listItemstable.AddColumn("Type");

            foreach(var i in items)
            {
                listItemstable.AddRow(
                    i.Id.ToString(),
                    i.Name,
                    i.Type
                );
            }

            AnsiConsole.Write(listItemstable);

            // ask the user to enter the name of an item
            var itemName = AnsiConsole.Ask<string>("Enter the name of the item you wish to search for: ");

            // get the item base on the name the user entered, if no item has the name inform the user and exit the method
            var item = _context.Items.Where(i => i.Name == itemName);
            if (!item.Any())
            {
                AnsiConsole.Markup("[yellow]No items by this name were found.[/]\n");
                AnsiConsole.Markup("[yellow]Make sure the item name is spelled correctly.[/]");

                PressAnyKey();
                return;
            }

            // get the id from the item
            var itemId = item.Select(i => i.Id).FirstOrDefault();

            // search the database to find a list of players that have the item
            var players = _context.Players
                .Where(p => p.Equipment.Id == itemId)
                .Include(p => p.Room);

            // if any players have the item display them in a table,
            // else display a message saying no players with the item were found
            if (!players.Any())
            {
                AnsiConsole.Markup("[yellow]No players with this item were found.[/]");
            }
            else
            {
                // list all items for user
                var listPLayersWithItem = new Table();
                listPLayersWithItem.AddColumn("Player Id");
                listPLayersWithItem.AddColumn("Player Name");
                listPLayersWithItem.AddColumn("Player Experience");
                listPLayersWithItem.AddColumn("Player Health");
                listPLayersWithItem.AddColumn("Player Location");
                listPLayersWithItem.AddColumn("Location Description");

                foreach (var player in players)
                {
                    listPLayersWithItem.AddRow(
                        player.Id.ToString(),
                        player.Name,
                        player.Experience.ToString(),
                        player.Health.ToString(),
                        player.Room.Name,
                        player.Room.Description
                    );
                }

                AnsiConsole.Write(listPLayersWithItem);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding characters with equipment");
            AnsiConsole.MarkupLine($"[red]Error finding characters with equipment: {ex.Message}[/]");
        }

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
