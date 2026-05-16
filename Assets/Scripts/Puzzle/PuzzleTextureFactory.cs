using UnityEngine;

internal static class PuzzleTextureFactory
{
    public static Texture2D PrepareReadableTexture(Texture sourceTexture, int maxDimension, string name)
    {
        var scale = maxDimension / (float)Mathf.Max(sourceTexture.width, sourceTexture.height);
        if (scale > 1f)
        {
            scale = 1f;
        }

        var width = Mathf.Max(2, Mathf.RoundToInt(sourceTexture.width * scale));
        var height = Mathf.Max(2, Mathf.RoundToInt(sourceTexture.height * scale));
        var renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;
        Graphics.Blit(sourceTexture, renderTexture);
        RenderTexture.active = renderTexture;

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
        texture.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);
        return texture;
    }

    public static Texture2D CreateSunsetTexture(int width, int height)
    {
        var pixels = new Color[width * height];

        for (var y = 0; y < height; y++)
        {
            var vertical = y / (float)(height - 1);
            var sky = Color.Lerp(new Color(0.13f, 0.22f, 0.49f), new Color(0.95f, 0.64f, 0.31f), vertical);

            for (var x = 0; x < width; x++)
            {
                pixels[y * width + x] = sky;
            }
        }

        DrawCircle(pixels, width, height, new Vector2(width * 0.72f, height * 0.76f), height * 0.13f, new Color(1f, 0.92f, 0.68f), 0.9f);

        for (var x = 0; x < width; x++)
        {
            var normalized = x / (float)(width - 1);
            var hillA = height * (0.43f + 0.07f * Mathf.Sin(normalized * 6f));
            var hillB = height * (0.28f + 0.05f * Mathf.Sin(normalized * 8.1f + 1.2f));
            var riverCenter = width * 0.54f + Mathf.Sin(normalized * 7.3f) * width * 0.06f;

            for (var y = 0; y < height; y++)
            {
                var index = y * width + x;

                if (y < hillB)
                {
                    pixels[index] = Color.Lerp(new Color(0.18f, 0.28f, 0.18f), new Color(0.27f, 0.42f, 0.25f), y / hillB);
                }
                else if (y < hillA)
                {
                    pixels[index] = Color.Lerp(new Color(0.24f, 0.45f, 0.25f), new Color(0.43f, 0.64f, 0.33f), (y - hillB) / Mathf.Max(1f, hillA - hillB));
                }

                if (Mathf.Abs(x - riverCenter) < width * 0.05f && y < hillA)
                {
                    pixels[index] = Color.Lerp(new Color(0.22f, 0.43f, 0.68f), new Color(0.62f, 0.82f, 0.94f), y / Mathf.Max(1f, hillA));
                }
            }
        }

        return BuildTexture("BuiltIn_Sunset", width, height, pixels);
    }

    public static Texture2D CreateGeometryTexture(int width, int height)
    {
        var pixels = new Color[width * height];

        for (var y = 0; y < height; y++)
        {
            var vertical = y / (float)(height - 1);
            for (var x = 0; x < width; x++)
            {
                var horizontal = x / (float)(width - 1);
                pixels[y * width + x] = Color.Lerp(
                    new Color(0.89f, 0.91f, 0.86f),
                    new Color(0.95f, 0.85f, 0.73f),
                    Mathf.Clamp01(horizontal * 0.42f + vertical * 0.58f));
            }
        }

        DrawRectangle(pixels, width, height, 64, 76, 340, 240, new Color(0.29f, 0.45f, 0.41f), 0.92f);
        DrawRectangle(pixels, width, height, 430, 152, 270, 205, new Color(0.68f, 0.43f, 0.28f), 0.93f);
        DrawRectangle(pixels, width, height, 180, 460, 520, 250, new Color(0.45f, 0.62f, 0.54f), 0.93f);
        DrawCircle(pixels, width, height, new Vector2(width * 0.76f, height * 0.76f), height * 0.12f, new Color(0.97f, 0.79f, 0.42f), 0.96f);
        DrawDiagonalBand(pixels, width, height, new Color(0.96f, 0.93f, 0.86f), 0.72f, 140f);
        DrawDiagonalBand(pixels, width, height, new Color(0.34f, 0.52f, 0.62f), 0.18f, 90f);

        return BuildTexture("BuiltIn_Geometry", width, height, pixels);
    }

    private static Texture2D BuildTexture(string name, int width, int height, Color[] pixels)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static void DrawRectangle(Color[] pixels, int width, int height, int x, int y, int rectangleWidth, int rectangleHeight, Color color, float blend)
    {
        var xMax = Mathf.Min(width, x + rectangleWidth);
        var yMax = Mathf.Min(height, y + rectangleHeight);

        for (var currentY = Mathf.Max(0, y); currentY < yMax; currentY++)
        {
            for (var currentX = Mathf.Max(0, x); currentX < xMax; currentX++)
            {
                var index = currentY * width + currentX;
                pixels[index] = Color.Lerp(pixels[index], color, blend);
            }
        }
    }

    private static void DrawCircle(Color[] pixels, int width, int height, Vector2 center, float radius, Color color, float blend)
    {
        var radiusSquared = radius * radius;
        var minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
        var maxX = Mathf.Min(width - 1, Mathf.CeilToInt(center.x + radius));
        var minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
        var maxY = Mathf.Min(height - 1, Mathf.CeilToInt(center.y + radius));

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                var deltaX = x - center.x;
                var deltaY = y - center.y;
                if (deltaX * deltaX + deltaY * deltaY > radiusSquared)
                {
                    continue;
                }

                var edgeFade = 1f - Mathf.Clamp01(Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY) / radius);
                var index = y * width + x;
                pixels[index] = Color.Lerp(pixels[index], color, blend * Mathf.Clamp01(edgeFade + 0.25f));
            }
        }
    }

    private static void DrawDiagonalBand(Color[] pixels, int width, int height, Color color, float originFactor, float thickness)
    {
        var bandOrigin = width * originFactor;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var distance = Mathf.Abs((x - bandOrigin) - y * 0.62f);
                if (distance > thickness)
                {
                    continue;
                }

                var blend = 1f - distance / thickness;
                var index = y * width + x;
                pixels[index] = Color.Lerp(pixels[index], color, blend * 0.68f);
            }
        }
    }
}
