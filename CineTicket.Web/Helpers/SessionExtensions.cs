using System.Text.Json;

namespace CineTicket.Web.Helpers;

public static class SessionExtensions
{
    public static void SetObject<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    public static T GetObject<T>(this ISession session, string key) where T : new()
    {
        var value = session.GetString(key);
        return value == null ? new T() : JsonSerializer.Deserialize<T>(value)!;
    }
}