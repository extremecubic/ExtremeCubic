using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileDatabase : MonoBehaviour
{
    public static TileDatabase instance { get; private set; }

    public List<TileModel> tilesToSerialize = new List<TileModel>();        // Collection gathering data from the inspector to insert into "_tiles" and "_tileTypes" upon program start

    Dictionary<string, TileModel> _tiles = new Dictionary<string, TileModel>();
    List<string> _tileTypes = new List<string>();
	
    public void Awake()
    {
        instance = this;

        foreach (TileModel tileModelToSerialize in tilesToSerialize)
        {
            _tileTypes.Add(tileModelToSerialize.typeName.ToLower());
            _tiles.Add(tileModelToSerialize.typeName.ToLower(), tileModelToSerialize);
        }
		
		// the model that will represent a edgetile, this should not be exposed in the regular list becuase it is a constant tiletype that never should be edited and niether have to exist in editor
		TileModel edgeModel = new TileModel();
		edgeModel.MakeEdgeTile();
		_tileTypes.Add(edgeModel.typeName.ToLower());
		_tiles.Add(edgeModel.typeName.ToLower(), edgeModel);
    }

	void OnDestroy()
	{
		instance = null;	
	}

	public int tileCount => 
        _tiles.Count;

    public TileModel GetTile(string tileName) =>
        _tiles[tileName.ToLower()];

    public TileModel GetTile(int index) =>
        _tiles[_tileTypes[index]];

    public int GetTileTypeIndex(string name) =>
        _tileTypes.IndexOf(name);
}
