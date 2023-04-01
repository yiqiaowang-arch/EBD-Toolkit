/*
DesignMind: A Toolkit for Evidence-Based, Cognitively- Informed and Human-Centered Architectural Design
Copyright (C) 2023  michal Gath-Morad, Christoph Hölscher, Raphaël Baur

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskScript : MonoBehaviour
{
    // The agents executing this task will spawn at the start-location, fulfill their needs and then go to the finish-location.
    public GameObject[] start;
    public GameObject[] end;

    // Agents can fulfill their needs at so called points-of-interest.
    // The order of these POIs defines the sequence, in which they will be visited (if non-deterministic choice is off).
    public GameObject[] pointsOfInterest;       // The set of points of interest.
    public int numberOfAgents;                  // The total number of agents that will be spawned for this task.
    public float spawnInterval;                 // Time in seconds that have to pass until the next agents is spawned.
    public Color taskColor;                     // Color of this task.
    public string taskName;                     // Name of this task.
    public string agentType;

    // These values define behavioural parameters of the agent.
    public int numberOfNeeds;                   // The number of needs that need to be fulfilled until the agent will go to end.
    public float agentSpeed;                    // Speed of the agent fullfilling this task.
    public float agentSize;
    public float agentRadius;
    public float privacyRadius;

    // These attributes define the behaviour at a POI.
    public float poiTime;                       // Time that will be spent at a point of interest.

    // These attributes define how an agent chooses the next POI.
    public bool revisit;                        // If true, the agent will revisit POI.
    public bool chooseNonDeterministically;     // If true, the next POI will be chosen non-deterministically from the set of remaining POI.

    // Keeping the state of the task.
    public int agentsSpawned;                  // Number of agents that have been spawned.

    // Start is called before the first frame update
    void Start()
    {
        /* We need to check the soundness of the input values.
         * Exceptions need to be thrown, when:
         * - numberOfNeeds > #POIs && (revisit == false || chooseDeterministically == false).
         */
        if (numberOfNeeds > pointsOfInterest.Length && (!revisit || !chooseNonDeterministically)) {
            throw new System.Exception(taskName + ": Your number of points of interest is smaller than the number of needs of your agent. Enable revisit and chooseNondeterministically or provide more points of interest.");
        }

        // Remove leading and trailing whitespaces.
        agentType = agentType.Trim();
        taskName = taskName.Trim();

        agentsSpawned = 0;
    }
}
