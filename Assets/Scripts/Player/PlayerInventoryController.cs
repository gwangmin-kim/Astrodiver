using UnityEngine;

public class PlayerInventoryController : MonoBehaviour
{
    public MagnetData magnetData;

    public void CollectCreature(CreatureResourceData data)
    {

    }

    public void CollectResourceFragment(FragementResourceData data)
    {

    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, magnetData.radius);

        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, magnetData.collectRadius);
    }
#endif
}

[System.Serializable]
public struct CreatureResourceData
{

}

[System.Serializable]
public struct FragementResourceData
{

}
