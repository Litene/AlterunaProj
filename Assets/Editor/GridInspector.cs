using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MineGrid))]public class GridInspector : Editor {

	public override void OnInspectorGUI() {
		DrawDefaultInspector();
		MineGrid grid = (MineGrid)target;
		if (GUILayout.Button("Generate") && grid) grid.GenerateGrid();
		if (GUILayout.Button("Clear") && grid) grid.ClearGird();
	}

}