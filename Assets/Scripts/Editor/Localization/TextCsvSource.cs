using UnityEditor.Localization;
using UnityEngine;

namespace Astrodiver.Editor.Localization
{
    [CreateAssetMenu(menuName = "Astrodiver/Localization/CSV Source")]
    public sealed class TextCsvSource : ScriptableObject
    {
        [SerializeField] private TextAsset _csv;
        [SerializeField] private StringTableCollection _collection;
        [SerializeField] private string _baseLocale = "ko";

        public TextAsset Csv => _csv;
        public StringTableCollection Collection => _collection;
        public string BaseLocale => _baseLocale;
    }
}
