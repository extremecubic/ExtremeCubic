using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class TOD_ParticleAtDay : MonoBehaviour
{
	public  float fadeTime = 1;
	private float lerpTime = 0;

	private ParticleSystem particleComponent;
	private float particleEmission;

	ParticleSystem.EmissionModule eModule;

	protected void Start()
	{
		particleComponent = GetComponent<ParticleSystem>();
		particleEmission  = particleComponent.emission.rateOverTimeMultiplier;

		eModule = particleComponent.emission;

		eModule.rateOverTimeMultiplier = TOD_Sky.Instance.IsDay ? particleEmission : 0;
	}

	protected void Update()
	{
		int sign = (TOD_Sky.Instance.IsDay) ? +1 : -1;
		lerpTime = Mathf.Clamp01(lerpTime + sign * Time.deltaTime / fadeTime);

		eModule.rateOverTimeMultiplier = Mathf.Lerp(0, particleEmission, lerpTime);
	}
}
