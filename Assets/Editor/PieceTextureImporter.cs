using UnityEditor;

// Ustawienia importu dla grafik figur z Assets/Resources/Pieces:
// sprite o rozmiarze dokładnie 1 jednostki (256 px = 1 pole), bez kompresji i mipmap.
// Zestaw "pixel" dostaje filtr Point, żeby piksele zostały ostre.
public class PieceTextureImporter : AssetPostprocessor
{
    const string PiecesPath = "Assets/Resources/Pieces/";

    public override uint GetVersion() => 1;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(PiecesPath)) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 256;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = assetPath.Contains("/pixel/")
            ? UnityEngine.FilterMode.Point
            : UnityEngine.FilterMode.Bilinear;
    }
}
