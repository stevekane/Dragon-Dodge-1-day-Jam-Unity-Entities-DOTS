using Unity.Entities;
using UnityEngine;

public class TileAuthoring : MonoBehaviour, IConvertGameObjectToEntity {
  public Tile Tile;

  public Color ColorForElement(in Element element) {
    switch (element) {
    case Element.Earth: return Color.green;
    case Element.Fire: return Color.red;
    case Element.Wind: return Color.white;
    case Element.Water: return Color.blue;
    default: return Color.black;
    }
  }

  public void OnDrawGizmos() {
    Gizmos.color = ColorForElement(Tile.North);
    Gizmos.DrawLine(transform.position, transform.position + Vector3.forward * .4f);
    Gizmos.color = ColorForElement(Tile.East);
    Gizmos.DrawLine(transform.position, transform.position + Vector3.right * .4f);
    Gizmos.color = ColorForElement(Tile.South);
    Gizmos.DrawLine(transform.position, transform.position - Vector3.forward * .4f);
    Gizmos.color = ColorForElement(Tile.West);
    Gizmos.DrawLine(transform.position, transform.position - Vector3.right * .4f);
  }

  public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
    dstManager.AddComponentData(entity, Tile);
  }
}