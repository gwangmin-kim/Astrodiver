using UnityEngine;

/// <summary>
/// 외부에서 플레이어의 구성요소에 접근하기 위한 싱글톤 객체
/// 씬에 종속된 플레이어 컴포넌트만 PlayerContext를 통해 접근
/// 씬 전환 간 유지되는 인벤토리는 PlayerInventoryController.Instance를 통해 접근
/// </summary>
[RequireComponent(typeof(PlayerBatteryController))]
public class PlayerContext : MonoBehaviour
{
    public static PlayerContext Instance { get; private set; }

    [Header("Owned Components")]
    public PlayerBatteryController Battery { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            Battery = GetComponent<PlayerBatteryController>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
