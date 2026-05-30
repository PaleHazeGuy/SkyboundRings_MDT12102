using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player.Controller
{
  public class Mouse : MonoBehaviour
  {
    [Header("Components")]
    [SerializeField]
    [Tooltip("Transform of the aircraft the rig follows and references")]
    private Transform aircraft = null;
    [SerializeField]
    [Tooltip("Transform of the object the mouse rotates to generate MouseAim position")]
    private Transform mouseAim = null;
    [SerializeField]
    [Tooltip("Transform of the object on the rig which the camera is attached to")]
    private Transform cameraRig = null;
    [SerializeField]
    [Tooltip("Transform of the camera itself")]
    private Transform cam = null;

    [Header("Options")]
    [SerializeField]
    [Tooltip("Follow aircraft using fixed update loop")]
    private bool useFixed = true;

    [SerializeField]
    [Tooltip("How quickly the camera tracks the mouse aim point.")]
    private float camSmoothSpeed = 5f;

    [SerializeField]
    [Tooltip("Mouse sensitivity for the mouse flight target")]
    private float mouseSensitivity = 3f;

    [SerializeField]
    [Tooltip("How far the boresight and mouse flight are from the aircraft")]
    private float aimDistance = 500f;

    private Vector3 frozenDirection = Vector3.forward;
    private bool isMouseAimFrozen = false;
    private bool didGoOffScreenDuringLook = false;
    private Camera playerCam;
    public Vector3 BoresightPos
    {
      get
      {
        return aircraft == null
             ? transform.forward * aimDistance
             : (aircraft.transform.forward * aimDistance) + aircraft.transform.position;
      }
    }
    public Vector3 MouseAimPos
    {
      get
      {
        if (mouseAim != null)
        {
          return isMouseAimFrozen
              ? GetFrozenMouseAimPos()
              : mouseAim.position + (mouseAim.forward * aimDistance);
        }
        else
        {
          return transform.forward * aimDistance;
        }
      }
    }

    public bool IsMouseAimFrozen => isMouseAimFrozen;
    public bool DidGoOffScreenDuringLook => didGoOffScreenDuringLook;

    private void Start()
    {
      Cursor.lockState = CursorLockMode.Locked;
      Cursor.visible = false;

      if (cam != null)
      {
        playerCam = cam.GetComponent<Camera>();
      }
      if (playerCam == null)
      {
        playerCam = Camera.main;
      }
    }

    private void Awake()
    {
      if (aircraft == null)
        Debug.LogError(name + "MouseController - No aircraft transform assigned!");
      if (mouseAim == null)
        Debug.LogError(name + "MouseController - No mouse aim transform assigned!");
      if (cameraRig == null)
        Debug.LogError(name + "MouseController - No camera rig transform assigned!");
      if (cam == null)
        Debug.LogError(name + "MouseController - No camera transform assigned!");

      transform.parent = null;
    }

    private void Update()
    {
      HandleCursorLock();

      if (useFixed == false)
        UpdateCameraPos();

      RotateRig();
    }

    private void HandleCursorLock()
    {
      if (Input.GetKeyDown(KeyCode.Escape))
      {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
          Cursor.lockState = CursorLockMode.None;
          Cursor.visible = true;
        }
        else
        {
          Cursor.lockState = CursorLockMode.Locked;
          Cursor.visible = false;
        }
      }

      if (Cursor.lockState == CursorLockMode.None && Input.GetMouseButtonDown(0))
      {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
      }
    }

    private void FixedUpdate()
    {
      if (useFixed == true)
        UpdateCameraPos();
    }

    private void RotateRig()
    {
      if (mouseAim == null || cam == null || cameraRig == null)
        return;

      if (Input.GetKeyDown(KeyCode.C))
      {
        isMouseAimFrozen = true;
        frozenDirection = mouseAim.forward;
        didGoOffScreenDuringLook = false;
      }
      else if (Input.GetKeyUp(KeyCode.C))
      {
        isMouseAimFrozen = false;
        if (didGoOffScreenDuringLook && aircraft != null)
        {
          mouseAim.forward = aircraft.forward;
        }
        else
        {
          mouseAim.forward = frozenDirection;
        }
        didGoOffScreenDuringLook = false;
      }

      float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
      float mouseY = -Input.GetAxis("Mouse Y") * mouseSensitivity;

      mouseAim.Rotate(cam.right, mouseY, Space.World);
      mouseAim.Rotate(cam.up, mouseX, Space.World);

      if (isMouseAimFrozen && playerCam != null && aircraft != null)
      {
        Vector3 viewportPoint = playerCam.WorldToViewportPoint(GetFrozenMouseAimPos());
        bool isOnScreen = viewportPoint.z > 0f && viewportPoint.x >= 0f && viewportPoint.x <= 1f && viewportPoint.y >= 0f && viewportPoint.y <= 1f;
        if (!isOnScreen)
        {
          didGoOffScreenDuringLook = true;
        }
      }

      Vector3 upVec = (Mathf.Abs(mouseAim.forward.y) > 0.9f) ? cameraRig.up : Vector3.up;

      cameraRig.rotation = Damp(cameraRig.rotation, Quaternion.LookRotation(mouseAim.forward, upVec), camSmoothSpeed, Time.deltaTime);
    }

    private Vector3 GetFrozenMouseAimPos()
    {
      if (mouseAim != null)
        return mouseAim.position + (frozenDirection * aimDistance);
      else
        return transform.forward * aimDistance;
    }

    private void UpdateCameraPos()
    {
      if (aircraft != null)
      {

        transform.position = aircraft.position;
      }
    }

    private Quaternion Damp(Quaternion a, Quaternion b, float lambda, float dt)
    {
      return Quaternion.Slerp(a, b, 1 - Mathf.Exp(-lambda * dt));
    }

    public void ResetAimToAircraft()
    {
      if (mouseAim != null && aircraft != null)
      {
        mouseAim.forward = aircraft.forward;
        if (cameraRig != null)
        {
          Vector3 upVec = (Mathf.Abs(mouseAim.forward.y) > 0.9f) ? aircraft.up : Vector3.up;
          cameraRig.rotation = Quaternion.LookRotation(mouseAim.forward, upVec);
        }
      }
    }
  }
}
