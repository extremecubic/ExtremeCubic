using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using Photon;

public class CharacterMovementComponent : Photon.MonoBehaviour
{
	public Tile currentTile       { get; private set; }
	public int currentDashCharges { get; private set; }

	Character _character;
	CharacterModel _model;

	CharacterStateComponent _stateComponent;
	CharacterFlagComponent _flagComponent;

	public Vector2DInt _lastMoveDirection = Vector2DInt.Up;

	CollisionTracker _collisionTracker;

	Quaternion _lastTargetRotation; // used to keep track of the rotation the player last was targeting when getting interupted by getting hit
	
	public void ManualAwake()
	{
		_character = GetComponent<Character>();
		_model = _character.model;

		_stateComponent = _character.stateComponent;
		_flagComponent = _character.flagComponent;

		_collisionTracker = FindObjectOfType<CollisionTracker>();

		_lastTargetRotation = transform.rotation;

		_character.OnCharacterSpawned += (Vector2DInt inSpawnTile) =>
		{
			currentTile = Match.instance.level.tileMap.GetTile(inSpawnTile);
			currentTile.SetCharacter(_character);
		};
	}

	void OnDestroy()
	{
		Timing.KillCoroutines(gameObject.GetInstanceID());
	}

	#region LOCAL FUNCTION CALLS
	public void TryWalk(Vector2DInt direction)
	{
		if (_stateComponent.currentState != CharacterState.Idle || _flagComponent.GetFlag(CharacterFlag.Cooldown_Walk))
			return;

		// cant walk to tile if occupied by other player or if not walkable tile
		Tile targetTile = currentTile.GetRelativeTile(direction);
		if (targetTile.IsOccupied() || !targetTile.model.data.walkable)
			return;

		photonView.RPC("NetworkWalk", PhotonTargets.All, currentTile.position.x, currentTile.position.y, targetTile.position.x, targetTile.position.y);
	}

	public void TryCharge()
	{
		if (_stateComponent.currentState != CharacterState.Idle || _flagComponent.GetFlag(CharacterFlag.Cooldown_Dash))
			return;

		photonView.RPC("NetworkCharge", PhotonTargets.Others); // send to all other then me, we start coroutine instead

		Timing.RunCoroutineSingleton(_Charge(), gameObject.GetInstanceID(), SingletonBehavior.Overwrite);
	}

	public void OnGettingDashed(Vector2DInt startTile, Vector2DInt direction, int hitPower)
	{
		photonView.RPC("NetworkOnGettingDashed", PhotonTargets.All, startTile.x, startTile.y, direction.x, direction.y, hitPower);
	}

	public void OnDashingOther(Vector2DInt lastTile, Quaternion rot, Vector2DInt targetTile)
	{
		photonView.RPC("NetworkOnDashingOther", PhotonTargets.All, lastTile.x, lastTile.y, targetTile.x, targetTile.y, rot.x, rot.y, rot.z, rot.w);
	}

	public void ResetAll()
	{
		Timing.KillCoroutines(gameObject.GetInstanceID());
		_stateComponent.SetState(CharacterState.Idle);

		transform.rotation = Quaternion.Euler(Vector3.zero);
		_lastTargetRotation = transform.rotation;
	}

	void StopMovementAndAddCooldowns()
	{
		// reset state and add cooldowns
		_stateComponent.SetState(CharacterState.Idle);
		_flagComponent.SetFlag(CharacterFlag.Cooldown_Walk, true, _model.walkCooldown, SingletonBehavior.Overwrite);
		_flagComponent.SetFlag(CharacterFlag.Cooldown_Dash, true, _model.dashCooldown, SingletonBehavior.Overwrite);
	}

	void SetNewTileReferences(Vector2DInt tile)
	{
		// remove old reference and set to new
		currentTile.RemovePlayer();
		currentTile = Match.instance.level.tileMap.GetTile(tile);
		currentTile.SetCharacter(_character);
	}

	bool DeadlyTile()
	{
		if (currentTile.model.data.deadly)
			photonView.RPC("Die", PhotonTargets.All, currentTile.position.x, currentTile.position.y);

		return currentTile.model.data.deadly;
	}

	bool DeadlyEdge()
	{
		return currentTile.model.typeName == Constants.EDGE_TYPE;
	}
	#endregion

	#region NETWORK FUNCTION CALLS
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
		_character.soundComponent.PlaySound(CharacterSoundComponent.CharacterSound.Charge);		
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

