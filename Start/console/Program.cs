using BlogConsole;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();

var apiKey = config["ApiKey"];
var apiBaseUrl = config["ApiBaseUrl"];

using var client = new HttpClient();
client.BaseAddress = new Uri(apiBaseUrl!);

var manager = new APIManager(client, apiKey);

while (true)
{
    Console.WriteLine("\nBlog User Management");
    Console.WriteLine("1. Create Admin");
    Console.WriteLine("2. Create Author");
    Console.WriteLine("3. Create Moderator");
    Console.WriteLine("4. Exit");
    Console.Write("\nSelect an option: ");

    var choice = Console.ReadLine();

    switch (choice)
    {
        case "1":
            await manager.RegisterAdmin();
            break;
        case "2":
            await manager.RegisterAuthor();
            break;
        case "3":
            await manager.RegisterModerator();
            break;
        case "4":
            return;
        default:
            Console.WriteLine("Invalid option");
            break;
    }
}