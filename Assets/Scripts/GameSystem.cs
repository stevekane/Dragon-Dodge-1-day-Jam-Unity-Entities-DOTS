using Unity.Collections;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Entities;
using Unity.Jobs;
using Unity.Scenes;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Ray = UnityEngine.Ray;
using RaycastHit = Unity.Physics.RaycastHit;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
public class GameSystem : SystemBase {
  Camera MainCamera;

  EntityQuery LoadingSubSceneQuery;
  EntityQuery TileQuery;
  EntityQuery SpellCardQuery;
  EntityQuery ElementCardQuery;

  BuildPhysicsWorld BuildPhysicsWorld;
  SceneSystem SceneSystem;

  // TODO: I think you could do this with a custom collector to avoid allocating the raycastHits
  public static bool TryPick<T>(
  ComponentDataFromEntity<T> componentDataFromEntity,
  in CollisionWorld collisionWorld,
  in Ray ray,
  out RaycastHit raycastHit,
  in float maxDistance = 100f)
  where T : struct, IComponentData {
    var raycastInput = new RaycastInput { 
      Start = ray.origin, 
      End = ray.origin + ray.direction * maxDistance, 
      Filter = CollisionFilter.Default
    };
    var raycastHits = new NativeList<RaycastHit>(Allocator.Temp);

    if (collisionWorld.CastRay(raycastInput, ref raycastHits)) {
      for (var i = 0; i < raycastHits.Length; i++) {
        if (componentDataFromEntity.HasComponent(raycastHits[i].Entity)) {
          raycastHit = raycastHits[i];
          return true;
        }
      }
    }
    raycastHit = default(RaycastHit);
    return false;
  }

  // You should be able to check if a tile is on the board and where it is
  // You should be able to query for all empty spaces on the board
  // You should be able to query for all tiles nearby a specified tile
  // You should be able to query for all empty spaces nearby a specified tile

  public static void ReturnCardsToDeck(
  EntityManager entityManager,
  Entity player1HandEntity,
  Entity player2HandEntity,
  Entity spellCardDeckEntity,
  Entity elementCardDeckEntity,
  EntityQuery spellCardQuery,
  EntityQuery elementCardQuery) {
    var player1Hand = entityManager.GetComponentData<Hand>(player1HandEntity);
    var player2Hand = entityManager.GetComponentData<Hand>(player2HandEntity);
    var spellCards = spellCardQuery.ToEntityArray(Allocator.Temp);
    var elementCards = elementCardQuery.ToEntityArray(Allocator.Temp);

    entityManager.GetBuffer<SpellCardEntry>(player1Hand.SpellCardsRootEntity).Clear();
    entityManager.GetBuffer<SpellCardEntry>(player2Hand.SpellCardsRootEntity).Clear();
    entityManager.GetBuffer<SpellCardEntry>(spellCardDeckEntity).Clear();
    entityManager.GetBuffer<SpellCardEntry>(spellCardDeckEntity).Reinterpret<Entity>().CopyFrom(spellCards);

    entityManager.GetBuffer<ElementCardEntry>(player1Hand.ElementCardsRootEntity).Clear();
    entityManager.GetBuffer<ElementCardEntry>(player2Hand.ElementCardsRootEntity).Clear();
    entityManager.GetBuffer<ElementCardEntry>(elementCardDeckEntity).Clear();
    entityManager.GetBuffer<ElementCardEntry>(elementCardDeckEntity).Reinterpret<Entity>().CopyFrom(elementCards);
    spellCards.Dispose();
    elementCards.Dispose();
  }

  public static void ShuffleCardsInDeck(
  EntityManager entityManager,
  Entity spellCardDeckEntity,
  Entity elementCardDeckEntity,
  uint seed) {
    entityManager.GetBuffer<SpellCardEntry>(spellCardDeckEntity).Shuffle(seed);
    entityManager.GetBuffer<ElementCardEntry>(elementCardDeckEntity).Shuffle(seed);
  }

  public static bool TryDraw(
  DynamicBuffer<Entity> entities, 
  out Entity entity) {
    if (entities.Length == 0) {
      entity = Entity.Null;
      return false;
    } else {
      entity = entities[0];
      entities.RemoveAtSwapBack(0);
      return true;
    }
  }

