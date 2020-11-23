using Unity.Entities;
using UnityEngine;

public class HandAuthoring : MonoBehaviour, IConvertGameObjectToEntity {
  public enum Player { Player1, Player2 }

  public GameObject TilesRoot;
  public GameObject SpellCardsRoot;
  public GameObject ElementCardsRoot;
  public Player OwningPlayer;

  public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
    var tilesRootEntity = conversionSystem.GetPrimaryEntity(TilesRoot);
    var spellCardsRootEntity = conversionSystem.GetPrimaryEntity(SpellCardsRoot);
    var elementCardsRootEntity = conversionSystem.GetPrimaryEntity(ElementCardsRoot);

    if (OwningPlayer == Player.Player1) {
      dstManager.AddComponent<Player1>(entity);
    } else {
      dstManager.AddComponent<Player2>(entity);
    }
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