		Timing.RunCoroutineSingleton(_Dash(new Vector2DInt(directionX, directionY), numDashtiles, true), gameObject.GetInstanceID(), SingletonBehavior.Overwrite); // takes over the dashPower from the player that dashed into us
	}

	[PunRPC]
	void NetworkOnDashingOther(int fromX, int fromY, int targetX, int targetY, float rotX, float rotY, float rotZ, float rotW)
	{
		if (_stateComponent.currentState == CharacterState.Dead)
			return;

		// kill all coroutines on this layer
		Timing.KillCoroutines(gameObject.GetInstanceID());

		// play hit sound and spawn effect
		_character.soundComponent.PlaySound(CharacterSoundComponent.CharacterSound.Punch);
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
		if(PhotonNetwork.isMasterClient)
		   DeadlyTile();
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
	void Die(int tileX, int tileY)
	{
		// remove reference and set state
		currentTile.RemovePlayer();
		_stateComponent.SetState(CharacterState.Dead);

		// stop all possible feedback
		_character.soundComponent.PlaySound(CharacterSoundComponent.CharacterSound.Death);
		_character.soundComponent.StopSound(CharacterSoundComponent.CharacterSound.Charge);
		_character.ParticleComponent.StopAll();

		transform.position = new Vector3(tileX, 1, tileY);

		_character.deathComponent.KillPlayer(currentTile.position, currentTile.model.data.deathType);

		if (PhotonNetwork.isMasterClient)
			Match.instance.OnPlayerDie(_character.playerID, photonView.viewID);
	}	

	[PunRPC]
	void SyncTransform(int px, int py, float rx, float ry, float rz, float rw)
	{
		transform.position = new Vector3(px, 1, py);
		transform.rotation = new Quaternion(rx, ry, rz, rw);
	}
	#endregion

	#region MOVEMENT ROUTINES
	IEnumerator<float> _Walk(Vector2DInt fromTilePos, Vector2DInt toTilePos)
	{
		_stateComponent.SetState(CharacterState.Walking);

		_character.soundComponent.PlaySound(CharacterSoundComponent.CharacterSound.Walk);

		// only handle tilebreaks on server
		if (PhotonNetwork.isMasterClient && !currentTile.model.data.unbreakable)
			Match.instance.level.BreakTile(currentTile.position.x, currentTile.position.y);

		TileMap tileMap = Match.instance.level.tileMap;

		// get references to tiles
		Tile fromTile = tileMap.GetTile(fromTilePos);
		Tile targetTile = tileMap.GetTile(toTilePos);		

		// Calculate lerp positions
		// lerp from current position (will catch up if laging)
		Vector3 fromPosition   = new Vector3(transform.position.x, 1, transform.position.z);
		Vector3 targetPosition = new Vector3(targetTile.position.x, 1, targetTile.position.y);

		// Calculate lerp rotations
		// get the movement direction based on vector between starttile and endtile
		// flip x and z to get the correct rotation in worldspace
		Vector3 movementDirection = (targetPosition - new Vector3(fromTile.position.x, 1, fromTile.position.y)).normalized;
		Vector3 movementDirectionRight = new Vector3(movementDirection.z, movementDirection.y, -movementDirection.x);

		// do lerp from current rotation if desynced(will catch up)
		// target is calculated using the target rotation we had during last movement if we are lagging behind
		// this prevents crooked target rotations
		Quaternion fromRotation = transform.rotation;
		Quaternion targetRotation = Quaternion.Euler(movementDirectionRight * 90) * _lastTargetRotation;

		// save our target rotation so we can use this as fromrotation if we would get interupted by dash and not have time to finish the lerp
		_lastTargetRotation = targetRotation;

		// Save last move direction if we would do dash and not give any direction during chargeup
		_lastMoveDirection = new Vector2DInt((int)movementDirection.x, (int)movementDirection.z);

		// Update tile player references NOTE: this is done right when a player starts moving to avoid players being able to move to the same tile (lerping is used when getting hit when not physiclly att target tile)
		SetNewTileReferences(targetTile.position);

		// do the movement itself
		float movementProgress = 0;
		while (movementProgress < 1)
		{
			movementProgress += _model.walkSpeed * Time.deltaTime;
			movementProgress = Mathf.Clamp01(movementProgress);

			transform.position = Vector3.Lerp(fromPosition, targetPosition, movementProgress);
			transform.position = new Vector3(transform.position.x, 1 + Mathf.Sin(movementProgress * (float)Math.PI), transform.position.z);

			transform.rotation = Quaternion.Lerp(fromRotation, targetRotation, movementProgress);

			yield return Timing.WaitForOneFrame;
		}

		// check if we ended up on deadly tile (only server handle deathchecks)
		if (PhotonNetwork.isMasterClient && DeadlyTile())
			yield break;

		currentTile.OnPlayerLand();

		// reset state and add cooldown
		_stateComponent.SetState(CharacterState.Idle);
		_flagComponent.SetFlag(CharacterFlag.Cooldown_Walk, true, _model.walkCooldown, SingletonBehavior.Overwrite);
	}

	IEnumerator<float> _Charge()
	{
		_stateComponent.SetState(CharacterState.Charging);

		// start sound and charge particles
		_character.ParticleComponent.EmitCharge(true);
		_character.soundComponent.PlaySound(CharacterSoundComponent.CharacterSound.Charge);	

		float chargeAmount = _model.dashMinCharge;

		while (Input.GetButton(Constants.BUTTON_CHARGE))
		{
			chargeAmount += (_model.dashChargeRate * Time.deltaTime);
			chargeAmount = Mathf.Clamp(chargeAmount, _model.dashMinCharge, _model.dashMaxCharge);

			// while charging direction can be changed
			if (Input.GetAxisRaw(Constants.AXIS_VERTICAL) > 0)
				_lastMoveDirection = Vector2DInt.Up;
			if (Input.GetAxisRaw(Constants.AXIS_VERTICAL) < 0)
				_lastMoveDirection = Vector2DInt.Down;
			if (Input.GetAxisRaw(Constants.AXIS_HORIZONTAL) < 0)
				_lastMoveDirection = Vector2DInt.Left;
			if (Input.GetAxisRaw(Constants.AXIS_HORIZONTAL) > 0)
				_lastMoveDirection = Vector2DInt.Right;

			currentDashCharges = (int)chargeAmount;

			yield return Timing.WaitForOneFrame;
		}

		Vector2DInt currentPos = currentTile.position;

		photonView.RPC("NetworkDash", PhotonTargets.All, currentPos.x, currentPos.y, _lastMoveDirection.x, _lastMoveDirection.y, currentDashCharges);
	}

	public IEnumerator<float> _Dash(Vector2DInt direction, int dashStrength, bool fromCollision = false)
	{
		_stateComponent.SetState(CharacterState.Dashing); // set state to dashing

		// only play dash sound if this was a volentary dash
		if (!fromCollision)
		{
			Vector2DInt currentPos = currentTile.position;
			Vector2DInt targetPos  = currentTile.GetRelativeTile(direction).position;

			_character.soundComponent.PlaySound(CharacterSoundComponent.CharacterSound.Dash);
			_character.ParticleComponent.EmitTrail(true, (new Vector3(targetPos.x, 1, targetPos.y) - new Vector3(currentPos.x, 1, currentPos.y)).normalized);
		}

		// stop feedback from charge
		_character.ParticleComponent.EmitCharge(false);
		_character.soundComponent.StopSound(CharacterSoundComponent.CharacterSound.Charge);

		// loop over all dash charges
		for (int i = 0; i < dashStrength; i++)
		{
			// get next tile in dash path			
			// current tile is corrected before coroutine if lagging so this is safe
			Tile targetTile = currentTile.GetRelativeTile(direction);			

			// abort dash if running into non walkable tile
			if (!targetTile.model.data.walkable)
				yield break;

			// Calculate lerp positions
			Vector3 fromPosition   = transform.position; // interpolate from current position to avoid teleporting if lagging
			Vector3 targetPosition = new Vector3(targetTile.position.x, 1, targetTile.position.y);

			// use the position of current tile instead of position of player to calculate rotation, otherwise we can get crooked rotation if laging
			Vector3 currentTilePos = new Vector3(currentTile.position.x, 1, currentTile.position.y);

			// Calculate lerp rotations
			// note: use last target rotation as base if we was in middle of movement when this dash started from getting hit from other player
			// this will make the rotation that was left from last movement to be added to this rotation and will be caught up
			Vector3 movementDirection = (targetPosition - currentTilePos).normalized;
			Quaternion fromRotation = transform.rotation;
			Quaternion targetRotation = Quaternion.Euler(movementDirection * (90 * _model.dashRotationSpeed)) * _lastTargetRotation;

			// if we will hit someone we need the target rotation we had last time becuase we wont start moving towards the future last target
			Quaternion previousLastTargetRotation = _lastTargetRotation;

			_lastTargetRotation = targetRotation;


			if (PhotonNetwork.isMasterClient) // do collision on master client
			{
				if (targetTile.IsOccupied())
				{
					// get occupying player and tell it to send an rpc that it got dashed
					Character playerToDash = targetTile.GetOccupyingPlayer();

					// save the collision on server so the clients can check that they did not stop their dash locally incorrectly 
					_collisionTracker.AddCollision(playerToDash.photonView.viewID, targetTile.position.x, targetTile.position.y);

					// tell all clients who got hit
					playerToDash.movementComponent.OnGettingDashed(targetTile.position, direction, dashStrength - i);

					// send rpc that we hit other player and cancel all our current movement
					OnDashingOther(currentTile.position, previousLastTargetRotation, targetTile.position);

					yield break;
				}
			}

			// stop locally aswell and dubblecheck so we had collision on server, if not the server will restart our dashroutine with the charges that was left
			if (targetTile.IsOccupied())
			{
				// add cooldowns and reset last target rotation becuase we never started interpolation
				StopMovementAndAddCooldowns();
				_lastTargetRotation = previousLastTargetRotation;

				// stop trailParticle
				_character.ParticleComponent.EmitTrail(false, Vector3.zero);

				_collisionTracker.photonView.RPC("CheckServerCollision", PhotonTargets.MasterClient,
												targetTile.GetOccupyingPlayer().photonView.viewID,
												photonView.viewID, currentTile.position.x, currentTile.position.y,
												targetTile.position.x, targetTile.position.y,
												direction.x, direction.y, dashStrength - i);

				// stop and frezze character while waiting for server to register collision
				// this becomes pretty noticable over 150 ping,
				// but is better then keping free movement and be interpolated back when getting corrected by server
				_character.stateComponent.SetState(CharacterState.Frozen);

				yield break;
			}

			// hurt tile if it is destructible(only do break detection on server)
			if (PhotonNetwork.isMasterClient && !currentTile.model.data.unbreakable)
				Match.instance.level.BreakTile(currentTile.position.x, currentTile.position.y);

			// Update tile player references 
			SetNewTileReferences(targetTile.position);

			// do the movement itself
			float movementProgress = 0;
			while (movementProgress < 1)
			{
				movementProgress += _model.dashSpeed * Time.deltaTime;
				movementProgress = Mathf.Clamp01(movementProgress);

				transform.position = Vector3.Lerp(fromPosition, targetPosition, movementProgress);
				transform.rotation = Quaternion.Lerp(fromRotation, targetRotation, movementProgress);

				yield return Timing.WaitForOneFrame;
			}

			// check if we exited the map after every tile
			// stop movement and flag dead locally and only handle death on server
			// will be corrected by server if say a collision happened on server but not locally that would have prevented us from exiting map
			if (DeadlyEdge())
			{
				if (PhotonNetwork.isMasterClient)
					photonView.RPC("Die", PhotonTargets.All, currentTile.position.x, currentTile.position.y);
				else
					_stateComponent.SetState(CharacterState.Frozen);

				yield break;
			}
		}

		// check if we ended up on deadly tile
		// only server handle death detection
		if (PhotonNetwork.isMasterClient && DeadlyTile())
			yield break;

		// add cooldowns and stop feedback
		_character.ParticleComponent.EmitTrail(false, Vector3.zero);
		StopMovementAndAddCooldowns();
	}
	
	public IEnumerator<float> _Correct(Vector3 from, Vector3 to, Quaternion fromRot, Quaternion toRot, float time)
	{
		float fraction = 0;
		float timer = 0;
		while (fraction < 1)
		{
			timer += Time.deltaTime;
			fraction = Mathf.InverseLerp(0, time, timer);
			transform.position = Vector3.Lerp(from, to, fraction);
			transform.rotation = Quaternion.Lerp(fromRot, toRot, fraction);
			yield return Timing.WaitForOneFrame;
		}
	}
	#endregion

	#region DEBUG CALLS
#if DEBUG_TOOLS
	public void InfiniteDash()
	{
		Character[] c = FindObjectsOfType<Character>();

		foreach(Character p in c)
		{
			CharacterMovementComponent m = p.GetComponent<CharacterMovementComponent>();
			p.GetComponent<PhotonView>().RPC("NetworkDash", PhotonTargets.All, m.currentTile.position.x, m.currentTile.position.y, m._lastMoveDirection.x, m._lastMoveDirection.y, 100);
		}		
	}
#endif
	#endregion
}
