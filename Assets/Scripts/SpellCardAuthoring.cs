using Unity.Entities;
using UnityEngine;

public class SpellCardAuthoring : MonoBehaviour, IConvertGameObjectToEntity {
  public Spell Spell;

  public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
    dstManager.AddComponentData(entity, new SpellCard { Spell = Spell });
  }
}