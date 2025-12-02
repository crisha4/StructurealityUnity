using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARObjectPlacer : MonoBehaviour
{
    [Header("AR Components")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    
    [Header("Object to Place")]
    [SerializeField] private GameObject objectToPlace;
    
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private GameObject spawnedObject;
    private bool isPlaced = false;

    void Start()
    {
        // Auto-find AR managers if not assigned
        if (raycastManager == null)
        {
            raycastManager = FindObjectOfType<ARRaycastManager>();
            Debug.Log("ARRaycastManager: " + (raycastManager != null ? "Found" : "NOT FOUND"));
        }
        
        if (planeManager == null)
        {
            planeManager = FindObjectOfType<ARPlaneManager>();
            Debug.Log("ARPlaneManager: " + (planeManager != null ? "Found" : "NOT FOUND"));
        }
    }

    void Update()
    {
        // Check if user touched the screen
        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);

        // Only place on first touch
        if (touch.phase != TouchPhase.Began)
            return;

        // Perform raycast from touch position
        if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
        {
            // Get the hit pose (position and rotation)
            Pose hitPose = hits[0].pose;

            // If object hasn't been spawned yet, create it
            if (spawnedObject == null)
            {
                spawnedObject = Instantiate(objectToPlace, hitPose.position, hitPose.rotation);
                isPlaced = true;
                HidePlanes();
                Debug.Log("✅ Object placed! Planes hidden.");
            }
            else
            {
                // Move existing object to new position
                spawnedObject.transform.position = hitPose.position;
                spawnedObject.transform.rotation = hitPose.rotation;
                Debug.Log("Object moved!");
            }
        }
    }

    /// <summary>
    /// Call this method when the Clear button is clicked
    /// </summary>
    public void Clear()
    {
        if (spawnedObject != null)
        {
            Destroy(spawnedObject);
            spawnedObject = null;
            Debug.Log("🗑️ Object destroyed");
        }
        
        isPlaced = false;
        ShowPlanes();
        Debug.Log("👁️ Planes visible again - ready to place new object");
    }

    /// <summary>
    /// Hide all detected AR planes
    /// </summary>
    private void HidePlanes()
    {
        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }
            Debug.Log("👻 AR Planes hidden");
        }
    }

    /// <summary>
    /// Show all detected AR planes
    /// </summary>
    private void ShowPlanes()
    {
        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(true);
            }
            Debug.Log("👁️ AR Planes visible");
        }
    }

    /// <summary>
    /// Manual toggle for plane visualization (optional)
    /// </summary>
    public void TogglePlaneVisualization(bool show)
    {
        if (show)
            ShowPlanes();
        else
            HidePlanes();
    }

    /// <summary>
    /// Check if an object has been placed
    /// </summary>
    public bool IsObjectPlaced()
    {
        return isPlaced;
    }
}