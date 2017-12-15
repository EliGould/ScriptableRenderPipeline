﻿using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering.HDPipeline;

[CustomEditorForRenderPipeline(typeof(Cubemap), typeof(HDRenderPipelineAsset))]
class HDCubemapInspector : Editor
{
    private enum NavMode
    {
        None = 0,
        Zooming = 1,
        Rotating = 2
    }

    static GUIContent s_MipMapLow, s_MipMapHigh, s_CurveKeyframeSelected, s_CurveKeyframeSemiSelectedOverlay, s_RGBMIcon;
    static GUIStyle s_PreButton, s_PreSlider, s_PreSliderThumb, s_PreLabel;
    static Mesh s_SphereMesh;

    static Mesh sphereMesh
    {
        get { return s_SphereMesh ?? (s_SphereMesh = Resources.GetBuiltinResource(typeof(Mesh), "New-Sphere.fbx") as Mesh); }
    }

    Material m_ReflectiveMaterial;
    PreviewRenderUtility m_PreviewUtility;
    float m_CameraPhi = 0.75f;
    float m_CameraTheta = 0.5f;
    float m_CameraDistance = 2.0f;
    NavMode m_NavMode = NavMode.None;
    Vector2 m_PreviousMousePosition = Vector2.zero;

    public float previewExposure = 0f;
    public float mipLevelPreview = 0f;

    void Awake()
    {
        m_ReflectiveMaterial = new Material(Shader.Find("Debug/ReflectionProbePreview"))
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        InitIcons();
    }

    void OnEnable()
    {
        if (m_PreviewUtility == null)
            InitPreview();

        m_ReflectiveMaterial.SetTexture("_Cubemap", target as Texture);
    }

    void OnDisable()
    {
        if (m_PreviewUtility != null)
            m_PreviewUtility.Cleanup();
    }

    public override bool HasPreviewGUI()
    {
        return true;
    }

    public override void OnPreviewGUI(Rect r, GUIStyle background)
    {
        if(m_ReflectiveMaterial != null)
        {
            m_ReflectiveMaterial.SetFloat("_Exposure", previewExposure);
            m_ReflectiveMaterial.SetFloat("_MipLevel", mipLevelPreview);
        }

        if(m_PreviewUtility == null)
            InitPreview();

        UpdateCamera();

        m_PreviewUtility.BeginPreview(r, GUIStyle.none);
        m_PreviewUtility.DrawMesh(sphereMesh, Matrix4x4.identity, m_ReflectiveMaterial, 0);
        m_PreviewUtility.camera.Render();
        m_PreviewUtility.EndAndDrawPreview(r);

        if (Event.current.type != EventType.Repaint)
        {
            if (HandleMouse(r))
                Repaint();
        }
    }

    public override void OnPreviewSettings()
    {
        GUI.enabled = true;

        GUILayout.Box(s_MipMapHigh, s_PreLabel, GUILayout.MaxWidth(20));
        GUI.changed = false;
        mipLevelPreview = GUILayout.HorizontalSlider(mipLevelPreview, 0, ((Cubemap)target).mipmapCount, GUILayout.MaxWidth(80));
        GUILayout.Box(s_MipMapLow, s_PreLabel, GUILayout.MaxWidth(20));
        GUILayout.Space(5);
        GUILayout.Box(s_CurveKeyframeSemiSelectedOverlay, s_PreLabel, GUILayout.MaxWidth(20));
        GUI.changed = false;
        previewExposure = GUILayout.HorizontalSlider(previewExposure, -10f, 10f, GUILayout.MaxWidth(80));
        GUILayout.Box(s_CurveKeyframeSelected, s_PreLabel, GUILayout.MaxWidth(20));
    }

    void InitPreview()
    {
        m_PreviewUtility = new PreviewRenderUtility(false, true);
        m_PreviewUtility.cameraFieldOfView = 50.0f;
        m_PreviewUtility.camera.nearClipPlane = 0.01f;
        m_PreviewUtility.camera.farClipPlane = 20.0f;
        m_PreviewUtility.camera.transform.position = new Vector3(0, 0, 2);
        m_PreviewUtility.camera.transform.LookAt(Vector3.zero);
        //m_PreviewUtility.camera.clearFlags = CameraClearFlags.Skybox;
    }

    bool HandleMouse(Rect Viewport)
    {
        var result = false;

        if (Event.current.type == EventType.MouseDown)
        {
            if (Event.current.button == 0)
                m_NavMode = NavMode.Rotating;
            else if (Event.current.button == 1)
                m_NavMode = NavMode.Zooming;

            m_PreviousMousePosition = Event.current.mousePosition;
            result = true;
        }

        if (Event.current.type == EventType.MouseUp || Event.current.rawType == EventType.MouseUp)
            m_NavMode = NavMode.None;

        if (m_NavMode != NavMode.None)
        {
            var mouseDelta = Event.current.mousePosition - m_PreviousMousePosition;
            switch (m_NavMode)
            {
                case NavMode.Rotating:
                    m_CameraTheta = (m_CameraTheta - mouseDelta.x * 0.003f) % (Mathf.PI * 2);
                    m_CameraPhi = Mathf.Clamp(m_CameraPhi - mouseDelta.y * 0.003f, 0.2f, Mathf.PI - 0.2f);
                    break;
                case NavMode.Zooming:
                    m_CameraDistance = Mathf.Clamp(mouseDelta.y * 0.01f + m_CameraDistance, 1, 10);
                    break;
            }
            result = true;
        }

        m_PreviousMousePosition = Event.current.mousePosition;
        return result;
    }

    void UpdateCamera()
    {
        var pos = new Vector3(Mathf.Sin(m_CameraPhi) * Mathf.Cos(m_CameraTheta), Mathf.Cos(m_CameraPhi), Mathf.Sin(m_CameraPhi) * Mathf.Sin(m_CameraTheta)) * m_CameraDistance;
        m_PreviewUtility.camera.transform.position = pos;
        m_PreviewUtility.camera.transform.LookAt(Vector3.zero);
    }

    static void InitIcons()
    {
        s_MipMapLow = EditorGUIUtility.IconContent("PreTextureMipMapLow");
        s_MipMapHigh = EditorGUIUtility.IconContent("PreTextureMipMapHigh");
        s_CurveKeyframeSelected = EditorGUIUtility.IconContent("d_curvekeyframeselected");
        s_CurveKeyframeSemiSelectedOverlay = EditorGUIUtility.IconContent("d_curvekeyframesemiselectedoverlay");
        s_RGBMIcon = EditorGUIUtility.IconContent("PreMatLight1"); // TODO: proper icon for RGBM preview mode
        s_PreButton = "preButton";
        s_PreSlider = "preSlider";
        s_PreSliderThumb = "preSliderThumb";
        s_PreLabel = "preLabel";
    }
}
