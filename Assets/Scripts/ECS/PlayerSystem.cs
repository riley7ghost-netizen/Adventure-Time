using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Reads legacy Input, integrates Reynolds steering for the player,
/// and publishes position/velocity/interact to the PlayerSharedState singleton
/// so CRSystem and MissionSystem can read it without querying the player entity.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class PlayerSystem : SystemBase
{
    protected override void OnCreate()
    {
        // Create the singleton that other systems read.
        EntityManager.CreateEntity(ComponentType.ReadWrite<PlayerSharedState>());
        RequireForUpdate<PlayerTag>();
    }

    protected override void OnUpdate()
    {
        float dt = SystemAPI.Time.DeltaTime;

        // --- Input (legacy API, same as original PlayerManager) ---
        float3 dir = float3.zero;
        if (Input.GetKey(KeyCode.W)) dir.z += 1f;
        if (Input.GetKey(KeyCode.A)) dir.x -= 1f;
        if (Input.GetKey(KeyCode.S)) dir.z -= 1f;
        if (Input.GetKey(KeyCode.D)) dir.x += 1f;
        float dirLen = math.length(dir);
        if (dirLen > 0.001f) dir /= dirLen;
        bool interact = Input.GetKeyDown(KeyCode.Space);

        float3 outPos = float3.zero;
        float3 outVel = float3.zero;

        foreach (var (vehicle, transform, flash) in
            SystemAPI.Query<RefRW<VehicleData>, RefRW<LocalTransform>, RefRW<InteractFlashData>>()
            .WithAll<PlayerTag>())
        {
            // Reynolds steering:
            //   desired  = input_dir * max_speed
            //   steering = clamp(desired - velocity, max_force)
            //   accel    = steering / mass
            //   velocity = clamp(velocity + accel * dt, max_speed)
            float3 desired  = dir * vehicle.ValueRO.maxSpeed;
            float3 steering = ClampMag(desired - vehicle.ValueRO.velocity, vehicle.ValueRO.maxForce);
            float3 accel    = steering / vehicle.ValueRO.mass;
            float3 newVel   = ClampMag(vehicle.ValueRO.velocity + accel * dt, vehicle.ValueRO.maxSpeed);

            vehicle.ValueRW.velocity    = newVel;
            transform.ValueRW.Position += newVel * dt;

            // Red flash on Space
            if (interact) { flash.ValueRW.active = true; flash.ValueRW.timer = 0.25f; }
            if (flash.ValueRO.active)
            {
                flash.ValueRW.timer -= dt;
                if (flash.ValueRO.timer <= 0f) flash.ValueRW.active = false;
            }

            outPos = transform.ValueRO.Position;
            outVel = newVel;
        }

        // Publish shared state for other systems
        SystemAPI.SetSingleton(new PlayerSharedState
        {
            position          = outPos,
            velocity          = outVel,
            interactThisFrame = interact
        });
    }

    // Equivalent of Vector3.ClampMagnitude
    static float3 ClampMag(float3 v, float max)
    {
        float sq = math.lengthsq(v);
        return sq > max * max ? v * (max / math.sqrt(sq)) : v;
    }
}
