using UnityEngine;

/// <summary>
/// 星星物体：玩家碰到时广播星星收集事件并销毁自己
/// </summary>
public class Star : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("星星的值（通常为1）")]
    [SerializeField] private int starValue = 1;

    [Header("Audio")]
    [SerializeField] private bool enableAudio = true;
    [SerializeField] private AudioClip collectSfx;
    private AudioSource audioSource;

    // 星星收集事件委托
    public delegate void StarCollectedEventHandler(int value);
    // 静态事件，任何类都可以订阅
    public static event StarCollectedEventHandler OnStarCollected;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 检查是否是玩家碰撞
        FishPlayer player = other.GetComponentInParent<FishPlayer>();
        if (player != null)
        {
            CollectStar();
        }
    }

    private void CollectStar()
    {
        // 广播星星收集事件
        OnStarCollected?.Invoke(starValue);

        // 播放收集音效
        if (enableAudio && audioSource != null && collectSfx != null)
        {
            audioSource.PlayOneShot(collectSfx);
        }

        // 销毁星星物体
        Destroy(gameObject);

        Debug.Log($"[Star] 星星被收集！值: {starValue}");
    }
}