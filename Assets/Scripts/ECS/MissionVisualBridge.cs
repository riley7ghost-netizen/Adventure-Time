using Unity.Collections;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Hybrid MonoBehaviour: reads MissionSpotData from the matching ECS entity
/// and drives the Renderer color (green = player inside, black = complete, white = idle).
/// Attach to the same MissionSpot GameObject as MissionSpotAuthoring.
/// Set spotIndex to match MissionSpotAuthoring.spotIndex (0-3).
/// </summary>
[RequireComponent(typeof(Renderer))]
public class MissionVisualBridge : MonoBehaviour
{
    [Tooltip("Must match MissionSpotAuthoring.spotIndex on this GameObject.")]
    public int spotIndex;

    Renderer      _rend;
    Color         _originalColor;
    EntityQuery   _query;
    EntityManager _em;
    bool          _initialized;

    void Start()
    {
        _rend          = GetComponent<Renderer>();
        _originalColor = _rend.material.color;

        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return;

        _em          = world.EntityManager;
        _query       = _em.CreateEntityQuery(
            ComponentType.ReadOnly<MissionSpotData>(),
            ComponentType.ReadOnly<MissionSpotIndex>());
        _initialized = true;
    }

    void LateUpdate()
    {
        if (!_initialized || _query.IsEmpty) return;

        var entities = _query.ToEntityArray(Allocator.Temp);
        foreach (var e in entities)
        {
            if (_em.GetComponentData<MissionSpotIndex>(e).value != spotIndex) continue;

            var data = _em.GetComponentData<MissionSpotData>(e);
            if (data.isComplete)
                _rend.material.color = Color.black;
            else if (data.playerInside)
                _rend.material.color = Color.green;
            else
                _rend.material.color = _originalColor;
            break;
        }
        entities.Dispose();
    }

    void OnDestroy() { if (_initialized) _query.Dispose(); }
}
