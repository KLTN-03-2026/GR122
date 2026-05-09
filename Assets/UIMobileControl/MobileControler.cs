using UnityEngine;

public class MobileInput : MonoBehaviour
{
    // MOVE
    public static float Horizontal;
    public static float Vertical;

    // ENTER
    public static bool EnterPressed;

    // LEFT
    public void Left()
    {
        Horizontal = -1f;
    }

    // RIGHT
    public void Right()
    {
        Horizontal = 1f;
    }

    // UP
    public void Up()
    {
        Vertical = 1f;
    }

    // DOWN
    public void Down()
    {
        Vertical = -1f;
    }

    // STOP LEFT RIGHT
    public void StopHorizontal()
    {
        Horizontal = 0f;
    }

    // STOP UP DOWN
    public void StopVertical()
    {
        Vertical = 0f;
    }

    // ENTER BUTTON
    public void PressEnter()
    {
        EnterPressed = true;
    }
    public static bool GetEnterDown()
{
    if (EnterPressed)
    {
        EnterPressed = false;
        return true;
    }

    return false;
}
}