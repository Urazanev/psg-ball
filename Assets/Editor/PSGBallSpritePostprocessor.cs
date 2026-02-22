using System;
using UnityEditor;

public class PSGBallSpritePostprocessor : AssetPostprocessor
{
    const string UiSpriteFolder = "Assets/UI/Sprites/PSG-Ball/";
    const string ResourceSpriteFolder = "Assets/Resources/UI/Sprites/PSG-Ball/";
    const float PixelsPerUnit = 256f;

    void OnPreprocessTexture()
    {
        if (!IsPsgBallSprite(assetPath))
            return;

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
    }

    static bool IsPsgBallSprite(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        return path.StartsWith(UiSpriteFolder, StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith(ResourceSpriteFolder, StringComparison.OrdinalIgnoreCase);
    }
}
