using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NeuralNet;
using System;
using System.IO;

public class GameController : MonoBehaviour
{

    public enum MENU
    {
        MAINMENU,
        MAPLOAD,
        TRAIN,
        RESULTS
    }

    public MENU currentMenu = MENU.MAINMENU;

    TrackController trackCtrl;
    UI_Controller uiCtrl;   

    [Header("Cars")]
    string[] brainsToLoad;//This brains should be already sorted by fitness and ready for "implant" when StartGame is called
    string mostFitBrain; // Most fit brain of all generations

    [HideInInspector] public float maxFitness;
    [HideInInspector] public float fitLastGen;
    [HideInInspector] public float fitCurGen;

    [SerializeField] static public int NN_hlCount = 10;
    [SerializeField] static public int NN_inpCount = 5;

    public GameObject carPrefab;
    public Vector3 spawnPoint;

    [Header("Generations")]
    [HideInInspector] public int generation;//total generations
    public int minGenOnTrack = 50;//Min generations per track
    int genCurTrack = 0;//Generations on this track

    public float maxTimeout = 120f;//Max Timeout in seconds
    [HideInInspector] public float currentGenTime = -1;

    [HideInInspector] public int nActiveCars = 1;
    int carsReachedObjective = 0;//Essa variavel é incrementada mas não tem utilidade por enquanto
    List<Car_Behaviour> spawnedCars = new List<Car_Behaviour>();

    //Quantidade de carros por geração
    [Header("Cars per Gen")]
    [Range(0, 100)] public int randomParentsCount = 10;
    [Range(1, 100)] public int totallyRandomCount = 10;
    [Range(0, 100)] public int Top2Count = 10;    
    int carsPerGen;

    [Header("Parents")]
    [Range(0, 1)] public float minMutationChance = 0.5f;//Mutation threshold
    [Range(0, 5)] public float mutationRange = 0.1f;

    //LOGGING
    public bool logGenerations = true;//Logs all brains and fitnesses into a file
    Log_Controller logCtrl;

    //UI
    public float updateUIInterval = 0.3f;
    float cUIInterval = 0;

    // Start is called before the first frame update
    void Awake()
    {
        trackCtrl = GetComponent<TrackController>();
        uiCtrl = GetComponent<UI_Controller>();
        logCtrl = GetComponent<Log_Controller>();

        logCtrl.StartLogging();
    }

    void Start()
    {
        brainsToLoad = new string[randomParentsCount + totallyRandomCount + Top2Count];
    }

    // Update is called once per frame
    void Update()
    {
        if (currentMenu == MENU.TRAIN)
        {            
            if ((currentGenTime >= maxTimeout) || nActiveCars <= 0)
            {
                EndGen();
            }else if (currentGenTime >= 0)
            {
                currentGenTime += Time.fixedDeltaTime;
            }

        }
        cUIInterval += Time.fixedUnscaledDeltaTime;

        if (cUIInterval > updateUIInterval)
        {
            if (currentMenu == MENU.TRAIN)
            {
                spawnedCars = spawnedCars.OrderByDescending(o => o.GetFitness()).ToList();//Sort by fitness
            }

            uiCtrl.UpdateGamePlayUI();
            cUIInterval = 0;

            if (generation > 0)
            {
                if (spawnedCars[0].GetFitness() > fitCurGen)
                {
                    fitCurGen = spawnedCars[0].GetFitness();
                }
            }
        }
        
    }

