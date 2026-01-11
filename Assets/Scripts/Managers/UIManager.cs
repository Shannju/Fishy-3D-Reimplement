using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // 用于控制显示的 UI 面板
    public GameObject menuPanel;

    // 创建一个 InputAction 变量
    private InputAction escapeAction;

    void OnEnable()
    {
        // 绑定 InputAction，确保这个操作会在启用时被捕捉
        escapeAction = new InputAction("ToggleMenu", binding: "<Keyboard>/escape");
        escapeAction.performed += ctx => ToggleMenu();
        escapeAction.Enable();
    }

    void OnDisable()
    {
        // 在禁用时清除绑定
        escapeAction.Disable();
    }

    // 处理 ESC 键的按下事件，切换菜单显示
    private void ToggleMenu()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(!menuPanel.activeSelf);
        }
    }
}
