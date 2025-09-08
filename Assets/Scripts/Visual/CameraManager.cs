using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public Vector3 cameraMove;
    public float moveInterval = 0.95f;
    public float zoomInterval = 0.95f;

    private float _zoomRange;
    private Vector3 _targetPosition;

    private void Update()
    {
        if (Input.mouseScrollDelta.y != 0)
        {
            _zoomRange = Input.mouseScrollDelta.y * -0.2f;
        }

        Camera.main.fieldOfView += _zoomRange;
        _zoomRange *= zoomInterval;

        float mouseX = Input.mousePosition.x / Screen.width;
        float mouseY = Input.mousePosition.y / Screen.height;

        float cameraX = Mathf.Lerp(cameraMove.x, -cameraMove.x, mouseX);
        float cameraY = Mathf.Lerp(cameraMove.y, -cameraMove.y, mouseY);

        _targetPosition = new Vector3(cameraX, cameraY, -10);
        transform.position = Vector3.Lerp(_targetPosition, transform.position, moveInterval);
    }
}
