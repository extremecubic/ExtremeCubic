using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;

public class CollisionTracker : Photon.MonoBehaviour
{
	// keep track of the tile 
	// that someone got hit
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

	// called when the server detects a collision
	// is called on all clients aswell so they have the correct
	// state of the game incase of server migration
	[PunRPC]
	public void AddCollision(int photonId, int tileX, int tileY)
	{
		if (_recentCollisions.Count == Constants.NUM_COLLISIONS_TO_SAVE_ON_SERVER)
			_recentCollisions.RemoveAt(0);

		_recentCollisions.Add(new CollisionData { tile = new Vector2DInt(tileX, tileY), photonId = photonId });
	}

	// only called from clients to server and the
	// server will respond to the clients 
	[PunRPC]
	public void CheckServerCollision(int photonIdHit, int photonIdMine, int myTileX, int myTileY, int HitTileX, int hitTileY, int directionX, int directionY, int chargesLeft)
	{
		
		Vector2DInt tile = new Vector2DInt(HitTileX, hitTileY);

		// check if the server has registred a collision on the tile the client just had a collision	
		// the photon id of hit character matches then it means that the client made the correct
		// assumption that a collision accured, if no collision is found it means 
		// that the client assumed wrong and we the tell the client to continue the dash that was cancelled									 
		for (int i =0; i < _recentCollisions.Count; i++)
		{
			if (_recentCollisions[i].tile == tile) 		
				if (_recentCollisions[i].photonId == photonIdHit) 
					return;						
		}

		// if we get here it means no collision was found and the client cancelled his dash incorrectly 
		// make sure that this player is still online and then tell him to finish rest of dash
		PhotonView view = PhotonView.Find(photonIdMine);
		if (view != null)
		    view.RPC("FinishCancelledDash", PhotonTargets.All, myTileX, myTileY, directionX, directionY, chargesLeft);					
	}	
}
