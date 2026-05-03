using Unity.Entities;
using UnityEngine;

/// <summary>
/// Attach to each MissionSpot GameObject inside a SubScene.
/// spotIndex must be unique (0-3) and match the value set on MissionVisualBridge.
/// spotRadius replaces the trigger collider; tune to match the original collider size.
/// </summary>
public class MissionSpotAuthoring : MonoBehaviour
{
    public float completeTime = 20f;
    public float spotRadius   = 2f;
    [Tooltip("Must match MissionVisualBridge.spotIndex on the same GameObject (0-3).")]
    public int   spotIndex;

    class Baker : Baker<MissionSpotAuthoring>
    {
        public override void Bake(MissionSpotAuthoring a)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(e, new MissionSpotData
            {
                completeTime = a.completeTime,
                spotRadius   = a.spotRadius
            });
            AddComponent(e, new MissionSpotIndex { value = a.spotIndex });
        }
    }
}
