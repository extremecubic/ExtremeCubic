using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class TileMap
{
    readonly string name = "EditorTest";

    Dictionary<Vector2DInt, Tile> _tiles = new Dictionary<Vector2DInt, Tile>();

	Vector2DInt _gridSize;
	public readonly Transform tilesFolder;
	
    public TileMap(string mapName, Transform tilesFolder)
    {
        name = mapName;
		this.tilesFolder = tilesFolder;
        BinaryLoad();
    }

    public Tile GetTile(Vector2DInt position) => _tiles[position];
    public void SetTile(Vector2DInt position, Tile tile, float destroyDelay)
    {
        _tiles[position].Delete(destroyDelay);
        _tiles[position] = tile;
    }

	public Vector2DInt GetRandomTileCoords() =>	
		new Vector2DInt(Random.Range(0, _gridSize.x), Random.Range(0, _gridSize.y));
	  
    public void BinaryLoad()
    {
        using (FileStream stream = new FileStream(Path.Combine(Constants.TILEMAP_SAVE_FOLDER, name), FileMode.Open, FileAccess.Read))
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

	public void ClearTileViews()
	{
		for (int i = 0; i < tilesFolder.childCount; i++)
			Object.Destroy(tilesFolder.GetChild(i).gameObject);
	}

	public void ResetMap()
	{
		_tiles.Clear();
		BinaryLoad();
	}

	public Vector2DInt GetSpawnPointFromSpawnID(int id)
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



   


