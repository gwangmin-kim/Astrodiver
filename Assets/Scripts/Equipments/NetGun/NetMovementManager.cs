using System.Collections.Generic;
using UnityEngine;

public sealed class NetMovementManager
{
    private readonly Dictionary<NetCaptureController, NetMovementState> _movementStates = new();

    public void Register(NetCaptureController net)
    {
        if (net == null || _movementStates.ContainsKey(net)) return;

        _movementStates.Add(net, new NetMovementState
        {
            net = net
        });
    }

    public void Reset(NetCaptureController net)
    {
        if (net == null || !_movementStates.TryGetValue(net, out NetMovementState state)) return;

        state.currentVelocity = Vector2.zero;
        state.targetVelocity = Vector2.zero;
        state.smoothDampVelocity = Vector2.zero;
    }

    public void SetTargetVelocity(NetCaptureController net, Vector2 targetVelocity)
    {
        if (net == null || !_movementStates.TryGetValue(net, out NetMovementState state)) return;

        state.targetVelocity = targetVelocity;
    }

    public void Update(float deltaTime, float dampingTime)
    {
        if (deltaTime <= 0f) return;

        float safeDampingTime = Mathf.Max(0f, dampingTime);

        foreach (NetMovementState state in _movementStates.Values)
        {
            UpdateState(state, deltaTime, safeDampingTime);
        }
    }

    private static void UpdateState(NetMovementState state, float deltaTime, float dampingTime)
    {
        NetCaptureController net = state.net;
        if (net == null || !net.isActiveAndEnabled)
        {
            ResetState(state);
            return;
        }

        if (!net.IsDeployed)
        {
            ResetState(state);
            return;
        }

        state.currentVelocity = dampingTime <= 0f
            ? state.targetVelocity
            : Vector2.SmoothDamp(
                state.currentVelocity,
                state.targetVelocity,
                ref state.smoothDampVelocity,
                dampingTime,
                Mathf.Infinity,
                deltaTime);

        net.transform.position += (Vector3)(state.currentVelocity * deltaTime);
        net.UpdateCapturedTargets(state.currentVelocity, deltaTime);
        state.targetVelocity = Vector2.zero;
    }

    private static void ResetState(NetMovementState state)
    {
        state.currentVelocity = Vector2.zero;
        state.targetVelocity = Vector2.zero;
        state.smoothDampVelocity = Vector2.zero;
    }

    private sealed class NetMovementState
    {
        public NetCaptureController net;
        public Vector2 currentVelocity;
        public Vector2 targetVelocity;
        public Vector2 smoothDampVelocity;
    }
}
