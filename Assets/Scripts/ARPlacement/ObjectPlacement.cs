using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlaceCube : NetworkBehaviour
{
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private GameObject ChameleonPrefab;
    [SerializeField] private float rotationSpeed = 0.2f;
    [SerializeField] private ARPlaneManager planeManager;

    public GameObject CurrentChameleon => currentChameleon;

    private List<ARRaycastHit> rayHits = new();
    private Vector2 previousTouchPosition;
    public bool canRotate = true;
    bool isPlacing = false;
    private GameObject currentChameleon;
    private bool isRotationMode = false;
    private Camera arCamera;

    void Start()
    {
        arCamera = Camera.main;
    }

    void Update()
    {
        if (!raycastManager) return;

        if (currentChameleon == null) // Placement
        {
            if ((Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began ||
                Input.GetMouseButtonDown(0)) && !isPlacing)
            {
                isPlacing = true;

                if (Input.touchCount > 0)
                    PlaceObject(Input.GetTouch(0).position);
                else
                    PlaceObject(Input.mousePosition);
            }
        }
        else // Rotation
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    previousTouchPosition = touch.position;
                    if (currentChameleon != null)
                    {
                        isRotationMode = IsTouchingChameleon(touch.position);
                        Debug.Log("Rotation Mode: " + isRotationMode);
                    }
                }
                else if (touch.phase == TouchPhase.Moved && isRotationMode && canRotate)
                {
                    float deltaX = touch.position.x - previousTouchPosition.x;
                    currentChameleon.transform.Rotate(0, -deltaX * rotationSpeed, 0, Space.Self);
                    previousTouchPosition = touch.position;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                previousTouchPosition = Input.mousePosition;
                if (currentChameleon != null)
                {
                    isRotationMode = IsTouchingChameleon(Input.mousePosition);
                    Debug.Log("Rotation Mode: " + isRotationMode);
                }
            }

            if (Input.GetMouseButton(0) && currentChameleon != null && isRotationMode && canRotate)
            {
                Vector2 currentMousePosition = Input.mousePosition;
                float deltaX = currentMousePosition.x - previousTouchPosition.x;
                currentChameleon.transform.Rotate(0, -deltaX * rotationSpeed, 0, Space.Self);
                previousTouchPosition = currentMousePosition;
            }
        }
    }

    void PlaceObject(Vector2 touchPosition)
    {
        // Block placement if marker not scanned yet
        if (SharedAROrigin.SharedOrigin == null)
        {
            Debug.LogWarning("[ARPlaceCube] Scan the marker first before placing!");
            isPlacing = false;
            return;
        }

        rayHits.Clear();
        raycastManager.Raycast(touchPosition, rayHits, TrackableType.PlaneWithinPolygon);

        if (rayHits.Count > 0)
        {
            Vector3 hitPosePosition = rayHits[0].pose.position;
            Quaternion spawnRotation = ChameleonPrefab.transform.rotation;

            if (currentChameleon == null)
            {
                currentChameleon = Instantiate(ChameleonPrefab, hitPosePosition, spawnRotation);
                planeManager.enabled = false;

                foreach (ARPlane plane in planeManager.trackables)
                    plane.gameObject.SetActive(false);
            }
        }

        StartCoroutine(SetIsPlacingToFalseWithDelay());
    }

    IEnumerator SetIsPlacingToFalseWithDelay()
    {
        yield return new WaitForSeconds(0.25f);
        isPlacing = false;
    }

    // Screen-space check instead of physics raycast (Unity 6 scene physics fix)
    bool IsTouchingChameleon(Vector2 screenPosition)
    {
        if (arCamera == null || currentChameleon == null) return false;

        Vector3 screenPos = arCamera.WorldToScreenPoint(currentChameleon.transform.position);

        // Character is behind the camera
        if (screenPos.z < 0) return false;

        float distance = Vector2.Distance(screenPosition, new Vector2(screenPos.x, screenPos.y));
        return distance < 200f; // 200 pixel touch radius
    }

    // Called by the Seeker to register the received character
    public void SetCurrentChameleon(GameObject cham)
    {
        currentChameleon = cham;
    }

    public void ResetPlacement()
    {
        if (currentChameleon != null)
        {
            Destroy(currentChameleon);
            currentChameleon = null;
            isRotationMode = false;
        }

        if (planeManager != null)
        {
            planeManager.enabled = true;
            foreach (ARPlane plane in planeManager.trackables)
                plane.gameObject.SetActive(true);
        }
    }
}