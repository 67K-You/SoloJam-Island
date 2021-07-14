using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

/// <summary>
/// A class responsible for containing variables defining the hyperparameters of the simulation and the associated initializing methods.
/// </summary>
public class SimHyperParameters : MonoBehaviour
{
    [Tooltip("The number of islands where agents will play.")]
    public int numberOfIslands;
    [Tooltip("Island prefab.")]
    public Island island;

    [Tooltip("The number of agents to be initialized in each island")]
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
    [Tooltip("A number between 0 and 100 defining the probability of a migration happening")]
    public int migrationProbability;
    [Tooltip("Chooses whether or not the results of the experiment should be exported to CSV")]
    public bool recordMode;
    [Tooltip("The name of the CSV file")]
    public string filename;
    [Tooltip("Chooses whether or not the experiment should be conducted multiple times to have statisticaly significant results")]
    public bool gymMode;
    [Tooltip("Number of times the experimented should be conducted")]
    public int numberOfIterations;
    [Tooltip("Duration of the experiment (in musical patterns)")]
    public int numberOfPatterns;
    //An instantiation of a random number generator
    private System.Random random=new System.Random();
    private int agentID=0;
    private int iterationNumber = 1;
    private int musicalPatternNumber = 1;
    private List<MusicalAgent> AgentsList = new List<MusicalAgent>();
    private List<Island> islandsList = new List<Island>();

    // Start is called before the first frame update
    void Start()
    {
        islandPlacement();
        foreach(Island island in islandsList)
        {  
            spawnAgents(island);
        }
        createCSV();
    }

    // Update is called once per frame
    void Update()
    {
        //Only when all islands have played their musical patterns, the data is stored in the csv then agents can keep on playing
        if(islandsList.TrueForAll(isReady))
        {
            addCSVLine();
            musicalPatternNumber++;
            foreach (Island isle in islandsList)
            {
                isle.setReady(false);
            }
            if (gymMode && musicalPatternNumber>numberOfPatterns)
            {
                ResetEnvironnement();
                musicalPatternNumber = 1;
                iterationNumber++;
                if(iterationNumber<=numberOfIterations)
                {

                    islandPlacement();
                    foreach(Island island in islandsList)
                    {  
                        spawnAgents(island);
                    }
                }
                else
                {
                    Application.Quit();
                }
            }
        }
    }
    
    /// <summary>
    ///  Function responsible for spawning the correct number of agents in a circle centered on the island whose size depends on the number and size of the musical agents.
    /// </summary>
    /// <param name="island">The island the agents are going to spawn on</param>
    private void spawnAgents(Island island)
    {
        //We get the vector3 containing the dimensions of the MusicalAgent
        Vector3 agentSize = Agent.GetComponent<Collider>().bounds.size;
        //We take the maximum size of the agents between the x and the z axis
        float refLength = Mathf.Max(agentSize[0]+0.05f, agentSize[2]+0.05f);
        // We compute the radius of the circle given the 2*PI*radius = 2*refLength*numberOfAgents to ensure appropriate space between agents
        float radius = (refLength * numberOfAgents) / Mathf.PI;
        int chosenOne = random.Next(agentID, agentID+numberOfAgents);
        //Loop responsible for placing the agents
        for (int i = 0; i < numberOfAgents; i++)
        {
            AgentsList.Add(Instantiate(Agent) as MusicalAgent);
            //Some trigonometry to go in the right place
            float theta = i * 2 * Mathf.PI / numberOfAgents;
            AgentsList[agentID].transform.position = new Vector3(radius * Mathf.Cos(theta) + island.transform.position.x, 0, radius * Mathf.Sin(theta) + island.transform.position.z);
            //Assign a unique identifier to the agent
            AgentsList[agentID].setAgentNumber(agentID);
            //Assign the number of the island it is currently in to the agent
            AgentsList[agentID].setIslandNumber(island.getIslandNumber());
            AgentsList[agentID].setIsland(island);
            //Randomly chooses one agent to begin as the leader
            if (agentID == chosenOne)
            {
                AgentsList[agentID].setIsLeader(true);
                AgentsList[agentID].setLeaderColor();
            }   
            agentID++;
        }
    }
    /// <summary>
    /// Places the number of islands specified by numberOfIslands in a circle whose size depends of the floors
    /// </summary>
    private void islandPlacement()
    {
        if(numberOfIslands<=0)
        {
            return;
        }
        if(numberOfIslands==1)
        {
            islandsList.Add(Instantiate(island) as Island);
            islandsList[0].transform.position = new Vector3(0, 0, 0);
            islandsList[0].setIslandNumber(0);
            return;
        }
        else
        {
            //We try to find the biggest circle that can be fitted in the floor space(leaving a little space so that agents can move arround). The islands are going to be evenly spaced on this circle
            Vector3 floorSize = GameObject.FindGameObjectWithTag("Floor").GetComponent<Collider>().bounds.size;
            float refLength = 3f / 8f * Mathf.Min(floorSize[0], floorSize[2]);
            float increment = 2 * Mathf.PI / numberOfIslands;
            for (int i = 0; i < numberOfIslands;i++)
            {
                islandsList.Add(Instantiate(island) as Island);
                islandsList[i].transform.position = new Vector3(refLength * Mathf.Cos(i * increment), 0, refLength * Mathf.Sin(i * increment));
                islandsList[i].setIslandNumber(i);
            }
        }
    }

