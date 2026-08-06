using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public enum StageMapLayer
{
    Platform = 0,
    DecorationBack = 1,
    DecorationFront = 2
}

[Flags]
public enum StageMapLayerMask
{
    None = 0,
    Platform = 1 << (int)StageMapLayer.Platform,
    DecorationBack = 1 << (int)StageMapLayer.DecorationBack,
    DecorationFront = 1 << (int)StageMapLayer.DecorationFront,
    All = Platform | DecorationBack | DecorationFront
}

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class StageMap : MonoBehaviour
{
    public static readonly Vector3 cellSize = Vector3.one;

    [SerializeField] private Grid _grid;
    [SerializeField] private Tilemap _platformLogic;
    [SerializeField] private Tilemap _decorationBackLogic;
    [SerializeField] private Tilemap _decorationFrontLogic;
    [SerializeField] private Tilemap _platformVisual;
    [SerializeField] private Tilemap _decorationBackVisual;
    [SerializeField] private Tilemap _decorationFrontVisual;

    public Grid Grid => _grid;
    public Tilemap PlatformLogic => _platformLogic;
    public Tilemap DecorationBackLogic => _decorationBackLogic;
    public Tilemap DecorationFrontLogic => _decorationFrontLogic;
    public Tilemap PlatformVisual => _platformVisual;
    public Tilemap DecorationBackVisual => _decorationBackVisual;
    public Tilemap DecorationFrontVisual => _decorationFrontVisual;

    public Tilemap GetTilemap(StageMapLayer layer)
    {
        return GetLogicalTilemap(layer);
    }

    public Tilemap GetLogicalTilemap(StageMapLayer layer)
    {
        return layer switch
        {
            StageMapLayer.Platform => _platformLogic,
            StageMapLayer.DecorationBack => _decorationBackLogic,
            StageMapLayer.DecorationFront => _decorationFrontLogic,
            _ => null
        };
    }

    public Tilemap GetVisualTilemap(StageMapLayer layer)
    {
        return layer switch
        {
            StageMapLayer.Platform => _platformVisual,
            StageMapLayer.DecorationBack => _decorationBackVisual,
            StageMapLayer.DecorationFront => _decorationFrontVisual,
            _ => null
        };
    }

    public void Configure(
        Grid grid,
        Tilemap platformLogic,
        Tilemap decorationBackLogic,
        Tilemap decorationFrontLogic,
        Tilemap platformVisual,
        Tilemap decorationBackVisual,
        Tilemap decorationFrontVisual)
    {
        _grid = grid;
        _platformLogic = platformLogic;
        _decorationBackLogic = decorationBackLogic;
        _decorationFrontLogic = decorationFrontLogic;
        _platformVisual = platformVisual;
        _decorationBackVisual = decorationBackVisual;
        _decorationFrontVisual = decorationFrontVisual;
        EnforceTransformLock();
    }

    public void EnforceTransformLock()
    {
        PinToWorldOrigin(_grid != null ? _grid.transform : null);
        PinToWorldOrigin(
            _platformLogic != null ? _platformLogic.transform : null);
        PinToWorldOrigin(
            _decorationBackLogic != null
                ? _decorationBackLogic.transform
                : null);
        PinToWorldOrigin(
            _decorationFrontLogic != null
                ? _decorationFrontLogic.transform
                : null);
        PinToWorldOrigin(
            _platformVisual != null ? _platformVisual.transform : null);
        PinToWorldOrigin(
            _decorationBackVisual != null
                ? _decorationBackVisual.transform
                : null);
        PinToWorldOrigin(
            _decorationFrontVisual != null
                ? _decorationFrontVisual.transform
                : null);
    }

    public bool TryValidate(out string error)
    {
        if (_grid == null ||
            _platformLogic == null ||
            _decorationBackLogic == null ||
            _decorationFrontLogic == null ||
            _platformVisual == null ||
            _decorationBackVisual == null ||
            _decorationFrontVisual == null)
        {
            error =
                "Grid and all three Logic/Visual Tilemap pairs must be assigned.";
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
