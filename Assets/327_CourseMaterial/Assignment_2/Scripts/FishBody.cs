// 3-body Starter Code
// Fall 2026. IMDM 327
// Instructor. Myungin Lee
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using TMPro;

public class FishBody : MonoBehaviour
{
    public float G = 500f; // Gravitational constant for this simulation, not the real-world value.
    public float minDist = 10f;
    public float maxDist = 30f; // this creates an almost flock efffect --> farther away, chase other elements faster
    public float extraForce = 10f;
    public float z = 180f;
    public float initRadius = 300f;
    List<BodyProperty> bp;
    public float rotationSpeed = 1f;
    
    // fish variables
    public GameObject fish;
    public int numberOfFish = 3;
    public float fishSizeMax = 2f;
    public float fishSizeMin = 1f;
    public float maxFishSpeed = 30f;
    public float minFishSpeed = 5f;

    // mediapipe variables
    private GameObject[] interactivePoint = new GameObject[2]; // point used to spawn hand sprites
    public GameObject rightClosedHand; // reference hand GameObject
    public GameObject rightOpenHand; 
    public GameObject leftClosedHand; 
    public GameObject leftOpenHand; 
    public float handSize = 6f;
    HandProperty[] hands = new HandProperty[2]; // ^^ hands spawn at interactivePoints
    
    // food generation variables
    // every 1 seconds, generate food
    public float foodGenerationSpeed = 1f;
    private float timer = 0f;
    private List<actuationProp> actuation = new List<actuationProp>(); // how long smth stays (slowly decays over time as fish food disappears)
    public float interactiveMass = 30f; // mass of hand
    public float massDividerForScale = 10f;
    public float gravityDecay = 2f;
    public float minFoodSize = 5f;
    public float foodRadius = 30f;
    public float extraForceForFood = 5f;
    public float slowDown = 0.0001f;
    MediaPipeBodyTracker mp;
    Camera cam;


    // game-play
    // instructions
    private float overallTimer = 0f;
    
    private float instructionsTimer = 0f;
    public float instructionsTimeLength = 3f;
    public float startInstructions = 3f;
    private int instructionsIndex = 0;
    private string[] instructions = new string[3];
    
    private float startTime;
    private float surviveTimer = 0f;
    public float surviveTime = 10f;

    public TextMeshProUGUI mainText;
    public TextMeshProUGUI timerText;
    
    // enemy
    public GameObject enemyModel;
    EnemyProperty enemy;
    public float enemyRadius = 50f;
    public float enemySpeed = 10f;
    public float enemyTravelDist = 20f;
    private float spawnEnemyTimer = 0f;
    public float spawnEnemyRate = 5f;
    public float enemyLifespan = 2f;
    private bool died = false;
    

    class BodyProperty // why struct?
    {                   // https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/choosing-between-class-and-struct
        public GameObject body; // fish
        public float mass;
        public Vector3 velocity;
        public Vector3 acceleration;
    }

    class HandProperty
    {
        public GameObject open;
        public GameObject closed;
    }

    class actuationProp
    {
        public Vector3 location;
        public float mass;
        public GameObject debugBody;

        // TODO: get this implemented inside the code
        public GameObject[] pellets;
    }

    class EnemyProperty
    {
        public GameObject body;
        public float existTimer;
        public bool spawned;
        public Vector3 endDest;
    }

