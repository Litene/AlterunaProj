using System;
using System.Collections.Generic;
using UnityEngine;
using Utility;
[RequireComponent(typeof(SpriteRenderer))]
public class Tile : MonoBehaviour {

	private bool _hasBomb;
	public bool HasBomb {
		get => _hasBomb;
		set {
			//TileSpriteState = TileSpriteState.Bomb;
			_hasBomb = value;
		}
	}

	public bool IsFlagged;
	public bool IsRevealed;
	
	[SerializeField] private int _neighboringBombs;
	private SpriteRenderer _spriteRenderer;
	private TileSpriteState _tileSpriteState = global::TileSpriteState.Flag;
	private TileSpriteState _tileState = global::TileSpriteState.Hidden;
	private Vector2Int _position;
	private MineGrid _grid;

	private readonly Dictionary<TileSpriteState, Sprite> _spriteLookup = new();
	private readonly Dictionary<TileSpriteState, Sprite> _neighborToSpriteLookup = new();

	[SerializeField] private Sprite _oneSprite;
	[SerializeField] private Sprite _twoSprite;
	[SerializeField] private Sprite _threeSprite;
	[SerializeField] private Sprite _fourSprite;
	[SerializeField] private Sprite _fiveSprite;
	[SerializeField] private Sprite _sixSprite;
	[SerializeField] private Sprite _sevenSprite;
	[SerializeField] private Sprite _eightSprite;
	[SerializeField] private Sprite _explodedBombSprite;
	[SerializeField] private Sprite _bombSprite;
	[SerializeField] private Sprite _flagSprite;
	[SerializeField] private Sprite _emptySprite;
	[SerializeField] private Sprite _hiddenSprite;

	private void Awake() {
		_spriteRenderer = GetComponent<SpriteRenderer>();
		InitializeLookup();
	}
	
	private void InitializeLookup() {
		_spriteLookup.Add(TileSpriteState.One, _oneSprite);
		_spriteLookup.Add(TileSpriteState.Two, _twoSprite);
		_spriteLookup.Add(TileSpriteState.Three, _threeSprite);
		_spriteLookup.Add(TileSpriteState.Four, _fourSprite);
		_spriteLookup.Add(TileSpriteState.Five, _fiveSprite);
		_spriteLookup.Add(TileSpriteState.Six, _sixSprite);
		_spriteLookup.Add(TileSpriteState.Seven, _sevenSprite);
		_spriteLookup.Add(TileSpriteState.Eight, _eightSprite);
		_spriteLookup.Add(TileSpriteState.ExplodedBomb, _explodedBombSprite);
		_spriteLookup.Add(TileSpriteState.Bomb, _bombSprite);
		_spriteLookup.Add(TileSpriteState.Flag, _flagSprite);
		_spriteLookup.Add(TileSpriteState.Empty, _emptySprite);
		_spriteLookup.Add(TileSpriteState.Hidden, _hiddenSprite);
	}

	public void Reveal() { // should be used when lost, make it more dynamic
		IsRevealed = true;
		if (HasBomb) TileSpriteState = TileSpriteState.Bomb;
		else TileSpriteState = _neighboringBombs == 0 ? TileSpriteState.Empty : _tileState;
	}
	
	public TileSpriteState TileSpriteState {
		get => _tileSpriteState;
		set {
			if (_tileSpriteState == value) return;
			
			_spriteRenderer.sprite = _spriteLookup[value];
			_tileSpriteState = value;
		}
	}

	public void ToggleFlag() {
		if (IsRevealed) return;
		
		IsFlagged = !IsFlagged;
		TileSpriteState = IsFlagged ? TileSpriteState.Flag : TileSpriteState.Hidden;
	}

	public void Initialize(Vector2Int position, MineGrid grid) {
		_position = position;
		GetWorldPos = new Vector3(_position.x * Helper.TileSize + Helper.PixelOffset * _position.x,
			_position.y * Helper.TileSize + Helper.PixelOffset * _position.y);
		_grid = grid;
		TileSpriteState = TileSpriteState.Hidden;
	}

	public void AddNeighbors() {
		foreach (var pos in Helper.Directions)
			if (_grid.GetTile(_position + pos) != null && _grid.GetTile(_position + pos).HasBomb)
				_neighboringBombs++;

		_tileState = (TileSpriteState)_neighboringBombs;
	}

	public Vector2Int GetPosition() => _position;
	public Vector3 GetWorldPos { get; private set; }

}

public enum TileSpriteState : byte {
	Hidden = 0,
	One = 1,
	Two = 2,
	Three = 3,
	Four = 4,
	Five = 5,
	Six = 6,
	Seven = 7,
	Eight = 8,
	Flag = 9,
	Empty = 10,
	Bomb = 11,
	ExplodedBomb = 12
}
