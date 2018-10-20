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
	public void NetworkOnGettingDashed(int fromX, int fromY, int directionX, int directionY, int numDashtiles)
	{
		if (_stateComponent.currentState == CharacterState.Dead)
			return;

		// stop all potencial ongoing feedback
		_character.ParticleComponent.StopAll();
		_character.soundComponent.StopSound(CharacterSound.StunnedSound, 0.2f);

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
