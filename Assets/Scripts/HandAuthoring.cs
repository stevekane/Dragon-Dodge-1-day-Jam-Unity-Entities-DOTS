using Unity.Entities;
using UnityEngine;

public class HandAuthoring : MonoBehaviour, IConvertGameObjectToEntity {
  public enum Player { Player1, Player2 }

  public GameObject TilesRoot;
  public GameObject SpellCardsRoot;
  public GameObject ElementCardsRoot;
  public GameObject ActionRoot;
  public Player OwningPlayer;

  public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
    var tilesRootEntity = conversionSystem.GetPrimaryEntity(TilesRoot);
    var spellCardsRootEntity = conversionSystem.GetPrimaryEntity(SpellCardsRoot);
    var elementCardsRootEntity = conversionSystem.GetPrimaryEntity(ElementCardsRoot);
    var actionEntity = conversionSystem.GetPrimaryEntity(ActionRoot);

    // Identity the player for each hand
    if (OwningPlayer == Player.Player1) {
      dstManager.AddComponent<Player1>(entity);
    } else {
      dstManager.AddComponent<Player2>(entity);
    }

    // Add buffers storing tiles and cards
    dstManager.AddComponentData(entity, new Hand {
      TilesRootEntity = tilesRootEntity,
      SpellCardsRootEntity = spellCardsRootEntity,
      ElementCardsRootEntity = elementCardsRootEntity,
      ActionEntity = actionEntity
    });
    dstManager.AddBuffer<TileEntry>(tilesRootEntity);
    dstManager.AddBuffer<SpellCardEntry>(spellCardsRootEntity);
    dstManager.AddBuffer<ElementCardEntry>(elementCardsRootEntity);

    // Add action entity storing the current state of a given action
    dstManager.AddComponentData(actionEntity, Action.Default());

    // Add buffer to the actionEntity to store a list of selected element cards
    dstManager.AddBuffer<ElementCardEntry>(actionEntity);
  }
}