using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 掛在有視覺的 Player / CR GameObject 上。
/// 每幀把 ECS entity 的 LocalTransform 寫回 GameObject.transform。
///
/// Player：Start() 即可從 PlayerFactory.Instance 取得 entity。
/// CR：SpawnCRSystem 在第一幀才建立 entity，因此使用 lazy 查找。
///     LateUpdate() 每幀嘗試找對應的 CRSpawnIndex entity；找到後快取，不再搜尋。
/// </summary>
public class TransformSyncBridge : MonoBehaviour
{
    public enum SyncTarget { Player, CR }

    [SerializeField] SyncTarget target;
    [SerializeField] int crIndex;  // 只有 CR 才需要，對應 CRFactory.crEntries 的順序

    Entity        _entity;
    EntityManager _em;
    bool          _ready;

    // CR lazy-find 用
    EntityQuery _crQuery;
    bool        _queryCreated;

    void Start()
    {
        _em = World.DefaultGameObjectInjectionWorld.EntityManager;

        if (target == SyncTarget.Player)
        {
            _entity = ECSBootstrap.Instance.PlayerEntity;
            _ready  = true;
        }
        else
        {
            // CR entity 由 SpawnCRSystem 在第一幀建立，這裡先準備查詢
            _crQuery      = _em.CreateEntityQuery(
                ComponentType.ReadOnly<CRTag>(),
                ComponentType.ReadOnly<CRSpawnIndex>(),
                ComponentType.ReadOnly<LocalTransform>());
            _queryCreated = true;
        }
    }

    void LateUpdate()
    {
        if (!_ready)
        {
            TryFindCREntity();
            if (!_ready) return;
        }

        var lt = _em.GetComponentData<LocalTransform>(_entity);
        transform.SetPositionAndRotation(lt.Position, lt.Rotation);
    }

    void TryFindCREntity()
    {
        if (_crQuery.IsEmpty) return;

        var entities = _crQuery.ToEntityArray(Allocator.Temp);
        foreach (var e in entities)
        {
            if (_em.GetComponentData<CRSpawnIndex>(e).value != crIndex) continue;
            _entity = e;
            _ready  = true;
            break;
        }
        entities.Dispose();
    }

    void OnDestroy()
    {
        if (_queryCreated && World.DefaultGameObjectInjectionWorld is { IsCreated: true })
            _crQuery.Dispose();
    }
}
