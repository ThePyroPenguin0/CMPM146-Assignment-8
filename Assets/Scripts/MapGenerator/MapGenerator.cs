using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using JetBrains.Annotations;

public class MapGenerator : MonoBehaviour
{
    public List<Room> rooms;
    public Hallway vertical_hallway;
    public Hallway horizontal_hallway;
    public Room start;
    public Room target;
    public List<Room> deadEndRooms;
    // Constraint: How big should the dungeon be at most
    // this will limit the run time (~10 is a good value 
    // during development, later you'll want to set it to 
    // something a bit higher, like 25-30)
    public int MAX_SIZE;
    public int MIN_SIZE = 5;

    // set this to a high value when the generator works
    // for debugging it can be helpful to test with few rooms
    // and, say, a threshold of 100 iterations
    public int THRESHOLD;

    // keep the instantiated rooms and hallways here 
    private List<GameObject> generated_objects;

    int iterations;

    public void Generate()
    {
        // dispose of game objects from previous generation process
        foreach (var go in generated_objects)
        {
            Destroy(go);
        }
        generated_objects.Clear();

        generated_objects.Add(start.Place(new Vector2Int(0, 0)));
        List<Door> doors = start.GetDoors();
        List<Vector2Int> occupied = new List<Vector2Int>();
        occupied.Add(new Vector2Int(0, 0));
        iterations = 0;

        try // In space, nobody can hear your exceptions throwing
        {
            GenerateWithBacktracking(occupied, doors, 1);
        }
        catch (System.Exception ex)
        {
            if (ex.Message == "Iteration limit exceeded")
            {
                foreach (var go in generated_objects)
                {
                    Destroy(go);
                }
                generated_objects.Clear();
                Generate();
            }
            else
            {
                throw;
            }
        }
    }


    bool GenerateWithBacktracking(List<Vector2Int> occupied, List<Door> doors, int depth)
    {
        //Debug.Log($"Entered GenerateWithBackTracking. Depth: {depth}");
        iterations++;
        if (depth > MAX_SIZE)
        {
            Debug.LogWarning($"Reached maximum depth, stopping generation. Depth: {depth} Max_size: {MAX_SIZE}");
            return false;
        }
        if (doors.Count == 0)
        {
            return (occupied.Count >= MIN_SIZE);
        }

        for (int doorIdx = doors.Count - 1; doorIdx >= 0; doorIdx--)
        {
            Door currentDoor = doors[doorIdx];
            List<Door> remainingDoors = new List<Door>(doors);
            remainingDoors.RemoveAt(doorIdx);

            Vector2Int offset = Vector2Int.zero;
            switch (currentDoor.GetDirection())
            {
                case Door.Direction.NORTH: offset = currentDoor.GetGridCoordinates() + new Vector2Int(0, 1); break;
                case Door.Direction.SOUTH: offset = currentDoor.GetGridCoordinates() + new Vector2Int(0, -1); break;
                case Door.Direction.EAST:  offset = currentDoor.GetGridCoordinates() + new Vector2Int(1, 0); break;
                case Door.Direction.WEST:  offset = currentDoor.GetGridCoordinates() + new Vector2Int(-1, 0); break;
            }
            if (occupied.Contains(offset)) continue;

            List<Room> compatibleRooms = new List<Room>();

            bool isPlacingLastRoom = (occupied.Count + 1 == MAX_SIZE);
            IEnumerable<Room> roomSource = isPlacingLastRoom ? deadEndRooms : rooms;

            foreach (Room room in roomSource)
            {
                foreach (Door door in room.GetDoors())
                {
                    if (door.GetMatchingDirection() == currentDoor.GetDirection())
                    {
                        compatibleRooms.Add(room);
                        break;
                    }
                }
            }
            if (compatibleRooms.Count == 0) continue;
            if (compatibleRooms.Count == 0) continue;

            for (int i = 0; i < compatibleRooms.Count; i++)
            {
                int j = Random.Range(i, compatibleRooms.Count);
                var temp = compatibleRooms[i];
                compatibleRooms[i] = compatibleRooms[j];
                compatibleRooms[j] = temp;
            }

            foreach (Room candidate in compatibleRooms)
            {
                List<Door> candidateDoors = candidate.GetDoors(offset);
                Door.Direction oppositeDirection = currentDoor.GetMatchingDirection();
                bool adjacencyValid = true;
                foreach (Door door in candidateDoors)
                {
                    if (door.GetDirection() == oppositeDirection)
                        continue;

                    Vector2Int adjacentOffset = offset;
                    switch (door.GetDirection())
                    {
                        case Door.Direction.NORTH: adjacentOffset += new Vector2Int(0, 1); break;
                        case Door.Direction.SOUTH: adjacentOffset += new Vector2Int(0, -1); break;
                        case Door.Direction.EAST:  adjacentOffset += new Vector2Int(1, 0); break;
                        case Door.Direction.WEST:  adjacentOffset += new Vector2Int(-1, 0); break;
                    }

                    if (occupied.Contains(adjacentOffset))
                    {
                        Room adjacentRoom = null;
                        foreach (GameObject obj in generated_objects)
                        {
                            Room r = obj.GetComponent<Room>();
                            if (r != null)
                            {
                                List<Vector2Int> coords = r.GetGridCoordinates(adjacentOffset);
                                if (coords.Contains(adjacentOffset))
                                {
                                    adjacentRoom = r;
                                    break;
                                }
                            }
                        }
                        if (adjacentRoom != null)
                        {
                            Door.Direction matchingDir = door.GetMatchingDirection();
                            if (!adjacentRoom.HasDoorOnSide(matchingDir))
                            {
                                adjacencyValid = false;
                                break;
                            }
                        }
                    }
                }
                if (!adjacencyValid) continue;

                occupied.Add(offset);

                List<Door> newDoors = new List<Door>();
                foreach (Door door in candidateDoors)
                {
                    if (door.GetDirection() != oppositeDirection)
                        newDoors.Add(door);
                }
                newDoors.AddRange(remainingDoors);

                if (GenerateWithBacktracking(occupied, newDoors, depth + 1))
                {
                    Hallway HallwayPFB = currentDoor.IsVertical() ? vertical_hallway : horizontal_hallway;
                    GameObject newHallway = HallwayPFB.Place(currentDoor);
                    generated_objects.Add(newHallway);

                    GameObject newRoom = candidate.Place(offset);
                    generated_objects.Add(newRoom);

                    return true;
                }

                occupied.RemoveAt(occupied.Count - 1);
            }
        }

        if (iterations > THRESHOLD) throw new System.Exception("Iteration limit exceeded");
        return false;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        generated_objects = new List<GameObject>();
        Generate();
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.gKey.wasPressedThisFrame)
            Generate();
    }
}
