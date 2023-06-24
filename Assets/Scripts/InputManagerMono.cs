using InexperiencedDeveloper.Core;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManagerMono : Singleton<InputManagerMono>
{
    private InputActions m_input;
    protected override void Awake()
    {
        m_input = new InputActions();
    }

    private void OnEnable()
    {
        m_input.Enable();
    }

    private void OnDisable()
    {
        m_input.Disable();
    }

    public bool SelectPressed => m_input.Player.Select.WasPressedThisFrame();
    public Vector2 CursorPos => m_input.Player.CursorPos.ReadValue<Vector2>();
}
