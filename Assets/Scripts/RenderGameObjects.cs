using UnityEngine;

[CreateAssetMenu(menuName="RenderGameObjects")]
public class RenderGameObjects : ScriptableObject {
  public static RenderGameObjects Instance;

  public GameObject RenderBoardTile;
  public GameObject RenderBoardWizard;
  public GameObject RenderBoardDragon;

  public GameObject RenderHand;

  public Material Team1Material;
  public Material Team2Material;

  public Material EarthMaterial;
  public Material FireMaterial;
  public Material WindMaterial;
  public Material WaterMaterial;
}