using Unity.Entities;
using UnityEngine;

public class ElementCardDeckAuthoring : MonoBehaviour, IConvertGameObjectToEntity {
  public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
    dstManager.AddComponent<ElementCardDeck>(entity);
    dstManager.AddBuffer<ElementCardEntry>(entity);
  }
}