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
        TRAIN,
        RESULTS
    }

    MENU currentMenu = MENU.MAINMENU;

    TrackController trackCtrl;
    UI_Controller uiCtrl;   

    [Header("Cars")]    
    string[] brainsToLoad;//This brains should be already sorted by fitness and ready for "implant" when StartGame is called
    string mostFitBrain; // Most fit brain of all generations
    [HideInInspector] public float maxFitness;
    [HideInInspector] public float fitLastGen;
    [HideInInspector] public float fitCurGen;

    public GameObject carPrefab;
    public Vector3 spawnPoint;

    [Header("Generations")]    
    public bool logGenerations = true;//Logs all brains and fitnesses into a file
    [HideInInspector] public int generation;

    public float maxTimeout = 120f;//Max Timeout in seconds
    [HideInInspector] public float currentGenTime = -1;
   
    [HideInInspector] public int nActiveCars = 1;
    int carsReachedObjective = 0;//Essa variavel é incrementada mas não tem utilidade por enquanto
    List<Car_Behaviour> spawnedCars = new List<Car_Behaviour>();
    public List<Car_Behaviour> sortedSpawnedCars;

    //Quantidade de carros por geração
    [Header("Cars per Gen")]
    [Range(0, 100)] public int randomParentsCount = 10;
    [Range(1, 100)] public int totallyRandomCount = 10;
    [Range(0, 100)] public int Top2Count = 10;    
    int carsPerGen;

    [Header("Parents")]
    [Range(0, 1)] public float minMutationChance = 0.5f;//Its the percentage of random parents selected for the next breed
    [Range(0, 5)] public float mutationRange = 0.1f;

    //LOGGING
    StreamWriter sw;
    string filename = string.Concat(DateTime.Now.Day, "-", DateTime.Now.Month, "-", DateTime.Now.Year, "-", DateTime.Now.Hour, "_", DateTime.Now.Minute, " NN Log");

    //UI
    public float updateUIInterval = 0.3f;
    float cUIInterval = 0;

    // Start is called before the first frame update
    void Awake()
    {
        trackCtrl = GetComponent<TrackController>();
        uiCtrl = GetComponent<UI_Controller>();

        brainsToLoad = new string[randomParentsCount + totallyRandomCount + Top2Count];
    }

    // Update is called once per frame
    void Update()
    {
        if (currentMenu == MENU.TRAIN)
        {            
            if ((currentGenTime >= maxTimeout) || nActiveCars <= 0)
            {
                EndGame();
            }else if (currentGenTime >= 0)
            {
                currentGenTime += Time.fixedDeltaTime;
            }

        }
        cUIInterval += Time.fixedUnscaledDeltaTime;

        if (cUIInterval > updateUIInterval)
        {
            sortedSpawnedCars = SortFitness();
            uiCtrl.UpdateGamePlayUI();
            cUIInterval = 0;
        }
        
    }

    public void StartGame()
    {
        if(generation==0)
            trackCtrl.InstantiateTrack(1);

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
            Car_Behaviour.BrainType brainType = Car_Behaviour.BrainType.RandomParents;
            
            if (i < randomParentsCount)//Random Parents Generation
            {
                brainType = Car_Behaviour.BrainType.RandomParents;
            }
            else if (i < randomParentsCount + totallyRandomCount)//Totally Random Generation
            {
                brainType = Car_Behaviour.BrainType.TotallyRandom;
            }
            else if (i < randomParentsCount + totallyRandomCount + Top2Count)//Top2 Generation
            {
                brainType = Car_Behaviour.BrainType.TOP2;
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

        if (car.currentFitness > fitCurGen)
        {
            fitCurGen = car.currentFitness;
        }

        if (endReached)
        {
            carsReachedObjective++;
        }
    }

    public void EndGame()
    {
        currentGenTime = -1;
        
        spawnedCars = SortFitness();

        if (fitCurGen > maxFitness)
        {
            mostFitBrain = spawnedCars[0].ExtractBrain();
            Debug.Log("mostfitbrain=" + mostFitBrain + "fitness: "+ fitCurGen);
            maxFitness = fitCurGen;
        }

        LogGenerationIntoFile();
        uiCtrl.UpdateGamePlayUI();

        //Begin Next Generation
        
        NextGen(spawnedCars);
        StartGame();
    }
    
    //Controls the generation of the next children
    void NextGen(List<Car_Behaviour> lastGen)
    {
        carsPerGen = randomParentsCount + totallyRandomCount + Top2Count + 1;//Number of all cars count plus the best fit car

        brainsToLoad = new string[carsPerGen];

        //Generates by random parents
        for(int i = 1;i< brainsToLoad.Length-1;i++)
        {
            if(i< randomParentsCount)//Random Parents Generation
            {
                int parent1 = UnityEngine.Random.Range(0, lastGen.Count);
                int parent2 = UnityEngine.Random.Range(0, lastGen.Count);

                brainsToLoad[i] = ChildGen(lastGen[parent1].ExtractBrain(), lastGen[parent2].ExtractBrain());
            }
            else if(i < randomParentsCount + totallyRandomCount)//Totally Random Generation
            {
                brainsToLoad[i] = ""; //When a null brain is loaded, it automatically creates a random one
            }
            else if(i < randomParentsCount + totallyRandomCount + Top2Count)//Top2 Generation
            {
                brainsToLoad[i] = ChildGen(lastGen[0].ExtractBrain(), lastGen[1].ExtractBrain());
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
                ret += (swapGenes == 1) ? brain2Buf[i] : brain1Buf[i]+";";
            }
            else
            {
                //Debug.LogWarning("ChildGen p1: " + parent1 + " p2: " + parent2);
                float retN = (swapGenes == 1) ? float.Parse(brain2Buf[i]) : float.Parse(brain1Buf[i]);
                retN += UnityEngine.Random.Range(-mutationRange, mutationRange);

                ret += retN.ToString() +";";
            }
        }
        
        ret = ret.TrimEnd(';');
        return ret;
    }
    
    Car_Behaviour InstantiateCar(Vector3 spawnPoint,string brain,string name,Car_Behaviour.BrainType brainType)
    {
        Car_Behaviour behaviour = Instantiate(carPrefab,spawnPoint,Quaternion.identity).GetComponent<Car_Behaviour>();

        behaviour.active = true;
        behaviour.AIControlled = true;

        behaviour.transform.name = name;
        behaviour.objective = trackCtrl.trackEnd.position;
        behaviour.brainType = brainType;

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

    List<Car_Behaviour> SortFitness()
    {
        List<Car_Behaviour> sortedList = spawnedCars.OrderByDescending(o => o.currentFitness).ToList();

        return sortedList;
    }

    //Logging
    void LogGenerationIntoFile()
    {
        string[] brainsRegistered = brainsToLoad;

        StreamWriter sw;
        string fn = "NNLogs\\"+ filename + '_' + generation + ".txt";

        if (File.Exists(fn))
        {
            Debug.LogWarning("Log File already exists!");
            return ;
        }
        sw = File.CreateText(fn);

        Debug.Log("File saved to: " + fn);

        sw.WriteLine(string.Concat("Generation: ", generation, " | maxFitness: ", maxFitness, " | maxFitnessGen: ", fitLastGen, " | maxFitnessGenBuf: ", fitCurGen,"\n",
                                    "N TotallyRandom: ",totallyRandomCount," | N RandomParents: ",randomParentsCount," | N Top2: ",Top2Count, " | Min Mutation: ",minMutationChance, "\n", "\n",
                                    "Index|Brain Type|Fitness|BrainData"));



        for (int i =0; i < spawnedCars.Count; i++)
        {
            sw.WriteLine(string.Concat(i,"|", spawnedCars[i].brainType, "|", spawnedCars[i].currentFitness,  "|", spawnedCars[i].ExtractBrain()));
        }
        
        //sw.Close();        
    }
    
}
