using UnityEngine;

public class WaterManager : MonoBehaviour
{
    [Header("Water Settings")]
    [Tooltip("水面高度")]
    [SerializeField] private float waterHeight = 0f;

    [Tooltip("水颜色")]
    [SerializeField] private Color waterColor = new Color(0.2f, 0.4f, 0.6f, 0.8f);

    [Tooltip("水影响强度")]
    [Range(0f, 1f)]
    [SerializeField] private float waterInfluence = 0.5f;

    [Tooltip("水深度衰减")]
    [SerializeField] private float waterDepthFade = 2f;

    private static WaterManager _instance;
    public static WaterManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<WaterManager>();
            }
            return _instance;
        }
    }

    public float WaterHeight => waterHeight;
    public Color WaterColor => waterColor;
    public float WaterInfluence => waterInfluence;
    public float WaterDepthFade => waterDepthFade;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    // 在Scene视图中显示水位线
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Gizmos.DrawCube(new Vector3(0f, waterHeight, 0f), new Vector3(1000f, 0.1f, 1000f));
    }

    // 工具方法：设置所有水下物体的Shader参数
    [ContextMenu("Update All Underwater Objects")]
    public void UpdateAllUnderwaterObjects()
    {
        UnderwaterObjectShader[] underwaterObjects = FindObjectsOfType<UnderwaterObjectShader>();
        foreach (var obj in underwaterObjects)
        {
            obj.UpdateShaderParameters();
        }
    }
}