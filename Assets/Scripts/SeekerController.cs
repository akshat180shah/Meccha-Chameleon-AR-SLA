using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement; // Added for Restart

public class SeekerController : MonoBehaviour
{
    [SerializeField] private Camera arCamera;
    [SerializeField] private GameFlowManager gameFlowManager;
    [SerializeField] private ARPlaceCube placementScript;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private TextMeshProUGUI winText;
    [SerializeField] private TextMeshProUGUI timerText; // UI Text for the timer
    //UI elments for turn off/on
    [SerializeField] private GameObject crosshair;
    [SerializeField] private GameObject paintingUI;
    [SerializeField] private GameObject colorPanel;

    private int hiderScore = 0;
    private int seekerScore = 0;
    private bool scoringRoutineRunning = false;

    private float seekerTimer = 90f; // 1 minute 30 seconds

    void Update()
    {
        if (!gameFlowManager.seekerTurn) return;

        // Timer Logic
        seekerTimer -= Time.deltaTime;
        if (timerText != null)
        {
            // Format timer as M:SS
            int minutes = Mathf.FloorToInt(seekerTimer / 60);
            int seconds = Mathf.FloorToInt(seekerTimer % 60);
            timerText.text = string.Format("{0}:{1:00}", minutes, seconds);
        }

        if (seekerTimer <= 0)
        {
            // Time's up! Hider automatically wins.
            gameFlowManager.seekerTurn = false;
            DeclareTimeUpWinner();
            return;
        }

        // Start scoring timer once when Seeker turn begins
        if (!scoringRoutineRunning)
        {
            StartCoroutine(HiderScoringRoutine());
            scoringRoutineRunning = true;
        }

        if (Input.GetMouseButtonDown(0)) Shoot();
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) Shoot();
    }

    private IEnumerator HiderScoringRoutine()
    {
        while (gameFlowManager.seekerTurn)
        {
            yield return new WaitForSeconds(0.5f);

            if (placementScript.CurrentChameleon != null && IsChameleonVisible())
            {
                hiderScore += 10;
            }
        }
    }

    private bool IsChameleonVisible()
    {
        if (placementScript.CurrentChameleon == null) return false;

        Vector3 screenPos = arCamera.WorldToScreenPoint(placementScript.CurrentChameleon.transform.position);

        // Check if the Chameleon is visibly on the screen (in front of camera, inside screen bounds)
        if (screenPos.z > 0 && 
            screenPos.x > 0 && screenPos.x < Screen.width && 
            screenPos.y > 0 && screenPos.y < Screen.height)
        {
            return true;
        }
        return false;
    }

    void Shoot()
    {
        if (!gameFlowManager.seekerTurn) return; // FIX: Prevent double shooting on the same frame (fixes 1000 score bug)
        if (placementScript.CurrentChameleon == null) return;

        Vector3 screenPos = arCamera.WorldToScreenPoint(placementScript.CurrentChameleon.transform.position);

        if (screenPos.z > 0)
        {
            Vector2 centerScreen = new Vector2(Screen.width / 2f, Screen.height / 2f);
            float dist = Vector2.Distance(centerScreen, new Vector2(screenPos.x, screenPos.y));

            if (dist < 400f)
            {
                seekerScore += 500;
                placementScript.CurrentChameleon.SetActive(false);
                gameFlowManager.seekerTurn = false;

                DeclareWinner();
            }
        }
    }

    private void DeclareWinner()
    {
        timerText.gameObject.SetActive(false);
        crosshair.SetActive(false);
        winPanel.SetActive(true);
        string result = hiderScore > seekerScore ? "HIDER WON!" : "SEEKER WON!";

        if (winText != null)
        {
            winText.text = $"{result}\nSeeker: {seekerScore} | Hider: {hiderScore}";
        }

        // Sync to Hider
        gameFlowManager.GameEndedServerRpc(hiderScore, seekerScore, false);
    }

    private void DeclareTimeUpWinner()
    {
        timerText.gameObject.SetActive(false);
        crosshair.SetActive(false);
        winPanel.SetActive(true);
        if (winText != null)
        {
            winText.text = $"TIME'S UP!\nHIDER WON!\nSeeker: {seekerScore} | Hider: {hiderScore}";
        }

        // Sync to Hider
        gameFlowManager.GameEndedServerRpc(hiderScore, seekerScore, true);
    }

    public void ShowEndScreen(int hScore, int sScore, bool timeUp)
    {
        winPanel.SetActive(true);
        string result = "";
        
        if (timeUp) result = "TIME'S UP!\nHIDER WON!";
        else result = hScore > sScore ? "HIDER WON!" : "SEEKER WON!";

        if (winText != null)
        {
            winText.text = $"{result}\nSeeker: {sScore} | Hider: {hScore}";
        }
    }

    // Connect this to the OnClick() event of your Restart Button
    public void RestartGame()
    {
        // 1. Destroy the placed chameleon AND clear the reference
        if (placementScript.CurrentChameleon != null)
        {
            Destroy(placementScript.CurrentChameleon);
            
        }

        // 2. Wipe the AR Environment
        UnityEngine.XR.ARFoundation.ARSession arSession = FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARSession>();
        if (arSession != null)
        {
            arSession.Reset();
        }

        // 3. RE-ENABLE AR PLANE MANAGER! (It was turned off when you placed the first chameleon)
        UnityEngine.XR.ARFoundation.ARPlaneManager planeManager = FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARPlaneManager>();
        if (planeManager != null)
        {
            planeManager.enabled = true; // Turn plane detection back on
        }

        // 4. Reset all Seeker scores and timers
        hiderScore = 0;
        seekerScore = 0;
        seekerTimer = 90f; // Reset to 1:30
        gameFlowManager.seekerTurn = false;
        scoringRoutineRunning = false;

        // 5. Reset UI
        winPanel.SetActive(false);
        paintingUI.SetActive(true);
        colorPanel.SetActive(true);
    }
}