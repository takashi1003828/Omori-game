using UnityEngine;

public class PlayerInputReader : MonoBehaviour
{
    private const string _horizontal = "Horizontal";
    private const string _vertical = "Vertical";
    private const string _mouseXAxis = "Mouse X";
    private const string _mouseYAxis = "Mouse Y";
    
    public Vector2 GetMoveInput()
    {
        float x = Input.GetAxisRaw(_horizontal);
        float z = Input.GetAxisRaw(_vertical);
        Vector2 moveInput = new Vector2(x, z);
        return moveInput;
    }
    public bool CheckJumpPressed()
    {
        return Input.GetKeyDown(KeyCode.Space);
    }
    public bool CheckWeightButtonPressed()
    {
        return Input.GetMouseButtonDown(0);
    }
    public bool IsReelButtonHeld()
    {
        return Input.GetMouseButton(1);
    }
    public Vector2 GetCameraMoveInput()
    {   
        float x = Input.GetAxis(_mouseXAxis);
        float y = Input.GetAxis(_mouseYAxis);   
        return new Vector2(x, y);
    }
    public bool CheckHeavyButtonPressed()
    {
        return Input.GetKeyDown(KeyCode.E);
    }
    public bool ChackLightButtonPressed()
    {
        return Input.GetKeyDown(KeyCode.Q);
    }
}
