using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct BoardSetupBlobAsset {
  public int2 Dimensions;
  public BlobArray<int2> TilePositions;
}

public struct BoardSetup : IComponentData {
  public BlobAssetReference<BoardSetupBlobAsset> Reference;
}

public class BoardSetupAuthoring : MonoBehaviour, IConvertGameObjectToEntity {
  public int2 Dimensions;
  public int2[] TilePositions = new int2[0];

  public static bool InBounds(
  in int2 p,
  in int2 dimensions) {
    var min = -((dimensions - new int2(1,1)) / 2);
    var max = (dimensions - new int2(1,1)) / 2;

    return p.x >= min.x && p.y >= min.y && p.x <= max.x && p.y <= max.y;
  }

  public void OnValidate() {
    if (Dimensions.x % 2 == 0) {
      Debug.LogError("Board x dimensions must be an odd number.");
    }
    if (Dimensions.y % 2 == 0) {
      Debug.LogError("Board y dimensions must be an odd number.");
    }
  }

  public void OnDrawGizmos() {
    const float TILE_SCALE = .9f;

    Gizmos.color = Color.grey;
    Gizmos.DrawWireCube(transform.position, new Vector3(Dimensions.x, 0, Dimensions.y));
    foreach (var tileposition in TilePositions) {
      Gizmos.color = InBounds(tileposition, Dimensions) ? Color.green : Color.red;
      Gizmos.DrawWireCube(new Vector3(tileposition.x, 0, tileposition.y), new Vector3(TILE_SCALE, 0, TILE_SCALE));
    }
  }

  public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
    using (var builder = new BlobBuilder(Allocator.Temp)) {
      ref var root = ref builder.ConstructRoot<BoardSetupBlobAsset>();
      var tilePositions = builder.Allocate(ref root.TilePositions, TilePositions.Length);

      root.Dimensions = Dimensions;
      for (int i = 0; i < TilePositions.Length; i++) {
        tilePositions[i] = TilePositions[i];
      }
      var reference = builder.CreateBlobAssetReference<BoardSetupBlobAsset>(Allocator.Persistent);

      dstManager.AddComponentData(entity, new BoardSetup { 
        Reference = reference
      });
    }
  }
}