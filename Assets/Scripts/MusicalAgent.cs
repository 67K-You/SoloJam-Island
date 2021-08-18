using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

//[RequireComponent(typeof(CustomNavMeshAgent))]
public class MusicalAgent : Agent
{
    //A script responsible for initialising everything following the hyperparameters specified by the user.
    protected SimHyperParameters Sim;
    //The script responsible for showing the musical patterns in a HUD when needed.
    protected DisplayMusicalPatterns Displayer;
    
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
    static protected System.Random random = new System.Random();
    public AudioClip perc;
    protected MeshCollider visionCollider;
    protected CustomNavMeshAgent agentNavigator;
    protected float colliderRadius = 0.01f;
    //An array of 0 and 1 containing the agent's musical pattern
    protected Vector3 initialPosition;
    protected int[] musicalPattern;
    //An array of 0 and 1 containing the musical pattern of the leader of the agent's previous environment
    protected int[] formerLeaderMusicalPattern;
    //The position of the cursor indicating at what beat the agent is playing
    protected int cursorPosition = 0;
    //An instantiation of a random number generator
    protected bool isLeader = false;
    protected bool isTraveling = false;
    protected bool justHandedLeadership = false;
    protected bool isPlaying = false;
    protected bool justArrived = false;
    protected bool noOneIsPlaying;
    protected bool auctionTookPlace;
    protected int timePlaying = 0;
    protected int timeInNewIsland = 0;
    protected int agentNumber;
    //The agent's current island
    protected Island island;
    //The number of the island the agent is currently in
    protected int islandNumber;
    protected float utility;
    protected float currentIslandUtility = 0;
    protected float formerIslandUtility = 0;
    protected List<MusicalAgent> agents=new List<MusicalAgent>();
    //List containing the musical patterns ranked by utility
    protected List<Data> rankedMP = new List<Data>();
    public double frequency = 440;
    protected double increment;
    protected double phase;
    protected double samplingFrequency = 48000;
    protected double gain;
    public double volume = 0.003;
    //Pentatonic scale frequencies
    protected double[] musicScale = { 415.3047, 440.0000, 466.1638, 493.8833, 523.2511, 554.3653, 587.3295, 622.2540 };

    //Unity specific methods

    // Start is called before the first frame update
    protected void Awake()
    {
        Sim = FindObjectOfType<SimHyperParameters>();
        //The agent is added adds itself to its list of perceived agents (if behaviourMode is set to Reinforcement learning, this is done in the overrode version of the start method.)
        if(Sim.behaviourMode==SimHyperParameters.BehaviourMode.EvolutionaryAlgorithm)
        {
            //The script describing the reinforcement learning behaviour of the agent is destroyed because it is not going to be used
            Destroy(gameObject.GetComponent<MLMusicalAgent>());
            agents.Add(this);
        }
        Displayer = FindObjectOfType<DisplayMusicalPatterns>();
        //The collider representing how far the agent sees.
        visionCollider = GetComponentInChildren<MeshCollider>();
        //The amount we need to scale our sensing collider so that it has a radius of sensingRadius
        float upscale = sensingRadius / colliderRadius;
        visionCollider.gameObject.transform.localScale = new Vector3(upscale, upscale, visionCollider.gameObject.transform.localScale.z);
        //The size of the musical pattern is defined to match the length specified in Sim
        musicalPattern = new int[Sim.getMusicalPatternLength()];
        formerLeaderMusicalPattern = new int[Sim.getMusicalPatternLength()];
        //This array is initialized randomly with 0 and 1
        for (int i = 0; i < Sim.musicalPatternLength; i++)
        {
            musicalPattern[i] = random.Next(0, 2);
        }
    }
    
    protected virtual void Start()
    {
        if(Sim.behaviourMode==SimHyperParameters.BehaviourMode.EvolutionaryAlgorithm)
        {
            StartCoroutine(NavigatorInit());
            initialPosition = gameObject.transform.position;
        }
    }


    // Update is called once per frame
    protected void Update()
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

    protected void OnMouseEnter()
    {
        //Show the sensing range of the agent if the mouse cursor goes over the agent
        sensingCircleDecal.size =new Vector3(2*sensingRadius,2*sensingRadius,0.18f);
    }

