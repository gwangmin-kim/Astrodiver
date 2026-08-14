[System.Serializable]
public abstract class UpgradeEffect
{
    public abstract bool TryValidate(out string error);
    public abstract bool TryApply(UpgradeEffectContext context, out string error);

    public virtual bool TryCreatePreview(
        GameRuntimeData runtimeData,
        out UpgradeEffectPreview preview)
    {
        preview = default;
        return false;
    }
}
