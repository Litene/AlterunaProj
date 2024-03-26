using System;
using Utility;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;
using Alteruna;
using UnityEngine.Serialization;


//This class is a follows the Monolithic Persistance Antipattern (https://learn.microsoft.com/en-us/azure/architecture/antipatterns/monolithic-persistence/),
//in reality of for a shipped game this should never be the case. A more reasonable split would follow A Model view controller pattern.
//There are multiple improvement that could be done, as of now I pass the whole board everytime it changes, I chose to do this because of simplicity.
//The best approach I can think of would to keep track of all changes pass them as a string array to reduce load on the network. 
public class MineGrid : AttributesSync, PlayerInputActionsMap.IPlayerControllInputActions {

	#region Syncronizable Feilds

	[SynchronizableField] public bool GameIsStarted;
	[SynchronizableField] public bool GameIsOver;
	[SynchronizableField] public int CurrentPlayerIndex;

	#endregion

	#region Seriliazed Fields

	[SerializeField] private Multiplayer _multiplayer;
	[SerializeField] private PlayerInputActionsMap _map;
	[SerializeField] private GameObject _lobby;
	[SerializeField] private GameObject _tilePrefab;
	[SerializeField] private GameObject _youWonObject;
	[SerializeField] private GameObject _youLostObject;
	[SerializeField] private GameObject _yourTurnObject;
	[SerializeField] private GameObject _teamMatesTurnObject;

	#endregion

	#region private Members

	private ConvertableMatrix<Tile> _board;

	#endregion

	#region private Variables

	private Vector2Int _boardSize;
	private int _numberOfPlayers;
	private const int NumberOfBombs = 20;

	#endregion

	#region Utility

	//used to see all the bomb positions
	public bool CheatMode;

	#endregion

	#region Initialization

	// Generates the play grid, called by the initiating player.
	public void GenerateGrid() {
		//if the board is null it generates a local board for the player.
		_board ??= GenerateLocalBoard(); 

		//Since Generate grid is called from the initiating player, the first player in order is set to the calling player.
		CurrentPlayerIndex = _multiplayer.Me.Index;

		//broadcasts Start game so that UI is setup properly for everyone.
		BroadcastRemoteMethod("StartTheGame");

		_multiplayer.LockRoom();
		
		_numberOfPlayers = _multiplayer.CurrentRoom.Users.Count;

		GameIsOver = false;
		
		BroadcastRemoteMethod("UpdateBoard", _board.GetStringBoard, false);
	}
	
	// initial UI Setup and adding listener Joined game.
	private void Start() {
		_multiplayer.OnRoomJoined.AddListener(JoinedGame);

		_youWonObject.SetActive(false);
		_youLostObject.SetActive(false);
		_yourTurnObject.SetActive(false);
		_teamMatesTurnObject.SetActive(false);
	}

	// Input system setup for the players.
	private void JoinedGame(Multiplayer player, Room room, User user) {
		_map ??= new PlayerInputActionsMap();
		_map.PlayerControllInput.SetCallbacks(this);
		_map.PlayerControllInput.Enable();
	}

	// place the bombs, this is done after the first click so that the player doesn't click a bomb the first time. 
	private void StartGame(Vector3 clickPosition) {
		PlaceBombs(clickPosition);
		foreach (var tile in _board) tile.AddNeighbors();
	}
	private void OnDisable() => _map?.PlayerControllInput.Disable();

	#endregion

	#region Synctronizable Methods

	// UI setup for all players
	[SynchronizableMethod] public void StartTheGame() {

		if (_lobby.activeInHierarchy) _lobby.SetActive(false);

		if (_youLostObject.activeInHierarchy) _youLostObject.SetActive(false);

		if (_youWonObject.activeInHierarchy) _youWonObject.SetActive(false);

		if (_teamMatesTurnObject.activeInHierarchy) _teamMatesTurnObject.SetActive(false);

		if (_yourTurnObject.activeInHierarchy) _yourTurnObject.SetActive(false);

		SetTurnUI(CurrentPlayerIndex == _multiplayer.Me.Index);
	}

	
	// Main update method for the game logic, Passes a string version of the board so that it can be read by all players in the game.
	[SynchronizableMethod] public void UpdateBoard(string[] board, bool initialSetup) {

		_board ??= GenerateLocalBoard();

		// Iterates over the string board and set coordinates and tilestate based on the overriden ToString method in Tile
		foreach (string state in board) { 
			int x = int.Parse(state.Substring(0, 2));
			int y = int.Parse(state.Substring(2, 2));
			int targetState = int.Parse(state.Substring(4, 2));
			GetTile(new Vector2Int(x, y)).CurrentState = ((TileState)targetState);
		}

		// obligatory win-check
		CheckWinState();

		// Setup neighboring bombs so that all the other players get the correct value
		if (initialSetup) {
			_numberOfPlayers = _multiplayer.CurrentRoom.Users.Count;
			SetupNeighbors();
		}
		
		//UI update
		SetTurnUI(CurrentPlayerIndex == _multiplayer.Me.Index);
	}
	
