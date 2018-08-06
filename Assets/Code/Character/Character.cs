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

	public event Action<Vector2DInt> OnCharacterSpawned;

	public void Initialize(string viewName, int playerID, string nickname, int skinID)
    {
		isMasterClient = PhotonNetwork.isMasterClient;
		photonView.RPC("NetworkInitialize", PhotonTargets.AllBuffered, viewName, playerID, nickname, skinID); // wont need be buffered later when level loading is synced
	}

	public void Spawn(Vector2DInt spawnTile)
	{
		photonView.RPC("NetworkSpawn", PhotonTargets.AllBuffered, spawnTile.x, spawnTile.y); // wont need be buffered later when level loading is synced
	}

	[PunRPC]
	void NetworkInitialize(string viewname, int playerID, string nickname, int skinID)
	{
		this.playerID  = playerID;
		playerNickname = nickname;

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

		// initialize components
		movementComponent.ManualAwake();
		flagComponent.ManualAwake();
		stateComponent.ManualAwake();
		soundComponent.ManualAwake(viewData, view.transform);
		ParticleComponent.ManualAwake(viewData, view.transform);		

		if (photonView.isMine)
			Match.instance.photonView.RPC("RegisterPlayer", PhotonTargets.AllViaServer, this.playerID, playerNickname);

#if DEBUG_TOOLS
		if (photonView.isMine)
			FindObjectOfType<PlayerPage>().Initialize(this);
#endif
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

	[PunRPC]
	void NetworkSpawn(int spawnTileX, int spawnTileY)
	{
		movementComponent.ResetAll();
		ParticleComponent.StopAll();
		soundComponent.StopSound(CharacterSoundComponent.CharacterSound.Charge);
		transform.position = new Vector3(spawnTileX, 1, spawnTileY);
		OnCharacterSpawned?.Invoke(new Vector2DInt(spawnTileX, spawnTileY));		
	}
		
	void Update()
	{
		if (!photonView.isMine || !Match.instance.matchStarted || stateComponent.currentState == CharacterState.Frozen)
			return;

		if (Input.GetButton(Constants.BUTTON_CHARGE))
			movementComponent.TryCharge();

		if (Input.GetAxisRaw(Constants.AXIS_VERTICAL) > 0)
			movementComponent.TryWalk(Vector2DInt.Up);
		if (Input.GetAxisRaw(Constants.AXIS_VERTICAL) < 0)
			movementComponent.TryWalk(Vector2DInt.Down);
		if (Input.GetAxisRaw(Constants.AXIS_HORIZONTAL) < 0)
			movementComponent.TryWalk(Vector2DInt.Left);
		if (Input.GetAxisRaw(Constants.AXIS_HORIZONTAL) > 0)
			movementComponent.TryWalk(Vector2DInt.Right);

		

#if DEBUG_TOOLS

		if (PhotonNetwork.isMasterClient && Input.GetKeyDown(KeyCode.P))
			movementComponent.InfiniteDash();

		if (Input.GetKeyDown(KeyCode.L))
			soundComponent.PlaySound(CharacterSoundComponent.CharacterSound.Dash);

#endif
	}
}
