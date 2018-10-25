using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterMovementComponent))]
[RequireComponent(typeof(CharacterFlagComponent))]
[RequireComponent(typeof(CharacterStateComponent))]
[RequireComponent(typeof(CharacterSoundComponent))]
[RequireComponent(typeof(CharacterParticlesComponent))]
[RequireComponent(typeof(CharacterDeathComponent))]
[RequireComponent(typeof(CharacterPowerUpComponent))]
[RequireComponent(typeof(CharacterSpecialTileHandler))]
public class Character : Photon.MonoBehaviour
{		
	// read only properties
	public CharacterModel model                {get; private set;}
	public GameObject view                     {get; private set;}
	public bool isMasterClient                 {get; private set;}
	public int playerPhotonID                  {get; private set;} // in local play this will be set to the same as "playerIndexID"
	public string playerNickname               {get; private set;}
	public CharacterDatabase.ViewData viewData {get; private set;}
	public int playerIndexID                   {get; private set;}

	// all character components
	public CharacterMovementComponent  movementComponent  {get; private set;}
    public CharacterFlagComponent      flagComponent      {get; private set;}
    public CharacterStateComponent     stateComponent     {get; private set;}
	public CharacterSoundComponent     soundComponent     {get; private set;}
	public CharacterParticlesComponent ParticleComponent  {get; private set;}
	public CharacterDeathComponent	   deathComponent     {get; private set;}
	public CharacterPowerUpComponent   powerUpComponent   {get; private set;}
	public CharacterSpecialTileHandler specialTileHandler {get; private set;}

	public void Initialize(string viewName, int playerID, string nickname, int skinID, int indexID)
    {
		if (Constants.onlineGame)
		    photonView.RPC("NetworkInitialize", PhotonTargets.All, viewName, playerID, nickname, skinID, indexID); 

		if (!Constants.onlineGame)
			NetworkInitialize(viewName, playerID, nickname, skinID, indexID);
	}

	public void Spawn()
	{
		if (Constants.onlineGame)
			photonView.RPC("NetworkSpawn", PhotonTargets.All); 

		if (!Constants.onlineGame)
			NetworkSpawn();
	}

	[PunRPC]
	void NetworkInitialize(string viewname, int playerID, string nickname, int skinID, int indexID)
	{
		playerPhotonID  = playerID;
		playerNickname       = nickname;
		playerIndexID        = indexID;

		model    = CharacterDatabase.instance.standardModel;
		viewData = CharacterDatabase.instance.GetViewFromName(viewname);

		// Setup the correct view based on the name of model
		// and the index ID of the skin
		view = Instantiate(viewData.prefabs[skinID]);
		view.transform.SetParent(transform, false);

		// get components
		movementComponent  = GetComponent<CharacterMovementComponent>();
		flagComponent      = GetComponent<CharacterFlagComponent>();
		stateComponent     = GetComponent<CharacterStateComponent>();
		soundComponent     = GetComponent<CharacterSoundComponent>();
		ParticleComponent  = GetComponent<CharacterParticlesComponent>();
		deathComponent	   = GetComponent<CharacterDeathComponent>();
		powerUpComponent   = GetComponent<CharacterPowerUpComponent>();
		specialTileHandler = GetComponent<CharacterSpecialTileHandler>();

		// initialize components
		movementComponent.ManualAwake();
		flagComponent.ManualAwake();
		stateComponent.ManualAwake();
		soundComponent.ManualAwake(viewData, view.transform);
		ParticleComponent.ManualAwake(viewData, view.transform);
		specialTileHandler.ManualAwake(this);

		// register this player in match class fro score kepping
		// and for in game UI
		if (Constants.onlineGame && photonView.isMine)				
			Match.instance.photonView.RPC("RegisterPlayer", PhotonTargets.AllViaServer, this.playerPhotonID, playerIndexID, playerNickname, viewname);

		if (!Constants.onlineGame)
			Match.instance.RegisterPlayer(playerIndexID, playerIndexID, playerNickname, viewname);

#if DEBUG_TOOLS
		if (Constants.onlineGame && photonView.isMine)
		{
			isMasterClient = PhotonNetwork.isMasterClient;
			FindObjectOfType<PlayerPage>().Initialize(this);
		}
#endif
	}

