using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    private float zoomSpeed = 5f;
    private Vector3 dragOrigin;

    private static bool canMove = true;
    public static bool CanMove { get => canMove; set => canMove = value; }


    void Update()
    {
        ZoomCamera();
        MoveCamera();
    }

    void ZoomCamera()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        Camera.main.orthographicSize -= scroll * zoomSpeed;
    }

    void MoveCamera()
    {
        if (!canMove) return;
        if (EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(0))
        {
            Vector3 difference = dragOrigin - Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Camera.main.transform.position += difference;
            ClampCameraPosition();
        }
    }

    void ClampCameraPosition()
    {
        Vector3 pos = Camera.main.transform.position;
        Camera.main.transform.position = pos;
    }
}
