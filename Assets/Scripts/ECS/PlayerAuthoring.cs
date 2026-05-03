using Unity.Entities;
using UnityEngine;

/// <summary>
/// Attach to the Player GameObject inside a SubScene.
/// Baker writes all components PlayerSystem needs.
/// Also attach PlayerVisualBridge to the same GameObject for color-flash rendering.
/// </summary>
public class PlayerAuthoring : MonoBehaviour
{
    [Header("Reynolds Vehicle")]
    public float maxSpeed = 10f;
    public float maxForce = 60f;
    public float mass     = 1f;

    class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring a)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(e, new PlayerTag());
            AddComponent(e, new VehicleData
            {
                maxSpeed = a.maxSpeed,
                maxForce = a.maxForce,
                mass     = a.mass
            });
            AddComponent<PlayerInputData>(e);
            AddComponent<InteractFlashData>(e);
        }
    }
}
