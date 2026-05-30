using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AircraftController = Player.Controller.Aircraft;

namespace Visuals
{
  public class AircraftVisuals : MonoBehaviour
  {
    [Header("Target Mesh References")]
    [SerializeField] private Transform propellerTransform;

    [Header("Rotation Settings")]
    [SerializeField] private float maxSpinSpeed = 1500f;
    [Range(0f, 1f)] public float engineThrustPercent = 0.5f;
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("Max speed propeller spins from airflow alone when engine is idle.")]
    private float maxWindmillPercent = 0.15f;

    private AircraftController planeController;
    private Rigidbody rb;

    private void Start()
    {
      planeController = GetComponent<AircraftController>();
      rb = GetComponent<Rigidbody>();
      if (rb == null && planeController != null)
      {
        rb = planeController.GetComponent<Rigidbody>();
      }
    }

    private void Update()
    {
      float airspeed = 0f;
      float maxSpeed = 80f;

      if (planeController != null)
      {
        engineThrustPercent = planeController.hasOil ? planeController.throttle : 0f;
        maxSpeed = planeController.maxSpeed;
      }

      if (rb != null)
      {
        airspeed = rb.velocity.magnitude;
      }

      if (propellerTransform != null)
      {
        float windmillingPercent = Mathf.Clamp01(airspeed / Mathf.Max(1f, maxSpeed)) * maxWindmillPercent;
        float spinPercent = Mathf.Max(engineThrustPercent, windmillingPercent);

        float currentSpeed = maxSpinSpeed * spinPercent;
        propellerTransform.Rotate(Vector3.right * currentSpeed * Time.deltaTime);
      }
    }
  }
}
