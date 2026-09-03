using System;
using UnityEngine;

[Serializable]
public struct PuzzleFrameStyle
{
    [Min(1f)] public float outerWidth;
    [Min(0f)] public float shadowWidth;
    [Min(0f)] public float trimWidth;
    [Min(0f)] public float matWidth;
    public Color outerColor;
    public Color shadowColor;
    public Color trimColor;
    public Color matColor;

    public float TotalInset => Mathf.Max(1f, outerWidth) + Mathf.Max(0f, shadowWidth) + Mathf.Max(0f, trimWidth) + Mathf.Max(0f, matWidth);

    public static PuzzleFrameStyle Default => new()
    {
        outerWidth = 30f,
        shadowWidth = 7f,
        trimWidth = 5f,
        matWidth = 12f,
        outerColor = new Color(0.36f, 0.19f, 0.05f, 1f),
        shadowColor = new Color(0.13f, 0.06f, 0.015f, 0.82f),
        trimColor = new Color(0.88f, 0.63f, 0.20f, 1f),
        matColor = new Color(0.94f, 0.86f, 0.65f, 1f)
    };
}
