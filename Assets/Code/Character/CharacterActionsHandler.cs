using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

// this file handles receving and processeing possible character actions
// and then forwarding them to all clients that should have them
// it also checks if we are in a local and online game so we know
// if the actions only should be ran locally or not
public partial class CharacterMovementComponent : Photon.MonoBehaviour
{
	// called locally on "my local player" when receving input
	// and then sends it to all other clients
	public void OnTryWalk(Vector2DInt direction)
	{
		if (_stateComponent.currentState != CharacterState.Idle || _flagComponent.GetFlag(CharacterFlag.Cooldown_Walk))
			return;

		// cant walk to tile if occupied by other player or if not walkable tile
		Tile targetTile = currentTile.GetRelativeTile(direction);
		if (targetTile.IsOccupied() || !targetTile.model.data.walkable)
			return;

		if (Constants.onlineGame)
			photonView.RPC("NetworkWalk", PhotonTargets.All, currentTile.position.x, currentTile.position.y, direction.x, direction.y);

		if (!Constants.onlineGame)
			NetworkWalk(currentTile.position.x, currentTile.position.y, direction.x, direction.y);
	}

	// called locally on "my local player" and
	// only send message to all other clients to start
	// feedback of charge
	public void OnTryCharge()
	{
		if (_stateComponent.currentState != CharacterState.Idle || _flagComponent.GetFlag(CharacterFlag.Cooldown_Dash))
			return;

		// send to all other then me just for starting feedback, we start coroutine instead
		if (Constants.onlineGame)
			photonView.RPC("NetworkCharge", PhotonTargets.Others);

		Timing.RunCoroutineSingleton(_Charge(), gameObject.GetInstanceID(), SingletonBehavior.Overwrite);
	}

	// only called on server and then forwarded to
	// all other clients
	void OnDash(int fromX, int fromY, int directionX, int directionY, int dashCharges)
	{
		if (Constants.onlineGame)
			photonView.RPC("NetworkDash", PhotonTargets.All, fromX, fromY, directionX, directionY, dashCharges);

		if (!Constants.onlineGame)
			NetworkDash(fromX, fromY, directionX, directionY, dashCharges);
	}

	// only called on server and then forwarded to
	// all other clients
	public void OnGettingDashed(Vector2DInt startTile, Vector2DInt direction, int hitPower, int dashingPlayerID)
	{
		if (Constants.onlineGame)
			photonView.RPC("NetworkOnGettingDashed", PhotonTargets.All, startTile.x, startTile.y, direction.x, direction.y, hitPower, dashingPlayerID);

		if (!Constants.onlineGame)
			NetworkOnGettingDashed(startTile.x, startTile.y, direction.x, direction.y, hitPower, dashingPlayerID);
	}

	// only called on server and then forwarded to
	// all other clients
	public void OnDashingOther(Vector2DInt lastTile, Quaternion rot, Vector2DInt targetTile)
	{
		if (Constants.onlineGame)
			photonView.RPC("NetworkOnDashingOther", PhotonTargets.All, lastTile.x, lastTile.y, targetTile.x, targetTile.y, rot.x, rot.y, rot.z, rot.w);

		if (!Constants.onlineGame)
			NetworkOnDashingOther(lastTile.x, lastTile.y, targetTile.x, targetTile.y, rot.x, rot.y, rot.z, rot.w);
	}

	// called locally on all clients
	// but only server takes action
	void OnClaimPowerUp()
	{
		if (Constants.onlineGame && PhotonNetwork.isMasterClient)
			photonView.RPC("NetworkClaimPowerUp", PhotonTargets.All, currentTile.position.x, currentTile.position.y);

		if (!Constants.onlineGame)
			NetworkClaimPowerUp(currentTile.position.x, currentTile.position.y);
	}

	// called locally on all clients
	// but only server takes action of collide and send it to
	// all clients, the clients only freaze locally while waiting for conformation
	void OnHittingObstacle(Vector2DInt direction)
	{
		if (Constants.onlineGame)
		{
			if (PhotonNetwork.isMasterClient)
				photonView.RPC("NetworkOnhittingObstacle", PhotonTargets.All, currentTile.position.x, currentTile.position.y, direction.x, direction.y);
			else
				_character.stateComponent.SetState(CharacterState.Frozen);
		}

		if (!Constants.onlineGame)
			NetworkOnhittingObstacle(currentTile.position.x, currentTile.position.y, direction.x, direction.y);
	}

	// called locally on all clients
	// but only server takes action of the special tile functionality and send it to
	// all clients, the clients only freaze locally while waiting for conformation
	bool OnEnterSpecialTile()
	{
		if (currentTile.model.data.isSpecialTile)
		{
			// create empty targetTileCoords if this special tile need a target to do its thing
			// teleport is example of this, if not needing target these wont be used in specialTileHandler
			// but is always sent to avoid having to deal with specialtiles in two different ways
			Vector2DInt targetTile = new Vector2DInt(0, 0);

			// if need target, find non ocupied tile of same type
			// that is not the same as current
			if (currentTile.model.data.needTargetTileSameType)
				targetTile = _tileMap.GetRandomTileCoordsByType(currentTile.model.typeName, currentTile);

			if (Constants.onlineGame)
			{
				if (PhotonNetwork.isMasterClient)
					photonView.RPC("NetworkClaimSpecialTile", PhotonTargets.All, currentTile.position.x, currentTile.position.y, targetTile.x, targetTile.y);
				else
					_stateComponent.SetState(CharacterState.Frozen);
			}

			if (!Constants.onlineGame)
				NetworkClaimSpecialTile(currentTile.position.x, currentTile.position.y, targetTile.x, targetTile.y);

			return true;
		}
		return false;
	}

