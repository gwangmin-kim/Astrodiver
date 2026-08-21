using System;

public static class ResourceDisplayOrder
{
    public static int Compare(
        ResourceDefinition left,
        ResourceDefinition right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        int order = left.SortOrder.CompareTo(right.SortOrder);
        return order != 0
            ? order
            : StringComparer.Ordinal.Compare(left.Id, right.Id);
    }

    public static int Compare(
        UpgradeResourceCost left,
        UpgradeResourceCost right)
    {
        return Compare(left.Resource, right.Resource);
    }
}
