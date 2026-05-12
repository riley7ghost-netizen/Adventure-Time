using Unity.Entities;
using UnityEngine;

/// <summary>
/// 讀取 ECS Player entity 的狀態並更新視覺。
/// 優先序：互動閃紅 > 免疫顏色 > 原始顏色
/// 之後要加特效：在 OnImmunityChanged() 裡呼叫粒子 / Animator 等。
/// </summary>
[RequireComponent(typeof(Renderer))]
public class PlayerVisualBridge : MonoBehaviour
{
    [SerializeField] Color immuneColor = Color.cyan;

    Renderer      _rend;
    Color         _originalColor;
    EntityManager _em;
    EntityQuery   _query;
    bool          _initialized;
    bool          _wasImmune;   // 上一幀的免疫狀態，用來偵測狀態切換

    void Start()
    {
        _rend          = GetComponent<Renderer>();
        _originalColor = _rend.material.color;

        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return;

        _em    = world.EntityManager;
        _query = _em.CreateEntityQuery(
            ComponentType.ReadOnly<PlayerTag>(),
            ComponentType.ReadOnly<InteractFlashData>(),
            ComponentType.ReadOnly<PlayerImmunityData>());
        _initialized = true;
    }

    void LateUpdate()
    {
        if (!_initialized || _query.IsEmpty) return;

        var e        = _query.GetSingletonEntity();
        var flash    = _em.GetComponentData<InteractFlashData>(e);
        var immunity = _em.GetComponentData<PlayerImmunityData>(e);

        // 優先序：閃紅 > 免疫 > 原色
        if (flash.active)
            _rend.material.color = Color.red;
        else if (immunity.isImmune)
            _rend.material.color = immuneColor;
        else
            _rend.material.color = _originalColor;

        // 狀態切換時觸發（之後在這裡加特效）
        if (immunity.isImmune != _wasImmune)
        {
            OnImmunityChanged(immunity.isImmune);
            _wasImmune = immunity.isImmune;
        }
    }

    /// <summary>
    /// 免疫狀態切換時呼叫。
    /// active=true 表示剛進入免疫，active=false 表示剛解除。
    /// 之後在這裡加：粒子特效、Animator trigger、音效等。
    /// </summary>
    void OnImmunityChanged(bool active)
    {
        // 目前只改顏色（已在 LateUpdate 處理）
        // 範例：GetComponent<ParticleSystem>()?.Play();
    }

    void OnDestroy()
    {
        if (_initialized && World.DefaultGameObjectInjectionWorld is { IsCreated: true })
            _query.Dispose();
    }
}
