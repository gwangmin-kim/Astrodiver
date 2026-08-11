[System.Serializable]
public abstract class UpgradeEffect
{
    public abstract bool TryValidate(out string error);
    public abstract bool TryApply(UpgradeEffectContext context, out string error);
}
