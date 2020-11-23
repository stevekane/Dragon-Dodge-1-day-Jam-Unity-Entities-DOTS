using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Scenes;
using Unity.Mathematics;

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
          // Restore all element cards to the deck and shuffle
          {
            var shuffleRandomSeed = ((uint)Time.ElapsedTime % 42) + 1;
            var elementCardDeckEntity = GetSingletonEntity<ElementCardDeck>();
            var elementCardEntities = SpellCardQuery.ToEntityArray(Allocator.Temp);

            EntityManager.RemoveComponent<PlayerIndex>(elementCardEntities);
            Shuffle(elementCardEntities, shuffleRandomSeed);
            EntityManager.GetBuffer<ElementCardDeckEntry>(elementCardDeckEntity).Clear();
            EntityManager.GetBuffer<ElementCardDeckEntry>(elementCardDeckEntity).Reinterpret<Entity>().CopyFrom(elementCardEntities);
            elementCardEntities.Dispose();
          }

          // Restore all spell cards to the deck and shuffle
          {
            var shuffleRandomSeed = ((uint)Time.ElapsedTime % 24) + 1;
            var spellCardDeckEntity = GetSingletonEntity<SpellCardDeck>();
            var spellCardEntities = SpellCardQuery.ToEntityArray(Allocator.Temp);

            EntityManager.RemoveComponent<PlayerIndex>(spellCardEntities);
            UnityEngine.Debug.Log($"Before {spellCardEntities[0]}");
            Shuffle(spellCardEntities, shuffleRandomSeed);
            UnityEngine.Debug.Log($"After {spellCardEntities[0]}");
            EntityManager.GetBuffer<SpellCardDeckEntry>(spellCardDeckEntity).Clear();
            EntityManager.GetBuffer<SpellCardDeckEntry>(spellCardDeckEntity).Reinterpret<Entity>().CopyFrom(spellCardEntities);
            spellCardEntities.Dispose();
          }

          // TODO: destroy / reload tiles
          // TODO: destroy / reload dragons 
          // TODO: destroy / reload wizards
          game.GameState = GameState.SelectingAction;
        }
        break;

        case GameState.SelectingAction:
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