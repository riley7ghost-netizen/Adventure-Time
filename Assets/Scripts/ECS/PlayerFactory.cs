using System;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[Serializable]
public class PlayerFactory
{
    [SerializeField] Transform playerTransform;
    [SerializeField] float maxSpeed = 10f;
    [SerializeField] float maxForce = 60f;
    [SerializeField] float mass     = 1f;

    public Entity PlayerEntity { get; private set; }

    public void Create(EntityManager em)
    {
        PlayerEntity = em.CreateEntity(
            typeof(PlayerTag),
            typeof(VehicleData),
            typeof(PlayerInputData),
            typeof(InteractFlashData),
            typeof(PlayerImmunityData),
            typeof(LocalTransform));

        em.SetComponentData(PlayerEntity, new VehicleData
        {
            maxSpeed = maxSpeed,
            maxForce = maxForce,
            mass     = mass
        });
        em.SetComponentData(PlayerEntity, LocalTransform.FromPosition(playerTransform.position));
    }
}