  public static void DrawCardsForTurn(
  EntityManager entityManager,
  Entity handEntity,
  Entity spellCardDeckEntity,
  Entity elementCardDeckEntity) {
    const float CARD_OFFSET = .5f;

    var hand = entityManager.GetComponentData<Hand>(handEntity);
    var handSpellCardsEntity = hand.SpellCardsRootEntity;
    var handElementCardsEntity = hand.ElementCardsRootEntity;
    var handSpellCardsLocalToWorld = entityManager.GetComponentData<LocalToWorld>(handSpellCardsEntity);
    var handElementCardsLocalToWorld = entityManager.GetComponentData<LocalToWorld>(handElementCardsEntity);
    var spellCardDeckBuffer = entityManager.GetBuffer<SpellCardEntry>(spellCardDeckEntity).Reinterpret<Entity>();
    var elementCardDeckBuffer = entityManager.GetBuffer<ElementCardEntry>(elementCardDeckEntity).Reinterpret<Entity>();
    
    if (TryDraw(spellCardDeckBuffer, out Entity spellCardEntity)) {
      var handSpellCardsBuffer = entityManager.GetBuffer<SpellCardEntry>(handSpellCardsEntity);
      var index = handSpellCardsBuffer.Length;

      entityManager.SetComponentData(spellCardEntity, new Rotation { 
        Value = handSpellCardsLocalToWorld.Rotation 
      });
      entityManager.SetComponentData(spellCardEntity, new Translation { 
        Value = handSpellCardsLocalToWorld.Position + new float3(index * CARD_OFFSET, 0, 0) 
      });
      handSpellCardsBuffer.Reinterpret<Entity>().Add(spellCardEntity);
    }
    if (TryDraw(elementCardDeckBuffer, out Entity elementCardEntity)) {
      var handElementCardsBuffer = entityManager.GetBuffer<ElementCardEntry>(handElementCardsEntity);
      var index = handElementCardsBuffer.Length;

      entityManager.SetComponentData(elementCardEntity, new Rotation { 
        Value = handElementCardsLocalToWorld.Rotation 
      });
      entityManager.SetComponentData(elementCardEntity, new Translation { 
        Value = handElementCardsLocalToWorld.Position + new float3(index * CARD_OFFSET, 0, 0) 
      });
      handElementCardsBuffer.Reinterpret<Entity>().Add(elementCardEntity);
    }
  }

  public static bool AllScenesLoaded(
  EntityQuery LoadingSubScenesQuery,
  SceneSystem sceneSystem) {
    using (var sceneEntities = LoadingSubScenesQuery.ToEntityArray(Allocator.Temp)) {
      for (var i = 0; i < sceneEntities.Length; i++) {
        if (!sceneSystem.IsSceneLoaded(sceneEntities[i])) {
          return false;
        }
      }
      return true;
    }
  }

  protected override void OnCreate() {
    MainCamera = Camera.main;
    LoadingSubSceneQuery = EntityManager.CreateEntityQuery(typeof(SceneReference));
    TileQuery = EntityManager.CreateEntityQuery(typeof(Tile));
    SpellCardQuery = EntityManager.CreateEntityQuery(typeof(SpellCard));
    ElementCardQuery = EntityManager.CreateEntityQuery(typeof(ElementCard));
    SceneSystem = World.GetExistingSystem<SceneSystem>();
    BuildPhysicsWorld = World.GetExistingSystem<BuildPhysicsWorld>();
    EntityManager.CreateEntity(typeof(Game));
    EntityManager.CreateEntity(typeof(ElementCardDeck), typeof(ElementCardEntry));
    EntityManager.CreateEntity(typeof(SpellCardDeck), typeof(SpellCardEntry));
    RequireSingletonForUpdate<Player1>();
    RequireSingletonForUpdate<Player2>();
  }

