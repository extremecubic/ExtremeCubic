﻿using System;
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
public class Character : Photon.MonoBehaviour
{		
	public CharacterModel model                {get; private set;}
	public GameObject view                     {get; private set;}
	public bool isMasterClient                 {get; private set;}
	public int playerID                        {get; private set;}
	public string playerNickname               {get; private set;}
	public CharacterDatabase.ViewData viewData {get; private set;}

    public CharacterMovementComponent  movementComponent {get; private set;}
    public CharacterFlagComponent      flagComponent     {get; private set;}
    public CharacterStateComponent     stateComponent    {get; private set;}
	public CharacterSoundComponent     soundComponent    {get; private set;}
	public CharacterParticlesComponent ParticleComponent {get; private set;}
	public CharacterDeathComponent	   deathComponent    {get; private set;}
	public CharacterPowerUpComponent   powerUpComponent  {get; private set;}

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
		view = Instantiate(viewData.prefab);
		view.transform.SetParent(transform, false);
		SetSkin(skinID);

		// gat components
		movementComponent = GetComponent<CharacterMovementComponent>();
		flagComponent     = GetComponent<CharacterFlagComponent>();
		stateComponent    = GetComponent<CharacterStateComponent>();
		soundComponent    = GetComponent<CharacterSoundComponent>();
		ParticleComponent = GetComponent<CharacterParticlesComponent>();
		deathComponent	  = GetComponent<CharacterDeathComponent>();
		powerUpComponent  = GetComponent<CharacterPowerUpComponent>();

		// initialize components
		movementComponent.ManualAwake();
		flagComponent.ManualAwake();
		stateComponent.ManualAwake();
		soundComponent.ManualAwake(viewData, view.transform);
		ParticleComponent.ManualAwake(viewData, view.transform);		

		if (Constants.onlineGame && photonView.isMine)				
			Match.instance.photonView.RPC("RegisterPlayer", PhotonTargets.AllViaServer, this.playerID, playerNickname);

		if (!Constants.onlineGame)
			Match.instance.RegisterPlayer(this.playerID, playerNickname);

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

	void SetSkin(int skinID)
	{
		Renderer renderer = view.GetComponent<Renderer>();
		if (renderer != null)
			renderer.material = viewData.materials[skinID];

		for (int i = 0; i < view.transform.childCount; i++)
		{
			renderer = view.transform.GetChild(i).GetComponent<Renderer>();
			if (renderer != null)
				renderer.material = viewData.materials[skinID];
		}
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

		if (Input.GetButton(Constants.BUTTON_CHARGE))
			movementComponent.TryCharge();

		if (Input.GetAxisRaw(Constants.AXIS_VERTICAL) > 0)
			movementComponent.TryWalk(invert == false ? Vector2DInt.Up : Vector2DInt.Down);
		if (Input.GetAxisRaw(Constants.AXIS_VERTICAL) < 0)
			movementComponent.TryWalk(invert == false ? Vector2DInt.Down : Vector2DInt.Up);
		if (Input.GetAxisRaw(Constants.AXIS_HORIZONTAL) < 0)
			movementComponent.TryWalk(invert == false ? Vector2DInt.Left : Vector2DInt.Right);
		if (Input.GetAxisRaw(Constants.AXIS_HORIZONTAL) > 0)
			movementComponent.TryWalk(invert == false ? Vector2DInt.Right : Vector2DInt.Left);

#if DEBUG_TOOLS
		if (PhotonNetwork.isMasterClient && Input.GetKeyDown(KeyCode.P))
			movementComponent.InfiniteDash();

		if (Input.GetKeyDown(KeyCode.L))
			soundComponent.PlaySound(CharacterSound.Dash);
#endif
	}

	void UpdateLocal()
	{
		if (Input.GetKeyDown(KeyCode.B) && playerID == 0)
			movementComponent.Die(movementComponent.currentTile.position.x, movementComponent.currentTile.position.y);
	}
}
