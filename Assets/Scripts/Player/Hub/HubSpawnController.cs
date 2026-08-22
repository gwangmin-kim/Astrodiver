using UnityEngine;

/// <summary>
/// Places the scene-owned Hub player at the point selected by the preceding
/// scene transition. Unspecified Hub entry always falls back to ReturnPoint.
/// </summary>
[DefaultExecutionOrder(-900)]
public sealed class HubSpawnController : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _startPoint;
    [SerializeField] private Transform _returnPoint;

    private void Awake()
    {
        if (_player == null || _startPoint == null || _returnPoint == null)
        {
            Debug.LogError(
                "HubSpawnController: Player, StartPoint, and ReturnPoint must be assigned.",
                this);
            return;
        }

        HubSpawnPoint spawnPoint = SceneTransitionManager.Instance != null
            ? SceneTransitionManager.Instance.ConsumeHubSpawnPoint()
            : HubSpawnPoint.Return;
        Transform destination = spawnPoint == HubSpawnPoint.Start
            ? _startPoint
            : _returnPoint;

        _player.SetPositionAndRotation(destination.position, destination.rotation);

        if (_player.TryGetComponent(out Rigidbody2D rigidbody))
        {
            rigidbody.linearVelocity = Vector2.zero;
            rigidbody.angularVelocity = 0f;
        }
    }
}
