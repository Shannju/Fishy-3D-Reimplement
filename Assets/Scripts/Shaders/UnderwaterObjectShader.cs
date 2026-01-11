using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class UnderwaterObjectShader : MonoBehaviour
{
    private Renderer _renderer;
    private MaterialPropertyBlock _propertyBlock;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        UpdateShaderParameters();
    }

    private void OnEnable()
    {
        UpdateShaderParameters();
    }

    // 更新Shader参数
    public void UpdateShaderParameters()
    {
        if (WaterManager.Instance == null || _renderer == null)
            return;

        // 获取当前的材质属性
        _renderer.GetPropertyBlock(_propertyBlock);

        // 设置水相关的参数
        _propertyBlock.SetFloat("_WaterHeight", WaterManager.Instance.WaterHeight);
        _propertyBlock.SetColor("_WaterColor", WaterManager.Instance.WaterColor);
        _propertyBlock.SetFloat("_WaterInfluence", WaterManager.Instance.WaterInfluence);
        _propertyBlock.SetFloat("_WaterDepthFade", WaterManager.Instance.WaterDepthFade);

        // 应用属性块
        _renderer.SetPropertyBlock(_propertyBlock);
    }

    // 在编辑器中实时更新（可选）
    private void OnValidate()
    {
        if (Application.isPlaying && WaterManager.Instance != null)
        {
            UpdateShaderParameters();
        }
    }
}