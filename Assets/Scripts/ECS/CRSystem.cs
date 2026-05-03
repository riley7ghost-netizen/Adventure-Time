using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Handles all 6 CR (NPC) entities.
/// Replicates the three-phase Reynolds behaviour from CRManager:
///   Idle  → Wander (sphere-projection method)
///   Chase → Pursuit (seek predicted player position)
///   Flee  → Flee from player current position
/// Rigidbody is removed; position/rotation set directly on LocalTransform.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PlayerSystem))]
public partial class CRSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<CRTag>();
        RequireForUpdate<PlayerSharedState>();
    }

    protected override void OnUpdate()
    {
        float dt          = SystemAPI.Time.DeltaTime;
        var   playerState = SystemAPI.GetSingleton<PlayerSharedState>();

        foreach (var (state, cfg, transform, rngData) in
            SystemAPI.Query<RefRW<CRStateData>, RefRO<CRConfigData>,
                            RefRW<LocalTransform>, RefRW<RandomData>>()
            .WithAll<CRTag>())
        {
            state.ValueRW.timer += dt;

            // ── Phase timeout → return to idle ─────────────────────────
            if (state.ValueRO.phase != CRPhase.Idle &&
                state.ValueRO.timer > state.ValueRO.getAffectedTime + cfg.ValueRO.chaseFleeDuration)
            {
                state.ValueRW.phase          = CRPhase.Idle;
                state.ValueRW.currentMaxSpeed = cfg.ValueRO.maxSpeed;
            }

            // ── Interact trigger (Space key broadcast via singleton) ────
            if (playerState.interactThisFrame &&
                state.ValueRO.timer >= state.ValueRO.nextTriggerTime)
            {
                float3 flatSelf   = Flatten(transform.ValueRO.Position);
                float3 flatPlayer = Flatten(playerState.position);
                float  dist       = math.distance(flatSelf, flatPlayer);

                if (dist < cfg.ValueRO.triggerDistance)
                {
                    state.ValueRW.nextTriggerTime = state.ValueRO.timer + cfg.ValueRO.keyCooldown;
                    state.ValueRW.getAffectedTime = state.ValueRO.timer;
                    state.ValueRW.currentMaxSpeed = dist / 0.5f;
                    state.ValueRW.phase = rngData.ValueRO.rng.NextFloat() < 0.2f
                        ? CRPhase.Flee : CRPhase.Chase;
                    rngData.ValueRW.rng = rngData.ValueRO.rng; // save updated rng state
                }
            }

            // ── Steering direction ─────────────────────────────────────
            float3 steeringDir = state.ValueRO.phase == CRPhase.Idle
                ? Wander(ref state.ValueRW, cfg.ValueRO, transform.ValueRO, ref rngData.ValueRW, dt)
                : SeekOrFlee(state.ValueRO, cfg.ValueRO, transform.ValueRO, playerState);

            // ── Boundary containment ───────────────────────────────────
            float3 toCenter = cfg.ValueRO.areaCenter - transform.ValueRO.Position;
            if (math.length(toCenter) > cfg.ValueRO.areaRadius)
                steeringDir = math.normalize(toCenter) * cfg.ValueRO.maxForce;

            // ── Reynolds integration ───────────────────────────────────
            float3 force  = ClampMag(steeringDir, cfg.ValueRO.maxForce);
            float3 accel  = force / cfg.ValueRO.mass;
            float3 newVel = ClampMag(
                state.ValueRO.velocity + accel * dt,
                state.ValueRO.currentMaxSpeed);
            state.ValueRW.velocity = newVel;

            // ── Position ───────────────────────────────────────────────
            transform.ValueRW.Position += newVel * dt;

            // ── Velocity-aligned rotation ──────────────────────────────
            if (math.lengthsq(newVel) > 0.01f)
            {
                quaternion target = quaternion.LookRotationSafe(newVel, math.up());
                transform.ValueRW.Rotation = RotateTowards(
                    transform.ValueRO.Rotation, target,
                    math.radians(cfg.ValueRO.turnSpeed) * dt);
            }
        }
    }

    // ── Steering behaviours ───────────────────────────────────────────────

    /// Reynolds sphere-projection wander.
    static float3 Wander(
        ref CRStateData state, in CRConfigData cfg,
        in LocalTransform t, ref RandomData rng, float dt)
    {
        state.wanderAngle += rng.rng.NextFloat(-cfg.wanderJitter, cfg.wanderJitter) * dt;

        float3 vel     = state.velocity;
        float3 forward = math.lengthsq(vel) > 0.0001f
            ? math.normalize(Flatten(vel))
            : math.forward(t.Rotation);

        float  rad  = math.radians(state.wanderAngle);
        float3 disp = new float3(math.cos(rad), 0f, math.sin(rad)) * cfg.wanderRadius;
        return forward * cfg.wanderDistance + disp;
    }

    /// Pursuit (chase) or Flee.
    static float3 SeekOrFlee(
        in CRStateData state, in CRConfigData cfg,
        in LocalTransform t, in PlayerSharedState player)
    {
        float3 flatSelf   = Flatten(t.Position);
        float3 flatPlayer = Flatten(player.position);

        float3 target;
        if (state.phase == CRPhase.Chase)
        {
            // Reynolds simple estimator: T = distance / own_speed
            float  T         = math.distance(flatSelf, flatPlayer)
                                / math.max(state.currentMaxSpeed, 0.001f);
            float3 playerVel = Flatten(player.velocity);
            target = flatPlayer + playerVel * T;
        }
        else
        {
            target = flatPlayer;
        }

        float3 toTarget     = target - flatSelf;
        float3 desiredVel   = state.phase == CRPhase.Chase
            ?  math.normalizesafe(toTarget) * state.currentMaxSpeed
            : -math.normalizesafe(toTarget) * state.currentMaxSpeed;

        return desiredVel - Flatten(state.velocity);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static float3 Flatten(float3 v) => new float3(v.x, 0f, v.z);

    static float3 ClampMag(float3 v, float max)
    {
        float sq = math.lengthsq(v);
        return sq > max * max ? v * (max / math.sqrt(sq)) : v;
    }

    static quaternion RotateTowards(quaternion from, quaternion to, float maxRad)
    {
        float dot   = math.abs(math.dot(from, to));
        float angle = 2f * math.acos(math.clamp(dot, 0f, 1f));
        if (angle < 0.0001f) return to;
        return math.slerp(from, to, math.min(1f, maxRad / angle));
    }
}
