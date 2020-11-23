using System;
using Unity.Mathematics;
using Unity.Entities;
using UnityEngine;
using static Unity.Mathematics.math;

public enum Element { 
  Earth, 
  Wind, 
  Fire, 
  Water
}

public enum Spell { 
  Rotate, 
  Move, 
  Place 
}

public enum CardinalRotation { 
  North = 0, 
  East, 
  South, 
  West 
}

public enum GameState { 
  Loading, 
  Ready, 
  TakingTurn,
  GameOver 
}

public enum ActionState {
  Base,
  RotateCardSelected,
  BoardTileToRotateSelected,
  RotationSelected,
  PlayingRotationAction
}

public struct PlayerAction : IComponentData {}

public struct SelectCard : IComponentData {
  public Entity Value;
}

public struct SelectTile : IComponentData {
  public Entity Value;
}

public struct SelectTilePosition : IComponentData {
  public int2 Value;
}

public struct SelectCardinalRotation : IComponentData {
  public CardinalRotation Value;
}

public struct ElementCard : IComponentData {
  public Element Element;
}

public struct SpellCard : IComponentData {
  public Spell Spell;
}

public struct ElementCardDeck : IComponentData {}

public struct SpellCardDeck : IComponentData {}

public struct ElementCardEntry : IBufferElementData {
  public Entity ElementCardEntity;
}

public struct SpellCardEntry : IBufferElementData {
  public Entity SpellCardEntity;
}

public struct TileEntry : IBufferElementData {
  public Entity TileEntity;
}

public struct PlayerIndex : ISharedComponentData {
  public int Value;
}

public struct Hand : IComponentData {
  public Entity TilesRootEntity;
  public Entity SpellCardsRootEntity;
  public Entity ElementCardsRootEntity;
}

[Serializable]
public struct Tile : IComponentData {
  public Element North;
  public Element East;
  public Element South;
  public Element West;
}

public struct TilePosition : IComponentData {
  public int2 Value;

  public Vector3 WorldPosition {
    get { return new Vector3(Value.x, 0, Value.y); } 
  }

  public static TilePosition FromWorldPosition(in Vector3 worldPosition) {
    return new TilePosition { Value = int2((int)worldPosition.x, (int)worldPosition.z) };
  }
}

public struct TileRotation : IComponentData {
  const float QUARTER_ROTATION_RADIANS = PI / 2f;

  public CardinalRotation Value;

  public Quaternion WorldRotation {
    get { return Quaternion.AngleAxis((float)Value * QUARTER_ROTATION_RADIANS, Vector3.up); }
  }
}

public struct Wizard : IComponentData {}

public struct Dragon : IComponentData {}

public struct Board : IComponentData {
  public int2 Dimensions;
}

public struct Game : IComponentData {
  public GameState GameState;
  public int CurrentTurnPlayerIndex;
}