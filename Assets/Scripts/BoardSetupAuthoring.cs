using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct BoardSetupBlobAsset {
  public BlobArray<int2> TilePositions;
  public BlobArray<int2> DragonPositions;
  public BlobArray<int2> Player1Positions;
  public BlobArray<int2> Player2Positions;
}

public struct BoardSetup : IComponentData {
  public BlobAssetReference<BoardSetupBlobAsset> Reference;
}

public class BoardSetupAuthoring : MonoBehaviour, IConvertGameObjectToEntity {
  [Header("Editor settings")]
  public Color TileColor = Color.white;
  public Color DragonColor = Color.red;
  public Color Player1Color = Color.green;
  public Color Player2Color = Color.blue;
  public Vector3 TileSize = new Vector3(.9f, 0, .9f);
  public Vector3 WizardSize = new Vector3(.5f, 1, .5f);
  public float DragonRadius = .75f;

  [Header("Board Objects")]
  public int2[] TilePositions = new int2[0];
  public int2[] DragonPositions = new int2[2];
  public int2[] Player1Positions = new int2[2];
  public int2[] Player2Positions = new int2[2];

  public void OnDrawGizmos() {
    Gizmos.color = TileColor;
    foreach (var position in TilePositions) {
      Gizmos.DrawWireCube(new Vector3(position.x, 0, position.y), TileSize);
    }
    Gizmos.color = Player1Color;
    foreach (var position in Player1Positions) {
      Gizmos.DrawWireCube(new Vector3(position.x, .5f, position.y), WizardSize);
    }
    Gizmos.color = Player2Color;
    foreach (var position in Player2Positions) {
      Gizmos.DrawWireCube(new Vector3(position.x, .5f, position.y), WizardSize);
    }
    Gizmos.color = DragonColor;
    foreach (var position in DragonPositions) {
      Gizmos.DrawWireSphere(new Vector3(position.x, .5f, position.y), DragonRadius);
    }
  }

  public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
    using (var builder = new BlobBuilder(Allocator.Temp)) {
      ref var root = ref builder.ConstructRoot<BoardSetupBlobAsset>();
      var tilePositions = builder.Allocate(ref root.TilePositions, TilePositions.Length);
      var dragonPositions = builder.Allocate(ref root.DragonPositions, DragonPositions.Length);
      var player1Positions = builder.Allocate(ref root.Player1Positions, Player1Positions.Length);
      var player2Positions = builder.Allocate(ref root.Player2Positions, Player2Positions.Length);

      for (int i = 0; i < TilePositions.Length; i++) {
        tilePositions[i] = TilePositions[i];
      }
      for (int i = 0; i < DragonPositions.Length; i++) {
        dragonPositions[i] = DragonPositions[i];
      }
      for (int i = 0; i < Player1Positions.Length; i++) {
        player1Positions[i] = Player1Positions[i];
      }
      for (int i = 0; i < Player2Positions.Length; i++) {
        player2Positions[i] = Player2Positions[i];
      }

      dstManager.AddComponentData(entity, new BoardSetup { 
        Reference = builder.CreateBlobAssetReference<BoardSetupBlobAsset>(Allocator.Persistent)
      });
    }
  }
}