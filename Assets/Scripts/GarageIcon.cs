using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GarageIcon (robust) - updated to avoid race-condition on exit and to optionally hide a minimap canvas when entering garage.
/// - On Enter (TryOpenGarage): teleports player to garageTeleportPoint, disables controls, shows garage UI, and disables minimapCanvas (if assigned).
/// - On GarageOutButtonPressed: restores player (asks GarageCarSelector to cleanup previews), waits one frame, teleports to outOfGaragePoint, rebinds camera, re-enables controls, hides garage UI.
/// - Does NOT automatically re-enable minimapCanvas on exit — you handle turning it back on via OnClick/Inspector as requested.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GarageIcon : MonoBehaviour
{
    [Header("Activation")]
    public Collider2D activationCollider;   // optional; if null uses distance
    public float activationDistance = 3f;
    public KeyCode activationKey = KeyCode.Return;
    public string playerTag = "Player Racer";

    [Header("Teleport Points (RacePoint components)")]
    public RacePoint garageTeleportPoint;
    public RacePoint outOfGaragePoint;

    [Header("UI")]
    public GameObject garageUI;
    public Button garageOutButton;

    [Header("Optional UI")]
    [Tooltip("Minimap canvas (or parent GameObject) to disable when entering garage. Optional.")]
    public GameObject minimapCanvas;

    // runtime
    bool playerInside = false;
    GameObject playerObj = null;

    // store disabled components so we can re-enable exactly those later
    Dictionary<GameObject, List<Behaviour>> disabledBehaviours = new Dictionary<GameObject, List<Behaviour>>();

    // internals to support temporary disabling
    SpriteRenderer iconRenderer;
    bool temporarilyDisabled = false;

    // store the real player instance when opening the garage (safe fallback)
    GameObject originalPlayerStored = null;

    void Reset()
    {
        if (activationCollider == null)
            activationCollider = GetComponent<Collider2D>();
    }

    void Awake()
    {
        iconRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        if (garageOutButton != null)
        {
            garageOutButton.onClick.RemoveListener(OnGarageOutButtonPressed);
            garageOutButton.onClick.AddListener(OnGarageOutButtonPressed);
        }
    }

    void Update()
    {
        if (temporarilyDisabled) return;

        if (playerInside && playerObj != null)
        {
            bool stillInRange = false;
            if (activationCollider != null)
            {
                if (activationCollider.enabled)
                    stillInRange = activationCollider.bounds.Contains(playerObj.transform.position);
                else
                    stillInRange = Vector2.Distance(playerObj.transform.position, transform.position) <= activationDistance;
            }
            else
            {
                stillInRange = Vector2.Distance(playerObj.transform.position, transform.position) <= activationDistance;
            }

            if (!stillInRange)
            {
                playerInside = false;
                playerObj = null;
            }
            else
            {
                if (Input.GetKeyDown(activationKey)|| MobileInput.GetEnterDown())
                    TryOpenGarage(playerObj);
                return;
            }
        }

        GameObject player = FindPlayerByTag();
        if (player == null) return;

        if (activationCollider != null)
        {
            if (activationCollider.bounds.Contains(player.transform.position))
            {
                playerInside = true;
                playerObj = player;
                if (Input.GetKeyDown(activationKey)|| MobileInput.GetEnterDown()) TryOpenGarage(player);
                return;
            }
        }
        else
        {
            float d = Vector2.Distance(player.transform.position, transform.position);
            if (d <= activationDistance)
            {
                playerInside = true;
                playerObj = player;
                if (Input.GetKeyDown(activationKey)|| MobileInput.GetEnterDown()) TryOpenGarage(player);
                return;
            }
        }

        playerInside = false;
        playerObj = null;
    }

    GameObject FindPlayerByTag()
    {
        if (string.IsNullOrEmpty(playerTag)) return null;
        return GameObject.FindGameObjectWithTag(playerTag);
    }

    void TryOpenGarage(GameObject player)
    {
        if (WantedSystem.Instance != null && !WantedSystem.Instance.CanEnterGarage())
            return;        
        if (player == null) return;

        // store original player reference for safe-keeping (will be used on exit if preview hides it)
        originalPlayerStored = player;

        // 1) teleport player TO garageTeleportPoint using RacePoint
        if (garageTeleportPoint != null)
        {
            PlayerTeleportHelper.TeleportPlayer(player, garageTeleportPoint.WorldPosition, garageTeleportPoint.zRotation);
        }
        else
        {
            Debug.LogWarning("[GarageIcon] garageTeleportPoint not assigned.");
        }

        // 2) disable player controls (TopDownCarController, CarInputHandler etc)
        DisablePlayerControls(player);

        // 3) show garage UI
        if (garageUI != null) garageUI.SetActive(true);

        // 4) hide minimap if assigned (user will handle re-enabling when exiting)
        if (minimapCanvas != null)
        {
            try
            {
                minimapCanvas.SetActive(false);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GarageIcon] Failed to disable minimapCanvas: " + ex.Message);
            }
        }
    }

    void DisablePlayerControls(GameObject player)
    {
        if (player == null) return;

        if (disabledBehaviours.ContainsKey(player))
            disabledBehaviours[player].Clear();
        else
            disabledBehaviours[player] = new List<Behaviour>();

        // Common control components to disable — extend list if your project uses different names
        var tdc = player.GetComponent<TopDownCarController>() ?? player.GetComponentInChildren<TopDownCarController>();
        if (tdc != null && tdc.enabled)
        {
            disabledBehaviours[player].Add(tdc);
            tdc.enabled = false;
        }

        var input = player.GetComponent<CarInputHandler>() ?? player.GetComponentInChildren<CarInputHandler>();
        if (input != null && input.enabled)
        {
            disabledBehaviours[player].Add(input);
            input.enabled = false;
        }

        // Optionally, freeze physics movement so the car doesn't slide during UI
        var rb = player.GetComponent<Rigidbody2D>() ?? player.GetComponentInChildren<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = false;
        }

        // store reference for restoration
        playerObj = player;
    }

    void EnablePlayerControls(GameObject player)
    {
        if (player == null) return;
        if (!disabledBehaviours.ContainsKey(player)) return;

        foreach (var b in disabledBehaviours[player])
        {
            if (b == null) continue;
            b.enabled = true;
        }
        disabledBehaviours.Remove(player);

        var rb = player.GetComponent<Rigidbody2D>() ?? player.GetComponentInChildren<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
        }
    }

    /// <summary>
    /// Entry for Out button: starts coroutine to safely restore and teleport player.
    /// </summary>
    public void OnGarageOutButtonPressed()
    {
        StartCoroutine(HandleGarageOutCoroutine());
    }

    /// <summary>
    /// Coroutine that:
    /// 1) Calls RestoreOriginalPlayerAndDestroyPreview() on all GarageCarSelector instances (including inactive scene instances),
    /// 2) Waits one frame to allow Unity to process Destroy()/SetActive(),
    /// 3) Finds the active player GameObject by tag (or uses originalPlayerStored as fallback),
    /// 4) Teleports it to outOfGaragePoint, rebinds camera (immediate + next frame), re-enables controls, hides UI.
    /// </summary>
    IEnumerator HandleGarageOutCoroutine()
    {
        // 1) Request all selectors to restore preview (if any)
        var allSelectors = Resources.FindObjectsOfTypeAll(typeof(GarageCarSelector)) as GarageCarSelector[];
        if (allSelectors != null)
        {
            foreach (var s in allSelectors)
            {
                if (s == null) continue;

                // Only operate on scene instances (avoid calling methods on asset prefabs)
#if UNITY_EDITOR
                // In editor, skip assets by checking scene validity of the GameObject
                if (s.gameObject.scene.name == null) continue;
#endif
                try
                {
                    s.RestoreOriginalPlayerAndDestroyPreview();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[GarageIcon] Exception while calling RestoreOriginal on selector: " + ex.Message);
                }
            }
        }

        // 2) WAIT ONE FRAME to allow SetActive(true) / Destroy() to complete
        yield return null;

        // 3) Find the active player by tag (active objects only)
        GameObject p = FindPlayerByTag();

        // 4) If still not found, try the stored original (may be inactive), reactivate it and use it
        if (p == null && originalPlayerStored != null)
        {
            try
            {
                if (!originalPlayerStored.activeInHierarchy)
                    originalPlayerStored.SetActive(true);
            }
            catch { /* ignore */ }
            p = originalPlayerStored;
        }

        // 5) Fallback: use cached playerObj
        if (p == null) p = playerObj;

        if (p == null)
        {
            Debug.LogWarning("[GarageIcon] Can't find player to exit garage after restore.");
            if (garageUI != null) garageUI.SetActive(false);
            yield break;
        }

        // 6) teleport player to outOfGaragePoint first (if assigned)
        if (outOfGaragePoint != null)
        {
            PlayerTeleportHelper.TeleportPlayer(p, outOfGaragePoint.WorldPosition, outOfGaragePoint.zRotation);
        }
        else
        {
            Debug.LogWarning("[GarageIcon] outOfGaragePoint not assigned. Player will not be teleported out.");
        }

        // 7) Rebind Cinemachine immediately and again next frame to be robust
        try { CinemachineTargetBinder.SetTargetStatic(p.transform); } catch { }
        yield return null;
        try { CinemachineTargetBinder.SetTargetStatic(p.transform); } catch { }

        // 8) re-enable controls
        EnablePlayerControls(p);

        // 9) hide garage UI
        if (garageUI != null)
        {
            garageUI.SetActive(false);
        }

        // clear stored references
        playerObj = null;
        originalPlayerStored = null;
    }

    /// <summary>
    /// Called when we replace the player GameObject (ConfirmSelection).
    /// Transfer disabled-controls state from oldPlayer -> newPlayer so we keep 'disabled' until player presses Out button.
    /// </summary>
    public void TransferDisabledControls(GameObject oldPlayer, GameObject newPlayer)
    {
        if (newPlayer == null) return;

        // if we have disabled component list for oldPlayer, map them to newPlayer
        if (oldPlayer != null && disabledBehaviours.ContainsKey(oldPlayer))
        {
            var oldList = disabledBehaviours[oldPlayer];
            var newList = new List<Behaviour>();

            foreach (var oldB in oldList)
            {
                if (oldB == null) continue;
                var t = oldB.GetType();
                // search component on newPlayer (including children)
                var comp = newPlayer.GetComponentInChildren(t, true) as Behaviour;
                if (comp != null)
                {
                    // disable it to keep consistent with state
                    if (comp.enabled) comp.enabled = false;
                    newList.Add(comp);
                }
            }

            // also ensure rigidbody simulated false
            var rb = newPlayer.GetComponent<Rigidbody2D>() ?? newPlayer.GetComponentInChildren<Rigidbody2D>();
            if (rb != null) rb.simulated = false;

            // remove old key, add new key
            disabledBehaviours.Remove(oldPlayer);
            disabledBehaviours[newPlayer] = newList;

            // update playerObj to point to newPlayer
            playerObj = newPlayer;
            return;
        }

        // else no mapping existed; just ensure newPlayer is marked as the one to restore later
        playerObj = newPlayer;
        disabledBehaviours[newPlayer] = new List<Behaviour>(); // empty list
        var rbd = newPlayer.GetComponent<Rigidbody2D>() ?? newPlayer.GetComponentInChildren<Rigidbody2D>();
        if (rbd != null) rbd.simulated = false;
    }

    // -----------------------
    // NEW: allow external scripts to temporarily disable this GarageIcon
    // -----------------------
    public void SetTemporarilyDisabled(bool disabled)
    {
        temporarilyDisabled = disabled;

        if (iconRenderer == null) iconRenderer = GetComponent<SpriteRenderer>();
        if (iconRenderer != null) iconRenderer.enabled = !disabled;

        if (activationCollider != null) activationCollider.enabled = !disabled;

        if (disabled)
        {
            playerInside = false;
            playerObj = null;
        }
    }
}
