using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JFA : MonoBehaviour
{
    public enum JFAType { VoronoiDiagram,DistanceTransform};
    private Vector2Int[] Seeds;

    private Vector2[] SeedPos;
    private Vector2[] SeedSpeeds;

    private Vector3[] Colors;
    public int SeedAmount = 5;
    public float Speed = 1f;
    public ComputeShader JFAShader;
    private ComputeBuffer seedBuffer;
    private ComputeBuffer colorBuffer;

    private int InitSeedKernel;
    private int JFAKernel;
    private int FillVoronoiDiagramKernel;
    private int FillDistanceTransformKernel;
    private RenderTexture tmp1, tmp2;
    public JFAType DisplayType = JFAType.VoronoiDiagram;
    // Start is called before the first frame update
    void Start()
    {
        Seeds = new Vector2Int[SeedAmount];
        SeedSpeeds = new Vector2[SeedAmount];
        SeedPos = new Vector2[SeedAmount];

        Colors = new Vector3[SeedAmount];
        for (int i=0;i< SeedAmount;i++)
        {
            Seeds[i] = new Vector2Int(Random.Range(1,2000), Random.Range(1, 2000));

            SeedSpeeds[i] = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));
            SeedPos[i] = Seeds[i];

            Colors[i] = new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        }
        seedBuffer = new ComputeBuffer(SeedAmount, sizeof(int)*2);
        seedBuffer.SetData(Seeds);
        colorBuffer = new ComputeBuffer(SeedAmount, sizeof(float)*3);
        colorBuffer.SetData(Colors);
        InitSeedKernel = JFAShader.FindKernel("InitSeed");
        JFAKernel = JFAShader.FindKernel("JFA");
        FillVoronoiDiagramKernel = JFAShader.FindKernel("FillVoronoiDiagram");
        FillDistanceTransformKernel = JFAShader.FindKernel("FillDistanceTransform");

    }

    private void Update()
    {
        for(int i = 0; i < SeedAmount; i++)
        {
            float tempX = SeedPos[i].x +  SeedSpeeds[i].x*Time.deltaTime*Speed;
            float tempY = SeedPos[i].y + SeedSpeeds[i].y * Time.deltaTime * Speed;
            float SpeedX = SeedSpeeds[i].x;
            float SpeedY = SeedSpeeds[i].y;
            if (tempX < 0 || tempX >= Screen.width)
            {
                SpeedX = -SpeedX;
                tempX = SeedPos[i].x + SpeedX * Time.deltaTime * Speed;
            }
            if (tempY < 0 || tempY >= Screen.height)
            {
                SpeedY = -SpeedY;
                tempY = SeedPos[i].y + SpeedY * Time.deltaTime * Speed;
            }
            SeedPos[i] = new Vector2(tempX,tempY);
            SeedSpeeds[i] = new Vector2(SpeedX, SpeedY);
            Seeds[i] = new Vector2Int((int)SeedPos[i].x, (int)SeedPos[i].y);
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        InitRenderTexture(source);
        // Init Seed
        seedBuffer.SetData(Seeds);
        JFAShader.SetBuffer(InitSeedKernel, "Seeds", seedBuffer);
        JFAShader.SetTexture(InitSeedKernel, "Source", tmp1);
        JFAShader.SetInt("Width", source.width);
        JFAShader.SetInt("Height", source.height);
        JFAShader.Dispatch(InitSeedKernel, SeedAmount, 1, 1);
        // JFA
        int stepAmount = (int)Mathf.Log(Mathf.Max(source.width, source.height), 2);
        //Debug.Log("stepAmount:"+ stepAmount);
        int threadGroupsX = Mathf.CeilToInt(source.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(source.height / 8.0f);
        for (int i = 0; i < stepAmount; i++)
        {
            int step = (int)Mathf.Pow(2, stepAmount - i - 1);
            //Debug.Log("step:" + step);
            JFAShader.SetInt("Step", step);
            JFAShader.SetTexture(JFAKernel, "Source", tmp1);
            JFAShader.SetTexture(JFAKernel, "Result", tmp2);

            JFAShader.Dispatch(JFAKernel, threadGroupsX, threadGroupsY, 1);
            Graphics.Blit(tmp2, tmp1);
        }

        // Fill with Color
        switch (DisplayType)
        {
            case JFAType.VoronoiDiagram:
                JFAShader.SetBuffer(FillVoronoiDiagramKernel, "Colors", colorBuffer);
                JFAShader.SetTexture(FillVoronoiDiagramKernel, "Source", tmp1);
                JFAShader.SetTexture(FillVoronoiDiagramKernel, "Result", tmp2);
                JFAShader.Dispatch(FillVoronoiDiagramKernel, threadGroupsX, threadGroupsY, 1);
                break;
            case JFAType.DistanceTransform:
                JFAShader.SetTexture(FillDistanceTransformKernel, "Source", tmp1);
                JFAShader.SetTexture(FillDistanceTransformKernel, "Result", tmp2);
                JFAShader.Dispatch(FillDistanceTransformKernel, threadGroupsX, threadGroupsY, 1);
                break;
        }

        Graphics.Blit(tmp2, destination);
    }

    private void InitRenderTexture(RenderTexture source)
    {
        if (tmp1 == null || tmp1.width != source.width || tmp1.height != source.height)
        {
            // Release render texture if we already have one
            if (tmp1 != null)
                tmp1.Release();
            if (tmp2 != null)
                tmp2.Release();
            // Get a render target for Ray Tracing
            tmp1 = new RenderTexture(source.width, source.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            tmp1.enableRandomWrite = true;
            tmp1.Create();

            tmp2 = new RenderTexture(source.width, source.height, 0,
               RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            tmp2.enableRandomWrite = true;
            tmp2.Create();
        }


        RenderTexture rt = UnityEngine.RenderTexture.active;
        UnityEngine.RenderTexture.active = tmp1;
        GL.Clear(true, true, Color.clear);
        UnityEngine.RenderTexture.active = rt;
    }
}
