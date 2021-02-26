using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TileBehaviour : MonoBehaviour
{
	[SerializeField] public TileType type;
	[HideInInspector] [SerializeField] public Vector2Int position;
	[HideInInspector] [SerializeField] private MaterialPropertyBlock mpb;
	[HideInInspector] [SerializeField] private Renderer rend;

	public void SetTileType(TileType type, Vector2Int position)
	{
		this.type = type;
		this.position = position;
	}

	void OnEnable()
	{
		mpb = new MaterialPropertyBlock();
		rend = GetComponent<Renderer>();
	}

	public void ChangeType(TileType type)
	{
		rend.GetPropertyBlock(mpb);
		switch (type)
		{
			case TileType.room:
				mpb.SetColor("_Color", Color.cyan);
				rend.SetPropertyBlock(mpb);
				return;
			case TileType.hallway:
				mpb.SetColor("_Color", Color.red);
				rend.SetPropertyBlock(mpb);
				return;
			case TileType.hallwayEnd:
				mpb.SetColor("_Color", new Color(0.5f, 0, 0));
				rend.SetPropertyBlock(mpb);
				return;
			case TileType.wall:
				mpb.SetColor("_Color", Color.gray);
				rend.SetPropertyBlock(mpb);
				return;
			case TileType.start:
				mpb.SetColor("_Color", Color.green);
				rend.SetPropertyBlock(mpb);
				return;
			case TileType.end:
				mpb.SetColor("_Color", Color.yellow);
				rend.SetPropertyBlock(mpb);
				return;
			case TileType.door:
				mpb.SetColor("_Color", Color.black);
				rend.SetPropertyBlock(mpb);
				return;
			case TileType.edge:
				mpb.SetColor("_Color", new Color(0.2641509f, 0.2641509f, 0.2641509f));
				rend.SetPropertyBlock(mpb);
				return;
			case TileType.roomWall:
				mpb.SetColor("_Color", Color.magenta);
				rend.SetPropertyBlock(mpb);
				return;
		}
		rend.GetPropertyBlock(mpb);
	}

	public void ShowTileType()
	{
		rend.GetPropertyBlock(mpb);
		switch (type)
		{
			case TileType.room:
				mpb.SetColor("_Color", Color.cyan);
				rend.SetPropertyBlock(mpb);
				return;
			case TileType.hallway:
				mpb.SetColor("_Color", Color.red);
				rend.SetPropertyBlock(mpb);
				return;
			case TileType.hallwayEnd:
				mpb.SetColor("_Color", new Color(0.5f, 0, 0));
				rend.SetPropertyBlock(mpb);
				return;
			case TileType.wall:
				mpb.SetColor("_Color", Color.gray);
				rend.SetPropertyBlock(mpb);
				return;
			case TileType.start:
				mpb.SetColor("_Color", Color.green);
				rend.SetPropertyBlock(mpb);
				return;
			case TileType.end:
				mpb.SetColor("_Color", Color.yellow);
				rend.SetPropertyBlock(mpb);
				return;
			case TileType.door:
				mpb.SetColor("_Color", Color.black);
				rend.SetPropertyBlock(mpb);
				return;
			case TileType.edge:
				mpb.SetColor("_Color", new Color(0.2641509f, 0.2641509f, 0.2641509f));
				rend.SetPropertyBlock(mpb);
				return;
			case TileType.roomWall:
				mpb.SetColor("_Color", Color.magenta);
				rend.SetPropertyBlock(mpb);
				return;
		}
	}

	public void HideTileType()
	{
		rend.GetPropertyBlock(mpb);
		mpb.SetColor("_Color", Color.gray);
		rend.SetPropertyBlock(mpb);
	}

	public void HideTileTypeEdge()
	{
		rend.GetPropertyBlock(mpb);
		mpb.SetColor("_Color", Color.black);
		rend.SetPropertyBlock(mpb);
	}
}
