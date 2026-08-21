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
    private const string WorktableRecipePath =
        "Assets/Data/Facilities/WorktableRecipeTable.asset";
    private const string WorktableServicePrefabPath =
        "Assets/Resources/Prefabs/DontDestroyOnLoad/WorktableService.prefab";

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
            WorktableRecipeTable worktableRecipes =
                AssetDatabase.LoadAssetAtPath<WorktableRecipeTable>(WorktableRecipePath);
            Require(worktableRecipes != null, "Worktable recipe table could not be loaded.");
            Require(
                worktableRecipes.TryValidate(out string recipeError),
                recipeError);
            Require(
                worktableRecipes.Recipes.Count == catalog.Creatures.Count,
                "Every creature must have exactly one worktable recipe.");
            for (int i = 0; i < catalog.Creatures.Count; i++)
            {
                Require(
                    worktableRecipes.TryGetRecipe(
                        catalog.Creatures[i].Id,
                        out WorktableRecipe recipe) &&
                    recipe.Resource != null,
                    $"Creature '{catalog.Creatures[i].Id}' has no worktable recipe.");
            }

            GameObject worktableServicePrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    WorktableServicePrefabPath);
            WorktableService worktableService =
                worktableServicePrefab != null
                    ? worktableServicePrefab.GetComponent<WorktableService>()
                    : null;
            Require(
                worktableServicePrefab != null &&
                worktableService != null,
                "Worktable service prefab is missing its service component.");
            SerializedObject serializedWorktableService =
                new(worktableService);
            Require(
                serializedWorktableService.FindProperty("_recipeTable")
                    .objectReferenceValue == worktableRecipes,
                "Worktable service prefab is not connected to its recipe table.");
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
                        NumericUpgradeTarget.MovementSpeedRatio,
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
            FacilityRuntimeData facilities = runtimeData.Facilities;
            FacilityRuntimeData independentFacilities = independentRuntimeData.Facilities;
            Require(
                !ReferenceEquals(source, independentDefaultsCopy) &&
                !ReferenceEquals(source.inventory, independentDefaultsCopy.inventory) &&
                !ReferenceEquals(source.resourceChest, independentDefaultsCopy.resourceChest) &&
                !ReferenceEquals(source.worktable, independentDefaultsCopy.worktable) &&
                !ReferenceEquals(
                    source.worktable.Inventory,
                    independentDefaultsCopy.worktable.Inventory),
                "GameDataDefaults returned a shared mutable data object.");
            Require(
                !ReferenceEquals(runtimeData, independentRuntimeData) &&
                !ReferenceEquals(playerStats, independentPlayerStats) &&
                !ReferenceEquals(equipment, independentEquipment) &&
                !ReferenceEquals(runtimeData.Inventory, independentRuntimeData.Inventory) &&
                !ReferenceEquals(facilities, independentFacilities),
                "GameDataDefaults returned shared mutable runtime data.");
            InventoryData inventoryReference = source.inventory;
            source.inventory = new InventoryData(
                new[] { new CreatureInventoryEntry(creatureDefinition.Id, 2) },
                new[] { new ResourceInventoryEntry(resourceDefinition.Id, 42) });
            inventoryReference = source.inventory;
            Require(
                independentDefaultsCopy.inventory.GetResourceAmount(resourceDefinition.Id) == 0,
                "Mutating a defaults copy changed another defaults copy.");

            source.resourceChest = new InventoryData(
                null,
                new[] { new ResourceInventoryEntry(resourceDefinition.Id, 9) });
            source.worktable.Inventory.CopyFrom(new InventoryData(
                new[] { new CreatureInventoryEntry(creatureDefinition.Id, 1) },
                null));
            source.unlockedUpgradeIds.Add("movement.speed");
            source.upgradeNodes.Add(new UpgradeNodeSaveData
            {
                nodeId = "battery.capacity",
                level = 3
            });
            UpgradeEffect movementEffect = temporaryNode.Effects[0];
            UpgradeEffectContext effectContext = new(runtimeData);
            float movementSpeedBeforeEffect =
                runtimeData.PlayerStats.movement.baseMoveSpeed;
            float movementSpeedRatioBeforeEffect =
                runtimeData.PlayerStats.movement.moveSpeedRatio;
            Require(movementEffect.TryApply(effectContext, out string effectError), effectError);
            Require(
                Mathf.Approximately(
                    runtimeData.PlayerStats.movement.baseMoveSpeed,
                    movementSpeedBeforeEffect) &&
                Mathf.Approximately(
                    runtimeData.PlayerStats.movement.moveSpeedRatio,
                    movementSpeedRatioBeforeEffect + 0.5f) &&
                Mathf.Approximately(
                    runtimeData.PlayerStats.movement.MoveSpeed,
                    movementSpeedBeforeEffect *
                    (movementSpeedRatioBeforeEffect + 0.5f)),
                "The ratio upgrade did not update only the ratio field.");

            UpgradeEffect batteryCapacityEffect = new NumericUpgradeEffect(
                NumericUpgradeTarget.BatteryCapacity,
                NumericUpgradeOperation.Set,
                20f);
            Require(
                batteryCapacityEffect.TryApply(effectContext, out string batteryEffectError),
                batteryEffectError);
            Require(
                Mathf.Approximately(runtimeData.PlayerStats.battery.amount, 20f),
                "Battery capacity was not applied as an absolute value.");

            int plasmaDamageBeforeEffect = runtimeData.Equipment.plasmaGun.tickDamage;
            UpgradeEffect plasmaDamageEffect = new NumericUpgradeEffect(
                NumericUpgradeTarget.PlasmaDamage,
                NumericUpgradeOperation.Add,
                25f);
            Require(
                plasmaDamageEffect.TryApply(effectContext, out string plasmaDamageEffectError),
                plasmaDamageEffectError);
            Require(
                runtimeData.Equipment.plasmaGun.tickDamage == plasmaDamageBeforeEffect + 25,
                "Plasma damage was not applied as an absolute value.");

            float plasmaChargeTimeBeforeEffect =
                runtimeData.Equipment.plasmaGun.baseChargeTime;
            float plasmaChargeSpeedBeforeEffect =
                runtimeData.Equipment.plasmaGun.chargeSpeedMultiplier;
            UpgradeEffect plasmaChargeSpeedEffect = new NumericUpgradeEffect(
                NumericUpgradeTarget.PlasmaChargeSpeedMultiplier,
                NumericUpgradeOperation.Add,
                0.5f);
            Require(
                plasmaChargeSpeedEffect.TryApply(
                    effectContext,
                    out string plasmaChargeSpeedEffectError),
                plasmaChargeSpeedEffectError);
            Require(
                Mathf.Approximately(
                    runtimeData.Equipment.plasmaGun.baseChargeTime,
                    plasmaChargeTimeBeforeEffect) &&
                Mathf.Approximately(
                    runtimeData.Equipment.plasmaGun.chargeSpeedMultiplier,
                    plasmaChargeSpeedBeforeEffect + 0.5f),
                "Plasma charge speed upgrade changed the base charge time.");

            UpgradeEffect timeoutLossEffect = new NumericUpgradeEffect(
                NumericUpgradeTarget.TimeoutInventoryLossRatio,
                NumericUpgradeOperation.Set,
                0.8f);
            Require(
                timeoutLossEffect.TryApply(effectContext, out string timeoutLossEffectError),
                timeoutLossEffectError);
            Require(
                Mathf.Approximately(runtimeData.Inventory.TimeoutInventoryLossRatio, 0.8f),
                "Timeout inventory loss ratio was not applied as an absolute value.");
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
            Require(
                runtimeData.Inventory.CreatureMaxStackCount == 10,
                "The default creature max stack count is incorrect.");
            UpgradeEffect creatureMaxStackEffect = new NumericUpgradeEffect(
                NumericUpgradeTarget.CreatureMaxStackCount,
                NumericUpgradeOperation.Add,
                2f);
            Require(
                creatureMaxStackEffect.TryApply(
                    effectContext,
                    out string creatureMaxStackEffectError),
                creatureMaxStackEffectError);
            Require(
                creatureMaxStackEffect.TryApply(
                    effectContext,
                    out creatureMaxStackEffectError),
                creatureMaxStackEffectError);
            Require(
                runtimeData.Inventory.CreatureMaxStackCount == 14,
                "The creature max stack count upgrade was not applied once per level.");
            Require(
                independentRuntimeData.Inventory.CreatureMaxStackCount == 10,
                "The creature max stack count upgrade changed another defaults copy.");
            GameRuntimeData rebuiltRuntimeData = defaults.CreateRuntimeData();
            UpgradeEffectContext rebuiltEffectContext = new(rebuiltRuntimeData);
            Require(
                creatureMaxStackEffect.TryApply(
                    rebuiltEffectContext,
                    out creatureMaxStackEffectError) &&
                creatureMaxStackEffect.TryApply(
                    rebuiltEffectContext,
                    out creatureMaxStackEffectError),
                creatureMaxStackEffectError);
            Require(
                rebuiltRuntimeData.Inventory.CreatureMaxStackCount ==
                runtimeData.Inventory.CreatureMaxStackCount,
                "Rebuilding runtime data did not reproduce the stack count upgrade levels.");

            NumericUpgradeEffect decreasingStackMultiplier = new(
                NumericUpgradeTarget.CreatureMaxStackCount,
                NumericUpgradeOperation.Multiply,
                -0.5f);
            Require(
                !decreasingStackMultiplier.TryValidate(out _),
                "A numeric effect accepted a negative multiplier.");
            UpgradeEffect netGunUnlockEffect = new UnlockUpgradeEffect(
                UnlockUpgradeTarget.NetGun);
            Require(
                netGunUnlockEffect.TryApply(effectContext, out string unlockEffectError),
                unlockEffectError);
            Require(
                runtimeData.Equipment.netGun.isUnlocked,
                "The unlock upgrade effect did not unlock the net gun.");

            UpgradeEffect resourceChestUnlockEffect = new UnlockUpgradeEffect(
                UnlockUpgradeTarget.ResourceChest);
            Require(
                resourceChestUnlockEffect.TryApply(
                    effectContext,
                    out string facilityUnlockError),
                facilityUnlockError);
            Require(
                runtimeData.Facilities.ResourceChestUnlocked &&
                !independentRuntimeData.Facilities.ResourceChestUnlocked,
                "The unlock upgrade effect did not isolate the resource chest state.");
            UpgradeEffect worktableUnlockEffect = new UnlockUpgradeEffect(
                UnlockUpgradeTarget.Worktable);
            Require(
                worktableUnlockEffect.TryApply(
                    effectContext,
                    out string worktableUnlockError),
                worktableUnlockError);
            Require(
                runtimeData.Facilities.WorktableUnlocked &&
                !independentRuntimeData.Facilities.WorktableUnlocked,
                "The unlock upgrade effect did not isolate the worktable state.");
            UpgradeEffect worktableCapacityEffect = new NumericUpgradeEffect(
                NumericUpgradeTarget.WorktableSlotCapacity,
                NumericUpgradeOperation.Add,
                2f);
            Require(
                worktableCapacityEffect.TryApply(
                    effectContext,
                    out string worktableCapacityError),
                worktableCapacityError);
            Require(
                runtimeData.Facilities.WorktableSlotCapacity == 3,
                "The worktable slot capacity upgrade was not applied.");
            Require(
                !Mathf.Approximately(
                    runtimeData.PlayerStats.movement.moveSpeedRatio,
                    independentPlayerStats.movement.moveSpeedRatio),
                "Applying an upgrade changed another defaults copy.");
            source.completedEvents.Add(GameProgressEventId.None);

            Require(
                GameDataFileStore.TrySave(testPath, source, out string saveError),
                $"Save failed: {saveError}");
            string savedJson = File.ReadAllText(testPath);
            Require(
                !savedJson.Contains("\"playerStats\"") &&
                !savedJson.Contains("\"equipment\"") &&
                !savedJson.Contains("creatureSlotCapacity") &&
                !savedJson.Contains("creatureMaxStackCount"),
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
                loaded.resourceChest.GetResourceAmount(resourceDefinition.Id) == 9,
                "Resource chest amount was not preserved.");
            Require(
                loaded.worktable.Inventory.Creatures.Count == 1 &&
                loaded.worktable.Inventory.Creatures[0].DefinitionId ==
                    creatureDefinition.Id &&
                loaded.worktable.Inventory.Creatures[0].Count == 1,
                "Worktable inventory was not preserved.");
            Require(
                loaded.upgradeNodes.Exists(entry =>
                    entry.nodeId == "movement.speed" && entry.level == 1),
                "The legacy upgrade id was not migrated to level 1.");
            Require(
                loaded.upgradeNodes.Exists(entry =>
                    entry.nodeId == "battery.capacity" && entry.level == 3),
                "Upgrade node level was not preserved.");
            Require(
                loaded.completedEvents.Count == 0,
                "Invalid progress event ids were not removed during normalization.");

            source.completedEvents.Clear();
            source.completedEvents.Add((GameProgressEventId)1000);
            source.completedEvents.Add((GameProgressEventId)1100);
            // Retired initialized fields remain here only to verify that legacy saves ignore them.
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
            Require(
                migrated.completedEvents.Count == 0,
                "Legacy upgrade unlock events were not removed from progress events.");

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
