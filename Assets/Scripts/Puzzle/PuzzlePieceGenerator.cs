using System.Buffers;
using Unity.Collections;
using UnityEngine;

public static class PuzzlePieceGenerator
{
    public static PuzzlePieceShape[,] GenerateShapes(int rows, int columns, PuzzlePieceMode mode = PuzzlePieceMode.Classic)
    {
        var shapes = new PuzzlePieceShape[rows, columns];
        if (mode == PuzzlePieceMode.Tiles) return shapes;

        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var shape = new PuzzlePieceShape
                {
                    Top = row == 0 ? 0 : -shapes[row - 1, column].Bottom,
                    Left = column == 0 ? 0 : -shapes[row, column - 1].Right,
                    Right = column == columns - 1 ? 0 : (Random.value > 0.5f ? 1 : -1),
                    Bottom = row == rows - 1 ? 0 : (Random.value > 0.5f ? 1 : -1)
                };

                shapes[row, column] = shape;
            }
        }

        return shapes;
    }

    public static PuzzleSliceBounds CalculateSliceBounds(int row, int column, int rows, int columns, int textureWidth, int textureHeight)
    {
        var xMin = Mathf.FloorToInt(column * textureWidth / (float)columns);
        var xMax = Mathf.FloorToInt((column + 1) * textureWidth / (float)columns);
        var yMin = Mathf.FloorToInt((rows - row - 1) * textureHeight / (float)rows);
        var yMax = Mathf.FloorToInt((rows - row) * textureHeight / (float)rows);
        return new PuzzleSliceBounds(xMin, xMax, yMin, yMax);
    }

    public static Sprite CreatePieceSprite(Texture2D sourceTexture, PuzzleSliceBounds slice, PuzzlePieceShape shape, int tabMargin)
    {
        var textureWidth = slice.Width + tabMargin * 2;
        var textureHeight = slice.Height + tabMargin * 2;
        var pieceTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = $"Piece_{slice.XMin}_{slice.YMin}"
        };
        // Write directly into the readable texture: UI alpha hit testing needs
        // this CPU copy, but a second managed Color[] copy is unnecessary.
        var pixels = pieceTexture.GetPixelData<Color32>(0);
        var sourcePixels = sourceTexture.format == TextureFormat.RGBA32
            ? sourceTexture.GetPixelData<Color32>(0) : default;
        var sourceWidth = sourceTexture.width;
        var sourceHeight = sourceTexture.height;
        var mask = ArrayPool<bool>.Shared.Rent(pixels.Length);

        try
        {
            for (var y = 0; y < textureHeight; y++)
            {
                for (var x = 0; x < textureWidth; x++)
                {
                    var inside = IsInsidePieceMask(x + 0.5f, y + 0.5f, slice.Width, slice.Height, tabMargin, shape);
                    var index = y * textureWidth + x;
                    mask[index] = inside;

                    if (!inside)
                    {
                        pixels[index] = default;
                        continue;
                    }

                    var sampleX = slice.XMin - tabMargin + x;
                    var sampleY = slice.YMin - tabMargin + y;
                    pixels[index] = sampleX < 0 || sampleX >= sourceWidth || sampleY < 0 || sampleY >= sourceHeight
                        ? default
                        : sourcePixels.IsCreated ? sourcePixels[sampleY * sourceWidth + sampleX]
                        : (Color32)sourceTexture.GetPixel(sampleX, sampleY);
                }
            }

            ShadeMaskBorder(pixels, mask, textureWidth, textureHeight);

            pieceTexture.Apply(false, false);

            return Sprite.Create(
                pieceTexture,
                new Rect(0f, 0f, textureWidth, textureHeight),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.Tight);
        }
        catch
        {
            Object.Destroy(pieceTexture);
            throw;
        }
        finally { ArrayPool<bool>.Shared.Return(mask); }
    }

    private static bool IsInsidePieceMask(float x, float y, int cellWidth, int cellHeight, int tabMargin, PuzzlePieceShape shape)
    {
        var left = tabMargin;
        var right = tabMargin + cellWidth;
        var bottom = tabMargin;
        var top = tabMargin + cellHeight;

        var inside = x >= left && x <= right && y >= bottom && y <= top;

        ApplyVerticalConnector(ref inside, x, y, left, (bottom + top) * 0.5f, -1f, shape.Left, cellWidth, cellHeight);
        ApplyVerticalConnector(ref inside, x, y, right, (bottom + top) * 0.5f, 1f, shape.Right, cellWidth, cellHeight);
        ApplyHorizontalConnector(ref inside, x, y, (left + right) * 0.5f, bottom, -1f, shape.Bottom, cellWidth, cellHeight);
        ApplyHorizontalConnector(ref inside, x, y, (left + right) * 0.5f, top, 1f, shape.Top, cellWidth, cellHeight);

        return inside;
    }

    private static void ApplyHorizontalConnector(ref bool inside, float x, float y, float centerX, float edgeY, float outwardSign, int connector, int cellWidth, int cellHeight)
    {
        if (connector == 0)
        {
            return;
        }

        var radius = Mathf.Min(cellWidth, cellHeight) * 0.225f;
        var neckHalf = radius * 0.44f;
        var neckDepth = radius * 0.70f;
        var circleCenterY = edgeY + outwardSign * connector * radius * 0.52f;

        var inNeck = Mathf.Abs(x - centerX) <= neckHalf && Between(y, edgeY, edgeY + outwardSign * connector * neckDepth);
        var inCircle = (x - centerX) * (x - centerX) + (y - circleCenterY) * (y - circleCenterY) <= radius * radius;

        if (connector > 0)
        {
            if (inNeck || inCircle)
            {
                inside = true;
            }
        }
        else if (inNeck || inCircle)
        {
            inside = false;
        }
    }

    private static void ApplyVerticalConnector(ref bool inside, float x, float y, float edgeX, float centerY, float outwardSign, int connector, int cellWidth, int cellHeight)
    {
        if (connector == 0)
        {
            return;
        }

        var radius = Mathf.Min(cellWidth, cellHeight) * 0.225f;
        var neckHalf = radius * 0.44f;
        var neckDepth = radius * 0.70f;
        var circleCenterX = edgeX + outwardSign * connector * radius * 0.52f;

        var inNeck = Mathf.Abs(y - centerY) <= neckHalf && Between(x, edgeX, edgeX + outwardSign * connector * neckDepth);
        var inCircle = (x - circleCenterX) * (x - circleCenterX) + (y - centerY) * (y - centerY) <= radius * radius;

        if (connector > 0)
        {
            if (inNeck || inCircle)
            {
                inside = true;
            }
        }
        else if (inNeck || inCircle)
        {
            inside = false;
        }
    }

    private static bool Between(float value, float start, float end)
    {
        return value >= Mathf.Min(start, end) && value <= Mathf.Max(start, end);
    }

    private static void ShadeMaskBorder(NativeArray<Color32> pixels, bool[] mask, int width, int height)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = y * width + x;
                if (!mask[index])
                {
                    continue;
                }

                var edgeCount = 0;
                for (var offsetY = -1; offsetY <= 1; offsetY++)
                {
                    for (var offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        if (offsetX == 0 && offsetY == 0)
                        {
                            continue;
                        }

                        var neighborX = x + offsetX;
                        var neighborY = y + offsetY;
                        if (neighborX < 0 || neighborX >= width || neighborY < 0 || neighborY >= height)
                        {
                            edgeCount++;
                            continue;
                        }

                        if (!mask[neighborY * width + neighborX])
                        {
                            edgeCount++;
                        }
                    }
                }

                if (edgeCount > 0)
                {
                    pixels[index] = Color.Lerp(pixels[index], new Color(0.10f, 0.10f, 0.10f, 1f), Mathf.Clamp01(edgeCount * 0.05f));
                }
            }
        }
    }
}
