using UnityEngine;

public class GameConfiguration : MonoBehaviour {
  public static GameConfiguration Instance;

  public RenderGameObjects RenderGameObjects;

  public void Awake() {
    Instance = this;
  }
}