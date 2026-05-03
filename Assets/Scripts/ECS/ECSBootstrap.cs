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

    [Header("Player")]
    [SerializeField] Transform playerTransform;
    [SerializeField] float playerMaxSpeed = 10f;
    [SerializeField] float playerMaxForce = 60f;
    [SerializeField] float playerMass     = 1f;

    [Header("CR NPCs (shared config, drag 6 CRs)")]
    [SerializeField] Transform[] crTransforms;
    [SerializeField] float crMaxSpeed          = 3f;
    [SerializeField] float crMaxForce          = 10f;
    [SerializeField] float crMass              = 1f;
    [SerializeField] float crTurnSpeed         = 120f;
    [SerializeField] float crAreaRadius        = 25f;
    [SerializeField] Vector3 crAreaCenter      = new(0f, 1f, 0f);
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

    // Entity handles — TransformSyncBridge / VisualBridge 透過這裡取得對應 entity
    Entity   _playerEntity;
    Entity[] _crEntities;
    Entity[] _missionEntities;

    void Awake() => Instance = this;

    void Start()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;

        _playerEntity = CreatePlayerEntity(em);

        _crEntities = new Entity[crTransforms.Length];
        for (int i = 0; i < crTransforms.Length; i++)
            _crEntities[i] = CreateCREntity(em, crTransforms[i], i);

        _missionEntities = new Entity[missionTransforms.Length];
        for (int i = 0; i < missionTransforms.Length; i++)
            _missionEntities[i] = CreateMissionEntity(em, missionTransforms[i], i);
    }

    // ── 建立各類 entity ───────────────────────────────────────────────────

    Entity CreatePlayerEntity(EntityManager em)
    {
        var e = em.CreateEntity(
            typeof(PlayerTag),
            typeof(VehicleData),
            typeof(PlayerInputData),
            typeof(InteractFlashData),
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

    Entity CreateCREntity(EntityManager em, Transform t, int index)
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
            areaRadius        = crAreaRadius,
            areaCenter        = new float3(crAreaCenter.x, crAreaCenter.y, crAreaCenter.z),
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

        // 每個 CR 用位置 hash + index 產生不同的 seed
        uint seed = (uint)((t.position.GetHashCode() * 397) ^ (index + 1));
        if (seed == 0) seed = 1;
        em.SetComponentData(e, new RandomData { rng = new Unity.Mathematics.Random(seed) });
        em.SetComponentData(e, LocalTransform.FromPositionRotation(
            t.position, t.rotation));

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

    // ── 供 Bridge 查詢 ────────────────────────────────────────────────────

    public Entity GetPlayerEntity()           => _playerEntity;
    public Entity GetCREntity(int index)      => _crEntities[index];
    public Entity GetMissionEntity(int index) => _missionEntities[index];
}
