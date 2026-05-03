using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 掛在有視覺的 Player / CR GameObject 上。
/// 每幀把 ECS entity 的 LocalTransform 寫回 GameObject.transform，
/// 讓 Renderer / Animator 等 Unity 元件保持正確位置和朝向。
/// MissionSpot 不會移動，不需要掛這個。
/// </summary>
public class TransformSyncBridge : MonoBehaviour
{
    public enum SyncTarget { Player, CR }

    [SerializeField] SyncTarget target;
    [SerializeField] int crIndex; // 只有 CR 才需要，對應 ECSBootstrap.crTransforms 的順序

    Entity        _entity;
    EntityManager _em;
    bool          _ready;

    void Start()
    {
        var bootstrap = ECSBootstrap.Instance;
        _entity = target == SyncTarget.Player
            ? bootstrap.GetPlayerEntity()
            : bootstrap.GetCREntity(crIndex);

        _em    = World.DefaultGameObjectInjectionWorld.EntityManager;
        _ready = true;
    }

    void LateUpdate()
    {
        if (!_ready) return;
        var lt = _em.GetComponentData<LocalTransform>(_entity);
        transform.SetPositionAndRotation(lt.Position, lt.Rotation);
    }
}