    public void StartGen()
    {
        if (genCurTrack >= minGenOnTrack || generation==0)//If has passed X generations, change map
        {
            trackCtrl.BuildNextMap();
            spawnPoint = new Vector3(trackCtrl.trackStart.position.x, trackCtrl.trackStart.position.y + spawnPoint.y, trackCtrl.trackStart.position.z);
            Camera.main.transform.root.position = new Vector3(trackCtrl.trackStart.position.x, trackCtrl.trackStart.position.y + Camera.main.transform.root.position.y, trackCtrl.trackStart.position.z);
            genCurTrack = 0;
        }

        nActiveCars = 0;
        currentGenTime = 0;
        fitLastGen = fitCurGen;
        fitCurGen = 0;
        generation++;

        for (int i = 0; i < spawnedCars.Count; i++)
        {
            Destroy(spawnedCars[i].gameObject);
        }

        spawnedCars = new List<Car_Behaviour>();

        for (int i = 0; i < brainsToLoad.Length; i++)
        {
            NN.TYPE brainType = NN.TYPE.RandomParents;

            if (i < randomParentsCount)//Random Parents Generation
            {
                brainType = NN.TYPE.RandomParents;
            }
            else if (i < randomParentsCount + totallyRandomCount)//Totally Random Generation
            {
                brainType = NN.TYPE.TotallyRandom;
            }
            else if (i < randomParentsCount + totallyRandomCount + Top2Count)//Top2 Generation
            {
                brainType = NN.TYPE.TOP2;
            }

            spawnedCars.Add(InstantiateCar(spawnPoint, brainsToLoad[i], "car " + i.ToString(), brainType));
            nActiveCars++;
        }

        uiCtrl.UpdateGamePlayUI();

        currentMenu = MENU.TRAIN;
    }

    public void NotifyDeath(Car_Behaviour car,bool endReached)//Used by the Car Behaviours to notify when inactive
    {
        nActiveCars--;

        if (car.GetFitness() > fitCurGen)
        {
            fitCurGen = car.GetFitness();
        }

        if (endReached)
        {
            carsReachedObjective++;
        }
    }

    public void EndGen()
    {
        currentGenTime = -1;

        spawnedCars = spawnedCars.OrderByDescending(o => o.GetFitness()).ToList();//Sort

        if (fitCurGen > maxFitness)
        {
            mostFitBrain = spawnedCars[0].ExtractBrain();
            Debug.Log("mostfitbrain=" + mostFitBrain + "fitness: " + fitCurGen);
            maxFitness = fitCurGen;
        }

        LogGenerationIntoFile();
        uiCtrl.UpdateGamePlayUI();

        //Begin Next Generation

        NextGen(spawnedCars);
        StartGen();
    }

    //Controls the generation of the next children
    void NextGen(List<Car_Behaviour> lastGen)
    {
        carsPerGen = randomParentsCount + totallyRandomCount + Top2Count + 1;//Number of all cars count plus the best fit car

        brainsToLoad = new string[carsPerGen];

        for (int i = 1; i < brainsToLoad.Length - 1; i++)
        {            
            if (i < randomParentsCount + totallyRandomCount)//Totally Random Generation
            {
                brainsToLoad[i] = ""; //When a null brain is loaded, it automatically creates a random one
            }
            else if (i < randomParentsCount)//Random Parents Generation
            {
                int parent1 = UnityEngine.Random.Range(0, lastGen.Count);
                int parent2 = UnityEngine.Random.Range(0, lastGen.Count);

                brainsToLoad[i] = ChildGen(lastGen[parent1].ExtractBrain(), lastGen[parent2].ExtractBrain()) + "r";
            }
            else if (i < randomParentsCount + totallyRandomCount + Top2Count)//Top2 Generation
            {
                brainsToLoad[i] = ChildGen(lastGen[0].ExtractBrain(), lastGen[1].ExtractBrain()) + "t";
            }
        }

        brainsToLoad[brainsToLoad.Length - 1] = mostFitBrain;//The fittest brain is always at last index
    }

