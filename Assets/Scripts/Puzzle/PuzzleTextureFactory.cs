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
        Texture2D texture = null;
        try
        {
            Graphics.Blit(sourceTexture, renderTexture);
            RenderTexture.active = renderTexture;
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
            texture.Apply(false, false);
            return texture;
        }
        catch
        {
            if (texture != null) Object.Destroy(texture);
            throw;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
        }
    }

}
