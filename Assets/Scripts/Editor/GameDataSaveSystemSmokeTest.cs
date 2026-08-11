using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GameDataSaveSystemSmokeTest
{
    private const string MenuPath = "Astrodiver/Tests/Run Save System Smoke Test";
    private const string DefaultsPath = "Assets/Data/GameDataDefaults.asset";
    private const string DefinitionCatalogPath = "Assets/Data/GameDefinitionCatalog.asset";

    [MenuItem(MenuPath)]
    public static void Run()
    {
        string testDirectory = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Library",
            "CodexSaveSystemSmokeTest");
        string testPath = Path.Combine(testDirectory, "test-save.json");
        string legacyTestPath = Path.Combine(testDirectory, "legacy-test-save.json");
        string newGameTestPath = Path.Combine(testDirectory, "new-game-save.json");
        UpgradeNodeDefinition temporaryNode = null;

        try
        {
            GameDataDefaults defaults =
                AssetDatabase.LoadAssetAtPath<GameDataDefaults>(DefaultsPath);
            Require(defaults != null, "GameDataDefaults asset could not be loaded.");
            GameDefinitionCatalog catalog =
                AssetDatabase.LoadAssetAtPath<GameDefinitionCatalog>(DefinitionCatalogPath);
            Require(catalog != null, "GameDefinitionCatalog asset could not be loaded.");
            Require(catalog.TryValidate(out string catalogError), catalogError);
            GameDefinitionRegistry definitions = new(catalog);
            Require(catalog.Resources.Count > 0, "The catalog has no resource definitions.");
            Require(catalog.Creatures.Count > 0, "The catalog has no creature definitions.");
            ResourceDefinition resourceDefinition = catalog.Resources[0];
            CreatureDefinition creatureDefinition = catalog.Creatures[0];
            Require(
                definitions.TryGetResource(resourceDefinition.Id, out ResourceDefinition resolvedResource) &&
                resolvedResource == resourceDefinition,
                "The resource definition could not be resolved by id.");
            Require(
                definitions.TryGetCreature(creatureDefinition.Id, out CreatureDefinition resolvedCreature) &&
                resolvedCreature == creatureDefinition,
                "The creature definition could not be resolved by id.");

            temporaryNode = ScriptableObject.CreateInstance<UpgradeNodeDefinition>();
            temporaryNode.ConfigureForEditor(
                "test.movement.speed",
                null,
                3,
                new[] { new UpgradeResourceCost(resourceDefinition, 10) },
                new[] { new UpgradeResourceCost(resourceDefinition, 5) },
                new[]
                {
                    new NumericUpgradeEffect(
                        NumericUpgradeTarget.MovementSpeed,
                        NumericUpgradeOperation.Add,
                        0.5f)
                });
            List<UpgradeResourceCost> calculatedCosts = new();
            temporaryNode.GetCostForNextLevel(2, calculatedCosts);
            Require(
                calculatedCosts.Count == 1 && calculatedCosts[0].Amount == 20,
                "The linear upgrade cost was calculated incorrectly.");

            GameSaveData source = defaults.CreateSaveData();
            GameSaveData independentDefaultsCopy = defaults.CreateSaveData();
            GameRuntimeData runtimeData = defaults.CreateRuntimeData();
            GameRuntimeData independentRuntimeData = defaults.CreateRuntimeData();
            PlayerStatsRuntimeData playerStats = runtimeData.PlayerStats;
            PlayerStatsRuntimeData independentPlayerStats = independentRuntimeData.PlayerStats;
            EquipmentRuntimeData equipment = runtimeData.Equipment;
            EquipmentRuntimeData independentEquipment = independentRuntimeData.Equipment;
            Require(
                !ReferenceEquals(source, independentDefaultsCopy) &&
                !ReferenceEquals(source.inventory, independentDefaultsCopy.inventory),
                "GameDataDefaults returned a shared mutable data object.");
            Require(
                !ReferenceEquals(runtimeData, independentRuntimeData) &&
                !ReferenceEquals(playerStats, independentPlayerStats) &&
                !ReferenceEquals(equipment, independentEquipment) &&
                !ReferenceEquals(runtimeData.Inventory, independentRuntimeData.Inventory),
                "GameDataDefaults returned shared mutable runtime data.");
            InventoryData inventoryReference = source.inventory;
            source.inventory = new InventoryData(
                new[] { new CreatureInventoryEntry(creatureDefinition.Id, 2) },
                new[] { new ResourceInventoryEntry(resourceDefinition.Id, 42) });
            inventoryReference = source.inventory;
            Require(
                independentDefaultsCopy.inventory.GetResourceAmount(resourceDefinition.Id) == 0,
                "Mutating a defaults copy changed another defaults copy.");
            source.unlockedUpgradeIds.Add("movement.speed");
            source.upgradeNodes.Add(new UpgradeNodeSaveData
            {
                nodeId = "battery.capacity",
                level = 3
            });
            UpgradeEffect movementEffect = temporaryNode.Effects[0];
            List<GameProgressEventId> completedEvents = new();
            UpgradeEffectContext effectContext = new(
                runtimeData,
                eventId =>
                {
                    if (completedEvents.Contains(eventId))
                    {
                        return false;
                    }

                    completedEvents.Add(eventId);
                    return true;
                });
            float movementSpeedBeforeEffect =
                runtimeData.PlayerStats.movement.moveSpeed;
            Require(movementEffect.TryApply(effectContext, out string effectError), effectError);
            Require(
                Mathf.Approximately(
                    runtimeData.PlayerStats.movement.moveSpeed,
                    movementSpeedBeforeEffect + 0.5f),
                "The upgrade effect did not update runtime stats.");
            UpgradeEffect inventoryCapacityEffect = new NumericUpgradeEffect(
                NumericUpgradeTarget.CreatureSlotCapacity,
                NumericUpgradeOperation.Add,
                2f);
            int creatureSlotCapacityBeforeEffect =
                runtimeData.Inventory.CreatureSlotCapacity;
            Require(
                inventoryCapacityEffect.TryApply(effectContext, out string inventoryEffectError),
                inventoryEffectError);
            Require(
                runtimeData.Inventory.CreatureSlotCapacity ==
                creatureSlotCapacityBeforeEffect + 2,
                "The inventory capacity upgrade did not update runtime data.");
            UpgradeEffect netGunUnlockEffect = new UnlockUpgradeEffect(
                UnlockUpgradeTarget.NetGun);
            Require(
                netGunUnlockEffect.TryApply(effectContext, out string unlockEffectError),
                unlockEffectError);
            Require(
                runtimeData.Equipment.netGun.isUnlocked,
                "The unlock upgrade effect did not unlock the net gun.");
            UpgradeEffect progressEventEffect = new ProgressEventUpgradeEffect(
                GameProgressEventId.RootUpgradeUnlocked);
            Require(
                progressEventEffect.TryApply(effectContext, out string progressEventError),
                progressEventError);
            Require(
                progressEventEffect.TryApply(effectContext, out progressEventError),
                progressEventError);
            Require(
                completedEvents.Count == 1 &&
                completedEvents[0] == GameProgressEventId.RootUpgradeUnlocked,
                "The progress event upgrade effect was not idempotent.");
            Require(
                !Mathf.Approximately(
                    runtimeData.PlayerStats.movement.moveSpeed,
                    independentPlayerStats.movement.moveSpeed),
                "Applying an upgrade changed another defaults copy.");
            source.completedEvents.Add(GameProgressEventId.RootUpgradeUnlocked);
            source.completedEvents.Add(GameProgressEventId.RootUpgradeUnlocked);
            source.completedEvents.Add(GameProgressEventId.None);

            Require(
                GameDataFileStore.TrySave(testPath, source, out string saveError),
                $"Save failed: {saveError}");
            string savedJson = File.ReadAllText(testPath);
            Require(
                !savedJson.Contains("\"playerStats\"") &&
                !savedJson.Contains("\"equipment\"") &&
                !savedJson.Contains("creatureSlotCapacity"),
                "Derived runtime stats were written to the save file.");
            Require(
                ReferenceEquals(source.inventory, inventoryReference),
                "Saving replaced the live inventory object.");
            Require(
                GameDataFileStore.TryLoad(testPath, out GameSaveData loaded, out string loadError),
                $"Load failed: {loadError}");

            Require(loaded.schemaVersion == GameSaveData.CurrentSchemaVersion, "Schema version changed.");
            Require(
                loaded.inventory.Creatures.Count == source.inventory.Creatures.Count,
                "Creature entries were not preserved.");
            Require(
                loaded.inventory.Creatures[0].DefinitionId == creatureDefinition.Id &&
                loaded.inventory.Creatures[0].Count == 2,
                "Creature entry data was not preserved.");
            Require(loaded.inventory.ResourceAmounts.Count == 1, "Resource count was not preserved.");
            Require(
                loaded.inventory.ResourceAmounts[0].DefinitionId == resourceDefinition.Id,
                "Resource id changed.");
            Require(loaded.inventory.ResourceAmounts[0].Amount == 42, "Resource amount changed.");
            Require(
                loaded.upgradeNodes.Exists(entry =>
                    entry.nodeId == "movement.speed" && entry.level == 1),
                "The legacy upgrade id was not migrated to level 1.");
            Require(
                loaded.upgradeNodes.Exists(entry =>
                    entry.nodeId == "battery.capacity" && entry.level == 3),
                "Upgrade node level was not preserved.");
            Require(
                loaded.completedEvents.Count == 1 &&
                loaded.completedEvents.Contains(GameProgressEventId.RootUpgradeUnlocked),
                "Progress event ids were not normalized as a unique collection.");

            string legacyJson = JsonUtility.ToJson(source, true)
                .Replace(
                    $"\"schemaVersion\": {GameSaveData.CurrentSchemaVersion}",
                    "\"schemaVersion\": 4")
                .Replace("\"_creatures\":", "\"_creatureSlots\":")
                .Replace(
                    "\"inventory\": {",
                    "\"playerStats\": { \"movementInitialized\": true, " +
                    "\"movement\": { \"moveSpeed\": 999 } },\n  " +
                    "\"equipment\": { \"netGunInitialized\": true },\n  " +
                    "\"inventory\": {\n    \"initialized\": true,");
            File.WriteAllText(legacyTestPath, legacyJson);
            Require(
                GameDataFileStore.TryLoad(
                    legacyTestPath,
                    out GameSaveData migrated,
                    out string migrationError),
                $"Legacy save migration failed: {migrationError}");
            Require(
                migrated.schemaVersion == GameSaveData.CurrentSchemaVersion,
                "Legacy save schema was not upgraded.");
            Require(
                migrated.inventory.ResourceAmounts.Count == 1 &&
                migrated.inventory.ResourceAmounts[0].Amount == 42,
                "Legacy inventory data was not preserved.");
            Require(
                migrated.inventory.Creatures.Count == 1 &&
                migrated.inventory.Creatures[0].DefinitionId == creatureDefinition.Id &&
                migrated.inventory.Creatures[0].Count == 2,
                "Legacy creature slots were not migrated to creature entries.");

            GameSaveData previousRun = defaults.CreateSaveData();
            previousRun.inventory = new InventoryData(
                null,
                new[] { new ResourceInventoryEntry(resourceDefinition.Id, 99) });
            Require(
                GameDataFileStore.TrySave(newGameTestPath, previousRun, out string previousRunError),
                $"Previous run save failed: {previousRunError}");
            Require(
                GameDataFileStore.TrySave(newGameTestPath, previousRun, out previousRunError),
                $"Previous run backup creation failed: {previousRunError}");

            GameSaveData newRun = defaults.CreateSaveData();
            newRun.inventory = new InventoryData(
                null,
                new[] { new ResourceInventoryEntry(resourceDefinition.Id, 7) });
            Require(
                GameDataFileStore.TrySaveNewGame(newGameTestPath, newRun, out string newRunError),
                $"New game overwrite failed: {newRunError}");
            Require(
                GameDataFileStore.TryLoad(
                    newGameTestPath,
                    out GameSaveData loadedNewRun,
                    out string loadedNewRunError),
                $"New game primary load failed: {loadedNewRunError}");
            Require(
                GameDataFileStore.TryLoad(
                    newGameTestPath + ".bak",
                    out GameSaveData loadedNewRunBackup,
                    out string loadedNewRunBackupError),
                $"New game backup load failed: {loadedNewRunBackupError}");
            Require(
                loadedNewRun.inventory.GetResourceAmount(resourceDefinition.Id) == 7 &&
                loadedNewRunBackup.inventory.GetResourceAmount(resourceDefinition.Id) == 7,
                "A new game left the previous run in the primary or backup save file.");

            Debug.Log("SAVE_SYSTEM_SMOKE_TEST_PASSED");
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }

            if (temporaryNode != null)
            {
                UnityEngine.Object.DestroyImmediate(temporaryNode);
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
