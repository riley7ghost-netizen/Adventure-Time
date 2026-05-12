using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// 負責兩件事：
/// 1. 偵測 Player 靠近免疫道具 → 標記已撿取、啟動免疫計時器
/// 2. 每幀倒數免疫計時器，歸零時解除免疫
///
/// 在 PlayerSystem 之前執行，確保同一幀的免疫狀態能被 PlayerSystem 寫入 singleton，
/// 再由 CRSystem 讀取。
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(PlayerSystem))]
public partial class ImmunitySystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<PlayerImmunityData>();
        RequireForUpdate<ImmunityItemData>();
    }

    protected override void OnUpdate()
    {
        float dt = SystemAPI.Time.DeltaTime;

        // PlayerSharedState 由 PlayerSystem 寫入，這裡讀的是上一幀的玩家位置。
        // 1 幀的延遲對靠近偵測完全無感。
        var playerState = SystemAPI.GetSingleton<PlayerSharedState>();
        float2 playerXZ = new float2(playerState.position.x, playerState.position.z);

        // ── 第一輪：檢查道具是否被撿取 ────────────────────────────────
        bool  newlyCollected = false;
        float newDuration    = 0f;

        foreach (var (item, transform) in
            SystemAPI.Query<RefRW<ImmunityItemData>, RefRO<LocalTransform>>())
        {
            if (item.ValueRO.isCollected) continue;

            float2 itemXZ = new float2(
                transform.ValueRO.Position.x,
                transform.ValueRO.Position.z);

            if (math.distance(playerXZ, itemXZ) <= item.ValueRO.pickupRadius)
            {
                item.ValueRW.isCollected = true;
                newlyCollected           = true;
                newDuration              = item.ValueRO.immunityDuration;
            }
        }

        // ── 第二輪：更新 Player 的免疫計時器 ──────────────────────────
        foreach (var immunity in
            SystemAPI.Query<RefRW<PlayerImmunityData>>().WithAll<PlayerTag>())
        {
            if (newlyCollected)
            {
                // 撿到道具：重設計時器（可疊加）
                immunity.ValueRW.timer    = newDuration;
                immunity.ValueRW.isImmune = true;
            }
            else if (immunity.ValueRO.timer > 0f)
            {
                immunity.ValueRW.timer -= dt;
                if (immunity.ValueRO.timer <= 0f)
                {
                    immunity.ValueRW.timer    = 0f;
                    immunity.ValueRW.isImmune = false;
                }
            }
        }
    }
}
