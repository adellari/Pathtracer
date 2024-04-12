using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using Random = UnityEngine.Random;

public class RaytraceMaster : MonoBehaviour
{
    public RenderTexture target;
    public RenderTexture converged;
    public ComputeShader tracer;
    public Texture Skybox;
    public Transform mainLight;
    
    [Range(1, 20)]
    public int SphereCount = 4;

    public bool randomized = true;
    struct DispatchParams
    {
        public int x;
        public int y;
        public int z;
    }

    [System.Serializable]
    public struct Sphere
    {
        public Vector4 point;
        public Vector3 specular;
        public Vector3 albedo;
        public Vector3 emission;
        public Vector3 refractionColor;
        public float smoothness;
        public float RefractiveIndex;
        public float refractionRoughness;
        public float refractionChance;
    }

    [SerializeField] public Sphere[] _Spheres;
    
    private DispatchParams groups;
    private Camera main;
    private Light dirLight;
    private ComputeBuffer Spheres;
    private uint _currentSample = 0;
    private Material _addMaterial;
    void Start()
    {
        FreeAllocations();
        
        main = Camera.main;
        dirLight = mainLight.GetComponent<Light>();
        PrimeTarget();
        
        if (randomized)
            CreateSpheres();
        else
            CommandUpdateSpheres();
        
    }

    void PrimeTarget()
    {
        target = new RenderTexture(main.pixelWidth, main.pixelHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        target.enableRandomWrite = true;
        target.Create();
        
        converged = new RenderTexture(main.pixelWidth, main.pixelHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        converged.enableRandomWrite = true;
        converged.Create();

        groups = new DispatchParams();
        groups.x = Mathf.CeilToInt(main.pixelWidth / 8);
        groups.y = Mathf.CeilToInt(main.pixelHeight / 8);
        groups.z = 0;
        _currentSample = 0;

    }

    [ContextMenu("Update Spheres")]
    public void CommandUpdateSpheres()
    {
        if(Spheres != null)
            Spheres.Dispose();
        Spheres = new ComputeBuffer(_Spheres.Length, Marshal.SizeOf(typeof(Sphere)));
        Spheres.SetData(_Spheres);
    }

    void CreateSpheres()
    {
        Sphere[] spheres = new Sphere[SphereCount];
        for (int a = 0; a < SphereCount; a++)
        {
            Sphere s = new Sphere();
            float radius = Random.Range(0.1f, 1f);

            var xz = Random.insideUnitCircle * 5f; //space them out because they have radius
            //var spec = Random.insideUnitSphere;
            var alb = Random.ColorHSV();
            bool metallic = Random.value < 0.5f;
            var chance = Random.value;

            if (chance > 0.8f)
            {
                var col = Random.ColorHSV(0, 1, 0, 1, 3.0f, 8.0f);
                s.emission = new Vector3(col.r, col.g, col.b);
            }
            else
            {
                s.emission = new Vector3();
            }
            
            s.point = new Vector4(xz.x, radius, xz.y, radius);
            s.specular = metallic ? new Vector3(alb.r, alb.g, alb.b) : Vector3.one * 0.04f;
            s.albedo = metallic? Vector3.zero : new Vector3(alb.r, alb.g, alb.b);;
            s.smoothness = Random.value;
            spheres[a] = s;
            s.RefractiveIndex = 0;
        }

        Spheres = new ComputeBuffer(SphereCount, Marshal.SizeOf(typeof(Sphere)));
        Spheres.SetData(spheres);
    }

    void CallTrace()
    {
        tracer.SetTexture(0, "Result", target);
        tracer.SetMatrix("CameraToWorld", main.cameraToWorldMatrix);
        tracer.SetMatrix("WorldToCamera", main.worldToCameraMatrix);
        tracer.SetMatrix("CameraInverseProjection", main.projectionMatrix.inverse);
        tracer.SetBuffer(0, "Spheres", Spheres);
        tracer.SetTexture(0, "Skybox", Skybox);
        tracer.SetInt("SphereCount", SphereCount);
        tracer.SetVector("CameraPosition", transform.position);
        tracer.SetVector("light", new Vector4(mainLight.forward.x, mainLight.forward.y, mainLight.forward.z, dirLight.intensity));
        tracer.SetVector("_Pixel", new Vector4(0, 0, Random.value, Random.value));
        tracer.SetFloat("_Seed", Random.value);
        tracer.Dispatch(0, groups.x, groups.y, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        CallTrace();
        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/TSAA"));
        _addMaterial.SetFloat("_Sample", _currentSample);
        Graphics.Blit(target, converged, _addMaterial);
        Graphics.Blit(converged, destination);
        _currentSample++;
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.hasChanged || dirLight.transform.hasChanged)
        {
            _currentSample = 0;
            transform.hasChanged = false;
            dirLight.transform.hasChanged = false;
        }
    }

    void FreeAllocations()
    {
        if (target != null)
            target.Release();

        if (Spheres != null)
            Spheres.Dispose();

        if (converged != null)
            converged.Release();
    }

    private void OnApplicationQuit()
    {
        FreeAllocations();
    }
}
