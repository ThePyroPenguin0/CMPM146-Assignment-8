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
        Debug.Log("Entered GenerateWithBackTracking.");
        iterations++;
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
            randomRooms.RemoveAt(rand);
        }
        GameObject newRoom = newRoomPrefab.Place(offset);
        generated_objects.Add(newRoom);
        occupied.Add(offset);

        if (iterations > THRESHOLD) throw new System.Exception("Iteration limit exceeded");
        return false;

        /*
        Debug.Log("Entered GenerateWithBackTracking.");
        iterations++;
        if (doors.Count == 0)
        {
            if (occupied.Count == 0) return true;
            else return false;
        }
        Debug.Log("Breakpoint 2.");
        Door currentDoor = doors[doors.Count - 1];
        doors.RemoveAt(doors.Count - 1);

        Hallway HallwayPFB = currentDoor.IsVertical() ? vertical_hallway : horizontal_hallway;
        GameObject newHallway = HallwayPFB.Place(currentDoor);
        generated_objects.Add(newHallway);
        
        List<(Room, Vector2Int, Door)> possible = new List<(Room, Vector2Int, Door)>();
        foreach (Room room in rooms)
        {
            // Debug.Log($"Iterating through room loop. Room: {room.name}");
            foreach (Door roomDoor in room.GetDoors())
            {


                // Debug.Log($"Iterating through door loop. RoomDoor: {roomDoor}, Direction: {roomDoor.GetDirection()}, CurrentDoor: {currentDoor}, MatchingDirection: {currentDoor.GetMatchingDirection()}");
                if (roomDoor.GetDirection() == currentDoor.GetMatchingDirection())
                {   
                    Debug.Log($"CURRENTDOOR COORD:  + {currentDoor.GetGridCoordinates()}, ROOMDOOR COORD: {roomDoor.GetGridCoordinates()}");
                    Vector2Int offset = currentDoor.GetGridCoordinates() - roomDoor.GetGridCoordinates(); // Problem might be here
                    // Debug.Log($"Trying offset: {offset} for Room: {room.name}, RoomDoor: {roomDoor}");

                    bool overlap = false;
                    var candidateCoords = room.GetGridCoordinates(offset);
                    foreach (Vector2Int coordinate in candidateCoords)
                    {
                        if (occupied.Contains(coordinate))
                        {
                            overlap = true;
                            // Debug.Log($"Overlap detected at {coordinate} for Room: {room.name} with offset {offset}");
                            break;
                        }
                    }
                    if (!overlap)
                    {
                        // Debug.Log($"Possible placement: Room {room.name} at offset {offset} via RoomDoor {roomDoor}");
                        possible.Add((room, offset, roomDoor));
                    }
                }
            }
        }
        Debug.Log($"Possible rooms: {possible.Count}");

        if (possible.Count == 0)
        {
            Debug.LogWarning($"No possible placements for currentDoor {currentDoor}. Occupied: [{string.Join(", ", occupied)}]");
            doors.Add(currentDoor);
            return false;
        }

        Debug.Log("Breakpoint 4.");
        foreach (var (room, offset, roomDoor) in possible)
        {
            List<Vector2Int> newCoordinates = room.GetGridCoordinates(offset);
            occupied.AddRange(newCoordinates);

            List<Door> newDoors = room.GetDoors(offset);
            newDoors.RemoveAll(d => d.GetGridCoordinates() == roomDoor.GetGridCoordinates() && d.GetDirection() == roomDoor.GetDirection());
            List<Door> nextDoors = new List<Door>(doors);
            nextDoors.AddRange(newDoors);

            if (GenerateWithBacktracking(occupied, nextDoors, depth + 1))
            {
                generated_objects.Add(room.Place(offset));
                return true;
            }

            foreach (Vector2Int coordinate in newCoordinates)
            {
                occupied.Remove(coordinate);
            }
        }
        Debug.Log("Breakpoint 5.");
        doors.Add(currentDoor);
        if (iterations > THRESHOLD) throw new System.Exception("Iteration limit exceeded");
        return false;
        */
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
