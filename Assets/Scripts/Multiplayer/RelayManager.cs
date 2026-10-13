using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using TMPro;

public class RelayManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField joinCodeInput;
    public TMP_Text statusText;
    public TMP_Text joinCodeDisplay; // shows the code when hosting
    [SerializeField] private GameObject Multiplayer_canvas;
    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            Debug.Log($"[Relay] Client Connected! ID: {clientId}");
            SetStatus($"Player {clientId} joined!");
        };

        NetworkManager.Singleton.OnClientDisconnectCallback += (clientId) =>
        {
            Debug.Log($"[Relay] Client Disconnected! ID: {clientId}");
            SetStatus($"Player {clientId} left.");
        };
    }

    private async Task InitServices()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    public async void HostGame()
    {
        ElementHider();

        SetStatus("Initializing…");
        await InitServices();

        SetStatus("Creating Relay allocation…");
        var allocation = await RelayService.Instance.CreateAllocationAsync(10);

        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        if (joinCodeDisplay != null) joinCodeDisplay.text = $"Join Code: {joinCode}";
        SetStatus($"Hosting! Code: {joinCode}");

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetHostRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData);

        NetworkManager.Singleton.StartHost();
        
    }

    public async void JoinGame()
    {
        string code = joinCodeInput != null ? joinCodeInput.text.Trim() : "";
        if (string.IsNullOrEmpty(code)) { SetStatus("Enter a join code."); return; }

        ElementHider();

        SetStatus("Initializing…");
        await InitServices();

        SetStatus($"Joining {code}…");
        var allocation = await RelayService.Instance.JoinAllocationAsync(code);

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetClientRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData,
            allocation.HostConnectionData);

        NetworkManager.Singleton.StartClient();
        SetStatus("Connected!");
        
    }

    public void ElementHider()
    {
        if (Multiplayer_canvas == null)
        {
            Debug.LogError("[RelayManager] Multiplayer_canvas is NULL! Check the Inspector reference.");
            return;
        }
        Multiplayer_canvas.SetActive(false);
    }
    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log("[Relay] " + msg);
    }
}