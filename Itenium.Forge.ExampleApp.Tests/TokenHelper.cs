using System.Text.Json;

namespace Itenium.Forge.ExampleApp.Tests;

public static class TokenHelper
{
    public static async Task<string> GetTokenAsync(
        HttpClient client,
        string username = "user",
        string password = "UserPassword123!")
    {
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "forge-spa",
            ["username"] = username,
            ["password"] = password,
            ["scope"] = "openid profile email"
        });

        var response = await client.PostAsync("/connect/token", tokenRequest);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(content);

        return tokenResponse.GetProperty("access_token").GetString()!;
    }

    public static async Task<string> GetAdminTokenAsync(HttpClient client)
    {
        return await GetTokenAsync(client, "admin", "AdminPassword123!");
    }

    public static async Task<string> GetUserTokenAsync(HttpClient client)
    {
        return await GetTokenAsync(client, "user", "UserPassword123!");
    }
}
