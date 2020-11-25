using System;
using Unity.Mathematics;
using Unity.Entities;

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
  PlayingRotationAction
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

public struct Player1 : IComponentData {}

public struct Player2 : IComponentData {}

public struct PlayerIndex : IComponentData {
  public int Value;
}

public struct Hand : IComponentData {
  public Entity TilesRootEntity;
  public Entity SpellCardsRootEntity;
  public Entity ElementCardsRootEntity;
  public Entity ActionEntity;
}

public struct Action : IComponentData {
  public CardinalRotation SelectedCardinalRotation;
  public Entity SelectedTileEntity;
  public Entity SelectedSpellCardEntity;
}

[Serializable]
public struct Tile : IComponentData {
  public Element North;
  public Element East;
  public Element South;
  public Element West;
}

public struct Wizard : IComponentData {}

public struct Dragon : IComponentData {}

public struct Board : IComponentData {}

public struct BoardTileEntry : IBufferElementData {
  public int2 BoardPosition;
  public CardinalRotation CardinalRotation;
  public Entity Entity;
}

public struct BoardPieceEntry : IBufferElementData {
  public int2 BoardPosition;
  public Entity Entity;
}

public struct Game : IComponentData {
  public GameState GameState;
  public ActionState ActionState;
  public int CurrentTurnPlayerIndex;
}