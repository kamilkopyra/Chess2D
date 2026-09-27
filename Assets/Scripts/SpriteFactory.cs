using System.Collections.Generic;
using UnityEngine;

// Proste sprite'y generowane w kodzie (białe, kolor nadaje SpriteRenderer.color).
// Wszystkie mają rozmiar 1x1 jednostki, więc skaluje się je przez transform.
public static class SpriteFactory
{
    const int Size = 128;
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static Sprite Square => Get("square", (x, y) => 1f);

    // Pełne koło o promieniu r (0..0.5 szerokości)
    public static Sprite Circle => Get("circle", (x, y) => Coverage(Dist(x, y), 0.5f));

    // Pierścień przy krawędzi pola (podpowiedź bicia)
    public static Sprite Ring => Get("ring", (x, y) =>
    {
        float d = Dist(x, y);
        return Mathf.Min(Coverage(d, 0.5f), 1f - Coverage(d, 0.41f));
    });

    // Miękka czerwona poświata pod królem w szachu
    public static Sprite Glow => Get("glow", (x, y) =>
    {
        float t = Mathf.Clamp01(Dist(x, y) / 0.5f);
        return Mathf.Pow(1f - t, 1.6f);
    });

    // Kwadrat z zaokrąglonymi rogami (ramka i cień planszy). radius w ułamku boku.
    public static Sprite RoundedSquare(float radius) => Get("rounded" + radius, (x, y) =>
    {
        float px = Mathf.Abs(x - 0.5f), py = Mathf.Abs(y - 0.5f);
        float inner = 0.5f - radius;
        float dx = Mathf.Max(px - inner, 0f), dy = Mathf.Max(py - inner, 0f);
        return Coverage(Mathf.Sqrt(dx * dx + dy * dy), radius);
    });

    // Rozmyty kwadrat (cień)
    public static Sprite SoftSquare(float blur) => Get("soft" + blur, (x, y) =>
    {
        float px = Mathf.Abs(x - 0.5f), py = Mathf.Abs(y - 0.5f);
        float inner = 0.5f - blur;
        float dx = Mathf.Max(px - inner, 0f), dy = Mathf.Max(py - inner, 0f);
        float t = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / blur);
        return 1f - t * t * (3f - 2f * t);
    });

    static float Dist(float x, float y) => Mathf.Sqrt((x - 0.5f) * (x - 0.5f) + (y - 0.5f) * (y - 0.5f));

    // Wygładzona krawędź (antyaliasing ~1 piksel)
    static float Coverage(float dist, float radius) => Mathf.Clamp01((radius - dist) * Size + 0.5f);

    static Sprite Get(string key, System.Func<float, float, float> alpha)
    {
        if (cache.TryGetValue(key, out Sprite s) && s != null) return s;

        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = key
        };

        var pixels = new Color32[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float a = alpha((x + 0.5f) / Size, (y + 0.5f) / Size);
                pixels[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        s = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        s.name = key;
        cache[key] = s;
        return s;
    }
}
