using System;
using Utility;
using System.Linq;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class MineGrid : MonoBehaviour, PlayerInputActionsMap.IPlayerControllInputActions {

	private Vector2Int _boardSize; // maybe serialize this to let the player chose?
	public bool GameIsStarted;
	public bool GameIsOver; //used for debugging
	private Tile[,] _board;
	private const int NumberOfBombs = 20;
	[SerializeField] private GameObject _tilePrefab;

	[SerializeField] private PlayerInputActionsMap _map;

	[SerializeField] private GameObject _youWonObject;
	[SerializeField] private GameObject _youLostObject;

	private void OnValidate() =>
		_board = _boardSize != Vector2.zero
			? new Tile[_boardSize.x, _boardSize.y]
			: new Tile[Helper.DefaultBoardSizeX, Helper.DefaultBoardSizeY];

	private void Start() {
		_youWonObject.SetActive(false);
		_youLostObject.SetActive(false);
		GenerateGrid();
	}

	public void GenerateGrid() {
		if (_board is null) return;

		GameIsOver = false;
		// this is messy don't like double for loops here
		for (int y = 0; y < Helper.DefaultBoardSizeY; y++)
			for (int x = 0; x < Helper.DefaultBoardSizeX; x++) {
				var tile = _board[x, y] =
					Instantiate(_tilePrefab, Vector3.zero, quaternion.identity).GetComponentInChildren<Tile>();
				tile.Initialize(new Vector2Int(x, y), this);
				tile.gameObject.transform.parent.position = tile.GetWorldPos;
			}

		//PlaceBombs();
	}

	private void CheckWinState() {
		int numberToComplete = _board.Length - NumberOfBombs;
		int currentRevealedCount = 0;
		foreach (var tile in _board) {
			if (tile.IsRevealed) {
				currentRevealedCount++;
			}
		}

		if (currentRevealedCount == numberToComplete) {
			GameIsOver = true;
			_youWonObject.SetActive(true);
		}
	}

	private void OnEnable() {
		_map ??= new PlayerInputActionsMap();
		_map.PlayerControllInput.SetCallbacks(this);
		_map.PlayerControllInput.Enable();
	}

	private void OnDisable() {
		_map.PlayerControllInput.Disable();
	}

	// private void Start() {
	// 	_map ??= new PlayerInputActionsMap();
	// 	_map.Enable();
	// }

	public void StartGame(Vector3 clickPosition) {
		PlaceBombs(clickPosition);
		foreach (var tile in _board) {
			tile.AddNeighbors();
		}
	}

	public void ClearGird() {
		foreach (var tile in _board) Destroy(tile.gameObject);
	}

	private void PlaceBombs(Vector3 initialClickPos) {
		foreach (var position in GenerateBombPositions(WorldToGridPosition(initialClickPos))) {
			GetTile(position).HasBomb = true;
		}
	}

	private List<Vector2Int> GenerateBombPositions(Vector2Int initialClickPosition) {
		var bombPositions = new List<Vector2Int>();
		var safePositions = new HashSet<Vector2Int>();

		AddSafePositions(initialClickPosition, safePositions);

		int numBombsToPlace = NumberOfBombs;

		while (numBombsToPlace > 0) {
			var position = new Vector2Int(Random.Range(0, Helper.DefaultBoardSizeX), Random.Range(0, Helper.DefaultBoardSizeY));

			if (bombPositions.Contains(position) || safePositions.Contains(position)) continue;

			bombPositions.Add(position);
			numBombsToPlace--;
		}

		return bombPositions;
	}

	private void AddSafePositions(Vector2Int initialClickPosition, ISet<Vector2Int> safePositions) {
		safePositions.Add(initialClickPosition);

		foreach (var direction in Helper.Directions) {
			var neighborPosition = initialClickPosition + direction;
			if (Helper.IsWithinBounds(neighborPosition)) safePositions.Add(neighborPosition);
		}
	}

	public Tile GetTile(Vector2Int position) {
		return !Helper.IsWithinBounds(position) ? null : _board[position.x, position.y];
	}

	// public Tile
	// 	GetTileWorldPosition(Vector3 worldPosition) => //todo: fix this has a bug, does not consider the consider pixeloffset
	// 	GetTile(new Vector2Int(Mathf.FloorToInt((worldPosition.x / Helper.TileSize) + worldPosition.x * Helper.PixelOffset),
	// 		Mathf.FloorToInt((worldPosition.y / Helper.TileSize) + worldPosition.y * Helper.PixelOffset)));

	public Vector2Int WorldToGridPosition(Vector3 worldPosition) => new(
		Mathf.FloorToInt((worldPosition.x / Helper.TileSize)),
		Mathf.FloorToInt((worldPosition.y / Helper.TileSize)));

	public void OnLeftClick(InputAction.CallbackContext context) {
		if (!context.performed) return;


		if (GameIsOver) return;

		var mousePosition = Mouse.current.position.ReadValue();
		var mouseWorldPosition = Camera.main!.ScreenToWorldPoint(mousePosition);

		if (!Helper.IsWithinBounds(WorldToGridPosition(mouseWorldPosition))) return;

		var clickedTile = GetTile(WorldToGridPosition(mouseWorldPosition));

		if (clickedTile.IsFlagged) return;

		if (!GameIsStarted) {
			StartGame(mouseWorldPosition);
			GameIsStarted = true;
		}
		else {
			if (clickedTile.HasBomb) {
				foreach (var tile in _board) {
					if (tile != clickedTile && tile.HasBomb) tile.TileSpriteState = TileSpriteState.Bomb;
				}

				clickedTile.TileSpriteState = TileSpriteState.ExplodedBomb;
				_youLostObject.SetActive(true);
				GameIsOver = true;
				return;
			}


		}

		FloodFill(WorldToGridPosition(mouseWorldPosition));

		CheckWinState();
	}

	private void FloodFill(Vector2Int position) {
		var tile = GetTile(position);

		if (tile.IsRevealed || tile.IsFlagged) return;

		tile.Reveal();

		if (tile.TileSpriteState != TileSpriteState.Empty) return;

		foreach (var direction in Helper.Directions) {
			var neighborPosition = position + direction;
			if (Helper.IsWithinBounds(neighborPosition)) {
				FloodFill(neighborPosition);
			}
		}
	}

	public void OnRightClick(InputAction.CallbackContext context) {


		if (context.performed) {
			if (GameIsOver) return;
			var mousePosition = Mouse.current.position.ReadValue();
			var mouseWorldPosition = Camera.main!.ScreenToWorldPoint(mousePosition);

			GetTile(WorldToGridPosition(mouseWorldPosition)).ToggleFlag();
			// if (GetTile(WorldToGridPosition(mouseWorldPosition))) {
			// 	Grid
			// }
		}
	}

	public void OnRestartAction(InputAction.CallbackContext context) {
		if (!context.performed || !GameIsOver) return;

		GameIsStarted = false;
		if (_youLostObject.activeInHierarchy) {
			_youLostObject.SetActive(false);
		}

		if (_youWonObject.activeInHierarchy) {
			_youWonObject.SetActive(false);
		}

		ClearGird();
		GenerateGrid();
	}

}