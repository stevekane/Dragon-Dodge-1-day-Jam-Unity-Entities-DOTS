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
  Entity handEntity,
  Entity spellCardDeckEntity,
  Entity elementCardDeckEntity) {
    var hand = entityManager.GetComponentData<Hand>(handEntity);
    var handSpellCardsEntity = hand.SpellCardsRootEntity;
    var handElementCardsEntity = hand.ElementCardsRootEntity;
    var handSpellCardsLocalToWorld = entityManager.GetComponentData<LocalToWorld>(handSpellCardsEntity);
    var handElementCardsLocalToWorld = entityManager.GetComponentData<LocalToWorld>(handSpellCardsEntity);
    
    if (TryDraw(entityManager.GetBuffer<SpellCardEntry>(spellCardDeckEntity).Reinterpret<Entity>(), out Entity spellCardEntity)) {
      var handSpellCardsBuffer = entityManager.GetBuffer<SpellCardEntry>(handSpellCardsEntity);
      var index = handSpellCardsBuffer.Length;
      var position = handSpellCardsLocalToWorld.Position + new float3(index * .5f, 0, 0);

      entityManager.SetComponentData(spellCardEntity, new Translation { Value = position });
      handSpellCardsBuffer.Reinterpret<Entity>().Add(spellCardDeckEntity);
    }
    if (TryDraw(entityManager.GetBuffer<ElementCardEntry>(elementCardDeckEntity).Reinterpret<Entity>(), out Entity elementCardEntity)) {
      var handElementCardsBuffer = entityManager.GetBuffer<ElementCardEntry>(handElementCardsEntity);
      var index = handElementCardsBuffer.Length;
      var position = handElementCardsLocalToWorld.Position + new float3(index * .5f, 0, 0);

      entityManager.SetComponentData(elementCardEntity, new Translation { Value = position });
      handElementCardsBuffer.Reinterpret<Entity>().Add(elementCardDeckEntity);
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
    var player1HandEntity = GetSingletonEntity<Player1>();
    var player2HandEntity = GetSingletonEntity<Player2>();
    var tileFromEntity = GetComponentDataFromEntity<Tile>(isReadOnly: true);
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

          ReturnCardsToDeck(EntityManager, player1HandEntity, player2HandEntity, spellCardDeckEntity, elementCardDeckEntity, SpellCardQuery, ElementCardQuery);
          ShuffleCardsInDeck(EntityManager, spellCardDeckEntity, elementCardDeckEntity, 1);
          DrawCardsForTurn(EntityManager, activeHandEntity, spellCardDeckEntity, elementCardDeckEntity);
          game.GameState = GameState.TakingTurn;
        }
        break;

        case GameState.TakingTurn: {
          if (mouseDown && TryPick(tileFromEntity, collisionWorld, screenRay, out RaycastHit hit)) {
            var activeHandEntity = (game.CurrentTurnPlayerIndex + 1) % 2 == 0 ? player1HandEntity : player2HandEntity;

            game.CurrentTurnPlayerIndex = game.CurrentTurnPlayerIndex == 0 ? 1 : 0;
            DrawCardsForTurn(EntityManager, activeHandEntity, spellCardDeckEntity, elementCardDeckEntity);
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