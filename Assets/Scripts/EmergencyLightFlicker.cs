using UnityEngine;

[RequireComponent(typeof(Light))]
public class EmergencyLightFlicker : MonoBehaviour
{
    [SerializeField] private float minimumDelay = 0.05f;
    [SerializeField] private float maximumDelay = 0.2f;

    private Light emergencyLight;
    private float normalIntensity;
    private float nextChangeTime;

    private void Awake()
    {
        emergencyLight = GetComponent<Light>();
        normalIntensity = emergencyLight.intensity;
    }

    private void Update()
    {
        if (Time.time < nextChangeTime)
            return;

        float brightness = Random.value < 0.15f
            ? 0.05f
            : Random.Range(0.4f, 1f);

        emergencyLight.intensity = normalIntensity * brightness;
        nextChangeTime = Time.time + Random.Range(minimumDelay, maximumDelay);
    }
}