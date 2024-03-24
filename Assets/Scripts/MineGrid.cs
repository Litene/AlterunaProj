using System;
using Utility;
using System.Linq;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;
using Alteruna;
using Unity.Collections;
using UnityEngine.Events;
using Avatar = Alteruna.Avatar;


// MVC splitt?
public class MineGrid : AttributesSync, PlayerInputActionsMap.IPlayerControllInputActions {

	private Vector2Int _boardSize; // maybe serialize this to let the player chose?
	[SynchronizableField] public bool GameIsStarted;
	public bool GameIsOver; //used for debugging
	private ConvertableMatrix<Tile> _board;
	private const int NumberOfBombs = 20;
	[SerializeField] private GameObject _tilePrefab;
	[SerializeField] private Multiplayer _multiplayer;

	[SerializeField] private PlayerInputActionsMap _map;
	[SerializeField] private GameObject _youWonObject;
	[SerializeField] private GameObject _youLostObject;


	public void GenerateGrid() {


		_board ??= GenerateLocalBoard(); // this doesn't work...

		GameIsOver = false; // should be moved to reset
		
		for (var y = 0; y < Helper.DefaultBoardSizeY; y++)
			for (var x = 0; x < Helper.DefaultBoardSizeX; x++) {
				var tile = _board[x, y] = Instantiate(_tilePrefab, Vector3.zero, Quaternion.identity).GetComponent<Tile>();
				//Instantiate(_tilePrefab, Vector3.zero, quaternion.identity).GetComponentInChildren<Tile>();
				tile.Initialize(new Vector2Int(x, y), this);
				tile.gameObject.transform.position = tile.GetWorldPos;
			
			}
		

		BroadcastRemoteMethod( "UpdateBoard", _board.GetStringBoard, true); // this gets called from both to both?
		//PlaceBombs();
	}

	[SynchronizableMethod] public void UpdateBoard(string[] board, bool update) { // wanted to take it in as a simpler form of data
                                                                                      //but couldn't pass a matrix or an array longer than 1
		
		_board ??= GenerateLocalBoard();

		foreach (string state in board) {
			int x = int.Parse(state.Substring(0,2));
			int y = int.Parse(state.Substring(2, 2));
			
			int targetState = int.Parse(state.Substring(4,2));
			GetTile(new Vector2Int(x, y)).CurrentState = ((TileState)targetState);
		}

		CheckWinState();
		
		// for (int y = 0; y < Helper.DefaultBoardSizeY; y++) {
		// 	for (int x = 0; x < Helper.DefaultBoardSizeX; x++) {
		// 		GetTile(new Vector2Int(x, y)).UpdateTile((boardState.CastConvertableMatrix[x, y].TileSpriteState);
		// 	}
		// }
	}

	private ConvertableMatrix<Tile> GenerateLocalBoard() {
		var tempBoard = new ConvertableMatrix<Tile>(Helper.DefaultBoardSizeX, Helper.DefaultBoardSizeY);
		for (int y = 0; y < Helper.DefaultBoardSizeY; y++) {
			for (int x = 0; x < Helper.DefaultBoardSizeX; x++) {
				var tile = tempBoard[x, y] = Instantiate(_tilePrefab, Vector3.zero, Quaternion.identity).GetComponent<Tile>();
				//Instantiate(_tilePrefab, Vector3.zero, quaternion.identity).GetComponentInChildren<Tile>();
				tile.Initialize(new Vector2Int(x, y), this);
				tile.gameObject.transform.position = tile.GetWorldPos;
			}
		}

		return tempBoard;
	}

	private void CheckWinState() {
		var numberToComplete = _board.Length - NumberOfBombs;

		var currentRevealedCount = _board.Count(tile => tile.IsRevealed);

		if (currentRevealedCount != numberToComplete) return;

		GameIsOver = true;
		_youWonObject.SetActive(true);
	}

	// private void Awake() {
	//     _spawnEvent += StartingTheGame();
	//     _multiplayer.OnConnected.AddListener(_spawnEvent);
	// }


	public void OnConnectedToServer() {
		Debug.Log(" we are connecting");
		_map ??= new PlayerInputActionsMap();
		_map.PlayerControllInput.SetCallbacks(this);
		_map.PlayerControllInput.Enable();
		_youWonObject.SetActive(false);
		_youLostObject.SetActive(false);

		foreach (var VARIABLE in _multiplayer.CurrentRoom.Users) {
			
		Debug.Log(VARIABLE);
		}
		// GenerateGrid();
	}


	// private  void  OnEnable() { // hides alteruna thingys
	// }

