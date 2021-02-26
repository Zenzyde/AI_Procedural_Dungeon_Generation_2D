using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Maze))]
public class MazeEditor : Editor
{
	private RuntimeMazeGenerator mazeGenerator;

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();
		if (GUILayout.Button("Generate Maze"))
		{
			if (mazeGenerator == null && !GameObject.Find("MazeParent"))
			{
				mazeGenerator = FindObjectOfType<RuntimeMazeGenerator>();
			}
			else
			{
				if (GameObject.Find("MazeParent"))
					GameObject.DestroyImmediate(GameObject.Find("MazeParent"));
				else
					mazeGenerator.DestroyMaze();
				mazeGenerator = FindObjectOfType<RuntimeMazeGenerator>();
			}
			mazeGenerator.StartMazeGeneration(target as Maze);
		}
		if (mazeGenerator != null && GUILayout.Button("Save Maze Prefab"))
			mazeGenerator.SaveMaze();
	}
}
