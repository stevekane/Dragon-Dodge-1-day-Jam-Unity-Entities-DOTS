using Unity.Entities;
using UnityEngine;

public class HandAuthoring : MonoBehaviour, IConvertGameObjectToEntity {
  public GameObject TilesRoot;
  public GameObject SpellCardsRoot;
  public GameObject ElementCardsRoot;
  public int PlayerIndex;

  public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
    var tilesRootEntity = conversionSystem.GetPrimaryEntity(TilesRoot);
    var spellCardsRootEntity = conversionSystem.GetPrimaryEntity(SpellCardsRoot);
    var elementCardsRootEntity = conversionSystem.GetPrimaryEntity(ElementCardsRoot);

    dstManager.AddSharedComponentData(entity, new PlayerIndex { 
      Value = PlayerIndex 
    });
    dstManager.AddComponentData(entity, new Hand {
      TilesRootEntity = tilesRootEntity,
      SpellCardsRootEntity = spellCardsRootEntity,
      ElementCardsRootEntity = elementCardsRootEntity 
    });
    dstManager.AddBuffer<TileEntry>(tilesRootEntity);
    dstManager.AddBuffer<SpellCardEntry>(spellCardsRootEntity);
    dstManager.AddBuffer<ElementCardEntry>(elementCardsRootEntity);
  }
}