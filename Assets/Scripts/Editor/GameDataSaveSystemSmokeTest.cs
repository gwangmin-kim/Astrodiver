using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GameDataSaveSystemSmokeTest
{
    private const string MenuPath = "Astrodiver/Tests/Run Save System Smoke Test";
    private const string DefinitionCatalogPath = "Assets/Data/GameDefinitionCatalog.asset";

    [MenuItem(MenuPath)]
    public static void Run()
    {
        string testDirectory = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Library",
            "CodexSaveSystemSmokeTest");
        string testPath = Path.Combine(testDirectory, "test-save.json");

        try
        {
            GameDataDefaults defaults = Resources.Load<GameDataDefaults>("GameDataDefaults");
            Require(defaults != null, "GameDataDefaults could not be loaded from Resources.");
            GameDefinitionCatalog catalog =
                AssetDatabase.LoadAssetAtPath<GameDefinitionCatalog>(DefinitionCatalogPath);
            Require(catalog != null, "GameDefinitionCatalog asset could not be loaded.");
            GameDefinitionRegistry definitions = new(catalog);
            Require(
                definitions.TryGetResource(
                    "default_fragment",
                    out ResourceDefinition resourceDefinition)
                && resourceDefinition != null,
                "The default resource definition could not be resolved by id.");
            Require(
                definitions.TryGetCreature(
                    "default_creature",
                    out CreatureDefinition creatureDefinition)
                && creatureDefinition != null,
                "The default creature definition could not be resolved by id.");

            GameSaveData source = defaults.CreateSaveData();
            source.inventory.resourceAmounts.Add(new ResourceAmountSaveData
            {
                definitionId = "default_fragment",
                amount = 42
            });
            source.unlockedUpgradeIds.Add("movement.speed");
            source.completedEventIds.Add("tutorial.first_entry");

            Require(
                GameDataFileStore.TrySave(testPath, source, out string saveError),
                $"Save failed: {saveError}");
            Require(
                GameDataFileStore.TryLoad(testPath, out GameSaveData loaded, out string loadError),
                $"Load failed: {loadError}");

            Require(loaded.schemaVersion == GameSaveData.CurrentSchemaVersion, "Schema version changed.");
            Require(loaded.inventory.initialized, "Inventory was not initialized.");
            Require(
                loaded.inventory.creatureSlots.Count == source.inventory.creatureSlots.Count,
                "Creature slot count was not preserved.");
            Require(loaded.inventory.resourceAmounts.Count == 1, "Resource count was not preserved.");
            Require(loaded.inventory.resourceAmounts[0].definitionId == "default_fragment", "Resource id changed.");
            Require(loaded.inventory.resourceAmounts[0].amount == 42, "Resource amount changed.");
            Require(loaded.playerStats.movement.moveSpeed == 5f, "Movement data changed.");
            Require(loaded.playerStats.battery.amount == 60f, "Battery data changed.");
            Require(loaded.playerStats.magnet.radius == 3f, "Magnet radius changed.");
            Require(
                loaded.playerStats.magnet.pullSpeedRange == new Vector2(1f, 10f),
                "Magnet pull speed range changed.");
            Require(loaded.playerStats.magnet.collectRadius == 0.5f, "Magnet collect radius changed.");
            Require(loaded.equipment.netGun.netData.captureCount == 4, "Net gun data changed.");
            Require(loaded.equipment.plasmaGun.tickDamage == 1f, "Plasma gun data changed.");
            Require(loaded.unlockedUpgradeIds.Contains("movement.speed"), "Upgrade id was not preserved.");
            Require(loaded.completedEventIds.Contains("tutorial.first_entry"), "Event id was not preserved.");

            Debug.Log("SAVE_SYSTEM_SMOKE_TEST_PASSED");
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
