using UnityEngine;

/// <summary>
/// 외부에서 플레이어의 구성요소에 접근하기 위한 싱글톤 객체
/// 외부에서 접근할 필요가 있는 컴포넌트는 PlayerContext를 통해 접근
/// </summary>
[RequireComponent(typeof(PlayerInventoryController))]
[RequireComponent(typeof(PlayerBatteryController))]
public class PlayerContext : MonoBehaviour
{
    public static PlayerContext Instance { get; private set; }

    [Header("Owned Components")]
    public PlayerInventoryController Inventory { get; private set; }
    public PlayerBatteryController Battery { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            Inventory = GetComponent<PlayerInventoryController>();
            Battery = GetComponent<PlayerBatteryController>();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
