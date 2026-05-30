using UnityEngine;
using Player.Controller;
using TMPro;

namespace Player.Interface
{
  public class Hud : MonoBehaviour
  {
    [Header("Components")]
    [SerializeField] private Mouse mouseFlight = null;

    [Header("HUD Elements")]
    [SerializeField] private RectTransform boresight = null;
    [SerializeField] private RectTransform mousePos = null;
    [SerializeField] private TextMeshProUGUI statusText = null;
    [SerializeField] private TextMeshProUGUI ringText = null;
    [SerializeField] private TextMeshProUGUI timerText = null;
    [Tooltip("Arrow UI image that points toward the next ring when off-screen")]
    [SerializeField] private RectTransform waypointArrow = null;
    [Tooltip("Distance from screen edge the arrow sits at (pixels)")]
    [SerializeField] private float arrowEdgeMargin = 60f;

    private Camera playerCam = null;
    private Aircraft aircraft = null;
    private Transform aircraftRoot = null;

    private Transform nextRingTarget = null;

    private float elapsedTime = 0f;
    private bool timerActive = false;

    private void Awake()
    {
      if (mouseFlight == null)
        Debug.LogError(name + ": Hud - Mouse Flight Controller not assigned!");
      else
        playerCam = mouseFlight.GetComponentInChildren<Camera>(true);

      aircraft = FindObjectOfType<Aircraft>();
      if (aircraft != null)
        aircraftRoot = aircraft.transform;
    }

    private void Update()
    {
      if (mouseFlight == null)
        return;

      if (playerCam == null)
      {
        playerCam = mouseFlight.GetComponentInChildren<Camera>(true);
        if (playerCam == null)
          return;
      }

      if (aircraft == null)
      {
        aircraft = FindObjectOfType<Aircraft>();
        if (aircraft != null)
          aircraftRoot = aircraft.transform;
      }

      elapsedTime += timerActive ? Time.deltaTime : 0f;

      UpdateGraphics(mouseFlight);
      UpdateStatusText();
    }

    public void UpdateRingText(int collected, int total)
    {
      if (ringText != null)
      {
        ringText.text = $"RING {collected}/{total}";
      }
    }

    public void SetNextRingTarget(Transform ring)
    {
      nextRingTarget = ring;
    }

    public void StartTimer()
    {
      timerActive = true;
    }

    public void ResetTimer()
    {
      elapsedTime = 0f;
      timerActive = false;
    }

    public string GetFormattedTime()
    {
      int tMins = Mathf.FloorToInt(elapsedTime / 60f);
      int tSecs = Mathf.FloorToInt(elapsedTime % 60f);
      return $"{tMins:00}:{tSecs:00}";
    }

    private void UpdateStatusText()
    {
      if (timerText != null)
      {
        timerText.text = GetFormattedTime();
      }

      if (statusText != null && aircraft != null)
      {
        int throttlePercent = Mathf.RoundToInt(aircraft.throttle * 100f);
        int speedKmh = Mathf.RoundToInt(aircraft.CurrentAirspeed * 3.6f);
        int altitude = Mathf.RoundToInt(aircraft.transform.position.y);

        const float maxRadarRange = 5000f;
        float radarAltFloat = Mathf.Max(0f, aircraft.transform.position.y);

        RaycastHit[] hits = Physics.RaycastAll(aircraft.transform.position, Vector3.down, maxRadarRange);
        float closestGroundDist = float.MaxValue;
        foreach (RaycastHit h in hits)
        {
          if (aircraftRoot != null && h.transform.root == aircraftRoot.root)
            continue;

          if (h.distance < closestGroundDist)
            closestGroundDist = h.distance;
        }

        if (closestGroundDist < float.MaxValue)
          radarAltFloat = closestGroundDist;

        int radarAlt = Mathf.RoundToInt(Mathf.Max(0f, radarAltFloat));

        float remainingFuelSeconds = aircraft.currentOilPercent * aircraft.oilBurnRateTime;
        int minutes = Mathf.FloorToInt(remainingFuelSeconds / 60f);
        int seconds = Mathf.FloorToInt(remainingFuelSeconds % 60f);
        string fuelStr = string.Format("{0:00}:{1:00}", minutes, seconds);

        statusText.text = $"THROTTLE\t\t{throttlePercent} %\n" +
                          $"SPEED\t\t\t{speedKmh} km/h\n" +
                          $"ALTITUDE\t\t{altitude} m\n" +
                          $"RADAR ALTITUDE\t{radarAlt} m\n" +
                          $"FUEL\t\t\t{fuelStr}";
      }
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

      UpdateWaypointArrow();
    }

    private void UpdateWaypointArrow()
    {
      if (waypointArrow == null || nextRingTarget == null || playerCam == null)
      {
        if (waypointArrow != null) waypointArrow.gameObject.SetActive(false);
        return;
      }

      Vector3 screenPos = playerCam.WorldToScreenPoint(nextRingTarget.position);

      bool isBehind = screenPos.z < 0f;
      if (isBehind) screenPos *= -1f;

      float w = Screen.width;
      float h = Screen.height;
      Vector3 screenCenter = new Vector3(w * 0.5f, h * 0.5f, 0f);

      bool onScreen = !isBehind &&
                       screenPos.x > arrowEdgeMargin && screenPos.x < w - arrowEdgeMargin &&
                       screenPos.y > arrowEdgeMargin && screenPos.y < h - arrowEdgeMargin;

      if (onScreen)
      {
        waypointArrow.gameObject.SetActive(false);
        return;
      }

      waypointArrow.gameObject.SetActive(true);

      Vector3 dir = (screenPos - screenCenter).normalized;

      float angle = Mathf.Atan2(dir.y, dir.x);
      float cosA = Mathf.Cos(angle);
      float sinA = Mathf.Sin(angle);
      float halfW = w * 0.5f - arrowEdgeMargin;
      float halfH = h * 0.5f - arrowEdgeMargin;

      float scaleX = (Mathf.Abs(cosA) > 0.0001f) ? halfW / Mathf.Abs(cosA) : float.MaxValue;
      float scaleY = (Mathf.Abs(sinA) > 0.0001f) ? halfH / Mathf.Abs(sinA) : float.MaxValue;
      float scale = Mathf.Min(scaleX, scaleY);

      Vector3 clampedPos = screenCenter + new Vector3(cosA * scale, sinA * scale, 0f);
      waypointArrow.position = clampedPos;

      float rotZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
      waypointArrow.rotation = Quaternion.Euler(0f, 0f, rotZ);
    }

    public void SetReferenceMouseFlight(Mouse controller)
    {
      mouseFlight = controller;
    }
  }
}
