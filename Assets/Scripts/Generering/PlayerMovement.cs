using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
	[SerializeField] private int visionRadius, lookingTime;
	[SerializeField] private Text timeText;

	private Maze maze;
	private TileBehaviour currentTile;
	private Camera cam;

	private float timeDelta = 0.12f, elapsedTime, currentLookingTime;
	private bool foundGoal = false, outOfTime;

	// Start is called before the first frame update
	void Start()
	{
		cam = Camera.main;
		visionRadius = visionRadius % 2 == 0 ? visionRadius + 1 : visionRadius;
		timeText.text = string.Format("<color=white>{0:00.00}</color>", currentLookingTime);
		currentLookingTime = lookingTime;
		maze = FindObjectOfType<Maze>();
		cam.GetComponent<Camera>().orthographicSize = 50;
		currentTile = maze.GetRndStartPosition();
		transform.position = currentTile.transform.position;
		cam.GetComponent<Camera>().orthographicSize = 3;
	}

	// Update is called once per frame
	void Update()
	{
		elapsedTime += Time.deltaTime;
		if (currentLookingTime > 0.0f && !outOfTime && !foundGoal)
			currentLookingTime -= Time.deltaTime;
		string color = outOfTime ? "red" : foundGoal ? "green" : "white";
		timeText.text = string.Format($"<color={color}>" + "{0:00.00} Seconds to find the exit</color>", currentLookingTime);
		if (currentLookingTime <= 0.0f && !foundGoal && !outOfTime)
			outOfTime = true;
		if (elapsedTime < timeDelta || outOfTime)
			return;


		if (Input.GetKey(KeyCode.S) && CanMoveTo(Vector2Int.up))
		{
			currentTile = maze.GetPosition(currentTile.position.x, currentTile.position.y, Vector2Int.up);
			transform.position = currentTile.transform.position;
			elapsedTime = 0;
		}

		if (Input.GetKey(KeyCode.W) && CanMoveTo(Vector2Int.down))
		{
			currentTile = maze.GetPosition(currentTile.position.x, currentTile.position.y, Vector2Int.down);
			transform.position = currentTile.transform.position;
			elapsedTime = 0;
		}

		if (Input.GetKey(KeyCode.A) && CanMoveTo(Vector2Int.left))
		{
			currentTile = maze.GetPosition(currentTile.position.x, currentTile.position.y, Vector2Int.left);
			transform.position = currentTile.transform.position;
			elapsedTime = 0;
		}

		if (Input.GetKey(KeyCode.D) && CanMoveTo(Vector2Int.right))
		{
			currentTile = maze.GetPosition(currentTile.position.x, currentTile.position.y, Vector2Int.right);
			transform.position = currentTile.transform.position;
			elapsedTime = 0;
		}
		Invisibility();
		Visibility();
		cam.transform.position = transform.position - Vector3.forward;
		if (CheckIfGoal())
			foundGoal = true;
	}

	bool CanMoveTo(Vector2Int dir)
	{
		return maze.GetPosition(currentTile.position.x, currentTile.position.y, dir).type != TileType.edge &&
			maze.GetPosition(currentTile.position.x, currentTile.position.y, dir).type != TileType.wall &&
			maze.GetPosition(currentTile.position.x, currentTile.position.y, dir).type != TileType.roomWall;
	}

	void Visibility()
	{
		for (int x = -visionRadius + 1; x < visionRadius; x++)
		{
			for (int y = -visionRadius + 1; y < visionRadius; y++)
			{
				Vector2Int dir = new Vector2Int(x, y);
				TileBehaviour tile = maze.GetPosition(currentTile.position.x, currentTile.position.y, dir);
				if (tile == null)
					continue;
				tile.ShowTileType();
			}
		}
	}

	void Invisibility()
	{
		for (int x = 0; x < maze.GetMazeSize().x; x++)
		{
			for (int y = 0; y < maze.GetMazeSize().y; y++)
			{
				TileBehaviour tile = maze.GetPosition(x, y, Vector2Int.zero);
				if (tile == null)
				{
					continue;
				}
				if (tile.position.x < currentTile.position.x + visionRadius && tile.position.x > currentTile.position.x - visionRadius &&
					tile.position.y < currentTile.position.y + visionRadius && tile.position.y > currentTile.position.y - visionRadius)
				{
					continue;
				}
				tile.HideTileTypeEdge();
			}
		}
	}

	bool CheckIfGoal()
	{
		return currentTile.type == TileType.end;
	}
}
