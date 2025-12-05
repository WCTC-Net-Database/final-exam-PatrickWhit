using ConsoleRpgEntities.Data;
using ConsoleRpgEntities.Models.Abilities.PlayerAbilities;
using ConsoleRpgEntities.Models.Characters;
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
            var charId = AnsiConsole.Ask<int>("Enter [green]character Id[/]:");
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
            var abilId = AnsiConsole.Ask<int>("Enter [green]ability Id[/]:");
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

    /// <summary>
    /// TODO: Implement this method
    /// Requirements:
    /// - Prompt the user to select a character (by ID or name) **
    /// - Retrieve the character and their abilities from the database (use Include or lazy loading) **
    /// - Display the character's name and basic info **
    /// - Display all abilities associated with that character in a formatted table 
    /// - For each ability, show: Name, Description, and any other relevant properties (e.g., Damage, Distance for ShoveAbility) 
    /// - Handle the case where the character has no abilities **
    /// - Log the operation **
    /// </summary>
    public void DisplayCharacterAbilities()
    {
        try
        {
            _logger.LogInformation("User selected Display Character Abilities");
            AnsiConsole.MarkupLine("[yellow]=== Display Character Abilities ===[/]");

            // user enters id of a character
            var id = AnsiConsole.Ask<int>("Enter character [green]ID[/] to see abilities:");
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

                    table.AddRow(
                        ability.Name,
                        ability.Description,
                        ability.AbilityType
                        //ability.Damage?.ToString() ?? "Null",
                        //ability.Distance?.ToString() ?? "Null"

                    // TODO: get damage and distance stats working
                    );
                }

                AnsiConsole.Write(table);
            }
            // TODO: ask about ability type

            //_logger.LogInformation("User looked at character abilities of {character}", player.Name);
            //Thread.Sleep(1000);

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

    /// <summary>
    /// TODO: Implement this method
    /// Requirements:
    /// - Prompt user for room name
    /// - Prompt user for room description
    /// - Optionally prompt for navigation (which rooms connect in which directions)
    /// - Create a new Room entity
    /// - Save to the database
    /// - Display confirmation with room details
    /// - Log the operation
    /// </summary>
    public void AddRoom()
    {
        _logger.LogInformation("User selected Add Room");
        AnsiConsole.MarkupLine("[yellow]=== Add New Room ===[/]");

        // TODO: Implement this method
        AnsiConsole.MarkupLine("[red]This feature is not yet implemented.[/]");
        AnsiConsole.MarkupLine("[yellow]TODO: Allow users to create new rooms and connect them to the world.[/]");

        PressAnyKey();
    }

    /// <summary>
    /// TODO: Implement this method
    /// Requirements:
    /// - Display a list of all rooms
    /// - Prompt user to select a room (by ID or name)
    /// - Retrieve room from database with related data (Include Players and Monsters)
    /// - Display room name, description, and exits
    /// - Display list of all players in the room (or message if none)
    /// - Display list of all monsters in the room (or message if none)
    /// - Handle case where room is empty gracefully
    /// - Log the operation
    /// </summary>
    public void DisplayRoomDetails()
    {
        _logger.LogInformation("User selected Display Room Details");
        AnsiConsole.MarkupLine("[yellow]=== Display Room Details ===[/]");

        // TODO: Implement this method
        AnsiConsole.MarkupLine("[red]This feature is not yet implemented.[/]");
        AnsiConsole.MarkupLine("[yellow]TODO: Display detailed information about a room and its inhabitants.[/]");

        PressAnyKey();
    }

    #endregion

    #region A-Level Requirements

    /// <summary>
    /// TODO: Implement this method
    /// Requirements:
    /// - Display list of all rooms
    /// - Prompt user to select a room
    /// - Display a menu of attributes to filter by (Health, Name, Experience, etc.)
    /// - Prompt user for filter criteria
    /// - Query the database for characters in that room matching the criteria
    /// - Display matching characters with relevant details in a formatted table
    /// - Handle case where no characters match
    /// - Log the operation
    /// </summary>
    public void ListCharactersInRoomByAttribute()
    {
        _logger.LogInformation("User selected List Characters in Room by Attribute");
        AnsiConsole.MarkupLine("[yellow]=== List Characters in Room by Attribute ===[/]");

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
