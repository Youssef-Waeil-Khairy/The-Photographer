using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SAE_Dubai.Leonardo.Client_System
{
    /// <summary>
    /// Handles spawning initial, non-session clients if needed.
    /// </summary>
    public class ClientSpawner : MonoBehaviour
    {
        [Header("- Spawning Configuration (Non-Session Clients)")]
        [SerializeField] private GameObject clientPrefab;

        [SerializeField] private List<ClientData> clientArchetypes;
        [SerializeField] private List<Transform> spawnPoints;
        [SerializeField] private int numberOfClientsToSpawn = 0;

        private List<ClientJobController> activeClientInstances = new();
        private List<Transform> availableSpawnPoints;

        void Start() {
            if (numberOfClientsToSpawn <= 0 || spawnPoints == null || spawnPoints.Count == 0) {
                Debug.Log("[ClientSpawner] No initial non-session clients configured to spawn.");
                this.enabled = false;
                return;
            }

            Debug.Log("[ClientSpawner] Initializing non-session clients.");

            if (clientPrefab == null) {
                Debug.LogError("[ClientSpawner] Client Prefab not assigned!", this);
                this.enabled = false;
                return;
            }

            if (clientArchetypes == null || clientArchetypes.Count == 0) {
                Debug.LogError("[ClientSpawner] No ClientData archetypes assigned!", this);
                this.enabled = false;
                return;
            }

            if (spawnPoints.Count < numberOfClientsToSpawn) {
                Debug.LogWarning(
                    $"[ClientSpawner] Not enough spawn points for {numberOfClientsToSpawn} non-session clients.", this);
            }

            availableSpawnPoints = new List<Transform>(spawnPoints);
            SpawnInitialClients();
        }

        public void SpawnInitialClients() {
            int clientsToActuallySpawn = Mathf.Min(numberOfClientsToSpawn, availableSpawnPoints.Count);
            if (clientsToActuallySpawn <= 0) return;

            if (clientsToActuallySpawn < numberOfClientsToSpawn) {
                Debug.LogWarning(
                    $"[ClientSpawner] Spawning {clientsToActuallySpawn} initial clients due to limited spawn points.");
            }
            else {
                Debug.Log($"[ClientSpawner] Spawning {clientsToActuallySpawn} initial clients.");
            }

            for (int i = 0; i < clientsToActuallySpawn; i++) {
                SpawnNewClient();
            }
        }

        private bool SpawnNewClient() {
            if (availableSpawnPoints == null || availableSpawnPoints.Count == 0) {
                Debug.LogWarning("[ClientSpawner] No available spawn points left.");
                return false;
            }

            int spawnIndex = Random.Range(0, availableSpawnPoints.Count);
            Transform spawnPoint = availableSpawnPoints[spawnIndex];
            if (spawnPoint == null) {
                Debug.LogError("[ClientSpawner] Null spawn point encountered!");
                return false;
            }

            availableSpawnPoints.RemoveAt(spawnIndex);

            ClientData chosenArchetype = clientArchetypes[Random.Range(0, clientArchetypes.Count)];
            List<PortraitShotType> potentialShots = chosenArchetype.potentialShotTypes
                .Where(st => st != PortraitShotType.Undefined).Distinct().ToList();

            if (potentialShots.Count == 0) {
                Debug.LogWarning(
                    $"[ClientSpawner] Archetype '{chosenArchetype.name}' has no valid potential shot types. Skipping.",
                    chosenArchetype);
                availableSpawnPoints.Add(spawnPoint);
                return false;
            }

            int numShotsToRequire =
                Random.Range(chosenArchetype.minRequiredShots, chosenArchetype.maxRequiredShots + 1);
            numShotsToRequire = Mathf.Min(numShotsToRequire, potentialShots.Count);
            List<PortraitShotType> specificRequirements =
                potentialShots.OrderBy(x => Random.value).Take(numShotsToRequire).ToList();
            int reward = Random.Range(chosenArchetype.minReward, chosenArchetype.maxReward + 1);

            GameObject clientInstanceGO = Instantiate(clientPrefab, spawnPoint.position, spawnPoint.rotation);
            ClientJobController clientController = clientInstanceGO.GetComponent<ClientJobController>();

            if (clientController != null) {
                clientController.SetupJob(chosenArchetype, specificRequirements, reward);
                clientController.OnJobCompleted += HandleClientJobCompleted;
                activeClientInstances.Add(clientController);
                Debug.Log(
                    $"[ClientSpawner] Spawned initial client '{clientController.clientName}' at {spawnPoint.name}.");
                return true;
            }
            else {
                Debug.LogError(
                    $"[ClientSpawner] Client Prefab '{clientPrefab.name}' is missing ClientJobController script!",
                    clientPrefab);
                Destroy(clientInstanceGO);
                availableSpawnPoints.Add(spawnPoint);
                return false;
            }
        }

        private void HandleClientJobCompleted(ClientJobController completedClient) {
            Debug.Log(
                $"[ClientSpawner] Received completion from non-session client '{completedClient?.clientName ?? "UNKNOWN"}'.");
            if (completedClient != null) {
                completedClient.OnJobCompleted -= HandleClientJobCompleted;
                activeClientInstances.Remove(completedClient);
                // ! TODO: Add spawn point back to availableSpawnPoints if these clients should respawn
            }
        }
    }
}