using UnityEngine;

public class PlayerCollisionHandler : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        WantedSystem.Instance?.OnPlayerCollision();
    }
}