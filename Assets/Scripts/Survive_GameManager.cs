using Unity.Netcode;
using UnityEngine;

// Save as GameManager.cs in Unity (replace the previous GameManager).
public class GameManager : MonoBehaviour
{
    [SerializeField] private MultiplayerUI multiplayerUI;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private NetworkManager net;
    private int nextSpawn;

    private void Start()
    {
        net = NetworkManager.Singleton;
        if (net == null)
        {
            Debug.LogError("Survive: NetworkManager is missing.");
            return;
        }

        net.OnClientConnectedCallback += SpawnPlayer;
        if (multiplayerUI != null)
        {
            multiplayerUI.OnStartHost += StartHost;
            multiplayerUI.OnStartClient += StartClient;
            multiplayerUI.OnDiconnectClient += Disconnect;
        }
        if (net.IsServer)
            foreach (ulong id in net.ConnectedClientsIds) SpawnPlayer(id);
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (net == null || !net.IsServer || playerPrefab == null) return;
        if (!net.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject != null) return;

        Vector3 pos = new Vector3(0f, 2f, 0f);
        Quaternion rot = Quaternion.identity;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform point = spawnPoints[nextSpawn++ % spawnPoints.Length];
            if (point != null) { pos = point.position; rot = point.rotation; }
        }

        GameObject player = Instantiate(playerPrefab, pos, rot);
        NetworkObject obj = player.GetComponent<NetworkObject>();
        if (obj == null)
        {
            Debug.LogError("Survive: Player prefab needs NetworkObject.");
            Destroy(player);
            return;
        }
        obj.SpawnAsPlayerObject(clientId, true);
    }

    public void StartHost()
    {
        if (net != null && !net.IsListening && net.StartHost())
            if (multiplayerUI != null) multiplayerUI.DisableButtons();
    }

    public void StartClient()
    {
        if (net != null && !net.IsListening && net.StartClient())
            if (multiplayerUI != null) multiplayerUI.DisableButtons();
    }

    public void Disconnect()
    {
        if (net != null && net.IsListening) net.Shutdown();
        if (multiplayerUI != null) multiplayerUI.EnableButtons();
    }

    private void OnDestroy()
    {
        if (net != null) net.OnClientConnectedCallback -= SpawnPlayer;
        if (multiplayerUI != null)
        {
            multiplayerUI.OnStartHost -= StartHost;
            multiplayerUI.OnStartClient -= StartClient;
            multiplayerUI.OnDiconnectClient -= Disconnect;
        }
    }
}
