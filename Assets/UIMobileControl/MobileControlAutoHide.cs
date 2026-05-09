using UnityEngine;

public class MobileControlAutoHide : MonoBehaviour
{
    public GameObject garageUI;
    public GameObject mobileControl;

    void Update()
    {
        if (garageUI.activeSelf)
        {
            mobileControl.SetActive(false);
        }
        else
        {
            mobileControl.SetActive(true);
        }
    }
}