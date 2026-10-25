namespace Astrodiver.Editor.Localization
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEditor.Localization;
    using UnityEditor.Localization.Plugins.CSV;
    using UnityEditor.Localization.Plugins.CSV.Columns;
    using UnityEngine;
    using UnityEngine.Localization.Tables;
    using UnityEngine.Localization.Metadata;

    public static class TextCsvSync
    {
        public sealed class Plan
        {
            public TextCsvSource Source;
            public TextCsvDocument Document;
            public List<long> RemovedIds;
            public int Added;
            public int Renamed;
        }

        private static TextCsvSource[] Sources() => AssetDatabase.FindAssets("t:TextCsvSource", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.Ordinal)
            .Select(AssetDatabase.LoadAssetAtPath<TextCsvSource>).ToArray();

        public static List<Plan> ValidateAll(bool requireSynchronized = false)
            => Validate(Sources(), requireSynchronized);

        public static List<Plan> Validate(IEnumerable<TextCsvSource> selectedSources, bool requireSynchronized = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before CSV synchronization.");
            if (selectedSources == null) throw new ArgumentNullException(nameof(selectedSources));
            var sources = selectedSources.ToArray();
            if (sources.Length == 0) throw new InvalidOperationException("No TextCsvSource assets are configured.");
            if (sources.Any(s => s.Csv == null || s.Collection == null)) throw new InvalidOperationException("Every CSV Source requires a CSV and collection.");
            if (sources.GroupBy(s => s.Collection).Any(g => g.Count() > 1) || sources.GroupBy(s => s.Csv).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Each CSV and collection must have exactly one source mapping.");
            var plans = sources.Select(BuildPlan).ToList();
            ValidateReferences(plans, TextReferenceScanner.Scan());
            if (requireSynchronized)
            {
                foreach (Plan plan in plans)
                {
                    if (plan.RemovedIds.Count != 0 || plan.Added != 0 || plan.Renamed != 0)
                        throw new InvalidOperationException($"{plan.Source.name}: CSV is not synchronized. Run Synchronize CSV.");
                    foreach (var row in plan.Document.Rows)
                    {
                        string comment = plan.Source.Collection.SharedData.GetEntry(row.Id).Metadata.GetMetadata<Comment>()?.CommentText ?? string.Empty;
                        if (comment != row.Cells["Comment"])
                            throw new InvalidOperationException($"{plan.Source.name}: unsynchronized comment at {row.Key}.");
                        foreach (StringTable table in plan.Source.Collection.StringTables)
                        {
                            var entry = table.GetEntry(row.Id);
                            if (entry == null || entry.Value != row.Cells[table.LocaleIdentifier.Code] || entry.IsSmart != bool.Parse(row.Cells["Smart"]))
                                throw new InvalidOperationException($"{plan.Source.name}: unsynchronized value at {row.Key}/{table.LocaleIdentifier.Code}.");
                        }
                    }
                }
            }
            return plans;
        }

        public static Plan BuildPlan(TextCsvSource source)
        {
            string path = AssetDatabase.GetAssetPath(source.Csv);
            var document = TextCsvDocument.Parse(File.ReadAllText(path, new UTF8Encoding(false, true)));
            var shared = source.Collection.SharedData;
            var tables = source.Collection.StringTables;
            string[] locales = tables.Select(t => t.LocaleIdentifier.Code).ToArray();
            if (!locales.Contains(source.BaseLocale)) throw new FormatException($"{path}: missing base locale table {source.BaseLocale}.");
            string[] expected = new[] { "Key", "Id", "Comment", "Smart" }.Concat(locales).ToArray();
            if (document.Headers.Except(expected).Any() || expected.Except(document.Headers).Any())
                throw new FormatException($"{path}: headers must be {string.Join(", ", expected)}. Configure a locale table before adding a language column.");

            var plan = new Plan { Source = source, Document = document };
            var used = new HashSet<long>();
            foreach (var row in document.Rows)
            {
                foreach (string locale in locales)
                    if (string.IsNullOrWhiteSpace(row.Cells[locale]))
                        throw new FormatException($"{path}, row {row.Number}: empty translation for {row.Key}/{locale}.");
                var byKey = shared.GetEntry(row.Key);
                if (row.Id == 0 && byKey != null) row.Id = byKey.Id;
                var byId = row.Id == 0 ? null : shared.GetEntry(row.Id);
                if (byKey != null && byKey.Id != row.Id)
                    throw new FormatException($"{path}, row {row.Number}: Key '{row.Key}' belongs to another Id. Preserve existing IDs.");
                if (row.Id != 0 && !used.Add(row.Id)) throw new FormatException($"{path}: two rows resolve to Id {row.Id}.");
                if (byId != null && byId.Key != row.Key) plan.Renamed++;
                if (byId == null) plan.Added++;
            }
            foreach (var row in document.Rows.Where(r => r.Id == 0))
            {
                long id;
                do { id = shared.KeyGenerator.GetNextKey(); } while (id <= 0 || shared.Contains(id) || !used.Add(id));
                row.Id = id;
            }
            plan.RemovedIds = shared.Entries.Where(e => !used.Contains(e.Id)).Select(e => e.Id).ToList();
            return plan;
        }

        public static void ValidateReferences(IReadOnlyList<Plan> plans, IEnumerable<TextReferenceScanner.Usage> usages)
        {
            var failures = new List<string>();
            foreach (var usage in usages)
            {
                var reference = usage.Reference;
                var plan = plans.FirstOrDefault(p => reference.TableReference.ReferenceType == TableReference.Type.Guid
                    ? reference.TableReference.TableCollectionNameGuid == p.Source.Collection.SharedData.TableCollectionNameGuid
                    : reference.TableReference.TableCollectionName == p.Source.Collection.TableCollectionName);
                if (plan == null) continue;
                bool exists = reference.TableEntryReference.ReferenceType == TableEntryReference.Type.Id
                    ? plan.Document.Rows.Any(r => r.Id == reference.TableEntryReference.KeyId)
                    : plan.Document.Rows.Any(r => r.Key == reference.TableEntryReference.Key);
                if (!exists) failures.Add($"{reference.TableReference}/{reference.TableEntryReference} <- {usage.Location}");
            }
            if (failures.Count > 0) throw new InvalidOperationException("CSV would invalidate referenced text. Nothing was imported:\n" + string.Join("\n", failures));
        }

        public static string SynchronizeAll()
            => Synchronize(Sources());

        public static string Synchronize(IEnumerable<TextCsvSource> selectedSources)
        {
            var plans = Validate(selectedSources); // No mutation before EVERY selected CSV and reference has passed.
            var objects = plans.SelectMany(p => p.Source.Collection.StringTables.Cast<UnityEngine.Object>()
                .Concat(new UnityEngine.Object[] { p.Source.Collection, p.Source.Collection.SharedData })).Distinct().ToArray();
            var snapshots = objects.ToDictionary(o => o, o => EditorJsonUtility.ToJson(o));
            string[] paths = objects.Select(AssetDatabase.GetAssetPath).Concat(plans.Select(p => AssetDatabase.GetAssetPath(p.Source.Csv))).Distinct().ToArray();
            var files = paths.ToDictionary(p => p, File.ReadAllBytes);
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Synchronize text CSV");
            Undo.RegisterCompleteObjectUndo(objects, "Synchronize text CSV");
            try
            {
                foreach (Plan plan in plans)
                {
                    var mappings = new List<CsvColumns>
                    {
                        new KeyIdColumns { IncludeId = true, IncludeSharedComments = true, SharedCommentFieldName = "Comment" }
                    };
                    mappings.AddRange(plan.Source.Collection.StringTables.Select(t => new LocaleColumns
                    { LocaleIdentifier = t.LocaleIdentifier, FieldName = t.LocaleIdentifier.Code }));
                    using var reader = new StringReader(plan.Document.Write());
                    Csv.ImportInto(reader, plan.Source.Collection, mappings, false, null, true);
                    foreach (var row in plan.Document.Rows)
                        foreach (StringTable table in plan.Source.Collection.StringTables)
                        {
                            table.GetEntry(row.Id).IsSmart = bool.Parse(row.Cells["Smart"]);
                            EditorUtility.SetDirty(table);
                        }
                }
                foreach (var obj in objects) { EditorUtility.SetDirty(obj); AssetDatabase.SaveAssetIfDirty(obj); }
                foreach (Plan plan in plans)
                {
                    File.WriteAllText(AssetDatabase.GetAssetPath(plan.Source.Csv), plan.Document.Write(), new UTF8Encoding(false));
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(plan.Source.Csv));
                    LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, plan.Source.Collection);
                }
                Undo.CollapseUndoOperations(group);
                return string.Join("\n", plans.Select(p => $"{p.Source.Collection.TableCollectionName}: {p.Document.Rows.Count} rows; added {p.Added}, renamed {p.Renamed}, deleted {p.RemovedIds.Count}."));
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                foreach (var pair in snapshots) EditorJsonUtility.FromJsonOverwrite(pair.Value, pair.Key);
                foreach (var pair in files) File.WriteAllBytes(pair.Key, pair.Value);
                foreach (string path in paths) AssetDatabase.ImportAsset(path);
                throw;
            }
        }

        [MenuItem("Astrodiver/Localization/Validate CSV")]
        private static void ValidateMenu()
        {
            try { var plans = ValidateAll(); Debug.Log($"Text CSV validation passed: {plans.Count} collections, {plans.Sum(p => p.Document.Rows.Count)} rows."); }
            catch (Exception exception) { Debug.LogError(exception.Message); }
        }

        [MenuItem("Astrodiver/Localization/Synchronize CSV")]
        private static void SyncMenu()
        {
            try { Debug.Log(SynchronizeAll()); }
            catch (Exception exception) { Debug.LogError(exception.Message); }
        }
    }

    public sealed class TextCsvBuildValidation : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;
        public void OnPreprocessBuild(BuildReport report)
        {
            try { TextCsvSync.ValidateAll(true); }
            catch (Exception exception) { throw new BuildFailedException(exception.Message); }
        }
    }
}
