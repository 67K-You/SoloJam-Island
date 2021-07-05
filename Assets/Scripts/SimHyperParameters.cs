using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

/// <summary>
/// A class responsible for containing variables defining the hyperparameters of the simulation and the associated initializing methods.
/// </summary>
public class SimHyperParameters : MonoBehaviour
{
    [Tooltip("The number of agents to be initialized")]
    public int numberOfAgents;
    [Tooltip("The Musical Agent that is going to be initialized")]
    public MusicalAgent Agent;
    [Tooltip("The BPM of the musical peformance")]
    public int bpm;
    [Tooltip("The length of the musical patterns(in beats)")]
    public int musicalPatternLength;
    
    [Tooltip("Chooses whether the agents should play percussions or tones")]
    public PlayMode playMode;
    public enum PlayMode { Tonal, Percussion };
    [Tooltip("Chooses whether or not crossovers should be used")]
    public bool crossVariant;
    [Tooltip("A number between 0 and 100 defining the probability of a crossover happening")]
    public int crossProb;
    [Tooltip("Chooses whether or not the results of the experiment should be exported to CSV")]
    public bool recordMode;
    [Tooltip("The name of the CSV file")]
    public string filename;
    //List containing the musical patterns ranked by utility
    public List<(int agentNumber,float utility, int[] musicalPattern)> rankedMP = new List<(int agentNumber, float utility, int[] musicalPattern)>();
    //An instantiation of a random number generator
    private System.Random random=new System.Random();
    private List<MusicalAgent> AgentsList = new List<MusicalAgent>();
    private MusicalAgent[] AgentsArray;

    // Start is called before the first frame update
    void Start()
    {
        SpawnAgents();
    }

    // Update is called once per frame
    void Update()
    {
    }

    /// <summary>
    ///  Function responsible for spawning the correct number of agents in a circle whose size depends on the number and size of the musical agents.
    /// </summary>
    private void SpawnAgents()
    {
        MusicalAgent MA = Instantiate(Agent) as MusicalAgent;
        //We get the vector3 containing the dimensions of the MusicalAgent
        Vector3 agentSize = MA.GetComponent<Collider>().bounds.size;
        //We take the maximum size of the agents between the x and the z axis
        float refLength = Mathf.Max(agentSize[0], agentSize[2]);
        // We compute the radius of the circle given the 2*PI*radius = 2*refLength*numberOfAgents to ensure appropriate space between agents
        float radius = (refLength * numberOfAgents) / Mathf.PI;
        DestroyImmediate(MA.gameObject);
        int chosenOne = random.Next(0, numberOfAgents);
        //Loop responsible for placing the agents
        for (int i = 0; i < numberOfAgents; i++)
        {
            MusicalAgent a = Instantiate(Agent) as MusicalAgent;
            //Some trigonometry to go in the right place
            float theta = i * 2 * Mathf.PI / numberOfAgents;
            a.transform.position = new Vector3(radius * Mathf.Cos(theta), 0, radius * Mathf.Sin(theta));
            //Assign a number to the agent
            a.setAgentNumber(i);
            //Randomly chooses one agent to begin as the leader
            if (i == chosenOne)
            {
                a.setIsLeader(true);
                a.setLeaderColor();
            }
            AgentsList.Add(a);
        }
        //Loop responsible for making each agent aware of it's surrounding when every agent has been initialised
        foreach(MusicalAgent agent in AgentsList)
        {
            agent.setAgentsArray();
        }

        foreach (MusicalAgent agent in AgentsList)
        {
            agent.compareWithLeader();
        }
        updateRankedMP();
        createCSV();
    }

    public int getMusicalPatternLength()
    {
        return musicalPatternLength;
    }

    public int getBpm()
    {
        return bpm;
    }

    public List<(int agentNumber,float utility, int[] musicalPattern)> getRankedMP()
    {
        return rankedMP;
    }

    public List<MusicalAgent> getAgentsList()
    {
        return AgentsList;
    }
    
    public void updateRankedMP()
    {
        rankedMP.Clear();
        (int,float, int[]) values;
        AgentsArray = FindObjectsOfType<MusicalAgent>();
        foreach(MusicalAgent agent in AgentsArray)
        {
            values = (agent.getAgentNumber(), agent.getUtility(), agent.getMusicalPattern());
            rankedMP.Add(values);
        }
        rankedMP.Sort((x, y) => y.utility.CompareTo(x.utility));
    }
    /// <summary>
    /// This method fills the first line of the csv with the ID's of the agents.
    /// </summary>
    public void createCSV()
    {
        //Doesn't do anything if the session isn't recorded
        if(!recordMode)
        {
            return;
        }
        TextWriter tw = new StreamWriter(System.IO.Directory.GetCurrentDirectory() + "\\" + filename + ".csv", false);
        string firstLine = "";
        for (int i = 0; i < AgentsList.Count; i++)
        {
            firstLine = firstLine + AgentsList[i].getAgentNumber();
            if(i!=AgentsList.Count-1)
            {
                firstLine += ";";
            }
        }
        tw.WriteLine(firstLine);
        tw.Close();
        Debug.Log("csv created");
    }
    /// <summary>
    /// This method adds a line to the CSV containing the utilities of the agents in the order of their ID's 
    /// </summary>
    public void addCSVLine()
    {
        //Doesn't do anything if the session isn't recorded
        if(!recordMode)
        {
            return;
        }

        TextWriter tw = new StreamWriter(System.IO.Directory.GetCurrentDirectory() + "\\"+filename+".csv", true);
        string line = "";
        Debug.Log(AgentsList.Count);
        for (int i = 0; i < AgentsList.Count; i++)
        {
            Debug.Log(AgentsList[i].getUtility());
            line = line + AgentsList[i].getUtility();
            if(i!=AgentsList.Count-1)
            {
                line += ";";
            }
        }
        tw.WriteLine(line);
        tw.Close();
        Debug.Log("new line added");
    }
}