	// called when all players respawn on thier start tiles when a round is over
	[PunRPC]
	void NetworkSpawn()
	{
		Vector2DInt spawnTile = Match.instance.level.tileMap.GetSpawnPointFromPlayerIndexID(playerIndexID);

		// abort possible movement and feedback
		movementComponent.ResetAll();
		ParticleComponent.StopAll();
		powerUpComponent.AbortPowerUp();
		soundComponent.StopAll();

		// set new position
		transform.position = new Vector3(spawnTile.x, 1, spawnTile.y);
		movementComponent.SetSpawnTile(spawnTile);
	}

	// called when respawning in middle of a game on an empty tile
	[PunRPC]
	public void ReSpawn(int tileX, int tileY)
	{
		movementComponent.ResetAll();
		transform.position = new Vector3(tileX, 1, tileY);
		movementComponent.SetSpawnTile(new Vector2DInt(tileX, tileY));
	}

	void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
	{		
		if(photonView.isMine)
		   Match.instance.OnPlayerLeft(otherPlayer.ID);
	}
		
	void Update()
	{
		if (Constants.onlineGame)
			UpdateOnline();

		if (!Constants.onlineGame)
			UpdateLocal();
	}

	void UpdateOnline()
	{
		if (!photonView.isMine || !Match.instance.matchStarted || stateComponent.currentState == CharacterState.Frozen)
			return;

		// handle input and call appropiate actions
		// in online play we always use ID 0 for input
		bool invert = powerUpComponent.invertControlls;

		if (Input.GetButton(Constants.BUTTON_CHARGE + "0"))
			movementComponent.OnTryCharge();

		if (Input.GetAxisRaw(Constants.AXIS_VERTICAL + "0") > 0)
			movementComponent.OnTryWalk(invert == false ? Vector2DInt.Up : Vector2DInt.Down);
		if (Input.GetAxisRaw(Constants.AXIS_VERTICAL + "0") < 0)
			movementComponent.OnTryWalk(invert == false ? Vector2DInt.Down : Vector2DInt.Up);
		if (Input.GetAxisRaw(Constants.AXIS_HORIZONTAL + "0") < 0)
			movementComponent.OnTryWalk(invert == false ? Vector2DInt.Left : Vector2DInt.Right);
		if (Input.GetAxisRaw(Constants.AXIS_HORIZONTAL + "0") > 0)
			movementComponent.OnTryWalk(invert == false ? Vector2DInt.Right : Vector2DInt.Left);

#if DEBUG_TOOLS
		if (PhotonNetwork.isMasterClient && Input.GetKeyDown(KeyCode.P))
			movementComponent.InfiniteDash();

		if (Input.GetKeyDown(KeyCode.L))
			soundComponent.PlaySound(CharacterSound.Dash);
#endif
	}

	void UpdateLocal()
	{
		if (!Match.instance.matchStarted)
			return;

		// handle input of our character by the 
		// controller index that this player is using
		bool invert = powerUpComponent.invertControlls;

		if (Input.GetButton(Constants.BUTTON_CHARGE + playerIndexID.ToString()))
			movementComponent.OnTryCharge();

		if (Input.GetAxisRaw(Constants.AXIS_VERTICAL + playerIndexID.ToString()) > 0)
			movementComponent.OnTryWalk(invert == false ? Vector2DInt.Up : Vector2DInt.Down);
		if (Input.GetAxisRaw(Constants.AXIS_VERTICAL + playerIndexID.ToString()) < 0)
			movementComponent.OnTryWalk(invert == false ? Vector2DInt.Down : Vector2DInt.Up);
		if (Input.GetAxisRaw(Constants.AXIS_HORIZONTAL + playerIndexID.ToString()) < 0)
			movementComponent.OnTryWalk(invert == false ? Vector2DInt.Left : Vector2DInt.Right);
		if (Input.GetAxisRaw(Constants.AXIS_HORIZONTAL + playerIndexID.ToString()) > 0)
			movementComponent.OnTryWalk(invert == false ? Vector2DInt.Right : Vector2DInt.Left);
	}
}
