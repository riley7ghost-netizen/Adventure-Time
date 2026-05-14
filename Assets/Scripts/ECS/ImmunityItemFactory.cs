using System;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[Serializable]
public class ImmunityItemFactory
{
    [SerializeField] GameObject itemPrefab;
    [SerializeField] int        count        = 1;
    [SerializeField] Vector3    spawnCenter  = Vector3.zero;
    [SerializeField] float      spawnRadius  = 20f;
    [SerializeField] float      pickupRadius = 2f;
    [SerializeField] float      duration     = 10f;

    public void Create(EntityManager em)
    {
        for (int i = 0; i < count; i++)
        {
            var pos = RandomPointInCircle(spawnCenter, spawnRadius);

            var e = em.CreateEntity(
                typeof(ImmunityItemData),
                typeof(ImmunityItemIndex),
                typeof(LocalTransform));

            em.SetComponentData(e, new ImmunityItemData
            {
                pickupRadius     = pickupRadius,
                immunityDuration = duration
            });
            em.SetComponentData(e, new ImmunityItemIndex { value = i });
            em.SetComponentData(e, LocalTransform.FromPosition(pos));

            if (itemPrefab != null)
            {
                var go = UnityEngine.Object.Instantiate(itemPrefab, pos, Quaternion.identity);
                go.GetComponent<ImmunityItemBridge>()?.Init(i);
            }
        }
    }

    static Vector3 RandomPointInCircle(Vector3 center, float radius)
    {
        var offset = UnityEngine.Random.insideUnitCircle * radius;
        return new Vector3(center.x + offset.x, center.y, center.z + offset.y);
    }
}
