using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

internal static class StageMapDefaultTiles
{
    private const string DataFolder = "Assets/Data";
    private const string StageMapsFolder = "Assets/Data/StageMaps";
    private const string CommonFolder = "Assets/Data/StageMaps/Common";
    private const string TexturePath = CommonFolder + "/LogicalTileTexture.asset";

    internal const string PlatformPath =
        CommonFolder + "/PlatformLogicalTile.asset";
    internal const string DecorationBackPath =
        CommonFolder + "/DecorationBackLogicalTile.asset";
    internal const string DecorationFrontPath =
        CommonFolder + "/DecorationFrontLogicalTile.asset";

    internal static void EnsureAssets()
    {
        EnsureFolder("Assets", "Data");
        EnsureFolder(DataFolder, "StageMaps");
        EnsureFolder(StageMapsFolder, "Common");

        Sprite sprite = EnsureSprite();
        EnsureTile(
            PlatformPath,
            "PlatformLogicalTile",
            sprite,
            GetColor(StageMapLayer.Platform),
            Tile.ColliderType.Grid);
        EnsureTile(
            DecorationBackPath,
            "DecorationBackLogicalTile",
            sprite,
            GetColor(StageMapLayer.DecorationBack),
            Tile.ColliderType.None);
        EnsureTile(
            DecorationFrontPath,
            "DecorationFrontLogicalTile",
            sprite,
            GetColor(StageMapLayer.DecorationFront),
            Tile.ColliderType.None);

        AssetDatabase.SaveAssets();
    }

    internal static Color GetColor(StageMapLayer layer)
    {
        return layer switch
        {
            StageMapLayer.Platform => Color.white,
            StageMapLayer.DecorationBack =>
                new Color32(255, 166, 201, 255),
            StageMapLayer.DecorationFront =>
                new Color32(143, 217, 251, 255),
            _ => Color.magenta
        };
    }

    internal static Tile Get(StageMapLayer layer)
    {
        string path = layer switch
        {
            StageMapLayer.Platform => PlatformPath,
            StageMapLayer.DecorationBack => DecorationBackPath,
            StageMapLayer.DecorationFront => DecorationFrontPath,
            _ => string.Empty
        };

        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile != null)
        {
            return tile;
        }

        EnsureAssets();
        return AssetDatabase.LoadAssetAtPath<Tile>(path);
    }

    private static Sprite EnsureSprite()
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        if (texture == null)
        {
            texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "Logical Tile Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, false);
            AssetDatabase.CreateAsset(texture, TexturePath);
        }

        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(TexturePath)
            .OfType<Sprite>()
            .FirstOrDefault();
        if (sprite != null)
        {
            return sprite;
        }

        sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f,
            0,
            SpriteMeshType.FullRect);
        sprite.name = "Logical Tile Sprite";
        AssetDatabase.AddObjectToAsset(sprite, texture);
        EditorUtility.SetDirty(texture);
        AssetDatabase.SaveAssets();
        return sprite;
    }

    private static void EnsureTile(
        string path,
        string name,
        Sprite sprite,
        Color color,
        Tile.ColliderType colliderType)
    {
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }

        tile.name = name;
        tile.sprite = sprite;
        tile.color = color;
        tile.colliderType = colliderType;
        tile.flags = TileFlags.LockColor | TileFlags.LockTransform;
        EditorUtility.SetDirty(tile);
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
