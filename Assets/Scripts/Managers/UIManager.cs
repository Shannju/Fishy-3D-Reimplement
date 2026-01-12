using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // 用于控制显示的 UI 面板
    public GameObject menuPanel;

    void Update()
    {
        // 检测空格键、回车键和ESC键按下
        if (Keyboard.current != null &&
            (Keyboard.current.spaceKey.wasPressedThisFrame ||
             Keyboard.current.enterKey.wasPressedThisFrame ||
             Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            ToggleMenu();
        }
    }

    void OnEnable()
    {
        // 不再需要InputAction，改为在Update中检测任意键
    }


    // 处理空格键、回车键和ESC键的按下事件，切换菜单显示
    private void ToggleMenu()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(!menuPanel.activeSelf);
        }
    }
}
