using Newtonsoft.Json;

namespace Entities.Models;

public class TelegramUser
{
    [JsonProperty("UserId")]
    public long Id { get; set; }

    public string FullName { get; set; }
    public string UserName { get; set; }
    public int Reputation { get; set; }

    public TelegramUser(long userId, int reputation, string fullName)
    {
        Id = userId;
        Reputation = reputation;
        FullName = fullName;
    }
    public TelegramUser(long userId, string fullName, string username)
    {
        Id = userId;
        FullName = fullName;
        UserName = username;
    }
    
    public TelegramUser()
    {
        
    }
}