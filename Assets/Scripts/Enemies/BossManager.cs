using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class BossManager : MonoBehaviour
{
    public enum EncounterState { Sleeping, Active, Died, Awakening, Dying, Resetting }

    [Header("Tree Boss Encounter")]
    [SerializeField] private TreeBoss treeBoss;
    [SerializeField] private Collider2D bossTrigger;
    [SerializeField] private GameObject bossPanel;
    [SerializeField, Tooltip("Existing scene object activated when TreeBoss dies.")]
    private GameObject treasure;
    [SerializeField] private Vector2 treeArenaCenter = new Vector2(-135f, 14.5f);
    [SerializeField, Min(0.01f)] private float treeArenaWidth = 30f;
    [SerializeField, Min(0f)] private float awakeningDuration = 2f;
    [SerializeField, Min(0f)] private float awakeningCameraShake = 0.12f;
    [SerializeField] private GameObject sealDoor;
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField, Min(0.01f)] private float cameraBlendTime = 1.5f;

    private EncounterState state;
    private CinemachineBrain brain;
    private Camera outputCamera;
    private CinemachineCamera arenaCamera;
    private CinemachineBlenderSettings originalBlends;
    private CinemachineBlenderSettings encounterBlends;
    private Coroutine encounterRoutine;
    private Coroutine checkpointShakeRoutine;
    private Player encounterPlayer;
    private bool ownsBossState;
    private StateManager encounterStateManager;

    public EncounterState State => state;

    private void Start()
    {
        outputCamera = Camera.main;
        if (outputCamera != null)
            brain = outputCamera.GetComponent<CinemachineBrain>();
        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<CinemachineCamera>();

        if (treeBoss == null || bossTrigger == null || sealDoor == null || playerCamera == null || brain == null)
        {
            Debug.LogError("BossManager needs a TreeBoss, Boss Trigger collider, Seal Door, player Cinemachine camera, and main-camera CinemachineBrain.", this);
            enabled = false;
            return;
        }

        state = EncounterState.Sleeping;
        if (treasure != null) treasure.SetActive(false);
        treeBoss.Died += OnBossDied;
        treeBoss.CheckpointReached += OnCheckpointReached;
        treeBoss.gameObject.SetActive(false);
        bossTrigger.isTrigger = true;
        BossEncounterTrigger trigger = bossTrigger.GetComponent<BossEncounterTrigger>();
        if (trigger == null) trigger = bossTrigger.gameObject.AddComponent<BossEncounterTrigger>();
        trigger.Initialize(this);
        if (bossPanel != null)
        {
            foreach (BossHealthBar bar in bossPanel.GetComponentsInChildren<BossHealthBar>(true))
                bar.SetBoss(treeBoss);
            bossPanel.SetActive(false);
        }
        sealDoor.SetActive(false);
    }

    private void Update()
    {
        if ((state == EncounterState.Active || state == EncounterState.Awakening) && arenaCamera != null)
            UpdateArenaLens();
    }

    public void ActivateEncounter()
    {
        if (!isActiveAndEnabled || state != EncounterState.Sleeping || brain == null || playerCamera == null) return;
        encounterPlayer = FindFirstObjectByType<Player>();
        encounterStateManager = StateManager.Instance;
        if (encounterPlayer == null || encounterPlayer.IsDying || !encounterStateManager.IsGameplay ||
            encounterStateManager.CurrentState == GameState.Boss || !encounterStateManager.RequestState(GameState.Boss)) return;
        ownsBossState = true;
        encounterPlayer.Died += OnPlayerDied;
        GameObject cameraObject = new GameObject("Boss Arena Camera");
        cameraObject.SetActive(false);
        arenaCamera = cameraObject.AddComponent<CinemachineCamera>();
        arenaCamera.OutputChannel = playerCamera.OutputChannel;
        arenaCamera.Lens = playerCamera.Lens;
        // Default horizontal bounds: -150 to -120.
        cameraObject.transform.position = new Vector3(treeArenaCenter.x, treeArenaCenter.y, outputCamera.transform.position.z);
        cameraObject.transform.rotation = Quaternion.identity;
        UpdateArenaLens();
        arenaCamera.Priority = (int)playerCamera.Priority + 1;
        ConfigureEncounterBlends();

        state = EncounterState.Awakening;
        cameraObject.SetActive(true);
        sealDoor.SetActive(true);
        EnemyPoolSpawner.Instance?.DespawnAll();
        encounterRoutine = StartCoroutine(AwakenTreeBoss());
    }

    private IEnumerator AwakenTreeBoss()
    {
        // Complete the smooth move to the fixed arena view before revealing the boss.
        yield return null;
        while (brain != null && brain.IsBlending) yield return null;
        treeBoss.gameObject.SetActive(true);
        treeBoss.SetPresentationAlpha(0f);
        if (bossPanel != null) bossPanel.SetActive(true);
        Vector3 cameraPosition = arenaCamera.transform.position;
        float elapsed = 0f;
        while (elapsed < awakeningDuration)
        {
            treeBoss.SetPresentationAlpha(elapsed / awakeningDuration);
            if (Time.deltaTime > 0f)
                arenaCamera.transform.position = cameraPosition + (Vector3)(Random.insideUnitCircle * awakeningCameraShake);
            yield return null;
            elapsed += Time.deltaTime;
        }
        arenaCamera.transform.position = cameraPosition;
        treeBoss.SetPresentationAlpha(1f);
        treeBoss.BeginFight();
        state = EncounterState.Active;
        encounterRoutine = null;
    }

    private void UpdateArenaLens()
    {
        LensSettings lens = arenaCamera.Lens;
        lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
        // Also keep the skills' Y=7..22 arena visible on wide screens.
        lens.OrthographicSize = Mathf.Max(7.5f, treeArenaWidth * 0.5f / Mathf.Max(0.01f, outputCamera.aspect));
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
        state = EncounterState.Dying;
        if (treasure != null) treasure.SetActive(true);
        LeaveBossState();
        StopCheckpointShake();
        if (bossPanel != null) bossPanel.SetActive(false);
        encounterRoutine = StartCoroutine(FinishTreeBossEncounter());
    }

    private void LeaveBossState()
    {
        if (encounterPlayer != null) encounterPlayer.Died -= OnPlayerDied;
        if (!ownsBossState) return;
        ownsBossState = false;
        if (encounterStateManager != null) encounterStateManager.CloseState(GameState.Boss);
    }

    private void OnPlayerDied()
    {
        if (encounterRoutine != null) StopCoroutine(encounterRoutine);
        encounterRoutine = null;
        StopCheckpointShake();
        if (treeBoss != null) treeBoss.gameObject.SetActive(false);
        if (bossPanel != null) bossPanel.SetActive(false);
        if (sealDoor != null) sealDoor.SetActive(false);
        LeaveBossState();
        // A failed attempt can be triggered again after respawning.
        state = EncounterState.Resetting;
        if (arenaCamera != null)
        {
            arenaCamera.gameObject.SetActive(false);
            encounterRoutine = StartCoroutine(RestoreFailedEncounter());
        }
        else state = EncounterState.Sleeping;
    }

    private IEnumerator RestoreFailedEncounter()
    {
        yield return RestoreAfterBlend();
        encounterRoutine = null;
        state = EncounterState.Sleeping;
    }

    private Vector3 checkpointCameraPosition;
    private void OnCheckpointReached()
    {
        if (arenaCamera == null || state != EncounterState.Active) return;
        StopCheckpointShake();
        checkpointCameraPosition = arenaCamera.transform.position;
        checkpointShakeRoutine = StartCoroutine(ShakeCheckpointCamera());
    }

    private IEnumerator ShakeCheckpointCamera()
    {
        float elapsed = 0f;
        while (elapsed < 1f && arenaCamera != null)
        {
            if (Time.deltaTime > 0f)
                arenaCamera.transform.position = checkpointCameraPosition + (Vector3)(Random.insideUnitCircle * awakeningCameraShake);
            yield return null;
            elapsed += Time.deltaTime;
        }
        if (arenaCamera != null) arenaCamera.transform.position = checkpointCameraPosition;
        checkpointShakeRoutine = null;
    }

    private void StopCheckpointShake()
    {
        if (checkpointShakeRoutine == null) return;
        StopCoroutine(checkpointShakeRoutine);
        checkpointShakeRoutine = null;
        if (arenaCamera != null) arenaCamera.transform.position = checkpointCameraPosition;
    }

    private IEnumerator FinishTreeBossEncounter()
    {
        // TreeBoss owns the red fade and body shake. Keep the arena sealed until it finishes.
        while (treeBoss != null && treeBoss.gameObject.activeSelf) yield return null;
        state = EncounterState.Died;
        arenaCamera.gameObject.SetActive(false);
        yield return RestoreAfterBlend();
        if (sealDoor != null) sealDoor.SetActive(false);
        encounterRoutine = null;
    }

    private IEnumerator RestoreAfterBlend()
    {
        // Let the brain begin the return blend before checking its state.
        yield return null;
        while (brain != null && brain.IsBlending)
            yield return null;
        RestoreCameraSettings();
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
        if (treeBoss != null)
        {
            treeBoss.Died -= OnBossDied;
            treeBoss.CheckpointReached -= OnCheckpointReached;
        }
        LeaveBossState();
        StopCheckpointShake();
        if (encounterRoutine != null) StopCoroutine(encounterRoutine);
        RestoreCameraSettings();
    }
}
