using UnityEngine;
using Player.Controller;

namespace Player.Interface
{
  public class Hud : MonoBehaviour
  {
    [Header("Components")]
    [SerializeField] private Mouse mouseFlight = null;

    [Header("HUD Elements")]
    [SerializeField] private RectTransform boresight = null;
    [SerializeField] private RectTransform mousePos = null;

    private Camera playerCam = null;

    private void Awake()
    {
      if (mouseFlight == null)
        Debug.LogError(name + ": Hud - Mouse Flight Controller not assigned!");

      playerCam = mouseFlight.GetComponentInChildren<Camera>();

      if (playerCam == null)
        Debug.LogError(name + ": Hud - No camera found on assigned Mouse Flight Controller!");
    }

    private void Update()
    {
      if (mouseFlight == null || playerCam == null)
        return;

      UpdateGraphics(mouseFlight);
    }

    private void UpdateGraphics(Mouse controller)
    {
      if (boresight != null)
      {
        boresight.position = playerCam.WorldToScreenPoint(controller.BoresightPos);
        boresight.gameObject.SetActive(boresight.position.z > 1f);
      }

      if (mousePos != null)
      {
        bool shouldVisualSnap = controller.IsMouseAimFrozen && controller.DidGoOffScreenDuringLook;
        Vector3 targetAimPos = shouldVisualSnap ? controller.BoresightPos : controller.MouseAimPos;

        mousePos.position = playerCam.WorldToScreenPoint(targetAimPos);
        mousePos.gameObject.SetActive(mousePos.position.z > 1f);
      }
    }

    public void SetReferenceMouseFlight(Mouse controller)
    {
      mouseFlight = controller;
    }
  }
}
