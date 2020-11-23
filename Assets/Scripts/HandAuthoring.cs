using Unity.Entities;
using UnityEngine;

public struct Hand : IComponentData {}

[DisallowMultipleComponent]
public class HandAuthoring : MonoBehaviour, IConvertGameObjectToEntity {
  public int PlayerIndex;

  public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
    dstManager.AddComponent<Hand>(entity);
    dstManager.AddSharedComponentData(entity, new PlayerIndex { Value = PlayerIndex });
  }
}