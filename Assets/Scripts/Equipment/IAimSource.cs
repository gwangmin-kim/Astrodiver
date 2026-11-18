using UnityEngine;

namespace Astrodiver.Equipment
{
    /// <summary>
    /// 장비가 조준 판정에 사용하는 기준선.
    /// 총구 위치(장비 크기)에 의존하지 않도록 몸 중심에서 손이 뻗은 방향으로 정의된다.
    /// </summary>
    public interface IAimSource
    {
        /// <summary>조준 기준점(몸 중심). 손이 이 점을 중심으로 궤도를 그린다.</summary>
        Vector2 AimOrigin { get; }

        /// <summary>현재 손이 뻗어 있는 방향(정규화).</summary>
        Vector2 AimDirection { get; }
    }
}