	// UI update for all the players
	[SynchronizableMethod] public void GameOver(bool lost) {
		if (lost) _youLostObject.SetActive(true);
		else _youWonObject.SetActive(true);
	}
	
	// clears the _board, it is set to null so that a new local board is generated if a new game is played. 
	[SynchronizableMethod] public void ClearGrid() {
		foreach (var tile in _board) Destroy(tile.gameObject);
		_board = null;
	}

	#endregion

	#region Utility Methods
	
	// takes the tile at the position in the _board matrix
	public Tile GetTile(Vector2Int position) {
		return !Helper.IsWithinBounds(position) || _board is null ? null : _board[position.x, position.y];
	}

	// Get the grid position from the worldposition
	private Vector2Int WorldToGridPosition(Vector3 worldPosition) => new(
		Mathf.FloorToInt((worldPosition.x / Helper.TileSize)),
		Mathf.FloorToInt((worldPosition.y / Helper.TileSize)));
	
	// iterates to next player, can not go over the index of the array, it will restart from 0
	private void NextPlayer() => CurrentPlayerIndex = (CurrentPlayerIndex + 1) % _numberOfPlayers;
	
	// Method to prewarm the tiles with neigbors so that it can be used to chose correct sprite when revealed
	private void SetupNeighbors() {
		foreach (var tile in _board) tile.AddNeighbors();
	}
	
	// Creates local board, initializes it and returns it
	private ConvertableMatrix<Tile> GenerateLocalBoard() {
		var tempBoard = new ConvertableMatrix<Tile>(Helper.DefaultBoardSizeX, Helper.DefaultBoardSizeY);
		for (int y = 0; y < Helper.DefaultBoardSizeY; y++) {
			for (int x = 0; x < Helper.DefaultBoardSizeX; x++) {
				var tile = tempBoard[x, y] = Instantiate(_tilePrefab, Vector3.zero, Quaternion.identity).GetComponent<Tile>();
				tile.Initialize(new Vector2Int(x, y), this);
				tile.gameObject.transform.position = tile.GetWorldPos;
			}
		}

		return tempBoard;
	}
	#endregion

	#region UI
	
	private void SetTurnUI(bool yourTurn) {
		_yourTurnObject.SetActive(yourTurn);
		_teamMatesTurnObject.SetActive(!yourTurn);
	}

	#endregion

	#region Gameplay alogrithm
	
	// silly win-check, all tiles that are empty or has a number is revealed, but is fully working but this can probably be
	// improved so it doesn't have to recount every time. On the other side,
	// simple counting is something a computer is good at and this is done locally
	private void CheckWinState() {
		var numberToComplete = _board.Length - NumberOfBombs;

		var currentRevealedCount = _board.Count(tile => tile.IsRevealed);

		if (currentRevealedCount != numberToComplete) return;

		GameIsOver = true;
		BroadcastRemoteMethod("GameOver", false);
	}
	
	// main algorithm for placing bombs, takes the initial clicked position and uses that to place bombs a bit a way 
	private void PlaceBombs(Vector3 initialClickPos) {
		foreach (var bombTile in GenerateBombPositions(WorldToGridPosition(initialClickPos)).Select(GetTile)) {
			bombTile.CurrentState = CheatMode ? TileState.Bomb : TileState.HiddenWithBomb;
		}
	}

