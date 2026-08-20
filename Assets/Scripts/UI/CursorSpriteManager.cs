using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies the exploration cursor only while a scene contains a SessionManager.
/// All other scenes use the hub cursor.
/// </summary>
public static class CursorSpriteManager
{
    private const string HubCursorPath = "Cursors/pointer_hub";
    private const string SessionCursorPath = "Cursors/pointer_session";

    // These align the mouse position with the arrow tip and crosshair center.
    private static readonly Vector2 HubHotspot = new(2f, 3f);
    private static readonly Vector2 SessionHotspot = new(16f, 16f);

    private static Texture2D _hubCursor;
    private static Texture2D _sessionCursor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        _hubCursor = Resources.Load<Texture2D>(HubCursorPath);
        _sessionCursor = Resources.Load<Texture2D>(SessionCursorPath);

        if (_hubCursor == null || _sessionCursor == null)
        {
            Debug.LogError("Cursor sprites could not be loaded from Resources/Cursors.");
            return;
        }

        SceneManager.sceneLoaded += ApplyCursorForScene;
        ApplyCursor(isExploreSession: false);
    }

    private static void ApplyCursorForScene(Scene scene, LoadSceneMode mode)
    {
        ApplyCursor(Object.FindFirstObjectByType<SessionManager>() != null);
    }

    private static void ApplyCursor(bool isExploreSession)
    {
        Cursor.SetCursor(
            isExploreSession ? _sessionCursor : _hubCursor,
            isExploreSession ? SessionHotspot : HubHotspot,
            CursorMode.Auto);
    }
}
