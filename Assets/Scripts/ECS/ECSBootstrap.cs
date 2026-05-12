using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 掛在場景裡一個空 GameObject 上。
/// 在 Start() 用 EntityManager 直接建立所有 ECS entity，
/// 不需要 SubScene / Baker / Authoring。
/// Inspector 裡把場景中的 Player、CR、MissionSpot GameObject 拖進對應欄位。
/// </summary>
public class ECSBootstrap : MonoBehaviour
{
    public static ECSBootstrap Instance { get; private set; }

    /// <summary>每個 CR 各自的巡邏範圍設定。</summary>
    [System.Serializable]
    public class CREntry
    {
        public Transform transform;
        public float     areaRadius = 25f;
        // areaCenter 自動使用 transform.position，不需要在 Inspector 重複設定
    }

    [Header("Player")]
    [SerializeField] Transform playerTransform;
    [SerializeField] float playerMaxSpeed = 10f;
    [SerializeField] float playerMaxForce = 60f;
    [SerializeField] float playerMass     = 1f;

    [Header("CR NPCs")]
    [SerializeField] CREntry[] crEntries;           // 每個 CR 有自己的 areaCenter / areaRadius
    [SerializeField] float crMaxSpeed          = 3f;
    [SerializeField] float crMaxForce          = 10f;
    [SerializeField] float crMass              = 1f;
    [SerializeField] float crTurnSpeed         = 120f;
    [SerializeField] float crTriggerDistance   = 20f;
    [SerializeField] float crKeyCooldown       = 0.5f;
    [SerializeField] float crChaseFleeDuration = 5f;
    [SerializeField] float crWanderDistance    = 2f;
    [SerializeField] float crWanderRadius      = 1.5f;
    [SerializeField] float crWanderJitter      = 40f;

    [Header("Mission Spots (drag 4 spots)")]
    [SerializeField] Transform[] missionTransforms;
    [SerializeField] float missionCompleteTime = 20f;
    [SerializeField] float missionSpotRadius   = 2f;

    [Header("Immunity Items (runtime spawned)")]
    [SerializeField] GameObject immunityItemPrefab;       // 帶有 ImmunityItemBridge 的 Prefab
    [SerializeField] int        immunityItemCount  = 1;   // 一次生成幾個
    [SerializeField] Vector3    immunitySpawnCenter = Vector3.zero; // 生成範圍中心
    [SerializeField] float      immunitySpawnRadius = 20f;          // 生成範圍半徑
    [SerializeField] float      immunityPickupRadius = 2f;
    [SerializeField] float      immunityDuration     = 10f;

    // Entity handles — TransformSyncBridge / VisualBridge 透過這裡取得對應 entity
    Entity   _playerEntity;
    Entity[] _crEntities;
    Entity[] _missionEntities;
    Entity[] _immunityEntities;

    void Awake()
    {
        Instance = this;

        // 在 Awake 建立 entity，確保所有 TransformSyncBridge.Start() 執行時 entity 已存在
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;

        _playerEntity = CreatePlayerEntity(em);

        _crEntities = new Entity[crEntries.Length];
        for (int i = 0; i < crEntries.Length; i++)
            _crEntities[i] = CreateCREntity(em, crEntries[i], i);

        _missionEntities = new Entity[missionTransforms.Length];
        for (int i = 0; i < missionTransforms.Length; i++)
            _missionEntities[i] = CreateMissionEntity(em, missionTransforms[i], i);

        _immunityEntities = new Entity[immunityItemCount];
        for (int i = 0; i < immunityItemCount; i++)
        {
            var spawnPos = RandomPointInCircle(immunitySpawnCenter, immunitySpawnRadius);
            _immunityEntities[i] = CreateImmunityItemEntity(em, spawnPos, i);

            if (immunityItemPrefab != null)
            {
                var go = Instantiate(immunityItemPrefab, spawnPos, Quaternion.identity);
                go.GetComponent<ImmunityItemBridge>()?.Init(i);
            }
        }
    }