	// called locally on all clients
	// but only server takes action of killing the player and
	// send the information to all clients 
	bool OnDeadlyTile()
	{
		if (Constants.onlineGame && PhotonNetwork.isMasterClient && currentTile.model.data.deadly)
		{
			photonView.RPC("Die", PhotonTargets.All, currentTile.position.x, currentTile.position.y, PhotonNetwork.time);
			return true;
		}

		if (!Constants.onlineGame && currentTile.model.data.deadly)
		{
			Die(currentTile.position.x, currentTile.position.y, 0.0f);
			return true;
		}
		return false;
	}

	// called locally on all clients
	// server handeling killing the player
	// and clients freaze while waiting for the server
	bool OnDeadlyEdge()
	{
		if (currentTile.model.typeName == Constants.EDGE_TYPE)
		{
			// stop movement and flag frozen locally and only handle death on server
			// will be corrected by server if say a collision happened on server but not locally that would have prevented us from exiting map
			if (Constants.onlineGame)
			{
				if (PhotonNetwork.isMasterClient)
					photonView.RPC("Die", PhotonTargets.All, currentTile.position.x, currentTile.position.y, PhotonNetwork.time);
				else
					_stateComponent.SetState(CharacterState.Frozen);
			}

			if (!Constants.onlineGame)
				Die(currentTile.position.x, currentTile.position.y, 0.0f);

			return true;
		}
		return false;
	}

	// RPC:S THAT IS RUN ON ALL CLIENTS
	[PunRPC]
	void NetworkWalk(int fromX, int fromY, int directionX, int directionY)
	{
		if (_stateComponent.currentState == CharacterState.Dead)
			return;

		// set new current tile if desynced
		SetNewTileReferences(new Vector2DInt(fromX, fromY));

		Timing.RunCoroutineSingleton(_Walk(new Vector2DInt(directionX, directionY)), gameObject.GetInstanceID(), SingletonBehavior.Overwrite);
	}

	[PunRPC]
	void NetworkCharge()
	{
		if (_stateComponent.currentState == CharacterState.Dead)
			return;

		_stateComponent.SetState(CharacterState.Charging);

		// abort all current particleFeedBack
		_character.ParticleComponent.StopAll();

		// start feedback
		_character.ParticleComponent.EmitCharge(true);
		_character.soundComponent.PlaySound(CharacterSound.Charge);
	}

	[PunRPC]
	void NetworkDash(int fromX, int fromY, int directionX, int directionY, int dashCharges)
	{
		if (_stateComponent.currentState == CharacterState.Dead)
			return;

		// abort all current particleFeedBack
		_character.ParticleComponent.StopAll();

		// set new current tile if desynced
		SetNewTileReferences(new Vector2DInt(fromX, fromY));

		Timing.RunCoroutineSingleton(_Dash(new Vector2DInt(directionX, directionY), dashCharges), gameObject.GetInstanceID(), SingletonBehavior.Overwrite);
	}

	[PunRPC]
	public void NetworkOnGettingDashed(int fromX, int fromY, int directionX, int directionY, int numDashtiles, int dashingPlayerID)
	{
		if (_stateComponent.currentState == CharacterState.Dead)
			return;

		// stop all potencial ongoing feedback
		_character.ParticleComponent.StopAll();
		_character.soundComponent.StopSound(CharacterSound.StunnedSound, 0.2f);

		// set the id of the player that dashed into us
		// if it has a valid ID, invalid ID will be sent in from ex
		// the special speedtile that forces us to dash in
		// the same way as a regular dash collision work
		if (dashingPlayerID != Constants.INVALID_ID)
		   _character.dashingPlayerID = dashingPlayerID;

		// kill all coroutines on this layer
		Timing.KillCoroutines(gameObject.GetInstanceID());

		// set new current tile if desynced
		SetNewTileReferences(new Vector2DInt(fromX, fromY));

		// takes over the dashPower from the player that dashed into us
		Timing.RunCoroutineSingleton(_Dash(new Vector2DInt(directionX, directionY), numDashtiles, true), gameObject.GetInstanceID(), SingletonBehavior.Overwrite);
	}

