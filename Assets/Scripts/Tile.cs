using System;
using System.Collections.Generic;
using Alteruna;
using UnityEngine;
using Utility;

public class Tile : MonoBehaviour, IConvertable {

	#region Properties

	// All these three can be removed but I do believe they provide clarity. Same information is stores within the TileState enum
	public bool HasBomb { get; private set; }
	public bool IsFlagged { get; private set; }
	public bool IsRevealed { get; private set; }

	private TileState _currentState = TileState.Flag;

	/// <summary>
	/// Set the current state of the tile, also changes the sprite accordingly.
	/// </summary>
	// Setup so that this controls the tile and sprite, the main pipeline for the tile, an enum is also easily convertable.
	public TileState CurrentState {
		get => _currentState;
		set {
			if (_currentState == value) return;

			_spriteRenderer.sprite = _spriteLookup[value];
			_currentState = value;

			IsRevealed = (int)_currentState > (int)TileState.Hidden && (int)_currentState < (int)TileState.Flag;
			
			if (_currentState == TileState.HiddenWithBomb) HasBomb = true;
			//else if (_currentState is not (TileState.HiddenWithBomb or TileState.Bomb or TileState.Flag)) HasBomb = false; // I can see it here...
			
			IsFlagged = _currentState is TileState.Flag;
		}
	}

	#endregion

	#region private variables

	private int _neighboringBombs;
	
	private Vector2Int _position;

	#endregion

	#region private members

	private SpriteRenderer _spriteRenderer;
	
	private MineGrid _grid;
	
	#endregion

	#region Collections

	private readonly Dictionary<TileState, Sprite> _spriteLookup = new();

	#endregion

	#region Serialized Feilds

	// sprite collection for the tiles, these can be loaded from resources and matched with strings but this is a micro optimization.
	
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

	#endregion

	#region Initialization
	private void Awake() {
		_spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		
		InitializeLookup(); 
	}

	// initializes the _sprite lookup dictionary, matching the tile state to the correct sprite.
	private void InitializeLookup() {
		_spriteLookup.Add(TileState.One, _oneSprite);
		_spriteLookup.Add(TileState.Two, _twoSprite);
		_spriteLookup.Add(TileState.Three, _threeSprite);
		_spriteLookup.Add(TileState.Four, _fourSprite);
		_spriteLookup.Add(TileState.Five, _fiveSprite);
		_spriteLookup.Add(TileState.Six, _sixSprite);
		_spriteLookup.Add(TileState.Seven, _sevenSprite);
		_spriteLookup.Add(TileState.Eight, _eightSprite);
		_spriteLookup.Add(TileState.ExplodedBomb, _explodedBombSprite);
		_spriteLookup.Add(TileState.Bomb, _bombSprite);
		_spriteLookup.Add(TileState.Flag, _flagSprite);
		_spriteLookup.Add(TileState.Revealed, _emptySprite);
		_spriteLookup.Add(TileState.Hidden, _hiddenSprite);
		_spriteLookup.Add(TileState.HiddenWithBomb, _hiddenSprite);
	}

	/// <summary>
	/// Initializes the tile so that position is set, also sets the World Position for the tile 
	/// </summary>
	/// <param name="position">Matrix coordinates</param> 
	/// <param name="grid">Grid Reference</param>
	public void Initialize(Vector2Int position, MineGrid grid) {
		_position = position;

		// Converts the matrix position of the tile to world position by multiplying it by the Tile size and adding the pixel offset.
		GetWorldPos = new Vector3(_position.x * Helper.TileSize + Helper.PixelOffset * _position.x,
									_position.y * Helper.TileSize + Helper.PixelOffset * _position.y);
		
		_grid = grid;
		
		// Initial State
		CurrentState = TileState.Hidden;
	}
	
	#endregion

	#region public methods
	
	/// <summary>
	/// Reveals the tile showing either an empty tile or how many neighboring bombs
	/// </summary>
	// TileState is setup so that it correlates to neighboring bombs, making it easy to change and use. 
	public void Reveal() => CurrentState = _neighboringBombs == 0 ? TileState.Revealed : (TileState)_neighboringBombs;

	/// <summary>
	/// Toggles the flag on the tile and toggles it back to the previous state.
	/// </summary>
	public void ToggleFlag() {
		if (CurrentState is not (TileState.HiddenWithBomb or TileState.Hidden or TileState.Flag)) return;

		//IsFlagged = !IsFlagged;
		CurrentState = CurrentState is not TileState.Flag ? TileState.Flag : HasBomb ? TileState.HiddenWithBomb : TileState.Hidden;
	}

	/// <summary>
	/// Counts neighboring bombs and sets a private variable used by reveal.
	/// </summary>
	public void AddNeighbors() {
		foreach (var pos in Helper.Directions)
			if (_grid.GetTile(_position + pos) != null && _grid.GetTile(_position + pos).HasBomb)
				_neighboringBombs++;
	}
	
	#endregion

	#region public Utility
	
	// returns the position of the tile
	public Vector2Int GetPosition() => _position; 

	// returns World position, set in initialize
	public Vector3 GetWorldPos { get; private set; }

	// Convert is part of IConvertable since you cannot force a override through an interface. 
	public string Convert() => ToString();

	// ToString override for custom serialization 
	public override string ToString() {
		string xPos = _position.x > 9 ? _position.x.ToString() : $"0{_position.x}";
		string yPos = _position.y > 9 ? _position.y.ToString() : $"0{_position.y}";
		string spriteState = (int)CurrentState > 9 ? ((int)CurrentState).ToString() : $"0{((int)CurrentState).ToString()}";

		return xPos + yPos + spriteState;
	}

	#endregion
}

public enum TileState {
	Hidden = 0,
	One = 1,
	Two = 2,
	Three = 3,
	Four = 4,
	Five = 5,
	Six = 6,
	Seven = 7,
	Eight = 8,
	Revealed = 9,
	Flag = 10,
	Bomb = 11,
	ExplodedBomb = 12,
	HiddenWithBomb = 13
}