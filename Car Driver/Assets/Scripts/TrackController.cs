using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class TrackController : MonoBehaviour
{
    public List<string> loadedMaps;
    public Vector3 spawnPoint = Vector3.zero;
    public bool random = false;
    
    public Vector2 mapSize = new Vector2(30, 30);
    public int tileSize = 30;

    int cMapIndex = 0;
    int[][] cMap;
    GameObject[][] cMapObjs;
    int cMapPiecesQuant = 0;
    public Transform trackEnd;
    public Transform trackStart;

    public string[] allMapFiles;

    //Pieces
    public GameObject[] roadPieces;

    // Start is called before the first frame update
    void Start()
    {
        allMapFiles = Directory.GetFiles("Maps\\");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BuildNextMap()
    {
        GameObject[] lastTrack = GameObject.FindGameObjectsWithTag("Road");
        if (lastTrack.Length > 0)
        {
            for(int i = 0; i < lastTrack.Length; i++)
            {
                Destroy(lastTrack[i]);
            }
        }

        int mapIndex = cMapIndex;

        if (random  && loadedMaps.Count>1)
        {
            mapIndex = Random.Range(0, loadedMaps.Count - 1);
            //if (mapIndex == cMapIndex) return;
        }

        trackStart = null;

        Debug.Log("BuildNextMap: " + mapIndex +"/"+ loadedMaps.Count + " random = " + random);
        cMap = DecodeMap(loadedMaps[mapIndex]);
        
        BuildMap(cMap);

        cMapIndex = (cMapIndex < loadedMaps.Count-1) ? cMapIndex + 1 : 0;
    }

    int[][] DecodeMap(string map)
    {
        Debug.Log("Decode Map " + map);
        int[][] ret = new int[Mathf.FloorToInt(mapSize.y)][];

        if (map.Length < mapSize.x * mapSize.y)
        {
            Debug.LogError("Wrong map size!! mapsize: " + (mapSize.x * mapSize.y) + "string size: " + map.Length);
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
    
    void BuildMap(int[][] map)
    {
        cMapPiecesQuant = 0;

        for (int i = 0; i < map.Length; i++)
        {
            for (int j = 0; j < map[i].Length; j++)
            {
                //Debug.Log("BuildMap ij:" + i +" " + j + " | " + map[i][j] + " / " + map[i].Length);

                if (map[i][j] > 0)
                {
                    Vector3 position = new Vector3(j * tileSize, 0, i * tileSize);
                    //Debug.Log(map[i][j] - 1);
                    GameObject obj = Instantiate(roadPieces[map[i][j] - 1], position, Quaternion.identity);
                    obj.name = cMapPiecesQuant.ToString();
                    trackEnd = obj.transform;

                    cMapPiecesQuant++;

                    if (trackStart == null)
                        trackStart = obj.transform;
                }
            }
        }
    }

    public string LoadMap(string path)
    {
        string ret = "";

        StreamReader file = new StreamReader(path);
        ret = file.ReadToEnd();

        Debug.Log("File: " + path + " loaded!");
        file.Close();

        return ret;
    }

    //Usado para determinar o fitness, retorna a porcentagem que um objeto viajou pela estrada(NÃO USADO)
    public float PercentageTraveled(Transform individual)
    {
        float ret = 0;
        RaycastHit hit;
        int trackNumber = 0;

        if (Physics.Raycast(individual.position, -individual.up, out hit))
        {
            if(hit.collider != individual.GetComponent<Collider>() && hit.transform.root.CompareTag("Road"))
            {
                trackNumber = int.Parse(hit.transform.root.name);                
            }
        }
        else
        {
            ret = 0;
        }

        ret = trackNumber/ cMapPiecesQuant;
        Debug.LogWarning(individual.name + " ret%: " + ret + " piecesQ: " + cMapPiecesQuant + " trackN: " + trackNumber);
        return ret;
    }
}
