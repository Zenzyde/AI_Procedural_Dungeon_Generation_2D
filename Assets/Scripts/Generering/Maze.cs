using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Maze : MonoBehaviour
{
	[Header("Link maze-parent here once generated")]
	[SerializeField] private Transform mazeParent;

	[Header("Maze settings")]
	public Vector2Int mazeSize, minRoomSize, maxRoomSize;
	public GameObject wallPrefab, edgePrefab, hallwayPrefab, roomPrefab, roomWallPrefab, startPrefab, endPrefab, doorPrefab;
	public Transform startPosition;
	public float brancingChance;
	public int roomPlaceAttempts, doorPlaceAttempts, hallwaysToRemove;
	public MazeAlgorithm mazeAlgorithm;

	public enum MazeAlgorithm
	{
		rogueRecursiveBackTracker, rogueGrowingTree
	}
	private TileBehaviour[,] mazeGrid;
	private List<TileBehaviour> start = new List<TileBehaviour>(), end = new List<TileBehaviour>();

	void Awake()
	{
		if (mazeParent == null)
			mazeParent = GameObject.Find("MazeParent").transform;
		mazeGrid = new TileBehaviour[mazeSize.x, mazeSize.y];
		int listIndex = 0;
		for (int i = 0; i < mazeSize.x; i++)
		{
			for (int j = 0; j < mazeSize.y; j++)
			{
				mazeGrid[i, j] = mazeParent.GetChild(listIndex).GetComponent<TileBehaviour>();
				if (mazeGrid[i, j].type == TileType.start)
					start.Add(mazeGrid[i, j]);
				if (mazeGrid[i, j].type == TileType.end)
					end.Add(mazeGrid[i, j]);
				listIndex++;
			}
		}
	}

	public TileBehaviour GetRndStartPosition()
	{
		System.Random rnd = new System.Random();
		int tile = rnd.Next(start.Count);
		Vector2Int intPos = start[tile].position;
		return mazeGrid[intPos.x, intPos.y];
	}

	public TileBehaviour GetPosition(int x, int y, Vector2Int dir)
	{
		Vector2Int intPos = mazeGrid[x, y].position + dir;
		if (intPos.x > mazeSize.x - 1 || intPos.y > mazeSize.y - 1 || intPos.x < 0 || intPos.y < 0)
			return null;
		return mazeGrid[intPos.x, intPos.y];
	}

	public Vector2Int GetMazeSize()
	{
		return mazeSize;
	}
}
