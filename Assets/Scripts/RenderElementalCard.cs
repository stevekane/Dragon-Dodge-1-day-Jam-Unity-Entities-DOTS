using UnityEngine;

public class RenderElementalCard : MonoBehaviour {
  public MeshRenderer ElementalMeshRenderer;

  public void SetElementalMaterial(RenderGameObjects renderGameObjects, in Element element) {
    switch (element) {
    case Element.Earth:
    ElementalMeshRenderer.material = renderGameObjects.EarthMaterial;
    break;

    case Element.Fire:
    ElementalMeshRenderer.material = renderGameObjects.FireMaterial;
    break;

    case Element.Wind:
    ElementalMeshRenderer.material = renderGameObjects.WindMaterial;
    break;

    case Element.Water:
    ElementalMeshRenderer.material = renderGameObjects.WaterMaterial;
    break;
    }
  }
}