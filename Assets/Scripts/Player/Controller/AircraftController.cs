using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player.Controller
{
  [RequireComponent(typeof(Rigidbody))]
  public class Aircraft : MonoBehaviour
  {
    [Header("Components")]
    [SerializeField] private Mouse controller = null;

    [Header("Engine Power")]
    public float maxSpeed = 80f;
    public float engineAcceleration = 15f;

    [Header("Throttle System")]
    [Range(0f, 1f)] public float throttle = 0.1f;
    public float throttleSensitivity = 0.5f;

    [Header("Aerodynamics & Gliding")]
    [Tooltip("How fast the plane slows down when throttle is at 0.")] public float glideBrakeRate = 2f;
    [Tooltip("How much speed you lose automatically during sharp, heavy turns.")] public float turnBrakeRate = 5f;
    [Tooltip("How tightly the plane's momentum snaps to where the nose is pointing.")] public float gripAirBite = 4f;

    [Header("Maneuverability")]
    public Vector3 turnTorque = new Vector3(90f, 25f, 45f);
    public float forceMult = 1000f;

    [Header("Engine Logistics")]
    public bool hasOil = true;
    public float currentOilPercent = 1.0f;
    public float oilBurnRateTime = 300f;

    [Header("Flight Telemetry (Read Only)")]
    [SerializeField] private float currentAirspeed = 0f;
    [SerializeField] private bool isStalling = false;
    [SerializeField] private float stallPitchDirection = 1f;

    public float CurrentAirspeed => currentAirspeed;

    [Header("Autopilot")]
    public float sensitivity = 5f;
    public float aggressiveTurnAngle = 10f;

    [Header("Input Monitor")]
    [SerializeField][Range(-1f, 1f)] private float pitch = 0f;
    [SerializeField][Range(-1f, 1f)] private float yaw = 0f;
    [SerializeField][Range(-1f, 1f)] private float roll = 0f;

    public float Pitch { set { pitch = Mathf.Clamp(value, -1f, 1f); } get { return pitch; } }
    public float Yaw { set { yaw = Mathf.Clamp(value, -1f, 1f); } get { return yaw; } }
    public float Roll { set { roll = Mathf.Clamp(value, -1f, 1f); } get { return roll; } }

    private Rigidbody rigid;
    private bool rollOverride = false;
    private bool pitchOverride = false;
    private bool yawOverride = false;
    private float targetForwardSpeed = 0f;

    [HideInInspector] public bool IsControllable = false;

    private void Awake()
    {
      rigid = GetComponent<Rigidbody>();
      rigid.useGravity = false;
      rigid.drag = 0f;
      rigid.angularDrag = 2f;

      if (controller == null)
        Debug.LogError(name + ": Plane - Missing reference to MouseFlightController!");
    }

    private void Update()
    {
      if (!IsControllable) return;

      HandleThrottleAndOil();
      HandleManualOverrides();

      float autoYaw = 0f;
      float autoPitch = 0f;
      float autoRoll = 0f;

      bool disableAutopilot = (controller != null) && controller.IsMouseAimFrozen && controller.DidGoOffScreenDuringLook;

      if (controller != null && !disableAutopilot)
        RunAutopilot(controller.MouseAimPos, out autoYaw, out autoPitch, out autoRoll);

      if (isStalling)
      {
        pitch = stallPitchDirection;
        roll = 0f;
        yaw = 0f;
      }
      else
      {
        pitch = (pitchOverride) ? Input.GetAxis("Vertical") : autoPitch;
        roll = (rollOverride) ? Input.GetAxis("Horizontal") : autoRoll;

        if (yawOverride)
        {
          float manualYaw = 0f;
          if (Input.GetKey(KeyCode.Q)) manualYaw = -1f;
          if (Input.GetKey(KeyCode.E)) manualYaw = 1f;
          yaw = manualYaw;
        }
        else
        {
          yaw = autoYaw;
        }
      }
    }

    private void HandleThrottleAndOil()
    {
      if (Input.GetKey(KeyCode.LeftShift)) throttle += throttleSensitivity * Time.deltaTime;
      if (Input.GetKey(KeyCode.LeftControl)) throttle -= throttleSensitivity * Time.deltaTime;
      throttle = Mathf.Clamp01(throttle);

      if (hasOil && throttle > 0.05f)
      {
        currentOilPercent -= (1f / oilBurnRateTime) * throttle * Time.deltaTime;
        currentOilPercent = Mathf.Max(0f, currentOilPercent);
        if (currentOilPercent <= 0f) hasOil = false;
      }
    }

    private void HandleManualOverrides()
    {
      rollOverride = false;
      pitchOverride = false;
      yawOverride = false;

      if (Mathf.Abs(Input.GetAxis("Horizontal")) > .25f) rollOverride = true;
      if (Mathf.Abs(Input.GetAxis("Vertical")) > .25f)
      {
        pitchOverride = true;
        rollOverride = true;
      }

      if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E)) yawOverride = true;
    }

    private void RunAutopilot(Vector3 flyTarget, out float yaw, out float pitch, out float roll)
    {
      var localFlyTarget = transform.InverseTransformPoint(flyTarget).normalized * sensitivity;
      var angleOffTarget = Vector3.Angle(transform.forward, flyTarget - transform.position);

      yaw = Mathf.Clamp(localFlyTarget.x, -1f, 1f);
      pitch = -Mathf.Clamp(localFlyTarget.y, -1f, 1f);

      var agressiveRoll = Mathf.Clamp(localFlyTarget.x, -1f, 1f);
      var wingsLevelRoll = transform.right.y;

      var wingsLevelInfluence = Mathf.InverseLerp(0f, aggressiveTurnAngle, angleOffTarget);
      roll = Mathf.Lerp(wingsLevelRoll, agressiveRoll, wingsLevelInfluence);
    }

    private void FixedUpdate()
    {
      if (!IsControllable)
      {
        currentAirspeed = 0f;
        rigid.velocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;
        return;
      }

      currentAirspeed = rigid.velocity.magnitude;

      if (currentAirspeed < 0.001f)
      {
        rigid.velocity = Vector3.zero;
        currentAirspeed = 0f;
      }

      Vector3 localVelocity = transform.InverseTransformDirection(rigid.velocity);
      float forwardAirflow = localVelocity.z;

      float engineDesiredSpeed = (hasOil) ? (throttle * maxSpeed) : 0f;

      if (throttle <= 0.01f || !hasOil)
      {
        targetForwardSpeed = Mathf.MoveTowards(targetForwardSpeed, 12f, glideBrakeRate * Time.fixedDeltaTime);
      }
      else
      {
        targetForwardSpeed = Mathf.MoveTowards(targetForwardSpeed, engineDesiredSpeed, engineAcceleration * Time.fixedDeltaTime);
      }

      float pitchInclination = transform.forward.y;
      if (pitchInclination > 0.05f)
      {
        targetForwardSpeed -= pitchInclination * 32f * Time.fixedDeltaTime;
      }
      else if (pitchInclination < -0.05f)
      {
        targetForwardSpeed -= pitchInclination * 38f * Time.fixedDeltaTime;
        targetForwardSpeed = Mathf.Min(targetForwardSpeed, maxSpeed * 1.5f);
      }

      float turningDeflection = Mathf.Max(Mathf.Abs(pitch), Mathf.Abs(yaw));
      if (turningDeflection > 0.2f && currentAirspeed > 15f)
      {
        targetForwardSpeed -= turningDeflection * turnBrakeRate * Time.fixedDeltaTime;
      }

      targetForwardSpeed = Mathf.Max(0f, targetForwardSpeed);

      if (pitchInclination > 0.35f && (forwardAirflow < 10f || currentAirspeed < 1f))
      {
        if (!isStalling)
        {
          isStalling = true;
          float leaningDirection = transform.up.y;

          if (leaningDirection >= Mathf.Cos(70f * Mathf.Deg2Rad))
          {
            stallPitchDirection = 1.0f;
          }
          else
          {
            stallPitchDirection = -1.0f;
          }
        }
      }

      if (isStalling)
      {
        if (pitchInclination < -0.1f) isStalling = false;
      }
      if (isStalling)
      {

        Vector3 fallVelocity = Vector3.down * 20f;
        rigid.velocity = Vector3.Lerp(rigid.velocity, fallVelocity, 2.5f * Time.fixedDeltaTime);

        rigid.AddRelativeTorque(Vector3.right * turnTorque.x * stallPitchDirection * forceMult * 0.8f, ForceMode.Force);
      }
      else
      {
        Vector3 desiredVelocityVector = transform.forward * targetForwardSpeed;

        bool isGliding = (!hasOil || throttle <= 0.01f);
        if (isGliding && currentAirspeed <= 25f)
        {
          float glideSinkRate = Mathf.InverseLerp(25f, 5f, currentAirspeed) * 20f;
          desiredVelocityVector += Vector3.down * glideSinkRate;

          float divePitchTorque = Mathf.InverseLerp(25f, 5f, currentAirspeed) * 0.4f;
          rigid.AddRelativeTorque(Vector3.right * turnTorque.x * divePitchTorque * forceMult, ForceMode.Force);
        }

        rigid.velocity = Vector3.Lerp(rigid.velocity, desiredVelocityVector, gripAirBite * Time.fixedDeltaTime);
      }

      float aerodynamicEffectiveness = Mathf.Clamp01(forwardAirflow / 15f);
      if (isStalling) aerodynamicEffectiveness = 1.0f;

      rigid.AddRelativeTorque(new Vector3(turnTorque.x * pitch, turnTorque.y * yaw, -turnTorque.z * roll) * forceMult * aerodynamicEffectiveness, ForceMode.Force);
    }

    private void OnCollisionEnter(Collision collision)
    {
      if (!IsControllable) return;

      if (collision.gameObject.GetComponentInParent<Gameplay.Ring>() != null) return;

      var manager = FindObjectOfType<Gameplay.Manager>();
      if (manager != null)
      {
        manager.OnPlayerDeath();
      }
    }
  }
}
