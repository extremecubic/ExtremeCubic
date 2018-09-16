using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// tilesounds
public enum TileSounds
{
	Land,
	Break,
	Kill,
	Special,
	FailedSpecial,

	Count,
}

public enum SpecialTile
{
	PowerDash,
	Teleport,
}

// settings of a tile (this is exposed to the editor from TileDatabase.cs to create custom Tiletypes)
[System.Serializable]
public class TileModel
{
    [SerializeField] string _typeName; public string typeName => _typeName;
    [SerializeField] Data _data; public Data data => _data;

    [System.Serializable]
    public struct Data           
    {
		[Header("BASIC SETTINGS"),Space(3)]
        public bool walkable;       // Can a player ever enter this tile?
        public int  health;         // How many times can a player step on this tile?
        public bool deadly;         // Will a player die if it steps on this tile?
        public bool unbreakable;    // tile cant break 
		public DeathType deathType; // what death scenario will play
		public bool replaceTileOnDeath;
		public string replacementTile;

		[Header("SPECIAL TILE SETTINGS")]
		public bool        isSpecialTile;
		public SpecialTile specialType;
		public bool        needTargetTileSameType;
		[Tooltip("Used for \"PowerDash\" = num tiles to dash\n")]
		public int         intValue;

		[Header("SOUNDS"), Space(3)]
		public AudioClip   landSound;
		public AudioClip   breakSound;
		public AudioClip   killSound;
		public AudioClip   specialTileSound;
		public AudioClip   failedSpecialTileSound;

		[Header("PARTICLES"), Space(3)]
		public GameObject landParticle;
		public GameObject breakParticle;
		public GameObject killParticle;
		public GameObject enterSpecialParticle;
		public GameObject targetSpecialParticle;
		public GameObject enterSpecialFailedParticle;

		[Header("MODEL PREFAB")]
		public GameObject prefab;
    }  

	// this tiletype is created at runtime from Tiledatabase.cs 
	public void MakeEdgeTile()
	{
		_typeName = Constants.EDGE_TYPE;
		_data = new Data();

		_data.prefab = null;
		_data.walkable = true;
		_data.health = 0;
		_data.deadly = true;
		_data.unbreakable = true;
		_data.deathType = DeathType.Sink;
	}
}

public class Tile
{
    public readonly TileModel model;
	public readonly Vector2DInt position;

	public int currentHealth { get; private set; } = 0;

    TileDatabase _tileDB;
	SoundData[] _sounds;

    public GameObject view { get; private set; }
	Character  _character;

	PowerUpType _powerUp = PowerUpType.None;
	GameObject  _powerView;

	public void SetCharacter(Character character) =>
		_character = character;

	public void RemovePlayer() =>
		_character = null;

	public bool IsOccupied() =>
		_character != null;

	public Character GetOccupyingPlayer() =>
		 _character;

	public Tile GetRelativeTile(Vector2DInt offset) =>
		Level.instance.tileMap.GetTile(position + offset);

	public void Delete(float delay) =>
		Object.Destroy(view, delay);

	public bool ContainsPowerUp() =>
		_powerUp != PowerUpType.None;

	public Tile(Vector2DInt position, string tileName, float yRotation, float tintStrength, Transform tilesFolder)
    {
		_tileDB       = TileDatabase.instance;
		model         = _tileDB.GetTile(tileName);
		this.position = position;
		currentHealth = model.data.health;

		CreateView(position, yRotation, tintStrength, tilesFolder);		
		CreateSounds();
    }

	void CreateView(Vector2DInt position, float yRotation, float tintStrength, Transform tilesFolder)
	{
		if (model.data.prefab == null)
			return;

		view = Object.Instantiate(model.data.prefab, tilesFolder);
		view.transform.rotation = view.transform.rotation * Quaternion.Euler(new Vector3(0, yRotation, 0));
		view.transform.position = new Vector3(position.x, 0, position.y);

		TintTile(view, tintStrength);
	}

	public PowerUpType ClaimPowerUp()
	{
		PowerUpType power = _powerUp;

		_powerUp = PowerUpType.None;
		Object.Destroy(_powerView);

		return power;
	}

	public void SpawnPowerUp(PowerUp power, Transform powerUpFolder)
	{
		_powerUp = power.type;
		_powerView = Object.Instantiate(power.prefab, new Vector3(position.x, 1, position.y), power.prefab.transform.rotation, powerUpFolder);

		MusicManager.instance.SpawnAndPlaySound(power.spawnSound, 5);

		if (power.spawnParticle != null)
		{
			GameObject system = Object.Instantiate(power.spawnParticle, new Vector3(position.x, 0, position.y), power.spawnParticle.transform.rotation);
			Object.Destroy(system, 10);
		}
	}

	void CreateSounds()
	{
		if (view == null)
			return;

		_sounds = new SoundData[(int)TileSounds.Count];
		for (int i = 0; i < _sounds.Length; i++)
			_sounds[i] = new SoundData();

		MusicManager MM = MusicManager.instance;

		MM.CreateSound(_sounds[(int)TileSounds.Land],          "LandSound",        model.data.landSound             , false, view.transform);
		MM.CreateSound(_sounds[(int)TileSounds.Break],         "BreakSound",       model.data.breakSound            , false, view.transform);
		MM.CreateSound(_sounds[(int)TileSounds.Kill],          "KillSound",        model.data.killSound             , false, view.transform);
		MM.CreateSound(_sounds[(int)TileSounds.Special],       "SpecialSound",     model.data.specialTileSound      , false, view.transform);
		MM.CreateSound(_sounds[(int)TileSounds.FailedSpecial], "SpecialSoundFail", model.data.failedSpecialTileSound, false, view.transform);
	}

	// play a sound belonging to the tile as child
	public void PlaySound(TileSounds type)
	{
		if (view == null)
			return;

		MusicManager MM = MusicManager.instance;
		MM.PlaySound(_sounds[(int)type]);
	}

	public void OnPlayerLand()
	{
		PlaySound(TileSounds.Land);

		// spawn land particle if tile has one defined
		if (model.data.landParticle != null)
		{
			GameObject p = Object.Instantiate(model.data.landParticle, new Vector3(position.x, 0, position.y), model.data.landParticle.transform.rotation);
			Object.Destroy(p, 5);
		}
	}

	public void TintTile(GameObject tile, float strength)
	{
		// get renderer of main object and tint
		Renderer renderer = tile.GetComponent<Renderer>();
		if (renderer != null)
			renderer.material.color = Color.white * strength;

		// loop over all child renderers and tint
		for (int i = 0; i < tile.transform.childCount; i++)
		{
			renderer = tile.transform.GetChild(i).GetComponent<Renderer>();
			if (renderer != null)
				renderer.material.color = Color.white * strength;
		}
	}

	public void DamageTile()
	{
		currentHealth--;

		view.GetComponent<Animator>().SetInteger("health", currentHealth);

		PlaySound(TileSounds.Break);

		if (model.data.breakParticle != null)
		{
			GameObject p = Object.Instantiate(model.data.breakParticle, new Vector3(position.x, 0, position.y), model.data.breakParticle.transform.rotation);
			Object.Destroy(p, 8);
		}

		if (currentHealth == 0)
			Level.instance.tileMap.SetTile(position, new Tile(position, "empty", 0.0f, 0.0f, null), 1.0f);
	}
}



