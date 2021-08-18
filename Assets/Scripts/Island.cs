using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Island : MonoBehaviour
{
    public GameObject TextIndicator;
    public float islandRadius;
    private int islandNumber;
    //boolean representing whether the island is being pointed at or not
    private bool CursorOn = false;
    //Tells whether new Data that needs to be written to CSV is waiting to be transmitted
    private bool ready=false;
    private List<Data> rankedMP = new List<Data>();
    private List<(int,float, float)> detailedUtility = new List<(int,float, float)>();
    private MeshCollider islandArea;
    private float colliderRadius = 0.01f;
    DisplayMusicalPatterns Displayer;

    // Start is called before the first frame update
    void Start()
    {
        Displayer = FindObjectOfType<DisplayMusicalPatterns>();
        //The collider representing the surface of the island.
        islandArea = GetComponentInChildren<MeshCollider>();
        //The amount we need to scale our sensing collider so that it has a radius of sensingRadius
        float upscale = islandRadius / colliderRadius;
        islandArea.gameObject.transform.localScale = new Vector3(upscale, upscale, islandArea.gameObject.transform.localScale.z);   
    }

    // Update is called once per frame
    void Update()
    {
        TextIndicator.GetComponent<TextMesh>().text = islandNumber.ToString();
        this.transform.LookAt(Vector3.ProjectOnPlane(Camera.main.transform.position,Vector3.up));
    }

    void OnMouseEnter()
    {
        //decides to show the musical patterns of the island if the mouse cursor is pointed at the island
        CursorOn = true;
        Displayer.showMusicalPatterns(this);
    }

    void OnMouseExit()
    {
        //hides the musical patterns of the island if the mouse cursor is not pointing the island
        CursorOn = false;
        foreach(Canvas i in GameObject.FindObjectsOfType<Canvas>())
        {
            Destroy(i.gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        //if the body of an agent traveling to this island enters the island's range it stops the journey of the agent 
        if (other.transform.root.gameObject.tag == "MusicalAgent" && !other.isTrigger && other.transform.root.gameObject.GetComponent<MusicalAgent>().getIslandNumber()==islandNumber)
        {
            Debug.Log("agent " + other.transform.root.gameObject.GetComponent<MusicalAgent>().getAgentNumber().ToString() + " entered island " + islandNumber.ToString());
            //This verification is put in place because when agents are instantiated at the beginning of each iteration they trigger this collider and justArrived should only be set if an agent arrives to a new Island
            if(other.transform.root.gameObject.GetComponent<MusicalAgent>().getIsTraveling())
            {
                other.transform.root.gameObject.GetComponent<MusicalAgent>().setJustArrived(true);
            }
            other.transform.root.gameObject.GetComponent<MusicalAgent>().setIsTraveling(false);
            other.transform.root.gameObject.GetComponent<CustomNavMeshAgent>().ResetPath();
        }
    }
    /// <summary>
    /// Adds the musical patterns of the island's agent ranked by the leader
    /// </summary>
    /// <param name="agentrankedMP">A list containing the utility, agent number, and musical patterns of the agents of the island ranked by utility (agentrankedMP[0] being the best).</param>
    public void updateRankedMP(List<Data> agentrankedMP,List<(int,float,float)> agentdetailedUtility)
    {
        rankedMP.Clear();
        detailedUtility.Clear();
        foreach(Data element in agentrankedMP)
        {
            rankedMP.Add(element.Clone());
        }
        foreach((int,float,float) element in agentdetailedUtility)
        {
            detailedUtility.Add(element);
        }
        ready = true;
        Debug.Log(islandNumber);
    }

    public void ResetIsle()
    {
        rankedMP.Clear();
        detailedUtility.Clear();
    }

    //SETTERS AND GETTERS

    public void setIslandNumber(int number)
    {
        islandNumber = number;
    }

    public void setReady(bool state)
    {
        ready = state;
    }

    public int getIslandNumber()
    {
        return islandNumber;
    }

    public List<Data> getRankedMP()
    {
        return rankedMP;
    }

    public List<(int,float,float)> getDetailedUtility()
    {
        return detailedUtility;
    }
    public bool getReady()
    {
        return ready;
    }
    
    public float getIslandRadius()
    {
        return islandRadius;
    }

    /// <summary>
    /// Returns a boolean stating if the island is being pointed at by the mouse cursor.
    /// </summary>
    /// <returns>A boolean stating if the island is being pointed at by the mouse cursor.</returns>
    public bool isPointedAt()
    {
        return CursorOn;
    }
}
