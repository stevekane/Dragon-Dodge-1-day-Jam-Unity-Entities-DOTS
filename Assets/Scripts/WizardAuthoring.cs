using Unity.Entities;
using UnityEngine;

public class WizardAuthoring : MonoBehaviour, IConvertGameObjectToEntity {
  public int PlayerIndex;
  
  public void OnDrawGizmos() {
    Gizmos.color = PlayerIndex % 2 == 0 ? Color.green : Color.blue;
    Gizmos.DrawWireCube(transform.position, Vector3.one * .3f);
  }

  public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
    dstManager.AddComponent<Wizard>(entity);
    dstManager.AddComponentData(entity, TilePosition.FromWorldPosition(transform.position));
    dstManager.AddSharedComponentData(entity, new PlayerIndex { Value = PlayerIndex  });
  }
}