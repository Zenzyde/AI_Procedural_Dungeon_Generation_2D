using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class EditorTile
{
	[HideInInspector] public TileType tileType = TileType.wall;
	[HideInInspector] public GameObject tile;
	[HideInInspector] public Vector2Int position;

	public EditorTile(GameObject tile, Vector2Int position, GameObject parent)
	{
		this.tile = tile;
		this.position = position;
		tile.transform.SetParent(parent.transform);
	}

	public void ChangeType(TileType type, GameObject prefab, GameObject parent)
	{
		tileType = type;
		Vector3 pos = tile.transform.position;
		GameObject.DestroyImmediate(tile);
		tile = GameObject.Instantiate(prefab, pos, Quaternion.Euler(-90, 0, 0), parent.transform);
	}
}
