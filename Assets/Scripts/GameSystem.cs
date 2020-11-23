using Unity.Collections;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Entities;
using Unity.Jobs;
using Unity.Scenes;
using UnityEngine;
using Ray = UnityEngine.Ray;
using RaycastHit = Unity.Physics.RaycastHit;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
public class GameSystem : SystemBase {
  Camera MainCamera;

  EntityQuery LoadingSubSceneQuery;
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
      for (var i = raycastHits.Length - 1; i >= 0; i--) {
        if (componentDataFromEntity.HasComponent(raycastHits[i].Entity)) {
          raycastHit = raycastHits[i];
          return true;
        }
      }
      raycastHit = default(RaycastHit);
      return false;
    } else {
      raycastHit = default(RaycastHit);
      return false;
    }
  }

  public static void ReturnCardsToDeck(
  EntityManager entityManager,
  Entity spellCardDeckEntity,
  Entity elementCardDeckEntity,
  EntityQuery spellCardQuery,
  EntityQuery elementCardQuery) {
    var spellCards = spellCardQuery.ToEntityArray(Allocator.Temp);
    var elementCards = elementCardQuery.ToEntityArray(Allocator.Temp);

    entityManager.RemoveComponent<PlayerIndex>(spellCards);
    entityManager.RemoveComponent<PlayerIndex>(elementCards);
    entityManager.GetBuffer<SpellCardEntry>(spellCardDeckEntity).Reinterpret<Entity>().CopyFrom(spellCards);
    entityManager.GetBuffer<ElementCardEntry>(elementCardDeckEntity).Reinterpret<Entity>().CopyFrom(elementCards);
    spellCards.Dispose();
    elementCards.Dispose();
  }

  public static void Shuffle<T>(
  DynamicBuffer<T> xs, 
  in uint seed) 
  where T : struct {  
    var rng = new Random(seed);
    var n = xs.Length;  

    while (n > 1) {
      n--;  
      int k = rng.NextInt(n + 1);  
      T value = xs[k];  
      xs[k] = xs[n];  
      xs[n] = value;  
    }  
  }

  public static void ShuffleCardsInDeck(
  EntityManager entityManager,
  Entity spellCardDeckEntity,
  Entity elementCardDeckEntity,
  uint seed) {
    Shuffle(entityManager.GetBuffer<SpellCardEntry>(spellCardDeckEntity), seed);
    Shuffle(entityManager.GetBuffer<ElementCardEntry>(elementCardDeckEntity), seed);
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
  Entity spellCardDeckEntity,
  Entity elementCardDeckEntity,
  int currentTurnPlayerIndex) {
    var playerIndex = new PlayerIndex { Value = currentTurnPlayerIndex };
    
    if (TryDraw(entityManager.GetBuffer<SpellCardEntry>(spellCardDeckEntity).Reinterpret<Entity>(), out Entity elementCardEntity)) {
      entityManager.AddSharedComponentData(elementCardEntity, playerIndex);
    }
    if (TryDraw(entityManager.GetBuffer<ElementCardEntry>(elementCardDeckEntity).Reinterpret<Entity>(), out Entity spellCardEntity)) {
      entityManager.AddSharedComponentData(spellCardEntity, playerIndex);
    }
  }

  public static bool AllScenesLoaded(NativeArray<Entity> sceneEntities, SceneSystem sceneSystem) {
    var allLoaded = true;

    for (var i = 0; i < sceneEntities.Length; i++) {
      allLoaded = allLoaded && sceneSystem.IsSceneLoaded(sceneEntities[i]);
    }
    return allLoaded;
  }

  protected override void OnCreate() {
    MainCamera = Camera.main;
    LoadingSubSceneQuery = EntityManager.CreateEntityQuery(typeof(SceneReference));
    SpellCardQuery = EntityManager.CreateEntityQuery(typeof(SpellCard));
    ElementCardQuery = EntityManager.CreateEntityQuery(typeof(ElementCard));
    SceneSystem = World.GetExistingSystem<SceneSystem>();
    BuildPhysicsWorld = World.GetExistingSystem<BuildPhysicsWorld>();
    EntityManager.CreateEntity(typeof(Game));
    EntityManager.CreateEntity(typeof(ElementCardDeck), typeof(ElementCardEntry));
    EntityManager.CreateEntity(typeof(SpellCardDeck), typeof(SpellCardEntry));
    RequireSingletonForUpdate<Game>();
    RequireSingletonForUpdate<SpellCardDeck>();
    RequireSingletonForUpdate<ElementCardDeck>();
  }

  protected override void OnUpdate() {
    var sceneSystem = SceneSystem;
    var elementCardDeckEntity = GetSingletonEntity<ElementCardDeck>();
    var spellCardDeckEntity = GetSingletonEntity<SpellCardDeck>();
    var tileFromEntity = GetComponentDataFromEntity<Tile>(isReadOnly: true);
    var collisionWorld = BuildPhysicsWorld.PhysicsWorld.CollisionWorld;
    var mouseDown = Input.GetMouseButtonDown(0);
    var screenRay = MainCamera.ScreenPointToRay(Input.mousePosition);

    Entities
    .ForEach((ref Game game) => {
      switch (game.GameState) {
        case GameState.Loading: {
          var loadingSubSceneEntities = LoadingSubSceneQuery.ToEntityArray(Allocator.Temp);

          if (AllScenesLoaded(loadingSubSceneEntities, sceneSystem)) {
            game.GameState = GameState.Ready;
          }
          loadingSubSceneEntities.Dispose();
        }
        break;

        case GameState.Ready: {
          ReturnCardsToDeck(EntityManager, spellCardDeckEntity, elementCardDeckEntity, SpellCardQuery, ElementCardQuery);
          ShuffleCardsInDeck(EntityManager, spellCardDeckEntity, elementCardDeckEntity, 1);
          DrawCardsForTurn(EntityManager, spellCardDeckEntity, elementCardDeckEntity, game.CurrentTurnPlayerIndex);
          // When a player draws cards, they need to actually store them in their hand in some order
          // The cards need to have a physical position that is derived from the location of the hands
          // in world space
          // This means I need to create singletons for PlayerXSpellCards, PlayerXElementCards, PlayerXTiles, PlayerX Pass Button
          // When a player draws a card, it should get added to their hand AND placed in physical space according to its
          // order in the hand
          game.GameState = GameState.TakingTurn;
        }
        break;

        case GameState.TakingTurn: {
          if (mouseDown && TryPick(tileFromEntity, collisionWorld, screenRay, out RaycastHit hit)) {
            game.CurrentTurnPlayerIndex = game.CurrentTurnPlayerIndex == 0 ? 1 : 0;
            DrawCardsForTurn(EntityManager, spellCardDeckEntity, elementCardDeckEntity, game.CurrentTurnPlayerIndex);
            game.GameState = GameState.TakingTurn;
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