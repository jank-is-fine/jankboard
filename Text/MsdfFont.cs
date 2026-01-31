using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// <para>Parses MSDF font JSON data and creates a renderable font with glyph metrics and atlas texture</para>
/// <para>Atlas should be generated with yOrigin "top" see <see cref="https://github.com/Chlumsky/msdf-atlas-gen?tab=readme-ov-file#outputs"/>/ for msdf-atlas-gen</para>
/// </summary>

//Consider : Maybe switch to newtonsoft.Json?
//This was not done from the beginning since this class predates the SaveManager and the color (and such) serialazation shenanigans
public class MsdfFont : IDisposable
{

    public string Name { get; private set; } = string.Empty;
    public FontMetrics Metrics { get; private set; } = new();
    public Dictionary<char, Glyph> Glyphs { get; private set; } = [];
    public Texture? AtlasTexture { get; private set; } = null;
    public float DistanceRange { get; private set; }

    public MsdfFont(string jsonContent, Texture atlas, string name = "")
    {
        Name = name;
        LoadFromJson(jsonContent);
        AtlasTexture = atlas;
    }

    private void LoadFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) { return; }

        try
        {
            var fontData = JsonSerializer.Deserialize<FontData>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            }) ?? throw new InvalidDataException("Invalid font JSON");

            Metrics = new FontMetrics
            {
                EmSize = fontData.Metrics.GetValueOrDefault("emSize", 0),
                LineHeight = fontData.Metrics.GetValueOrDefault("lineHeight", 0),
                Ascender = fontData.Metrics.GetValueOrDefault("ascender", 0),
                Descender = fontData.Metrics.GetValueOrDefault("descender", 0),
                UnderlineY = fontData.Metrics.GetValueOrDefault("underlineY", 0),
                UnderlineThickness = fontData.Metrics.GetValueOrDefault("underlineThickness", 0)
            };

            DistanceRange = fontData.Atlas.DistanceRange;

            foreach (var glyphData in fontData.Glyphs)
            {
                var character = glyphData.Unicode;
                var glyph = new Glyph
                {
                    Unicode = character,
                    Advance = glyphData.Advance,

                    PlaneBoundsMin = new Vector2(
                        glyphData.PlaneBounds.GetValueOrDefault("left", 0),
                        glyphData.PlaneBounds.GetValueOrDefault("top", 0)
                    ),
                    PlaneBoundsMax = new Vector2(
                        glyphData.PlaneBounds.GetValueOrDefault("right", 0),
                        glyphData.PlaneBounds.GetValueOrDefault("bottom", 0)
                    ),

                    AtlasBoundsMin = new Vector2(
                    glyphData.AtlasBounds.GetValueOrDefault("left", 0) / fontData.Atlas.Width,
                    glyphData.AtlasBounds.GetValueOrDefault("top", 0) / fontData.Atlas.Height
                    ),

                    AtlasBoundsMax = new Vector2(
                    glyphData.AtlasBounds.GetValueOrDefault("right", 0) / fontData.Atlas.Width,
                    glyphData.AtlasBounds.GetValueOrDefault("bottom", 0) / fontData.Atlas.Height
                    )
                };

                Glyphs[character] = glyph;
            }

        }
        catch (JsonException ex)
        {
            Logger.Log("MsdfFont", $"JSON parsing error: {ex.Message}\nPath: {ex.Path}, Line: {ex.LineNumber}\nDisposing Font", LogLevel.FATAL);
            Dispose();
        }
    }

    public void Dispose()
    {
        AtlasTexture?.Dispose();
    }

    private class UnicodeConverter : JsonConverter<char>
    {
        public override char Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    var str = reader.GetString();
                    return string.IsNullOrEmpty(str) ? ' ' : str[0];
                case JsonTokenType.Number:
                    return (char)reader.GetInt32();
                default:
                    throw new JsonException($"Cannot convert {reader.TokenType} to char.");
            }
        }

        public override void Write(Utf8JsonWriter writer, char value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    public class Glyph
    {
        public char Unicode { get; set; }
        public float Advance { get; set; }
        public Vector2 PlaneBoundsMin { get; set; }
        public Vector2 PlaneBoundsMax { get; set; }
        public Vector2 AtlasBoundsMin { get; set; }
        public Vector2 AtlasBoundsMax { get; set; }
    }

    public class FontMetrics
    {
        public float EmSize { get; set; }
        public float LineHeight { get; set; }
        public float Ascender { get; set; }
        public float Descender { get; set; }
        public float UnderlineY { get; set; }
        public float UnderlineThickness { get; set; }
    }

    public class AtlasData
    {
        public string Type { get; set; } = string.Empty;
        public float DistanceRange { get; set; }
        public float Size { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public string YOrigin { get; set; } = string.Empty;
    }

    public class GlyphData
    {
        [JsonConverter(typeof(UnicodeConverter))]
        public char Unicode { get; set; }
        public float Advance { get; set; }
        public Dictionary<string, float> PlaneBounds { get; set; } = [];
        public Dictionary<string, float> AtlasBounds { get; set; } = [];
    }

    public class FontData
    {
        public AtlasData Atlas { get; set; } = new();
        public Dictionary<string, float> Metrics { get; set; } = [];
        public List<GlyphData> Glyphs { get; set; } = [];
    }
}

