// 3-body Starter Code
// Fall 2026. IMDM 327
// Instructor. Myungin Lee
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

public class ThreeBody : MonoBehaviour
{
    public float G = 500f; // Gravitational constant for this simulation, not the real-world value.
    public float minDist = 10f;
    public float maxDist = 30f; // this creates an almost flock efffect --> farther away, chase other elements faster
    public float extraForce = 10f;
    public float z = 180f;
    public float initRadius = 300f;
    BodyProperty[] bp;
    public int numberOfSphere = 3;

    // mediapipe variables
    private GameObject[] interactivePoint = new GameObject[2];
    private Vector3[] previousHandPosition = new Vector3[2];
    private bool[] handWasPinched = new bool[2];
    private float[] actuation = new float[2]; // how long smth stays (slowly decays over time as fish food disappears)
    public float interactiveMass = 100f; // mass of hand
    public float gravityDecay = 2f;
    MediaPipeBodyTracker mp;
    Camera cam;
    public float maxVelocity = 6f; // may not use

    class BodyProperty // why struct?
    {                   // https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/choosing-between-class-and-struct
        public GameObject body;
        public float mass;
        public Vector3 velocity;
        public Vector3 acceleration;
    }


    void Start()
    {
        // Allocate an array to store each body's properties.
        bp = new BodyProperty[numberOfSphere];
        // Loop generating the gameobject and assign initial conditions (type, position, (mass/velocity/acceleration)
        for (int i = 0; i < numberOfSphere; i++)
        {
            // Our gameobjects are created here:
            bp[i] = new BodyProperty();
            bp[i].body = GameObject.CreatePrimitive(PrimitiveType.Sphere); // why sphere? try different options.
            // https://docs.unity3d.com/ScriptReference/GameObject.CreatePrimitive.html

            // initial conditions
            float r = initRadius;
            // position is (x,y,z). In this case, I want to plot them on the circle with r

            // ******** Fill in this part ********
            float theta = i * (2 * Mathf.PI/numberOfSphere);
            float x = r * Mathf.Cos(theta) + Random.Range(-10f, 10f);
            float y = r * Mathf.Sin(theta) + Random.Range(-10f, 10f);
            bp[i].body.transform.position = new Vector3(x, y, z);
            // z = 180 places the bodies in front of a camera near the origin looking along +Z. Try other positions too.

            bp[i].velocity = new Vector3(Random.Range(-10f, 10f), Random.Range(-10f, 10f)); 
            bp[i].mass = Random.Range(1f, 10f); 
            bp[i].body.transform.localScale *= bp[i].mass;

            // + This is just pretty trails
            TrailRenderer trailRenderer = bp[i].body.AddComponent<TrailRenderer>();
            // Configure the TrailRenderer's properties
            trailRenderer.time = 100.0f;  // Duration of the trail
            trailRenderer.startWidth = 0.5f;  // Width of the trail at the start
            trailRenderer.endWidth = 0.1f;    // Width of the trail at the end
            // a material to the trail
            trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
            // Set the colour gradient along the trail.
            Gradient gradient = new Gradient();
            Color targetColor = Color.HSVToRGB((float)i / numberOfSphere, 1f, 1f);
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f), // (color, normalized position)
                    new GradientColorKey(targetColor, 0.8f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f), // (alpha, normalized position) 
                    new GradientAlphaKey(0f, 1f)
                }
            );
            trailRenderer.colorGradient = gradient;

            Renderer matRenderer = bp[i].body.GetComponent<Renderer>();
            matRenderer.material.color = targetColor;
        }
    }

    void Update()
    {
        // Loop for N-body gravity
        // How should we design the loop?
        // for (int i = 0; i < numberOfSphere; i++)
        // {
        //     bp[i].acceleration = Vector3.zero;
        //     for (int j = 0; j < numberOfSphere; j++)
        //     {
        //         if (i != j)
        //         {
        //             // grav force
        //             Vector3 distance = bp[i].body.transform.position - bp[j].body.transform.position;
        //             Vector3 grav = CalculateGravity(distance, bp[i].mass, bp[j].mass);

        //             // acceleration   
        //             bp[i].acceleration -= grav / bp[j].mass;
        //         }
        //     }

        //     // Velocity
        //     bp[i].velocity += bp[i].acceleration * Time.deltaTime * 30f;

        //     // Position
        //     bp[i].body.transform.position += bp[i].velocity * Time.deltaTime;
        // }


        for (int i = 0; i < numberOfSphere; i ++)
        {
            bp[i].acceleration = Vector3.zero;
        }

        for (int i = 0; i < numberOfSphere; i ++)
        {
            for (int j = i + 1; j < numberOfSphere; j++)
            {
                Vector3 distance = bp[i].body.transform.position - bp[j].body.transform.position;
                Vector3 grav = CalculateGravity(distance, bp[i].mass, bp[j].mass);

                Vector3 centerDist = bp[i].body.transform.position - new Vector3(0, 0, z);

                // has spheres do little bumps --> in order for this to work, it can't be minDst
                // as minDist is used inside of CalculateGravity (grav= Vector3.zero if > minDist)
                if (distance.magnitude < minDist + 3)
                {
                    bp[i].acceleration += extraForce * grav / bp[i].mass;
                    bp[j].acceleration -= extraForce * grav / bp[i].mass;
                }
                else
                {
                    bp[i].acceleration -= grav/bp[i].mass;
                    bp[j].acceleration += grav/bp[j].mass;
                }

                // apply extra force towards center if too far off-screen
                if (centerDist.magnitude > maxDist)
                {
                    Vector3 centerGrav = CalculateGravity(centerDist, bp[i].mass, 30f);
                    bp[i].acceleration -= extraForce * centerGrav/bp[i].mass;
                }

            }
        }

        for (int i = 0; i < numberOfSphere; i ++)
        {
            // Velocity
            bp[i].velocity += bp[i].acceleration * Time.deltaTime * 30f;

            // Position
            bp[i].body.transform.position += bp[i].velocity * Time.deltaTime;
        }

        // predators = mouse

    }
    

    // Gravity Fuction to finish
    private Vector3 CalculateGravity(Vector3 distanceVector, float m1, float m2)
    {
        Vector3 gravity = Vector3.zero; // note this is also Vector3
                                        // **** Fill in the function below. 
                                        // gravity = ****;
        if (distanceVector != Vector3.zero && distanceVector.magnitude > minDist)
        {
            gravity = G * m1 * m2 / (distanceVector.sqrMagnitude) * distanceVector.normalized;
        }

        return gravity;
    }

}

