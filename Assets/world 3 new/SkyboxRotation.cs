using UnityEngine;

public class SkyboxRotation : MonoBehaviour
{
    public float rotationSpeed = 0.5f;

    private Material skyboxMaterial;
    private float rotation;

    void Start()
    {
        skyboxMaterial = RenderSettings.skybox;

        if (skyboxMaterial == null)
        {
            Debug.LogError("No Skybox Material is assigned!");
            return;
        }

        if (!skyboxMaterial.HasProperty("_Rotation"))
        {
            Debug.LogError("This Skybox shader does not have a Rotation property!");
            return;
        }

        rotation = skyboxMaterial.GetFloat("_Rotation");
    }

    void Update()
    {
        if (skyboxMaterial == null)
            return;

        rotation += rotationSpeed * Time.deltaTime;

        if (rotation >= 360f)
            rotation -= 360f;

        skyboxMaterial.SetFloat("_Rotation", rotation);
    }
}