    // ── 建立各類 entity ───────────────────────────────────────────────────

    Entity CreatePlayerEntity(EntityManager em)
    {
        var e = em.CreateEntity(
            typeof(PlayerTag),
            typeof(VehicleData),
            typeof(PlayerInputData),
            typeof(InteractFlashData),
            typeof(PlayerImmunityData),
            typeof(LocalTransform));

        em.SetComponentData(e, new VehicleData
        {
            maxSpeed = playerMaxSpeed,
            maxForce = playerMaxForce,
            mass     = playerMass
        });
        em.SetComponentData(e, LocalTransform.FromPosition(playerTransform.position));
        return e;
    }

    Entity CreateCREntity(EntityManager em, CREntry entry, int index)
    {
        var e = em.CreateEntity(
            typeof(CRTag),
            typeof(CRConfigData),
            typeof(CRStateData),
            typeof(RandomData),
            typeof(LocalTransform));

        em.SetComponentData(e, new CRConfigData
        {
            maxSpeed          = crMaxSpeed,
            maxForce          = crMaxForce,
            mass              = crMass,
            turnSpeed         = crTurnSpeed,
            areaRadius        = entry.areaRadius,
            areaCenter        = new float3(entry.transform.position.x, entry.transform.position.y, entry.transform.position.z),
            triggerDistance   = crTriggerDistance,
            keyCooldown       = crKeyCooldown,
            chaseFleeDuration = crChaseFleeDuration,
            wanderDistance    = crWanderDistance,
            wanderRadius      = crWanderRadius,
            wanderJitter      = crWanderJitter
        });
        em.SetComponentData(e, new CRStateData
        {
            phase           = CRPhase.Idle,
            currentMaxSpeed = crMaxSpeed,
            wanderAngle     = UnityEngine.Random.Range(0f, 360f)
        });

        var t = entry.transform;
        uint seed = (uint)((t.position.GetHashCode() * 397) ^ (index + 1));
        if (seed == 0) seed = 1;
        em.SetComponentData(e, new RandomData { rng = new Unity.Mathematics.Random(seed) });
        em.SetComponentData(e, LocalTransform.FromPositionRotation(t.position, t.rotation));

        return e;
    }

    Entity CreateMissionEntity(EntityManager em, Transform t, int index)
    {
        var e = em.CreateEntity(
            typeof(MissionSpotData),
            typeof(MissionSpotIndex),
            typeof(LocalTransform));

        em.SetComponentData(e, new MissionSpotData
        {
            completeTime = missionCompleteTime,
            spotRadius   = missionSpotRadius
        });
        em.SetComponentData(e, new MissionSpotIndex { value = index });
        em.SetComponentData(e, LocalTransform.FromPosition(t.position));
        return e;
    }

    Entity CreateImmunityItemEntity(EntityManager em, Vector3 position, int index)
    {
        var e = em.CreateEntity(
            typeof(ImmunityItemData),
            typeof(ImmunityItemIndex),
            typeof(LocalTransform));

        em.SetComponentData(e, new ImmunityItemData
        {
            pickupRadius     = immunityPickupRadius,
            immunityDuration = immunityDuration
        });
        em.SetComponentData(e, new ImmunityItemIndex { value = index });
        em.SetComponentData(e, LocalTransform.FromPosition(position));
        return e;
    }

    // ── 供 Bridge 查詢 ────────────────────────────────────────────────────

    public Entity GetPlayerEntity()                => _playerEntity;
    public Entity GetCREntity(int index)           => _crEntities[index];
    public Entity GetMissionEntity(int index)      => _missionEntities[index];
    public Entity GetImmunityItemEntity(int index) => _immunityEntities[index];

    // XZ 平面上的圓形範圍內取隨機點，Y 保持與 center 相同
    static Vector3 RandomPointInCircle(Vector3 center, float radius)
    {
        var offset = UnityEngine.Random.insideUnitCircle * radius;
        return new Vector3(center.x + offset.x, center.y, center.z + offset.y);
    }
}
