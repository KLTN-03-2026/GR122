using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarInputHandler : MonoBehaviour
{
    // Components
    TopDownCarController topDownCarController;

    // Awake is called when the script instance is being loaded
    void Awake()
    {
        topDownCarController = GetComponent<TopDownCarController>();
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 inputVector = Vector2.zero;

        // Keyboard input
        float keyboardH = Input.GetAxis("Horizontal");
        float keyboardV = Input.GetAxis("Vertical");

        // Mobile input
        float mobileH = MobileInput.Horizontal;
        float mobileV = MobileInput.Vertical;

        // Combine both inputs
        inputVector.x = Mathf.Clamp(keyboardH + mobileH, -1f, 1f);
        inputVector.y = Mathf.Clamp(keyboardV + mobileV, -1f, 1f);

        // Send input to car
        topDownCarController.SetInputVector(inputVector);
    }
}