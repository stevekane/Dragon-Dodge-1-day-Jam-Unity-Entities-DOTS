using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

public class RenderHandInstance : ISystemStateComponentData {
  public GameObject Instance;
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
public class HandRendererLifeCycleSYstem : SystemBase {
  protected override void OnUpdate() {
    var handRenderer = GameConfiguration.Instance.RenderGameObjects.RenderHand;

    Entities
    .WithName("Add_Render_Hand_Instance")
    .WithNone<RenderHandInstance>()
    .ForEach((Entity e, in Hand hand, in PlayerIndex playerIndex, in LocalToWorld localToWorld) => {
      var handRendererInstance = GameObject.Instantiate(handRenderer);

      handRendererInstance.transform.SetPositionAndRotation(localToWorld.Position, localToWorld.Rotation);
      EntityManager.AddComponentData(e, new RenderHandInstance { Instance = handRendererInstance });
    })
    .WithStructuralChanges()
    .WithoutBurst()
    .Run();

    Entities
    .WithName("Remove_Render_Hand_Instance")
    .WithNone<Hand, PlayerIndex, LocalToWorld>()
    .ForEach((Entity e, RenderHandInstance instance) => {
      GameObject.Destroy(instance.Instance);
      EntityManager.RemoveComponent<RenderHandInstance>(e);
    })
    .WithStructuralChanges()
    .WithoutBurst()
    .Run();
  }
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateAfter(typeof(HandRendererLifeCycleSYstem))]
public class HandRenderingSystem : SystemBase {
  protected override void OnUpdate() {
    // Hand rendering should be simple:
    //  Send commands to the hand renderer telling it what to draw
    //  For simplicity, make the hand renderer have an immediate-mode API
    //  RenderTiles(tiles)
    //  RenderElementalCards(elementalCards)
    //  RenderSpellCards(spellCards)
    // Need to also handle the states of cards
    //  Key this simple as well
    //  cards can be selected or not
    //  tiles can be selected or not
  }
}