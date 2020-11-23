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

// one way to do this: use some kind of "event" that both the data and visual layers can react to
// another way to do this: use some kind of polling to detect changes in the data and reflect them in the visuals
// another way to do this: brute force. delete everything on every frame and re-create visuals based on the data
// 
// Let's start with brute-force although... the more I think about it the more the event-method sounds most sane
// It also allows us to replay games and might be much, much simpler
[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateAfter(typeof(HandRendererLifeCycleSYstem))]
public class HandRenderingSystem : SystemBase {
  EntityQuery TilesInHandQuery;
  EntityQuery ElementCardsInHandQuery;
  EntityQuery SpellCardsInHandQuery;

  public static void RenderHand(
  RenderGameObjects renderGameObjects,
  RenderHand renderHand, 
  NativeArray<Tile> tiles, 
  NativeArray<ElementCard> elementCards, 
  NativeArray<SpellCard> spellCards) {
    RenderTiles(renderGameObjects, renderHand, tiles);
    RenderElementCards(renderGameObjects, renderHand, elementCards);
    RenderSpellCards(renderGameObjects, renderHand, spellCards);
  }

  public static void RenderTiles(
  RenderGameObjects renderGameObjects,
  RenderHand renderHand,
  NativeArray<Tile> tiles) {
    for (var i = 0; i < renderHand.Tiles.Count; i++) {
      GameObject.Destroy(renderHand.Tiles[i].gameObject);
    }
    renderHand.Tiles.Clear();

    for (var i = 0; i < tiles.Length; i++) {
      var renderTile = HandRenderTileForTile(renderGameObjects, tiles[i]);

      renderHand.Tiles.Add(renderTile);
      renderHand.transform.SetParent(renderHand.TilesTransform);
      renderTile.transform.localPosition = new Vector3(renderHand.TileSpacing.x * i, renderHand.TileSpacing.y * (i % 6), 0);
    }
  }

  public static void RenderElementCards(
  RenderGameObjects renderGameObjects,
  RenderHand renderHand,
  NativeArray<ElementCard> elementCards) {
    for (var i = 0; i < renderHand.ElementCards.Count; i++) {
      GameObject.Destroy(renderHand.ElementCards[i].gameObject);
    }
    renderHand.ElementCards.Clear();

    for (var i = 0; i < elementCards.Length; i++) {
      var renderElementCard = HandRenderElementCardForElement(renderGameObjects, elementCards[i].Element);

      renderHand.ElementCards.Add(renderElementCard);
      renderElementCard.transform.SetParent(renderHand.ElementCardsTransform);
      renderElementCard.transform.localPosition = new Vector3(renderHand.CardSpacing.x * i, 0, 0);
    }
  }

  public static void RenderSpellCards(
  RenderGameObjects renderGameObjects,
  RenderHand renderHand,
  NativeArray<SpellCard> spellCards) {
    for (var i = 0; i < renderHand.SpellCards.Count; i++) {
      GameObject.Destroy(renderHand.SpellCards[i].gameObject);
    }
    renderHand.SpellCards.Clear();

    for (var i = 0; i < spellCards.Length; i++) {
      var renderSpellCard = HandRenderSpellCardForSpell(renderGameObjects, spellCards[i].Spell);

      renderHand.SpellCards.Add(renderSpellCard);
      renderSpellCard.transform.SetParent(renderHand.SpellCardsTransform);
      renderSpellCard.transform.localPosition = new Vector3(renderHand.CardSpacing.x * i, 0, 0);
    }
  }

  public static RenderTile HandRenderTileForTile(
  RenderGameObjects renderGameObjects, 
  in Tile tile) {
    var renderTile = GameObject.Instantiate(renderGameObjects.HandRenderTile);

    renderTile.GetComponent<RenderTile>().SetElementalMaterials(renderGameObjects, tile);
    return renderGameObjects.HandRenderTile;
  }

  public static HandRenderElementCard HandRenderElementCardForElement(
  RenderGameObjects renderGameObjects,
  in Element element) {
    switch (element) {
      case Element.Earth: return HandRenderElementCard.Instantiate(renderGameObjects.HandRenderElementCardEarth);
      case Element.Fire:  return HandRenderElementCard.Instantiate(renderGameObjects.HandRenderElementCardFire);
      case Element.Wind:  return HandRenderElementCard.Instantiate(renderGameObjects.HandRenderElementCardWind);
      case Element.Water: return HandRenderElementCard.Instantiate(renderGameObjects.HandRenderElementCardWater);
      default:            return HandRenderElementCard.Instantiate(renderGameObjects.HandRenderElementCardUnknown);
    }
  }

