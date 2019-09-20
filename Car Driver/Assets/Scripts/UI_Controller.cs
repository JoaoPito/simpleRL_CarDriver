using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Controller : MonoBehaviour
{

    GameController gameCtrl;

    [Header("Main Menu")]
    public Canvas MainMenuCanvas;


    [Header("In Action")]
    public Canvas GameplayCanvas;

    public Text statsText;
    public Slider outputSlider;
    
    bool paused = false;

    [Header("Cameras")]
    public UnityStandardAssets.Cameras.FreeLookCam mainCam;

    public Camera topdownCam;

    public Slider timeSlider;
    public Text timeText;

    //Environment Control
    public Slider RPCountSlider;
    public Slider TRCountSlider;
    public Slider TOPCountSlider;

    public Slider mutChanceSlider;

    public InputField maxTimeInput;

    public Text RPCountText;
    public Text TRCountText;
    public Text TOPCountText;

    public Text mutChanceText;

    public Text maxTimetext;

    // Start is called before the first frame update
    void Awake()
    {
        gameCtrl = GetComponent<GameController>();
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
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.C) && gameCtrl.sortedSpawnedCars.Count > 0)
        {
            if(gameCtrl.sortedSpawnedCars[0] != null)
                mainCam.SetTarget(gameCtrl.sortedSpawnedCars[0].transform);
        }

        if (Input.GetMouseButtonDown(1))
        {
            mainCam.enabled = !mainCam.enabled;
        }

        if (gameCtrl.sortedSpawnedCars.Count > 0)
        {
            if (gameCtrl.sortedSpawnedCars[0] != null)
            {
                outputSlider.value = gameCtrl.sortedSpawnedCars[0].steering;
                topdownCam.transform.position = new Vector3(gameCtrl.sortedSpawnedCars[0].transform.position.x, 10, gameCtrl.sortedSpawnedCars[0].transform.position.z);
            }
        }
    }

    public void UpdateGamePlayUI()
    {
        float TimeLeft = gameCtrl.maxTimeout - gameCtrl.currentGenTime;

        statsText.text = string.Concat("Geração: ", gameCtrl.generation, "\n",
                         "Max Fitness: ", gameCtrl.maxFitness, "\n",
                         "Fitness Ult. Geração: ", gameCtrl.fitLastGen, "\n",
                         "Fitness Geração: ", (gameCtrl.sortedSpawnedCars.Count > 0) ? gameCtrl.sortedSpawnedCars[0].currentFitness.ToString() : "", "\n",
                         "Carros Restantes: ", gameCtrl.nActiveCars , "\n" ,
                         "Tempo Restante(seg): ", TimeLeft.ToString());
    }

    //Main Menu Button Functions
    public void StartGameBut()
    {
        MainMenuCanvas.gameObject.SetActive(false);
        gameCtrl.StartGame();

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


}
