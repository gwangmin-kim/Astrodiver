using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Astrodiver.Editor
{
    /// <summary>
    /// Sets each tileset sprite's Custom Physics Shape to the axis-aligned bounding box
    /// of its opaque pixels. Tiles whose art leaves a transparent margin at the edge
    /// then get a collider that matches the visible surface instead of the full cell.
    /// Requires tiles to use Collider Type = Sprite.
    /// </summary>
    public static class TilesetPhysicsShapeGenerator
    {
        private const string DefaultFolder = "Assets/Arts/Sprites/Tilemaps";
        private const string MenuRoot = "Tools/Astrodiver/Tileset Physics Shapes/";
        private const byte AlphaThreshold = 1;

        [MenuItem(MenuRoot + "Generate For Tilemaps Folder")]
        private static void GenerateForDefaultFolder()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { DefaultFolder });
            var paths = new List<string>();
            foreach (string guid in guids)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            Generate(paths);
        }

        [MenuItem(MenuRoot + "Generate For Selected Textures")]
        private static void GenerateForSelection()
        {
            var paths = new List<string>();
            foreach (Object obj in Selection.GetFiltered<Texture2D>(SelectionMode.Assets))
            {
                paths.Add(AssetDatabase.GetAssetPath(obj));
            }

            Generate(paths);
        }

        [MenuItem(MenuRoot + "Generate For Selected Textures", true)]
        private static bool ValidateGenerateForSelection()
        {
            return Selection.GetFiltered<Texture2D>(SelectionMode.Assets).Length > 0;
        }

        public static void Generate(IEnumerable<string> texturePaths)
        {
            int textureCount = 0;
            int spriteCount = 0;

            foreach (string path in texturePaths)
            {
                int updated = GenerateForTexture(path);
                if (updated < 0)
                {
                    continue;
                }

                textureCount++;
                spriteCount += updated;
            }

            Debug.Log(
                $"[TilesetPhysicsShapeGenerator] Updated {spriteCount} sprite physics shapes " +
                $"in {textureCount} texture(s).");
        }

        private static int GenerateForTexture(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer ||
                importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                Debug.LogWarning(
                    $"[TilesetPhysicsShapeGenerator] Skipped '{path}': not a Multiple-mode sprite texture.");
                return -1;
            }

            // Read source pixels from the file so the texture does not need Read/Write enabled.
            var pixels = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!pixels.LoadImage(File.ReadAllBytes(path)))
                {
                    Debug.LogWarning($"[TilesetPhysicsShapeGenerator] Could not decode '{path}'.");
                    return -1;
                }

                var factory = new SpriteDataProviderFactories();
                factory.Init();
                ISpriteEditorDataProvider dataProvider =
                    factory.GetSpriteEditorDataProviderFromObject(importer);
                dataProvider.InitSpriteEditorDataProvider();

                var physicsProvider =
                    dataProvider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
                SpriteRect[] spriteRects = dataProvider.GetSpriteRects();
                Color32[] sourcePixels = pixels.GetPixels32();
                int updated = 0;

                foreach (SpriteRect spriteRect in spriteRects)
                {
                    var outlines = new List<Vector2[]>();
                    if (TryGetOpaqueBounds(sourcePixels, pixels.width, spriteRect.rect, out RectInt bounds))
                    {
                        // Physics outline points are in pixels, relative to the sprite rect center.
                        Vector2 center = spriteRect.rect.size * 0.5f;
                        float xMin = bounds.xMin - center.x;
                        float yMin = bounds.yMin - center.y;
                        float xMax = bounds.xMax - center.x;
                        float yMax = bounds.yMax - center.y;
                        outlines.Add(new[]
                        {
                            new Vector2(xMin, yMin),
                            new Vector2(xMin, yMax),
                            new Vector2(xMax, yMax),
                            new Vector2(xMax, yMin),
                        });
                    }

                    physicsProvider.SetOutlines(spriteRect.spriteID, outlines);
                    updated++;
                }

                dataProvider.Apply();
                importer.SaveAndReimport();
                return updated;
            }
            finally
            {
                Object.DestroyImmediate(pixels);
            }
        }

        /// <summary>
        /// Bounds of opaque pixels, in pixels relative to the sprite rect's bottom-left.
        /// </summary>
        private static bool TryGetOpaqueBounds(
            Color32[] block, int textureWidth, Rect rect, out RectInt bounds)
        {
            int x0 = Mathf.RoundToInt(rect.x);
            int y0 = Mathf.RoundToInt(rect.y);
            int width = Mathf.RoundToInt(rect.width);
            int height = Mathf.RoundToInt(rect.height);

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            for (int y = 0; y < height; y++)
            {
                int row = (y0 + y) * textureWidth;
                for (int x = 0; x < width; x++)
                {
                    if (block[row + x0 + x].a < AlphaThreshold)
                    {
                        continue;
                    }

                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX < minX)
            {
                bounds = default;
                return false;
            }

            bounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
            return true;
        }
    }
}
