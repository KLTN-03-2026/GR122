using UnityEngine;

public class RainFollowCar : MonoBehaviour
{
    public Transform car; // kéo xe vào đây
    public Vector3 offset = new Vector3(0, 10f, 0);

    void LateUpdate()
    {
        if (car == null) return;

        transform.position = car.position + offset;
    }
}