    private static bool isReady(Island isle)
    {
        return isle.getReady();
    }

    public int getMusicalPatternLength()
    {
        return musicalPatternLength;
    }

    public int getBpm()
    {
        return bpm;
    }

    public List<MusicalAgent> getAgentsList()
    {
        return AgentsList;
    }
    

    /// <summary>
    /// This method fills the first line of the csv with the ID's of the agents.
    /// </summary>
    public void createCSV()
    {
        //Doesn't do anything if the session isn't recorded
        if (!recordMode)
        {
            return;
        }
        TextWriter tw = new StreamWriter(System.IO.Directory.GetCurrentDirectory() + "\\" + filename + ".csv", false);
        string firstLine = "";
        for (int i = 0; i < islandsList.Count; i++)
        {
            firstLine += islandsList[i].getIslandNumber();
            firstLine += ";";
            for (int j = 0; j < AgentsList.Count; j++)
            {
                firstLine = firstLine + AgentsList[j].getAgentNumber();
                if (j != (AgentsList.Count - 1 ) || (i !=islandsList.Count-1))
                {
                firstLine += ";";
                }
            }
        }
        tw.WriteLine(firstLine);
        tw.Close();
        Debug.Log("csv created");
    }

    /// <summary>
    /// This method adds a line to the CSV containing the utilities of the agents in the order of their ID's at the position of the island they're at. The overall utility of an island is also computed
    /// </summary>
    /// <param name="reset">If true adds a blank spacing line to the CSV</param>
    public void addCSVLine(bool reset=false)
    {
        //Doesn't do anything if the session isn't recorded
        if(!recordMode)
        {
            return;
        }

        TextWriter tw = new StreamWriter(System.IO.Directory.GetCurrentDirectory() + "\\"+filename+".csv", true);
        string line = "";
        float islandUtility;
        for (int i = 0; i < islandsList.Count; i++)
        {
            islandUtility = 0;
            //The overall utility of the island is computed
            foreach (Data agentData in islandsList[i].getRankedMP())
            {
                islandUtility += agentData.utility;;
            }
            if(!reset)
            {
                line += islandUtility;
            }
            line += ";";
            //for each island the whole list of agents is parsed in order of ascending agentID. If an agent is currently part of this island his utility is added to the csv.
            for (int j = 0; j < AgentsList.Count; j++)
            {
                foreach(Data agentData in islandsList[i].getRankedMP())
                {
                    if(!reset && agentData.agentNumber==j)
                    {
                        line += agentData.utility;
                    }
                }
                if (j != (AgentsList.Count - 1 ) || (i !=islandsList.Count-1))
                {
                line += ";";
                }
            }
        }
        tw.WriteLine(line);
        tw.Close();
        Debug.Log("new line added");
    }

    private void ResetEnvironnement()
    {
        foreach(MusicalAgent agent in AgentsList)
        {
            GameObject.DestroyImmediate(agent.gameObject);
        }
        foreach(Island isle in islandsList)
        {
            GameObject.DestroyImmediate(isle.gameObject);
        }
        AgentsList.Clear();
        islandsList.Clear();
        agentID = 0;
        addCSVLine(true);
    }
}
