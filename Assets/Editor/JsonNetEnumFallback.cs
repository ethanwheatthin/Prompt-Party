#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEditor;

// Register a lenient StringEnum converter in the editor so Newtonsoft.Json won't throw for unknown enum strings
[InitializeOnLoad]
static class JsonNetEnumFallbackInitializer
{
    static JsonNetEnumFallbackInitializer()
    {
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new LenientStringEnumConverter() },
            MissingMemberHandling = MissingMemberHandling.Ignore
        };
    }
}

/// <summary>
/// A StringEnumConverter that falls back to the enum's default value when parsing fails instead of throwing.
/// Useful when deserializing data that may contain enum values not present in the local enum type.
/// </summary>
class LenientStringEnumConverter : StringEnumConverter
{
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            var nullableType = Nullable.GetUnderlyingType(objectType);
            if (nullableType != null)
                return null;
            throw new JsonSerializationException($"Cannot convert null value to {objectType}.");
        }

        var isNullable = Nullable.GetUnderlyingType(objectType) != null;
        var enumType = Nullable.GetUnderlyingType(objectType) ?? objectType;

        if (reader.TokenType == JsonToken.String)
        {
            var enumText = reader.Value?.ToString();
            if (string.IsNullOrEmpty(enumText))
            {
                if (isNullable) return null;
                return GetDefaultEnum(enumType);
            }

            try
            {
                // Try normal parse (case-insensitive)
                return Enum.Parse(enumType, enumText, true);
            }
            catch
            {
                // Fallback: return default enum value (first defined)
                return GetDefaultEnum(enumType);
            }
        }

        // If it's a number (integer), let base handle it (will throw if invalid)
        try
        {
            return base.ReadJson(reader, objectType, existingValue, serializer);
        }
        catch
        {
            return isNullable ? null : GetDefaultEnum(enumType);
        }
    }

    private object GetDefaultEnum(Type enumType)
    {
        var values = Enum.GetValues(enumType);
        if (values.Length > 0) return values.GetValue(0);
        return Activator.CreateInstance(enumType);
    }
}
#endif
