using Unity.Entities;
using UnityEngine;

/// <summary>
/// Hybrid MonoBehaviour: reads InteractFlashData from the ECS player entity
/// and updates the MeshRenderer color (red flash on Space, original otherwise).
/// Attach to the same Player GameObject as PlayerAuthoring.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class PlayerVisualBridge : MonoBehaviour
{
    Renderer    _rend;
    Color       _originalColor;
    EntityQuery _query;
    bool        _initialized;

    void Start()
    {
        _rend          = GetComponent<Renderer>();
        _originalColor = _rend.material.color;

        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return;

        _query       = world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<PlayerTag>(),
            ComponentType.ReadOnly<InteractFlashData>());
        _initialized = true;
    }

    void LateUpdate()
    {
        if (!_initialized || _query.IsEmpty) return;
        var flash = _query.GetSingleton<InteractFlashData>();
        _rend.material.color = flash.active ? Color.red : _originalColor;
    }

    void OnDestroy() { if (_initialized) _query.Dispose(); }
}
