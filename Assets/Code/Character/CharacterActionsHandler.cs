using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public partial class CharacterMovementComponent : Photon.MonoBehaviour
{
	[PunRPC]
	void NetworkWalk(int fromX, int fromY, int toX, int toY)
	{
		if (_stateComponent.currentState == CharacterState.Dead)
			return;

		Timing.RunCoroutineSingleton(_Walk(new Vector2DInt(fromX, fromY), new Vector2DInt(toX, toY)), gameObject.GetInstanceID(), SingletonBehavior.Overwrite);
	}

	[PunRPC]
	void NetworkCharge()
	{
		if (_stateComponent.currentState == CharacterState.Dead)
			return;

		_stateComponent.SetState(CharacterState.Charging);

		// start feedback
		_character.ParticleComponent.EmitCharge(true);
		_character.soundComponent.PlaySound(CharacterSound.Charge);
	}

	[PunRPC]
	void NetworkDash(int fromX, int fromY, int directionX, int directionY, int dashCharges)
	{
		if (_stateComponent.currentState == CharacterState.Dead)
			return;

		// set new current tile if desynced
		SetNewTileReferences(new Vector2DInt(fromX, fromY));

		Timing.RunCoroutineSingleton(_Dash(new Vector2DInt(directionX, directionY), dashCharges), gameObject.GetInstanceID(), SingletonBehavior.Overwrite);
	}

	[PunRPC]
	void NetworkOnGettingDashed(int fromX, int fromY, int directionX, int directionY, int numDashtiles)
	{
		if (_stateComponent.currentState == CharacterState.Dead)
			return;

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

		// stop trail emitter
		_character.ParticleComponent.EmitTrail(false, Vector3.zero);

		// add cooldowns
		StopMovementAndAddCooldowns();

		// check if we got stopped on deadly tile(only server handles deathchecks)
		OnDeadlyTile();
	}

	[PunRPC]
	public void Die(int tileX, int tileY)
	{
		// remove reference and set state
		currentTile.RemovePlayer();
		_stateComponent.SetState(CharacterState.Dead);

		// stop all possible feedback
		_character.soundComponent.PlaySound(CharacterSound.Death);
		_character.soundComponent.StopSound(CharacterSound.Charge);
		_character.ParticleComponent.StopAll();
		_character.powerUpComponent.AbortPowerUp();

		Tile deathTile = _tileMap.GetTile(new Vector2DInt(tileX, tileY));

		transform.position = new Vector3(deathTile.position.x, 1, deathTile.position.y);

		_character.deathComponent.KillPlayer(deathTile);

		if (Constants.onlineGame && PhotonNetwork.isMasterClient)
			Match.instance.OnPlayerDie(_character.playerID);

		if (!Constants.onlineGame)
			Match.instance.OnPlayerDie(_character.playerID);
	}

	[PunRPC]
	void NetworkClaimPowerUp(int tileX, int tileY)
	{
		Tile tile = _tileMap.GetTile(new Vector2DInt(tileX, tileY));
		_character.powerUpComponent.RegisterPowerup(tile.ClaimPowerUp(), new Vector3(tileX, 1, tileY));
	}

	[PunRPC]
	void NetworkClaimSpecialTile(int tileX, int tileY)
	{
		Tile tile = _tileMap.GetTile(new Vector2DInt(tileX, tileY));
		_character.specialTileHandler.OnEnterSpecialTile(tile);
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
