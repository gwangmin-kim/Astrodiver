namespace Astrodiver.Localization.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Astrodiver.Editor.Localization;
    using NUnit.Framework;
    using TMPro;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.Localization;
    using UnityEditor.Localization;
    using UnityEditor.AddressableAssets;

    public sealed class TextCsvTests
    {
        private TextCsvSource _first;
        private TextCsvSource _second;
        private TextCsvSource[] _sources;
        private string _fixtureRoot;

        [SetUp]
        public void CreateIsolatedFixtures()
        {
            string suffix = Guid.NewGuid().ToString("N");
            _fixtureRoot = "Assets/__TextCsvTests_" + suffix;
            Assert.That(AssetDatabase.IsValidFolder(_fixtureRoot), Is.False);
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(_fixtureRoot));
            _first = CreateSource("First_" + suffix, "test.used,1,fixture,false,사용 문구\ntest.other,2,fixture,false,다른 문구\n");
            _second = CreateSource("Second_" + suffix, "test.common,1,fixture,false,공통 문구\n");
            _sources = new[] { _first, _second };
            TextCsvSync.Synchronize(_sources);
            var reference = ScriptableObject.CreateInstance<TextReferenceTestAsset>();
            reference.text = new LocalizedString(_first.Collection.SharedData.TableCollectionNameGuid, 1);
            AssetDatabase.CreateAsset(reference, _fixtureRoot + "/UsedTextReference.asset");
            AssetDatabase.SaveAssetIfDirty(reference);
        }

        [TearDown]
        public void RemoveOnlyOwnedFixtures()
        {
            if (string.IsNullOrEmpty(_fixtureRoot) || !AssetDatabase.IsValidFolder(_fixtureRoot)) return;
            string absolute = Path.GetFullPath(_fixtureRoot);
            string assets = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
            if (!absolute.StartsWith(assets, StringComparison.OrdinalIgnoreCase) ||
                !Path.GetFileName(absolute).StartsWith("__TextCsvTests_", StringComparison.Ordinal))
                throw new InvalidOperationException("Refusing to delete an unexpected test fixture directory.");
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            foreach (string guid in AssetDatabase.FindAssets("", new[] { _fixtureRoot })) settings.RemoveAssetEntry(guid);
            AssetDatabase.DeleteAsset(_fixtureRoot);
            foreach (var group in settings.groups) if (group != null) AssetDatabase.SaveAssetIfDirty(group);
            AssetDatabase.SaveAssetIfDirty(settings);
            _fixtureRoot = null;
        }

        private TextCsvSource CreateSource(string name, string rows)
        {
            string directory = _fixtureRoot + "/" + name;
            AssetDatabase.CreateFolder(_fixtureRoot, name);
            var locale = LocalizationEditorSettings.GetLocales().Single(l => l.Identifier.Code == "ko");
            var collection = LocalizationEditorSettings.CreateStringTableCollection(name, directory, new List<Locale> { locale });
            string csvPath = directory + "/Text.csv";
            File.WriteAllText(csvPath, "Key,Id,Comment,Smart,ko\n" + rows, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(csvPath);
            var source = ScriptableObject.CreateInstance<TextCsvSource>();
            AssetDatabase.CreateAsset(source, directory + "/Source.asset");
            using var serialized = new SerializedObject(source);
            serialized.FindProperty("_csv").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(csvPath);
            serialized.FindProperty("_collection").objectReferenceValue = collection;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(source);
            return source;
        }

        [Test]
        public void CsvRoundTripPreservesBomKoreanQuotesCommasAndActualNewlines()
        {
            string expected = "첫 줄, \"인용\"\n둘째 줄";
            string cell = "\"" + expected.Replace("\"", "\"\"") + "\"";
            var csv = TextCsvDocument.Parse("\uFEFFKey,Id,Comment,Smart,ko\r\nkey,12,설명,false," + cell + "\r\n");
            var roundTrip = TextCsvDocument.Parse(csv.Write());
            Assert.That(roundTrip.Rows[0].Cells["ko"], Is.EqualTo("첫 줄, \"인용\"\n둘째 줄"));
            Assert.That(roundTrip.Rows[0].Id, Is.EqualTo(12));
        }

        [TestCase("a,1,c,false,가\na,2,c,false,나", "duplicate Key")]
        [TestCase("a,1,c,false,가\nb,1,c,false,나", "duplicate Id")]
        [TestCase("a,1e3,c,false,가", "integer")]
        [TestCase("a,-1,c,false,가", "integer")]
        [TestCase("a,1,c,no,가", "Smart")]
        [TestCase("a,1,c,false,가,extra", "column count")]
        public void InvalidCsvIsRejected(string rows, string message)
        {
            var exception = Assert.Throws<FormatException>(() => TextCsvDocument.Parse("Key,Id,Comment,Smart,ko\n" + rows));
            Assert.That(exception.Message, Does.Contain(message));
        }

        [Test]
        public void EmptyTranslationAndUnknownLocaleAreRejected()
        {
            var document = Read(_first);
            document.Rows[0].Cells["ko"] = "";
            Write(_first, document);
            Assert.Throws<FormatException>(() => TextCsvSync.BuildPlan(_first));
            File.WriteAllText(AssetDatabase.GetAssetPath(_first.Csv), "Key,Id,Comment,Smart,en\na,1,c,false,Hello\n");
            Assert.Throws<FormatException>(() => TextCsvSync.BuildPlan(_first));
        }

        [Test]
        public void RenameWithSameIdKeepsSerializedReferencesValid()
        {
            var document = Read(_first);
            long id = document.Rows[0].Id;
            document.Rows[0].Key = "test.renamed";
            Write(_first, document);
            TextCsvSync.Synchronize(_sources);
            Assert.That(_first.Collection.SharedData.GetEntry(id).Key, Is.EqualTo("test.renamed"));
            Assert.DoesNotThrow(() => TextCsvSync.Validate(_sources, true));
        }

        [Test]
        public void UnusedRowIsAddedAndThenActuallyDeleted()
        {
            string path = AssetDatabase.GetAssetPath(_first.Csv);
            string original = File.ReadAllText(path);
            File.AppendAllText(path, "test.unused,100,unused,false,미사용\n");
            TextCsvSync.Synchronize(_sources);
            Assert.That(_first.Collection.SharedData.GetEntry(100), Is.Not.Null);
            File.WriteAllText(path, original);
            TextCsvSync.Synchronize(_sources);
            Assert.That(_first.Collection.SharedData.GetEntry(100), Is.Null);
            Assert.That(_first.Collection.StringTables.All(t => t.GetEntry(100) == null), Is.True);
        }

        [Test]
        public void ReferencedDeletionReportsRealAssetLocationAndPreventsAllTableWrites()
        {
            var common = Read(_second);
            common.Rows[0].Cells["ko"] = "동기화되면 안 되는 변경";
            Write(_second, common);
            var first = Read(_first);
            first.Rows.RemoveAll(r => r.Key == "test.used");
            Write(_first, first);
            string commonBefore = EditorJsonUtility.ToJson(_second.Collection.StringTables[0]);
            string firstBefore = EditorJsonUtility.ToJson(_first.Collection.StringTables[0]);
            var files = _sources.SelectMany(s => s.Collection.StringTables.Select(AssetDatabase.GetAssetPath)
                .Append(AssetDatabase.GetAssetPath(s.Collection.SharedData))).ToDictionary(p => p, File.ReadAllBytes);
            var exception = Assert.Throws<InvalidOperationException>(() => TextCsvSync.Synchronize(_sources));
            Assert.That(exception.Message, Does.Contain("UsedTextReference.asset"));
            Assert.That(EditorJsonUtility.ToJson(_second.Collection.StringTables[0]), Is.EqualTo(commonBefore));
            Assert.That(EditorJsonUtility.ToJson(_first.Collection.StringTables[0]), Is.EqualTo(firstBefore));
            foreach (var pair in files)
                Assert.That(File.ReadAllBytes(pair.Key), Is.EqualTo(pair.Value), pair.Key);
        }

        [Test]
        public void NewBlankIdIsWrittenBackAndStableAcrossImports()
        {
            string path = AssetDatabase.GetAssetPath(_first.Csv);
            File.AppendAllText(path, "test.generated,,generated,false,새 항목\n");
            TextCsvSync.Synchronize(_sources);
            long id = Read(_first).Rows.Single(r => r.Key == "test.generated").Id;
            Assert.That(id, Is.GreaterThan(0));
            TextCsvSync.Synchronize(_sources);
            Assert.That(Read(_first).Rows.Single(r => r.Key == "test.generated").Id, Is.EqualTo(id));
        }

        [Test]
        public void BuildValidationRejectsCsvThatWasEditedWithoutImporting()
        {
            var document = Read(_first);
            document.Rows[0].Cells["ko"] += "변경";
            Write(_first, document);
            Assert.Throws<InvalidOperationException>(() => TextCsvSync.Validate(_sources, true));
        }

        [Test]
        public void NativeTmpBindingIsPersistentAndIdempotent()
        {
            var go = new GameObject("Text binding test", typeof(RectTransform), typeof(TextMeshProUGUI));
            try
            {
                var text = go.GetComponent<TextMeshProUGUI>();
                var binding = LocalizedTmpBindingUtility.Bind(text);
                LocalizedTmpBindingUtility.Bind(text);
                Assert.That(binding.OnUpdateString.GetPersistentEventCount(), Is.EqualTo(1));
                Assert.That(binding.OnUpdateString.GetPersistentTarget(0), Is.EqualTo(text));
                Assert.That(binding.OnUpdateString.GetPersistentMethodName(0), Is.EqualTo("set_text"));
                Assert.That(binding.OnUpdateString.GetPersistentListenerState(0), Is.EqualTo(UnityEventCallState.EditorAndRuntime));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static TextCsvDocument Read(TextCsvSource source) => TextCsvDocument.Parse(File.ReadAllText(AssetDatabase.GetAssetPath(source.Csv)));
        private static void Write(TextCsvSource source, TextCsvDocument document) => File.WriteAllText(AssetDatabase.GetAssetPath(source.Csv), document.Write(), new UTF8Encoding(false));
    }
}
