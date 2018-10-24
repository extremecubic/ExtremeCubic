using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level : Photon.MonoBehaviour
{
	public TileMap tileMap { get; private set; }

	[SerializeField] string     _mapToLoad;
	[SerializeField] Transform  _tilesFolder;
	[SerializeField] Transform  _powerUpFolder;
	[SerializeField] GameObject _characterPrefab;

	[Header("LEVEL SPECIFIC DEATH FEEDBACK ON EDGES AND EMPTY TILES")]
	[SerializeField] ParticleSystem _emptyDeathParticle;   public ParticleSystem emptyDeathParticle { get { return _emptyDeathParticle; } }
	[SerializeField] AudioClip      _emptyDeathSound;      public AudioClip emptyDeathsound         { get { return _emptyDeathSound; } }
	[SerializeField] DeathType      _deathType;            public DeathType deathType               { get { return _deathType; } }
	[SerializeField] Transform      _flyToTargetTransform; public Transform flyToTargetTransform    { get { return _flyToTargetTransform; } }

	// always keep players in a list to be able
	// to keep it consistent between local and online play
	// even if we only have one pleyer in online play
    List<Character> _characters = new List<Character>();

	public void StartGameOnline()
	{
		// get player properties saved in photonplayer
		string characterName = PhotonNetwork.player.CustomProperties[Constants.CHARACTER_NAME].ToString();
		int skinID           = (int)PhotonNetwork.player.CustomProperties[Constants.SKIN_ID];
		int spawnID          = (int)PhotonNetwork.player.CustomProperties[Constants.SPAWN_ID];
		_mapToLoad           = (string)PhotonNetwork.player.CustomProperties[Constants.LEVEL_MAP_NAME];

		tileMap = new TileMap(_mapToLoad, _tilesFolder, _powerUpFolder);

		// in online play we only have one character
		// located at index 0
		_characters.Add(PhotonNetwork.Instantiate("Character", Vector3.zero, Quaternion.identity, 0).GetComponent<Character>());
		_characters[0].Initialize(characterName, PhotonNetwork.player.ID, PhotonNetwork.player.NickName, skinID, spawnID);
		_characters[0].Spawn();
	}

	public void StartGameLocal()
	{
		// just spawn 4 duplo right now
		// this will change once menus for starting local play is done
		tileMap = new TileMap(_mapToLoad, _tilesFolder, _powerUpFolder);

		for(int i =0; i< 4; i++)
		{
			_characters.Add(Instantiate(_characterPrefab, Vector3.zero, Quaternion.identity).GetComponent<Character>());
			_characters[i].Initialize("duplo", i, "LocalGuy", 0, i);
			_characters[i].Spawn();
		}		
	}

	public void ResetRound()
	{
		if (Constants.onlineGame)
		    photonView.RPC("NetworkResetRound", PhotonTargets.All);

		if (!Constants.onlineGame)
			NetworkResetRound();
	}

	// break a tile, this is only handled by the master client
	public void BreakTile(int x, int y)
	{
		if (Constants.onlineGame && PhotonNetwork.isMasterClient)
			photonView.RPC("NetworkBreakTile", PhotonTargets.All, x, y);

		if (!Constants.onlineGame)
			NetworkBreakTile(x, y);
	}

	// change the color of a tile, this is only handled by the master client
	public void ChangeColorTile(int currentTileX, int currentTileY, int directionX, int directionY)
	{
		Tile tile = tileMap.GetTile(new Vector2DInt(currentTileX, currentTileY));

		// get the current occupying player on tile and get the
		// photon id for score setting and the indexId for setting the color
		int currentPlayerPhotonID = tile.currentCharacter.playerPhotonID;
		int currentPlayerIndexID  = tile.currentCharacter.playerIndexID;

		// start lastPlayerPhoton id to invalid if we are the
		// first player that enters this tile
		int lastPlayerPhotonID = Constants.INVALID_ID;

		// if not null get the photon id of the last player so
		// we can decrease this players score
		if (tile.lastCharacter != null)
			lastPlayerPhotonID = tile.lastCharacter.playerPhotonID;

		if (Constants.onlineGame && PhotonNetwork.isMasterClient)
			photonView.RPC("NetworkChangeColorTile", PhotonTargets.All, currentTileX, currentTileY, directionX, directionY, lastPlayerPhotonID, currentPlayerPhotonID, currentPlayerIndexID);

		if (!Constants.onlineGame)
			NetworkChangeColorTile(currentTileX, currentTileY, directionX, directionY, lastPlayerPhotonID, currentPlayerPhotonID, lastPlayerPhotonID);
	}

	// spawn all characters on thier spawnpoints
	[PunRPC]
	void NetworkResetRound()
	{
		tileMap.ResetMap();
		for (int i =0; i < _characters.Count; i++)
		     _characters[i].Spawn();
	}

	[PunRPC]
	void NetworkBreakTile(int x, int y)
	{
		tileMap.GetTile(new Vector2DInt(x, y)).DamageTile();
	}

	[PunRPC]
	void NetworkChangeColorTile(int currentTileX, int currentTileY, int directionX, int directionY, int lastPlayerPhotonID, int currentPlayerPhotonID, int currentPlayerIndexID)
	{
		GameModesModel modeModel = Match.instance.gameModeModel;

		Tile tile = tileMap.GetTile(new Vector2DInt(currentTileX, currentTileY));

		tile.FlipAndChangeColorTile(modeModel.GetColorFromPlayerIndexID(currentPlayerIndexID), directionX, directionY);
		Match.instance.OnTileChangingColor(lastPlayerPhotonID, currentPlayerPhotonID);
	}
}
