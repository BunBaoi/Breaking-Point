using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClimbingRaycastVisualizer : MonoBehaviour
{
    [Header("References")]
    public Transform leftHandTransform;
    public Transform rightHandTransform;
    public Transform cameraTransform;
    public PlayerControls playerControls;

    [Header("Visual Settings")]
    public Color validRaycastColor = Color.green;
    public Color invalidRaycastColor = Color.red;
    public Color outOfReachColor = Color.yellow;
    public bool showRaycastLines = true;
    public float lineWidth = 0.02f;
    public float sphereRadius = 0.05f;
    public bool showHandReachSpheres = true;

    private LineRenderer centerRayLine;
    private GameObject centerPoint;
    private GameObject leftHandReachSphere;
    private GameObject rightHandReachSphere;

    void Start()
    {
        // Create line renderer for center ray
        centerRayLine = CreateLineRenderer("CenterRay", Color.red);

        // Create sphere indicators
        centerPoint = CreateSphereIndicator("CenterPoint");

        if (showHandReachSpheres)
        {
            leftHandReachSphere = CreateReachSphere("LeftHandReach");
            rightHandReachSphere = CreateReachSphere("RightHandReach");
        }
    }

    void Update()
    {
        if (showRaycastLines)
        {
            UpdateCenterRayVisualization();

            if (showHandReachSpheres)
            {
                UpdateHandReachVisuals();
            }
        }
    }

    private LineRenderer CreateLineRenderer(string name, Color defaultColor)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(transform);
        LineRenderer line = lineObj.AddComponent<LineRenderer>();

        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.startColor = defaultColor;
        line.endColor = defaultColor;
        line.positionCount = 2;

        return line;
    }

    private void UpdateCenterRayVisualization()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;

        centerRayLine.SetPosition(0, ray.origin);

        if (Physics.Raycast(ray, out hit, playerControls.maxClimbDistance))
        {
            centerRayLine.SetPosition(1, hit.point);
            centerPoint.transform.position = hit.point;
            centerPoint.SetActive(true);

            bool isClimbable = hit.collider.CompareTag(playerControls.climbableTag);
            bool leftHandInReach = Vector3.Distance(leftHandTransform.position, hit.point) <= playerControls.handReachOffset;
            bool rightHandInReach = Vector3.Distance(rightHandTransform.position, hit.point) <= playerControls.handReachOffset;

            Color rayColor;
            if (isClimbable && (leftHandInReach || rightHandInReach))
            {
                rayColor = validRaycastColor;
            }
            else if (isClimbable)
            {
                rayColor = outOfReachColor;
            }
            else
            {
                rayColor = invalidRaycastColor;
            }

            centerRayLine.startColor = rayColor;
            centerRayLine.endColor = rayColor;
            centerPoint.GetComponent<Renderer>().material.color = new Color(rayColor.r, rayColor.g, rayColor.b, 0.5f);
        }
        else
        {
            centerRayLine.SetPosition(1, ray.origin + ray.direction * playerControls.maxClimbDistance);
            centerRayLine.startColor = invalidRaycastColor;
            centerRayLine.endColor = invalidRaycastColor;
            centerPoint.SetActive(false);
        }
    }

    private void UpdateHandReachVisuals()
    {
        UpdateReachSphere(leftHandReachSphere, leftHandTransform);
        UpdateReachSphere(rightHandReachSphere, rightHandTransform);
    }

    private void UpdateReachSphere(GameObject sphere, Transform handTransform)
    {
        sphere.transform.position = handTransform.position;
        sphere.transform.localScale = Vector3.one * playerControls.handReachOffset * 2;
    }

    private GameObject CreateReachSphere(string name)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.SetParent(transform);

        // Make the sphere wireframe and transparent
        Destroy(sphere.GetComponent<Collider>());
        Renderer renderer = sphere.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        mat.color = new Color(1, 1, 1, 0.1f);
        renderer.material = mat;

        return sphere;
    }

    private GameObject CreateSphereIndicator(string name)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.SetParent(transform);
        sphere.transform.localScale = Vector3.one * sphereRadius * 2;

        // Make the sphere transparent and disable its collider
        Destroy(sphere.GetComponent<Collider>());
        Renderer renderer = sphere.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Mode", 3); // Transparent mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        mat.color = new Color(1, 1, 1, 0.5f);
        renderer.material = mat;

        return sphere;
    }


}