using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Player.Interface;

namespace Gameplay
{
  public class Manager : MonoBehaviour
  {
    [Header("Main Menu & Game State Objects")]
    [SerializeField] private GameObject coreInterface;
    [SerializeField] private TextMeshProUGUI logoText;
    [SerializeField] private TextMeshProUGUI playButtonText;
    [SerializeField] private TextMeshProUGUI totalTimeText;

    [Header("Gameplay Systems to Toggle")]
    [SerializeField] private GameObject planeObject;
    [SerializeField] private GameObject mouseFlightRig;
    [SerializeField] private GameObject gameplayHudObject;

    [Header("Spawner Settings")]

    [SerializeField] private Ring ringPrefab;
    [SerializeField] private int maxRings = 20;

    [Header("Spawn Area")]
    [SerializeField] private Vector3 spawnAreaCenter = new Vector3(0, 100, 0);
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(1000, 200, 1000);
    [SerializeField] private float clearRadius = 40f;
    [SerializeField] private float fixedRingSpacing = 120f;

    [Header("HUD")]
    [SerializeField] private Hud hud;

    [Header("Gameplay Rewards")]
    [SerializeField] private float fuelRewardPerRing = 0.2f;

    [Header("Player Spawn")]
    [SerializeField] private float playerSpawnDistance = 60f;
    [SerializeField] private Transform playerTransform;

    private List<Ring> activeCourse = new List<Ring>();
    private int currentRingIndex = 0;
    private int ringsCollected = 0;
    private bool isGameActive = false;

    private Player.Controller.Aircraft playerAircraft;

    private void Start()
    {
      if (hud == null) hud = FindObjectOfType<Hud>();

      if (playerTransform == null && planeObject != null)
      {
        playerTransform = planeObject.transform;
      }
      else if (playerTransform == null)
      {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
      }

      SetupMenuState(isVictory: false, isDead: false);
    }

    public void OnPlayButtonPressed()
    {
      isGameActive = true;
      if (hud == null) hud = FindObjectOfType<Hud>();

      StartCoroutine(WaitForTerrainAndGenerate());
    }

    private void SetupMenuState(bool isVictory, bool isDead = false)
    {
      isGameActive = false;

      Cursor.visible = true;
      Cursor.lockState = CursorLockMode.None;
      if (coreInterface != null) coreInterface.SetActive(true);

      if (totalTimeText != null)
      {
        totalTimeText.gameObject.SetActive(isVictory);
        if (isVictory && hud != null)
        {
          totalTimeText.text = $"Total Time  {hud.GetFormattedTime()}";
        }
      }

      if (planeObject != null) planeObject.SetActive(false);
      if (mouseFlightRig != null) mouseFlightRig.SetActive(false);
      if (gameplayHudObject != null) gameplayHudObject.SetActive(false);

      if (isVictory)
      {
        if (logoText != null) logoText.text = "Victory";
        if (playButtonText != null) playButtonText.text = "Restart";
      }
      else if (isDead)
      {
        if (logoText != null) logoText.text = "You Died";
        if (playButtonText != null) playButtonText.text = "Restart";
      }
      else
      {
        if (logoText != null) logoText.text = "SkyboundRings";
        if (playButtonText != null) playButtonText.text = "Play";
      }
    }

