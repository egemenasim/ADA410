using UnityEngine;

public class SolarSystemMovement : MonoBehaviour
{
    [Header("Central Body")]
    [Tooltip("Sun (center of the system). If left empty the script's GameObject will be used.")]
    public Transform Sun;

    [Header("Planets (assign each planet's Transform)")]
    [Tooltip("Mercury")]
    public Transform Mercury;

    [Tooltip("Venus")]
    public Transform Venus;

    [Tooltip("Earth")]
    public Transform Earth;

    [Tooltip("Mars")]
    public Transform Mars;

    [Tooltip("Jupiter")]
    public Transform Jupiter;

    [Tooltip("Saturn")]
    public Transform Saturn;

    [Tooltip("Uranus")]
    public Transform Uranus;

    [Tooltip("Neptune")]
    public Transform Neptune;

    // Orbit radii are derived from each planet's initial XZ distance to the chosen center
    float mercuryOrbitRadius, venusOrbitRadius, earthOrbitRadius, marsOrbitRadius, jupiterOrbitRadius, saturnOrbitRadius, uranusOrbitRadius, neptuneOrbitRadius;

    [Header("Global Speed")]
    [Tooltip("How many simulated Earth days pass per real second. One global control affects all planets (orbital + axial rotation).")]
    public float simulatedDaysPerRealSecond = 1f;

    [Tooltip("If true, planets' start positions are projected onto their configured orbit radius on the XZ plane (keeps their Y).")]
    public bool initializePositionsToOrbit = true;

    [Tooltip("If true, all planets orbit around world origin (0,0,0) instead of the assigned Sun.")]
    public bool orbitAroundWorldOrigin = false;

    // Internal angles (degrees) for each planet's orbit
    float mercuryAngle, venusAngle, earthAngle, marsAngle, jupiterAngle, saturnAngle, uranusAngle, neptuneAngle;

    void Reset()
    {
        if (Sun == null) Sun = this.transform;
    }
    
        [Header("Debug")]
        [Tooltip("Enable to log initialization details for each planet (center used, initial pos, computed radius, angle, final pos). Helps diagnose small position shifts.")]
        public bool debugLogInitialization = false;

    void Start()
    {
        if (Sun == null) Sun = this.transform;

        if (initializePositionsToOrbit)
        {
            Vector3 center = orbitAroundWorldOrigin ? Vector3.zero : Sun.position;
            InitializeAngle(ref Mercury, ref mercuryAngle, out mercuryOrbitRadius, center);
            InitializeAngle(ref Venus, ref venusAngle, out venusOrbitRadius, center);
            InitializeAngle(ref Earth, ref earthAngle, out earthOrbitRadius, center);
            InitializeAngle(ref Mars, ref marsAngle, out marsOrbitRadius, center);
            InitializeAngle(ref Jupiter, ref jupiterAngle, out jupiterOrbitRadius, center);
            InitializeAngle(ref Saturn, ref saturnAngle, out saturnOrbitRadius, center);
            InitializeAngle(ref Uranus, ref uranusAngle, out uranusOrbitRadius, center);
            InitializeAngle(ref Neptune, ref neptuneAngle, out neptuneOrbitRadius, center);

            if (debugLogInitialization)
            {
                Debug.Log($"[SolarSystemMovement] Center used: {center}");
                LogInit("Mercury", Mercury, mercuryOrbitRadius, mercuryAngle, center);
                LogInit("Venus", Venus, venusOrbitRadius, venusAngle, center);
                LogInit("Earth", Earth, earthOrbitRadius, earthAngle, center);
                LogInit("Mars", Mars, marsOrbitRadius, marsAngle, center);
                LogInit("Jupiter", Jupiter, jupiterOrbitRadius, jupiterAngle, center);
                LogInit("Saturn", Saturn, saturnOrbitRadius, saturnAngle, center);
                LogInit("Uranus", Uranus, uranusOrbitRadius, uranusAngle, center);
                LogInit("Neptune", Neptune, neptuneOrbitRadius, neptuneAngle, center);
            }
        }
    }

