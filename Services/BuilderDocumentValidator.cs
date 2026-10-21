using System.Text.Json;

namespace WebsiteAPI.Services;

public static class BuilderDocumentValidator
{
    public const int MaximumDocumentBytes = 2 * 1024 * 1024;

    public static string? Validate(JsonDocument? document)
    {
        if (document is null) return "A builder document is required.";

        var raw = document.RootElement.GetRawText();
        if (System.Text.Encoding.UTF8.GetByteCount(raw) > MaximumDocumentBytes)
            return "The page document exceeds the 2 MB limit.";

        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            return "The page document root must be an object.";
        if (!root.TryGetProperty("schemaVersion", out var schemaVersion) ||
            schemaVersion.ValueKind != JsonValueKind.Number ||
            schemaVersion.GetInt32() != 1)
            return "The page document must use schemaVersion 1.";
        if (!root.TryGetProperty("children", out var children) ||
            children.ValueKind != JsonValueKind.Array)
            return "The page document must contain a children array.";

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var child in children.EnumerateArray())
        {
            var error = ValidateNode(child, ids, 0);
            if (error is not null) return error;
        }

        return null;
    }

    private static string? ValidateNode(JsonElement node, HashSet<string> ids, int depth)
    {
        if (depth > 12) return "Builder nodes cannot be nested more than 12 levels.";
        if (node.ValueKind != JsonValueKind.Object) return "Every builder node must be an object.";
        if (!node.TryGetProperty("id", out var idProperty) || idProperty.ValueKind != JsonValueKind.String)
            return "Every builder node must have an id.";
        var id = idProperty.GetString();
        if (string.IsNullOrWhiteSpace(id) || !ids.Add(id))
            return "Every builder node id must be non-empty and unique.";
        if (!node.TryGetProperty("type", out var typeProperty) ||
            typeProperty.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(typeProperty.GetString()))
            return $"Builder node '{id}' must have a type.";

        if (node.TryGetProperty("children", out var children))
        {
            if (children.ValueKind != JsonValueKind.Array)
                return $"Builder node '{id}' children must be an array.";
            foreach (var child in children.EnumerateArray())
            {
                var error = ValidateNode(child, ids, depth + 1);
                if (error is not null) return error;
            }
        }

        return null;
    }

    public static JsonDocument Clone(JsonDocument document) =>
        JsonDocument.Parse(document.RootElement.GetRawText());

    public static JsonDocument Empty(string template = "builder") =>
        JsonSerializer.SerializeToDocument(new
        {
            schemaVersion = 1,
            type = "page",
            template,
            settings = new { headerMode = "solid", footerVisible = true },
            children = Array.Empty<object>()
        });
}
