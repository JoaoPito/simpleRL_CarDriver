using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeuralNet;
using UnityEngine.UI;

//[RequireComponent(typeof(Rigidbody))]
public class Car_Behaviour : MonoBehaviour
{

    public enum BrainType
    {
        TotallyRandom,
        RandomParents,
        TOP2
    }

    [Header("AI")]
    NN neuralNet = new NN();
    public BrainType brainType = BrainType.TotallyRandom;

    public bool active = false;
    public bool AIControlled = false;
    
    public float aiAcceleration = 0.75f;
    public float steering = 0;

    [Header("Fitness")]
    public float currentFitness = 0;
    List<float> fitnessSamples = new List<float>();
    [Range(0.01f, 2f)] public float fitSamplesInterval = 1f;
    float cSamplesInterval = 0;

    float[] raycastDist = new float[5];//Forward,Left,Right,Forward-Left,Forward-Right
    public Vector3 objective;
    public Vector3 startPos;

    [Header("Graphics/UI")]
    public Text statsText;
    Renderer[] childRender;
    
    [Header("Physics")]
    public WheelCollider frontRightWheel;
    public WheelCollider rearRightWheel;
    public WheelCollider frontLeftWheel;
    public WheelCollider rearLeftWheel;

    public float maxSteer = 25;
    public float maxPower = 100;

    float brake = 0;
    float torque = 0;
    float steer = 0;

    void Awake()
    {
        neuralNet = new NN(5, 4);

        childRender = GetComponentsInChildren<Renderer>();

        startPos = transform.position;
    }

    void LateUpdate()
    {
        if (active)
        {
            if (!AIControlled)
            {
                torque = Input.GetAxis("Vertical") * maxPower * 250 * Time.deltaTime;
                steer = Input.GetAxis("Horizontal") * maxSteer;
                brake = Input.GetKey("space") ? GetComponent<Rigidbody>().mass * 0.25f : 0.0f;
            }
            else
            {
                steering = neuralNet.CalculateNN(CalculateRaycasts());
                steer = steering * maxSteer;                

                torque = aiAcceleration * maxPower * 250 * Time.deltaTime;                
                brake = 0;   
                
                if (Vector3.Distance(transform.position, objective) < 2)
                {
                    Inactive(true);
                }

            }

            rearLeftWheel.motorTorque = torque;
            rearRightWheel.motorTorque = torque;            

            frontRightWheel.steerAngle = steer;
            frontLeftWheel.steerAngle = steer;

            frontLeftWheel.brakeTorque = brake;
            frontRightWheel.brakeTorque = brake;
            rearLeftWheel.brakeTorque = brake;
            rearRightWheel.brakeTorque = brake;

            cSamplesInterval += Time.fixedDeltaTime;

            if (cSamplesInterval > fitSamplesInterval)
            {                
                CalculateFitness();
                UpdateUI();
                cSamplesInterval = 0;
            }
        }

    }

    //***Fitness Control***
    void CalculateFitness()
    {        
        float lastFitnessSample = CaptureFitness(CalculateRaycasts());
        fitnessSamples.Add(lastFitnessSample);

        //Fitness Average Calculation
        float avg = 0;

        for(int i = 0; i < fitnessSamples.Count; i++)
        {
            avg += fitnessSamples[i];
        }

        avg = avg / fitnessSamples.Count;
        currentFitness = avg;
    }

    float CaptureFitness(float[] inputs)
    {
        float raycastDist = 0;

        //Raycast Calculations
        for (int i = 0; i < inputs.Length; i++)
        {
            raycastDist += inputs[i];
        }

        raycastDist = (raycastDist / inputs.Length)/10;

        //Distance Calculations
        float objectiveDist = (Vector3.Distance(startPos, objective) - Vector3.Distance(transform.position, objective)) / Vector3.Distance(startPos, objective);
        return objectiveDist * raycastDist;
    }

    float[] CalculateRaycasts()
    {

        Vector3[] directions = new Vector3[5]
        {
            transform.TransformDirection(Vector3.forward),
            transform.TransformDirection(Vector3.right),
            transform.TransformDirection(-Vector3.right),
            transform.TransformDirection(Vector3.forward + Vector3.right),
            transform.TransformDirection(Vector3.forward - Vector3.right),
        };
        float[] distRet = new float[directions.Length];
        RaycastHit hit;

        for(int i = 0; i < distRet.Length; i++)
        {
            if(Physics.Raycast(transform.position,directions[i], out hit))
            {
                if(hit.collider != GetComponent<Collider>())
                {
                    distRet[i] = hit.distance;
                    //Debug.Log("Hit: " + hit.collider.name + hit.distance);
                }
            }
            Debug.DrawRay(transform.position, directions[i] * 10, Color.red);
        }
        
        return distRet;
    }
    
    //***Neural Net Control***
    public void ActivateBrain(string brain)
    {
        neuralNet.InitHiddenLayers(brain);

        transform.tag = "PlayerActive";

        AIControlled = true;
        active = true;

        switch (brainType)
        {
            case BrainType.TotallyRandom:
                ColourSelf(Color.red);
                break;

            case BrainType.RandomParents:
                ColourSelf(Color.green);
                break;

            case BrainType.TOP2:
                ColourSelf(Color.yellow);
                break;
        }
    }

    public void ActivateBrain(float[][] hlWeights, float[] hlBiases, float[] OWeights, float OBiases)
    {
        neuralNet.InitHiddenLayers(hlWeights, hlBiases, OWeights, OBiases);

        transform.tag = "PlayerActive";

        AIControlled = true;
        active = true;

    }

    public void ActivateBrain()
    {
        neuralNet.InitHiddenLayers(4);

        transform.tag = "PlayerActive";

        AIControlled = true;
        active = true;
    }

    public string ExtractBrain()
    {
        return neuralNet.ExtractBrain();
    }

    //***State Control***
    void Inactive(bool endReached)
    {
        transform.tag = "PlayerInactive";

        CalculateFitness();

        Color inactiveColor = childRender[0].material.color;
        inactiveColor.a = 0.4f;
        ColourSelf(inactiveColor);

        GameObject.FindWithTag("GameController").GetComponent<GameController>().NotifyDeath(this, endReached);
        //Destroy(GetComponent<Rigidbody>());

        GetComponent<Rigidbody>().isKinematic = true;

        active = false;

        UpdateUI();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if((collision.collider.CompareTag("RoadBarrier") || collision.collider.name=="Barrier") && active)
        {
            Inactive(false);
        }
    }

    public void ColourSelf(Color color)
    {
        foreach(Renderer render in childRender)
        {
            render.material.color = color;
        }
    }

    //***UI***
    public void UpdateUI()
    {
        statsText.text = string.Concat("Fitness:" + currentFitness.ToString() + "\n" + 
                                        "active:" + active + "\n" +
                                        "steering: " + steering + "\n" +
                                        brainType.ToString());
    }
}
