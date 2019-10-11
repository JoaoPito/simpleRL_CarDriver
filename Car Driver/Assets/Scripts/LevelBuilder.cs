using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using System;
using System.IO;

public class LevelBuilder : MonoBehaviour
{

    public enum BUILDMODE
    {
        NULL,
        BUILD,
        SAVELOAD,
        TEST
    }

    public bool active = false;
    public BUILDMODE buildMode = BUILDMODE.NULL;
    int[][] currentMap;//0 is none;1 is straight fwd;2 is straight side;3 is left turn;4 is right turn;5 is 4way
    Transform[][] cMapPieces;    
    public Vector2 mapSize = new Vector2(30, 30);

    [Header("Tiles")]
    Vector2 startTile;
    int tilesCount = 0;
    public int tileSize = 30;

    public GameObject[] roadPieces;

    /*int currentHeight = 0;
    public int heightSteps = 2;*/

    //***UI***
    public InputField mapSaveInput;
    public InputField mapLoadInput;
    public Text statusText;

    void Start()
    {
        buildMode = BUILDMODE.BUILD;

        StartBuildMode();
    }

    // Update is called once per frame
    void Update()
    {
        if (active)
        {
            switch (buildMode)
            {
                case BUILDMODE.BUILD:
                    
                    if (Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        ChangeTile(true);
                    }

                    if (Input.GetKeyDown(KeyCode.Mouse1))
                    {
                        ChangeTile(false);
                    }

                    break;

                case BUILDMODE.TEST:

                    break;
            }
        }
    }

    //Build Mode
    void StartBuildMode()
    {
        currentMap = new int[Mathf.RoundToInt(mapSize.y)][];
        cMapPieces = new Transform[currentMap.Length][];

        for (int i = 0; i < currentMap.Length; i++)
        {
            currentMap[i] = new int[Mathf.RoundToInt(mapSize.x)];
            cMapPieces[i] = new Transform[currentMap[i].Length];
        }
        
        DrawMapBlueprint();
        active = true;
    }

    void DrawMapBlueprint()
    {
        for(int i = 0; i < currentMap.Length; i++)
        {
            for (int j = 0; j < currentMap[i].Length; j++)
            {
                Vector3 pos = new Vector3(j * tileSize, 0,i * tileSize);

                GameObject obj = Instantiate(roadPieces[0], pos, Quaternion.identity);
                obj.name = i.ToString() + "/" + j.ToString();

                currentMap[i][j] = 0;
            }
        }
    }

