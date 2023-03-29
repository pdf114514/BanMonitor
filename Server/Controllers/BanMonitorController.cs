using System.Text.Json;
using WebPush;
using Microsoft.AspNetCore.Mvc;
using BanMonitor.Shared;
using N = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace BanMonitor.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class BanMonitorController : ControllerBase {
    // Hardcoded 💀
    private const string FAccountId = "";
    private const string FDeviceId = "";
    private const string FSecret = "";
    private const string PublicKey = "BJwAkITyhY5ssKl6SMRnPRxY_khXZmmw2JId4Z081Y0Xme8ZGjbf9OiZeaO3E8SHcip6POp6hZ9yxNuWTg1v8Tk";
    private const string PrivateKey = "nMT2TWIeNeMXR8wYMmL9j_J_4otiJ7dzVGqrbK63yjs";

    private static readonly HttpClient Http = new();
    private static readonly WebPushClient WebPush = new();
    private static BanStatus _BanStatus = new() { bIsBanned = true, LastUpdated = DateTime.Now };

    static BanMonitorController() {
        if (!System.IO.File.Exists("subscriptions.json")) {
            var s = System.IO.File.Create("subscriptions.json");
            s.Write(System.Text.Encoding.UTF8.GetBytes("[]"));
            s.Close();
        }
        Task.Run(async () => {
            while (true) {
                _BanStatus.bIsBanned = await GetBannedStatus();
                _BanStatus.LastUpdated = DateTime.Now;
                await SendNotification();
                Console.WriteLine($"Next check at {DateTime.Now.AddHours(12)}");
                await Task.Delay(1000 * 60 * 60 * 12); // per 12 hours
            }
        });
    }

    [HttpGet("status")]
    public BanStatus Get() => new() { bIsBanned = _BanStatus.bIsBanned, LastUpdated = _BanStatus.LastUpdated };

    [HttpPost("subscribe")]
    public async Task<IActionResult> SubscribeNotification([FromBody] NotificationSubscription subscription) {
        Console.WriteLine($"New subscription: {subscription.Auth}");
        var subscriptions = JsonSerializer.Deserialize<List<NotificationSubscription>>(await System.IO.File.ReadAllTextAsync("subscriptions.json")) ?? new();
        subscriptions.Add(subscription);
        await System.IO.File.WriteAllTextAsync("subscriptions.json", JsonSerializer.Serialize(subscriptions));
        
        var push = new PushSubscription(subscription.Url, subscription.P256dh, subscription.Auth);
        var vapid = new VapidDetails("mailto:user@xthe.org", PublicKey, PrivateKey);
        await new WebPushClient().SendNotificationAsync(push, JsonSerializer.Serialize(new{ message = "解除方法は無い(絶望)", url = "" }), vapid);
        
        return NoContent();
    }

    public static async Task<bool> GetBannedStatus() {
        return true;
        var authRequest = new HttpRequestMessage(HttpMethod.Post, "https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token");
        authRequest.Headers.Authorization = new("basic", "MzQ0NmNkNzI2OTRjNGE0NDg1ZDgxYjc3YWRiYjIxNDE6OTIwOWQ0YTVlMjVhNDU3ZmI5YjA3NDg5ZDMxM2I0MWE=");
        authRequest.Content = new StringContent(
            "grant_type=device_auth"
            +$"&account_id={FAccountId}"
            +$"&device_id={FDeviceId}"
            +$"&secret={FSecret}",
            null, "application/x-www-form-urlencoded"
        );
        var authResponse = await Http.SendAsync(authRequest);
        var auth = (await authResponse.Content.ReadFromJsonAsync<AuthResponse>())!;

        var profileRequest = new HttpRequestMessage(HttpMethod.Post, $"https://fortnite-public-service-prod11.ol.epicgames.com/fortnite/api/game/v2/profile/{auth.AccountId}/client/QueryProfile?profileId=athena&rvn=-1");
        profileRequest.Headers.Authorization = new("bearer", auth.AccessToken);
        profileRequest.Content = new StringContent("{}", null, "application/json");
        var profileResponse = await Http.SendAsync(profileRequest);
        Console.WriteLine($"{(int)profileResponse.StatusCode}: {await profileResponse.Content.ReadAsStringAsync()}");
        return (int)profileResponse.StatusCode != 200;
    }

    public async static Task SendNotification() {
        var successes = new List<NotificationSubscription>();
        foreach (var subscription in JsonSerializer.Deserialize<List<NotificationSubscription>>(await System.IO.File.ReadAllTextAsync("subscriptions.json")) ?? new()) {
            try {
                Console.WriteLine($"SendNotification: {subscription.Auth}");
                var push = new PushSubscription(subscription.Url, subscription.P256dh, subscription.Auth);
                var vapid = new VapidDetails("mailto:user@xthe.org", PublicKey, PrivateKey);
                // await new WebPushClient().SendNotificationAsync(push, JsonSerializer.Serialize(new{ notification = new{ title = "fortnite.day", body = $"pdf114514 is {(_BanStatus.bIsBanned ? "still banned..." : "unbanned?!")}", icon = "/icon-192.png", url = "/", vibrate = new int[] { 100, 50, 100 } }}), vapid);
                await new WebPushClient().SendNotificationAsync(push, JsonSerializer.Serialize(new{ message = $"pdf114514 is {(_BanStatus.bIsBanned ? "still banned..." : "unbanned?!")}", url = "" }), vapid);
                successes.Add(subscription);
            } catch (Exception exc) { Console.WriteLine(exc.ToString()); }
        }
        await System.IO.File.WriteAllTextAsync("subscriptions.json", JsonSerializer.Serialize(successes));
    }
}

public class AuthResponse {
    [N("access_token")] public required string AccessToken { get; init; }
    [N("account_id")] public required string AccountId { get; init; }
    [N("displayName")] public required string DisplayName { get; init; }
}