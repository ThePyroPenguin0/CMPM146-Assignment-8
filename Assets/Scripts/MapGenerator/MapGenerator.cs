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

    // Constraint: How big should the dungeon be at most
    // this will limit the run time (~10 is a good value 
    // during development, later you'll want to set it to 
    // something a bit higher, like 25-30)
    public int MAX_SIZE;

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
        GenerateWithBacktracking(occupied, doors, 1);
    }


    bool GenerateWithBacktracking(List<Vector2Int> occupied, List<Door> doors, int depth)
    {
        Debug.Log($"Entered GenerateWithBackTracking. Depth: {depth}");
        iterations++;
        if (depth > MAX_SIZE)
        {
            Debug.LogWarning("Reached maximum depth, stopping generation.");
            return false;
        }
        if (doors.Count == 0)
        {
            if (occupied.Count == 0) return true;
            else return false;
        }

        Door currentDoor = doors[doors.Count - 1];
        doors.RemoveAt(doors.Count - 1);

        Hallway HallwayPFB = currentDoor.IsVertical() ? vertical_hallway : horizontal_hallway;
        GameObject newHallway = HallwayPFB.Place(currentDoor);
        generated_objects.Add(newHallway);

        Vector2Int offset = Vector2Int.zero;
        switch (currentDoor.GetDirection())
        {
            case Door.Direction.NORTH:
                offset = currentDoor.GetGridCoordinates() + new Vector2Int(0, 1);
                break;
            case Door.Direction.SOUTH:
                offset = currentDoor.GetGridCoordinates() + new Vector2Int(0, -1);
                break;
            case Door.Direction.EAST:
                offset = currentDoor.GetGridCoordinates() + new Vector2Int(1, 0);
                break;
            case Door.Direction.WEST:
                offset = currentDoor.GetGridCoordinates() + new Vector2Int(-1, 0);
                break;
        }

        // Create a new room from the hallway
        List<Room> randomRooms = rooms;
        if (randomRooms == null || randomRooms.Count == 0)
        {
            throw new System.Exception("No Room prefabs assigned to the MapGenerator.rooms list!");
        }
        Room newRoomPrefab = rooms[Random.Range(0, rooms.Count)];
        bool breakOut = false;
        while (!breakOut && randomRooms.Count > 0)
        // Pick random rooms to see if they can connect
        {
            int rand = Random.Range(0, randomRooms.Count);
            foreach (Door door in randomRooms[rand].GetDoors())
            {
                if (door.GetMatchingDirection() == currentDoor.GetDirection())
                {
                    newRoomPrefab = randomRooms[rand];
                    breakOut = true;
                    break;
                }
            }
            //randomRooms.RemoveAt(rand);
        }
        GameObject newRoom = newRoomPrefab.Place(offset);
        generated_objects.Add(newRoom);
        occupied.Add(offset);
        Debug.Log($"Placed room at {offset} with {newRoomPrefab.name}");

        List<Door> newDoors = newRoomPrefab.GetDoors(offset);
        Door.Direction oppositeDirection = currentDoor.GetMatchingDirection();
        foreach (Door door in newDoors)
        {
            if (door.GetDirection() != oppositeDirection)
            {
                doors.Add(door);
            }
        }
        GenerateWithBacktracking(occupied, new List<Door>(doors), depth + 1);
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
