using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public sealed class SpaceBackgroundController : MonoBehaviour
{
    [SerializeField] private SpaceBackgroundProfile _defaultProfile;

    private Renderer _renderer;
    private MaterialPropertyBlock _properties;

    public SpaceBackgroundProfile ActiveProfile { get; private set; }
    public SpaceBackgroundSeeds ActiveSeeds { get; private set; }

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _properties = new MaterialPropertyBlock();
    }

    private void Start()
    {
        ApplyForCurrentScene();
    }

    private void OnDisable()
    {
        if (_renderer != null)
        {
            _renderer.SetPropertyBlock(null);
        }
    }

    public void ApplyForCurrentScene()
    {
        SpaceBackgroundProfile profile = ResolveSceneProfile();
        if (profile == null)
        {
            Debug.LogWarning(
                "SpaceBackgroundController: No default or scene profile is assigned.",
                this);
            _renderer.SetPropertyBlock(null);
            ActiveProfile = null;
            return;
        }

        ActiveProfile = profile;
        ActiveSeeds = profile.CreateSeeds();
        _properties.Clear();
        profile.ApplyTo(_properties, ActiveSeeds);
        _renderer.SetPropertyBlock(_properties);
    }

    private SpaceBackgroundProfile ResolveSceneProfile()
    {
        SpaceBackgroundSceneSettings[] settings =
            FindObjectsByType<SpaceBackgroundSceneSettings>(
                FindObjectsInactive.Include);

        for (int i = 0; i < settings.Length; i++)
        {
            SpaceBackgroundSceneSettings candidate = settings[i];
            if (candidate.gameObject.scene == gameObject.scene &&
                candidate.Profile != null)
            {
                return candidate.Profile;
            }
        }

        StagePopulationManager[] stageManagers =
            FindObjectsByType<StagePopulationManager>(
                FindObjectsInactive.Include);

        for (int i = 0; i < stageManagers.Length; i++)
        {
            StagePopulationManager stageManager = stageManagers[i];
            StageDefinition definition = stageManager.Definition;
            if (stageManager.gameObject.scene == gameObject.scene &&
                definition != null &&
                definition.SpaceBackgroundProfile != null)
            {
                return definition.SpaceBackgroundProfile;
            }
        }

        return _defaultProfile;
    }
}
