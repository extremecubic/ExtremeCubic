using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

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
    [SerializeField] Data   _data;     public Data data => _data;

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
		public bool changeColorTile;

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
	// private set properties
    public TileModel   model            { get; private set; }
	public Vector2DInt position         { get; private set; }
    public GameObject  view             { get; private set; }
	public int         currentHealth    { get; private set; } 
	public Character   lastCharacter    { get; private set; }
	public Character   currentCharacter { get; private set; }

	// members
    TileDatabase    _tileDB;
	SoundData[]     _sounds;
	PowerUpType     _powerUp = PowerUpType.None;
	GameObject      _powerView;
	Quaternion      _lastTargetRotation;
	CoroutineHandle _flipHandle;

	// set occupying charcater of this tile
	public void SetCharacter(Character character)
	{
		currentCharacter = character;
	}			
	
	// remove the current character and
	// set it as last occupying player
	public void RemovePlayer()
	{
		lastCharacter = currentCharacter;
		currentCharacter = null;
	}

	// return if we have a character set to this tile
	public bool IsOccupied()
	{
		return currentCharacter != null;
	}

	// get tile from coordinate offset
	public Tile GetRelativeTile(Vector2DInt offset)
	{
		return Match.instance.level.tileMap.GetTile(position + offset);
	}

	// destroy the model that represent this tile
	public void Delete(float delay)
	{
		Object.Destroy(view, delay);
	}

	// is a powerup set to this tile
	public bool ContainsPowerUp()
	{
		return _powerUp != PowerUpType.None;
	}

	// called from the tilemap that deserialize the levelmap files
	public Tile(Vector2DInt position, string tileName, float yRotation, float tintStrength, Transform tilesFolder)
    {
		_tileDB       = TileDatabase.instance;
		model         = _tileDB.GetTile(tileName);
		this.position = position;
		currentHealth = model.data.health;

		// create the model that reperesent this tile
		CreateView(position, yRotation, tintStrength, tilesFolder);		
		CreateSounds();
    }

	void CreateView(Vector2DInt position, float yRotation, float tintStrength, Transform tilesFolder)
	{
		// check so this tile has a model asigned to it
		// tiles can be invisible, ex destroyed or edge tiles
		if (model.data.prefab == null)
			return;

		// spawn model on correct tile coords
		view = Object.Instantiate(model.data.prefab, tilesFolder);
		view.transform.rotation = view.transform.rotation * Quaternion.Euler(new Vector3(0, yRotation, 0));
		view.transform.position = new Vector3(position.x, 0, position.y);

		_lastTargetRotation = view.transform.rotation;

		// tint the tile with the value saved from the tilemap editor
		TintTile(view, tintStrength);
	}

	// always check if tile contains a powerup before 
	// calling this method
	public PowerUpType ClaimPowerUp()
	{
		// return the type of powerup this tile contains
		PowerUpType power = _powerUp;

		// set powerup to none
		_powerUp = PowerUpType.None;

		// destroy the model representing the powerup
		Object.Destroy(_powerView);

		return power;
	}

	public void SpawnPowerUp(PowerUp power, Transform powerUpFolder)
	{
		// set powerup properties and spawn poweerup model
		_powerUp = power.type;
		_powerView = Object.Instantiate(power.prefab, new Vector3(position.x, 1, position.y), power.prefab.transform.rotation, powerUpFolder);

		// spawn one use sound for the powerup
		SoundManager.instance.SpawnAndPlaySound(power.spawnSound, 5);

		// spawn feedback particle for spawning if one have been asigned
		if (power.spawnParticle != null)
		{
			GameObject system = Object.Instantiate(power.spawnParticle, new Vector3(position.x, 0, position.y), power.spawnParticle.transform.rotation);
			Object.Destroy(system, 10);
		}
	}

	void CreateSounds()
	{
		// if this tile doesent have a model no sounds exist
		if (view == null)
			return;

		// create sound containers for all sounds
		_sounds = new SoundData[(int)TileSounds.Count];
		for (int i = 0; i < _sounds.Length; i++)
			_sounds[i] = new SoundData();

		SoundManager MM = SoundManager.instance;

		// the sound manager is responisble for spawning and assigning all sounds 
		// to the soundcontainer that is passed in
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

		// play all sounds through the soundmanager
		SoundManager MM = SoundManager.instance;
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

	public void DamageTile()
	{
		// ChangeColorTile health of this tile
		currentHealth--;

		// set break animation
		view.GetComponent<Animator>().SetInteger("health", currentHealth);

		PlaySound(TileSounds.Break);

		// spawn a break particle if one have been defined
		if (model.data.breakParticle != null)
		{
			GameObject p = Object.Instantiate(model.data.breakParticle, new Vector3(position.x, 0, position.y), model.data.breakParticle.transform.rotation);
			Object.Destroy(p, 8);
		}

		// if no health is left set a new empty tile on this coordinate
		// the model for the old tile will be destroyed with passed in delay
		// and the current tile will be garbage collected
		if (currentHealth == 0)
			Match.instance.level.tileMap.SetTile(position, new Tile(position, "empty", 0.0f, 0.0f, null), 1.0f);
	}

	// will get all renderers in a model
	// and tint the brightnes of the texture
	// NOTE THIS WILL NOT WORK WITH MODELS
	// THAT ALREADY USE THE COLOR PROPERTY TO CHANGE
	// THE COLOR OF THE TEXTURE, THIS WILL OVERIDE THAT COLOR
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

	// will get all renderers in a model
	// and change color of them all 
	public void ChangeColorTile(Color color)
	{
		// get renderer of main object and color it
		Renderer renderer = view.GetComponent<Renderer>();
		if (renderer != null)
			renderer.material.color = color;

		// loop over all child renderers and color them aswell
		for (int i = 0; i < view.transform.childCount; i++)
		{
			renderer = view.transform.GetChild(i).GetComponent<Renderer>();
			if (renderer != null)
				renderer.material.color = color;
		}
	}

	public void FlipAndChangeColorTile(Color color, int directionX, int directionY)
	{
		_flipHandle = Timing.RunCoroutineSingleton(_FlipTile(color, directionX, directionY), _flipHandle, SingletonBehavior.Overwrite);
	}

	// will flip the tile 90 degress and change the color
	IEnumerator<float> _FlipTile(Color color, int directionX, int directionY)
	{
		// get the relative tile that the character is moving towards
		Vector2DInt targetTilePos = GetRelativeTile(new Vector2DInt(directionX, directionY)).position;

		// get the direction from current tile to the target tile in normalized world space
		// then get the world axis that we want to rotate around based on the direction the character is moving
		Vector3 moveDirection = (new Vector3(targetTilePos.x, 1.0f, targetTilePos.y) - new Vector3(position.x, 1, position.y)).normalized;
		Vector3 rotationAxis  = new Vector3(moveDirection.z, moveDirection.y, -moveDirection.x);

		Quaternion fromRotation = view.transform.rotation;
		Quaternion toRotation = Quaternion.Euler(rotationAxis * 90.0f) * _lastTargetRotation;

		// save last target rotation in case another character would
		// need to rotate this tile before it has finished
		// the missing rotation will then be added on to the next rotation so it will catch up
		_lastTargetRotation = toRotation;

		GameModesModel model = Match.instance.gameModeModel;

		bool haveChangedColor = false;
		float fraction = 0;
		while (fraction < 1)
		{
			fraction += Time.deltaTime / model.tileFlipTime;
			fraction = Mathf.Clamp01(fraction);

			// change the color when we are a certain percent into the flip
			if (!haveChangedColor && fraction >= 1 * model.tileChangeColorIntoFlipPercent)
			{
				ChangeColorTile(color);
				haveChangedColor = true;
			}

			// offset position on y axis, this will make  our y position go from 0 to desired height and then 
			// back to 0 during the duration of the flip
			view.transform.position = new Vector3(view.transform.position.x, 0 + (model.tileFlipHeight * Mathf.Sin(fraction * Mathf.PI)), view.transform.position.z);
			view.transform.rotation = Quaternion.Lerp(fromRotation, toRotation, fraction);

			yield return Timing.WaitForOneFrame;
		}
	}
}