  public static HandRenderSpellCard HandRenderSpellCardForSpell(
  RenderGameObjects renderGameObjects,
  in Spell spell) {
    switch (spell) {
      case Spell.Move:   return HandRenderSpellCard.Instantiate(renderGameObjects.HandRenderSpellCardMove);
      case Spell.Place:  return HandRenderSpellCard.Instantiate(renderGameObjects.HandRenderSpellCardPlace);
      case Spell.Rotate: return HandRenderSpellCard.Instantiate(renderGameObjects.HandRenderSpellCardRotate);
      default:           return HandRenderSpellCard.Instantiate(renderGameObjects.HandRenderSpellCardUnknown);
    }
  }

  protected override void OnCreate() {
    TilesInHandQuery = EntityManager.CreateEntityQuery(typeof(Tile), typeof(PlayerIndex));
    ElementCardsInHandQuery = EntityManager.CreateEntityQuery(typeof(ElementCard), typeof(PlayerIndex));
    SpellCardsInHandQuery = EntityManager.CreateEntityQuery(typeof(SpellCard), typeof(PlayerIndex));
  }

  protected override void OnUpdate() {
    var renderGameObjects = GameConfiguration.Instance.RenderGameObjects;
    var player1Index = new PlayerIndex { Value = 0 };

    TilesInHandQuery.AddSharedComponentFilter(player1Index);
    ElementCardsInHandQuery.AddSharedComponentFilter(player1Index);
    SpellCardsInHandQuery.AddSharedComponentFilter(player1Index);

    var player1Tiles = TilesInHandQuery.ToComponentDataArray<Tile>(Allocator.TempJob);
    var player1ElementCards = ElementCardsInHandQuery.ToComponentDataArray<ElementCard>(Allocator.TempJob);
    var player1SpellCards = SpellCardsInHandQuery.ToComponentDataArray<SpellCard>(Allocator.TempJob);

    TilesInHandQuery.ResetFilter();
    ElementCardsInHandQuery.ResetFilter();
    SpellCardsInHandQuery.ResetFilter();

    Entities
    .WithName("Render_Player1_Hand")
    .WithSharedComponentFilter(player1Index)
    .ForEach((Entity e, RenderHandInstance instance, in Hand hand) => {
      var renderHand = instance.Instance.GetComponent<RenderHand>();
      
      RenderHand(renderGameObjects, renderHand, player1Tiles, player1ElementCards, player1SpellCards);
    })
    .WithDisposeOnCompletion(player1Tiles)
    .WithDisposeOnCompletion(player1ElementCards)
    .WithDisposeOnCompletion(player1SpellCards)
    .WithoutBurst()
    .Run();

    var player2Index = new PlayerIndex { Value = 1 };

    TilesInHandQuery.AddSharedComponentFilter(player2Index);
    ElementCardsInHandQuery.AddSharedComponentFilter(player2Index);
    SpellCardsInHandQuery.AddSharedComponentFilter(player2Index);

    var player2Tiles = TilesInHandQuery.ToComponentDataArray<Tile>(Allocator.TempJob);
    var player2ElementCards = ElementCardsInHandQuery.ToComponentDataArray<ElementCard>(Allocator.TempJob);
    var player2SpellCards = SpellCardsInHandQuery.ToComponentDataArray<SpellCard>(Allocator.TempJob);

    TilesInHandQuery.ResetFilter();
    ElementCardsInHandQuery.ResetFilter();
    SpellCardsInHandQuery.ResetFilter();

    Entities
    .WithName("Render_Player2_Hand")
    .WithSharedComponentFilter(player2Index)
    .ForEach((Entity e, RenderHandInstance instance, in Hand hand) => {
      var renderHand = instance.Instance.GetComponent<RenderHand>();
      
      RenderHand(renderGameObjects, renderHand, player2Tiles, player2ElementCards, player2SpellCards);
    })
    .WithDisposeOnCompletion(player2Tiles)
    .WithDisposeOnCompletion(player2ElementCards)
    .WithDisposeOnCompletion(player2SpellCards)
    .WithoutBurst()
    .Run();
  }
}