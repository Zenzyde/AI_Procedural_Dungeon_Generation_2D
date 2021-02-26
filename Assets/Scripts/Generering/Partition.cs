using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Partition
{
	public Tile[,] tiles;
	//End & start -> exclusive (covers possible room tiles)
	public Vector2Int size, start, end;

	public Partition(Tile[,] tiles, Vector2Int start, Vector2Int end)
	{
		this.tiles = tiles;
		size = end - start;
		this.start = start;
		this.end = end;
	}
}
