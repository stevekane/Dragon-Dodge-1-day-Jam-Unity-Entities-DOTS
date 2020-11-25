using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

public class RenderDragonInstance : ISystemStateComponentData {
  public GameObject Instance; 
}

public class RenderWizardInstance : ISystemStateComponentData {
  public GameObject Instance; 
}

public class RenderTileInstance : ISystemStateComponentData {
  public GameObject Instance;
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
public class BoardRendererLifeCycleSystem : SystemBase {
  protected override void OnUpdate() {
    var gameConfiguration = GameConfiguration.Instance;
    var renderGameObjects = gameConfiguration.RenderGameObjects; 

    Entities
    .WithName("Add_Render_Tile_Instance")
    .WithNone<RenderTileInstance>()
    .WithAll<Tile>()
    .ForEach((Entity e) => {
      EntityManager.AddComponentData(e, new RenderTileInstance { Instance = GameObject.Instantiate(renderGameObjects.RenderBoardTile) });
    })
    .WithStructuralChanges()
    .WithoutBurst()
    .Run();

    Entities
    .WithName("Remove_Render_Tile_Instance")
    .WithAll<RenderTileInstance>()
    .WithNone<Tile>()
    .ForEach((Entity e, RenderTileInstance instance) => {
      GameObject.Destroy(instance.Instance);
      EntityManager.RemoveComponent<RenderTileInstance>(e);
    })
    .WithStructuralChanges()
    .WithoutBurst()
    .Run();

    Entities
    .WithName("Add_Render_Wizard_Instance")
    .WithNone<RenderWizardInstance>()
    .WithAll<Wizard>()
    .ForEach((Entity e) => {
      EntityManager.AddComponentData(e, new RenderWizardInstance { Instance = GameObject.Instantiate(renderGameObjects.RenderBoardWizard) });
    })
    .WithStructuralChanges()
    .WithoutBurst()
    .Run();

    Entities
    .WithName("Remove_Render_Wizard_Instance")
    .WithAll<RenderWizardInstance>()
    .WithNone<Wizard>()
    .ForEach((Entity e, RenderWizardInstance instance) => {
      GameObject.Destroy(instance.Instance);
      EntityManager.RemoveComponent<RenderWizardInstance>(e);
    })
    .WithStructuralChanges()
    .WithoutBurst()
    .Run();

    Entities
    .WithName("Add_Render_Dragon_Instance")
    .WithNone<RenderDragonInstance>()
    .WithAll<Dragon>()
    .ForEach((Entity e) => {
      EntityManager.AddComponentData(e, new RenderDragonInstance { Instance = GameObject.Instantiate(renderGameObjects.RenderBoardDragon) });
    })
    .WithStructuralChanges()
    .WithoutBurst()
    .Run();

    Entities
    .WithName("Remove_Render_Dragon_Instance")
    .WithAll<RenderDragonInstance>()
    .WithNone<Dragon>()
    .ForEach((Entity e, RenderDragonInstance instance) => {
      GameObject.Destroy(instance.Instance);
      EntityManager.RemoveComponent<RenderDragonInstance>(e);
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
      var renderTileInstance = entityManager.GetComponentObject<RenderTileInstance>(boardTile.Entity);
      var renderTile = renderTileInstance.Instance.GetComponent<RenderTile>();
      var position = boardTile.BoardPosition.ToWorldPosition();
      var rotation = boardTile.CardinalRotation.ToWorldRotation();

      entityManager.SetComponentData(boardTile.Entity, new Translation { Value = position });
      entityManager.SetComponentData(boardTile.Entity, new Rotation { Value = rotation });
      renderTile.SetElementalMaterials(renderGameObjects, tile);
      renderTileInstance.Instance.transform.SetPositionAndRotation(position, rotation);
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
      var renderWizardInstance = entityManager.GetComponentObject<RenderWizardInstance>(boardWizard.Entity);
      var renderWizard = renderWizardInstance.Instance.GetComponent<RenderWizard>();
      var position = boardWizard.BoardPosition.ToWorldPosition();
      var rotation = Quaternion.identity;

      entityManager.SetComponentData(boardWizard.Entity, new Translation { Value = position });
      entityManager.SetComponentData(boardWizard.Entity, new Rotation { Value = rotation });
      renderWizard.SetMaterialForPlayerIndex(renderGameObjects.Team1Material, renderGameObjects.Team2Material, 0);
      renderWizard.transform.SetPositionAndRotation(position, rotation);
    }

    for (var i = 0; i < player2WizardBuffer.Length; i++) {
      var boardWizard = player2WizardBuffer[i];
      var renderWizardInstance = entityManager.GetComponentObject<RenderWizardInstance>(boardWizard.Entity);
      var renderWizard = renderWizardInstance.Instance.GetComponent<RenderWizard>();
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
      var renderDragonInstance = entityManager.GetComponentObject<RenderDragonInstance>(boardDragon.Entity);
      var position = boardDragon.BoardPosition.ToWorldPosition();
      var rotation = Quaternion.identity;

      entityManager.SetComponentData(boardDragon.Entity, new Translation { Value = position });
      entityManager.SetComponentData(boardDragon.Entity, new Rotation { Value = rotation });
      renderDragonInstance.Instance.transform.SetPositionAndRotation(position, rotation);
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