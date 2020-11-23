using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Scenes;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class GameSystem : SystemBase {
  EntityQuery LoadingSubSceneQuery;
  EntityQuery SpellCardQuery;
  EntityQuery ElementCardQuery;

  SceneSystem SceneSystem;

  public static void Shuffle<T>(NativeArray<T> xs, in uint seed) where T : struct {  
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

  public static bool TryDraw(DynamicBuffer<Entity> entities, out Entity entity) {
    if (entities.Length == 0) {
      entity = Entity.Null;
      return false;
    }

    entity = entities[0];
    entities.RemoveAtSwapBack(0);
    return entity != Entity.Null;
  }

  public static bool AllScenesLoaded(NativeArray<Entity> sceneEntities, SceneSystem sceneSystem) {
    var allLoaded = true;

    for (var i = 0; i < sceneEntities.Length; i++) {
      allLoaded = allLoaded && sceneSystem.IsSceneLoaded(sceneEntities[i]);
    }
    return allLoaded;
  }

  protected override void OnCreate() {
    LoadingSubSceneQuery = EntityManager.CreateEntityQuery(typeof(SceneReference));
    SpellCardQuery = EntityManager.CreateEntityQuery(typeof(SpellCard));
    ElementCardQuery = EntityManager.CreateEntityQuery(typeof(ElementCard));
    SceneSystem = World.GetExistingSystem<SceneSystem>();
    EntityManager.CreateEntity(typeof(Game));
    EntityManager.CreateEntity(typeof(ElementCardDeck), typeof(ElementCardDeckEntry));
    EntityManager.CreateEntity(typeof(SpellCardDeck), typeof(SpellCardDeckEntry));
    RequireSingletonForUpdate<Game>();
    RequireSingletonForUpdate<SpellCardDeck>();
    RequireSingletonForUpdate<ElementCardDeck>();
  }

  protected override void OnUpdate() {
    var sceneSystem = SceneSystem;
    var spaceDown = Input.GetKeyDown(KeyCode.Space);

    Entities
    .ForEach((ref Game game) => {
      switch (game.GameState) {
        case GameState.Loading: {
          var loadingSubSceneEntities = LoadingSubSceneQuery.ToEntityArray(Allocator.Temp);

          if (AllScenesLoaded(loadingSubSceneEntities, sceneSystem)) {
            UnityEngine.Debug.Log($"Finished loading initial SubScenes");
            game.GameState = GameState.Ready;
          }
          loadingSubSceneEntities.Dispose();
        }
        break;

        case GameState.Ready: {
          // Restore all element cards to the deck and shuffle
          var elementCardDeckEntity = GetSingletonEntity<ElementCardDeck>();
          var elementCardEntities = ElementCardQuery.ToEntityArray(Allocator.Temp);

          UnityEngine.Debug.Log($"Shuffled {elementCardEntities.Length} Element Cards");
          EntityManager.RemoveComponent<PlayerIndex>(elementCardEntities);
          Shuffle(elementCardEntities, (uint)Time.ElapsedTime + 1);
          EntityManager.GetBuffer<ElementCardDeckEntry>(elementCardDeckEntity).Clear();
          EntityManager.GetBuffer<ElementCardDeckEntry>(elementCardDeckEntity).Reinterpret<Entity>().CopyFrom(elementCardEntities);
          elementCardEntities.Dispose();

          // Restore all spell cards to the deck and shuffle
          var spellCardDeckEntity = GetSingletonEntity<SpellCardDeck>();
          var spellCardEntities = SpellCardQuery.ToEntityArray(Allocator.Temp);

          UnityEngine.Debug.Log($"Shuffled {spellCardEntities.Length} Spell Cards");
          EntityManager.RemoveComponent<PlayerIndex>(spellCardEntities);
          Shuffle(spellCardEntities, (uint)Time.ElapsedTime + 5);
          EntityManager.GetBuffer<SpellCardDeckEntry>(spellCardDeckEntity).Clear();
          EntityManager.GetBuffer<SpellCardDeckEntry>(spellCardDeckEntity).Reinterpret<Entity>().CopyFrom(spellCardEntities);
          spellCardEntities.Dispose();

          // TODO: destroy / reload tiles
          // TODO: destroy / reload dragons 
          // TODO: destroy / reload wizards

          // Try to deal one of each type of card to the first player
          if (TryDraw(EntityManager.GetBuffer<ElementCardDeckEntry>(elementCardDeckEntity).Reinterpret<Entity>(), out Entity elementCardEntity)) {
            var playerIndex = new PlayerIndex { Value = game.CurrentTurnPlayerIndex };
            var element = EntityManager.GetComponentData<ElementCard>(elementCardEntity).Element;

            UnityEngine.Debug.Log($"Dealt ElementCard {element} to Player {playerIndex.Value}.");
            EntityManager.AddSharedComponentData(elementCardEntity, playerIndex);
          }
          if (TryDraw(EntityManager.GetBuffer<SpellCardDeckEntry>(spellCardDeckEntity).Reinterpret<Entity>(), out Entity spellCardEntity)) {
            var playerIndex = new PlayerIndex { Value = game.CurrentTurnPlayerIndex };
            var spell = EntityManager.GetComponentData<SpellCard>(spellCardEntity).Spell;

            UnityEngine.Debug.Log($"Dealt SpellCard {spell} to Player {playerIndex.Value}.");
            EntityManager.AddSharedComponentData(spellCardEntity, playerIndex);
          }

          // Begin first player's turn
          game.GameState = GameState.SelectingAction;
        }
        break;

        case GameState.SelectingAction: {
          var elementCardDeckEntity = GetSingletonEntity<ElementCardDeck>();
          var spellCardDeckEntity = GetSingletonEntity<SpellCardDeck>();

          if (spaceDown) {
            game.CurrentTurnPlayerIndex = game.CurrentTurnPlayerIndex == 0 ? 1 : 0;
            if (TryDraw(EntityManager.GetBuffer<ElementCardDeckEntry>(elementCardDeckEntity).Reinterpret<Entity>(), out Entity elementCardEntity)) {
              var playerIndex = new PlayerIndex { Value = game.CurrentTurnPlayerIndex };
              var element = EntityManager.GetComponentData<ElementCard>(elementCardEntity).Element;

              UnityEngine.Debug.Log($"Dealt ElementCard {element} to Player {playerIndex.Value}.");
              EntityManager.AddSharedComponentData(elementCardEntity, playerIndex);
            }
            if (TryDraw(EntityManager.GetBuffer<SpellCardDeckEntry>(spellCardDeckEntity).Reinterpret<Entity>(), out Entity spellCardEntity)) {
              var playerIndex = new PlayerIndex { Value = game.CurrentTurnPlayerIndex };
              var spell = EntityManager.GetComponentData<SpellCard>(spellCardEntity).Spell;

              UnityEngine.Debug.Log($"Dealt SpellCard {spell} to Player {playerIndex.Value}.");
              EntityManager.AddSharedComponentData(spellCardEntity, playerIndex);
            }
            // Begin next player's turn
            game.GameState = GameState.SelectingAction;
          }
        }
        break;

        case GameState.PlayingAction:
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