using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class TileVisualizer : MonoBehaviour
{
	[SerializeField, Range(5, 20)] int _sizeX = 5;
	[SerializeField, Range(5, 20)] int _sizeY = 5;

	[SerializeField] string _tileMapToShow;

	[SerializeField] TileDatabase _tileDatabase;
	[SerializeField] Material _gridMaterial;

	int _lastX = 5;
	int _lastY = 5;

	public void Show()
	{
		Clear();

		if (File.Exists(Path.Combine(Constants.TILEMAP_SAVE_FOLDER, _tileMapToShow)))
		{
			using (FileStream stream = new FileStream(Path.Combine(Constants.TILEMAP_SAVE_FOLDER, _tileMapToShow), FileMode.Open, FileAccess.Read))
			using (BinaryReader reader = new BinaryReader(stream))
			{
				int Y = reader.ReadInt32();        // Read: gridsize y
				int X = reader.ReadInt32();        // Read: gridsize x								

				for (int y = 0; y < Y; y++)
					for (int x = 0; x < X; x++)
					{
						Vector2DInt tilePosition = Vector2DInt.Zero;
						tilePosition.BinaryLoad(reader);       // Read: Position
						string typeName = reader.ReadString(); // Read: Tile type name  
						float yRot = reader.ReadSingle();
						float tintStrength = reader.ReadSingle();
			
						// dont spawn any tile if it is empty
						if (typeName == "empty")
							continue;

						GameObject model = null;

						for (int i = 0; i < _tileDatabase.tilesToSerialize.Count; i++)
						{
							if (_tileDatabase.tilesToSerialize[i].typeName == typeName.ToLower())
								model = _tileDatabase.tilesToSerialize[i].data.prefab;
						}

						// spawn new tile
						if (model == null)
						{
							print("model does not exist in tiledatabase");
							continue;
						}

						// spawn new tile
						GameObject tile = Instantiate(model, new Vector3(x, 0, y), model.transform.rotation * Quaternion.Euler(0, yRot, 0), transform);

						TintTile(tile, tintStrength);
					}
			}
		}
		else
			Debug.LogError(string.Format("level {0} was not found", _tileMapToShow));
	}

	public void Clear()
	{
		int numTiles = transform.childCount;
		for (int i = 0; i < numTiles; i++)			
				DestroyImmediate(transform.GetChild(0).gameObject);
	}

	public void ShowGrid()
	{
		Clear();
		
		// create gameobject and mesh
		GameObject grid = new GameObject("grid");
		Mesh mesh = new Mesh();

		float half = 0.5f;

		// add mesh filter and meshrenderer and assign them
		grid.AddComponent<MeshFilter>().mesh = mesh;
		grid.AddComponent<MeshRenderer>().material = _gridMaterial;

		// create vertex and index array
		Vector3[] vertices = new Vector3[(_sizeX * _sizeY) * 4];
		int[] indices = new int[(_sizeX * _sizeY) * 6];

		int column = 0;
		int tileCount = 0;
		int indexVertex = 0;
		int indexIndice = 0;

		// loop over and set all vertices and indices
		for (int i = 0; i < _sizeY * _sizeX; i++)
		{
			vertices[indexVertex + 0] = new Vector3(tileCount - half, -half, column + half); // top left
			vertices[indexVertex + 1] = new Vector3(tileCount + half, -half, column + half); // top right
			vertices[indexVertex + 2] = new Vector3(tileCount - half, -half, column - half); // bottom left
			vertices[indexVertex + 3] = new Vector3(tileCount + half, -half, column - half); // bottom right

			indices[indexIndice + 0] = indexVertex;
			indices[indexIndice + 1] = indexVertex + 1;
			indices[indexIndice + 2] = indexVertex + 2;
			indices[indexIndice + 3] = indexVertex + 2;
			indices[indexIndice + 4] = indexVertex + 1;
			indices[indexIndice + 5] = indexVertex + 3;

			tileCount++;
			indexVertex += 4;
			indexIndice += 6;
			if (tileCount == _sizeX)
			{
				column++;
				tileCount = 0;
			}
		}

		// assign vertices and indices
		mesh.vertices = vertices;
		mesh.triangles = indices;

		grid.transform.SetParent(transform);
	}

	public void CheckGrid()
	{
		if (_sizeX != _lastX)
			ShowGrid();

		if (_sizeY != _lastY)
			ShowGrid();

		_lastX = _sizeX;
		_lastY = _sizeY;
	}

	void TintTile(GameObject tile, float strength)
	{
		Renderer renderer = tile.GetComponent<Renderer>();
		if (renderer != null)
			renderer.sharedMaterial.color = Color.white * strength;

		for (int i = 0; i < tile.transform.childCount; i++)
		{
			renderer = tile.transform.GetChild(i).GetComponent<Renderer>();
			if (renderer != null)
				renderer.sharedMaterial.color = Color.white * strength;
		}
	}
}