    private IEnumerator WaitForTerrainAndGenerate()
    {
      bool terrainReady = false;
      float timeWaited = 0f;

      while (!terrainReady && timeWaited < 8f)
      {
        RaycastHit[] hits = UnityEngine.Physics.RaycastAll(new Vector3(spawnAreaCenter.x, 2000f, spawnAreaCenter.z), Vector3.down, 4000f, UnityEngine.Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        foreach (var h in hits)
        {
          if (h.collider.GetComponentInParent<Ring>() == null && !h.collider.CompareTag("Player"))
          {
            terrainReady = true;
            break;
          }
        }

        if (!terrainReady)
        {
          yield return new WaitForSeconds(0.2f);
          timeWaited += 0.2f;
        }
      }

      yield return new WaitForSeconds(0.5f);

      GenerateCourse();
      if (planeObject != null) planeObject.SetActive(true);
      SpawnPlayerAtFirstRing();
      if (mouseFlightRig != null) mouseFlightRig.SetActive(true);
      if (coreInterface != null) coreInterface.SetActive(false);
      if (gameplayHudObject != null) gameplayHudObject.SetActive(true);
      playerAircraft = FindObjectOfType<Player.Controller.Aircraft>();
      if (playerAircraft != null)
      {
        playerAircraft.IsControllable = true;
        playerAircraft.currentOilPercent = 0.05f;
        playerAircraft.hasOil = true;
        playerAircraft.throttle = 100f;
      }

      if (hud != null)
      {
        hud.ResetTimer();
        hud.StartTimer();
      }

      Cursor.visible = false;
      Cursor.lockState = CursorLockMode.Locked;
    }

    private void GenerateCourse()
    {
      if (ringPrefab == null)
      {
        Debug.LogError("GameplayManager: No Ring Prefab assigned!");
        return;
      }

      foreach (var ring in activeCourse)
      {
        if (ring != null) Destroy(ring.gameObject);
      }
      activeCourse.Clear();

      currentRingIndex = 0;
      ringsCollected = 0;

      Vector3 lastPos = spawnAreaCenter;
      float startAngle = Random.Range(-30f, 30f);
      Vector3 currentDir = Quaternion.Euler(0, startAngle, 0) * Vector3.forward;

      List<Vector3> coursePositions = new List<Vector3>();

      for (int i = 0; i < maxRings; i++)
      {
        Vector3 validPos = lastPos;
        bool foundSpace = false;

        for (int attempts = 0; attempts < 40; attempts++)
        {
          float angleDev = Random.Range(-25f, 25f);
          Vector3 attemptDir = Quaternion.Euler(0, angleDev, 0) * currentDir;

          Vector3 testPos = lastPos + attemptDir * fixedRingSpacing;

          bool tooClose = false;
          foreach (var existingPos in coursePositions)
          {
            if (Vector3.Distance(testPos, existingPos) < fixedRingSpacing * 0.75f)
            {
              tooClose = true;
              break;
            }
          }
          if (tooClose) continue;

          bool inBounds = testPos.x >= spawnAreaCenter.x - spawnAreaSize.x / 2 && testPos.x <= spawnAreaCenter.x + spawnAreaSize.x / 2 &&
                          testPos.z >= spawnAreaCenter.z - spawnAreaSize.z / 2 && testPos.z <= spawnAreaCenter.z + spawnAreaSize.z / 2;

          if (!inBounds)
          {
            Vector3 toCenter = (spawnAreaCenter - lastPos);
            toCenter.y = 0;
            if (toCenter != Vector3.zero) currentDir = Vector3.Lerp(currentDir, toCenter.normalized, 0.2f).normalized;
            continue;
          }

          float groundY = spawnAreaCenter.y - spawnAreaSize.y / 2f;
          bool groundFound = false;

          RaycastHit[] hits = UnityEngine.Physics.RaycastAll(new Vector3(testPos.x, lastPos.y + 800f, testPos.z), Vector3.down, 1600f, UnityEngine.Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
          float highestValidGround = -9999f;
          foreach (var h in hits)
          {
            if (h.collider.GetComponentInParent<Ring>() != null) continue;
            if (h.collider.CompareTag("Player")) continue;
            if (h.point.y > highestValidGround)
            {
              highestValidGround = h.point.y;
              groundFound = true;
            }
          }
          if (groundFound) groundY = highestValidGround;

          float courseProgress = (float)i / Mathf.Max(1, maxRings - 1);
          float wave = Mathf.Pow(Mathf.Sin(courseProgress * Mathf.PI), 4f);
          float altitudeBoost = wave * 60f;

          float minClearance = clearRadius + 5f;
          float targetHeight = groundY + minClearance + Random.Range(5f, 20f) + altitudeBoost;

          if (i > 0)
          {
            float maxHeightChange = fixedRingSpacing * 0.3f;
            testPos.y = Mathf.Clamp(targetHeight, lastPos.y - maxHeightChange, lastPos.y + maxHeightChange);
            if (testPos.y < groundY + minClearance) testPos.y = groundY + minClearance;
          }
          else
          {
            testPos.y = Mathf.Max(targetHeight, groundY + minClearance);
          }

          bool pathBlocked = false;
          float safeRadius = clearRadius * 0.4f;

          if (i > 0)
          {
            Vector3 pathDir = testPos - lastPos;
            RaycastHit[] pathHits = UnityEngine.Physics.SphereCastAll(lastPos, safeRadius, pathDir.normalized, pathDir.magnitude, UnityEngine.Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (var h in pathHits)
            {
              if (h.collider.GetComponentInParent<Ring>() != null) continue;
              if (h.collider.CompareTag("Player")) continue;
              pathBlocked = true;
              break;
            }
          }

          if (pathBlocked && attempts < 35) continue;

          bool spaceClear = true;
          Collider[] colliders = UnityEngine.Physics.OverlapSphere(testPos, clearRadius, UnityEngine.Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
          foreach (var c in colliders)
          {
            if (c.GetComponentInParent<Ring>() != null) continue;
            if (c.CompareTag("Player")) continue;
            spaceClear = false;
            break;
          }

          if (spaceClear && i == 0)
          {
            Vector3 simSpawnPos = testPos - attemptDir.normalized * playerSpawnDistance;
            if (UnityEngine.Physics.CheckSphere(simSpawnPos, safeRadius, UnityEngine.Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
              spaceClear = false;
            }
            else
            {
              RaycastHit[] spawnHits = UnityEngine.Physics.SphereCastAll(simSpawnPos, safeRadius, attemptDir.normalized, playerSpawnDistance, UnityEngine.Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
              foreach (var h in spawnHits)
              {
                if (h.collider.GetComponentInParent<Ring>() != null) continue;
                if (h.collider.CompareTag("Player")) continue;
                spaceClear = false;
                break;
              }
            }
          }

          if (spaceClear)
          {
            validPos = testPos;
            foundSpace = true;
            Vector3 actualDir = validPos - lastPos;
            actualDir.y = 0;
            if (actualDir != Vector3.zero) currentDir = actualDir.normalized;
            break;
          }
        }

        if (!foundSpace)
        {
          validPos = lastPos + currentDir * fixedRingSpacing;
          int escapeAttempts = 0;
          while (escapeAttempts < 30)
          {
            bool escapeClear = true;
            Collider[] cols = UnityEngine.Physics.OverlapSphere(validPos, clearRadius, UnityEngine.Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (var c in cols)
            {
              if (c.GetComponentInParent<Ring>() != null) continue;
              if (c.CompareTag("Player")) continue;
              escapeClear = false;
              break;
            }
            if (escapeClear) break;
            validPos += Vector3.up * 20f + currentDir * 10f;
            escapeAttempts++;
          }

          Vector3 escapeDir = validPos - lastPos;
          escapeDir.y = 0;
          if (escapeDir != Vector3.zero) currentDir = escapeDir.normalized;
        }

        lastPos = validPos;
        coursePositions.Add(validPos);
      }

      List<Vector3> guidedPositions = new List<Vector3>();

      for (int i = 0; i < coursePositions.Count; i++)
      {
        if (guidedPositions.Count >= maxRings) break;

        guidedPositions.Add(coursePositions[i]);

        if (i < coursePositions.Count - 1)
        {
          Vector3 inDir = (i == 0) ? (coursePositions[0] - spawnAreaCenter).normalized : (coursePositions[i] - coursePositions[i - 1]).normalized;
          inDir.y = 0;
          if (inDir == Vector3.zero) inDir = Vector3.forward;

          Vector3 outDir = (coursePositions[i + 1] - coursePositions[i]).normalized;
          outDir.y = 0;
          if (outDir == Vector3.zero) continue;

          float turnAngle = Vector3.Angle(inDir, outDir);

          if (turnAngle > 30f)
          {
            int steps = turnAngle > 60f ? 2 : 1;
            for (int s = 1; s <= steps; s++)
            {
              if (guidedPositions.Count >= maxRings) break;

              float t = (float)s / (float)(steps + 1);
              Vector3 midpoint = Vector3.Lerp(coursePositions[i], coursePositions[i + 1], t);

              float midGroundY = midpoint.y;
              RaycastHit[] midHits = UnityEngine.Physics.RaycastAll(new Vector3(midpoint.x, midpoint.y + 500f, midpoint.z), Vector3.down, 1000f, UnityEngine.Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
              float highestMid = -9999f;
              foreach (var h in midHits)
              {
                if (h.collider.GetComponentInParent<Ring>() != null) continue;
                if (h.collider.CompareTag("Player")) continue;
                if (h.point.y > highestMid) highestMid = h.point.y;
              }
              if (highestMid > -9999f)
                midpoint.y = Mathf.Max(midpoint.y, highestMid + clearRadius + 5f);

              guidedPositions.Add(midpoint);
            }
          }
        }
      }

      for (int i = 0; i < guidedPositions.Count; i++)
      {
        Quaternion placeholderRot = Quaternion.identity;
        Ring newRing = Instantiate(ringPrefab, guidedPositions[i], placeholderRot, transform);
        newRing.Initialize(this);
        newRing.gameObject.SetActive(true);
        activeCourse.Add(newRing);
      }

      for (int i = 0; i < activeCourse.Count; i++)
      {
        Vector3 outgoingDir = (i < activeCourse.Count - 1) ? (activeCourse[i + 1].transform.position - activeCourse[i].transform.position).normalized : (activeCourse[i].transform.position - activeCourse[i - 1].transform.position).normalized;
        Vector3 incomingDir = (i > 0) ? (activeCourse[i].transform.position - activeCourse[i - 1].transform.position).normalized : outgoingDir;

        Vector3 smoothDir = (incomingDir + outgoingDir).normalized;
        if (smoothDir == Vector3.zero) smoothDir = Vector3.forward;

        float yawAngle = Mathf.Atan2(smoothDir.x, smoothDir.z) * Mathf.Rad2Deg;
        yawAngle = Mathf.Round(yawAngle / 45f) * 45f;
        activeCourse[i].transform.rotation = Quaternion.Euler(90f, yawAngle, 0f);
      }

      UpdateRingVisibilityStates();
      UpdateHUD();

      if (hud != null && activeCourse.Count > 0)
        hud.SetNextRingTarget(activeCourse[0].transform);
    }

    private void UpdateRingVisibilityStates()
    {
      for (int i = 0; i < activeCourse.Count; i++)
      {
        if (activeCourse[i] == null) continue;

        if (i < currentRingIndex)
        {
          activeCourse[i].gameObject.SetActive(false);
        }
        else if (i == currentRingIndex)
        {
          activeCourse[i].gameObject.SetActive(true);
          activeCourse[i].SetState(true, false);
        }
        else if (i == currentRingIndex + 1)
        {
          activeCourse[i].gameObject.SetActive(true);
          activeCourse[i].SetState(false, true);
        }
        else if (i > currentRingIndex + 1 && i <= currentRingIndex + 3)
        {
          activeCourse[i].gameObject.SetActive(true);
          activeCourse[i].SetState(false, false);
        }
        else
        {
          activeCourse[i].gameObject.SetActive(false);
        }
      }
    }

    private void SpawnPlayerAtFirstRing()
    {
      if (activeCourse.Count == 0 || playerTransform == null) return;

      Transform firstRing = activeCourse[0].transform;

      Vector3 courseDir = (activeCourse.Count > 1) ? (activeCourse[1].transform.position - firstRing.position) : firstRing.forward;
      courseDir.y = 0f;
      courseDir.Normalize();

      Vector3 spawnPos = Vector3.zero;
      bool spawnClear = false;

      Vector3 ring0BasePos = firstRing.position;
      Vector3 perpendicular = new Vector3(-courseDir.z, 0f, courseDir.x);

      for (int attempt = 0; attempt < 20; attempt++)
      {
        Vector3 candidateSpawn = firstRing.position - courseDir * playerSpawnDistance;

        if (!UnityEngine.Physics.CheckSphere(candidateSpawn, clearRadius, UnityEngine.Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
          spawnPos = candidateSpawn;
          spawnClear = true;
          break;
        }

        float nudge = (attempt + 1) * 30f;
        float side = (attempt % 2 == 0) ? nudge : -nudge;
        firstRing.position = ring0BasePos + perpendicular * side;
      }

      if (!spawnClear)
      {
        spawnPos = firstRing.position - courseDir * playerSpawnDistance;
      }

      Vector3 lookDirection = (firstRing.position - spawnPos).normalized;
      if (lookDirection == Vector3.zero) lookDirection = courseDir;
      Quaternion spawnRotation = Quaternion.LookRotation(lookDirection, Vector3.up);

      Rigidbody rb = playerTransform.GetComponent<Rigidbody>();
      if (rb != null)
      {
        rb.position = spawnPos;
        rb.rotation = spawnRotation;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
      }
      else
      {
        playerTransform.position = spawnPos;
        playerTransform.rotation = spawnRotation;
      }

      Player.Controller.Mouse mouseCtrl = FindObjectOfType<Player.Controller.Mouse>();
      if (mouseCtrl != null)
      {
        mouseCtrl.ResetAimToAircraft();
      }
    }

    public void OnRingPassed(Ring passedRing)
    {
      if (!isGameActive) return;

      if (currentRingIndex < activeCourse.Count && passedRing == activeCourse[currentRingIndex])
      {
        currentRingIndex++;
        ringsCollected++;

        if (playerAircraft != null)
        {
          playerAircraft.currentOilPercent += fuelRewardPerRing;
          playerAircraft.currentOilPercent = Mathf.Clamp01(playerAircraft.currentOilPercent);
          playerAircraft.hasOil = true;
        }

        UpdateRingVisibilityStates();

        if (currentRingIndex < activeCourse.Count)
        {
          if (hud != null)
            hud.SetNextRingTarget(activeCourse[currentRingIndex].transform);
        }
        else
        {
          // COURSE COMPLETE
          if (hud != null) hud.SetNextRingTarget(null);

          foreach (var ring in activeCourse)
          {
            if (ring != null) Destroy(ring.gameObject);
          }
          activeCourse.Clear();

          SetupMenuState(isVictory: true);
        }

        UpdateHUD();
      }
    }

    public void OnPlayerDeath()
    {
      if (!isGameActive) return;

      if (hud != null) hud.SetNextRingTarget(null);

      foreach (var ring in activeCourse)
      {
        if (ring != null) Destroy(ring.gameObject);
      }
      activeCourse.Clear();

      SetupMenuState(isVictory: false, isDead: true);
    }

    private void UpdateHUD()
    {
      if (hud != null)
      {
        hud.UpdateRingText(ringsCollected, activeCourse.Count);
      }
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = new Color(0, 1, 0, 0.2f);
      Gizmos.DrawCube(spawnAreaCenter, spawnAreaSize);
      Gizmos.color = Color.green;
      Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);
    }
  }
}