	// Flood fill from the clicked position
	private void FloodFill(Vector2Int position) {
		var tile = GetTile(position);
		
		if (tile.IsRevealed || tile.IsFlagged) return;
		
		// revealed the current tile
		tile.Reveal();

		if (tile.CurrentState != TileState.Revealed) return;
		
		//iterates over neighbors and calls flood fill on the neighbors positions recursively
		foreach (var direction in Helper.Directions) { 
			var neighborPosition = position + direction;
			if (Helper.IsWithinBounds(neighborPosition)) FloodFill(neighborPosition);
		}
	}

	//method for generating positions, taking initial clicked position and surrounding area into consideration
	private List<Vector2Int> GenerateBombPositions(Vector2Int initialClickPosition) {
		var bombPositions = new List<Vector2Int>();
		var safePositions = new HashSet<Vector2Int>();

		// adding safe positions based on initial click
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
	
	//adding all safe spots so that bombs don't spawn there
	private void AddSafePositions(Vector2Int initialClickPosition, ISet<Vector2Int> safePositions) {
		safePositions.Add(initialClickPosition);

		foreach (var direction in Helper.Directions) {
			var neighborPosition = initialClickPosition + direction;
			if (Helper.IsWithinBounds(neighborPosition)) safePositions.Add(neighborPosition);
		}
	}

	#endregion

	#region Input logic

	// method that handles left click interaction. 
	public void OnLeftClick(InputAction.CallbackContext context) { // this should be cleaned up, method to long. 
		if (!context.performed) return;

		if (GameIsOver) return;

		if (CurrentPlayerIndex != _multiplayer.Me.Index) return;

		// reads the mouse position and converts it into world position
		var mousePosition = Mouse.current.position.ReadValue();
		var mouseWorldPosition = Camera.main!.ScreenToWorldPoint(mousePosition);

		// checks whether clicked position is within bounds
		if (!Helper.IsWithinBounds(WorldToGridPosition(mouseWorldPosition))) return;

		// finds the file
		var clickedTile = GetTile(WorldToGridPosition(mouseWorldPosition));

		// Return if it is not a clickable tile
		if (clickedTile is null || clickedTile.IsRevealed || clickedTile.IsFlagged) return;

		// if game is not started start game otherwise handle the clicked position
		if (!GameIsStarted) StartGame(mouseWorldPosition);
		else {
			if (clickedTile.HasBomb) {
				foreach (var tile in _board) if (tile != clickedTile && tile.HasBomb) tile.CurrentState = TileState.Bomb;

				clickedTile.CurrentState = TileState.ExplodedBomb;
				GameIsOver = true;
				
				//broadcasts 
				BroadcastRemoteMethod("GameOver", true);
				BroadcastRemoteMethod("UpdateBoard", _board.GetStringBoard, !GameIsStarted);
				return;
			}
		}

		// flood fill from mouses world position, this should be clicked tile, but I changed method half way through.
		FloodFill(WorldToGridPosition(mouseWorldPosition));

		// obligatory win check
		CheckWinState();
		
		// call to next player
		NextPlayer();

		// update board state for all players
		BroadcastRemoteMethod("UpdateBoard", _board.GetStringBoard, !GameIsStarted);

		// game has now started
		GameIsStarted = true;

	}

	// method that handles right click interaction. 
	public void OnRightClick(InputAction.CallbackContext context) {
		if (!context.performed || GameIsOver) return;

		if (CurrentPlayerIndex != _multiplayer.Me.Index) return;

		var mousePosition = Mouse.current.position.ReadValue();
		var mouseWorldPosition = Camera.main!.ScreenToWorldPoint(mousePosition);

		if (!Helper.IsWithinBounds(WorldToGridPosition(mouseWorldPosition))) return;

		// toggles flag for the current tile
		GetTile(WorldToGridPosition(mouseWorldPosition)).ToggleFlag();

		BroadcastRemoteMethod("UpdateBoard", _board.GetStringBoard, false);
	}

	// restarts the game
	public void OnRestartAction(InputAction.CallbackContext context) {
		if (!context.performed || !GameIsOver) return;

		if (_youLostObject.activeInHierarchy) _youLostObject.SetActive(false);

		if (_youWonObject.activeInHierarchy) _youWonObject.SetActive(false);

		BroadcastRemoteMethod("ClearGrid");
		GameIsStarted = false; 
		GenerateGrid();
	}

	#endregion
	
}