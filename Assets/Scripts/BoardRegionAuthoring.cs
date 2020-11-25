using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct BoardRegion : IComponentData {
  public int2 Min;
  public int2 Max; 
}

public class BoardRegionAuthoring : MonoBehaviour, IConvertGameObjectToEntity {
  public int2 Min;
  public int2 Max;

  public void OnDrawGizmos() {
    Gizmos.color = Color.magenta;
    Gizmos.DrawWireCube(transform.position, new Vector3(Max.x - Min.x, 0, Max.y - Min.y));
  }

  public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
    dstManager.AddComponentData(entity, new BoardRegion {
      Min = Min,
      Max = Max
    });
  }
}