    protected void OnMouseExit()
    {
        //Hides the sensing range of the agent if the mouse cursor leave the agent
        sensingCircleDecal.size =new Vector3(0.1f,0.1f,0.18f);
    }


    protected virtual void OnTriggerEnter(Collider other)
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

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.transform.root.gameObject.tag == "MusicalAgent")
        {
            MusicalAgent a = other.transform.root.gameObject.GetComponent<MusicalAgent>();
            //Remove all occurences of the the musical agent that just leaved the sensing range of this agent's agents.
            agents.RemoveAll(x => x.getAgentNumber() == a.getAgentNumber());
        }
    }

    public virtual void ResetAgent()
    {
        isLeader = false;
        isTraveling = false;
        justHandedLeadership = false;
        islandNumber = agentNumber / Sim.numberOfAgents;
        justArrived = false;
        noOneIsPlaying = false;
        auctionTookPlace = false;
        timePlaying = 0;
        timeInNewIsland = 0;
        utility = 0;
        formerIslandUtility = 0;
        currentIslandUtility = 0;
        cursorPosition = 0;
        bodyMeshRenderer.material.color = baseColor;
        Array.Clear(formerLeaderMusicalPattern,0,formerLeaderMusicalPattern.Length);
        //Randomly resets the musical pattern
        for (int i = 0; i < Sim.musicalPatternLength; i++)
        {
            musicalPattern[i] = random.Next(0, 2);
        }
        agentNavigator.ResetPath();
        gameObject.transform.position = initialPosition;
        //the agents list containing all agent in the sensing range is reset
        agents.Clear();
        int initialIslandID = agentNumber / Sim.numberOfAgents;
        foreach(var agent in Sim.getAgentsList())
        {
            if(initialIslandID==agent.getAgentNumber() / Sim.numberOfAgents)
            {
                agents.Add(agent);
            }
        }
        foreach(Island isle in Sim.getIslandsList())
        {
            if(isle.getIslandNumber()==islandNumber)
            {
                island = isle;
            }
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
    public void setJustArrived(bool arrivedState)
    {
        justArrived = arrivedState;
    }
    public void setJustHandedLeadership(bool state)
    {
        justHandedLeadership = state;
    }
    public void setTimeInNewIsland(int newTime)
    {
        timeInNewIsland = newTime;
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
    public bool getJustArrived()
    {
        return justArrived;
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
    public float getFormerIslandUtility()
    {
        return formerIslandUtility;
    }

    public float getCurrentIslandUtility()
    {
        return currentIslandUtility;
    }
    public int getTimeInNewIsland()
    {
        return timeInNewIsland;
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
    /// Compute a weighted mean between the agent's musical pattern utility compared with the current leader's and agent's musical pattern utility with the last snapshot of the leader from its previous island.
    /// This mean give higher importance to the current leader's pattern the longer the agent spends time in its new Island.
    /// Also increments the time the agent has spent on the Island by one musical pattern each time it is called.
    /// </summary>
    public void compareWithFormerLeader()
    {
        if (!isTraveling)
        {
            for (int i = 0; i < agents.Count; i++)
            {
                if (agents[i].getIsLeader())
                {
                    currentIslandUtility = computeUtility(musicalPattern, agents[i].getMusicalPattern());
                }
            }
            formerIslandUtility = computeUtility(musicalPattern, formerLeaderMusicalPattern);
            //The utility is a weighted mean of the utility the agent would have had compared with the leader of its former island and the utility of the agent compared with the leader of the current group
            utility = ((Sim.adaptationPeriod - timeInNewIsland) * formerIslandUtility + (Sim.adaptationPeriod + timeInNewIsland) * currentIslandUtility) / ((float)(2 * Sim.adaptationPeriod));
            timeInNewIsland++;
        }
    }

    /// <summary>
    /// Computes the utility of a musical pattern given a leader pattern.
    /// </summary>
    /// <param name="currentMP">The pattern whose utility has to be calculated</param>
    /// <param name="leaderMP">The pattern of the leader</param>
    /// <returns>The computed utility</returns>
    protected float computeUtility(int[] currentMP, int[] leaderMP)
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
    protected int hammingDistance(int[] A, int[] B)
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
                if (random.Next(0,100) < Sim.mutationRate*length*100)
                {
                    musicalPattern[i] = Mathf.Abs(musicalPattern[i] - 1);
                }
            }
        }
    }

    /// <summary>
    /// Performs a 2 points genetic crossover with the agents musical pattern and another pattern
    /// </summary>
    /// <param name="performWithLeader">If True performs a crossover with the pattern of the current leader, else performs a crossover with the former leaders pattern</param>
    private void crossover(bool performWithLeader)
    {
        if(!isLeader)
        {
            int length = musicalPattern.Length;
            int[] leaderMP=(int[])musicalPattern.Clone();
            int start = random.Next() % length;
            int end = random.Next() % length;
            if (performWithLeader)
            {
                for (int i = 0; i < agents.Count; i++)
                {
                    if (agents[i].getIsLeader())
                    {
                        leaderMP = agents[i].getMusicalPattern();
                    }
                }
            }
            else
            {
                leaderMP = formerLeaderMusicalPattern;
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
    protected void leaderUpdate(int i)
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
    protected void updateRankedMP()
    {
        rankedMP.Clear();
        List<(int,float,float)> detailedUtility = new List<(int,float,float)>();
        foreach(var agent in agents)
        {
            //An agent performance is considered only if it has stopped traveling
            if(!agent.getIsTraveling())
            {
                rankedMP.Add(new Data(agent.getAgentNumber(), agent.getUtility(), agent.getMusicalPattern()));
                if(agent.getJustArrived())
                {
                    detailedUtility.Add((agent.getAgentNumber(), agent.getFormerIslandUtility(), agent.getCurrentIslandUtility()));
                }
            }
        }
        rankedMP.Sort((x, y) => y.utility.CompareTo(x.utility));
        island.updateRankedMP(rankedMP,detailedUtility);
    }

    /// <summary>
    /// Method responsible for making the synthesizer play sine waves
    /// </summary>
    /// <param name="data"></param>
    /// <param name="channel"></param>
    protected void OnAudioFilterRead(float[] data, int channel)
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
    protected virtual IEnumerator AuctionProcess()
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
            if (!agents[i].getIsTraveling())
            {
                if (Sim.crossVariant)
                {
                    if (!agents[i].getJustArrived())
                    {
                        if (random.Next(0, 100) < Sim.crossProb)
                        {
                            agents[i].crossover(true);
                        }
                    }
                    else
                    {
                        if (random.Next(0, 100) < Sim.crossProb * (float)(Sim.adaptationPeriod + timeInNewIsland) / (2 * Sim.adaptationPeriod))
                        {
                            agents[i].crossover(true);
                        }
                        if (random.Next(0, 100) < Sim.crossProb * (float)(Sim.adaptationPeriod - timeInNewIsland) / (2 * Sim.adaptationPeriod))
                        {
                            agents[i].crossover(false);
                        }
                    }
                }
                agents[i].mutation();
                if (!agents[i].getJustArrived())
                {
                    agents[i].compareWithLeader();
                }
                else
                {
                    if (agents[i].getTimeInNewIsland() < Sim.adaptationPeriod)
                    {
                        //Use the custom comparison function if the agent has not fully adapted to its group yet.
                        agents[i].compareWithFormerLeader();
                    }
                    else
                    {
                        //After the adaptation period has passed, the agent becomes an agent just like the others in its group
                        agents[i].setJustArrived(false);
                        agents[i].setTimeInNewIsland(0);
                        agents[i].compareWithLeader();
                    }
                }
            }
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
    protected IEnumerator Migration()
    {
        Debug.Log(agentNumber.ToString() +" migrates.");
        //A little pause of half a musical pattern so the agent can say goodbye to its friends
        yield return new WaitForSeconds((float)(Sim.musicalPatternLength*60.0 / (4*Sim.getBpm())));
        //The musical pattern of the Current leader is copied
        island.getRankedMP()[0].musicalPattern.CopyTo(formerLeaderMusicalPattern,0);
        isTraveling = true;
        utility = 0;
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
            island = destination;
            agentNavigator.SetDestination(destination.transform.position);
        }
    }

    /// <summary>
    /// Store the navMesh component into agentNavigator so it can be accessed later (needs to wait a bit because the navigation component is instantiated on the first frame).
    /// </summary>
    /// <returns></returns>
    protected IEnumerator NavigatorInit()
    {
        yield return new WaitForSeconds(0.1f);
        agentNavigator = GetComponent<CustomNavMeshAgent>();
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