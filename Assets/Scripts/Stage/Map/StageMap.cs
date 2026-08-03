using UnityEngine;
using UnityEngine.Tilemaps;

public enum StageMapLayer
{
    Platform = 0,
    DecorationBack = 1,
    DecorationFront = 2
}

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class StageMap : MonoBehaviour
{
    public static readonly Vector3 cellSize = Vector3.one;

    [SerializeField] private Grid _grid;
    [SerializeField] private Tilemap _platform;
    [SerializeField] private Tilemap _decorationBack;
    [SerializeField] private Tilemap _decorationFront;

    public Grid Grid => _grid;
    public Tilemap Platform => _platform;
    public Tilemap DecorationBack => _decorationBack;
    public Tilemap DecorationFront => _decorationFront;

    public Tilemap GetTilemap(StageMapLayer layer)
    {
        return layer switch
        {
            StageMapLayer.Platform => _platform,
            StageMapLayer.DecorationBack => _decorationBack,
            StageMapLayer.DecorationFront => _decorationFront,
            _ => null
        };
    }

    public void Configure(
        Grid grid,
        Tilemap platform,
        Tilemap decorationBack,
        Tilemap decorationFront)
    {
        _grid = grid;
        _platform = platform;
        _decorationBack = decorationBack;
        _decorationFront = decorationFront;
        EnforceTransformLock();
    }

    public void EnforceTransformLock()
    {
        PinToWorldOrigin(_grid != null ? _grid.transform : null);
        PinToWorldOrigin(_platform != null ? _platform.transform : null);
        PinToWorldOrigin(
            _decorationBack != null ? _decorationBack.transform : null);
        PinToWorldOrigin(
            _decorationFront != null ? _decorationFront.transform : null);
    }

    public bool TryValidate(out string error)
    {
        if (_grid == null || _platform == null || _decorationBack == null ||
            _decorationFront == null)
        {
            error = "Grid and all three logical Tilemaps must be assigned.";
            return false;
        }

        if ((_grid.cellSize - cellSize).sqrMagnitude > 0.000001f)
        {
            error = "Stage map Grid cell size must be (1, 1, 1).";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private void OnEnable()
    {
        EnforceTransformLock();
    }

    private void OnValidate()
    {
        EnforceTransformLock();
    }

    private void LateUpdate()
    {
        EnforceTransformLock();
    }

    private static void PinToWorldOrigin(Transform target)
    {
        if (target == null)
        {
            return;
        }

        if (target.position != Vector3.zero ||
            target.rotation != Quaternion.identity)
        {
            target.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        if (target.localScale != Vector3.one)
        {
            target.localScale = Vector3.one;
        }
    }
}
