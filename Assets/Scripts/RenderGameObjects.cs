using UnityEngine;

[CreateAssetMenu(menuName="RenderGameObjects")]
public class RenderGameObjects : ScriptableObject {
  public static RenderGameObjects Instance;

  [Header("Board Rendering")]
  public GameObject RenderBoardTile;
  public GameObject RenderBoardWizard;
  public GameObject RenderBoardDragon;

  [Header("Hand Rendering")]
  public GameObject RenderHand;
  public RenderTile HandRenderTile;
  public HandRenderElementCard HandRenderElementCardUnknown;
  public HandRenderElementCard HandRenderElementCardEarth;
  public HandRenderElementCard HandRenderElementCardFire;
  public HandRenderElementCard HandRenderElementCardWind;
  public HandRenderElementCard HandRenderElementCardWater;
  public HandRenderSpellCard HandRenderSpellCardUnknown;
  public HandRenderSpellCard HandRenderSpellCardRotate;
  public HandRenderSpellCard HandRenderSpellCardMove;
  public HandRenderSpellCard HandRenderSpellCardPlace;

  [Header("Team Materials")]
  public Material Team1Material;
  public Material Team2Material;

  [Header("Earth Materials")]
  public Material EarthMaterial;
  public Material FireMaterial;
  public Material WindMaterial;
  public Material WaterMaterial;
}