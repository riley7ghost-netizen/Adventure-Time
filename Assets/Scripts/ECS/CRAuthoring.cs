using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Attach to each CR (NPC) GameObject inside a SubScene.
/// Replaces CRManager. _playerTransform is no longer needed in Inspector
/// because player data flows through the PlayerSharedState singleton.
/// </summary>
public class CRAuthoring : MonoBehaviour
{
    [Header("Reynolds Vehicle")]
    public float  maxSpeed  = 3f;
    public float  maxForce  = 10f;
    public float  mass      = 1f;
    public float  turnSpeed = 120f;

    [Header("Area Boundary")]
    public float   areaRadius = 25f;
    public Vector3 areaCenter = new(0f, 1f, 0f);

    [Header("Interaction")]
    public float triggerDistance   = 20f;
    public float keyCooldown       = 0.5f;
    public float chaseFleeDuration = 5f;

    [Header("Wander (Reynolds sphere)")]
    public float wanderDistance = 2f;
    public float wanderRadius   = 1.5f;
    public float wanderJitter   = 40f;  // degrees/sec

    class Baker : Baker<CRAuthoring>
    {
        public override void Bake(CRAuthoring a)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(e, new CRTag());

            AddComponent(e, new CRConfigData
            {
                maxSpeed          = a.maxSpeed,
                maxForce          = a.maxForce,
                mass              = a.mass,
                turnSpeed         = a.turnSpeed,
                areaRadius        = a.areaRadius,
                areaCenter        = new float3(a.areaCenter.x, a.areaCenter.y, a.areaCenter.z),
                triggerDistance   = a.triggerDistance,
                keyCooldown       = a.keyCooldown,
                chaseFleeDuration = a.chaseFleeDuration,
                wanderDistance    = a.wanderDistance,
                wanderRadius      = a.wanderRadius,
                wanderJitter      = a.wanderJitter
            });

            AddComponent(e, new CRStateData
            {
                phase           = CRPhase.Idle,
                currentMaxSpeed = a.maxSpeed,
                wanderAngle     = UnityEngine.Random.Range(0f, 360f)
            });

            // Unique seed per entity based on name hash + position
            uint seed = (uint)(a.gameObject.name.GetHashCode() ^ a.transform.position.GetHashCode());
            if (seed == 0) seed = 1;
            AddComponent(e, new RandomData
            {
                rng = new Unity.Mathematics.Random(seed)
            });
        }
    }
}
