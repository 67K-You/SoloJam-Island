using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class MusicalAgent : MonoBehaviour
{
    //A script responsible for initialising everything following the hyperparameters specified by the user.
    SimHyperParameters Sim;
    //The script responsible for showing the musical patterns in a HUD when needed.
    DisplayMusicalPatterns Displayer;
    
    [Tooltip("A class which exports the agents' utility to csv")]
    public GameObject TextIndicator;
    [Tooltip("Importance of the hamming distance of an agent’s current rhythmic pattern with respect to the leader’s current rhythmic pattern.")]
    public float a;
    [Tooltip("Importance of the length of time an agent has been playing the solo.")]
    public float b;
    [Tooltip("Normalisation constant of the utility function")]
    public float c;
    [Tooltip("The percentage of the musical pattern that should be different from the leader's")]
    public float eps;
    [Tooltip("The radius distance at which an agent can see another agent(in meters).")]
    public float sensingRadius;

    [Tooltip("The speed of the agent(in meters/s).")]
    public float speed;
    [Tooltip("The body's mesh renderer")]
    public MeshRenderer bodyMeshRenderer;
    [Tooltip("A visual indicator for the distance at which an agent can see.")]
    public DecalProjector sensingCircleDecal;
    [Tooltip("The body's color of the leader")]
    public Color leaderColor = new Color(1f, 0f, .3f);
    [Tooltip("The base color of a agent's body")]
    public Color baseColor = new Color(0.7508397f, 0.3841763f, 0.6840597f);
    public AudioClip perc;
    private MeshCollider visionCollider;
    private float colliderRadius = 0.01f;
    //An array of 0 and 1 containing the agent's musical pattern
    private Pathfinding pathfinder;
    private int[] musicalPattern;
    //The position of the cursor indicating at what beat the agent is playing
    private int cursorPosition = 0;
    //An instantiation of a random number generator
    static private System.Random random = new System.Random();
    private bool isLeader = false;
    private bool isTraveling = false;
    private bool justHandedLeadership = false;
    private bool isPlaying = false;
    private bool noOneIsPlaying;
    private bool auctionTookPlace;
    private int timePlaying = 0;
    private int agentNumber;
    private int leaderNumber;
    //The agent's current island
    private Island island;
    //The number of the island the agent is currently in
    private int islandNumber;
    private float utility;
    List<MusicalAgent> agents=new List<MusicalAgent>();
    //List containing the musical patterns ranked by utility
    private List<Data> rankedMP = new List<Data>();

    public double frequency = 440;
    private double increment;
    private double phase;
    private double samplingFrequency = 48000;
    private double gain;
    public double volume = 0.003;
    //Pentatonic scale frequencies
    private double[] musicScale = { 415.3047, 440.0000, 466.1638, 493.8833, 523.2511, 554.3653, 587.3295, 622.2540 };

    //Unity specific methods

    // Start is called before the first frame update
    void Awake()
    {
        Sim = FindObjectOfType<SimHyperParameters>();
        Displayer = FindObjectOfType<DisplayMusicalPatterns>();
        pathfinder = GetComponent<Pathfinding>();
        //The collider representing how far the agent sees.
        visionCollider = GetComponentInChildren<MeshCollider>();
        //The amount we need to scale our sensing collider so that it has a radius of sensingRadius
        float upscale = sensingRadius / colliderRadius;
        visionCollider.gameObject.transform.localScale = new Vector3(upscale, upscale, visionCollider.gameObject.transform.localScale.z);
        //The size of the musical pattern is defined to match the length specified in Sim
        musicalPattern = new int[Sim.getMusicalPatternLength()];
        //This array is initialized randomly with 0 and 1
        for (int i = 0; i < Sim.musicalPatternLength; i++)
        {
            musicalPattern[i] = random.Next(0, 2);
        }
        //The agent is added adds himself to its list of perceived agents
        agents.Add(this);
    }

    // Update is called once per frame
    void Update()
    {
        //if statement responsible for showing the TextIndicator facing the camera and displaying the right number.
            
        TextIndicator.GetComponent<TextMesh>().text = agentNumber.ToString();
        if(!isTraveling)
        {
            this.transform.LookAt(Vector3.ProjectOnPlane(Camera.main.transform.position,Vector3.up));
        }

        noOneIsPlaying = true;
        //try to define if no agent is playing
        for (int i = 0; i < agents.Count; i++)
        {
            if (agents[i].getIsPlaying())
            {
                noOneIsPlaying = false;
            }
        }
        //if no one is playing and the Data from the previous musical pattern has been saved to csv, let an auction take place and define a new leader that is going to play.
        if (noOneIsPlaying && isLeader && !island.getReady())
        {
            StartCoroutine(AuctionProcess());
        }
    }

    void OnMouseEnter()
    {
        //Show the sensing range of the agent if the mouse cursor goes over the agent
        sensingCircleDecal.size =new Vector3(2*sensingRadius,2*sensingRadius,0.18f);
    }

    void OnMouseExit()
    {
        //Hides the sensing range of the agent if the mouse cursor leave the agent
        sensingCircleDecal.size =new Vector3(0.1f,0.1f,0.18f);
    }


    void OnTriggerEnter(Collider other)
    {
        if(other.transform.root.gameObject.tag=="MusicalAgent")
        {
        //Adds the agent who entered the sensing range of this musical agent to the list of agents currently perceived by this agent if they belong to the same island
        if(other.transform.root.gameObject.GetComponent<MusicalAgent>().getIslandNumber()==islandNumber && !agents.Contains(other.transform.root.gameObject.GetComponent<MusicalAgent>()))
        {
            agents.Add(other.transform.root.gameObject.GetComponent<MusicalAgent>());
        }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.transform.root.gameObject.tag == "MusicalAgent")
        {
            MusicalAgent a = other.transform.root.gameObject.GetComponent<MusicalAgent>();
            //Remove all occurences of the the musical agent that just leaved the sensing range of this agent's agents.
            agents.RemoveAll(x => x.getAgentNumber() == a.getAgentNumber());
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isTraveling && collision.collider.transform.root.gameObject.tag == "MusicalAgent")
        {
            //if the travelling agent has hit something it recalculates its path.
            pathfinder.AstarPathing();
            pathfinder.pathIndex = 0;
        }
    }

    //Attributes setters

    public void setAgentNumber(int number)
    {
        agentNumber = number;
    }

    public void setIslandNumber(int number)
    {
        islandNumber = number;
    }

    public void setIsland(Island isle)
    {
        island = isle;
    }
    public void setIsLeader(bool leaderState)
    {
        isLeader = leaderState;
    }
    public void setIsTraveling(bool travelingState)
    {
        isTraveling = travelingState;
    }
    public void setJustHandedLeadership(bool state)
    {
        justHandedLeadership = state;
    }

    public void setAgentsArray(List<MusicalAgent> agentsList)
    {
        agents = agentsList;
    }

    public void setLeaderColor()
    {
        bodyMeshRenderer.material.color = leaderColor;
    }

    //Attributes accessors

    public int getAgentNumber()
    {
        return agentNumber;
    }

    public int getIslandNumber()
    {
        return islandNumber;
    }

    public Island getIsland()
    {
        return island;
    }

    public bool getIsLeader()
    {
        return isLeader;
    }

    public bool getIsTraveling()
    {
        return isTraveling;
    }

    public bool getJustHandedLeadership()
    {
        return justHandedLeadership;
    }

    public bool getIsPlaying()
    {
        return isPlaying;
    }
    public int[] getMusicalPattern()
    {
        return musicalPattern;
    }

    public float getUtility()
    {
        return utility;
    }

    public Pathfinding getPathfinding()
    {
        return pathfinder;
    }
    
    /// <summary>
    /// Compare an agent's musical pattern utility with the leader's.
    /// </summary>
    public void compareWithLeader()
    {
        if(!isTraveling)
        {
            for (int i = 0; i < agents.Count; i++)
            {
                if (agents[i].getIsLeader())
                {
                    utility = computeUtility(musicalPattern, agents[i].getMusicalPattern());
                }
            }
        }
    }

    /// <summary>
    /// Computes the utility of a musical pattern given a leader pattern.
    /// </summary>
    /// <param name="currentMP">The pattern whose utility has to be calculated</param>
    /// <param name="leaderMP">The pattern of the leader</param>
    /// <returns>The computed utility</returns>
    private float computeUtility(int[] currentMP, int[] leaderMP)
    {
        int dl = hammingDistance(currentMP, leaderMP);
        //Utility is zero if an agent outputs the exact same pattern as the leader,or has been the leader during the previous timestep
        if ((dl <  eps*Sim.musicalPatternLength && !isLeader) || justHandedLeadership)
        {
            return 0;
        }
        else
        {
            float denominator = (float)(1 + a * dl) * (1 + b * timePlaying * Sim.getMusicalPatternLength());
            float u = (float)(c / denominator);
            return u;
        }

    }
    /// <summary>
    /// Computes the Hamming distance between 2 arrays
    /// </summary>
    /// <param name="A">The first array</param>
    /// <param name="B">The second array</param>
    /// <returns>The hamming distance between those arrays</returns>
    private int hammingDistance(int[] A, int[] B)
    {
        if (A.Length != B.Length)
        {
            throw (new NotSameSize("The lists compared have different sizes."));
        }
        else
        {
            int hammingDist = 0;
            for (int i = 0; i < A.Length; i++)
            {
                if (A[i]==0 ^ B[i]==0)
                {
                    hammingDist += 1;
                }
            }
            return hammingDist;
        }
    }
    /// <summary>
    /// Computes Pearson's correlation coefficient between 2 arrays
    /// </summary>
    /// <param name="Xs">The first array</param>
    /// <param name="Ys">The second array</param>
    /// <returns></returns>The correlation between those 2 arrays
    public static Double Correlation(int[] Xs, int[] Ys) 
    {
        Double sumX = 0;
        Double sumX2 = 0;
        Double sumY = 0;
        Double sumY2 = 0;
        Double sumXY = 0;

        int n = Xs.Length < Ys.Length ? Xs.Length : Ys.Length;

        for (int i = 0; i < n; ++i) 
        {
           Double x = Xs[i];
            Double y = Ys[i];

            sumX += x;
            sumX2 += x * x;
            sumY += y;
            sumY2 += y * y;
            sumXY += x * y;
        }

        Double stdX = Math.Sqrt(sumX2 / n - sumX * sumX / n / n);
        Double stdY = Math.Sqrt(sumY2 / n - sumY * sumY / n / n);
        Double covariance = (sumXY / n - sumX * sumY / n / n);

        return covariance / stdX / stdY; 
    }

    /// <summary>
    /// Mutates the each step of the musical pattern musical pattern with a probability of 1/(length of the musical pattern)
    /// </summary>
    private void mutation()
    {
        if (!isLeader)
        {
            int length = musicalPattern.Length;
            for (int i = 0; i < length; i++)
            {
                if (random.Next() % length == 0)
                {
                    musicalPattern[i] = Mathf.Abs(musicalPattern[i] - 1);
                }
            }
        }
    }

    /// <summary>
    /// Performs a 2 points genetic crossover with the agents musical pattern and the leader's
    /// </summary>
    private void crossover()
    {
        if(!isLeader)
        {
            int length = musicalPattern.Length;
            int[] leaderMP=(int[])musicalPattern.Clone();
            int start = random.Next() % length;
            int end = random.Next() % length;
            for (int i = 0; i < agents.Count; i++)
            {
                if (agents[i].getIsLeader())
                {
                    leaderMP = agents[i].getMusicalPattern();
                }
            }
            if(start>end)
            {
                for (int i = start; i < length + end;i++)
                {
                    musicalPattern[i % length] = leaderMP[i % length];
                }
            }
            else
            {
                for (int i = start; i < end;i++)
                {
                    musicalPattern[i] = leaderMP[i];
                }
            }
        }
    }

    /// <summary>
    /// Updates the current leader of the band by replacing the actual leader by the agents number i. 
    /// The current leader gets into just handed leadership mode and agents who handed leadership during the previous patter leave this mode.
    /// Sometimes an agent migrates to another island
    /// </summary>
    /// <param name="i">The future leader's number.</param>
    private void leaderUpdate(int i)
    {
        isLeader = false;
        //tells if a migration is going to happen (provided there's at least 3 agents in this island)
        bool aMigration = (random.Next(0, 100) < Sim.migrationProbability);
        //The future leader won't migrate but anyone can
        int migratingAgentNumber = agents[random.Next(0, agents.Count)].getAgentNumber();
        bodyMeshRenderer.material.color = baseColor;
        for (int j = 0; j < agents.Count; j++)
        {
            //promote the best agent to leader status
            if (agents[j].getAgentNumber() == i)
            {
                agents[j].setIsLeader(true);
                agents[j].setLeaderColor();

            }
            //agents that just handed leadership are now free
            else if (agents[j].getJustHandedLeadership())
            {
                agents[j].setJustHandedLeadership(false);
            }
            //if there's enough agents on the island agents may migrate
            else if(aMigration && migratingAgentNumber != i && agents[j].getAgentNumber()==migratingAgentNumber && agents.Count>2)
            {
                Debug.Log("Migration de " + agents[j].getAgentNumber().ToString());
                StartCoroutine(agents[j].Migration());
            }
        }
        if (!isLeader)
        {
            justHandedLeadership = true;
            timePlaying = 0;
        }
    }
    /// <summary>
    /// The agent gathers all the interesting information of the agents it can see, ranks this information by utility and then stores it in the island structure it belongs to.
    /// </summary>
    private void updateRankedMP()
    {
        rankedMP.Clear();
        foreach(MusicalAgent agent in agents)
        {
            //An agent performance is considered only if it has stopped traveling
            if(!agent.getIsTraveling())
            {
                rankedMP.Add(new Data(agent.getAgentNumber(), agent.getUtility(), agent.getMusicalPattern()));
            }
        }
        rankedMP.Sort((x, y) => y.utility.CompareTo(x.utility));
        island.updateRankedMP(rankedMP);
    }

    /// <summary>
    /// Method responsible for making the synthesizer play sine waves
    /// </summary>
    /// <param name="data"></param>
    /// <param name="channel"></param>
    private void OnAudioFilterRead(float[] data, int channel)
    {
        increment = frequency * 2 * Mathf.PI / samplingFrequency;
        for (int i = 0; i < data.Length; i += channel)
        {
            phase += increment;
            data[i] = (float)(gain * Mathf.Sin((float)phase));

            if (channel == 2)
            {
                data[i + 1] = data[i];
            }

            if (phase > 2 * Mathf.PI)
            {
                phase = 0;
            }
        }
    }

    //COROUTINES

    /// <summary>
    /// Coroutine responsible for making the auction process happening
    /// </summary>
    /// <returns></returns>
    IEnumerator AuctionProcess()
    {
        //Makes the leader play its musical pattern.
        isPlaying = true;
        switch(Sim.playMode)
        {
            //When Tonal is selected the agent uses the synthesiser to play a note on the pentatonic scale
            case SimHyperParameters.PlayMode.Tonal:
                for (int i = 0; i < musicalPattern.Length; i++)
                {
                    if (musicalPattern[i] == 0)
                    {
                        gain = 0;
                    }
                    else
                    {
                        
                        //Only plays a sound if it needs to and the island is being pointed at to avoid cacophony
                        if(island.isPointedAt())
                        {
                            gain = volume;
                            frequency = musicScale[random.Next(0, musicScale.Length)];
                        }
                        else
                        {
                            gain = 0;
                        }
                    }
                    yield return new WaitForSeconds((float)60.0 / (2 * Sim.getBpm()));
                    cursorPosition++;
                    Displayer.translateCursors(cursorPosition);
                }
            break;

            //When atonal is selected the agent plays a percussion .wav when it needs to
            case SimHyperParameters.PlayMode.Percussion:
                for (int i = 0; i < musicalPattern.Length; i++)
                {
                    //Only plays a sound if it needs to and the island is being pointed at to avoid cacophony
                    if (musicalPattern[i] == 1 && island.isPointedAt())
                    {
                        AudioSource.PlayClipAtPoint(perc, this.transform.position, 0.5f);
                    }
                    yield return new WaitForSeconds((float)60.0 / (2*Sim.getBpm()));
                    cursorPosition++;
                    if (island.isPointedAt())
                    {
                        Displayer.translateCursors(cursorPosition);
                    }
                }
            break;
        }

        isPlaying = false;
        gain = 0;
        timePlaying += 1;
        //Define the utility of the leader's musical pattern
        compareWithLeader();
        //Iterate over the other agents to try to find a candidate with a better utility
        for (int i = 0; i < agents.Count; i++)
        {
            if(Sim.crossVariant && random.Next(0,100)<Sim.crossProb)
            {
                agents[i].crossover();
            }
            agents[i].mutation();
            agents[i].compareWithLeader();
        }
        //The musical patterns are ranked by utility
        updateRankedMP();
        cursorPosition = 0;
        //Updates the musical patterns shown on screen if the island is being pointed at.
        if(island.isPointedAt())
        {
            foreach(Canvas i in GameObject.FindObjectsOfType<Canvas>())
            {
                Destroy(i.gameObject);
            }
            //The UI shows the 5 best musical patterns
            Displayer.showMusicalPatterns(island);
        }
        //The best musician gets to be the next leader
        leaderUpdate(rankedMP[0].agentNumber);
    }

    /// <summary>
    /// Coroutine responsible for making an agent migrate randomly to another island.
    /// </summary>
    /// <returns></returns>
    IEnumerator Migration()
    {
        Debug.Log(agentNumber.ToString() +" migrates.");
        yield return new WaitForSeconds(0.5f);
        isTraveling = true;
        //Randomly choose another island
        int nextIslandnumber=(islandNumber + random.Next(1, Sim.numberOfIslands))%Sim.numberOfIslands;
        Island[] islands = FindObjectsOfType<Island>();
        Island destination=null;
        foreach(Island island in islands)
        {
            if(island.getIslandNumber()==nextIslandnumber)
            {
                destination = island;
            }
        }
        //if the island has been found tell the agent to go there
        if(destination!=null)
        {
            //now the agent belongs to the newly chosen island
            islandNumber = nextIslandnumber;
            utility = 0;
            island = destination;
            //the pathfinder aims at this new island's location
            pathfinder.setTarget(destination.transform);
            //A* algorithm is used to find a path
            pathfinder.AstarPathing();
            //A little pause so the agent can say goodbye to its friends
            yield return new WaitForSeconds(0.5f);
            //in worldPath the path has been broken down into a broken line and only the position of each consecutive vertex has been stored
            Vector3 currentWaypoint = pathfinder.worldPath[0];
            pathfinder.pathIndex = 0;
            //while the agent has not reached its goal
            while(pathfinder.getLookingToGo())
            {
                //if the agent is close enough to the vertex it can move towards the next one
               if(Vector3.Distance(transform.position,currentWaypoint)<=0.01)
                {
                    pathfinder.pathIndex++;
                    //if the is at the end goal (ie the position of an island) it stops. However there's another stop condition triggered by the island's trigger collider so this statement isn't reached under normal conditions
                    if(pathfinder.pathIndex>=pathfinder.worldPath.Length)
                    {
                        pathfinder.pathIndex = 0;
                        isTraveling = false;
                        pathfinder.Stop();
                        yield break;
                   }
                   currentWaypoint = pathfinder.worldPath[pathfinder.pathIndex];
                }
                transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, speed*Time.deltaTime);
                this.transform.LookAt(Vector3.ProjectOnPlane(currentWaypoint,Vector3.up));

                yield return null;
            }
            pathfinder.pathIndex = 0;
            yield return null;
        }
    }
}


public class NotSameSize : Exception
{
    public NotSameSize(string message) : base(message)
    {
    }
}
/// <summary>
/// A struct representing the data contained in the rankedMP array
/// </summary>
public struct Data
{
    public Data(int x, float y, int[] z)
    {
        agentNumber = x;
        utility = y;
        musicalPattern = z;
    }

    public int agentNumber { get; }
    public float utility { get; }
    public int[] musicalPattern{ get; }

    /// <summary>
    /// Provides a deep copy of the Data struct.
    /// </summary>
    /// <returns>A deep copy of this struct.</returns>
    public Data Clone()
    {
        int[] clonedMusicalPattern = new int[musicalPattern.Length];
        musicalPattern.CopyTo(clonedMusicalPattern, 0);
        return new Data(agentNumber, utility, clonedMusicalPattern);
    }
}