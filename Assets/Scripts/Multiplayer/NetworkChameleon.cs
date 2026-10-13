using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkChameleon : NetworkBehaviour
{
    private NetworkVariable<Vector3> sharedPos = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private NetworkVariable<Quaternion> sharedRot = new NetworkVariable<Quaternion>(
        Quaternion.identity,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Called from ARPlaceCube.SpawnChameleonServerRpc (always on server)
    public void SetSharedTransform(Vector3 pos, Quaternion rot)
    {
        sharedPos.Value = pos;
        sharedRot.Value = rot;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // ✅ Wait for position to be applied FIRST, then register with ARPlaceCube
        StartCoroutine(ApplyThenRegister());

        sharedPos.OnValueChanged += (_, __) => ApplySharedTransform();
        sharedRot.OnValueChanged += (_, __) => ApplySharedTransform();
    }

    private IEnumerator ApplyThenRegister()
    {
        // Wait until the marker has been scanned on this device
        while (SharedAROrigin.SharedOrigin == null)
        {
            Debug.Log("[NetworkChameleon] Waiting for marker scan...");
            yield return new WaitForSeconds(0.3f);
        }

        // Apply the correct world position FIRST
        ApplySharedTransform();

        Debug.Log($"[NetworkChameleon] Position applied at {transform.position}");

        // THEN tell ARPlaceCube this chameleon exists (disables planes AFTER position is correct)
        //FindAnyObjectByType<ARPlaceCube>()?.SetCurrentChameleon(this.gameObject);
    }

    private void ApplySharedTransform()
    {
        if (SharedAROrigin.SharedOrigin == null) return;

        transform.position = SharedAROrigin.FromSharedSpace(sharedPos.Value);
        transform.rotation = sharedRot.Value;

        Debug.Log($"[NetworkChameleon] ApplySharedTransform → world pos: {transform.position}");
    }
}