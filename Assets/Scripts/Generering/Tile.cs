using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Tile
{
	[HideInInspector] public TileType tileType = TileType.wall;
	[HideInInspector] public GameObject tile;
	[HideInInspector] public Vector2Int position;

	public Tile(GameObject tile, Vector2Int position, GameObject parent)
	{
		this.tile = tile;
		this.tile.AddComponent<TileBehaviour>().SetTileType(tileType, position);
		this.position = position;
	}

	public void ChangeType(TileType type, GameObject prefab, GameObject parent)
	{
		tileType = type;
		Vector3 pos = tile.transform.position;
		GameObject.DestroyImmediate(tile);
		tile = GameObject.Instantiate(prefab, pos, Quaternion.Euler(-90, 0, 0));
		tile.AddComponent<TileBehaviour>().SetTileType(tileType, position);
	}

	public void SetTileParent(GameObject parent)
	{
		tile.transform.SetParent(parent.transform);
	}
}

public enum TileType
{
	room, undecided, hallway, start, end, wall, hallwayEnd, door, edge, roomWall
}
