using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public sealed class RainCycleController : MonoBehaviour
{
    [Header("Cycle in seconds")]
    [SerializeField, Min(0.1f)] private float heavyRainDuration = 6f;
    [SerializeField, Min(0.1f)] private float fadeRainDuration = 9f;
    [SerializeField, Min(0.1f)] private float dryDuration = 6f;

    [Header("Rain density")]
    [SerializeField, Min(1f)] private float heavyRainRate = 900f;
    [SerializeField, Range(0f, 1f)] private float finalRainStrength = 0.04f;

    private ParticleSystem _rain;
    private ParticleSystem.EmissionModule _emission;
    private float _cycleTime;

    private void Awake()
    {
        _rain = GetComponent<ParticleSystem>();
        _emission = _rain.emission;
    }

    private void OnEnable()
    {
        _cycleTime = 0f;
        if (_rain != null)
            _rain.Play(true);
    }

    private void Update()
    {
        _cycleTime += Time.deltaTime;
        float activeDuration = heavyRainDuration + fadeRainDuration;
        float cycleDuration = activeDuration + dryDuration;
        if (_cycleTime >= cycleDuration)
            _cycleTime %= cycleDuration;

        float strength;
        if (_cycleTime < heavyRainDuration)
        {
            strength = 1f;
        }
        else if (_cycleTime < activeDuration)
        {
            float fadeProgress = (_cycleTime - heavyRainDuration) / fadeRainDuration;
            strength = Mathf.Lerp(1f, finalRainStrength, fadeProgress);
        }
        else
        {
            strength = 0f;
        }

        _emission.rateOverTime = heavyRainRate * strength;
    }
}
