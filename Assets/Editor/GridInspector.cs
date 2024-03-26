using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MineGrid))]public class GridInspector : Editor {

	// super basic addition Inspector, used in the beginning before I made UI but I left it here.
	public override void OnInspectorGUI() {
		DrawDefaultInspector();
		MineGrid grid = (MineGrid)target;
		//if (GUILayout.Button("Generate") && grid) grid.GenerateGrid();
		//if (GUILayout.Button("Clear") && grid) grid.ClearGrid();
	}

}