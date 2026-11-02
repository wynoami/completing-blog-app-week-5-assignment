using AtlasBlog.Models.API;
using System.Text;
using System.Text.Json;

namespace BlogConsole;

public class APIManager
{
    private readonly HttpClient _client;
    private readonly string _apiKey;

    public APIManager(HttpClient client, string apiKey)
    {
        _client = client;
        _apiKey = apiKey;
    }

    public async Task RegisterAdmin()
    {
        var data = await GetUserData();
        if (data == null) return;

        try
        {
            var response = await SendRequest("api/auth/registeradmin", data);
            Console.WriteLine(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public async Task RegisterModerator()
    {
        var data = await GetUserData();
        if (data == null) return;

        try
        {
            var response = await SendRequest("api/auth/registermoderator", data);
            Console.WriteLine(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public async Task RegisterAuthor()
    {
        var userData = await GetUserData();
        if (userData == null) return;

        Console.Write("Enter blog title: ");
        var blogTitle = Console.ReadLine();
        Console.Write("Enter blog subtitle (optional): ");
        var blogSubtitle = Console.ReadLine();
        Console.Write("Enter blog description (optional): ");
        var blogDescription = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(blogTitle))
        {
            Console.WriteLine("Blog title is required");
            return;
        }

        var authorData = new RegisterAPIAuthorRequest
        {
            ApiKey = userData.ApiKey,
            Email = userData.Email,
            Password = userData.Password,
            BlogTitle = blogTitle,
            BlogSubtitle = blogSubtitle,
            BlogDescription = blogDescription
        };

        try
        {
            var response = await SendRequest("api/auth/registerauthor", authorData);
            Console.WriteLine(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public async Task<RegisterAPIUserRequest?> GetUserData()
    {
        Console.Write("Enter email: ");
        var email = Console.ReadLine();
        Console.Write("Enter password: ");
        var password = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("Email and password are required");
            return null;
        }

        return new RegisterAPIUserRequest
        {
            ApiKey = _apiKey!,
            Email = email,
            Password = password
        };
    }

    public async Task<string> SendRequest(string endpoint, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(endpoint, content);
        var result = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"API Error: {response.StatusCode} {result}");

        return result;
    }
}
