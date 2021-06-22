using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;


public class MusicalAgent : MonoBehaviour
{
    [Tooltip("A reference to the SimHyperParameters class")]
    public SimHyperParameters Sim;
    [Tooltip("A reference to the DisplayMusicalPattern class")]
    public DisplayMusicalPatterns Displayer;
    [Tooltip("The 3d text that indicates the number of the Agent")]
    public GameObject TextIndicator;
    [Tooltip("Importance of the hamming distance of an agent’s current rhythmic pattern with respect to the leader’s current rhythmic pattern.")]
    public float a;
    [Tooltip("Importance of the length of time an agent has been playing the solo.")]
    public float b;
    [Tooltip("Normalisation constant of the utility function")]
    public float c;
    [Tooltip("An array of 0 and 1 containing the agent's musical pattern")]
    private int[] musicalPattern;
    [Tooltip("An instantiation of a random number generator")]
    static private System.Random random = new System.Random();
    [Tooltip("The body's mesh renderer")]
    public MeshRenderer bodyMeshRenderer;
    [Tooltip("The body's color of the leader")]
    public Color leaderColor = new Color(1f, 0f, .3f);
    [Tooltip("The base color of a agent's body")]
    public Color baseColor = new Color(0.7508397f, 0.3841763f, 0.6840597f);
    public AudioClip perc;
    private bool isLeader = false;
    private bool justHandedLeadership = false;
    private bool isPlaying = false;
    private bool noOneIsPlaying;
    private bool auctionTookPlace;
    private int timePlaying = 0;
    private int agentNumber;
    private int leaderNumber;
    private float utility;
    GameObject[] agents;

    public double frequency = 440;
    private double increment;
    private double phase;
    private double samplingFrequency = 48000;
    private double gain;
    public double volume = 0.03;
    //Pentatonic scale frequencies
    private double[] musicScale = { 415.3047, 440.0000, 466.1638, 493.8833, 523.2511, 554.3653, 587.3295, 622.2540 };

    // Start is called before the first frame update
    void Awake()
    {
        //The size of the musical pattern is defined to match the length specified in Sim
        musicalPattern = new int[Sim.getMusicalPatternLength()];
        //This array is initialized randomly with 0 and 1
        for (int i = 0; i < Sim.musicalPatternLength; i++)
        {
            musicalPattern[i] = random.Next(0, 2);
        }
    }

    // Update is called once per frame
    void Update()
    {
        //if statement responsible for showing the TextIndicator facing the camera and displaying the right number.
            
            TextIndicator.GetComponent<TextMesh>().text = agentNumber.ToString();
            this.transform.LookAt(Vector3.ProjectOnPlane(Camera.main.transform.position,Vector3.up));



        noOneIsPlaying = true;
        //try to define if no agent is playing
        for (int i = 0; i < agents.Length; i++)
        {
            if (agents[i].GetComponent<MusicalAgent>().getisPlaying())
            {
                noOneIsPlaying = false;
            }
        }
        //if no one is playing define let an auction take place and define a new leader that is going to play.
        if (noOneIsPlaying && isLeader)
        {
            StartCoroutine(AuctionProcess());
        }
    }

    //Attributes setters

    public void setAgentNumber(int number)
    {
        agentNumber = number;
    }

    public void setIsLeader(bool leaderState)
    {
        isLeader = leaderState;
    }

    public void setJustHandedLeadership(bool state)
    {
        justHandedLeadership = state;
    }

    public void setAgentsArray()
    {
        agents = GameObject.FindGameObjectsWithTag("MusicalAgent");
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

    public bool getIsLeader()
    {
        return isLeader;
    }

    public bool getJustHandedLeadership()
    {
        return justHandedLeadership;
    }

    public bool getisPlaying()
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

    /// <summary>
    /// Compare an agent's musical pattern utility with the leader's.
    /// </summary>
    public void compareWithLeader()
    {
        for (int i = 0; i < agents.Length; i++)
        {
            if (agents[i].GetComponent<MusicalAgent>().getIsLeader())
            {
                utility = computeUtility(musicalPattern, agents[i].GetComponent<MusicalAgent>().getMusicalPattern());
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
        if ((dl == 0 && !isLeader) || justHandedLeadership)
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
                if (A[i] != B[i])
                {
                    hammingDist += 1;
                }
            }
            return hammingDist;
        }
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
    /// Updates the current leader of the band by replacing the actual leader by the agents number i. 
    /// The current leader gets into just handed leadership mode and agents who handed leadership during the previous patter leave this mode.
    /// </summary>
    /// <param name="i">The future leader's number.</param>
    private void leaderUpdate(int i)
    {
        isLeader = false;
        bodyMeshRenderer.material.color = baseColor;
        for (int j = 0; j < agents.Length; j++)
        {
            if (agents[j].GetComponent<MusicalAgent>().getAgentNumber() == i)
            {
                agents[j].GetComponent<MusicalAgent>().setIsLeader(true);
                agents[j].GetComponent<MusicalAgent>().setLeaderColor();

            }
            else if (agents[j].GetComponent<MusicalAgent>().getJustHandedLeadership())
            {
                agents[j].GetComponent<MusicalAgent>().setJustHandedLeadership(false);
            }
        }
        if (!isLeader)
        {
            justHandedLeadership = true;
            timePlaying = 0;
        }
    }

    //Method responsible for making the 
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
                        gain = volume;
                        frequency = musicScale[random.Next(0, musicScale.Length)];
                    }
                    yield return new WaitForSeconds((float)60.0 / (2 * Sim.getBpm()));
                    Displayer.translateCursors();
                }
            break;

            //When atonal is selected the agent plays a percussion .wav when it needs to
            case SimHyperParameters.PlayMode.Percussion:
                for (int i = 0; i < musicalPattern.Length; i++)
                {
                    if (musicalPattern[i] == 1)
                    {
                        AudioSource.PlayClipAtPoint(perc, this.transform.position, 0.5f);
                    }
                    yield return new WaitForSeconds((float)60.0 / (2*Sim.getBpm()));
                    Displayer.translateCursors();
                }
            break;
        }

        isPlaying = false;
        gain = 0;
        timePlaying += 1;
        //Define the utility of the leader's musical pattern
        compareWithLeader();
        //Iterate over the other agents to try to find a candidate with a better utility
        for (int i = 0; i < agents.Length; i++)
        {
            agents[i].GetComponent<MusicalAgent>().mutation();
            agents[i].GetComponent<MusicalAgent>().compareWithLeader();
        }
        //The musical patterns are ranked by utility
        Sim.updateRankedMP();
        foreach(Canvas i in GameObject.FindObjectsOfType<Canvas>())
        {
            Destroy(i.gameObject);
        }
        //The UI shows the 5 best musical patterns
        Displayer.showMusicalPatterns();
        //The best musician gets to be the next leader
        leaderUpdate(Sim.rankedMP[0].agentNumber);
    }
}


public class NotSameSize : Exception
{
    public NotSameSize(string message) : base(message)
    {
    }
}