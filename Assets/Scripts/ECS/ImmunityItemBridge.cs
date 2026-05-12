using Unity.Collections;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// 掛在免疫道具 GameObject 上。
/// 當 ECS entity 被標記為已撿取時，隱藏 Renderer。
/// itemIndex 必須與 ECSBootstrap.immunityItemTransforms 的陣列順序一致（0, 1, 2…）。
/// </summary>
[RequireComponent(typeof(Renderer))]
public class ImmunityItemBridge : MonoBehaviour
{
    [SerializeField] int itemIndex;

    Renderer      _rend;
    EntityQuery   _query;
    EntityManager _em;
    bool          _initialized;

    /// <summary>ECSBootstrap 在 Instantiate 後立即呼叫，設定對應的 entity index。</summary>
    public void Init(int index) => itemIndex = index;

    void Start()
    {
        _rend = GetComponent<Renderer>();

        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return;

        _em    = world.EntityManager;
        _query = _em.CreateEntityQuery(
            ComponentType.ReadOnly<ImmunityItemData>(),
            ComponentType.ReadOnly<ImmunityItemIndex>());
        _initialized = true;
    }

    void LateUpdate()
    {
        if (!_initialized || _query.IsEmpty) return;

        var entities = _query.ToEntityArray(Allocator.Temp);
        foreach (var e in entities)
        {
            if (_em.GetComponentData<ImmunityItemIndex>(e).value != itemIndex) continue;
            _rend.enabled = !_em.GetComponentData<ImmunityItemData>(e).isCollected;
            break;
        }
        entities.Dispose();
    }

    void OnDestroy()
    {
        if (_initialized && World.DefaultGameObjectInjectionWorld is { IsCreated: true })
            _query.Dispose();
    }
}
