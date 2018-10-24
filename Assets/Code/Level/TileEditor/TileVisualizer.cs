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

	Color _originalColor;

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
							if (_tileDatabase.tilesToSerialize[i].typeName == typeName)
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

		// add mesh filter and meshrenderer and assign them
		grid.AddComponent<MeshFilter>().mesh = MeshGenerator.Create2DGrid(_sizeX, _sizeY, 1.0f);
		grid.AddComponent<MeshRenderer>().material = _gridMaterial;

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
		{			
			renderer.sharedMaterial.color = Color.white * strength;
		}

		for (int i = 0; i < tile.transform.childCount; i++)
		{
			renderer = tile.transform.GetChild(i).GetComponent<Renderer>();
			if (renderer != null)
			{				
				renderer.sharedMaterial.color = Color.white * strength;
			}
		}
	}
}
