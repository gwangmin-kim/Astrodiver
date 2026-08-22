using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-9000)]
[DisallowMultipleComponent]
public sealed class WorktableService : MonoBehaviour
{
    [SerializeField] private float _baseTransferInterval = 0.5f;
    [SerializeField] private float _saveCheckpointInterval = 5f;
    private const int MaxCompletionsPerFrame = 100;

    [SerializeField] private WorktableRecipeTable _recipeTable;

    private readonly List<CompletedRecipe> _completedBuffer = new();
    private GameDataManager _gameDataManager;
    private WorktableSaveData _data;
    private CreatureInventorySlot[] _slots =
        Array.Empty<CreatureInventorySlot>();
    private float _checkpointTimer;
    private bool _hasPendingChanges;

    private readonly struct CompletedRecipe
    {
        public CompletedRecipe(WorktableRecipe recipe, int amount)
        {
            Recipe = recipe;
            Amount = amount;
        }

        public WorktableRecipe Recipe { get; }
        public int Amount { get; }
    }

    public static WorktableService Instance { get; private set; }

    public event Action Changed;
    public event Action<CreatureDefinition, ResourceDefinition, int> Completed;

    public IReadOnlyList<CreatureInventorySlot> CreatureSlots => _slots;
    public int ProcessingSlotIndex
    {
        get
        {
            int slotIndex = FindLeftmostOccupiedSlot();
            return slotIndex >= 0 && HasRecipe(_slots[slotIndex].DefinitionId)
                ? slotIndex
                : -1;
        }
    }
    public bool IsInitialized => _data != null;
    public bool IsUnlocked => TryGetRuntimeData(out GameRuntimeData runtimeData) &&
        runtimeData.Facilities.WorktableUnlocked;
    public int SlotCapacity => TryGetRuntimeData(out GameRuntimeData runtimeData)
        ? runtimeData.Facilities.WorktableSlotCapacity
        : 1;
    public int MaxStackCount => TryGetRuntimeData(out GameRuntimeData runtimeData)
        ? runtimeData.Inventory.CreatureMaxStackCount
        : 1;
    public float TransferInterval => _baseTransferInterval / Mathf.Max(
        0.01f,
        TryGetRuntimeData(out GameRuntimeData runtimeData)
            ? runtimeData.Facilities.WorktableTransferSpeedMultiplier
            : 1f);
    public float NormalizedProgress
    {
        get
        {
            int slotIndex = FindLeftmostOccupiedSlot();
            if (slotIndex < 0 || _recipeTable == null ||
                !_recipeTable.TryGetRecipe(
                    _slots[slotIndex].DefinitionId,
                    out WorktableRecipe recipe))
            {
                return 0f;
            }

            float duration = recipe.Creature.WorktableProcessSeconds;
            float remaining = string.Equals(
                    _data?.ProcessingCreatureId,
                    _slots[slotIndex].DefinitionId,
                    StringComparison.Ordinal)
                ? _data.RemainingBaseProcessSeconds
                : duration;
            return duration <= 0f
                ? 0f
                : 1f - Mathf.Clamp01(
                    remaining / duration);
        }
    }

    public bool CanTransferOneFromPlayer
    {
        get
        {
            PlayerInventoryController player = PlayerInventoryController.Instance;
            return IsInitialized &&
                TryGetRuntimeData(out GameRuntimeData runtimeData) &&
                runtimeData.Facilities.WorktableUnlocked &&
                runtimeData.Facilities.ResourceChestUnlocked &&
                player != null &&
                player.TryPeekLeftmostCreatureForWorktable(out string creatureId) &&
                HasRecipe(creatureId) &&
                FindAddSlot(creatureId) >= 0;
        }
    }

