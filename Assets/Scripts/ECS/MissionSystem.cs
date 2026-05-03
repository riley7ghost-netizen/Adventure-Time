using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Tracks how long the player stands inside each mission zone.
/// Uses XZ-plane distance check instead of trigger colliders
/// (trigger colliders require Unity Physics; direct distance is simpler in DOTS).
/// Timer does NOT reset on exit — same behaviour as the original MissionSpot.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PlayerSystem))]
public partial class MissionSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<MissionSpotData>();
        RequireForUpdate<PlayerSharedState>();
    }

    protected override void OnUpdate()
    {
        float  dt        = SystemAPI.Time.DeltaTime;
        float3 playerPos = SystemAPI.GetSingleton<PlayerSharedState>().position;

        foreach (var (spot, transform) in
            SystemAPI.Query<RefRW<MissionSpotData>, RefRO<LocalTransform>>())
        {
            if (spot.ValueRO.isComplete) continue;

            // Flatten to XZ plane before distance check
            float dist = math.distance(
                new float2(transform.ValueRO.Position.x, transform.ValueRO.Position.z),
                new float2(playerPos.x, playerPos.z));

            spot.ValueRW.playerInside = dist <= spot.ValueRO.spotRadius;

            if (spot.ValueRO.playerInside)
                spot.ValueRW.timer += dt;

            if (spot.ValueRO.timer >= spot.ValueRO.completeTime)
                spot.ValueRW.isComplete = true;
        }
    }
}
