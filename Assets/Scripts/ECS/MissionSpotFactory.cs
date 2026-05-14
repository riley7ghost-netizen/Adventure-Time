using System;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[Serializable]
public class MissionSpotFactory
{
    [SerializeField] Transform[] spotTransforms;
    [SerializeField] float completeTime = 20f;
    [SerializeField] float spotRadius   = 2f;

    public void Create(EntityManager em)
    {
        for (int i = 0; i < spotTransforms.Length; i++)
        {
            var e = em.CreateEntity(
                typeof(MissionSpotData),
                typeof(MissionSpotIndex),
                typeof(LocalTransform));

            em.SetComponentData(e, new MissionSpotData
            {
                completeTime = completeTime,
                spotRadius   = spotRadius
            });
            em.SetComponentData(e, new MissionSpotIndex { value = i });
            em.SetComponentData(e, LocalTransform.FromPosition(spotTransforms[i].position));
        }
    }
}
