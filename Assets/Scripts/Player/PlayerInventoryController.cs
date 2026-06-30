using UnityEngine;

public class PlayerInventoryController : MonoBehaviour
{
    public MagnetData magnetData;

    public void CollectResourceFragment()
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
