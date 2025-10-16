using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Nutrion.Lib.Messaging.DTO;

public class GameClientEvent
{
    public string Topic { get; set; } = string.Empty;
    public string OwnerSessionId { get; set; } = string.Empty;
    public object Payload { get; set; } = default!;
}

public static class GameClientEventExtensions
{
    public static object? DeserializePayload(this GameClientEvent evt, Type type)
    {
        if (evt.Payload is JsonElement element)
        {
            try
            {
                // Always allow nested payload wrapper
                var root = element.ValueKind == JsonValueKind.Object ? element : default;
                return JsonSerializer.Deserialize(root.GetRawText(), type, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to deserialize payload for {type.Name}: {ex.Message}");
                return null;
            }
        }

        return evt.Payload?.GetType() == type ? evt.Payload : null;
    }
}