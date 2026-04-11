using UnityEngine;
using System.Reflection;

public static class PlayerTeleportHelper
{
    /// <summary>
    /// Teleport player GameObject to position+rotation (degrees Z), stop velocity and sync TopDownCarController internal state.
    /// Robust: sets Transform, Rigidbody2D.rotation, Rigidbody2D.position, clears velocities, and tries to set private 'rotationAngle'.
    /// </summary>
    public static void TeleportPlayer(GameObject player, Vector3 worldPos, float zRotation)
    {
        if (player == null) return;

        // 1) Set Transform (visual immediate)
        player.transform.position = worldPos;
        player.transform.rotation = Quaternion.Euler(0f, 0f, zRotation);

        // 2) Rigidbody2D: stop and snap position/rotation (handles physics side)
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // stop motion
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            // Snap position & rotation for Rigidbody2D
            // Use direct assignment to ensure immediate change (works in Edit & Play)
            rb.position = worldPos;
            rb.rotation = zRotation;

            // Also call MoveRotation once to be safe (works on kinematic as well)
            rb.MoveRotation(zRotation);
        }

        // 3) If TopDownCarController exists, reset its input and sync its internal rotationAngle
        var carCtrl = player.GetComponent<TopDownCarController>();
        if (carCtrl != null)
        {
            // reset inputs (public method available in your controller)
            carCtrl.SetInputVector(Vector2.zero);

            // Try to set private field(s) that likely hold rotationAngle.
            // We'll try some reasonable candidates to be robust.
            System.Type t = typeof(TopDownCarController);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            // Candidate field names observed commonly: "rotationAngle"
            string[] candidates = new string[] { "rotationAngle", "m_rotationAngle", "_rotationAngle", "rotation" };

            bool setOk = false;
            foreach (var name in candidates)
            {
                FieldInfo fi = t.GetField(name, flags);
                if (fi != null && fi.FieldType == typeof(float))
                {
                    fi.SetValue(carCtrl, zRotation);
                    setOk = true;
                    break;
                }
            }

            // If none of the named fields found, as a last resort try to find any private float that looks like rotationAngle by heuristics:
            if (!setOk)
            {
                // look for any single float field (risky), prefer fields with 'rot' in name
                FieldInfo[] all = t.GetFields(flags);
                FieldInfo best = null;
                foreach (var f in all)
                {
                    if (f.FieldType == typeof(float))
                    {
                        string n = f.Name.ToLower();
                        if (n.Contains("rot") || n.Contains("angle"))
                        {
                            best = f; break;
                        }
                        if (best == null) best = f;
                    }
                }
                if (best != null)
                {
                    best.SetValue(carCtrl, zRotation);
                    setOk = true;
                }
            }

            // As an extra precaution, also set the transform there again (some controllers cache transform)
            carCtrl.transform.rotation = Quaternion.Euler(0f, 0f, zRotation);
        }

        // 4) Reset velocities for children rigidbodies (if player has rigidbodies in children)
        var childRbs = player.GetComponentsInChildren<Rigidbody2D>();
        foreach (var c in childRbs)
        {
            c.linearVelocity = Vector2.zero;
            c.angularVelocity = 0f;
        }
    }
}
