using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MouseLook : MonoBehaviour
{
    [SerializeField] private Transform playerBody;
    [SerializeField] private float sensitivity = 0.1f;

    private float verticalLook;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        Vector2 mouseMovement = ReadMouseMovement();

        verticalLook -= mouseMovement.y;
        verticalLook = Mathf.Clamp(verticalLook, -80f, 80f);

        transform.localRotation = Quaternion.Euler(verticalLook, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseMovement.x);
    }

    private Vector2 ReadMouseMovement()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current == null
            ? Vector2.zero
            : Mouse.current.delta.ReadValue() * sensitivity;
#else
        return new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")) * sensitivity * 10f;
#endif
    }
}