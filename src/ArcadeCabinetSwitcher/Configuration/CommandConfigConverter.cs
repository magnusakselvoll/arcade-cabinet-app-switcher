using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArcadeCabinetSwitcher.Configuration;

/// <summary>
/// Deserializes a <see cref="CommandConfig"/> from either a plain JSON string or a JSON object
/// with <c>command</c> and optional <c>workingDirectory</c> properties.
/// </summary>
internal sealed class CommandConfigConverter : JsonConverter<CommandConfig>
{
    public override CommandConfig Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var command = reader.GetString()
                ?? throw new JsonException("Command string must not be null.");
            return new CommandConfig { Command = command };
        }

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected string or object for CommandConfig, got {reader.TokenType}.");

        string? command2 = null;
        string? workingDirectory = null;
        int? delaySeconds = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected property name.");

            var propertyName = reader.GetString()!;
            reader.Read();

            if (propertyName.Equals("command", StringComparison.OrdinalIgnoreCase))
                command2 = reader.GetString();
            else if (propertyName.Equals("workingDirectory", StringComparison.OrdinalIgnoreCase))
                workingDirectory = reader.GetString();
            else if (propertyName.Equals("delaySeconds", StringComparison.OrdinalIgnoreCase))
                delaySeconds = reader.GetInt32();
            else
                reader.Skip();
        }

        if (command2 is null)
            throw new JsonException("CommandConfig object must include a 'command' property.");

        return new CommandConfig { Command = command2, WorkingDirectory = workingDirectory, DelaySeconds = delaySeconds };
    }

    public override void Write(Utf8JsonWriter writer, CommandConfig value, JsonSerializerOptions options)
    {
        if (value.WorkingDirectory is null && value.DelaySeconds is null)
        {
            writer.WriteStringValue(value.Command);
        }
        else
        {
            writer.WriteStartObject();
            writer.WriteString("command", value.Command);
            if (value.WorkingDirectory is not null)
                writer.WriteString("workingDirectory", value.WorkingDirectory);
            if (value.DelaySeconds is not null)
                writer.WriteNumber("delaySeconds", value.DelaySeconds.Value);
            writer.WriteEndObject();
        }
    }
}
