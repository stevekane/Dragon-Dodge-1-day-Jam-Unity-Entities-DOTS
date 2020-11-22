using UnityEngine;

public class RenderTile : MonoBehaviour {
  public MeshRenderer NorthMeshRenderer;
  public MeshRenderer EastMeshRenderer;
  public MeshRenderer SouthMeshRenderer;
  public MeshRenderer WestMeshRenderer;

  public void SetElementalMaterial(RenderGameObjects renderGameObjects, MeshRenderer meshRenderer, in Element element) {
    switch (element) {
    case Element.Earth:
    meshRenderer.material = renderGameObjects.EarthMaterial;
    break;

    case Element.Fire:
    meshRenderer.material = renderGameObjects.FireMaterial;
    break;

    case Element.Wind:
    meshRenderer.material = renderGameObjects.WindMaterial;
    break;

    case Element.Water:
    meshRenderer.material = renderGameObjects.WaterMaterial;
    break;
    }
  }

  public void SetElementalMaterials(RenderGameObjects renderGameObjects, in Tile tile) {
    SetElementalMaterial(renderGameObjects, NorthMeshRenderer, tile.North);
    SetElementalMaterial(renderGameObjects, EastMeshRenderer, tile.East);
    SetElementalMaterial(renderGameObjects, SouthMeshRenderer, tile.South);
    SetElementalMaterial(renderGameObjects, WestMeshRenderer, tile.West);
  }
}