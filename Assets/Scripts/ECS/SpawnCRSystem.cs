using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// 消費 CRFactory 發出的 SpawnCRCommand entity。
/// 每個命令：從 CR template 複製出一個新 entity，再覆寫個別欄位（位置、areaCenter、index…）。
/// 全部命令處理完後，system 因 RequireForUpdate 不再滿足而休眠，不佔用後續幀的 CPU。
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(CRSystem))]
public partial class SpawnCRSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<CRPrefabData>();
        RequireForUpdate<SpawnCRCommand>();
    }

    protected override void OnUpdate()
    {
        var prefabEntity = SystemAPI.GetSingleton<CRPrefabData>().value;
        var baseConfig   = EntityManager.GetComponentData<CRConfigData>(prefabEntity);

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (cmd, cmdEntity) in
            SystemAPI.Query<RefRO<SpawnCRCommand>>().WithEntityAccess())
        {
            var e = ecb.Instantiate(prefabEntity);   // 複製 template，不含 Prefab tag

            // 個別覆寫 ──────────────────────────────────────────────────────
            var config        = baseConfig;
            config.areaCenter = cmd.ValueRO.areaCenter;
            config.areaRadius = cmd.ValueRO.areaRadius;
            ecb.SetComponent(e, config);

            ecb.SetComponent(e, LocalTransform.FromPositionRotation(
                cmd.ValueRO.position, cmd.ValueRO.rotation));

            ecb.SetComponent(e, new CRSpawnIndex { value = cmd.ValueRO.spawnIndex });

            // 用種子初始化各自的 RNG（偏移種子避免 wanderAngle 與 RNG 序列相關）
            var rng        = new Unity.Mathematics.Random(cmd.ValueRO.rngSeed);
            float wanderDeg = rng.NextFloat(0f, 360f);
            ecb.SetComponent(e, new RandomData
            {
                rng = new Unity.Mathematics.Random(cmd.ValueRO.rngSeed ^ 0x9E3779B9u)
            });
            ecb.SetComponent(e, new CRStateData
            {
                phase           = CRPhase.Idle,
                currentMaxSpeed = baseConfig.maxSpeed,
                wanderAngle     = wanderDeg
            });

            ecb.DestroyEntity(cmdEntity);   // 命令已消費
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
