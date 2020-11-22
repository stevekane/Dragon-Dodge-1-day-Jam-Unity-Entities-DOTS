using UnityEngine;

public class RenderWizard : MonoBehaviour {
  public SkinnedMeshRenderer SkinnedMeshRenderer;

  public void SetMaterialForPlayerIndex(Material team1Material, Material team2Material, int playerIndex) {
    if (playerIndex % 2 == 0) {
      SkinnedMeshRenderer.material = team1Material;
    } else {
      SkinnedMeshRenderer.material = team2Material;
    }
  }
}