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
    .WithAll<Tile, TilePosition, TileRotation>()
    .ForEach((Entity e) => {
      EntityManager.AddComponentData(e, new RenderTileInstance { Instance = GameObject.Instantiate(renderGameObjects.RenderBoardTile) });
    })
    .WithStructuralChanges()
    .WithoutBurst()
    .Run();

    Entities
    .WithName("Remove_Render_Tile_Instance")
    .WithAll<RenderTileInstance>()
    .WithNone<Tile, TilePosition, TileRotation>()
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
    .WithAll<Wizard, TilePosition>()
    .ForEach((Entity e) => {
      EntityManager.AddComponentData(e, new RenderWizardInstance { Instance = GameObject.Instantiate(renderGameObjects.RenderBoardWizard) });
    })
    .WithStructuralChanges()
    .WithoutBurst()
    .Run();

    Entities
    .WithName("Remove_Render_Wizard_Instance")
    .WithAll<RenderWizardInstance>()
    .WithNone<Wizard, TilePosition>()
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
    .WithAll<Dragon, TilePosition>()
    .ForEach((Entity e) => {
      EntityManager.AddComponentData(e, new RenderDragonInstance { Instance = GameObject.Instantiate(renderGameObjects.RenderBoardDragon) });
    })
    .WithStructuralChanges()
    .WithoutBurst()
    .Run();

    Entities
    .WithName("Remove_Render_Dragon_Instance")
    .WithAll<RenderDragonInstance>()
    .WithNone<Dragon, TilePosition>()
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
  protected override void OnUpdate() {
    var gameConfiguration = GameConfiguration.Instance;
    var renderGameObjects = gameConfiguration.RenderGameObjects; 

    Entities
    .WithName("Render_Tile_On_Board")
    .ForEach((Entity e, RenderTileInstance renderTile, in Tile tile, in TilePosition tilePosition, in TileRotation tileRotation) => {
      renderTile.Instance.GetComponent<RenderTile>().SetElementalMaterials(renderGameObjects, tile);
      renderTile.Instance.transform.SetPositionAndRotation(tilePosition.WorldPosition, tileRotation.WorldRotation);
    })
    .WithoutBurst()
    .Run();

    Entities
    .WithName("Render_Wizard_On_Board")
    .ForEach((Entity e, RenderWizardInstance renderWizard, in PlayerIndex playerIndex, in TilePosition tilePosition, in Rotation rotation) => {
      renderWizard.Instance.GetComponent<RenderWizard>().SetMaterialForPlayerIndex(renderGameObjects.Team1Material, renderGameObjects.Team2Material, playerIndex.Value);
      renderWizard.Instance.transform.SetPositionAndRotation(tilePosition.WorldPosition, rotation.Value);
    })
    .WithoutBurst()
    .Run();

    Entities
    .WithName("Render_Dragon_On_Board")
    .ForEach((Entity e, RenderDragonInstance renderDragon, in TilePosition tilePosition, in Rotation rotation) => {
      renderDragon.Instance.transform.SetPositionAndRotation(tilePosition.WorldPosition, rotation.Value);
    })
    .WithoutBurst()
    .Run();
  }
}