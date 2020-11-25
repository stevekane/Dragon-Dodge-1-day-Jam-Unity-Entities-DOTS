using Unity.Entities;
using UnityEngine;

public class DragonAuthoring : MonoBehaviour, IConvertGameObjectToEntity {
  public void OnDrawGizmos() {
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, .4f);
  }

  public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
    dstManager.AddComponent<Dragon>(entity);
  }
}