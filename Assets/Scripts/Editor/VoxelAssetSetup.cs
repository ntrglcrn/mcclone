using UnityEngine;

public static class VoxelGameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        Physics.gravity = new Vector3(0f, -6f, 0f);

        SkyboxSetup   skyboxSetup   = SkyboxSetup.EnsureInScene();
        LightingSetup lightingSetup = LightingSetup.EnsureInScene();

        // ── ChunkManager ──────────────────────────────────────────────────────
        ChunkManager chunkManager = Object.FindObjectOfType<ChunkManager>();
        if (chunkManager == null)
        {
            GameObject cmObj = new GameObject("ChunkManager");
            chunkManager = cmObj.AddComponent<ChunkManager>();
        }

        // ── Player ────────────────────────────────────────────────────────────
        PlayerController playerController = Object.FindObjectOfType<PlayerController>();
        if (playerController == null)
        {
            GameObject playerObject = new GameObject("Player");

            CharacterController cc = GetOrAddComponent<CharacterController>(playerObject);
            cc.enabled = false;
            playerObject.transform.position = new Vector3(Chunk.Size * 0.5f, 20f, Chunk.Size * 0.5f);
            cc.enabled = true;

            Inventory        inventory = GetOrAddComponent<Inventory>(playerObject);
            playerController           = GetOrAddComponent<PlayerController>(playerObject);
            BlockInteraction bi        = GetOrAddComponent<BlockInteraction>(playerObject);

            Camera camera = GetOrCreateCamera(playerObject.transform);
            ApplyCameraSettings(camera);
            skyboxSetup.Apply(camera);
            lightingSetup.Apply(camera);
            playerController.SetCamera(camera);
            bi.Configure(camera, chunkManager, inventory);
            EnsureHud(inventory);
        }
        else
        {
            Inventory        inventory = GetOrAddComponent<Inventory>(playerController.gameObject);
            BlockInteraction bi        = GetOrAddComponent<BlockInteraction>(playerController.gameObject);
            Camera camera = playerController.PlayerCamera != null
                ? playerController.PlayerCamera
                : GetOrCreateCamera(playerController.transform);

            ApplyCameraSettings(camera);
            skyboxSetup.Apply(camera);
            lightingSetup.Apply(camera);
            playerController.SetCamera(camera);
            bi.Configure(camera, chunkManager, inventory);
            EnsureHud(inventory);
        }

        // Передаём ссылку на игрока в ChunkManager
        chunkManager.player = Object.FindObjectOfType<PlayerController>().transform;
    }

    private static Camera GetOrCreateCamera(Transform playerTransform)
    {
        Camera camera = playerTransform.GetComponentInChildren<Camera>();
        if (camera == null)
        {
            Camera existing = Camera.main != null ? Camera.main : Object.FindObjectOfType<Camera>();
            if (existing != null)
            {
                camera = existing;
                camera.transform.SetParent(playerTransform, false);
            }
            else
            {
                GameObject camObj = new GameObject("PlayerCamera");
                camObj.transform.SetParent(playerTransform, false);
                camera = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }
        }

        camera.gameObject.tag = "MainCamera";
        camera.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        camera.transform.localRotation = Quaternion.identity;
        return camera;
    }

    private static void ApplyCameraSettings(Camera camera)
    {
        camera.clearFlags      = CameraClearFlags.Skybox;
        camera.backgroundColor = Color.black;
        camera.nearClipPlane   = 0.01f;
        camera.farClipPlane    = 500f;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T c = target.GetComponent<T>();
        return c != null ? c : target.AddComponent<T>();
    }

    private static void EnsureHud(Inventory inventory)
    {
        HUD hud = Object.FindObjectOfType<HUD>();
        if (hud == null)
            hud = new GameObject("HUD").AddComponent<HUD>();
        hud.SetInventory(inventory);
    }
}
