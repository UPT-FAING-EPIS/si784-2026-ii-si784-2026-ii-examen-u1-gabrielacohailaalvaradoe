using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using UniversitySocial.Api.Contracts;

namespace UniversitySocial.Api.Tests;

public sealed class ApiIntegrationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Register_Login_And_Create_Post_Work()
    {
        using var client = factory.CreateClient();
        var auth = await Register(client, "ana@upt.test", "Ana Estudiante");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var response = await client.PostAsJsonAsync("/posts", new CreatePostRequest("Mi primera publicación", null));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var post = await response.Content.ReadFromJsonAsync<PostResponse>();
        Assert.Equal(auth.User.Id, post!.AuthorId);
    }

    [Fact]
    public async Task User_Cannot_Edit_Another_Users_Profile()
    {
        using var client = factory.CreateClient();
        var owner = await Register(client, "owner@upt.test", "Owner");
        var other = await Register(client, "other@upt.test", "Other");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", other.Token);
        var response = await client.PutAsJsonAsync($"/users/{owner.User.Id}", new UpdateUserRequest("Changed", null, null, null));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Conversation_Does_Not_Expose_Third_Party_Messages()
    {
        using var client = factory.CreateClient();
        var ana = await Register(client, "ana2@upt.test", "Ana");
        var beto = await Register(client, "beto@upt.test", "Beto");
        var carla = await Register(client, "carla@upt.test", "Carla");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ana.Token);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/messages", new CreateMessageRequest(beto.User.Id, "Hola Beto"))).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", carla.Token);
        var result = await client.GetFromJsonAsync<PagedResponse<MessageResponse>>($"/messages/conversation/{beto.User.Id}");
        Assert.Empty(result!.Items);
    }

    [Fact]
    public async Task Protected_Endpoint_Rejects_Anonymous_Request()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/dashboard")).StatusCode);
    }

    private static async Task<AuthResponse> Register(HttpClient client, string email, string name)
    {
        var response = await client.PostAsJsonAsync("/auth/register", new RegisterRequest(email, "SecurePass123!", name, "student"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }
}
