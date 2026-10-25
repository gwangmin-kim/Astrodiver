namespace Astrodiver.Localization
{
    using System;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Localization;
    using UnityEngine.Localization.Settings;
    using UnityEngine.Localization.Tables;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.AddressableAssets;

    /// <summary>Uses Localization's locale state; callers keep serialized LocalizedString references.</summary>
    public static class GameText
    {
        public static IEnumerator Prepare(bool allLocales,
            System.Collections.Generic.ICollection<AsyncOperationHandle> retainedTables, params TableReference[] tables)
        {
            yield return LocalizationSettings.InitializationOperation;
            if (LocalizationSettings.InitializationOperation.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("GameText: Localization initialization failed.");
                yield break;
            }

            foreach (TableReference table in tables)
            {
                // Prepare every configured locale when the caller needs to keep all variants ready.
                foreach (Locale locale in allLocales ? LocalizationSettings.AvailableLocales.Locales :
                    new System.Collections.Generic.List<Locale> { LocalizationSettings.SelectedLocale })
                {
                    var operation = LocalizationSettings.StringDatabase.GetTableAsync(table, locale);
                    yield return operation;
                    if (operation.Status != AsyncOperationStatus.Succeeded || operation.Result == null)
                        Debug.LogError($"GameText: Could not prepare table {table}/{locale.Identifier.Code}.");
                    else if (retainedTables != null && !retainedTables.Contains(operation))
                    {
                        // Localization releases its own table handles on locale changes.
                        // An explicit owner can retain those tables until its own lifetime ends.
                        Addressables.ResourceManager.Acquire(operation);
                        retainedTables.Add(operation);
                    }
                }
            }
        }

        /// <summary>No blocking loads: an unprepared table returns false.</summary>
        public static bool TryResolve(LocalizedString reference, out string value)
        {
            value = string.Empty;
            if (reference == null || reference.IsEmpty)
            {
                return false;
            }

            var table = LocalizationSettings.StringDatabase.GetTableAsync(reference.TableReference, reference.LocaleOverride);
            if (!table.IsDone || table.Status != AsyncOperationStatus.Succeeded || table.Result == null) return false;
            var entry = reference.TableEntryReference.ReferenceType == TableEntryReference.Type.Id
                ? table.Result.GetEntry(reference.TableEntryReference.KeyId)
                : table.Result.GetEntry(reference.TableEntryReference.Key);
            if (entry == null || string.IsNullOrWhiteSpace(entry.Value)) return false;

            var operation = reference.GetLocalizedStringAsync();
            if (!operation.IsDone || operation.Status != AsyncOperationStatus.Succeeded)
            {
                return false;
            }

            value = operation.Result ?? string.Empty;
            return true;
        }

        public static string ResolvePrepared(LocalizedString reference)
        {
            if (TryResolve(reference, out string value))
            {
                return value;
            }

            Debug.LogError($"GameText: Missing or unprepared text: {reference?.TableReference}/{reference?.TableEntryReference}");
            return Application.isEditor || Debug.isDebugBuild
                ? $"[Missing text: {reference?.TableEntryReference}]"
                : string.Empty;
        }
    }
}
