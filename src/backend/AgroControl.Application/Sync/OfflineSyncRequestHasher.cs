using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AgroControl.Application.Sync;

public static class OfflineSyncRequestHasher
{
    public static string Compute(Guid farmId, OfflinePushOperationDto operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteString("farmId", farmId);
            writer.WriteString("operationId", operation.OperationId);
            writer.WriteString("entityKind", operation.EntityKind?.Trim().ToLowerInvariant());
            writer.WriteString("entityId", operation.EntityId);
            writer.WriteString("operation", operation.Operation?.Trim().ToLowerInvariant());
            if (string.IsNullOrWhiteSpace(operation.BaseServerVersion)) writer.WriteNull("baseServerVersion");
            else writer.WriteString("baseServerVersion", operation.BaseServerVersion.Trim());
            writer.WritePropertyName("payload");
            if (operation.Payload is null) writer.WriteNullValue();
            else WriteCanonical(writer, operation.Payload.Value);
            writer.WriteEndObject();
        }

        return Convert.ToHexStringLower(SHA256.HashData(buffer.ToArray()));
    }

    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(item => item.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(writer, property.Value);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray()) WriteCanonical(writer, item);
                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;

            case JsonValueKind.Number:
                writer.WriteRawValue(element.GetRawText(), skipInputValidation: false);
                break;

            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                writer.WriteNullValue();
                break;

            default:
                throw new InvalidOperationException($"Unsupported JSON kind {element.ValueKind} in offline sync request.");
        }
    }
}
