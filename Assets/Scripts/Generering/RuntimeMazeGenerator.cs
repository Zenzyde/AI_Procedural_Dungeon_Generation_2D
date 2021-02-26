using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;

// Inspiration: http://journal.stuffwithstuff.com/2014/12/21/rooms-and-mazes/ by Bob Nystrom
public class RuntimeMazeGenerator : MonoBehaviour
{
	[HideInInspector] [SerializeField] private Vector2Int mazeSize, minRoomSize, maxRoomSize;
	[HideInInspector] [SerializeField] private GameObject wallPrefab, edgePrefab, hallwayPrefab, roomPrefab, roomWallPrefab, startPrefab, endPrefab, doorPrefab;
	[HideInInspector] [SerializeField] private float brancingChance;
	[HideInInspector] [SerializeField] private int roomPlaceAttempts, doorPlaceAttempts, hallwaysToRemove;
	[HideInInspector] [SerializeField] private Maze.MazeAlgorithm mazeAlgorithm;

	[HideInInspector] public Tile[,] mazeGrid;
	[HideInInspector] public List<Tile> start = new List<Tile>(), end = new List<Tile>();

	private List<List<Tile[]>> rooms = new List<List<Tile[]>>();
	private List<Tile> hallways = new List<Tile>();
	private GameObject prefabParent;

	public void StartMazeGeneration(Maze settings)
	{
		this.mazeSize = settings.mazeSize;
		this.minRoomSize = settings.minRoomSize;
		this.maxRoomSize = settings.maxRoomSize;
		this.wallPrefab = settings.wallPrefab;
		this.edgePrefab = settings.edgePrefab;
		this.hallwayPrefab = settings.hallwayPrefab;
		this.roomPrefab = settings.roomPrefab;
		this.roomWallPrefab = settings.roomWallPrefab;
		this.startPrefab = settings.startPrefab;
		this.endPrefab = settings.endPrefab;
		this.doorPrefab = settings.doorPrefab;
		this.roomPlaceAttempts = settings.roomPlaceAttempts;
		this.brancingChance = settings.brancingChance;
		this.doorPlaceAttempts = settings.doorPlaceAttempts;
		this.mazeAlgorithm = settings.mazeAlgorithm;
		rooms = new List<List<Tile[]>>();
		hallways = new List<Tile>();
		SetupMazeGrid();
	}

