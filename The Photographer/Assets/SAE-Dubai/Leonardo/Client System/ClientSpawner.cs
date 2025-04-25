using System.Collections.Generic;
using System.Linq;
using SAE_Dubai.Leonardo.CameraSys;
using UnityEngine;

namespace SAE_Dubai.Leonardo.Client_System
{
    public class ClientSpawner : MonoBehaviour
    {
        [Header("- Spawning Configuration")]
        [SerializeField] private GameObject clientPrefab;

        [SerializeField] private List<ClientData> clientArchetypes;
        [SerializeField] private List<Transform> spawnPoints;
        [SerializeField] private int numberOfClientsToSpawn = 3;

        [Header("- Evaluation Settings")]
        [SerializeField] private LayerMask portraitSubjectLayer;

        [SerializeField] private float maxDetectionDistance = 20f;
        [SerializeField] [Range(0f, 0.5f)] private float detectionRadius = 0.1f;
        [SerializeField] private bool showEvaluatorDebugInfo;

        [Header("- System References")]
        [SerializeField] private string cameraManagerTag = "CameraManager";

        private CameraManager cameraManager;
        private CameraSystem currentCameraSystem;

        private List<ClientJobController> activeClientInstances = new();
        private List<Transform> availableSpawnPoints;

        void Start() {
            cameraManager = GameObject.FindWithTag(cameraManagerTag)?.GetComponent<CameraManager>();

            if (cameraManager == null) {
                Debug.LogError("ClientSpawner: CameraManager not found! Make sure it has the correct tag.", this);
                return;
            }

            if (clientPrefab == null) {
                Debug.LogError("ClientSpawner: Client Prefab is not assigned!", this);
                return;
            }

            if (clientArchetypes == null || clientArchetypes.Count == 0) {
                Debug.LogError("ClientSpawner: No ClientData archetypes assigned!", this);
                return;
            }

            if (spawnPoints == null || spawnPoints.Count < numberOfClientsToSpawn) {
                Debug.LogError(
                    $"ClientSpawner: Not enough spawn points assigned! Need at least {numberOfClientsToSpawn}.", this);
                return;
            }

            SubscribeToActiveCamera();

            availableSpawnPoints = new List<Transform>(spawnPoints);
            SpawnInitialClients();
        }

        private void SubscribeToActiveCamera() {
            if (currentCameraSystem != null) {
                currentCameraSystem.OnPhotoCapture -= HandlePhotoCaptured;
                currentCameraSystem = null;
            }

            if (cameraManager != null) {
                currentCameraSystem =
                    cameraManager.GetActiveCamera();

                if (currentCameraSystem != null) {
                    Debug.Log("ClientSpawner: Found active camera, subscribing to photo events.");
                    currentCameraSystem.OnPhotoCapture += HandlePhotoCaptured;
                }
                else {
                    Debug.Log("ClientSpawner: No active camera found. Will try again on next update.");
                }
            }
        }

        void Update() {
            if (cameraManager != null &&
                (currentCameraSystem == null || !currentCameraSystem.gameObject.activeInHierarchy)) {
                SubscribeToActiveCamera();
            }
        }

        void OnDestroy() {
            if (currentCameraSystem != null) {
                currentCameraSystem.OnPhotoCapture -= HandlePhotoCaptured;
            }
        }

        public void SpawnInitialClients() {
            if (availableSpawnPoints.Count < numberOfClientsToSpawn) {
                Debug.LogWarning("ClientSpawner: Not enough available spawn points to spawn initial clients.");
                numberOfClientsToSpawn = availableSpawnPoints.Count;
            }


            for (int i = 0; i < numberOfClientsToSpawn; i++) {
                SpawnNewClient();
            }
        }

