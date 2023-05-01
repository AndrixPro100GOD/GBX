using System.Net;
using UnityEngine;
using Cinemachine;
using System.Collections;
using UnityEngine.UIElements;
using System.Collections.Generic;
using static UnityEditor.PlayerSettings;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class CameraTest : MonoBehaviour
{
    [SerializeField]
    private float speedOfScrolling = 1;

    [SerializeField]
    private float maxZoomOut = 3;

    [SerializeField]
    private float minZoomIn = 1;

    private CinemachineVirtualCamera _virtualCamera;
    private float _scrollingValue = 0;

    #region Init

    private void Reset()
    {
        InitMonoBehavior();
    }

    private void InitMonoBehavior()
    {
        _virtualCamera = _virtualCamera == null ? GetComponent<CinemachineVirtualCamera>() : _virtualCamera;
    }

    private void Awake()
    {
        InitMonoBehavior();
    }

    #endregion Init

    // Update is called once per frame
    private void Update()
    {
        _scrollingValue = Input.mouseScrollDelta.y;

        if (_scrollingValue != 0)
            Zooming(_scrollingValue);
    }

    private void Zooming(float value)
    {
        var lens = _virtualCamera.m_Lens;
        float size = lens.OrthographicSize += -value * speedOfScrolling;

        if (size > maxZoomOut)
            lens.OrthographicSize = maxZoomOut;
        else if (size < minZoomIn)
            lens.OrthographicSize = minZoomIn;

        _virtualCamera.m_Lens = lens;
    }
}