	public void SaveMaze()
	{
		if (mazeGrid.Length == 0)
			return;
		string path = "Assets/Prefabs/Maze.prefab";
		bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path);
		if (!exists)
			PrefabUtility.SaveAsPrefabAsset(prefabParent, "Assets/Prefabs/Maze.prefab");
		else
		{
			for (int i = 0; i < 25; i++)
			{
				path = $"Assets/Prefabs/Maze_{i}.prefab";
				exists = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (!exists)
				{
					PrefabUtility.SaveAsPrefabAsset(prefabParent, $"Assets/Prefabs/Maze_{i}.prefab");
					break;
				}
				else
					continue;
			}
		}
	}

	public void DestroyMaze()
	{
		if (mazeGrid != null && mazeGrid.Length > 0)
		{
			foreach (Tile tile in mazeGrid)
			{
				GameObject.DestroyImmediate(tile.tile);
			}
			GameObject.DestroyImmediate(prefabParent);
			mazeGrid = new Tile[,] { };
			rooms.Clear();
			hallways.Clear();
		}
	}

	void SetupMazeGrid()
	{
		prefabParent = new GameObject("MazeParent");
		mazeGrid = new Tile[(int)mazeSize.x, (int)mazeSize.y];
		Vector2 position = (Vector2)transform.position - new Vector2(mazeSize.x, -mazeSize.y) / 2f;
		for (int i = 0; i < mazeSize.y; i++)
		{
			for (int j = 0; j < mazeSize.x; j++)
			{
				mazeGrid[j, i] = new Tile(Instantiate(wallPrefab, position + new Vector2(j, -i), Quaternion.Euler(-90, 0, 0)),
				new Vector2Int(j, i), prefabParent);
			}
		}
		CreateOuterWalls();
		SprinkleRooms();
		GenerateMazes();
		TryPlaceDoors();
		RemoveDeadEnds();
		RemoveDeadEndRooms();
		AddStartEndRooms();
	}

	void CreateOuterWalls()
	{
		for (int i = 0; i < mazeSize.y; i++)
		{
			for (int j = 0; j < mazeSize.x; j++)
			{
				if (mazeGrid[j, i].tileType == TileType.edge)
					continue;
				if (i == 0)
					mazeGrid[j, i].ChangeType(TileType.edge, edgePrefab, prefabParent);
				if (j == 0)
					mazeGrid[j, i].ChangeType(TileType.edge, edgePrefab, prefabParent);
				if (i == mazeSize.y - 1)
					mazeGrid[j, i].ChangeType(TileType.edge, edgePrefab, prefabParent);
				if (j == mazeSize.x - 1)
					mazeGrid[j, i].ChangeType(TileType.edge, edgePrefab, prefabParent);
			}
		}
	}

	void SprinkleRooms()
	{
		System.Random rnd = new System.Random();
		for (int i = 0; i < roomPlaceAttempts; i++)
		{
			Vector2Int start = new Vector2Int(
				rnd.Next(1, mazeSize.x),
				rnd.Next(1, mazeSize.y)
			);
			Vector2Int size = new Vector2Int(
				rnd.Next(minRoomSize.x, maxRoomSize.x),
				rnd.Next(minRoomSize.y, maxRoomSize.y)
			);
			TryPlaceRoom(start.x, start.y, size);
		}
	}

	bool TryPlaceRoom(int startX, int startY, Vector2Int size)
	{
		int endX = startX + size.x;
		if (endX >= mazeSize.x)
			return false;
		int endY = startY + size.y;
		if (endY >= mazeSize.y)
			return false;
		bool canFit = true;

		//Check if enough space for room
		for (int i = startY; i < endY; i++)
		{
			for (int j = startX; j < endX; j++)
			{
				//Center
				if (i > 0 && j > 0 && i < mazeSize.y && j < mazeSize.x && mazeGrid[j, i].tileType != TileType.wall)
					canFit = false;
				//Top row any wall or undecided?
				if (i - 1 > 0 && i == startY && (mazeGrid[j, i - 1].tileType != TileType.wall &&
					mazeGrid[j, i - 1].tileType != TileType.undecided))
					canFit = false;
				//Bottom Row any wall or undecided?
				if (i + 1 < mazeSize.y && i == endY - 1 && (mazeGrid[j, i + 1].tileType != TileType.wall &&
					mazeGrid[j, i + 1].tileType != TileType.undecided))
					canFit = false;
				//Left column any wall or undecided?
				if (j - 1 > 0 && j == startX && (mazeGrid[j - 1, i].tileType != TileType.wall &&
					mazeGrid[j - 1, i].tileType != TileType.undecided))
					canFit = false;
				//Right column any wall or undecided?
				if (j + 1 < mazeSize.x && j == endX - 1 && (mazeGrid[j + 1, i].tileType != TileType.wall &&
					mazeGrid[j + 1, i].tileType != TileType.undecided))
					canFit = false;

				//Diagonals
				//UpLeft
				if (i - 1 > 0 && j - 1 > 0 && i == startY && j == startX &&
					(mazeGrid[j - 1, i - 1].tileType != TileType.wall && mazeGrid[j - 1, i - 1].tileType != TileType.undecided))
					canFit = false;
				//UpRight
				if (i - 1 > 0 && j + 1 < mazeSize.x && i == startY && j == endX - 1 &&
					(mazeGrid[j + 1, i - 1].tileType != TileType.wall && mazeGrid[j + 1, i - 1].tileType != TileType.undecided))
					canFit = false;
				//DownLeft
				if (i + 1 < mazeSize.y && j - 1 > 0 && i == endY - 1 && j == startX &&
					(mazeGrid[j - 1, i + 1].tileType != TileType.wall && mazeGrid[j - 1, i + 1].tileType != TileType.undecided))
					canFit = false;
				//DownRight
				if (i + 1 < mazeSize.y && j + 1 < mazeSize.x && i == endY - 1 && j == endX - 1 &&
					(mazeGrid[j + 1, i + 1].tileType != TileType.wall && mazeGrid[j + 1, i + 1].tileType != TileType.undecided))
					canFit = false;
			}
		}
		if (!canFit)
			return false;
		List<Tile[]> room = new List<Tile[]>();

		int y = 0;
		//Create room
		for (int i = startY; i < endY; i++)
		{
			int x = 0;
			room.Add(new Tile[size.x]);
			for (int j = startX; j < endX; j++)
			{
				//DOORS & WALLS

				//Top left
				if (i == startY && j == startX && i > 1 && j > 1)
					mazeGrid[j - 1, i - 1].ChangeType(TileType.roomWall, roomWallPrefab, prefabParent);
				//Top right
				if (i == startY && j == endX - 1 && i > 1 && j < mazeSize.x - 1)
					mazeGrid[j + 1, i - 1].ChangeType(TileType.roomWall, roomWallPrefab, prefabParent);
				//Bottom left
				if (i == endY - 1 && j == startX && i < mazeSize.y - 1 && j > 1)
					mazeGrid[j - 1, i + 1].ChangeType(TileType.roomWall, roomWallPrefab, prefabParent);
				//Bottom right
				if (i == endY - 1 && j == endX - 1 && i < mazeSize.y - 1 && j < mazeSize.x - 1)
					mazeGrid[j + 1, i + 1].ChangeType(TileType.roomWall, roomWallPrefab, prefabParent);

				//Top row
				if (i == startY && i > 1)
				{
					mazeGrid[j, i - 1].ChangeType(TileType.roomWall, roomWallPrefab, prefabParent);
				}
				//Bottom row
				if (i == endY - 1 && i < mazeSize.y - 1)
				{
					mazeGrid[j, i + 1].ChangeType(TileType.roomWall, roomWallPrefab, prefabParent);
				}
				//Left column
				if (j == startX && j > 1)
				{
					mazeGrid[j - 1, i].ChangeType(TileType.roomWall, roomWallPrefab, prefabParent);
				}
				//Right column
				if (j == endX - 1 && j < mazeSize.x - 1)
				{
					mazeGrid[j + 1, i].ChangeType(TileType.roomWall, roomWallPrefab, prefabParent);
				}

				//ROOM
				mazeGrid[j, i].ChangeType(TileType.room, roomPrefab, prefabParent);
				room[y][x] = mazeGrid[j, i];
				x++;
			}
			y++;
		}
		rooms.Add(room);
		return true;
	}

	void TryPlaceDoors()
	{
		System.Random rnd = new System.Random();
		float chance = 0.2f;

		bool placedTop, placedBottom, placedRight, placedLeft;
		bool placedOppositeTop = false, placedOppositeBottom = false, placedOppositeLeft = false, placedOppositeRight = false;

		//X/List -> column, Y/Array -> row
		for (int i = 0; i < rooms.Count; i++)
		{
			List<Tile[]> room = rooms[i];
			placedTop = placedBottom = placedRight = placedLeft = false;
			for (int x = 0; x < room.Count; x++)
			{
				for (int y = 0; y < room[x].Length; y++)
				{
					Vector2Int pos = room[x][y].position;
					//Upwards doors
					if (rnd.NextDouble() > chance && CanPlaceDoor(pos.x, pos.y - 1, Vector2Int.up) && !placedTop)
					{
						mazeGrid[pos.x, pos.y - 1].ChangeType(TileType.door, doorPrefab, prefabParent);
						placedTop = true;
					}
					if (IsOppositeDoor(pos.x, pos.y - 1, Vector2Int.up) && !placedOppositeTop)
					{
						mazeGrid[pos.x, pos.y - 1].ChangeType(TileType.door, doorPrefab, prefabParent);
						mazeGrid[pos.x, pos.y - 2].ChangeType(TileType.door, doorPrefab, prefabParent);
						placedOppositeTop = true;
					}

					//Downwards doors
					if (rnd.NextDouble() > chance && CanPlaceDoor(pos.x, pos.y + 1, Vector2Int.down) && !placedBottom)
					{
						mazeGrid[pos.x, pos.y + 1].ChangeType(TileType.door, doorPrefab, prefabParent);
						placedBottom = true;
					}
					if (IsOppositeDoor(pos.x, pos.y + 1, Vector2Int.down) && !placedOppositeBottom)
					{
						mazeGrid[pos.x, pos.y + 1].ChangeType(TileType.door, doorPrefab, prefabParent);
						mazeGrid[pos.x, pos.y + 2].ChangeType(TileType.door, doorPrefab, prefabParent);
						placedOppositeBottom = true;
					}

					//Leftwards doors
					if (rnd.NextDouble() > chance && CanPlaceDoor(pos.x - 1, pos.y, Vector2Int.left) && !placedLeft)
					{
						mazeGrid[pos.x - 1, pos.y].ChangeType(TileType.door, doorPrefab, prefabParent);
						placedLeft = true;
					}
					if (IsOppositeDoor(pos.x - 1, pos.y, Vector2Int.left) && !placedOppositeLeft)
					{
						mazeGrid[pos.x - 1, pos.y].ChangeType(TileType.door, doorPrefab, prefabParent);
						mazeGrid[pos.x - 2, pos.y].ChangeType(TileType.door, doorPrefab, prefabParent);
						placedOppositeLeft = true;
					}

					//Rightwards doors
					if (rnd.NextDouble() > chance && CanPlaceDoor(pos.x + 1, pos.y, Vector2Int.right) && !placedRight)
					{
						mazeGrid[pos.x + 1, pos.y].ChangeType(TileType.door, doorPrefab, prefabParent);
						placedRight = true;
					}
					if (IsOppositeDoor(pos.x + 1, pos.y, Vector2Int.right) && !placedOppositeRight)
					{
						mazeGrid[pos.x + 1, pos.y].ChangeType(TileType.door, doorPrefab, prefabParent);
						mazeGrid[pos.x + 2, pos.y].ChangeType(TileType.door, doorPrefab, prefabParent);
						placedOppositeRight = true;
					}
				}
			}
		}
	}

	bool CanPlaceDoor(int x, int y, Vector2Int dir)
	{
		if (mazeGrid[x, y].tileType == TileType.door)
			return false;
		if (dir == Vector2Int.up)
		{
			bool state = mazeGrid[x - 1, y].tileType == TileType.roomWall &&
					mazeGrid[x + 1, y].tileType == TileType.roomWall &&
					mazeGrid[x, y - 1].tileType == TileType.hallway &&
					mazeGrid[x, y + 1].tileType == TileType.room;
			return state;
		}
		else if (dir == Vector2Int.down)
		{
			bool state = mazeGrid[x - 1, y].tileType == TileType.roomWall &&
					mazeGrid[x + 1, y].tileType == TileType.roomWall &&
					mazeGrid[x, y + 1].tileType == TileType.hallway &&
					mazeGrid[x, y - 1].tileType == TileType.room;
			return state;
		}
		else if (dir == Vector2Int.left)
		{
			bool state = mazeGrid[x, y - 1].tileType == TileType.roomWall &&
					mazeGrid[x, y + 1].tileType == TileType.roomWall &&
					mazeGrid[x - 1, y].tileType == TileType.hallway &&
					mazeGrid[x + 1, y].tileType == TileType.room;
			return state;
		}
		else if (dir == Vector2Int.right)
		{
			bool state = mazeGrid[x, y - 1].tileType == TileType.roomWall &&
					mazeGrid[x, y + 1].tileType == TileType.roomWall &&
					mazeGrid[x + 1, y].tileType == TileType.hallway &&
					mazeGrid[x - 1, y].tileType == TileType.room;
			return state;
		}
		return false;
	}

	bool IsOppositeDoor(int x, int y, Vector2Int dir)
	{
		if (dir == Vector2Int.up)
		{
			bool state = mazeGrid[x - 1, y].tileType == TileType.roomWall &&
					mazeGrid[x + 1, y].tileType == TileType.roomWall &&
					mazeGrid[x - 1, y - 1].tileType == TileType.roomWall &&
					mazeGrid[x + 1, y - 1].tileType == TileType.roomWall &&
					(mazeGrid[x, y - 1].tileType == TileType.door || mazeGrid[x, y - 1].tileType == TileType.roomWall) &&
					mazeGrid[x, y + 1].tileType == TileType.room;
			return state;
		}
		else if (dir == Vector2Int.down)
		{
			bool state = mazeGrid[x - 1, y].tileType == TileType.roomWall &&
					mazeGrid[x + 1, y].tileType == TileType.roomWall &&
					mazeGrid[x - 1, y + 1].tileType == TileType.roomWall &&
					mazeGrid[x + 1, y + 1].tileType == TileType.roomWall &&
					(mazeGrid[x, y + 1].tileType == TileType.door || mazeGrid[x, y + 1].tileType == TileType.roomWall) &&
					mazeGrid[x, y - 1].tileType == TileType.room;
			return state;
		}
		else if (dir == Vector2Int.left)
		{
			bool state = mazeGrid[x, y - 1].tileType == TileType.roomWall &&
					mazeGrid[x, y + 1].tileType == TileType.roomWall &&
					mazeGrid[x - 1, y - 1].tileType == TileType.roomWall &&
					mazeGrid[x - 1, y + 1].tileType == TileType.roomWall &&
					(mazeGrid[x - 1, y].tileType == TileType.door || mazeGrid[x - 1, y].tileType == TileType.roomWall) &&
					mazeGrid[x + 1, y].tileType == TileType.room;
			return state;
		}
		else if (dir == Vector2Int.right)
		{
			bool state = mazeGrid[x, y - 1].tileType == TileType.roomWall &&
					mazeGrid[x, y + 1].tileType == TileType.roomWall &&
					mazeGrid[x + 1, y - 1].tileType == TileType.roomWall &&
					mazeGrid[x + 1, y + 1].tileType == TileType.roomWall &&
					(mazeGrid[x + 1, y].tileType == TileType.door || mazeGrid[x + 1, y].tileType == TileType.roomWall) &&
					mazeGrid[x - 1, y].tileType == TileType.room;
			return state;
		}
		return false;
	}

	void RemoveDeadEndRooms()
	{
		//X/List -> column, Y/Array -> row
		for (int i = 0; i < rooms.Count; i++)
		{
			List<Tile[]> room = rooms[i];
			bool isDoor = false;
			//Check for any door
			for (int x = 0; x < room.Count; x++)
			{
				for (int y = 0; y < room[x].Length; y++)
				{
					Vector2Int pos = room[x][y].position;
					if (IsDoor(pos.x, pos.y, Vector2Int.up))
						isDoor = true;
					if (IsDoor(pos.x, pos.y, Vector2Int.down))
						isDoor = true;
					if (IsDoor(pos.x, pos.y, Vector2Int.left))
						isDoor = true;
					if (IsDoor(pos.x, pos.y, Vector2Int.right))
						isDoor = true;
				}
			}

			if (isDoor)
			{
				continue;
			}
			//No Doors, remove room
			for (int x = 0; x < room.Count; x++)
			{
				for (int y = 0; y < room[x].Length; y++)
				{
					Vector2Int pos = room[x][y].position;
					if (mazeGrid[pos.x, pos.y].tileType == TileType.edge)
						continue;
					mazeGrid[pos.x, pos.y].ChangeType(TileType.start, startPrefab, prefabParent);

					//Side walls
					if (x == 0 && mazeGrid[pos.x, pos.y - 1].tileType != TileType.edge)
						mazeGrid[pos.x, pos.y - 1].ChangeType(TileType.wall, wallPrefab, prefabParent);
					if (y == 0 && mazeGrid[pos.x - 1, pos.y].tileType != TileType.edge)
						mazeGrid[pos.x - 1, pos.y].ChangeType(TileType.wall, wallPrefab, prefabParent);
					if (x >= room.Count - 1 && mazeGrid[pos.x, pos.y + 1].tileType != TileType.edge)
						mazeGrid[pos.x, pos.y + 1].ChangeType(TileType.wall, wallPrefab, prefabParent);
					if (y >= room[x].Length - 1 && mazeGrid[pos.x + 1, pos.y].tileType != TileType.edge)
						mazeGrid[pos.x + 1, pos.y].ChangeType(TileType.wall, wallPrefab, prefabParent);

					//Corner walls
					if (x == 0 && y == 0 && mazeGrid[pos.x - 1, pos.y - 1].tileType != TileType.edge) //Upper left
						mazeGrid[pos.x - 1, pos.y - 1].ChangeType(TileType.wall, wallPrefab, prefabParent);
					if (x == 0 && y >= room[x].Length - 1 && mazeGrid[pos.x + 1, pos.y - 1].tileType != TileType.edge) //Upper right
						mazeGrid[pos.x + 1, pos.y - 1].ChangeType(TileType.wall, wallPrefab, prefabParent);
					if (x >= room.Count - 1 && y == 0 && mazeGrid[pos.x - 1, pos.y + 1].tileType != TileType.edge) //Lower left
						mazeGrid[pos.x - 1, pos.y + 1].ChangeType(TileType.wall, wallPrefab, prefabParent);
					if (x >= room.Count - 1 && y >= room[x].Length - 1 && mazeGrid[pos.x + 1, pos.y + 1].tileType != TileType.edge) //Lower right
						mazeGrid[pos.x + 1, pos.y + 1].ChangeType(TileType.wall, wallPrefab, prefabParent);

					mazeGrid[pos.x, pos.y].ChangeType(TileType.wall, wallPrefab, prefabParent);
				}
			}
			rooms.RemoveAt(i);
		}
	}

	bool IsDoor(int x, int y, Vector2Int dir)
	{
		if (dir == Vector2Int.up)
		{
			bool state = mazeGrid[x, y - 1].tileType == TileType.door;
			return state;
		}
		else if (dir == Vector2Int.down)
		{
			bool state = mazeGrid[x, y + 1].tileType == TileType.door;
			return state;
		}
		else if (dir == Vector2Int.left)
		{
			bool state = mazeGrid[x - 1, y].tileType == TileType.door;
			return state;
		}
		else if (dir == Vector2Int.right)
		{
			bool state = mazeGrid[x + 1, y].tileType == TileType.door;
			return state;
		}
		return false;
	}

	void AddStartEndRooms()
	{
		System.Random rnd = new System.Random();
		int startIndex = rnd.Next(rooms.Count);
		//X/List -> column, Y/Array -> row
		List<Tile[]> startRoom = rooms[startIndex];
		//Check for any door
		for (int x = 0; x < startRoom.Count; x++)
		{
			for (int y = 0; y < startRoom[x].Length; y++)
			{
				Vector2Int pos = startRoom[x][y].position;
				start.Add(mazeGrid[pos.x, pos.y]);
				mazeGrid[pos.x, pos.y].ChangeType(TileType.start, startPrefab, prefabParent);
			}
		}
		int endIndex = rnd.Next(rooms.Count);
		while (endIndex == startIndex)
		{
			endIndex = rnd.Next(rooms.Count);
		}
		//X/List -> column, Y/Array -> row
		List<Tile[]> endRoom = rooms[endIndex];
		//Check for any door
		for (int x = 0; x < endRoom.Count; x++)
		{
			for (int y = 0; y < endRoom[x].Length; y++)
			{
				Vector2Int pos = endRoom[x][y].position;
				end.Add(mazeGrid[pos.x, pos.y]);
				mazeGrid[pos.x, pos.y].ChangeType(TileType.end, endPrefab, prefabParent);
			}
		}

		foreach (Tile tile in mazeGrid)
		{
			tile.SetTileParent(prefabParent);
		}
	}

	void GenerateMazes()
	{
		for (int i = 1; i < mazeSize.y; i += 2)
		{
			for (int j = 1; j < mazeSize.x; j += 2)
			{
				Vector2Int position = new Vector2Int(j, i);
				if (mazeGrid[j, i].tileType != TileType.wall)
					continue;
				DigMazePath(position);
			}
		}
	}

	// Inspiration: Growing Tree Algorithm: http://www.astrolog.org/labyrnth/algrithm.htm
	// Similar to Growing Tree if always going from the newest Tile added
	// Similar to Recursive Backtracker if always going from the last Tile added
	// Could be similar to Prim's Algorithm if going from a random Tile in the list each time
	void DigMazePath(Vector2Int pos)
	{
		System.Random rnd = new System.Random();
		mazeGrid[pos.x, pos.y].ChangeType(TileType.hallway, hallwayPrefab, prefabParent);

		List<Tile> cells = new List<Tile>();
		Vector2Int lastDir = Vector2Int.zero;
		cells.Add(mazeGrid[pos.x, pos.y]);

		Vector2Int[] cardinalDirections = new Vector2Int[]
		{
			Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
		};

		while (cells.Count > 0)
		{
			Tile current = mazeAlgorithm == Maze.MazeAlgorithm.rogueGrowingTree ? cells[0] : cells[cells.Count - 1];
			hallways.Add(current);

			List<Vector2Int> openDirections = new List<Vector2Int>();

			for (int i = 0; i < cardinalDirections.Length; i++)
			{
				Vector2Int intPos = current.position + cardinalDirections[i] * 2;
				if (intPos.x > 0 && intPos.x < mazeSize.x && intPos.y > 0 && intPos.y < mazeSize.y &&
					mazeGrid[intPos.x, intPos.y].tileType == TileType.wall)
				{
					openDirections.Add(cardinalDirections[i]);
				}
			}

			if (openDirections.Count > 0)
			{
				Vector2Int dir = Vector2Int.zero;
				if (openDirections.Contains(lastDir) && rnd.NextDouble() > brancingChance)
				{
					dir = lastDir;
				}
				else
				{
					dir = openDirections[rnd.Next(openDirections.Count)];
				}

				Vector2Int nextPos = current.position + dir;
				Tile first = mazeGrid[nextPos.x, nextPos.y];
				first.ChangeType(TileType.hallway, hallwayPrefab, prefabParent);
				nextPos = current.position + dir * 2;
				Tile second = mazeGrid[nextPos.x, nextPos.y];
				second.ChangeType(TileType.hallway, hallwayPrefab, prefabParent);

				cells.Add(second);
				lastDir = dir;
			}
			else
			{
				cells.Remove(current);

				lastDir = Vector2Int.zero;
			}
		}
	}

	void RemoveDeadEnds()
	{
		bool done = false;
		int shrinkageX = 1, shrinkageY = 1;
		Vector2Int[] cardinalDirections = new Vector2Int[]
		{
			Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
		};
		int hallwaysLeft = hallwaysToRemove;
		while (!done)
		{
			done = true;
			bool possibleWall = false;
			for (int i = shrinkageY; i < mazeSize.y - shrinkageY; i++)
			{
				for (int j = shrinkageX; j < mazeSize.x - shrinkageX; j++)
				{
					if (mazeGrid[j, i].tileType == TileType.wall || mazeGrid[j, i].tileType == TileType.room ||
						mazeGrid[j, i].tileType == TileType.door || mazeGrid[j, i].tileType == TileType.roomWall)
						continue;
					int exits = 0;
					foreach (Vector2Int dir in cardinalDirections)
					{
						Vector2Int intPos = mazeGrid[j, i].position + dir;
						if (mazeGrid[intPos.x, intPos.y].tileType == TileType.hallway ||
							mazeGrid[intPos.x, intPos.y].tileType == TileType.door)
						{
							exits++;
						}
					}

					if (exits != 1)
					{
						continue;
					}

					possibleWall = true;
					done = false;
					mazeGrid[j, i].ChangeType(TileType.wall, wallPrefab, prefabParent);
					hallwaysLeft--;
				}
			}
			if (shrinkageX <= mazeSize.x)
				shrinkageX++;
			if (shrinkageY <= mazeSize.y)
				shrinkageY++;
			if (shrinkageX <= mazeSize.x || shrinkageY <= mazeSize.y)
			{
				done = false;
				if (hallwaysLeft > 0 && possibleWall)
					shrinkageX = shrinkageY = 1;
			}
		}
	}
}
