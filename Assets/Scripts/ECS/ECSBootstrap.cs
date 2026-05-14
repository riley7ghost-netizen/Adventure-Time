using Unity.Entities;
using UnityEngine;

/// <summary>
/// 場景中唯一的 ECS 進入點。
/// 各 Factory 為純 C# [Serializable] 類別，Inspector 裡直接展開設定。
/// Awake() 依序呼叫每個 Factory.Create()，確保所有 entity 在
/// TransformSyncBridge.Start() 執行前已建立。
/// </summary>
public class ECSBootstrap : MonoBehaviour
{
    public static ECSBootstrap Instance { get; private set; }

    [Header("Player")]
    [SerializeField] PlayerFactory playerFactory = new();

    [Header("CR NPCs")]
    [SerializeField] CRFactory crFactory = new();

    [Header("Mission Spots")]
    [SerializeField] MissionSpotFactory missionFactory = new();

    [Header("Immunity Items")]
    [SerializeField] ImmunityItemFactory immunityFactory = new();

    /// <summary>供 TransformSyncBridge 取得 Player entity。</summary>
    public Entity PlayerEntity => playerFactory.PlayerEntity;

    void Awake()
    {
        Instance = this;
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        playerFactory.Create(em);
        crFactory.Create(em);
        missionFactory.Create(em);
        immunityFactory.Create(em);
    }
}