        private bool SpawnNewClient() {
            if (availableSpawnPoints.Count == 0) {
                Debug.LogWarning("ClientSpawner: No available spawn points left.");
                return false;
            }

            // Select random spawn point and archetype.
            int spawnIndex = Random.Range(0, availableSpawnPoints.Count);
            Transform spawnPoint = availableSpawnPoints[spawnIndex];
            availableSpawnPoints.RemoveAt(spawnIndex);

            ClientData chosenArchetype = clientArchetypes[Random.Range(0, clientArchetypes.Count)];

            // Generate specific requirements.
            List<PortraitShotType> potentialShots = chosenArchetype.potentialShotTypes
                .Where(st => st != PortraitShotType.Undefined)
                .Distinct()
                .ToList();
            int numShotsToRequire =
                Random.Range(chosenArchetype.minRequiredShots, chosenArchetype.maxRequiredShots + 1);
            numShotsToRequire = Mathf.Min(numShotsToRequire, potentialShots.Count);

            List<PortraitShotType> specificRequirements = potentialShots
                .OrderBy(x => Random.value)
                .Take(numShotsToRequire)
                .ToList();


            int reward = Random.Range(chosenArchetype.minReward, chosenArchetype.maxReward + 1);

            GameObject clientInstanceGO = Instantiate(clientPrefab, spawnPoint.position, spawnPoint.rotation);
            ClientJobController clientController = clientInstanceGO.GetComponent<ClientJobController>();

            if (clientController != null) {
                clientController.SetupJob(chosenArchetype, specificRequirements, reward);
                clientController.OnJobCompleted += HandleClientJobCompleted;
                activeClientInstances.Add(clientController);
                return true;
            }
            else {
                Debug.LogError(
                    $"ClientSpawner: Client Prefab '{clientPrefab.name}' is missing the ClientJobController script!",
                    clientPrefab);
                Destroy(clientInstanceGO);
                availableSpawnPoints.Add(spawnPoint);
                return false;
            }
        }


        /// <summary>
        /// Handles the event when the CameraSystem captures a photo.
        /// Evaluates composition and routes the photo to the relevant client.
        /// </summary>
        private void HandlePhotoCaptured(CapturedPhoto photo) {
            Debug.Log("ClientSpawner: Photo captured event received.");

            // 1. Get the active camera from the CameraSystem.
            Camera activeCamera = null;
            if (currentCameraSystem != null && currentCameraSystem.isCameraOn) {
                activeCamera = currentCameraSystem.usingViewfinder
                    ? currentCameraSystem.viewfinderCamera
                    : currentCameraSystem.cameraRenderer;
            }

            if (activeCamera == null) {
                activeCamera = Camera.main; // Fallback (?).
            }

            if (activeCamera == null) {
                Debug.LogError("ClientSpawner.cs: Could not find an active camera for evaluation.");
                return;
            }


            // 2. Evaluate composition using the static evaluator.
            Transform subjectTransform;
            PortraitShotType? evaluatedShotType = PhotoCompositionEvaluator.EvaluateComposition(
                activeCamera,
                portraitSubjectLayer,
                maxDetectionDistance,
                detectionRadius,
                out subjectTransform,
                showEvaluatorDebugInfo
            );

            // 3. Tag the photo object with the result.
            photo.portraitShotType = evaluatedShotType;

            // 4. Find which client (if any) was photographed.
            if (subjectTransform != null && evaluatedShotType.HasValue) {
                ClientJobController targetClient = subjectTransform.GetComponent<ClientJobController>();

                if (targetClient != null && activeClientInstances.Contains(targetClient)) {
                    // 5. Route the evaluated photo to that specific client.
                    Debug.Log(
                        $"ClientSpawner.cs: Routing photo ({evaluatedShotType.Value}) to client '{targetClient.clientName}'.");
                    targetClient.CheckPhoto(photo);
                }
                else {
                    if (showEvaluatorDebugInfo)
                        Debug.Log(
                            $"ClientSpawner: Photographed subject '{subjectTransform.name}' is not an active client or has no ClientJobController.");
                }
            }
            else {
                if (showEvaluatorDebugInfo)
                    Debug.Log("ClientSpawner.cs: Photo did not contain a valid, evaluated portrait subject.");
            }

            //Debug.Log($"Camera used for evaluation: {activeCamera.name}, Layer mask: {portraitSubjectLayer.value}");
        }

        /// <summary>
        /// Handles a client completing their job.
        /// </summary>
        private void HandleClientJobCompleted(ClientJobController completedClient) {
            Debug.Log($"ClientSpawner: Received completion from '{completedClient.clientName}'.");

            // Unsubscribe and remove from active list.
            completedClient.OnJobCompleted -= HandleClientJobCompleted;
            activeClientInstances.Remove(completedClient);

            // Make the spawn point available again (find which one it used - todo: might need to store this).
            // For simplicity now, we just add *a* spawn point back if possible.
            // A better way would be to associate the client instance with its spawn point transform.
            
            // FindObjectOfType<SimpleSpawnPointManager>()?.FreeSpawnPoint(completedClient.transform.position); 
        }
    }
}