	private void Start() {
		_multiplayer.OnRoomJoined.AddListener(JoinedGame);

		_youWonObject.SetActive(false);
		_youLostObject.SetActive(false);
		// _map ??= new PlayerInputActionsMap();
		// _map.PlayerControllInput.SetCallbacks(this);
		// _map.PlayerControllInput.Enable();
		// _youWonObject.SetActive(false);
		// _youLostObject.SetActive(false);
		// GenerateGrid();
	}

	public void JoinedGame(Multiplayer player, Room room, User user) {
		// Debug.Log(_avatar.IsMe);
		// if (!_avatar || !_avatar.IsMe) return;

		_map ??= new PlayerInputActionsMap();
		_map.PlayerControllInput.SetCallbacks(this);
		_map.PlayerControllInput.Enable();
	}


	// public UnityAction<Multiplayer, Endpoint> StartingTheGame() {
	// 	_map ??= new PlayerInputActionsMap();
	// 	_map.PlayerControllInput.SetCallbacks(this);
	// 	_map.PlayerControllInput.Enable();
	//     _youWonObject.SetActive(false);
	//     _youLostObject.SetActive(false);
	//     GenerateGrid();
	//     return null;
	// }

	private void OnDisable() => _map?.PlayerControllInput.Disable();


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
		foreach (var bombTile in GenerateBombPositions(WorldToGridPosition(initialClickPos)).Select(GetTile)) {
			bombTile.CurrentState = TileState.HiddenWithBomb;
		}
	}

	private List<Vector2Int> GenerateBombPositions(Vector2Int initialClickPosition) {
		var bombPositions = new List<Vector2Int>();
		var safePositions = new HashSet<Vector2Int>();

		AddSafePositions(initialClickPosition, safePositions);

		int numBombsToPlace = NumberOfBombs;

		while (numBombsToPlace > 0) {
			var position = new Vector2Int(Random.Range(0, Helper.DefaultBoardSizeX),
				Random.Range(0, Helper.DefaultBoardSizeY));

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
		return !Helper.IsWithinBounds(position) || _board is null ? null : _board[position.x, position.y];
	}

	// public Tile
	// 	GetTileWorldPosition(Vector3 worldPosition) => //todo: fix this has a bug, does not consider the consider pixeloffset
	// 	GetTile(new Vector2Int(Mathf.FloorToInt((worldPosition.x / Helper.TileSize) + worldPosition.x * Helper.PixelOffset),
	// 		Mathf.FloorToInt((worldPosition.y / Helper.TileSize) + worldPosition.y * Helper.PixelOffset)));

	private Vector2Int WorldToGridPosition(Vector3 worldPosition) => new(
		Mathf.FloorToInt((worldPosition.x / Helper.TileSize)),
		Mathf.FloorToInt((worldPosition.y / Helper.TileSize)));

	public void OnLeftClick(InputAction.CallbackContext context) { // simplyfly
		// this needs a good amount of cleaning
		if (!context.performed) return;

		if (GameIsOver) return;

		//_multiplayer.CurrentRoom.Users
		
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
					if (tile != clickedTile && tile.HasBomb) tile.CurrentState = TileState.Bomb;
				}

				clickedTile.CurrentState = TileState.ExplodedBomb;
				_youLostObject.SetActive(true);
				GameIsOver = true;
				BroadcastRemoteMethod( "UpdateBoard", _board.GetStringBoard, true);
				return;
			}
		}

		FloodFill(WorldToGridPosition(mouseWorldPosition));

		CheckWinState(); // make more dynamic???
		
		BroadcastRemoteMethod( "UpdateBoard", _board.GetStringBoard, true);
	}

	private void FloodFill(Vector2Int position) {
		var tile = GetTile(position);

		if (tile.IsRevealed || tile.IsFlagged) return;

		tile.Reveal();

		if (tile.CurrentState != TileState.Revealed) return;

		foreach (var direction in Helper.Directions) {
			var neighborPosition = position + direction;
			if (Helper.IsWithinBounds(neighborPosition)) {
				FloodFill(neighborPosition);
			}
		}
	}

	public void OnRightClick(InputAction.CallbackContext context) {
		if (!context.performed || GameIsOver) return;

		var mousePosition = Mouse.current.position.ReadValue();
		var mouseWorldPosition = Camera.main!.ScreenToWorldPoint(mousePosition);

		if (!Helper.IsWithinBounds(WorldToGridPosition(mouseWorldPosition))) return;
		
		GetTile(WorldToGridPosition(mouseWorldPosition)).ToggleFlag();
		
		BroadcastRemoteMethod( "UpdateBoard", _board.GetStringBoard, true);
	}

	public void OnRestartAction(InputAction.CallbackContext context) {
		if (!context.performed || !GameIsOver) return;

		GameIsStarted = false;
		if (_youLostObject.activeInHierarchy) _youLostObject.SetActive(false);

		if (_youWonObject.activeInHierarchy) _youWonObject.SetActive(false);

		ClearGird();
		GenerateGrid();
	}


}