  protected override void OnUpdate() {
    var sceneSystem = SceneSystem;
    var elementCardDeckEntity = GetSingletonEntity<ElementCardDeck>();
    var spellCardDeckEntity = GetSingletonEntity<SpellCardDeck>();
    var boardEntity = GetSingleton<Board>();
    var player1HandEntity = GetSingletonEntity<Player1>();
    var player2HandEntity = GetSingletonEntity<Player2>();
    var tileFromEntity = GetComponentDataFromEntity<Tile>(isReadOnly: true);
    var spellCardFromEntity = GetComponentDataFromEntity<SpellCard>(isReadOnly: true);
    var elementCardFromEntity = GetComponentDataFromEntity<SpellCard>(isReadOnly: true);
    var collisionWorld = BuildPhysicsWorld.PhysicsWorld.CollisionWorld;
    var mouseDown = Input.GetMouseButtonDown(0);
    var screenRay = MainCamera.ScreenPointToRay(Input.mousePosition);

    Entities
    .ForEach((ref Game game) => {
      switch (game.GameState) {
        case GameState.Loading: {
          if (AllScenesLoaded(LoadingSubSceneQuery, sceneSystem)) {
            game.GameState = GameState.Ready;
          }
        }
        break;

        case GameState.Ready: {
          var activeHandEntity = game.CurrentTurnPlayerIndex % 2 == 0 ? player1HandEntity : player2HandEntity;

          // There are a bunch of tiles that should be created when the game is started.
          // These tiles should have specific combinations of elements which are always the same
          // The board is then populated by placing these tiles into the buffer which represents
          // the 2-dimensional array containing all tiles and empty spaces
          // an empty space is denoted by an entry in this array that points at a Empty Tile entity
          // while a entry that is a tile will point to a Tile entity
          // PopulateBoard(EntityManager, TileQuery)
          ReturnCardsToDeck(EntityManager, player1HandEntity, player2HandEntity, spellCardDeckEntity, elementCardDeckEntity, SpellCardQuery, ElementCardQuery);
          ShuffleCardsInDeck(EntityManager, spellCardDeckEntity, elementCardDeckEntity, 1);
          DrawCardsForTurn(EntityManager, activeHandEntity, spellCardDeckEntity, elementCardDeckEntity);
          game.GameState = GameState.TakingTurn;
        }
        break;

        case GameState.TakingTurn: {
          switch (game.ActionState) {
            case ActionState.Base: {
              if (mouseDown && TryPick<SpellCard>(spellCardFromEntity, collisionWorld, screenRay, out RaycastHit raycastHit)) {
                var spellCardEntity = raycastHit.Entity;
                var activeHandEntity = game.CurrentTurnPlayerIndex % 2 == 0 ? player1HandEntity : player2HandEntity;
                var activeHand = GetComponent<Hand>(activeHandEntity);
                var activeAction = GetComponent<Action>(activeHand.ActionEntity);
                var spellCardEntitiesInHand = GetBuffer<SpellCardEntry>(activeHand.SpellCardsRootEntity).Reinterpret<Entity>();
                
                // TODO: you must check what type of card this actually is...
                if (spellCardEntitiesInHand.Contains(spellCardEntity)) {
                  Debug.Log("You clicked a spell card in your hand!");
                  activeAction.SelectedSpellCardEntity = spellCardEntity;
                  SetComponent<Action>(activeHand.ActionEntity, activeAction);
                  game.ActionState = ActionState.RotateCardSelected;
                }
              }
            }
            break;

            case ActionState.RotateCardSelected: {
              if (mouseDown && TryPick<Tile>(tileFromEntity, collisionWorld, screenRay, out RaycastHit raycastHit)) {
                var tileEntity = raycastHit.Entity;
                var activeHandEntity = game.CurrentTurnPlayerIndex % 2 == 0 ? player1HandEntity : player2HandEntity;
                var activeHand = GetComponent<Hand>(activeHandEntity);
                var activeAction = GetComponent<Action>(activeHand.ActionEntity);

                if (/*OnBoard(raycastHit.Entity) &&*/ true) {
                  Debug.Log($"You clicked a tile to rotate!");
                  activeAction.SelectedTileEntity = tileEntity;
                  SetComponent<Action>(activeHand.ActionEntity, activeAction);
                  game.ActionState = ActionState.BoardTileToRotateSelected;
                }
              }
            }
            break;

            case ActionState.BoardTileToRotateSelected: {
              if (mouseDown && TryPick<Tile>(tileFromEntity, collisionWorld, screenRay, out RaycastHit raycastHit)) {
                var tileEntity = raycastHit.Entity;
                var activeHandEntity = game.CurrentTurnPlayerIndex % 2 == 0 ? player1HandEntity : player2HandEntity;
                var activeHand = GetComponent<Hand>(activeHandEntity);
                var activeAction = GetComponent<Action>(activeHand.ActionEntity);

                if (activeAction.SelectedTileEntity == tileEntity) {
                  Debug.Log($"You chose a cardinal rotation!");
                  activeAction.SelectedCardinalRotation = CardinalRotation.East; // TODO: obviously should be an actual rotation value...
                  SetComponent<Action>(activeHand.ActionEntity, activeAction);
                  game.ActionState = ActionState.PlayingRotationAction;
                }
              }
            }
            break;

            case ActionState.PlayingRotationAction: {
              var activeHandEntity = game.CurrentTurnPlayerIndex % 2 == 0 ? player1HandEntity : player2HandEntity;
              var activeHand = GetComponent<Hand>(activeHandEntity);
              var activeAction = GetComponent<Action>(activeHand.ActionEntity);
              var tileRotation = GetComponent<TileRotation>(activeAction.SelectedTileEntity);

              // TODO: remove selected card from hand
              // TODO: insert selected card into the deck
              // TODO: may be wise to flush the selected element cards buffer as well as part of wrapper method for resetting action?
              SetComponent(activeAction.SelectedTileEntity, new TileRotation { Value = activeAction.SelectedCardinalRotation });
              SetComponent(activeHand.ActionEntity, default(Action));
              Debug.Log($"Playing the sick rotate!");
              game.ActionState = ActionState.Base;
            }
            break;
          }
        }
        break;

        case GameState.GameOver:
        break;
      }
    })
    .WithStructuralChanges()
    .WithoutBurst()
    .Run();
  }
}