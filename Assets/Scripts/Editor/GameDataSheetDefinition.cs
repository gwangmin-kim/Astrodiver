using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(
    fileName = "GameDataSheet",
    menuName = "Astrodiver/Data/Game Data Sheet")]
public sealed class GameDataSheetDefinition : ScriptableObject
{
    private static readonly Regex _keyPattern = new(
        "^[a-z][a-z0-9_]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Header("Identity")]
    [SerializeField] private string _categoryKey;
    [SerializeField] private MonoScript _definitionScript;

    [Header("Asset Locations")]
    [SerializeField] private DefaultAsset _assetFolder;
    [SerializeField] private DefaultAsset _iconSearchFolder;
    [SerializeField] private string _iconNamePattern = "{key}";

    [Header("Filename")]
    [SerializeField, Range(1, 8)] private int _orderDigits = 3;

    public string CategoryKey => _categoryKey;
    public MonoScript DefinitionScript => _definitionScript;
    public DefaultAsset AssetFolder => _assetFolder;
    public DefaultAsset IconSearchFolder => _iconSearchFolder;
    public string IconNamePattern => _iconNamePattern;
    public int OrderDigits => Mathf.Clamp(_orderDigits, 1, 8);

    public string AssetFolderPath => _assetFolder != null
        ? AssetDatabase.GetAssetPath(_assetFolder)
        : string.Empty;

    public string IconSearchFolderPath => _iconSearchFolder != null
        ? AssetDatabase.GetAssetPath(_iconSearchFolder)
        : string.Empty;

    public Type DefinitionType => _definitionScript != null
        ? _definitionScript.GetClass()
        : null;

    public static bool IsValidKey(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && _keyPattern.IsMatch(value);
    }

    public bool TryValidate(out string error)
    {
        if (!IsValidKey(_categoryKey))
        {
            error = "Category key must contain only lowercase letters, numbers, and underscores, and must start with a letter.";
            return false;
        }

        Type definitionType = DefinitionType;
        if (definitionType == null || definitionType.IsAbstract ||
            !typeof(GameDefinition).IsAssignableFrom(definitionType))
        {
            error = "Definition script must declare a concrete GameDefinition type.";
            return false;
        }

        if (!IsFolder(_assetFolder))
        {
            error = "Asset folder must reference a folder inside the project.";
            return false;
        }

        if (_iconSearchFolder != null && !IsFolder(_iconSearchFolder))
        {
            error = "Icon search folder must reference a folder inside the project.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_iconNamePattern) ||
            !_iconNamePattern.Contains("{key}", StringComparison.Ordinal))
        {
            error = "Icon name pattern must contain the {key} token.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool IsFolder(DefaultAsset asset)
    {
        return asset != null &&
            AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(asset));
    }

    private void OnValidate()
    {
        _orderDigits = Mathf.Clamp(_orderDigits, 1, 8);
    }
}