    public bool TryResolveCreatureDefinition(
        CreatureInventorySlot slot,
        out CreatureDefinition definition)
    {
        definition = null;
        return slot != null && !slot.IsEmpty &&
            _gameDataManager != null && _gameDataManager.IsInitialized &&
            _gameDataManager.Definitions.TryGetCreature(
                slot.DefinitionId,
                out definition);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        string recipeError = "Recipe table is not assigned.";
        if (_recipeTable == null || !_recipeTable.TryValidate(out recipeError))
        {
            Debug.LogError(
                $"Worktable recipe table is missing or invalid. {recipeError}",
                this);
            enabled = false;
            return;
        }

        _gameDataManager = GameDataManager.Instance;
        if (_gameDataManager == null)
        {
            Debug.LogError("Cannot initialize worktable without GameDataManager.", this);
            enabled = false;
            return;
        }

        _gameDataManager.DataChanged += HandleDataChanged;
        _gameDataManager.RuntimeDataChanged += HandleRuntimeDataChanged;
        _gameDataManager.DataSaved += HandleDataSaved;
        _gameDataManager.SaveSuspensionEnded += HandleSaveSuspensionEnded;

        if (_gameDataManager.IsInitialized)
        {
            InitializeFromGameData(_gameDataManager.SaveData);
        }
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        if (_gameDataManager != null)
        {
            _gameDataManager.DataChanged -= HandleDataChanged;
            _gameDataManager.RuntimeDataChanged -= HandleRuntimeDataChanged;
            _gameDataManager.DataSaved -= HandleDataSaved;
            _gameDataManager.SaveSuspensionEnded -= HandleSaveSuspensionEnded;
        }

        Instance = null;
    }

