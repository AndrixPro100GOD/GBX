using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Assets.Game.Scripts
{
    public class PlayerCursorVisualizer : MonoBehaviour
    {
        [SerializeField]
        private Camera playerCamera;

        private Transform _cursorTransform;

        private void Awake()
        {
            _cursorTransform = transform;
        }

        private void Update()
        {
            _cursorTransform.position = playerCamera.ScreenToWorldPoint(Input.mousePosition);
        }
    }
}