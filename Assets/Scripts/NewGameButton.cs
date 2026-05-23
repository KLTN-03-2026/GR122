using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NewGameButton - ALWAYS reset all saved data then load Open World (buildIndex = openWorldBuildIndex).
/// This version additionally attempts to reset MoneyManager (via PlayerPrefs keys and reflection)
/// so UI cash will show 0 immediately.
/// 
/// Hook OnNewGamePressed() to the New Game button's OnClick() in Inspector.
/// </summary>
public class NewGameButton : MonoBehaviour
{
    [Tooltip("Build index of Open World scene. Default 1.")]
    public int openWorldBuildIndex = 1;

    [Tooltip("If true, prints debug logs while resetting.")]
    public bool verboseLogs = true;

    /// <summary>
    /// Public method to hook to Button.OnClick()
    /// </summary>
    public void OnNewGamePressed()
    {
        StartCoroutine(DoResetAndLoad());
    }

    IEnumerator DoResetAndLoad()
    {
        // 0) Ensure normal timescale
        Time.timeScale = 1f;

        // 1) Delete all PlayerPrefs
        try
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            if (verboseLogs) Debug.Log("[NewGameButton] PlayerPrefs.DeleteAll() executed.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[NewGameButton] Exception while deleting PlayerPrefs: " + ex);
        }

        // 1.5) Explicitly write common money keys = 0 in case MoneyManager reads them on startup
        try
        {
            PlayerPrefs.SetInt("Game_Cash_v1", 0);
            PlayerPrefs.SetInt("Game_Cash", 0);
            PlayerPrefs.SetInt("Cash", 0);
            PlayerPrefs.SetInt("Money", 0);
            PlayerPrefs.Save();
            if (verboseLogs) Debug.Log("[NewGameButton] Wrote common money PlayerPrefs keys = 0");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[NewGameButton] Exception while setting common money keys: " + ex);
        }

        // 2) Try to reset MoneyManager singleton / component in memory via reflection
        ResetMoneyToZero(verboseLogs);

