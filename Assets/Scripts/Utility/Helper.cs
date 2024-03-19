using UnityEngine;

namespace Utility {
	public class Helper {
		public static Vector2Int[] Directions = {
				new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1,1), 
				new Vector2Int(-1, 0), new Vector2Int(1, 0),
				new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1)
		};
		
		public const int TileSize = 1;
		public const float PixelOffset = 0.01f;
		public const int DefaultBoardSizeX = 24;
		public const int DefaultBoardSizeY = 16;
		
		public static bool IsWithinBounds(Vector2Int position) =>
			position.x is >= 0 and < DefaultBoardSizeX && position.y is >= 0 and < DefaultBoardSizeY;
		
	}
}