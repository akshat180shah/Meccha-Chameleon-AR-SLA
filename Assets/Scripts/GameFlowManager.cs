using UnityEngine;
using TMPro;
using System.Collections;
using Unity.Netcode;

public class GameFlowManager : NetworkBehaviour
{
    [Header("Multiplayer Roles")]
    public GameObject roleSelectionPanel;
    public GameObject seekerWaitingUI;
    public ARPlaceCube placementScript;
    public GameObject ChameleonPrefab;

    [Header("Existing UI Elements")]
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private ModeManager modeManager;
    [SerializeField] private GameObject paintingUI;
    [SerializeField] private GameObject crosshair;
    [SerializeField] private GameObject SeekerTimer;
    [SerializeField] private GameObject colorPanel;

    public bool seekerTurn = false;
    public bool isHider = false;

    // Data stored for the Seeker
    private Vector3 finalSharedPos;
    private Quaternion finalSharedRot;
    private PaintStroke[] receivedStrokes; // Networked Paint History

    void Start()
    {
        roleSelectionPanel.SetActive(false);
        paintingUI.SetActive(false);
        colorPanel.SetActive(false);
        seekerWaitingUI.SetActive(false);
        crosshair.SetActive(false);
        SeekerTimer.SetActive(false);

        if (placementScript != null) placementScript.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        roleSelectionPanel.SetActive(true);
    }

    public void SelectHider()
    {
        ClaimHiderServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    public void SelectSeeker()
    {
        ClaimSeekerServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    // ==========================================
    // STEP 2: DONE BUTTON & DATA SYNC
    // ==========================================

    public void OnDoneClicked()
    {
        if (placementScript.CurrentChameleon == null) return;

        Vector3 worldPos = placementScript.CurrentChameleon.transform.position;
        Quaternion worldRot = placementScript.CurrentChameleon.transform.rotation;

        Vector3 sharedPos = SharedAROrigin.ToSharedSpace(worldPos);
        Quaternion sharedRot = Quaternion.Inverse(SharedAROrigin.SharedOrigin.rotation) * worldRot;

        // Collect paint data
        PaintCanvas canvas = FindAnyObjectByType<PaintCanvas>();
        PaintStroke[] strokes = canvas != null ? canvas.strokeHistory.ToArray() : new PaintStroke[0];

        // Send everything to Seeker!
        SubmitHiderDataServerRpc(sharedPos, sharedRot, strokes);

        paintingUI.SetActive(false);
        colorPanel.SetActive(false);
        Debug.Log("Hider is done. Waiting for Seeker...");
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitHiderDataServerRpc(Vector3 pos, Quaternion rot, PaintStroke[] strokes)
    {
        ReceiveHiderDataClientRpc(pos, rot, strokes);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ReceiveHiderDataClientRpc(Vector3 pos, Quaternion rot, PaintStroke[] strokes)
    {
        if (!isHider) // If I am the Seeker, I receive the data!
        {
            finalSharedPos = pos;
            finalSharedRot = rot;
            receivedStrokes = strokes;

            StartCoroutine(WaitAndSpawnForSeeker());
        }
    }

    IEnumerator WaitAndSpawnForSeeker()
    {
        TMP_Text waitText = seekerWaitingUI.GetComponentInChildren<TMP_Text>();
        if (waitText != null) waitText.text = "Hider is done!\nScan the ChameleonMarker to hunt!";

        while (SharedAROrigin.SharedOrigin == null)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Wait 2 seconds for AR tracking to stabilize the marker on the floor!
        // Without this, the character spawns mid-air or sideways while the marker is still calibrating.
        yield return new WaitForSeconds(2.0f);

        Vector3 worldPos = SharedAROrigin.FromSharedSpace(finalSharedPos);
        Quaternion worldRot = SharedAROrigin.SharedOrigin.rotation * finalSharedRot;

        GameObject seekerCham = Instantiate(ChameleonPrefab, worldPos, worldRot);
        placementScript.SetCurrentChameleon(seekerCham);

        // INSTANTLY REPLAY PAINTING!
        PaintCanvas canvas = FindAnyObjectByType<PaintCanvas>();
        if (canvas != null && receivedStrokes != null)
        {
            canvas.InitializeCanvas(); // Critical: Init first so it doesn't wipe our strokes!
            canvas.ReplayStrokes(receivedStrokes);
        }

        StartSeekerTurn();
    }

    // ==========================================
    // NETWORK ROLES
    // ==========================================

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ClaimHiderServerRpc(ulong requestingClient)
    {
        AssignRolesClientRpc(requestingClient);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ClaimSeekerServerRpc(ulong requestingClient)
    {
        ulong hiderId = requestingClient == 0 ? 1ul : 0ul;
        AssignRolesClientRpc(hiderId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void AssignRolesClientRpc(ulong hiderClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == hiderClientId)
        {
            isHider = true;
            roleSelectionPanel.SetActive(false);
            paintingUI.SetActive(true);
            colorPanel.SetActive(true);
            placementScript.enabled = true;
        }
        else
        {
            isHider = false;
            roleSelectionPanel.SetActive(false);
            seekerWaitingUI.SetActive(true);
            placementScript.enabled = false;
        }
    }

    // ==========================================
    // SEEKER GAMEPLAY
    // ==========================================

    public void StartSeekerTurn()
    {
        seekerWaitingUI.SetActive(false);
        
        // HIDE AR PLANES FOR SEEKER! 
        // This prevents the AR floor grid from clipping over or hiding the character
        var planeManager = FindAnyObjectByType<UnityEngine.XR.ARFoundation.ARPlaneManager>();
        if (planeManager != null)
        {
            planeManager.enabled = false;
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }
        }

        StartCoroutine(StartCountdown());
    }

    IEnumerator StartCountdown()
    {
        countdownText.gameObject.SetActive(true);

        for (int i = 5; i >= 1; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        countdownText.text = "GO!";
        yield return new WaitForSeconds(1f);
        countdownText.gameObject.SetActive(false);

        seekerTurn = true;
        crosshair.SetActive(true);
        SeekerTimer.SetActive(true);
        Debug.Log("Find the Chameleon!");
    }

    // ==========================================
    // GAME OVER SYNC
    // ==========================================

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void GameEndedServerRpc(int hiderScore, int seekerScore, bool timeUp)
    {
        GameEndedClientRpc(hiderScore, seekerScore, timeUp);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void GameEndedClientRpc(int hiderScore, int seekerScore, bool timeUp)
    {
        if (isHider) // If I am the Hider, I need to see the result!
        {
            SeekerController seekerController = FindAnyObjectByType<SeekerController>();
            if (seekerController != null)
            {
                seekerController.ShowEndScreen(hiderScore, seekerScore, timeUp);
            }
        }
    }
}