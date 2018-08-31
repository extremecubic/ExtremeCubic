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
	public CharacterModel model                {get; private set;}
	public GameObject view                     {get; private set;}
	public bool isMasterClient                 {get; private set;}
	public int playerID                        {get; private set;}
	public string playerNickname               {get; private set;}
	public CharacterDatabase.ViewData viewData {get; private set;}

    public CharacterMovementComponent  movementComponent  {get; private set;}
    public CharacterFlagComponent      flagComponent      {get; private set;}
    public CharacterStateComponent     stateComponent     {get; private set;}
	public CharacterSoundComponent     soundComponent     {get; private set;}
	public CharacterParticlesComponent ParticleComponent  {get; private set;}
	public CharacterDeathComponent	   deathComponent     {get; private set;}
	public CharacterPowerUpComponent   powerUpComponent   {get; private set;}
	public CharacterSpecialTileHandler specialTileHandler {get; private set;}

	int _spawnPoint;

	public void Initialize(string viewName, int playerID, string nickname, int skinID, int spawnPoint)
    {
		if (Constants.onlineGame)
		    photonView.RPC("NetworkInitialize", PhotonTargets.AllBuffered, viewName, playerID, nickname, skinID, spawnPoint); // wont need be buffered later when level loading is synced

		if (!Constants.onlineGame)
			NetworkInitialize(viewName, playerID, nickname, skinID, spawnPoint);
	}

	public void Spawn()
	{
		if (Constants.onlineGame)
			photonView.RPC("NetworkSpawn", PhotonTargets.AllBuffered); // wont need be buffered later when level loading is synced

		if (!Constants.onlineGame)
			NetworkSpawn();
	}

	[PunRPC]
	void NetworkInitialize(string viewname, int playerID, string nickname, int skinID, int spawnPoint)
	{
		this.playerID  = playerID;
		playerNickname = nickname;
		_spawnPoint    = spawnPoint;

		model    = CharacterDatabase.instance.standardModel;
		viewData = CharacterDatabase.instance.GetViewFromName(viewname);

		// Setup the correct view, probably in a view component	
		view = Instantiate(viewData.prefabs[skinID]);
		view.transform.SetParent(transform, false);

		// gat components
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

		if (Constants.onlineGame && photonView.isMine)				
			Match.instance.photonView.RPC("RegisterPlayer", PhotonTargets.AllViaServer, this.playerID, playerNickname, viewname);

		if (!Constants.onlineGame)
			Match.instance.RegisterPlayer(this.playerID, playerNickname, viewname);

#if DEBUG_TOOLS
		if (Constants.onlineGame && photonView.isMine)
		{
			isMasterClient = PhotonNetwork.isMasterClient;
			FindObjectOfType<PlayerPage>().Initialize(this);
		}
#endif
	}

	[PunRPC]
	void NetworkSpawn()
	{
		Vector2DInt spawnTile = Level.instance.tileMap.GetSpawnPointFromSpawnID(_spawnPoint);

		movementComponent.ResetAll();
		ParticleComponent.StopAll();
		powerUpComponent.AbortPowerUp();
		soundComponent.StopSound(CharacterSound.Charge);
		transform.position = new Vector3(spawnTile.x, 1, spawnTile.y);
		movementComponent.SetSpawnTile(spawnTile);
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

		bool invert = powerUpComponent.invertControlls;

		if (Input.GetButton(Constants.BUTTON_CHARGE + playerID.ToString()))
			movementComponent.OnTryCharge();

		if (Input.GetAxisRaw(Constants.AXIS_VERTICAL + playerID.ToString()) > 0)
			movementComponent.OnTryWalk(invert == false ? Vector2DInt.Up : Vector2DInt.Down);
		if (Input.GetAxisRaw(Constants.AXIS_VERTICAL + playerID.ToString()) < 0)
			movementComponent.OnTryWalk(invert == false ? Vector2DInt.Down : Vector2DInt.Up);
		if (Input.GetAxisRaw(Constants.AXIS_HORIZONTAL + playerID.ToString()) < 0)
			movementComponent.OnTryWalk(invert == false ? Vector2DInt.Left : Vector2DInt.Right);
		if (Input.GetAxisRaw(Constants.AXIS_HORIZONTAL + playerID.ToString()) > 0)
			movementComponent.OnTryWalk(invert == false ? Vector2DInt.Right : Vector2DInt.Left);
	}
}
