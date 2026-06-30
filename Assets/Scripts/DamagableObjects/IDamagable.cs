public interface IDamagable
{
    public void ApplyDamage(AttackData data);
}

public enum DamageSource
{
    Player,
}

/// <summary>
/// 공격에 대한 정보를 담고 있는 구조체
///
/// 각종 통계 기록에 사용
/// - 내가 준 피해량은 얼만지
/// - 치명타로 준 피해량은 얼만지
/// - (만약 만든다면) 소환수가 준 피해량, 기믹으로 인해 일어난 피해량은 얼만지
/// - 기타 등등
/// </summary>
public struct AttackData
{
    public float damage; // 피해량
    public DamageSource source; // 공격 주체
}
