using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackController : MonoBehaviour
{
    //BETA VERSION
    public GameObject[] spawnableTrackPieces;
    public Vector3 spawnPoint = Vector3.zero;

   // public GameObject[] generatedTrack;
    GameObject currentTrack;
    public Transform trackEnd;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //Instantiate a track on a index of spawnableTrackPieces, if trackNumber==0 instantiate randomly
    public void InstantiateTrack(int trackNumber)
    {
        if (currentTrack != null)
            Destroy(currentTrack);

        if (trackNumber == 0)
        {
            currentTrack = Instantiate(spawnableTrackPieces[Random.Range(0,spawnableTrackPieces.Length-1)], spawnPoint, Quaternion.identity);
        }
        else
        {
            currentTrack = Instantiate(spawnableTrackPieces[trackNumber-1], spawnPoint, Quaternion.identity);
        }

        trackEnd = currentTrack.transform.GetChild(currentTrack.transform.childCount - 1);
        trackEnd = trackEnd.GetChild(trackEnd.childCount - 1);
    }

    //Usado para determinar o fitness, retorna a porcentagem que um objeto viajou pela estrada(NÃO USADO)
    public float PercentageTraveled(Transform individual)
    {
        float ret = 0;
        RaycastHit hit;
        int trackNumber = 0;

        if (Physics.Raycast(individual.position, -individual.up, out hit))
        {
            if(hit.collider != individual.GetComponent<Collider>() && hit.collider.tag == "Road")
            {
                trackNumber = int.Parse(hit.collider.name);
                
            }
        }
        else
        {
            ret = -1;
        }

        ret = trackNumber/currentTrack.transform.childCount;
        //Debug.Log("ret%: " + trackNumber);
        return ret;
    }

    public float DistancePercent(Vector3 pos)
    {
        float ret = 0;

        return ret;
    }

    /* public void GenerateTrack(int minLength, int maxLength)
     {
         int totalLength = Random.Range(minLength, maxLength);


     }*/
}
