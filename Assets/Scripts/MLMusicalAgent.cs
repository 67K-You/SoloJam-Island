using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

/// <summary>
/// Subclass of MusicalAgent describing ML-Agent's reinforcement learning for its rhythmic adaptation behaviour.
/// </summary>
public class MLMusicalAgent : MusicalAgent
{
    private bool firstIteration = true;
    public override void Initialize()
    {
        Academy.Instance.AutomaticSteppingEnabled = false;
        if(Sim.behaviourMode==SimHyperParameters.BehaviourMode.ReinforcementLearning)
        {
            agents.Add(this);
            StartCoroutine(NavigatorInit());
            initialPosition = gameObject.transform.position;
            Debug.Log(agentNumber);
        } 
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if(other.transform.root.gameObject.tag=="MusicalAgent")
        {
        //Adds the agent who entered the sensing range of this musical agent to the list of agents currently perceived by this agent if they belong to the same island
        if(other.transform.root.gameObject.GetComponent<MusicalAgent>().getIslandNumber()==islandNumber && !agents.Contains(other.transform.root.gameObject.GetComponent<MLMusicalAgent>()))
        {
            agents.Add(other.transform.root.gameObject.GetComponent<MLMusicalAgent>());
            Debug.Log(agentNumber);
        }
        }
    }

    protected override void OnTriggerExit(Collider other)
    {
        if (other.transform.root.gameObject.tag == "MusicalAgent")
        {
            MusicalAgent a = other.transform.root.gameObject.GetComponent<MLMusicalAgent>();
            //Remove all occurences of the the musical agent that just left the sensing range of this agent's agents.
            agents.RemoveAll(x => x.getAgentNumber() == a.getAgentNumber());
        }
    }

    //ML Agent specific methods

    public override void OnActionReceived(float[] vectorAction)
    {
        for (int i = 0; i < vectorAction.Length;i++)
        {
            musicalPattern[i] = (int)vectorAction[i];
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        for (int i = 0; i < formerLeaderMusicalPattern.Length; i++)
        {
            sensor.AddObservation(formerLeaderMusicalPattern[i]);
        }
        for (int i = 0; i < agents.Count; i++)
        {
            if (agents[i].getIsLeader())
            {
                for (int j = 0; j < agents[i].getMusicalPattern().Length; j++)
                {
                    sensor.AddObservation(agents[i].getMusicalPattern()[j]);
                }
            }
        }
        if(justArrived)
        {
            sensor.AddObservation(1);
        }
        else
        {
            sensor.AddObservation(0);
        }
        if(Sim.adaptationPeriod!=0)
        {
            sensor.AddObservation(timeInNewIsland);
        }
        else
        {
            sensor.AddObservation(0);
        }        
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("Episode began");
        if (firstIteration)
        {
            firstIteration = false;
        }
        else
        {
            isTraveling = false;
            isLeader = false;
            justHandedLeadership = false;
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
            Array.Clear(formerLeaderMusicalPattern, 0, formerLeaderMusicalPattern.Length);
            //Randomly resets the musical pattern
            for (int i = 0; i < Sim.musicalPatternLength; i++)
            {
                musicalPattern[i] = random.Next(0, 2);
            }
            agents.Clear();
            agents.Add(this);
            gameObject.transform.position = initialPosition;
        }
    }

    //COROUTINES

    /// <summary>
    /// Coroutine responsible for making the auction process happening
    /// </summary>
    /// <returns></returns>
    protected override IEnumerator AuctionProcess()
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
            agents[i].RequestDecision();
            if(!agents[i].getJustArrived())
            {
                agents[i].compareWithLeader();
            }
            else
            {
                if(agents[i].getTimeInNewIsland()<Sim.adaptationPeriod)
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
            agents[i].SetReward(agents[i].getUtility());
            Academy.Instance.EnvironmentStep();
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

    public override void ResetAgent()
    {
        EndEpisode();
    }
}