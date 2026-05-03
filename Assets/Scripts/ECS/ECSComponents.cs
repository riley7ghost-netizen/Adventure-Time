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
