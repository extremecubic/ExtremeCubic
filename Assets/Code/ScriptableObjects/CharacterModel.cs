using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSettings", menuName = "Data Models/Player Model", order =1)]
public class CharacterModel : ScriptableObject
{
    [Header("Walking")]
	public float walkSpeed         = 2.0f;                // Tiles per second the player walks
	public float walkCooldown      = 0.25f;               // Cooldown between walks
                                                          
    [Header("Dashing")]                                   
	public float dashSpeed         = 6.0f;                // Dash speed in tiles per second
	public float dashCooldown      = 0.5f;                // Time between the end of a dash and beginning of a charge
    public int   dashMinCharge     = 2;                   // Min dash distance in tiles
	public int   dashMaxCharge     = 4;                   // Max dash distance in tiles
    public float dashChargeRate   = 2.0f;                 // Dash tiles per second
	public int   dashRotationSpeed = 1;                   // How many 90 degree rotations per tile the character does during a dash
	public float dashCameraShakeDuration = 0.2f;          // how long the camera shake will be
	public float dashCameraShakeSpeed = 50.0f;            // how many shake interpolations per second
	public float dashCameraShakeIntensity = 0.3f;         // radius of shake from origin
	public float dashCameraShakeIntensityDamping = 0.1f;  // how much the intensity will be damped during the duration, 1 == will go from max to 0 (0 is clamped to min value of 0.1) 

	[Header("Colliding")]
	public float collideSpeed = 1.0f;
	public int   numCollideRolls = 3;
	public float collideBounceHeight = 1;
	public float collideFlyBackAmount = 0.75f;
	public float collideStunTime = 3.0f;
	public float collideCameraShakeDuration = 0.2f;
	public float collideCameraShakeSpeed = 50.0f;
	public float collideCameraShakeIntensity = 0.3f;
	public float collideCameraShakeIntensityDamping = 0.1f;

	[Header("DEATH SETTINGS"), Space(10)]
	[Header("Fall")]
	public float fallSpeed = 5.0f;
	public float fallAcceleration = 10.0f;

	[Header("quicksand")]
	public Vector2        startEndMoveSpeedQvick;
	public Vector2        startEndRotationspeedQvick;
	public float          durationQvick;
	public AnimationCurve moveCurveQvick;
	public AnimationCurve rotationCurveQvick;

	[Header("explode")]
	public float speedExplode;

	[Header("Fly to target")]
	public float flySpeed = 10.0f;
	public float flyAcceleration = 5.0f;

}
