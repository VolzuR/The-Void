using UnityEngine;

public class Flashlight : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.F;
    public Transform cameraTransform;
    public Light spot;                       // если пусто, создастся сам
    public bool startOn = false;

    [Header("Свет")]
    public float range = 25f;
    public float spotAngle = 60f;
    public float intensity = 3f;
    public Color color = new Color(1f, 0.95f, 0.8f);

    [Header("Звук щелчка")]
    public AudioSource clickSource;
    public AudioClip clickClip;

    bool on;

    void Start()
    {
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;

        if (!spot)
        {
            GameObject g = new GameObject("FlashlightSpot");
            g.transform.SetParent(cameraTransform, false);
            g.transform.localPosition = new Vector3(0.15f, -0.1f, 0.2f);
            g.transform.localRotation = Quaternion.identity;
            spot = g.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.shadows = LightShadows.Soft;
        }

        spot.range = range;
        spot.spotAngle = spotAngle;
        spot.intensity = intensity;
        spot.color = color;

        on = startOn;
        spot.enabled = on;
    }

    void Update()
    {
        if (PlayerController.inputLocked) return;
        if (Input.GetKeyDown(toggleKey))
        {
            on = !on;
            spot.enabled = on;
            if (clickSource && clickClip) clickSource.PlayOneShot(clickClip);
        }
    }
}