	[PunRPC]
	void NetworkOnDashingOther(int fromX, int fromY, int targetX, int targetY, float rotX, float rotY, float rotZ, float rotW)
	{
		if (_stateComponent.currentState == CharacterState.Dead)
			return;

		// abort all current particleFeedBack
		_character.ParticleComponent.StopAll();

		// kill all coroutines on this layer
		Timing.KillCoroutines(gameObject.GetInstanceID());

		// play hit sound and spawn effect
		_character.soundComponent.PlaySound(CharacterSound.Punch);
		_character.ParticleComponent.SpawnHitEffect(new Vector2DInt(fromX, fromY), new Vector2DInt(targetX, targetY));

		// set new current tile if desynced
		SetNewTileReferences(new Vector2DInt(fromX, fromY));

		// lerp desync during cooldown
		Timing.RunCoroutineSingleton(_Correct(transform.position, new Vector3(fromX, 1, fromY), transform.rotation, new Quaternion(rotX, rotY, rotZ, rotW), _character.model.walkCooldown), gameObject.GetInstanceID(), SingletonBehavior.Overwrite);

		// set last target rotation to current rotation(we never started lerping towards target)
		_lastTargetRotation = new Quaternion(rotX, rotY, rotZ, rotW);

		// add cooldowns
		StopMovementAndAddCooldowns();

		// do Camerashake
		Match.instance.gameCamera.DoShake(_model.dashCameraShakeDuration, _model.dashCameraShakeSpeed, _model.dashCameraShakeIntensity, _model.dashCameraShakeIntensityDamping);

		// check if we got stopped on deadly tile(only server handles deathchecks)
		OnDeadlyTile();
	}

	[PunRPC]
	public void NetworkOnhittingObstacle(int tileX, int tileY, int directionX, int directionY)
	{
		// set new current tile if desynced
		SetNewTileReferences(new Vector2DInt(tileX, tileY));

		// the feedback when hitting the obstacle is stored in the obstacles "land properties"
		_tileMap.GetTile(new Vector2DInt(tileX + directionX, tileY + directionY)).OnPlayerLand();

		// stop potencial feedback
		_character.ParticleComponent.StopAll();

		// set position to this tile (no point in lerping here, will look worse if having alot of desync)
		transform.position = new Vector3(tileX, 1, tileY);

		// do Camerashake
		Match.instance.gameCamera.DoShake(_model.collideCameraShakeDuration, _model.collideCameraShakeSpeed, _model.collideCameraShakeIntensity, _model.collideCameraShakeIntensityDamping);

		Timing.RunCoroutineSingleton(_ObstacleCollide(new Vector2DInt(tileX, tileY), new Vector2DInt(directionX, directionY)), gameObject.GetInstanceID(), SingletonBehavior.Overwrite);
	}

	[PunRPC]
	public void Die(int tileX, int tileY, double delta)
	{
		// remove reference and set state
		currentTile.RemovePlayer();
		_stateComponent.SetState(CharacterState.Dead);

		// stop all possible feedback
		_character.soundComponent.StopAll();
		_character.ParticleComponent.StopAll();
		_character.powerUpComponent.AbortPowerUp();

		// play character death sound and get the tile we died on
		_character.soundComponent.PlaySound(CharacterSound.Death);
		Tile deathTile = _tileMap.GetTile(new Vector2DInt(tileX, tileY));

		// set position
		transform.position = new Vector3(deathTile.position.x, 1, deathTile.position.y);

		// play death feedback depending on tileType
		_character.deathComponent.KillPlayer(deathTile, delta);

		if (Constants.onlineGame && PhotonNetwork.isMasterClient)
			Match.instance.OnPlayerDie(_character.playerPhotonID, _character.dashingPlayerID);

		if (!Constants.onlineGame)
			Match.instance.OnPlayerDie(_character.playerPhotonID, _character.dashingPlayerID);
	}

	[PunRPC]
	void NetworkClaimPowerUp(int tileX, int tileY)
	{
		Tile tile = _tileMap.GetTile(new Vector2DInt(tileX, tileY));
		_character.powerUpComponent.RegisterPowerup(tile.ClaimPowerUp(), new Vector3(tileX, 1, tileY));
	}

	[PunRPC]
	void NetworkClaimSpecialTile(int tileX, int tileY, int targetTileX, int targetTileY)
	{
		Tile tile = _tileMap.GetTile(new Vector2DInt(tileX, tileY));
		_character.specialTileHandler.OnEnterSpecialTile(tile, new Vector2DInt(targetTileX,targetTileY));
	}

	[PunRPC]
	public void FinishCancelledDash(int fromX, int fromY, int directionX, int directionY, int dashCharges)
	{
		if (_stateComponent.currentState == CharacterState.Dead)
			return;

		// kill all coroutines on this layer
		Timing.KillCoroutines(gameObject.GetInstanceID());

		// set back tilereferences to the tile where we stopped
		SetNewTileReferences(new Vector2DInt(fromX, fromY));

		Timing.RunCoroutineSingleton(_Dash(new Vector2DInt(directionX, directionY), dashCharges, true), gameObject.GetInstanceID(), SingletonBehavior.Overwrite);
	}

	[PunRPC]
	void SyncTransform(int px, int py, float rx, float ry, float rz, float rw)
	{
		transform.position = new Vector3(px, 1, py);
		transform.rotation = new Quaternion(rx, ry, rz, rw);
	}

}
