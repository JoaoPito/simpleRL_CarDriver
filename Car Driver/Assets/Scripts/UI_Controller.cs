using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UI_Controller : MonoBehaviour
{

    GameController gameCtrl;
    TrackController trackCtrl;

    bool paused = false;

    Car_Behaviour mostFitCar;

    [Header("Cameras")]
    public UnityStandardAssets.Cameras.FreeLookCam mainCam;
    public Camera topdownCam;    

    [Header("Main Menu")]
    public Canvas MainMenuCanvas;

    [Header("Map Selection")]
    public GameObject mapContent;
    public Transform mapSVContent;
    public float mapContentSpacing = 40;

    public Dropdown mapSeqMode;
    public Slider minGenSlider;
    public Text minGenText;

    [Header("In Action")]
    public Canvas GameplayCanvas;

    public Text statsText;
    public Slider outputSlider;

    public Slider timeSlider;
    public Text timeText;

    //Environment Control
    public Slider RPCountSlider;
    public Slider TRCountSlider;
    public Slider TOPCountSlider;

    public Slider mutChanceSlider;
    
    public Text maxTimetext;
    public Slider maxTimeSlider;

    public Text RPCountText;
    public Text TRCountText;
    public Text TOPCountText;

    public Text mutChanceText;

    // Start is called before the first frame update
    void Awake()
    {
        gameCtrl = GetComponent<GameController>();
        trackCtrl = GetComponent<TrackController>();
    }

    // Update is called once per frame
    void Start()
    {
        GameplayCanvas.gameObject.SetActive(false);
        MainMenuCanvas.gameObject.SetActive(true);

        timeText.text = "Velocidade do tempo: 100%";

        RPCountSlider.value = gameCtrl.randomParentsCount;
        TRCountSlider.value = gameCtrl.totallyRandomCount;
        TOPCountSlider.value = gameCtrl.Top2Count;

        mutChanceSlider.value = gameCtrl.minMutationChance*100;

        RPCountChange();
        TRCountChange();
        TOPCountChange();
        MutChanceChange();

        DrawMapList();
    }

    private void Update()
    {
        mostFitCar = gameCtrl.GetMostFitCar();
        if (Input.GetKeyUp(KeyCode.C) && mostFitCar != null)
        {
            if(mostFitCar != null)
                mainCam.SetTarget(mostFitCar.transform);
        }

        /*if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Tab))
        {
            mainCam.enabled = !mainCam.enabled;
        }*/

        
            if (mostFitCar != null)
            {
                outputSlider.value = mostFitCar.steering;
                topdownCam.transform.position = new Vector3(mostFitCar.transform.position.x, 10, mostFitCar.transform.position.z);
            }
    }

    public void UpdateGamePlayUI()
    {
        float TimeLeft = gameCtrl.maxTimeout - gameCtrl.currentGenTime;

        statsText.text = string.Concat("Geração: ", gameCtrl.generation, "\n",
                         "Max Fitness: ", gameCtrl.maxFitness, "\n",
                         "Fitness Ult. Geração: ", gameCtrl.fitLastGen, "\n",
                         "Fitness Geração: ", (mostFitCar != null) ? mostFitCar.GetNN().GetFitness().ToString() : "", "\n",
                         "Carros Restantes: ", gameCtrl.nActiveCars , "\n" ,
                         "Tempo Restante(seg): ", TimeLeft.ToString());
    }

    public void UpdateMapUI()
    {
        Toggle[] toggles = mapSVContent.GetComponentsInChildren<Toggle>();
        trackCtrl.loadedMaps = new List<string>();

        ChangeMinGenerations();

        for (int i = 0; i < toggles.Length; i++)
        {
            if (toggles[i].isOn)
            {
                string loadedMap = trackCtrl.LoadMap(trackCtrl.allMapFiles[i]);
                trackCtrl.loadedMaps.Add(loadedMap);
                //Debug.Log(trackCtrl.allMapFiles[i] + " Added");
            }            
        }
    }

    public void MaxTimeChange()
    {
        gameCtrl.maxTimeout = maxTimeSlider.value;

        maxTimetext.text = string.Concat("Tempo Máx.(seg): ", maxTimeSlider.value.ToString());
    }

    //Map Select Functions
    void DrawMapList()
    {
        int mapQuant = trackCtrl.allMapFiles.Length;
        Transform contObj;

        for(int i = 0; i < mapQuant; i++)
        {            
            contObj = Instantiate(mapContent,Vector3.zero,Quaternion.identity).transform;
            contObj.SetParent(mapSVContent);

            Vector3 pos = new Vector3(0,(i * -mapContentSpacing) - 20,0);
            contObj.localPosition = pos;

            mapSVContent.GetComponent<RectTransform>().sizeDelta = new Vector2(mapSVContent.GetComponent<RectTransform>().sizeDelta.x, i * mapContentSpacing);

            contObj.Find("Label").GetComponent<Text>().text = trackCtrl.allMapFiles[i];
            contObj.GetComponent<Toggle>().onValueChanged.AddListener(delegate { UpdateMapUI(); });
        }
    }
    
    public void MapSelection(int mapN)
    {
        Debug.Log(mapN);
        trackCtrl.loadedMaps.Add(trackCtrl.allMapFiles[mapN]);
    }

    public void ChangeMapMode()
    {
        if (mapSeqMode.value == 0)//Random
        {
            trackCtrl.random = true;
        }
        else//Seq
        {
            trackCtrl.random = false;
        }
    }

    public void ChangeMinGenerations()
    {
        gameCtrl.minGenOnTrack = Mathf.RoundToInt(minGenSlider.value);
        minGenText.text = string.Concat("Nº min de ger. por mapa: " + minGenSlider.value.ToString());
    }

    //Main Menu Button Functions
    public void StartGameBut()
    {
        MainMenuCanvas.gameObject.SetActive(false);
        gameCtrl.StartGen();

        GameplayCanvas.gameObject.SetActive(true);
    }

    public void TimeSpeed()
    {
        Time.timeScale = timeSlider.value/100;
        timeText.text = string.Concat("Velocidade do tempo: ", timeSlider.value,"%");
    }

    public void RPCountChange()
    {
        gameCtrl.randomParentsCount = Mathf.RoundToInt(RPCountSlider.value);
        RPCountText.text = string.Concat("Pais Aleatórios: ", Mathf.RoundToInt(RPCountSlider.value).ToString());
    }

    public void TRCountChange()
    {
        gameCtrl.totallyRandomCount = Mathf.RoundToInt(TRCountSlider.value);
        TRCountText.text = string.Concat("Totalmente avulso: ", Mathf.RoundToInt(TRCountSlider.value).ToString());
    }

    public void TOPCountChange()
    {
        gameCtrl.Top2Count = Mathf.RoundToInt(TOPCountSlider.value);
        TOPCountText.text = string.Concat("Pais tops: ", Mathf.RoundToInt(TOPCountSlider.value).ToString());
    }

    public void MutChanceChange()
    {
        gameCtrl.minMutationChance = mutChanceSlider.value/100;
        mutChanceText.text = string.Concat("Chance mutação: ", Mathf.RoundToInt(mutChanceSlider.value).ToString(),"%");
    }

    public void MapBuilderBut()
    {
        SceneManager.LoadScene(1);
    }

    //Pause Menu
    public void NextGen()
    {
        gameCtrl.EndGen();
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(0);
    }

    private void OnDestroy()
    {
        
    }

}
