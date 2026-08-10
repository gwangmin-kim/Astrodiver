using UnityEngine;
using UnityEngine.Serialization;

public abstract class GameDefinition : ScriptableObject
{
    [SerializeField, HideInInspector]
    [FormerlySerializedAs("_stageId")]
    private string _id;

    [SerializeField, HideInInspector]
    private string _key;

    [SerializeField, Min(0)]
    private int _sortOrder;

    public string Id => _id;
    public string Key => _key;
    public int SortOrder => Mathf.Max(0, _sortOrder);

#if UNITY_EDITOR
    protected void ConfigureIdentityForEditor(
        string id,
        string key = null,
        int sortOrder = 0)
    {
        _id = id;
        if (key != null)
        {
            _key = key;
        }

        _sortOrder = Mathf.Max(0, sortOrder);
    }
#endif
}