    void Start()
    {
        // instructions setup
        instructions[0] = "Open your hands to drop fish food.\nClose/Pinch your hand to stop dropping food.";
        instructions[1] = "Keep your koi fish away from the Salmon, they'll eat your fish.";
        instructions[2] = $"Survive for {surviveTime} seconds to win.";
        // offset the gameplay time by instructions time 
        startTime = startInstructions + instructionsTimeLength * instructions.Length;

        Debug.Log($"Start time: {startTime}");

        // code to activate mediaPipe 
        if (mp == null)
        {
            mp = FindObjectOfType<MediaPipeBodyTracker>();
            if (mp == null)
            {
                Debug.LogWarning("InteractiveBody could not locate a MediaPipeBodyTracker in the scene.");
            }
        }
        cam = Camera.main;
        // 0: left hand, 1: right hand
        hands[0] = new HandProperty();
        hands[1] = new HandProperty();

        hands[0].open = Instantiate(leftOpenHand);
        hands[0].closed = Instantiate(leftClosedHand);
        hands[1].open = Instantiate(rightOpenHand);
        hands[1].closed = Instantiate(rightClosedHand);
        for (int hand = 0; hand < 2; hand++)
        {
            interactivePoint[hand] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            interactivePoint[hand].transform.localScale = Vector3.zero;
            Destroy(interactivePoint[hand].GetComponent<Collider>());
            interactivePoint[hand].SetActive(false);

            hands[hand].closed.transform.localScale = new Vector3(handSize, handSize, handSize);
            hands[hand].open.transform.localScale = new Vector3(handSize, handSize, handSize);
            // later set rotation so that the hand is facing the bottom of the floor

            // set false when hands aren't on screen
            hands[hand].closed.SetActive(false);
            hands[hand].open.SetActive(false);
        }


        // get enemy set up
        if (enemyModel != null)
        {
            if (spawnEnemyRate < enemyLifespan)
            {
                spawnEnemyRate = enemyLifespan;
            }
            enemy = new EnemyProperty();
            enemy.body = Instantiate(enemyModel);
            enemy.body.SetActive(false);
            enemy.existTimer = 0f;
        }

        // Allocate an array to store each body's properties.
        bp = new List<BodyProperty>();
        // Loop generating the gameobject and assign initial conditions (type, position, (mass/velocity/acceleration)
        for (int i = 0; i < numberOfFish; i++)
        {
            // Create fish
            bp.Add(new BodyProperty());
            bp[i].body = Instantiate(fish); 
            Debug.Log($"instantiated fish: {bp[i].body.transform.position.x}");
            bp[i].body.SetActive(true);

            // initial conditions
            float r = initRadius;
            // position is (x,y,z). In this case, I want to plot them on the circle with r

            // ******** Fill in this part ********
            float theta = i * (2 * Mathf.PI/numberOfFish);
            float x = r * Mathf.Cos(theta) + Random.Range(-10f, 10f);
            float y = r * Mathf.Sin(theta) + Random.Range(-10f, 10f);
            bp[i].body.transform.position = new Vector3(x, y, z);
            // z = 180 places the bodies in front of a camera near the origin looking along +Z. Try other positions too.

            bp[i].velocity = new Vector3(Random.Range(-10f, 10f), Random.Range(-10f, 10f)); 
            bp[i].mass = Random.Range(fishSizeMin / 10, fishSizeMax / 10); 
            float size = (bp[i].mass * 10 - fishSizeMin) / 2 + fishSizeMin;
            bp[i].body.transform.localScale *= size;
        }
    }

