using Unity.Entities;
using UnityEngine;

public class SpellCardDeckAuthoring : MonoBehaviour, IConvertGameObjectToEntity {
  public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
    dstManager.AddComponent<SpellCardDeck>(entity);
    dstManager.AddBuffer<SpellCardDeckEntry>(entity);
  }
}