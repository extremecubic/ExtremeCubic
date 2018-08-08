using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSettings", menuName = "Data Models/Player Model", order =1)]
public class CharacterModel : ScriptableObject
{
    [Header("Walking")]
	public float walkSpeed         = 2.0f;   // Tiles per second the player walks
	public float walkCooldown      = 0.25f;  // Cooldown between walks
                                             
    [Header("Dashing")]                      
	public float dashSpeed         = 6.0f;   // Dash speed in tiles per second
	public float dashCooldown      = 0.5f;   // Time between the end of a dash and beginning of a charge
    public int   dashMinCharge     = 2;      // Min dash distance in tiles
	public int   dashMaxCharge     = 4;      // Max dash distance in tiles
    public float dashChargeRate   = 2.0f;    // Dash tiles per second
	public int   dashRotationSpeed = 1;      // How many 90 degree rotations per tile the character does during a dash

	[Header("sink")]
	public float sinkSpeed = 5.0f;

	[Header("quicksand")]
	public Vector2        startEndMoveSpeedQvick;
	public Vector2        startEndRotationspeedQvick;
	public float          durationQvick;
	public AnimationCurve moveCurveQvick;
	public AnimationCurve rotationCurveQvick;

	[Header("explode")]
	public float speedExplode;

}
