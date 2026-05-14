using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[Serializable]
public class CRFactory
{
    [Serializable]
    public class CREntry
    {
        public Transform transform;
        public float     areaRadius = 25f;
    }

    [SerializeField] CREntry[] crEntries;
    [SerializeField] float maxSpeed          = 3f;
    [SerializeField] float maxForce          = 10f;
    [SerializeField] float mass              = 1f;
    [SerializeField] float turnSpeed         = 120f;
    [SerializeField] float triggerDistance   = 20f;
    [SerializeField] float keyCooldown       = 0.5f;
    [SerializeField] float chaseFleeDuration = 5f;
    [SerializeField] float wanderDistance    = 2f;
    [SerializeField] float wanderRadius      = 1.5f;
    [SerializeField] float wanderJitter      = 40f;

    public void Create(EntityManager em)
    {
        // 1. CR template（Prefab entity，被正常 query 排除）
        var prefab = em.CreateEntity(
            typeof(CRTag),
            typeof(CRConfigData),
            typeof(CRStateData),
            typeof(RandomData),
            typeof(CRSpawnIndex),
            typeof(LocalTransform),
            typeof(Prefab));

        em.SetComponentData(prefab, new CRConfigData
        {
            maxSpeed          = maxSpeed,
            maxForce          = maxForce,
            mass              = mass,
            turnSpeed         = turnSpeed,
            triggerDistance   = triggerDistance,
            keyCooldown       = keyCooldown,
            chaseFleeDuration = chaseFleeDuration,
            wanderDistance    = wanderDistance,
            wanderRadius      = wanderRadius,
            wanderJitter      = wanderJitter
            // areaCenter / areaRadius 由 SpawnCRSystem 逐個覆寫
        });

        // 2. Singleton：讓 SpawnCRSystem 找到 template
        var singletonE = em.CreateEntity(typeof(CRPrefabData));
        em.SetComponentData(singletonE, new CRPrefabData { value = prefab });

        // 3. 每個 entry 發出一個 SpawnCRCommand
        for (int i = 0; i < crEntries.Length; i++)
        {
            var t    = crEntries[i].transform;
            uint seed = (uint)((t.position.GetHashCode() * 397) ^ (i + 1));
            if (seed == 0) seed = 1;

            var cmdE = em.CreateEntity(typeof(SpawnCRCommand));
            em.SetComponentData(cmdE, new SpawnCRCommand
            {
                position   = t.position,
                rotation   = t.rotation,
                areaCenter = new float3(t.position.x, t.position.y, t.position.z),
                areaRadius = crEntries[i].areaRadius,
                rngSeed    = seed,
                spawnIndex = i
            });
        }
    }
}