    void InitializeAngle(ref Transform t, ref float angle, out float orbitRadius, Vector3 center)
    {
        orbitRadius = 0f;
        if (t == null) return;
        Vector3 dir = t.position - center;
        Vector3 dirXZ = new Vector3(dir.x, 0f, dir.z);
        float dist = dirXZ.magnitude;
        if (dist < 0.0001f)
        {
            angle = 0f;
            orbitRadius = 1f; // default small radius so it doesn't sit on center
            t.position = center + new Vector3(orbitRadius, 0f, 0f);
        }
        else
        {
            orbitRadius = dist;
            angle = Mathf.Atan2(dirXZ.z, dirXZ.x) * Mathf.Rad2Deg;
            Vector3 orbitPos = center + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad)) * orbitRadius;
            float originalY = t.position.y;
            t.position = new Vector3(orbitPos.x, originalY, orbitPos.z);
        }
    }

    void LogInit(string name, Transform t, float orbitRadius, float angleDeg, Vector3 center)
    {
        if (t == null)
        {
            Debug.Log($"[SolarSystemMovement] {name}: Transform not assigned");
            return;
        }
        Vector3 initialPos = t.position;
        Vector3 expectedPos = center + new Vector3(Mathf.Cos(angleDeg * Mathf.Deg2Rad), 0f, Mathf.Sin(angleDeg * Mathf.Deg2Rad)) * orbitRadius;
        Debug.Log($"[SolarSystemMovement] {name}: initialPos={initialPos}, orbitRadius={orbitRadius:F6}, angleDeg={angleDeg:F4}, expectedPos={expectedPos}");
    }

    void Update()
    {
        if (Sun == null) return;

        float deltaSimDays = Time.deltaTime * simulatedDaysPerRealSecond;

        // For each planet we use hardcoded orbital + rotation periods from your data
        // Orbital periods (days) are not the full orbital elements (we use simple angular speed)

        // SUN: no orbit

    Vector3 center = orbitAroundWorldOrigin ? Vector3.zero : Sun.position;

    // MERCURY
    UpdatePlanet(Mercury, mercuryOrbitRadius, ref mercuryAngle, 88f /* orbital period days (approx) */, 58.646f /* rotation days */, deltaSimDays, center);

        // VENUS
    UpdatePlanet(Venus, venusOrbitRadius, ref venusAngle, 224.7f /* orbital */, -243.018f /* rotation negative = retrograde */, deltaSimDays, center);

        // EARTH
    UpdatePlanet(Earth, earthOrbitRadius, ref earthAngle, 365.256f /* orbital (sidereal) */, 0.9973f /* rotation in days */, deltaSimDays, center);

        // MARS
    UpdatePlanet(Mars, marsOrbitRadius, ref marsAngle, 686.98f /* orbital */, 1.026f /* rotation */, deltaSimDays, center);

        // JUPITER
    UpdatePlanet(Jupiter, jupiterOrbitRadius, ref jupiterAngle, 4332.59f /* orbital */, 0.4135f /* rotation */, deltaSimDays, center);

        // SATURN
    UpdatePlanet(Saturn, saturnOrbitRadius, ref saturnAngle, 10759f /* orbital */, 0.444f /* rotation */, deltaSimDays, center);

        // URANUS
    UpdatePlanet(Uranus, uranusOrbitRadius, ref uranusAngle, 30685f /* orbital */, -0.718f /* rotation - appears retrograde due axis tilt; negative to reflect */ , deltaSimDays, center);

        // NEPTUNE
    UpdatePlanet(Neptune, neptuneOrbitRadius, ref neptuneAngle, 60189f /* orbital */, 0.671f /* rotation */, deltaSimDays, center);
    }

    void UpdatePlanet(Transform t, float orbitRadius, ref float angleDeg, float orbitalPeriodDays, float rotationPeriodDays, float deltaSimDays, Vector3 center)
    {
        if (t == null) return;

        // Orbital angular speed (degrees per day)
        float orbitDegPerDay = (Mathf.Approximately(orbitalPeriodDays, 0f)) ? 0f : 360f / orbitalPeriodDays;
        angleDeg += orbitDegPerDay * deltaSimDays;

        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 orbitPos = center + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * orbitRadius;
        float height = t.position.y - center.y;
        t.position = new Vector3(orbitPos.x, center.y + height, orbitPos.z);

        // Axial rotation
        if (!Mathf.Approximately(rotationPeriodDays, 0f))
        {
            float rotDegPerDay = 360f / rotationPeriodDays;
            float rotDelta = rotDegPerDay * deltaSimDays;
            t.Rotate(Vector3.up, rotDelta, Space.Self);
        }
    }
}
