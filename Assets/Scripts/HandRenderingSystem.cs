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
    .ForEach((Entity e, in Hand hand, in LocalToWorld localToWorld) => {
      var handRendererInstance = GameObject.Instantiate(handRenderer);

      handRendererInstance.transform.SetPositionAndRotation(localToWorld.Position, localToWorld.Rotation);
      EntityManager.AddComponentData(e, new RenderHandInstance { Instance = handRendererInstance });
    })
    .WithStructuralChanges()
    .WithoutBurst()
    .Run();

    Entities
    .WithName("Remove_Render_Hand_Instance")
    .WithNone<Hand, LocalToWorld>()
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
  EntityManager entityManager,
  RenderGameObjects renderGameObjects,
  RenderHand renderHand,
  DynamicBuffer<Entity> elementCardEntities) {
    for (var i = 0; i < renderHand.ElementCards.Count; i++) {
      GameObject.Destroy(renderHand.ElementCards[i].gameObject);
    }
    renderHand.ElementCards.Clear();

    for (var i = 0; i < elementCardEntities.Length; i++) {
      var localToWorld = entityManager.GetComponentData<LocalToWorld>(elementCardEntities[i]);
      var elementCard = entityManager.GetComponentData<ElementCard>(elementCardEntities[i]);
      var renderElementCard = HandRenderElementCardForElement(renderGameObjects, elementCard.Element);

      renderHand.ElementCards.Add(renderElementCard);
      renderElementCard.transform.SetPositionAndRotation(localToWorld.Position, localToWorld.Rotation);
    }
  }

  public static void RenderSpellCards(
  EntityManager entityManager,
  RenderGameObjects renderGameObjects,
  RenderHand renderHand,
  DynamicBuffer<Entity> spellCardEntities) {
    for (var i = 0; i < renderHand.SpellCards.Count; i++) {
      GameObject.Destroy(renderHand.SpellCards[i].gameObject);
    }
    renderHand.SpellCards.Clear();

    for (var i = 0; i < spellCardEntities.Length; i++) {
      var localToWorld = entityManager.GetComponentData<LocalToWorld>(spellCardEntities[i]);
      var spellCard = entityManager.GetComponentData<SpellCard>(spellCardEntities[i]);
      var renderSpellCard = HandRenderSpellCardForSpell(renderGameObjects, spellCard.Spell);

      renderHand.SpellCards.Add(renderSpellCard);
      renderSpellCard.transform.SetPositionAndRotation(localToWorld.Position, localToWorld.Rotation);
    }
  }

  public static void RenderHand(
  EntityManager entityManager,
  RenderGameObjects renderGameObjects,
  RenderHand renderHand, 
  in Hand hand) {
    var spellCardEntities = entityManager.GetBuffer<SpellCardEntry>(hand.SpellCardsRootEntity).Reinterpret<Entity>();
    var elementCardEntities = entityManager.GetBuffer<ElementCardEntry>(hand.ElementCardsRootEntity).Reinterpret<Entity>();
    var tileEntities = entityManager.GetBuffer<TileEntry>(hand.TilesRootEntity).Reinterpret<Entity>();

    // RenderTiles(renderGameObjects, renderHand, tiles);
    RenderElementCards(entityManager, renderGameObjects, renderHand, elementCardEntities);
    RenderSpellCards(entityManager, renderGameObjects, renderHand, spellCardEntities);
  }

  protected override void OnUpdate() {
    var renderGameObjects = GameConfiguration.Instance.RenderGameObjects;

    Entities
    .WithName("Render_Player1_Hand")
    .WithAll<Player1>()
    .ForEach((Entity e, RenderHandInstance instance, in Hand hand) => {
      RenderHand(EntityManager, renderGameObjects, instance.Instance.GetComponent<RenderHand>(), hand);
    })
    .WithoutBurst()
    .Run();

    Entities
    .WithName("Render_Player2_Hand")
    .WithAll<Player2>()
    .ForEach((Entity e, RenderHandInstance instance, in Hand hand) => {
      RenderHand(EntityManager, renderGameObjects, instance.Instance.GetComponent<RenderHand>(), hand);
    })
    .WithoutBurst()
    .Run();
  }
}