using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// CarStatsUI:
/// - Gán 4 Text UI (Speed, Drift, Acceleration, Turn Factor)
/// - Tự tìm player theo tag và lấy component TopDownCarController bằng reflection
/// - Lấy 4 giá trị (max speed, drift, acceleration, turn factor) bằng reflection (field hoặc property)
/// - Update text mỗi frame
/// </summary>
public class CarStatsUI : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Tag của player object (mặc định 'Player Racer')")]
    public string playerTag = "Player Racer";

    [Header("UI targets (assign either UnityEngine.UI.Text or TextMeshProUGUI)")]
    public Text speedText_UI;
    public Text driftText_UI;
    public Text accelerationText_UI;
    public Text turnFactorText_UI;

    public TextMeshProUGUI speedText_TMP;
    public TextMeshProUGUI driftText_TMP;
    public TextMeshProUGUI accelerationText_TMP;
    public TextMeshProUGUI turnFactorText_TMP;

    [Header("Formatting")]
    [Tooltip("Float format string used to display numbers (e.g. 'F2' or 'F0').")]
    public string numberFormat = "F2";

    // runtime
    GameObject currentPlayer;
    Component controllerComp; // TopDownCarController component (via reflection)
    Type controllerType;

    // accessors for each stat (returns float)
    Func<float> getMaxSpeed;
    Func<float> getDrift;
    Func<float> getAcceleration;
    Func<float> getTurnFactor;

    void Start()
    {
        // If UI fields not assigned, try to auto-find by name in scene (useful when you created GameObjects named exactly)
        AutoAssignIfMissing();
        // initial find
        TryFindPlayerAndController();
    }

    void Update()
    {
        // If player changed/destroyed, re-find
        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != currentPlayer)
        {
            currentPlayer = p;
            controllerComp = null;
            controllerType = null;
            getMaxSpeed = getDrift = getAcceleration = getTurnFactor = null;
            TryFindPlayerAndController();
        }

        // If we have accessors, update UI
        if (controllerComp != null && getMaxSpeed != null)
        {
            UpdateUIText(speedText_UI, speedText_TMP, "Speed : " + FormatValue(getMaxSpeed()));
        }
        else
        {
            UpdateUIText(speedText_UI, speedText_TMP, "Speed : -");
        }

        if (controllerComp != null && getDrift != null)
        {
            UpdateUIText(driftText_UI, driftText_TMP, "Drift : " + FormatValue(getDrift()));
        }
        else
        {
            UpdateUIText(driftText_UI, driftText_TMP, "Drift : -");
        }

        if (controllerComp != null && getAcceleration != null)
        {
            UpdateUIText(accelerationText_UI, accelerationText_TMP, "Acceleration : " + FormatValue(getAcceleration()));
        }
        else
        {
            UpdateUIText(accelerationText_UI, accelerationText_TMP, "Acceleration : -");
        }

        if (controllerComp != null && getTurnFactor != null)
        {
            UpdateUIText(turnFactorText_UI, turnFactorText_TMP, "Turn Factor : " + FormatValue(getTurnFactor()));
        }
        else
        {
            UpdateUIText(turnFactorText_UI, turnFactorText_TMP, "Turn Factor : -");
        }
    }

    void AutoAssignIfMissing()
    {
        // If user created GameObjects named exactly "Speed Text" / "Drift Text" / etc, try to auto-assign
        if (speedText_UI == null && speedText_TMP == null)
        {
            var go = GameObject.Find("Speed Text");
            if (go != null)
            {
                speedText_UI = go.GetComponent<Text>();
                speedText_TMP = speedText_TMP ?? go.GetComponent<TextMeshProUGUI>();
            }
        }
        if (driftText_UI == null && driftText_TMP == null)
        {
            var go = GameObject.Find("Drift Text");
            if (go != null)
            {
                driftText_UI = go.GetComponent<Text>();
                driftText_TMP = driftText_TMP ?? go.GetComponent<TextMeshProUGUI>();
            }
        }
        if (accelerationText_UI == null && accelerationText_TMP == null)
        {
            var go = GameObject.Find("Acceleration Text");
            if (go != null)
            {
                accelerationText_UI = go.GetComponent<Text>();
                accelerationText_TMP = accelerationText_TMP ?? go.GetComponent<TextMeshProUGUI>();
            }
        }
        if (turnFactorText_UI == null && turnFactorText_TMP == null)
        {
            var go = GameObject.Find("Turn Factor Text");
            if (go != null)
            {
                turnFactorText_UI = go.GetComponent<Text>();
                turnFactorText_TMP = turnFactorText_TMP ?? go.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    void TryFindPlayerAndController()
    {
        if (string.IsNullOrEmpty(playerTag)) return;
        currentPlayer = GameObject.FindGameObjectWithTag(playerTag);
        if (currentPlayer == null) return;

        // try to find component by common type name
        // prefer exact name TopDownCarController if exists
        controllerComp = currentPlayer.GetComponent("TopDownCarController") as Component;
        if (controllerComp == null)
        {
            // try to find any component whose type name contains "TopDownCar" or "TopDown"
            var comps = currentPlayer.GetComponents<Component>();
            foreach (var c in comps)
            {
                if (c == null) continue;
                var tn = c.GetType().Name;
                if (tn.IndexOf("TopDownCar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tn.IndexOf("TopDown", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tn.IndexOf("CarController", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    controllerComp = c;
                    break;
                }
            }
        }

        if (controllerComp == null) return;

        controllerType = controllerComp.GetType();

        // Build accessors for each stat using reflection; try a list of candidate names (fields/properties)
        getMaxSpeed = BuildAccessorFloat(controllerComp, controllerType, new string[] {
            "maxSpeed", "MaxSpeed", "max_speed", "topSpeed", "TopSpeed", "maxVelocity", "MaxVelocity", "speedMax", "SpeedMax"
        });

        getDrift = BuildAccessorFloat(controllerComp, controllerType, new string[] {
            "driftFactor", "DriftFactor", "drift_factor", "drift", "Drift"
        });

        getAcceleration = BuildAccessorFloat(controllerComp, controllerType, new string[] {
            "accelerationFactor", "AccelerationFactor", "accelFactor", "acceleration", "Acceleration", "accel"
        });

        getTurnFactor = BuildAccessorFloat(controllerComp, controllerType, new string[] {
            "turnFactor", "TurnFactor", "turn_factor", "turn", "Turn"
        });
    }

    Func<float> BuildAccessorFloat(Component comp, Type t, string[] candidateNames)
    {
        if (comp == null || t == null) return null;

        foreach (var name in candidateNames)
        {
            // try field
            var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null && (f.FieldType == typeof(float) || f.FieldType == typeof(double) || f.FieldType == typeof(int)))
            {
                return () =>
                {
                    object v = f.GetValue(comp);
                    return ConvertToFloat(v);
                };
            }

            // try property
            var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && p.CanRead && (p.PropertyType == typeof(float) || p.PropertyType == typeof(double) || p.PropertyType == typeof(int)))
            {
                return () =>
                {
                    object v = p.GetValue(comp, null);
                    return ConvertToFloat(v);
                };
            }
        }

        // fallback: try find any numeric field/property that has candidate keywords inside its name
        var members = t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var m in members)
        {
            string mn = m.Name;
            foreach (var key in candidateNames)
            {
                if (mn.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (m.MemberType == MemberTypes.Field)
                    {
                        var f = t.GetField(mn, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (f != null && (f.FieldType == typeof(float) || f.FieldType == typeof(double) || f.FieldType == typeof(int)))
                            return () => ConvertToFloat(f.GetValue(comp));
                    }
                    else if (m.MemberType == MemberTypes.Property)
                    {
                        var p = t.GetProperty(mn, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (p != null && p.CanRead && (p.PropertyType == typeof(float) || p.PropertyType == typeof(double) || p.PropertyType == typeof(int)))
                            return () => ConvertToFloat(p.GetValue(comp, null));
                    }
                }
            }
        }

        return null;
    }

    float ConvertToFloat(object v)
    {
        if (v == null) return 0f;
        if (v is float f) return f;
        if (v is double d) return (float)d;
        if (v is int i) return (float)i;
        if (float.TryParse(v.ToString(), out float parsed)) return parsed;
        return 0f;
    }

    string FormatValue(float v)
    {
        try
        {
            return v.ToString(numberFormat);
        }
        catch
        {
            return v.ToString();
        }
    }

    void UpdateUIText(Text uiText, TextMeshProUGUI tmpText, string value)
    {
        if (uiText != null) uiText.text = value;
        if (tmpText != null) tmpText.text = value;
    }
}
