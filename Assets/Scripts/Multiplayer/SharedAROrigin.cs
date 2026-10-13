using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class SharedAROrigin : MonoBehaviour
{
    public static Transform SharedOrigin { get; private set; }

    private ARTrackedImageManager imgMgr;
    private bool originSet = false;

    private void Awake()
    {
        // Search the whole scene – works regardless of which GameObject this is on
        imgMgr = FindAnyObjectByType<ARTrackedImageManager>();

        if (imgMgr == null)
            Debug.LogError("[SharedAROrigin] No ARTrackedImageManager found in scene!");
    }

    private void Update()
    {
        // Already set – nothing to do
        if (originSet || imgMgr == null) return;

        // Poll the live trackables every frame until we find the marker
        foreach (ARTrackedImage image in imgMgr.trackables)
        {
            if (image.trackingState == TrackingState.Tracking)
            {
                
                SharedOrigin = image.transform;
                originSet = true;

                Debug.Log($"[SharedAROrigin] MARKER DETECTED ({image.referenceImage.name}) – shared origin set.");
                break;
            }
        }
    }

    public static Vector3 ToSharedSpace(Vector3 worldPos) =>
        SharedOrigin ? SharedOrigin.InverseTransformPoint(worldPos) : worldPos;

    public static Vector3 FromSharedSpace(Vector3 sharedPos) =>
        SharedOrigin ? SharedOrigin.TransformPoint(sharedPos) : sharedPos;
}