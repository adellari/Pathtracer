using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaytraceMaster : MonoBehaviour
{
    public RenderTexture target;
    public ComputeShader tracer;
    public Texture Skybox;
    struct DispatchParams
    {
        public int x;
        public int y;
        public int z;
    }
    
    
    private DispatchParams groups;
    private Camera main;
    
    
    
    void Start()
    {
        main = Camera.main;
        PrimeTarget();
    }

    void PrimeTarget()
    {
        target = new RenderTexture(main.pixelWidth, main.pixelHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        target.enableRandomWrite = true;
        target.Create();

        groups = new DispatchParams();
        groups.x = Mathf.CeilToInt(main.pixelWidth / 8);
        groups.y = Mathf.CeilToInt(main.pixelHeight / 8);
        groups.z = 0;
    }

    void CallTrace()
    {
        tracer.SetTexture(0, "Result", target);
        tracer.SetMatrix("CameraToWorld", main.cameraToWorldMatrix);
        tracer.SetMatrix("WorldToCamera", main.worldToCameraMatrix);
        tracer.SetMatrix("CameraInverseProjection", main.projectionMatrix.inverse);
        tracer.SetTexture(0, "Skybox", Skybox);
        tracer.Dispatch(0, groups.x, groups.y, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        CallTrace();
        Graphics.Blit(target, destination);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnApplicationQuit()
    {
        if (target != null)
            target.Release();
        
    }
}
