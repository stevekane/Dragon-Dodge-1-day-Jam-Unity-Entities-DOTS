using Unity.Entities;
using UnityEngine;

public class ElementCardAuthoring : MonoBehaviour, IConvertGameObjectToEntity {
  public Element Element;

  public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
    dstManager.AddComponentData(entity, new ElementCard { Element = Element });
  }
}