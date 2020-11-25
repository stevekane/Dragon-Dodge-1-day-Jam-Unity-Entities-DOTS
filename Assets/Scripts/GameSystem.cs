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
  EntityQuery DragonQuery;
  EntityQuery WizardQuery;
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

  public static bool TryGetBoardTileIndex(
  EntityManager entityManager,
  Entity boardEntity,
  Entity tileEntity,
  out int boardTileIndex) {
    var boardTiles = entityManager.GetBuffer<BoardTileEntry>(boardEntity);

    for (var i = 0; i < boardTiles.Length; i++) {
      if (boardTiles[i].Entity == tileEntity) {
        boardTileIndex = i;
        return true;
      }
    }
    boardTileIndex = -1;
    return false;
  }

  // TODO: QUERIES
  // You should be able to check if a tile is on the board and where it is
  // You should be able to query for all empty spaces on the board
  // You should be able to query for all tiles nearby a specified tile
  // You should be able to query for all empty spaces nearby a specified tile

  // I'd like to query the board for empty positions, tiles, dragons, and wizards
  // There are several ways I can think of to do this:
  // Store separate buffers for each thing: tiles, dragons, wizards
  // Store the buffer as a dense 2d-array where entries pointing at Entity.Null contain nothing
  // Store a sparse buffer where each entry stores its own tilePosition
  // Queries that find no matches into the sparse buffer are therefore empty
  // Queries against the sparse data have two key weaknesses:
  //    They O(n)
  //    They allow multiple entries to share the same tilePosition even if that is illogical
  // Storing sparse data is simpler and takes less space
  //    All queries are just scanning a list for entries which match your search function
  //
  // After a sort of "gut check" I am leaning towards the following solution:
  //    There are things on the board. Those things are always a pair: TilePosition and Entity
  //    The board is just a list of these pairs
  //    In this way, no entity's data is polluted with board-only information (position)
  //    and queries are simply performed by scanning this array with some predicate. 
  //    I believe, overall, this is the simplest and most scalable solution

  public static void PlaceTilesOnBoard(    
  EntityManager entityManager,
  EntityQuery tileQuery,
  EntityQuery dragonQuery,
  EntityQuery wizardQuery,
  Entity boardSetupEntity,
  Entity boardEntity) {
    var tileEntities = tileQuery.ToEntityArray(Allocator.Temp);
    var dragonEntities = dragonQuery.ToEntityArray(Allocator.Temp);
    var wizardEntities = wizardQuery.ToEntityArray(Allocator.Temp);
    var boardConfiguration = entityManager.GetComponentData<BoardSetup>(boardSetupEntity);
    ref var boardTilePositions = ref boardConfiguration.Reference.Value.TilePositions;
    ref var dragonTilePositions = ref boardConfiguration.Reference.Value.DragonPositions;
    ref var player1WizardPositions = ref boardConfiguration.Reference.Value.Player1Positions;
    ref var player2WizardPositions = ref boardConfiguration.Reference.Value.Player2Positions;
    var boardTiles = entityManager.GetBuffer<BoardTileEntry>(boardEntity);
    var boardPieces = entityManager.GetBuffer<BoardPieceEntry>(boardEntity);
    var tileIndex = 0;
    var dragonIndex = 0;
    var player1Index = 0;
    var player2Index = 0;

    boardTiles.Length = boardTilePositions.Length;
    for (int i = 0; i < boardTilePositions.Length; i++) {
      boardTiles[i] = new BoardTileEntry { 
        BoardPosition = boardTilePositions[i], 
        CardinalRotation = CardinalRotation.North, 
        Entity = tileEntities[tileIndex] 
      };
      tileIndex++;
    }
    tileEntities.Dispose();
    dragonEntities.Dispose();
    wizardEntities.Dispose();
  }

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
    DragonQuery = EntityManager.CreateEntityQuery(typeof(Dragon));
    WizardQuery = EntityManager.CreateEntityQuery(typeof(Wizard));
    SpellCardQuery = EntityManager.CreateEntityQuery(typeof(SpellCard));
    ElementCardQuery = EntityManager.CreateEntityQuery(typeof(ElementCard));
    SceneSystem = World.GetExistingSystem<SceneSystem>();
    BuildPhysicsWorld = World.GetExistingSystem<BuildPhysicsWorld>();
    EntityManager.CreateEntity(typeof(Game));
    EntityManager.CreateEntity(typeof(ElementCardDeck), typeof(ElementCardEntry));
    EntityManager.CreateEntity(typeof(SpellCardDeck), typeof(SpellCardEntry));
    EntityManager.CreateEntity(typeof(Board), typeof(BoardPieceEntry), typeof(BoardTileEntry));
  }

  protected override void OnUpdate() {
    var sceneSystem = SceneSystem;
    var elementCardDeckEntity = GetSingletonEntity<ElementCardDeck>();
    var spellCardDeckEntity = GetSingletonEntity<SpellCardDeck>();
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
          var player1HandEntity = GetSingletonEntity<Player1>();
          var player2HandEntity = GetSingletonEntity<Player2>();
          var boardEntity = GetSingletonEntity<Board>();
          var boardSetupEntity = GetSingletonEntity<BoardSetup>();
          var activeHandEntity = game.CurrentTurnPlayerIndex % 2 == 0 ? player1HandEntity : player2HandEntity;

          PlaceTilesOnBoard(EntityManager, TileQuery, DragonQuery, WizardQuery, boardSetupEntity, boardEntity);
          ReturnCardsToDeck(EntityManager, player1HandEntity, player2HandEntity, spellCardDeckEntity, elementCardDeckEntity, SpellCardQuery, ElementCardQuery);
          ShuffleCardsInDeck(EntityManager, spellCardDeckEntity, elementCardDeckEntity, 1);
          DrawCardsForTurn(EntityManager, activeHandEntity, spellCardDeckEntity, elementCardDeckEntity);
          game.GameState = GameState.TakingTurn;
        }
        break;

        case GameState.TakingTurn: {
          var player1HandEntity = GetSingletonEntity<Player1>();
          var player2HandEntity = GetSingletonEntity<Player2>();
          var boardEntity = GetSingletonEntity<Board>();
          var boardSetupEntity = GetSingletonEntity<BoardSetup>();
          var activeHandEntity = game.CurrentTurnPlayerIndex % 2 == 0 ? player1HandEntity : player2HandEntity;

          switch (game.ActionState) {
            case ActionState.Base: {
              if (mouseDown && TryPick<SpellCard>(spellCardFromEntity, collisionWorld, screenRay, out RaycastHit raycastHit)) {
                var spellCardEntity = raycastHit.Entity;
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
                var activeHand = GetComponent<Hand>(activeHandEntity);
                var activeAction = GetComponent<Action>(activeHand.ActionEntity);

                if (TryGetBoardTileIndex(EntityManager, boardEntity, raycastHit.Entity, out int boardTileIndex)) {
                  Debug.Log($"You clicked a tile to rotate!");
                  activeAction.SelectedBoardTileIndex = boardTileIndex;
                  SetComponent<Action>(activeHand.ActionEntity, activeAction);
                  game.ActionState = ActionState.BoardTileToRotateSelected;
                }
              }
            }
            break;

            case ActionState.BoardTileToRotateSelected: {
              if (mouseDown && TryPick<Tile>(tileFromEntity, collisionWorld, screenRay, out RaycastHit raycastHit)) {
                var tileEntity = raycastHit.Entity;
                var activeHand = GetComponent<Hand>(activeHandEntity);
                var activeAction = GetComponent<Action>(activeHand.ActionEntity);

                Debug.Log($"You chose a cardinal rotation!");
                activeAction.SelectedCardinalRotation = CardinalRotation.East; // TODO: obviously should be an actual rotation value...
                SetComponent<Action>(activeHand.ActionEntity, activeAction);
                game.ActionState = ActionState.PlayingRotationAction;
              }
            }
            break;

            case ActionState.PlayingRotationAction: {
              var activeHand = GetComponent<Hand>(activeHandEntity);
              var activeAction = GetComponent<Action>(activeHand.ActionEntity);
              var boardTiles = GetBuffer<BoardTileEntry>(boardEntity);
              var selectedBoardTile = boardTiles[activeAction.SelectedBoardTileIndex];
              
              // TODO: remove selected card from hand
              // TODO: insert selected card into the deck
              // TODO: may be wise to flush the selected element cards buffer as well as part of wrapper method for resetting action?
              selectedBoardTile.CardinalRotation = CardinalRotation.East;
              boardTiles[activeAction.SelectedBoardTileIndex] = selectedBoardTile;
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