using UnityEngine;

[RequireComponent(typeof(CreatureMotionController))]
public class CreatureController : CapturableObject
{
    [SerializeField] private CaptureData _data;
    [SerializeField] private CreatureBrain _brain;
    [SerializeField] private CreatureMotionController _motionController;

    private bool _isCaptured;
    private NetCaptureController _capturingNet;

    public override CaptureData CaptureData => _data;
    public override Vector2 Position => transform.position;
    public override float Radius => _data.radius;

    private void Awake()
    {
        if (_brain == null) _brain = GetComponent<CreatureBrain>();
        if (_motionController == null) _motionController = GetComponent<CreatureMotionController>();
        _brain.CaptureReleaseRequested += RequestCaptureRelease;
    }

    private void OnDestroy()
    {
        if (_brain != null) _brain.CaptureReleaseRequested -= RequestCaptureRelease;
    }

    private void OnDisable()
    {
        if (_isCaptured) _capturingNet?.RequestRelease(this, CaptureReleaseReason.Interrupted);
        ResetCapture();
    }

    public override bool CanBeCaptured(NetCaptureContext context) => isActiveAndEnabled && !_isCaptured;

    public override void OnCaptureStarted(NetCaptureContext context)
    {
        _isCaptured = true;
        _capturingNet = context.net;
        _brain.NotifyCaptureStarted(context);
    }

    public override void OnCapturedMove(CaptureMotionContext context, float deltaTime)
    {
        if (_isCaptured) _motionController.UpdateCapturedMotion(context, deltaTime);
    }

    public override void OnCaptureClamped(Vector2 position)
    {
        if (_isCaptured) _motionController.ClampCapturedPosition(position);
    }

    public override void OnCaptureReleased(CaptureReleaseReason reason)
    {
        ResetCapture();
        _brain.NotifyCaptureReleased(reason);
    }

    private void RequestCaptureRelease(CaptureReleaseReason reason)
    {
        if (_isCaptured) _capturingNet?.RequestRelease(this, reason);
    }

    private void ResetCapture()
    {
        _isCaptured = false;
        _capturingNet = null;
    }
}