    //Child Generator: Generates children based on 2 parents and adds a mutation
    string ChildGen(string parent1, string parent2)
    {
        string ret = "";
        string[] brain1Buf = parent1.Split(';');
        string[] brain2Buf = parent2.Split(';');

        for (int i = 0; i < brain1Buf.Length; i++)
        {
            float mutationChance = UnityEngine.Random.Range(0f, 1f);
            float swapGenes = UnityEngine.Random.Range(0, 1);

            if (mutationChance > minMutationChance)
            {
                ret += (swapGenes == 1) ? brain2Buf[i] : brain1Buf[i] + ";";
            }
            else
            {
                //Debug.LogWarning("ChildGen p1: " + parent1 + " p2: " + parent2);
                float retN = (swapGenes == 1) ? float.Parse(brain2Buf[i]) : float.Parse(brain1Buf[i]);
                retN += UnityEngine.Random.Range(-mutationRange, mutationRange);

                ret += retN.ToString() + ";";
            }
        }

        //ret = ret.TrimEnd(';');
        return ret;
    }
        
    Car_Behaviour InstantiateCar(Vector3 spawnPoint,string brain,string name,NN.TYPE brainType)
    {
        Car_Behaviour behaviour = Instantiate(carPrefab,spawnPoint,Quaternion.identity).GetComponent<Car_Behaviour>();

        behaviour.active = true;
        behaviour.AIControlled = true;

        behaviour.transform.name = name;
        behaviour.objective = trackCtrl.trackEnd.position;
        //behaviour.type = brainType;

        if (brain!=null)
            behaviour.ActivateBrain(brain);
        else
            behaviour.ActivateBrain();        

        return behaviour;
    }

    Car_Behaviour InstantiateCar(Vector3 spawnPoint, float[][] hlWeights,float[] hlBiases, float[] OWeights, float OBiases, string name)
    {
        Car_Behaviour behaviour = Instantiate(carPrefab, spawnPoint, Quaternion.identity).GetComponent<Car_Behaviour>();

        behaviour.active = true;
        behaviour.AIControlled = true;

        if (hlWeights != null || hlBiases != null)
            behaviour.ActivateBrain(hlWeights, hlBiases, OWeights, OBiases);
        else
            behaviour.ActivateBrain();

        behaviour.transform.name = name;
        behaviour.objective = trackCtrl.trackEnd.position;

        return behaviour;
    }

    Car_Behaviour InstantiateCar(Vector3 spawnPoint, NN nN, string name)
    {
        Car_Behaviour behaviour = Instantiate(carPrefab, spawnPoint, Quaternion.identity).GetComponent<Car_Behaviour>();
        
        if (nN != null)
            behaviour.ActivateBrain(nN);
        else
            behaviour.ActivateBrain();

        behaviour.transform.name = name;
        behaviour.objective = trackCtrl.trackEnd.position;

        behaviour.active = true;
        behaviour.AIControlled = true;

        return behaviour;
    }

    public Car_Behaviour GetCarByNN(NN neuralNet)
    {
        Car_Behaviour[] cars = FindObjectsOfType<Car_Behaviour>();

        for(int i = 0; i < cars.Length; i++)
        {
            if(cars[i].GetNN() == neuralNet)
            {
                return cars[i];
            }
        }

        return null;
    }

    public Car_Behaviour GetMostFitCar()
    {
        return (spawnedCars.Count<=0)?null: spawnedCars[0];
    }

    //Logging
    void LogGenerationIntoFile()
    {        
        logCtrl.LogLine(string.Concat("Generation: ", generation, " | maxFitness: ", maxFitness, " | maxFitnessGen: ", fitLastGen, " | maxFitnessGenBuf: ", fitCurGen,"\n",
                                    "N TotallyRandom: ",totallyRandomCount," | N RandomParents: ",randomParentsCount," | N Top2: ",Top2Count, " | Min Mutation: ",minMutationChance, "\n", "\n",
                                    "Index|Brain Type|Fitness|BrainData"));

        for (int i =0; i < spawnedCars.Count; i++)
        {
            NN neuralNet = spawnedCars[i].GetNN();
            logCtrl.LogLine(string.Concat(i,"|", neuralNet.type, "|", neuralNet.GetFitness(),  "|", neuralNet.ExtractBrain()));
        }    
    }
    
}
