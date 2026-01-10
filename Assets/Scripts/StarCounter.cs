using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StarCounter : MonoBehaviour
{
    public static int starCount = 0; // static variable to maintain global uniqueness
    public TextMeshProUGUI starCountText; // UI TextMeshPro to display star count

    // 更新UI显示星星数量
    void Start()
    {
        // 订阅星星收集事件
        Star.OnStarCollected += OnStarCollected;

        if (starCountText != null)
        {
            UpdateStarCountUI(); // Use UpdateStarCountUI instead of UpdateStarCountText
        }
    }

    private void OnDestroy()
    {
        // 取消订阅事件，避免内存泄漏
        Star.OnStarCollected -= OnStarCollected;
    }

    // 星星收集事件处理器
    private void OnStarCollected(int value)
    {
        starCount += value;
        // 调用UI更新
        UpdateStarCountUI();
        Debug.Log($"[StarCounter] 星星收集！当前总数: {starCount}");
    }

    // 增加星星数量（保持向后兼容）
    public static void AddStar()
    {
        starCount++;
        // 调用UI更新
        UpdateStarCountUI(); // Use UpdateStarCountUI instead of UpdateStarCountText
    }

    // 更新Text显示
    private static void UpdateStarCountUI()
    {
        // 找到一个StarCountText对象并更新显示
        StarCounter[] allStarCounters = GameObject.FindObjectsOfType<StarCounter>();
        foreach (var starCounter in allStarCounters)
        {
            if (starCounter.starCountText != null)
            {
                // 星星数量为0时显示空字符串，>=1时显示"Stars: X"
                starCounter.starCountText.text =
                    starCount == 0 ? "" : "Stars: " + starCount.ToString();
            }
        }
    }
}
