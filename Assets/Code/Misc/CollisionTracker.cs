using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;

public class CollisionTracker : Photon.MonoBehaviour
{

	public struct CollisionData
	{
		public Vector2DInt tile;
		public int photonId;					
	}

	List <CollisionData> _recentCollisions;

	public void ManualStart()
	{		
		_recentCollisions = new List<CollisionData>();					
	}

	public void AddCollision(int photonId, int tileX, int tileY)
	{
		if (_recentCollisions.Count == Constants.NUM_COLLISIONS_TO_SAVE_ON_SERVER)
			_recentCollisions.RemoveAt(0);

		_recentCollisions.Add(new CollisionData { tile = new Vector2DInt(tileX, tileY), photonId = photonId });
	}

	[PunRPC]
	public void CheckServerCollision(int photonIdHit, int photonIdMine, int myTileX, int myTileY, int HitTileX, int hitTileY, int directionX, int directionY, int chargesLeft)
	{
		
		Vector2DInt tile = new Vector2DInt(HitTileX, hitTileY);

		for(int i =0; i < _recentCollisions.Count; i++)
		{
			if(_recentCollisions[i].tile == tile) // found collision on requested tile			
				if (_recentCollisions[i].photonId == photonIdHit) // the photon id of hit character matches, collision happened on server aswell and we don't need to take action									 
					return;						
		}

		// if we get here it means no collision was found and the client cancelled his dash incorrectly, tell him to finish rest of dash
		PhotonView.Find(photonIdMine).RPC("FinishCancelledDash", PhotonTargets.All, myTileX, myTileY, directionX, directionY, chargesLeft);					
	}	
}
