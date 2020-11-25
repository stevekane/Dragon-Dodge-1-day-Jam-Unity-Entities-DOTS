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
  protected override void OnCreate() {
    RequireSingletonForUpdate<Board>();
  }

  protected override void OnUpdate() {
    var gameConfiguration = GameConfiguration.Instance;
    var renderGameObjects = gameConfiguration.RenderGameObjects; 

    Entities
    .WithName("Render_Board")
    .ForEach((Entity entity, in Board board) => {
      var tileBuffer = EntityManager.GetBuffer<BoardTileEntry>(entity);

      for (int i = 0; i < tileBuffer.Length; i++) {
        var boardTile = tileBuffer[i];
        var tile = EntityManager.GetComponentData<Tile>(boardTile.Entity);
        var renderTileInstance = EntityManager.GetComponentObject<RenderTileInstance>(boardTile.Entity);
        var renderTile = renderTileInstance.Instance.GetComponent<RenderTile>();
        var position = boardTile.BoardPosition.ToWorldPosition();
        var rotation = boardTile.CardinalRotation.ToWorldRotation();

        // Set the actual tile position and rotation
        EntityManager.SetComponentData(boardTile.Entity, new Translation { Value = position });
        EntityManager.SetComponentData(boardTile.Entity, new Rotation { Value = rotation });
        // Set the rendered prefab's properties and transform
        renderTile.SetElementalMaterials(renderGameObjects, tile);
        renderTileInstance.Instance.transform.SetPositionAndRotation(position, rotation);
      }
    })
    .WithoutBurst()
    .Run();
  }
}