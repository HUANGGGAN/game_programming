using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class BossManager : MonoBehaviour
{
    public enum EncounterState { Sleeping, Active, Died }

    [SerializeField] private Transform player;
    [SerializeField] private Boss boss;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private GameObject sealDoor;
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField, Min(0.01f)] private float cameraBlendTime = 1.5f;

    private EncounterState state;
    private CinemachineBrain brain;
    private Camera outputCamera;
    private CinemachineCamera arenaCamera;
    private CinemachineBlenderSettings originalBlends;
    private CinemachineBlenderSettings encounterBlends;
    private Coroutine restoreRoutine;

    public EncounterState State => state;

    private void Start()
    {
        if (waveManager == null)
            waveManager = FindFirstObjectByType<WaveManager>();

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }
        outputCamera = Camera.main;
        if (outputCamera != null)
            brain = outputCamera.GetComponent<CinemachineBrain>();
        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<CinemachineCamera>();

        if (boss == null || sealDoor == null || playerCamera == null || brain == null)
        {
            Debug.LogError("BossManager needs a Boss, Seal Door, player Cinemachine camera, and main-camera CinemachineBrain.", this);
            enabled = false;
            return;
        }

        state = EncounterState.Sleeping;
        boss.Died += OnBossDied;
        boss.gameObject.SetActive(false);
        sealDoor.SetActive(false);
    }

    private void Update()
    {
        if (state == EncounterState.Sleeping && player != null &&
            player.position.x < -12f && player.position.y > 9f)
            ActivateEncounter();

        if (state == EncounterState.Active && arenaCamera != null)
            UpdateArenaLens();
    }

    private void ActivateEncounter()
    {
        GameObject cameraObject = new GameObject("Boss Arena Camera");
        cameraObject.SetActive(false);
        arenaCamera = cameraObject.AddComponent<CinemachineCamera>();
        arenaCamera.OutputChannel = playerCamera.OutputChannel;
        arenaCamera.Lens = playerCamera.Lens;
        // Arena corners (-28, 27) and (-11, 10): center (-19.5, 18.5), size 17 x 17.
        cameraObject.transform.position = new Vector3(-19.5f, 18.5f, outputCamera.transform.position.z);
        cameraObject.transform.rotation = Quaternion.identity;
        UpdateArenaLens();
        arenaCamera.Priority = (int)playerCamera.Priority + 1;
        ConfigureEncounterBlends();

        state = EncounterState.Active;
        cameraObject.SetActive(true);
        sealDoor.SetActive(true);
        if (waveManager != null)
            waveManager.enabled = false;
        EnemyPoolSpawner.Instance?.DespawnAll();
        Tutorial.Instance?.ShowTutorial("boss", "Each area has a boss. This time, try defeating the boss to complete the tutorial level.");
        boss.gameObject.SetActive(true);
    }

    private void UpdateArenaLens()
    {
        LensSettings lens = arenaCamera.Lens;
        lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
        lens.OrthographicSize = Mathf.Max(8.5f, 8.5f / Mathf.Max(0.01f, outputCamera.aspect));
        arenaCamera.Lens = lens;
    }

    private void ConfigureEncounterBlends()
    {
        originalBlends = brain.CustomBlends;
        encounterBlends = ScriptableObject.CreateInstance<CinemachineBlenderSettings>();
        var existing = originalBlends != null ? originalBlends.CustomBlends : null;
        encounterBlends.CustomBlends = new CinemachineBlenderSettings.CustomBlend[(existing != null ? existing.Length : 0) + 2];
        var blend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, cameraBlendTime);
        encounterBlends.CustomBlends[0] = new CinemachineBlenderSettings.CustomBlend
        {
            From = playerCamera.Name, To = arenaCamera.Name, Blend = blend
        };
        encounterBlends.CustomBlends[1] = new CinemachineBlenderSettings.CustomBlend
        {
            From = arenaCamera.Name, To = playerCamera.Name, Blend = blend
        };
        if (existing != null)
            System.Array.Copy(existing, 0, encounterBlends.CustomBlends, 2, existing.Length);
        brain.CustomBlends = encounterBlends;
    }

    private void OnBossDied()
    {
        if (state != EncounterState.Active)
            return;
        state = EncounterState.Died;
        sealDoor.SetActive(false);
        arenaCamera.gameObject.SetActive(false);
        restoreRoutine = StartCoroutine(RestoreAfterBlend());
        Tutorial.Instance?.ShowTutorial("boss_defeated", "Congratulations! You have defeated the boss and completed the tutorial level. You may leave the game now.");
    }

    private IEnumerator RestoreAfterBlend()
    {
        // Let the brain begin the return blend before checking its state.
        yield return null;
        while (brain != null && brain.IsBlending)
            yield return null;
        RestoreCameraSettings();
        restoreRoutine = null;
    }

    private void RestoreCameraSettings()
    {
        if (brain != null && brain.CustomBlends == encounterBlends)
            brain.CustomBlends = originalBlends;
        if (encounterBlends != null)
            Destroy(encounterBlends);
        encounterBlends = null;
        if (arenaCamera != null)
            Destroy(arenaCamera.gameObject);
        arenaCamera = null;
    }

    private void OnDestroy()
    {
        if (boss != null)
            boss.Died -= OnBossDied;
        if (restoreRoutine != null)
            StopCoroutine(restoreRoutine);
        RestoreCameraSettings();
    }
}
