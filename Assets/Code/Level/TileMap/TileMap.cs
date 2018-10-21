using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class TileMap
{
    readonly string mapName = "EditorTest";

    Dictionary<Vector2DInt, Tile> _tiles = new Dictionary<Vector2DInt, Tile>();

	Vector2DInt _gridSize;
	public readonly Transform tilesFolder;
	public readonly Transform powerUpsFolder;
	
    public TileMap(string mapName, Transform tilesFolder, Transform powerUpsFolder)
    {
        this.mapName = mapName;
		this.tilesFolder = tilesFolder;
		this.powerUpsFolder = powerUpsFolder;
        BinaryLoad();
    }

	// return the tile at given tilecoordinates
	public Tile GetTile(Vector2DInt position)
	{
		return _tiles[position];
	} 

	// set a new tile at given coordinates and remove the old one
    public void SetTile(Vector2DInt position, Tile tile, float destroyDelay)
    {
        _tiles[position].Delete(destroyDelay);
        _tiles[position] = tile;
    }

	// will return a random tile coord inside the map
	// this can be to an empty or occupied tile aswell
	public Vector2DInt GetRandomTileCoords()
	{
		return new Vector2DInt(Random.Range(0, _gridSize.x), Random.Range(0, _gridSize.y));
	}
	  
	// load in the tile data from the level file
	// and create all tiles with the saved properties
    public void BinaryLoad()
    {
		if (File.Exists(Path.Combine(Constants.TILEMAP_SAVE_FOLDER, mapName)))
		{
			using (FileStream stream = new FileStream(Path.Combine(Constants.TILEMAP_SAVE_FOLDER, mapName), FileMode.Open, FileAccess.Read))
			using (BinaryReader reader = new BinaryReader(stream))
			{
				int gridSizeY = reader.ReadInt32();        // Read: Num tiles Vertical
				int gridSizeX = reader.ReadInt32();        // Read: Num tiles Horizontal

				int tileCount = gridSizeY * gridSizeX;        // Num tiles in total

				_gridSize = new Vector2DInt(gridSizeX, gridSizeY); // save gridsize if we need it later

				for (int i = 0; i < tileCount; i++)
				{
					Vector2DInt tilePosition = Vector2DInt.Zero;
					tilePosition.BinaryLoad(reader);       // Read: Position

					string typeName = reader.ReadString(); // Read: Tile type name  

					float yRot = reader.ReadSingle();

					float tintStrength = reader.ReadSingle();

					_tiles.Add(tilePosition, new Tile(tilePosition, typeName, yRot, tintStrength, tilesFolder));
				}

				AddEdgeTiles(gridSizeX, gridSizeY);
			}
		}		
    }

	// create a boarder of empty deadly edge tiles
	public void AddEdgeTiles(int sizeX, int sizeY)
	{
		// left edges
		for (int i = 0; i < sizeY; i++)
			_tiles.Add(new Vector2DInt(-1, i), new Tile(new Vector2DInt(-1, i), Constants.EDGE_TYPE, 0, 0, null));

		// right edges
		for (int i = 0; i < sizeY; i++)
			_tiles.Add(new Vector2DInt(sizeX, i), new Tile(new Vector2DInt(sizeX, i), Constants.EDGE_TYPE, 0, 0, null));

		// top edges
		for (int i = 0; i < sizeX; i++)
			_tiles.Add(new Vector2DInt(i, sizeY), new Tile(new Vector2DInt(i, sizeY), Constants.EDGE_TYPE, 0, 0, null));

		// bottom edges
		for (int i = 0; i < sizeX; i++)
			_tiles.Add(new Vector2DInt(i, -1), new Tile(new Vector2DInt(i, -1), Constants.EDGE_TYPE, 0, 0, null));
	}

	// remove all visual representation of tiles
	void ClearTileViews()
	{
		for (int i = 0; i < tilesFolder.childCount; i++)
			Object.Destroy(tilesFolder.GetChild(i).gameObject);
	}

	// remove all visual representation of powerups
	void ClearPowerUpViews()
	{
		for (int i = 0; i < powerUpsFolder.childCount; i++)
			Object.Destroy(powerUpsFolder.GetChild(i).gameObject);
	}

	// clear everything and reload map
	public void ResetMap()
	{
		ClearTileViews();
		ClearPowerUpViews();
		_tiles.Clear();
		BinaryLoad();
	}

	// tries to find a random tile within a certain amount of tries
	// deadly tiles is also considered empty
	public Tile GetRandomFreeTile(int numTries)
	{
		for(int i =0; i < numTries; i++)
		{
			Tile tile = GetTile(GetRandomTileCoords());
			if (!tile.IsOccupied() && !tile.ContainsPowerUp() && tile.model.data.walkable)
				return tile;
		}

		return null;
	}

	// this generates quite abit of garbage
	// but is run quite rarely so it should be OK?
	public Tile GetRandomFreeSpawnTile()
	{
		List<Tile> allFreeTiles = new List<Tile>();

		foreach (var tilePair in _tiles)
		{
			Tile tile = tilePair.Value;

			if (!tile.IsOccupied() && !tile.model.data.deadly)
				allFreeTiles.Add(tile);
		}

		if (allFreeTiles.Count == 0)
			return null;

		return allFreeTiles.TakeRandom();
	}

	public Vector2DInt GetRandomTileCoordsFromType(string tileType, Tile myTile)
	{
		// try to find a tile
		List<Tile> possibleTiles = new List<Tile>();

		foreach (KeyValuePair<Vector2DInt, Tile> tile in _tiles)		
			if (tile.Value.model.typeName == tileType && tile.Value != myTile && !tile.Value.IsOccupied())
				possibleTiles.Add(tile.Value);

		if (possibleTiles.Count > 0)
			return possibleTiles[Random.Range(0, possibleTiles.Count)].position;

		// no tile found, flag coords to magic numbers for invalid Tile coords
		// these are used to check if we can perform whatever functionality
		// we would like to do or not
		return Constants.NOT_FOUND_SPECIALTILE;
	}

	// get a spawnpoint based on the index this player got set
	// in the character select screen by looping over the
	// list of photon players on the master client and giving
	// them the index of thier order in list
	public Vector2DInt GetSpawnPointFromPlayerIndexID(int id)
	{
		Vector2DInt point;

		if (id == 0)
			point = new Vector2DInt(1, 1); // bottom left
		else if (id == 1)
			point = new Vector2DInt(1, _gridSize.y - 2); // top left
		else if (id == 2)
			point = new Vector2DInt(_gridSize.x - 2, _gridSize.y - 2); // top right
		else
			point = new Vector2DInt(_gridSize.x - 2, 1); // bottom right

		return point;
	}

}



   


