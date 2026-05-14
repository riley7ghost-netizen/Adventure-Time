using Unity.Entities;
using Unity.Mathematics;

// ── Tags ──────────────────────────────────────────────────────────────────
public struct PlayerTag : IComponentData { }
public struct CRTag     : IComponentData { }

// ── Player ────────────────────────────────────────────────────────────────
/// <summary>Reynolds Simple Vehicle state for the player.</summary>
public struct VehicleData : IComponentData
{
    public float3 velocity;
    public float  maxSpeed;
    public float  maxForce;
    public float  mass;
}

public struct PlayerInputData : IComponentData
{
    public float3 direction;
    public bool   interact;
}

/// <summary>Drives the red-flash visual on Space press.</summary>
public struct InteractFlashData : IComponentData
{
    public float timer;
    public bool  active;
}

/// <summary>
/// Singleton written each frame by PlayerSystem.
/// Read by CRSystem and MissionSystem so they don't need direct entity queries.
/// </summary>
public struct PlayerSharedState : IComponentData
{
    public float3 position;
    public float3 velocity;
    public bool   interactThisFrame;
    public bool   isImmune;          // 免疫期間 CRSystem 不觸發 phase change
}

// ── CR (NPC) ──────────────────────────────────────────────────────────────
public enum CRPhase : byte { Idle, Chase, Flee }

public struct CRStateData : IComponentData
{
    public CRPhase phase;
    public float3  velocity;
    public float   timer;
    public float   nextTriggerTime;
    public float   getAffectedTime;
    public float   currentMaxSpeed;
    public float   wanderAngle;      // degrees, updated each frame
}

public struct CRConfigData : IComponentData
{
    public float  maxSpeed;
    public float  maxForce;
    public float  mass;
    public float  turnSpeed;         // degrees / sec
    public float  areaRadius;
    public float3 areaCenter;
    public float  triggerDistance;
    public float  keyCooldown;
    public float  chaseFleeDuration;
    public float  wanderDistance;
    public float  wanderRadius;
    public float  wanderJitter;      // degrees / sec max angular jitter
}

/// <summary>Per-entity RNG so each CR wanders independently.</summary>
public struct RandomData : IComponentData
{
    public Unity.Mathematics.Random rng;
}

// ── Immunity ──────────────────────────────────────────────────────────────
/// <summary>掛在 Player entity 上，記錄免疫剩餘時間。</summary>
public struct PlayerImmunityData : IComponentData
{
    public float timer;
    public bool  isImmune;
}

/// <summary>免疫道具 entity 的資料。</summary>
public struct ImmunityItemData : IComponentData
{
    public float pickupRadius;
    public float immunityDuration;
    public bool  isCollected;
}

/// <summary>用來讓 ImmunityItemBridge 找到對應 entity 的穩定 index。</summary>
public struct ImmunityItemIndex : IComponentData
{
    public int value;
}

// ── CR Spawn Command (FM Command Pattern) ────────────────────────────────────
/// <summary>Singleton: holds the CR template (Prefab) entity created by CRFactory.</summary>
public struct CRPrefabData : IComponentData
{
    public Entity value;
}

/// <summary>
/// 一個短命 entity，代表「在此位置生成一個 CR」的請求。
/// CRFactory.Awake() 發出，SpawnCRSystem 消費後銷毀。
/// </summary>
public struct SpawnCRCommand : IComponentData
{
    public float3     position;
    public quaternion rotation;
    public float3     areaCenter;
    public float      areaRadius;
    public uint       rngSeed;
    public int        spawnIndex;   // 成為 CRSpawnIndex.value
}

/// <summary>
/// 每個已生成的 CR entity 的穩定 index。
/// TransformSyncBridge 用來定位對應的 entity。
/// </summary>
public struct CRSpawnIndex : IComponentData
{
    public int value;
}

// ── MissionSpot ────────────────────────────────────────────────────────────
public struct MissionSpotData : IComponentData
{
    public float timer;
    public float completeTime;
    public float spotRadius;    // proximity radius replaces trigger collider
    public bool  isComplete;
    public bool  playerInside;
}

/// <summary>Stable index (0-3) used by MissionVisualBridge to identify its entity.</summary>
public struct MissionSpotIndex : IComponentData
{
    public int value;
}
