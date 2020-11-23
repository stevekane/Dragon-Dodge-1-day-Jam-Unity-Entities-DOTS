using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

public class RenderHandInstance : ISystemStateComponentData {
  public GameObject Instance;
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
public class HandRendererLifeCycleSYstem : SystemBase {
  protected override void OnUpdate() {
    var handRenderer = GameConfiguration.Instance.RenderGameObjects.RenderHand;

    Entities
    .WithName("Add_Render_Hand_Instance")
    .WithNone<RenderHandInstance>()
    .ForEach((Entity e, in Hand hand, in PlayerIndex playerIndex, in LocalToWorld localToWorld) => {
      var handRendererInstance = GameObject.Instantiate(handRenderer);

      handRendererInstance.transform.SetPositionAndRotation(localToWorld.Position, localToWorld.Rotation);
      EntityManager.AddComponentData(e, new RenderHandInstance { Instance = handRendererInstance });
    })
    .WithStructuralChanges()
    .WithoutBurst()
    .Run();

    Entities
    .WithName("Remove_Render_Hand_Instance")
    .WithNone<Hand, PlayerIndex, LocalToWorld>()
    .ForEach((Entity e, RenderHandInstance instance) => {
      GameObject.Destroy(instance.Instance);
      EntityManager.RemoveComponent<RenderHandInstance>(e);
    })
    .WithStructuralChanges()
    .WithoutBurst()
    .Run();
  }
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateAfter(typeof(HandRendererLifeCycleSYstem))]
public class HandRenderingSystem : SystemBase {
  EntityQuery TilesInHandQuery;
  EntityQuery ElementCardsInHandQuery;
  EntityQuery SpellCardsInHandQuery;

  protected override void OnCreate() {
    TilesInHandQuery = EntityManager.CreateEntityQuery(typeof(Tile), typeof(PlayerIndex));
    ElementCardsInHandQuery = EntityManager.CreateEntityQuery(typeof(ElementCard), typeof(PlayerIndex));
    SpellCardsInHandQuery = EntityManager.CreateEntityQuery(typeof(SpellCard), typeof(PlayerIndex));
  }

  // Hand rendering should be simple:
  //  Send commands to the hand renderer telling it what to draw
  //  For simplicity, make the hand renderer have an immediate-mode API
  //  RenderTiles(tiles)
  //  RenderElementalCards(elementalCards)
  //  RenderSpellCards(spellCards)
  // Need to also handle the states of cards
  //  Key this simple as well
  //  cards can be selected or not
  //  tiles can be selected or not
  protected override void OnUpdate() {
    if (Input.GetKeyDown(KeyCode.Space)) {
      // construct a card assigned to player 1
      Debug.Log("Space");
    }

    var player1Index = new PlayerIndex { Value = 0 };
    var player2Index = new PlayerIndex { Value = 1 };

    TilesInHandQuery.AddSharedComponentFilter(player1Index);
    ElementCardsInHandQuery.AddSharedComponentFilter(player1Index);
    SpellCardsInHandQuery.AddSharedComponentFilter(player1Index);

    // var player1Tiles = TilesInHandQuery.ToComponentDataArray<Tile>(Allocator.TempJob);
    // var player1ElementCards = ElementCardsInHandQuery.ToComponentDataArray<ElementCard>(Allocator.TempJob);
    // var player1SpellCards = SpellCardsInHandQuery.ToComponentDataArray<SpellCard>(Allocator.TempJob);

    TilesInHandQuery.ResetFilter();
    ElementCardsInHandQuery.ResetFilter();
    SpellCardsInHandQuery.ResetFilter();

    Entities
    .WithName("Render_Hand")
    .WithSharedComponentFilter(player1Index)
    .ForEach((Entity e, RenderHandInstance instance, in Hand hand) => {
      var renderHand = instance.Instance.GetComponent<RenderHand>();
      
      // Debug.Log($"Player1 has {player1Tiles.Length} Tiles. {player1ElementCards.Length} ElementCards. {player1SpellCards.Length} SpellCards.");
    })
    // .WithDisposeOnCompletion(player1Tiles)
    // .WithDisposeOnCompletion(player1ElementCards)
    // .WithDisposeOnCompletion(player1SpellCards)
    .WithoutBurst()
    .Run();

    /*
    var player2Tiles = TilesInHandQuery.ToComponentDataArray<Tile>(Allocator.TempJob);
    var player2ElementCards = ElementCardsInHandQuery.ToComponentDataArray<ElementCard>(Allocator.TempJob);
    var player2SpellCards = SpellCardsInHandQuery.ToComponentDataArray<SpellCard>(Allocator.TempJob);
    */
  }
}