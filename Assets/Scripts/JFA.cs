using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JFA : MonoBehaviour
{
    private Vector2Int[] Seeds;
    private Vector3[] Colors;
    public int SeedAmount = 5;
    public ComputeShader JFAShader;
    private ComputeBuffer seedBuffer;
    private ComputeBuffer colorBuffer;

    private int InitSeedKernel;
    private int JFAKernel;
    private int FillColorSeedKernel;
    // Start is called before the first frame update
    void Start()
    {
        Seeds = new Vector2Int[SeedAmount];
        Colors = new Vector3[SeedAmount];
        for (int i=0;i< SeedAmount;i++)
        {
            Seeds[i] = new Vector2Int(Random.Range(1,2000), Random.Range(1, 2000));
            Colors[i] = new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        }
        seedBuffer = new ComputeBuffer(SeedAmount, sizeof(int)*2* SeedAmount);
        seedBuffer.SetData(Seeds);
        colorBuffer = new ComputeBuffer(SeedAmount, sizeof(float) * 3 * SeedAmount);
        colorBuffer.SetData(Colors);
        InitSeedKernel = JFAShader.FindKernel("InitSeed");
        JFAKernel = JFAShader.FindKernel("JFA");
        FillColorSeedKernel = JFAShader.FindKernel("FillColor");

    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        RenderTextureDescriptor desc = new RenderTextureDescriptor(source.width, source.height);
        desc.enableRandomWrite = true;
        desc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;

        RenderTexture tmp1 = RenderTexture.GetTemporary(desc);
        RenderTexture tmp2 = RenderTexture.GetTemporary(desc);
        // Init Seed
        JFAShader.SetBuffer(InitSeedKernel, "Seeds", seedBuffer);
        JFAShader.SetTexture(InitSeedKernel, "Source", tmp1);
        JFAShader.SetInt("Width", source.width);
        JFAShader.SetInt("Height", source.height);
        JFAShader.Dispatch(InitSeedKernel, SeedAmount,1,1);
        // JFA
        int stepAmount = (int)Mathf.Log(2, Mathf.Max(source.width, source.height));
        for(int i = 0;i < stepAmount; i++)
        {
            int step = (int)Mathf.Pow(2, stepAmount - i - 1);
            JFAShader.SetTexture(JFAKernel, "Source", tmp1);
            JFAShader.SetTexture(JFAKernel, "Result", tmp2);
            JFAShader.SetInt("Step", step);
            JFAShader.Dispatch(JFAKernel, source.width, source.height, 1);
            Graphics.Blit(tmp2, tmp1);
        }

        // Fill Color
        JFAShader.SetBuffer(FillColorSeedKernel, "Colors", colorBuffer);
        JFAShader.SetTexture(FillColorSeedKernel, "Source", tmp2);
        JFAShader.SetTexture(FillColorSeedKernel, "Result", tmp1);
        JFAShader.Dispatch(FillColorSeedKernel, source.width, source.height, 1);

        Graphics.Blit(tmp1, destination);

        RenderTexture.ReleaseTemporary(tmp1);
        RenderTexture.ReleaseTemporary(tmp2);

    }
}