    void ChangeTile(bool Incr)
    {
        //creates a raycast to the what kind of tile is under mouse pos
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform.root.CompareTag("BuildModeIndic") || hit.transform.root.CompareTag("Road"))//If its the indicator, creates a new road tile
            {
                int iX = Mathf.RoundToInt(hit.transform.root.position.x / tileSize);
                int iY = Mathf.RoundToInt(hit.transform.root.position.z / tileSize);

                if (Incr)
                {
                    currentMap[iY][iX] = (currentMap[iY][iX] < roadPieces.Length - 1) ? currentMap[iY][iX] + 1 : 0;
                }
                else
                {
                    currentMap[iY][iX] = (currentMap[iY][iX] > 0) ? currentMap[iY][iX] - 1 : roadPieces.Length - 1;
                }
                
                GameObject obj  = Instantiate(roadPieces[currentMap[iY][iX]], new Vector3(iX* tileSize,0, iY* tileSize), Quaternion.identity);
                obj.name = iY.ToString() + "/" + iX.ToString();

                Destroy(hit.transform.root.gameObject);

                if (currentMap[iY][iX] != 0)
                {
                    if (tilesCount == 0)
                        startTile = new Vector2(iX, iY);

                    tilesCount++;
                }
            }
        }
       
    }

    void ChangeTile(int iX, int iY, int tile)
    {
        Vector3 position = new Vector3(iX * tileSize, 0, iY * tileSize);
        Transform lastObj = GameObject.Find(iY.ToString() + "/" + iX.ToString()).transform.root;
        
        if (lastObj.CompareTag("BuildModeIndic") || lastObj.CompareTag("Road"))//If its the indicator, creates a new road tile
        {
            Destroy(lastObj.gameObject);

            GameObject obj = Instantiate(roadPieces[tile], position, Quaternion.identity);
            obj.name = iY.ToString() + "/" + iX.ToString();

            if (tile != 0)
            {
                if (tilesCount == 0)
                    startTile = new Vector2(iX, iY);

                tilesCount++;
            }
        }

    }

    void PlaceTrackPiece(int indexX, int indexY, int PieceType, GameObject lastObj)
    {
        Debug.Log(PieceType + lastObj.name);

        Vector3 pos = new Vector3(indexX * tileSize, 0, indexY * tileSize);
        Vector3 rotEuler = Vector3.zero;

        Destroy(lastObj);

        switch (PieceType)
        {
            case 0://None
                rotEuler = Vector3.zero;
                return;

            case 1://Straight forward
                rotEuler = Vector3.zero;
                return;

            case 2://Straight sideways
                rotEuler.y = 90;
                return;

            case 3://Left turn
                rotEuler = Vector3.zero;
                return;

            case 4://Right turn
                rotEuler.y = 90;
                return;

            case 5://4 Way
                rotEuler = Vector3.zero;
                return;
        }

        

    }

    void RedrawMap()
    {
        //ClearMap();

        GameObject[] tiles = GameObject.FindGameObjectsWithTag("Road");

        foreach (GameObject obj in tiles)
        {
            Destroy(obj);
        }


        tilesCount = 0;

        for (int i = 0; i < currentMap.Length; i++)
        {
            for(int j = 0; j < currentMap[i].Length; j++)
            {
                //Debug.Log("Redrawing: " + i + j + " tile n: " + currentMap[i][j]);
                ChangeTile(j, i, currentMap[i][j]);
            }
        }
    }

    public void ClearMap()
    {
        tilesCount = 0;

        for (int i = 0; i < currentMap.Length; i++)
        {
            for (int j = 0; j < currentMap[i].Length; j++)
            {
                currentMap[i][j] = 0;
                //Debug.Log("Redrawing: " + i + j + "tile n: " + currentMap[i][j]);
                ChangeTile(j, i, currentMap[i][j]);
            }
        }
    }
    

    //Save/Load Maps
    public void LoadMap()
    {
        LoadMap(mapLoadInput.text);
    }

    public void LoadMap(string mapName)
    {
        mapName = mapName.ToUpper();
        string fn = "Maps\\" + mapName + ".m";

        StreamReader file = null;

        try
        {
            file = new StreamReader(fn);
        }catch
        {
            statusText.text = "File not Found!";
            return;
        }
        
        string map = file.ReadToEnd();

        currentMap = DecodeMap(map);

        statusText.text = "File: " + fn + " loaded!";
        Debug.Log("File: " + fn + " loaded! cont: " + EncodeMap());
        file.Close();

        RedrawMap();
    }
    
    public void SaveMap()
    {
        SaveMap(mapSaveInput.text);
    }

    public void SaveMap(string mapName)
    {
        mapName = mapName.ToUpper();
        string fn = "Maps\\" + mapName + ".m";

        if (File.Exists(fn))
        {
            Debug.LogWarning("Map already exists!");
            return;
        }
        StreamWriter sw = File.CreateText(fn);

        sw.WriteLine(EncodeMap());
        
        sw.Close();
        Debug.Log("Map saved to: " + fn);
        statusText.text = "Map saved to: " + fn;
    }

    string EncodeMap()
    {
        string retStr = "";

        for(int i =0;i < currentMap.Length; i++)
        {
            for(int j = 0;j < currentMap[i].Length; j++)
            {
                retStr += currentMap[i][j].ToString();
                retStr += ';';
            }
        }

        retStr.TrimEnd(';');

        return retStr;
    }

    int[][] DecodeMap(string map)
    {
        int[][] ret = new int[Mathf.FloorToInt(mapSize.y)][];

        if (map.Length < mapSize.x*mapSize.y)
        {
            Debug.LogError("Wrong map size!! mapsize: " + (mapSize.x * mapSize.y) + "string size: " + map.Length);
            statusText.text = "Wrong map size!!, choose another map!";
        }
        else
        {
            string[] strContent = map.Split(';');

            int a = 0;
            for (int i = 0; i < mapSize.y; i++)
            {
                ret[i] = new int[Mathf.FloorToInt(mapSize.x)];

                for (int j = 0; j < mapSize.x; j++)
                {
                    ret[i][j] = int.Parse(strContent[a]);
                    a++;
                }
            }
        }
        //Debug.Log("DecodeMap: " + map);
        return ret;
    }
    
    //UI
    public void SetEditingActive(bool act)
    {
        active = act;
    }

    public void BackButton()
    {
        SceneManager.LoadScene(0);
    }
}