    private void Update()
    {
        if (!IsInitialized || !IsUnlocked || Time.deltaTime <= 0f)
        {
            return;
        }

        Process(Time.deltaTime);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            SaveCheckpoint();
        }
    }

    private void OnApplicationQuit()
    {
        SaveCheckpoint();
    }

    public bool TryTransferOneFromPlayer()
    {
        if (!CanTransferOneFromPlayer)
        {
            return false;
        }

        PlayerInventoryController player = PlayerInventoryController.Instance;
        if (!player.TryPeekLeftmostCreatureForWorktable(out string creatureId))
        {
            return false;
        }

        int targetIndex = FindAddSlot(creatureId);
        if (targetIndex < 0)
        {
            return false;
        }

        InventoryData playerSnapshot =
            player.CreateWorktableTransferSnapshot();
        WorktableSaveData worktableSnapshot = _data.Clone();
        bool wasDirty = _gameDataManager.HasUnsavedChanges;

        if (!player.TryRemoveLeftmostCreatureForWorktable(creatureId))
        {
            return false;
        }

        CreatureInventorySlot target = _slots[targetIndex];
        int nextCount = target.IsEmpty ? 1 : target.Count + 1;
        target.Set(creatureId, nextCount);
        SyncCreatureSaveData();
        _gameDataManager.MarkDirty();

        if (!_gameDataManager.SaveNow())
        {
            player.RestoreWorktableTransferSnapshot(playerSnapshot);
            RestoreSnapshot(worktableSnapshot);
            _gameDataManager.RestoreDirtyState(wasDirty);
            return false;
        }

        player.NotifyWorktableTransferCommitted();
        Changed?.Invoke();
        return true;
    }

    private void Process(float deltaTime)
    {
        int firstSlotIndex = FindLeftmostOccupiedSlot();
        if (firstSlotIndex < 0)
        {
            if (!string.IsNullOrEmpty(_data.ProcessingCreatureId))
            {
                _data.ClearProcessing();
                RegisterPendingChange();
            }

            return;
        }

        string creatureId = _slots[firstSlotIndex].DefinitionId;
        if (!TryGetRecipe(creatureId, out WorktableRecipe firstRecipe))
        {
            return;
        }

        WorktableSaveData worktableSnapshot = _data.Clone();
        InventoryData chestSnapshot =
            _gameDataManager.SaveData.resourceChest.Clone();
        float remaining = string.Equals(
                _data.ProcessingCreatureId,
                creatureId,
                StringComparison.Ordinal) &&
            _data.RemainingBaseProcessSeconds > 0f
                ? _data.RemainingBaseProcessSeconds
                : firstRecipe.Creature.WorktableProcessSeconds;

        float speed = Mathf.Max(
            0.01f,
            _gameDataManager.RuntimeData.Facilities
                .WorktableProcessSpeedMultiplier);
        remaining -= deltaTime * speed;
        _completedBuffer.Clear();

        int completionCount = 0;
        while (remaining <= 0f && completionCount < MaxCompletionsPerFrame)
        {
            int occupiedIndex = FindLeftmostOccupiedSlot();
            if (occupiedIndex < 0)
            {
                break;
            }

            CreatureInventorySlot slot = _slots[occupiedIndex];
            if (!TryGetRecipe(slot.DefinitionId, out WorktableRecipe recipe))
            {
                break;
            }

            int amount = CalculateYield(recipe.BaseAmount);
            _gameDataManager.SaveData.resourceChest.AddResource(
                recipe.Resource.Id,
                amount);
            RemoveOneAndCompact(occupiedIndex);
            _completedBuffer.Add(new CompletedRecipe(recipe, amount));
            completionCount++;

            int nextIndex = FindLeftmostOccupiedSlot();
            if (nextIndex < 0)
            {
                break;
            }

            if (!TryGetRecipe(
                    _slots[nextIndex].DefinitionId,
                    out WorktableRecipe nextRecipe))
            {
                break;
            }

            remaining += nextRecipe.Creature.WorktableProcessSeconds;
        }

        int currentIndex = FindLeftmostOccupiedSlot();
        if (currentIndex < 0)
        {
            _data.ClearProcessing();
        }
        else if (TryGetRecipe(
                     _slots[currentIndex].DefinitionId,
                     out WorktableRecipe currentRecipe))
        {
            if (remaining <= 0f)
            {
                remaining = currentRecipe.Creature.WorktableProcessSeconds;
            }

            _data.SetProcessing(
                _slots[currentIndex].DefinitionId,
                remaining);
        }

        SyncCreatureSaveData();
        RegisterPendingChange();

        if (_completedBuffer.Count > 0 && !TryPersistPendingChanges())
        {
            RestoreSnapshot(worktableSnapshot);
            _gameDataManager.SaveData.resourceChest.CopyFrom(chestSnapshot);
            return;
        }

        if (_completedBuffer.Count > 0)
        {
            PlayerInventoryController.Instance
                ?.NotifyWorktableProcessingCommitted();
        }

        for (int i = 0; i < _completedBuffer.Count; i++)
        {
            CompletedRecipe result = _completedBuffer[i];
            Completed?.Invoke(
                result.Recipe.Creature,
                result.Recipe.Resource,
                result.Amount);
        }

        _checkpointTimer += deltaTime;
        if (_checkpointTimer >= _saveCheckpointInterval)
        {
            SaveCheckpoint();
        }

        Changed?.Invoke();
    }

    private int CalculateYield(int baseAmount)
    {
        double scaled = baseAmount * (double)Mathf.Max(
            0f,
            _gameDataManager.RuntimeData.Facilities.WorktableYieldMultiplier);
        if (scaled >= int.MaxValue)
        {
            return int.MaxValue;
        }

        return Mathf.Max(1, Mathf.FloorToInt((float)scaled));
    }

    private bool TryPersistPendingChanges()
    {
        _gameDataManager.MarkDirty();
        _hasPendingChanges = true;
        if (_gameDataManager.IsSaveSuspended)
        {
            return true;
        }

        return _gameDataManager.SaveNow();
    }

    private void RegisterPendingChange()
    {
        _hasPendingChanges = true;
        _gameDataManager.MarkDirty();
    }

    private void SaveCheckpoint()
    {
        _checkpointTimer = 0f;
        if (!_hasPendingChanges || _gameDataManager == null ||
            _gameDataManager.IsSaveSuspended)
        {
            return;
        }

        _gameDataManager.MarkDirty();
        _gameDataManager.SaveNow();
    }

    private void HandleDataChanged(GameSaveData data)
    {
        InitializeFromGameData(data);
    }

    private void HandleRuntimeDataChanged(GameRuntimeData data)
    {
        if (!IsInitialized)
        {
            return;
        }

        ResizeSlots(data?.Facilities?.WorktableSlotCapacity ?? 1);
        Changed?.Invoke();
    }

    private void HandleDataSaved(GameSaveData data)
    {
        _hasPendingChanges = false;
        _checkpointTimer = 0f;
    }

    private void HandleSaveSuspensionEnded()
    {
        SaveCheckpoint();
    }

    private void InitializeFromGameData(GameSaveData data)
    {
        _data = data?.worktable;
        if (_data == null)
        {
            _slots = Array.Empty<CreatureInventorySlot>();
            Changed?.Invoke();
            return;
        }

        InitializeSlots(SlotCapacity, _data.Inventory.Creatures);
        ReconcileProcessingState();
        _hasPendingChanges = false;
        _checkpointTimer = 0f;
        Changed?.Invoke();
    }

    private void InitializeSlots(
        int capacity,
        IReadOnlyList<CreatureInventoryEntry> entries)
    {
        _slots = CreateEmptySlots(capacity);
        if (entries == null)
        {
            return;
        }

        int slotIndex = 0;
        for (int entryIndex = 0;
             entryIndex < entries.Count && slotIndex < _slots.Length;
             entryIndex++)
        {
            CreatureInventoryEntry entry = entries[entryIndex];
            if (entry == null || entry.IsEmpty || !HasRecipe(entry.DefinitionId))
            {
                continue;
            }

            int remaining = entry.Count;
            while (remaining > 0 && slotIndex < _slots.Length)
            {
                int count = Mathf.Min(remaining, MaxStackCount);
                _slots[slotIndex].Set(entry.DefinitionId, count);
                remaining -= count;
                slotIndex++;
            }

            if (remaining > 0)
            {
                Debug.LogWarning(
                    $"Worktable inventory exceeded its {capacity} slot capacity. " +
                    $"{remaining} of '{entry.DefinitionId}' could not be loaded.",
                    this);
            }
        }

        SyncCreatureSaveData();
    }

    private void ResizeSlots(int capacity)
    {
        int normalizedCapacity = Mathf.Max(1, capacity);
        if (_slots.Length == normalizedCapacity)
        {
            return;
        }

        if (normalizedCapacity < _slots.Length)
        {
            for (int i = normalizedCapacity; i < _slots.Length; i++)
            {
                if (!_slots[i].IsEmpty)
                {
                    Debug.LogError(
                        "Worktable slot capacity cannot shrink while removed slots contain creatures.",
                        this);
                    return;
                }
            }
        }

        Array.Resize(ref _slots, normalizedCapacity);
        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i] ??= new CreatureInventorySlot();
        }

        SyncCreatureSaveData();
    }

    private void ReconcileProcessingState()
    {
        int firstIndex = FindLeftmostOccupiedSlot();
        if (firstIndex < 0 || !string.Equals(
                _slots[firstIndex].DefinitionId,
                _data.ProcessingCreatureId,
                StringComparison.Ordinal))
        {
            _data.ClearProcessing();
        }
    }

    private bool HasRecipe(string creatureId)
    {
        return _recipeTable != null &&
            _recipeTable.TryGetRecipe(creatureId, out _);
    }

    private bool TryGetRuntimeData(out GameRuntimeData runtimeData)
    {
        runtimeData = null;
        if (_gameDataManager == null || !_gameDataManager.IsInitialized)
        {
            return false;
        }

        runtimeData = _gameDataManager.RuntimeData;
        return runtimeData != null;
    }

    private bool TryGetRecipe(string creatureId, out WorktableRecipe recipe)
    {
        if (_recipeTable != null &&
            _recipeTable.TryGetRecipe(creatureId, out recipe))
        {
            return true;
        }

        Debug.LogError(
            $"No worktable recipe exists for creature '{creatureId}'.",
            this);
        recipe = null;
        return false;
    }

    private int FindAddSlot(string creatureId)
    {
        int capacity = Mathf.Min(SlotCapacity, _slots.Length);
        for (int i = 0; i < capacity; i++)
        {
            CreatureInventorySlot slot = _slots[i];
            if (slot.Matches(creatureId) && slot.Count < MaxStackCount)
            {
                return i;
            }
        }

        for (int i = 0; i < capacity; i++)
        {
            if (_slots[i].IsEmpty)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindLeftmostOccupiedSlot()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null && !_slots[i].IsEmpty)
            {
                return i;
            }
        }

        return -1;
    }

    private void RemoveOneAndCompact(int slotIndex)
    {
        CreatureInventorySlot slot = _slots[slotIndex];
        slot.Set(slot.DefinitionId, slot.Count - 1);
        if (!slot.IsEmpty)
        {
            return;
        }

        int writeIndex = slotIndex;
        for (int readIndex = slotIndex + 1;
             readIndex < _slots.Length;
             readIndex++)
        {
            CreatureInventorySlot source = _slots[readIndex];
            if (source.IsEmpty)
            {
                continue;
            }

            _slots[writeIndex].Set(source.DefinitionId, source.Count);
            source.Clear();
            writeIndex++;
        }

        for (int i = writeIndex; i < _slots.Length; i++)
        {
            _slots[i].Clear();
        }
    }

    private void RestoreSnapshot(WorktableSaveData snapshot)
    {
        _data.CopyFrom(snapshot);
        InitializeSlots(SlotCapacity, _data.Inventory.Creatures);
        ReconcileProcessingState();
        Changed?.Invoke();
    }

    private void SyncCreatureSaveData()
    {
        _data?.Inventory.SetCreatures(_slots);
    }

    private static CreatureInventorySlot[] CreateEmptySlots(int capacity)
    {
        CreatureInventorySlot[] slots =
            new CreatureInventorySlot[Mathf.Max(1, capacity)];
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = new CreatureInventorySlot();
        }

        return slots;
    }
}
