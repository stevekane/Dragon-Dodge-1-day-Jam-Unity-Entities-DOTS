using System.Collections.Generic;
using UnityEngine;

public class RenderHand : MonoBehaviour {
  public Vector2 CardSpacing = new Vector2(.5f, 0);
  public Vector2 TileSpacing = new Vector2(.5f, .5f);
  public List<RenderTile> Tiles;
  public List<HandRenderElementCard> ElementCards;
  public List<HandRenderSpellCard> SpellCards;
  public Transform TilesTransform;
  public Transform ElementCardsTransform;
  public Transform SpellCardsTransform;
}