        // 3) Delete files under persistentDataPath (all files and subfolders)
        string pd = Application.persistentDataPath;
        if (!string.IsNullOrEmpty(pd))
        {
            try
            {
                if (Directory.Exists(pd))
                {
                    // Delete files
                    var files = Directory.GetFiles(pd);
                    foreach (var f in files)
                    {
                        try { File.Delete(f); if (verboseLogs) Debug.Log("[NewGameButton] Deleted file: " + f); }
                        catch (Exception e) { Debug.LogWarning("[NewGameButton] Failed delete file: " + f + " -> " + e.Message); }
                    }

                    // Delete subdirectories
                    var dirs = Directory.GetDirectories(pd);
                    foreach (var d in dirs)
                    {
                        try { Directory.Delete(d, true); if (verboseLogs) Debug.Log("[NewGameButton] Deleted directory: " + d); }
                        catch (Exception e) { Debug.LogWarning("[NewGameButton] Failed delete directory: " + d + " -> " + e.Message); }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NewGameButton] Exception while cleaning persistentDataPath: " + ex);
            }
        }

        // small wait to ensure file IO finishes before scene load
        yield return null;

        // 4) Load Open World scene (build index) với loading screen
        try
        {
            // 👇 THAY DÒNG SceneManager.Loadscene BẰNG DÒNG NÀY
            if (LoadingManager.Instance != null)
                LoadingManager.Instance.LoadScene(openWorldBuildIndex);
            else
            {
                Debug.LogWarning("[NewGameButton] LoadingManager not found, fallback to direct load");
                SceneManager.LoadScene(openWorldBuildIndex);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[NewGameButton] Failed to load scene index " + openWorldBuildIndex + " : " + ex);
        }
    }

    /// <summary>
    /// Try to reset in-memory MoneyManager to zero by:
    /// - setting common PlayerPrefs money keys to 0 (already done earlier)
    /// - searching assemblies for a MoneyManager-like type and trying to set its value via:
    ///     SetMoney/SetCash/SetBalance methods, or writable property/field, or AddMoney with negative.
    /// This uses reflection and best-effort; if your MoneyManager has a public SetMoney(int) method it's ideal.
    /// </summary>
    void ResetMoneyToZero(bool verbose = true)
    {
        try
        {
            // Attempt to find MoneyManager-like type
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Type moneyType = null;

            // Try to find type by common exact names first
            foreach (var asm in assemblies)
            {
                try
                {
                    moneyType = asm.GetType("MoneyManager", false, false)
                                ?? asm.GetType("GameMoneyManager", false, false)
                                ?? asm.GetType("CurrencyManager", false, false);
                    if (moneyType != null) break;
                }
                catch { /* ignore assembly issues */ }
            }

            // Fallback: find any type whose name contains "Money" or "Currency"
            if (moneyType == null)
            {
                foreach (var asm in assemblies)
                {
                    Type[] types = null;
                    try { types = asm.GetTypes(); }
                    catch { continue; }
                    foreach (var t in types)
                    {
                        if (t.Name.IndexOf("Money", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            t.Name.IndexOf("Currency", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            moneyType = t;
                            break;
                        }
                    }
                    if (moneyType != null) break;
                }
            }

            if (moneyType == null)
            {
                if (verbose) Debug.Log("[NewGameButton] No MoneyManager-like type found via reflection.");
                return;
            }

            // Try to get instance: static Instance property or field
            object instance = null;
            var instProp = moneyType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (instProp != null)
            {
                instance = instProp.GetValue(null, null);
            }
            else
            {
                var instField = moneyType.GetField("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (instField != null)
                {
                    instance = instField.GetValue(null);
                }
            }

            // If instance still null, try find a component of that type in scene
            if (instance == null)
            {
                // find all GameObjects and check components
                var allGOs = GameObject.FindObjectsOfType<GameObject>();
                foreach (var go in allGOs)
                {
                    if (go == null) continue;
                    var comp = go.GetComponent(moneyType);
                    if (comp != null)
                    {
                        instance = comp;
                        break;
                    }
                }
            }

            if (instance == null)
            {
                if (verbose) Debug.Log("[NewGameButton] MoneyManager type found but no instance detected in scene.");
                return;
            }

            // 1) Try public setter methods
            var setMethod = moneyType.GetMethod("SetMoney", BindingFlags.Public | BindingFlags.Instance)
                            ?? moneyType.GetMethod("SetCash", BindingFlags.Public | BindingFlags.Instance)
                            ?? moneyType.GetMethod("SetBalance", BindingFlags.Public | BindingFlags.Instance);
            if (setMethod != null)
            {
                try
                {
                    setMethod.Invoke(instance, new object[] { 0 });
                    if (verbose) Debug.Log("[NewGameButton] Called " + setMethod.Name + "(0) on MoneyManager instance.");
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[NewGameButton] Exception calling SetMoney: " + ex);
                }
            }

            // 2) Try writable property
            var prop = moneyType.GetProperty("money", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? moneyType.GetProperty("cash", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? moneyType.GetProperty("balance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? moneyType.GetProperty("Amount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(float) || prop.PropertyType == typeof(double))
                {
                    prop.SetValue(instance, Convert.ChangeType(0, prop.PropertyType), null);
                    if (verbose) Debug.Log("[NewGameButton] Set property " + prop.Name + " = 0 on MoneyManager instance.");
                    return;
                }
            }

            // 3) Try field
            var field = moneyType.GetField("money", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? moneyType.GetField("cash", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? moneyType.GetField("balance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? moneyType.GetField("currentMoney", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                if (field.FieldType == typeof(int) || field.FieldType == typeof(float) || field.FieldType == typeof(double))
                {
                    field.SetValue(instance, Convert.ChangeType(0, field.FieldType));
                    if (verbose) Debug.Log("[NewGameButton] Set field " + field.Name + " = 0 on MoneyManager instance.");
                    return;
                }
            }

            // 4) Last resort: call AddMoney(-bigNumber) if exists
            var addMethod = moneyType.GetMethod("AddMoney", BindingFlags.Public | BindingFlags.Instance)
                            ?? moneyType.GetMethod("ChangeMoney", BindingFlags.Public | BindingFlags.Instance)
                            ?? moneyType.GetMethod("ModifyMoney", BindingFlags.Public | BindingFlags.Instance);
            if (addMethod != null)
            {
                // attempt to read current money
                double currentVal = 0;
                try
                {
                    var getProp = moneyType.GetProperty("money", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                    ?? moneyType.GetProperty("cash", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                    ?? moneyType.GetProperty("balance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (getProp != null && getProp.CanRead)
                    {
                        var obj = getProp.GetValue(instance, null);
                        double.TryParse(obj?.ToString() ?? "0", out currentVal);
                    }
                }
                catch { currentVal = 0; }

                int subtract = (int)Math.Ceiling(Math.Abs(currentVal)) + 1000000;
                try
                {
                    addMethod.Invoke(instance, new object[] { -subtract });
                    if (verbose) Debug.Log("[NewGameButton] Called " + addMethod.Name + " with negative to try zeroing out money.");
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[NewGameButton] Exception calling AddMoney: " + ex);
                }
            }

            if (verbose) Debug.Log("[NewGameButton] MoneyManager found but no suitable setter method/property/field detected. Consider adding a public SetMoney(int) to MoneyManager.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[NewGameButton] Exception during ResetMoneyToZero reflection: " + ex);
        }
    }
}
