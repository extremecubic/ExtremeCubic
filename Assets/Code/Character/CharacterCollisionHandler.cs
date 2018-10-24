using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class CharacterMovementComponent : Photon.MonoBehaviour
{
	bool IsCollidingOnline(Quaternion previousLastTargetRotation, Tile targetTile, Vector2DInt direction, int dashStrength, int dashIndex)
	{
		if (PhotonNetwork.isMasterClient) // do collision on master client
		{
			if (targetTile.IsOccupied())
			{
				// get occupying player and tell it to send an rpc that it got dashed
				Character playerToDash = targetTile.currentCharacter;

				// save the collision on server and all clients
				// clients will then check with server if thier local collisions is correct or not,
				// the data is kept an all clients aswell just incase of server migration
				_collisionTracker.photonView.RPC("AddCollision", PhotonTargets.All, playerToDash.photonView.viewID, targetTile.position.x, targetTile.position.y);

				// tell all clients who got hit
				playerToDash.movementComponent.OnGettingDashed(targetTile.position, direction, dashStrength - dashIndex);

				// send rpc that we hit other player and cancel all our current movement
				OnDashingOther(currentTile.position, previousLastTargetRotation, targetTile.position);

				return true;
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
											targetTile.currentCharacter.photonView.viewID,
											photonView.viewID, currentTile.position.x, currentTile.position.y,
											targetTile.position.x, targetTile.position.y,
											direction.x, direction.y, dashStrength - dashIndex);

			// stop and frezze character while waiting for server to register collision
			// this becomes pretty noticable over 150 ping,
			// but is better then keping free movement and be interpolated back when getting corrected by server
			_character.stateComponent.SetState(CharacterState.Frozen);

			return true;
		}
		return false;
	}

	bool IsCollidingLocal(Quaternion previousLastTargetRotation, Tile targetTile, Vector2DInt direction, int dashStrength, int dashIndex)
	{
		if (targetTile.IsOccupied())
		{
			// get occupying player and tell it to send an rpc that it got dashed
			Character playerToDash = targetTile.currentCharacter;

			playerToDash.movementComponent.OnGettingDashed(targetTile.position, direction, dashStrength - dashIndex);
			OnDashingOther(currentTile.position, previousLastTargetRotation, targetTile.position);
			
			_lastTargetRotation = previousLastTargetRotation;
			
			return true;
		}
		return false;
	}

}
