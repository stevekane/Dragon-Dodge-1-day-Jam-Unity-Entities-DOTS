using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

public class ManagedRenderer : ISystemStateComponentData {
  public GameObject Value;
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
public class BoardRendererLifeCycleSystem : SystemBase {
  protected override void OnUpdate() {
    var gameConfiguration = GameConfiguration.Instance;
    var renderGameObjects = gameConfiguration.RenderGameObjects; 

    Entities
    .WithNone<ManagedRenderer>()
    .WithAll<Tile>()
    .ForEach((Entity e) => {
      EntityManager.AddComponentData(e, new ManagedRenderer { 
        Value = GameObject.Instantiate(renderGameObjects.RenderBoardTile) 
      });
    })
    .WithStructuralChanges()
    .WithoutBurst()
    .Run();

    Entities
    .WithNone<ManagedRenderer>()
    .WithAll<Wizard>()
    .ForEach((Entity e) => {
      EntityManager.AddComponentData(e, new ManagedRenderer { 
        Value = GameObject.Instantiate(renderGameObjects.RenderBoardWizard) 
      });
    })
    .WithStructuralChanges()
    .WithoutBurst()
    .Run();

    Entities
    .WithNone<ManagedRenderer>()
    .WithAll<Dragon>()
    .ForEach((Entity e) => {
      EntityManager.AddComponentData(e, new ManagedRenderer { 
        Value = GameObject.Instantiate(renderGameObjects.RenderBoardDragon) 
      });
    })
    .WithStructuralChanges()
    .WithoutBurst()
    .Run();

    Entities
    .WithAll<ManagedRenderer>()
    .WithNone<Tile, Wizard, Dragon>()
    .ForEach((Entity e, ManagedRenderer renderer) => {
      GameObject.Destroy(renderer.Value);
      EntityManager.RemoveComponent<ManagedRenderer>(e);
    })
    .WithStructuralChanges()
    .WithoutBurst()
    .Run();
  }
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateAfter(typeof(BoardRendererLifeCycleSystem))]
public class BoardRenderingSystem : SystemBase {
  public static void RenderBoardTiles(
  EntityManager entityManager,
  Entity boardEntity,
  RenderGameObjects renderGameObjects) {
    var tileBuffer = entityManager.GetBuffer<BoardTileEntry>(boardEntity);

    for (int i = 0; i < tileBuffer.Length; i++) {
      var boardTile = tileBuffer[i];
      var tile = entityManager.GetComponentData<Tile>(boardTile.Entity);
      var renderer = entityManager.GetComponentObject<ManagedRenderer>(boardTile.Entity);
      var renderTile = renderer.Value.GetComponent<RenderTile>();
      var position = boardTile.BoardPosition.ToWorldPosition();
      var rotation = boardTile.CardinalRotation.ToWorldRotation();

      entityManager.SetComponentData(boardTile.Entity, new Translation { Value = position });
      entityManager.SetComponentData(boardTile.Entity, new Rotation { Value = rotation });
      renderTile.SetElementalMaterials(renderGameObjects, tile);
      renderer.Value.transform.SetPositionAndRotation(position, rotation);
    }
  }

  public static void RenderBoardWizards(
  EntityManager entityManager,
  Entity boardEntity,
  RenderGameObjects renderGameObjects) {
    var player1WizardBuffer = entityManager.GetBuffer<BoardPlayer1WizardEntry>(boardEntity);
    var player2WizardBuffer = entityManager.GetBuffer<BoardPlayer2WizardEntry>(boardEntity);

    for (var i = 0; i < player1WizardBuffer.Length; i++) {
      var boardWizard = player1WizardBuffer[i];
      var renderer = entityManager.GetComponentObject<ManagedRenderer>(boardWizard.Entity);
      var renderWizard = renderer.Value.GetComponent<RenderWizard>();
      var position = boardWizard.BoardPosition.ToWorldPosition();
      var rotation = Quaternion.identity;

      entityManager.SetComponentData(boardWizard.Entity, new Translation { Value = position });
      entityManager.SetComponentData(boardWizard.Entity, new Rotation { Value = rotation });
      renderWizard.SetMaterialForPlayerIndex(renderGameObjects.Team1Material, renderGameObjects.Team2Material, 0);
      renderWizard.transform.SetPositionAndRotation(position, rotation);
    }

    for (var i = 0; i < player2WizardBuffer.Length; i++) {
      var boardWizard = player2WizardBuffer[i];
      var renderer = entityManager.GetComponentObject<ManagedRenderer>(boardWizard.Entity);
      var renderWizard = renderer.Value.GetComponent<RenderWizard>();
      var position = boardWizard.BoardPosition.ToWorldPosition();
      var rotation = Quaternion.identity;

      entityManager.SetComponentData(boardWizard.Entity, new Translation { Value = position });
      entityManager.SetComponentData(boardWizard.Entity, new Rotation { Value = rotation });
      renderWizard.SetMaterialForPlayerIndex(renderGameObjects.Team1Material, renderGameObjects.Team2Material, 1);
      renderWizard.transform.SetPositionAndRotation(position, rotation);
    }
  }

  public static void RenderBoardDragons(
  EntityManager entityManager,
  Entity boardEntity,
  RenderGameObjects renderGameObjects) {
    var dragonBuffer = entityManager.GetBuffer<BoardDragonEntry>(boardEntity);

    for (var i = 0; i < dragonBuffer.Length; i++) {
      var boardDragon = dragonBuffer[i];
      var renderer = entityManager.GetComponentObject<ManagedRenderer>(boardDragon.Entity);
      var position = boardDragon.BoardPosition.ToWorldPosition();
      var rotation = Quaternion.identity;

      entityManager.SetComponentData(boardDragon.Entity, new Translation { Value = position });
      entityManager.SetComponentData(boardDragon.Entity, new Rotation { Value = rotation });
      renderer.Value.transform.SetPositionAndRotation(position, rotation);
    }
  }

  protected override void OnCreate() {
    RequireSingletonForUpdate<Board>();
  }

  protected override void OnUpdate() {
    var gameConfiguration = GameConfiguration.Instance;
    var renderGameObjects = gameConfiguration.RenderGameObjects; 

    Entities
    .WithName("Render_Board")
    .WithAll<Board>()
    .ForEach((Entity entity) => {
      RenderBoardTiles(EntityManager, entity, renderGameObjects);
      RenderBoardDragons(EntityManager, entity, renderGameObjects);
      RenderBoardWizards(EntityManager, entity, renderGameObjects);
    })
    .WithoutBurst()
    .Run();
  }
}