    void Update()
    {
        // timer is for food
        timer += Time.deltaTime;
        overallTimer += Time.deltaTime;
        // TODO: slowly decrease alpha of instructions then delete


        for (int i = 0; i < bp.Count; i ++)
        {
            bp[i].acceleration = Vector3.zero;
        }

        for (int i = 0; i < bp.Count; i ++)
        {
            for (int j = i + 1; j < bp.Count; j++)
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

            // calculate grav for all actuation points and fish
            for (int k = 0; k < actuation.Count; k ++)
            {
                // Debug.Log($"k = {k}");
                // Debug.Log($"actuation is fine : {actuation[k]}");
                // Debug.Log($"i: {i}");
                // Debug.Log($"bp is fine: {bp[i]}");
                Vector3 distance = actuation[k].location - bp[i].body.transform.position;
                // acc = F/g
                bp[i].acceleration += CalculateGravity(distance, bp[i].mass, actuation[k].mass) / bp[i].mass;
                float radius = Mathf.Max(0.01f, foodRadius);
                if (distance.magnitude > radius)
                    bp[i].acceleration += distance.normalized * (distance.magnitude - radius) * extraForceForFood;
                else
                {
                    bp[i].acceleration = -bp[i].velocity.normalized * slowDown;
                }
                    
            }
        }

        // (Force) Interactive Acceleration : reacts to the actuation of the interactive point
        if (mp == null)
        {
            mp = FindObjectOfType<MediaPipeBodyTracker>();
        }

        for (int hand = 0; hand < 2; hand++)
        {
            bool handTracked = mp != null && (hand == 0 ? mp.LeftHandTracked : mp.RightHandTracked);
            bool handPinched = handTracked && (hand == 0 ? mp.LeftHandPinch : mp.RightHandPinch);

            // if handtracked and pinched, closed hand + no fish food falling (no grav)
            if (handPinched)
            {
                // close hand
                hands[hand].open.SetActive(false);
                hands[hand].closed.SetActive(true);

                // set position
                Vector3 handPosition = hand == 0 ? mp.LeftHandPosition : mp.RightHandPosition;
                Vector3 interactPoint = cam.ViewportToWorldPoint(new Vector3(1f - handPosition.x, 1f - handPosition.y, z - 50));

                hands[hand].closed.transform.position = interactPoint;
            }
            // if handtracked and NOT pinched, open hand + fish food falling (grav calc)
            else if (handTracked)
            {
                // open hand
                hands[hand].closed.SetActive(false);
                hands[hand].open.SetActive(true);

                // calculate interaction Point (aka handposition in world space)
                Vector3 handPosition = hand == 0 ? mp.LeftHandPosition : mp.RightHandPosition;
                Vector3 interactPoint = cam.ViewportToWorldPoint(new Vector3(1f - handPosition.x, 1f - handPosition.y, 50));
                interactivePoint[hand].transform.position = interactPoint;

                hands[hand].open.transform.position = interactPoint;
                
                // only actually create a new actuation point if timer is done
                // this is done to prevent over generation of food
                if (timer >= foodGenerationSpeed)
                {
                    // reset timer
                    timer = 0f;

                    // create new Actuation Point (food) which slowly degrades overtime (gets eatten/falls to floor)
                    actuationProp newAct = new actuationProp();
                    newAct.mass = interactiveMass;
                    newAct.location = interactPoint;
                    // make it spawn below the hand
                    newAct.location.z = z;

                    if (newAct.location.y < 700 && newAct.location.y > -700 && newAct.location.x < 400 && newAct.location.x > -400)
                    {
                        // just for debugging
                        newAct.debugBody = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        newAct.debugBody.transform.localScale = new Vector3(newAct.mass / massDividerForScale, newAct.mass/ massDividerForScale, newAct.mass/ massDividerForScale);
                        newAct.debugBody.transform.position = newAct.location;
                        
                        actuation.Add(newAct);
                    }
                }
            }
            else
            {
                // no hand, close both
                hands[hand].closed.SetActive(false);
                hands[hand].open.SetActive(false);
            }

        }

        // and degrade old ones + delete if <= 0
        for (int i = actuation.Count - 1; i >= 0; i--)
        {
            float Scale = actuation[i].mass / massDividerForScale;
            actuation[i].mass = Mathf.MoveTowards(actuation[i].mass, 0f, gravityDecay * Time.fixedDeltaTime);

            // Debug.Log($"decaying mass: {actuation[i].mass}");

            actuation[i].debugBody.transform.localScale = new Vector3(Scale, Scale, Scale);
            if (Scale < minFoodSize)
            {
                Destroy(actuation[i].debugBody);
                actuation.RemoveAt(i);
            }
        }

        for (int i = 0; i < bp.Count; i ++)
        {
            // Velocity
            bp[i].velocity += bp[i].acceleration * Time.deltaTime * 30f;

            // Limit the maximum velocity
            if (bp[i].velocity.magnitude > maxFishSpeed)
            {
                bp[i].velocity = maxFishSpeed * bp[i].velocity.normalized;
            }
            else if (bp[i].velocity.magnitude < minFishSpeed)
            {
                bp[i].velocity = minFishSpeed * bp[i].velocity.normalized;
            }
            else if (bp[i].velocity.magnitude == 0)
            {
                // give a slight random velocity
                bp[i].velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                bp[i].velocity = minFishSpeed * bp[i].velocity.normalized;
            }

            // Position
            bp[i].body.transform.position += bp[i].velocity * Time.deltaTime;

            // TODO: rotation not working :(
            // some elements stolen from ChickenBoids
            if (bp[i].velocity.sqrMagnitude > 0.02f)
            {
                // added 20 so the fish loop "up" at the food
                var lookat = bp[i].velocity;
                lookat.z += 20;
                var rotation = Quaternion.LookRotation(lookat);

                bp[i].body.transform.rotation = Quaternion.Slerp(bp[i].body.transform.rotation,
                rotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }

        // Debug.Log($"Current time: {overallTimer}");

        // have to survive for a certain amount of time
        if (overallTimer > startInstructions && overallTimer < startTime)
        {
            Debug.Log("started instructions");
            instructionsTimer -= Time.deltaTime;
            if (instructionsTimer <= 0)
            {
                instructionsTimer = instructionsTimeLength;
                Debug.Log($"instructions in array {instructions[instructionsIndex]}");
                Debug.Log($"instructions in text {mainText.text}");
                mainText.text = instructions[instructionsIndex];

                // get next instruction
                instructionsIndex += 1;
            }
        }
        else if (overallTimer >= startTime && overallTimer < (surviveTime+ startTime) && !died)
        {
            surviveTimer += Time.deltaTime;
            mainText.text = "";
            timerText.text = $"{surviveTimer}";

            // TODO: add in predator that moves randomly in a straight line 
            // random spawn, random dest, moves in that direction linearly (z stays the same, just random x and y)
            // if collides, die
            spawnEnemyTimer += Time.deltaTime;
            Debug.Log($"enemy spawn timer: {spawnEnemyTimer}");

            if (enemy != null && spawnEnemyTimer > spawnEnemyRate)
            {
                Debug.Log("spawned");
                spawnEnemyTimer = 0f;
                // spawn enemy
                enemy.body.SetActive(true);

                enemy.spawned = true;

                // set random spawn location
                Vector3 spawnLoc = new Vector3(Random.Range(-600f, 600f), Random.Range(-350f, 350f), z);
                enemy.body.transform.position = spawnLoc;

                // set random end destination
                Vector3 endDest = new Vector3(-spawnLoc.x, -spawnLoc.y, z);
                enemy.endDest = endDest;

                Debug.Log($"enemy start pos: {enemy.body.transform.position}");
                Debug.Log($"enemy end pos: {enemy.endDest}");
            }
            else if (enemy != null && enemy.existTimer < enemyLifespan && enemy.spawned)
            {
                enemy.existTimer += Time.deltaTime;
                Debug.Log($"enemy exist timer: {enemy.existTimer}");

                // actually move enemy 
                float step = enemySpeed * Time.deltaTime; 
                
                enemy.body.transform.position = Vector3.MoveTowards(enemy.body.transform.position, enemy.endDest, step);
                
                var lookat = enemy.endDest;
                lookat.z += 20;
                enemy.body.transform.LookAt(lookat);

                // calculate collision
                for (int i = bp.Count - 1; i >= 0; i--)
                {
                    Vector3 distance = enemy.body.transform.position - bp[i].body.transform.position;
                    if (distance.magnitude < enemyRadius)
                    {
                        Destroy(bp[i].body);
                        bp.RemoveAt(i);
                    }
                }

                // check if at destination
                if (enemy.body.transform.position == enemy.endDest)
                {
                    Debug.Log("despawned");

                    float remainingTime = enemyLifespan - enemy.existTimer;

                    spawnEnemyTimer += remainingTime;

                    // enemy despawn
                    enemy.existTimer = 0f;
                    enemy.body.SetActive(false);
                    enemy.spawned = false;
                }
            }
            else if (enemy != null && enemy.existTimer > enemyLifespan)
            {
                Debug.Log("despawned");
                // enemy despawn
                enemy.existTimer = 0f;
                enemy.body.SetActive(false);
                enemy.spawned = false;
            }

            if (bp.Count == 0)
            {
                died = true;
            }

        }
        else if (overallTimer >= (surviveTime + startTime) && !died)
        {
            timerText.text = "";
            mainText.text = "You Win!";
        }
        else if (died)
        {
            mainText.text = "You Lose :(";
        }

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

