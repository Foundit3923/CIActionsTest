/*
* Generates a random layout consisting of required rooms (if specified), a random number and quantity of rooms from a room list, and connecting hallwayTiles between them.
* Hallways are generated using A*, and Delaunay triangulation.
* Doorways are generated when a hallway passes through the edge of a room. This results in connected rooms when the rooms are touching and the hallway passes through both.
* The floor is placed first as the grid system and is in the `NavMesh` layer. All other room assets are placed separately and are in the `Obstacle` layer.
* Once everything is placed the floor is pruned of any unused tiles. The NavMeshSurface is baked into the remaining tiles with everything in the `Obstacle` layer as obstacles.
* At this point we will know every monster in the level and a Nav Mesh is created for each.
* The monsters are then spawned in the world along with any monster items.
* Finally a target portal is placed connecting the starting room with the lobby.
*/

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Graphs;
using UnityServiceLocator;
using Unity.AI.Navigation;
using UnityEngine.AI;
using Mirror;
using NightGauntStates;
using Unity.VisualScripting;
using static Unity.VisualScripting.Member;
using UnityEngine.Events;

public class GrimmGen : MonoBehaviour
{
    [SerializeField] private GameObject WorldGeometry;
    [SerializeField] private GameObject[] Geometries;
    [SerializeField] private GameObject WorldFloor = null;
    [SerializeField] GameObject _blankTile;
    [SerializeField] Vector3Int _worldSize;
    [SerializeField] int roomAttempts;
    //Rooms currently have their floors still attached
    [SerializeField] List<GameObject> _roomList, _receptionRooms, _requiredRoomsList, _hallwaysPristine, _hallwaysDestroyed, _wallsPristine, _wallsDestroyed;
    [SerializeField] GameObject _hallwayPrefab, _stairsPrefab, _markerPrefab, _emptyPrefab, _targetPortal;
    [SerializeField] Material _green, _blue, _purple, _black, _white, _darkGreen, _gray;
    [SerializeField] public bool spawnMoreThanOneLevel;
    [SerializeField] public bool CacheSources;
    [SerializeField] bool _triangulation;
    [SerializeField] bool _mst;
    [SerializeField] bool _additionalEdges;
    [SerializeField] bool _failedEdges;
    [SerializeField] bool _overlapCubes;
    [SerializeField] public static bool debug = false;
    [SerializeField] private int MaxAttempts;
    [SerializeField] private Int32 seed;
    private int attempts;
    public Tile blankTileTemplate;
    public Vector3Int _blankTileDimensions;
    public Material _originalTileMat;
    private int unitSize;

    Grid3D<Tile> grid;
    List<Tile> doors;
    List<Room> hallwaySegments = new();
    List<Room> rooms;
    List<Hallway> hallways;
    List<Tile> hallwayTiles;
    List<Tile> allTiles;
    List<Tile> allRoomTiles = new();
    List<GameObject> obstacles;
    Delaunay2D delaunay;
    HashSet<Prim.Edge> selectedEdges;
    System.Random _rand;
    BlackboardController blackboardController;
    Blackboard blackboard;
    BlackboardKey receptionRoomKey;
    int agentTypeId;

    float ProcGenBudget;

    public DifficultyManager difficultyManager;
    [SerializeField] public int budget;
    public int requiredMonsterCount;
    public BlackboardKey navMeshAgentDictionaryKey;
    public Dictionary<string, GameObject> navMeshAgentDictionary = new();
    public Dictionary<string, int> selectedNavMeshAgentDictionary = new();
    public List<(GameObject, Vector3)> monstersToSpawn = new();
    public List<InitializeItems> itemsToSpawn = new();

    public delegate void NavMeshUpdatedEvent(Bounds bounds);
    public NavMeshUpdatedEvent OnNavMeshUpdate;
    private List<NavMeshSurface> fullBakeSurfaces = new();
    private Dictionary<int, Dictionary<int, List<NavMeshBuildSource>>> SourcesPerSurface = new();
    private Dictionary<int, Dictionary<int, List<NavMeshBuildMarkup>>> MarkupsPerSurface = new();
    private Dictionary<int, Dictionary<int, List<NavMeshModifier>>> ModifiersPerSurface = new();
    private List<List<NavMeshData>> NavMeshDatas;
    private Vector3 WorldAnchor;
    private Bounds navMeshBounds;
    private int navMeshBakeRetryCount;
    private bool rebuildAll;

    private List<pairs> toDrawTriangulation = new();
    private List<pairs> toDrawMst = new();
    private List<pairs> toDrawAdditionalEdges = new();
    private List<pairs> toDrawFailedEdges = new();
    private List<cube> toDrawDoorCube = new();
    private List<cube> toDrawFreeCube = new();

    public delegate void PruneTileEvent(bool x);
    public static event PruneTileEvent OnPruneTile;

    public UnityEvent OnEventTriggered;

    private Utility utils;

    private FizzyNetworkManager manager;
    private FizzyNetworkManager Manager
    {
        get
        {
            if (manager != null)
            {
                return manager;
            }

            if (FizzyNetworkManager.singleton != null)
            {
                return manager = FizzyNetworkManager.singleton as FizzyNetworkManager;
            }

            return ServiceLocator.For(this).Get<FizzyNetworkManager>();
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    //                Collider[] hitColliders = Physics.OverlapBox(t.self.GetComponentInChildren<Target>().transform.position, new Vector3(unitSize,unitSize,unitSize)/2, Quaternion.identity, LayerMask.NameToLayer("Prop"));
    public class cube
    {
        public Vector3 position;
        public Vector3 size;
        public cube(Vector3 _position, Vector3 _size)
        {
            this.position = _position;
            this.size = _size;
        }
    }

    public class pairs
    {
        public Vector2 a, b;
        public pairs(Vector2 _a, Vector2 _b)
        {
            a = _a;
            b = _b;
        }
    }

    public static void PrintDebug<T>(T msg)
    {
        if (debug)
        {
            Debug.Log(msg);
        }
    }
    public enum WallType
    {
        Plain,
        Door,
        None
        //Window
    }
    public enum Status
    {
        Pristine,
        Destroyed,
        None
    }
    public enum CellType
    {
        None,
        Room,
        Hallway,
        Stairs,
        Edge,
        Door,
        Restricted
    }
    public class Room
    {
        public BoundsInt bounds;
        public List<Tile> tiles = new();
        public Vector3Int location;
        public List<Vector3Int> positions = new();
        public Dictionary<string, Tile> edgeDirections = new();
        public Connections Connections = new();
        public List<Prop> props = new();
        public Dictionary<string, List<(Room, Tile)>> roomConnections = new();
        public Dictionary<string, Dictionary<Room, List<Tile>>> availableRoomConnections = new();
        public Dictionary<string, Dictionary<string, int>> availableRoomConnectionCounts = new();
        public GameObject roomPrefab;
        public Vector3Int roomUnitSize;
        public Vector3Int roomWorldSize;
        public int unitSize;
        public bool outOfBounds;
        public bool isHallway = false;
        public Material material;
        public TileProperties properties;

        public Room(Vector3Int location, Vector3Int size, Grid3D<Tile> grid, GameObject roomPrefab, int unitSize, Material mat, GrimmGen gg = null) => InitializeRoom(location, size, grid, roomPrefab, unitSize, mat, gg);

        public void InitializeRoom(Vector3Int location, Vector3Int size, Grid3D<Tile> grid, GameObject roomPrefab, int unitSize, Material mat, GrimmGen gg = null)
        {
            this.outOfBounds = false;
            this.unitSize = unitSize;
            this.roomUnitSize = size;
            this.roomWorldSize = size * unitSize;
            this.location = location;
            this.material = mat;
            PrintDebug($"Location: {location}");
            PrintDebug($"Size: {roomWorldSize}");
            this.bounds = new BoundsInt();
            this.bounds.SetMinMax(location, location + this.roomWorldSize);
            this.tiles = GetTilesInRoom(grid, this.unitSize, gg);
            this.roomPrefab = roomPrefab;
            this.properties = this.roomPrefab.GetComponent<TileProperties>();
        }

        public void Deregister(Vector3Int blankTileDimensions)
        {
            this.positions.Clear();
            this.tiles.Clear();
            this.bounds = new BoundsInt();
            this.properties = null;
            this.roomPrefab = null;
            this.edgeDirections.Clear();
            this.roomConnections.Clear();
            this.availableRoomConnections.Clear();
            this.availableRoomConnectionCounts.Clear();

        }

        public List<Tile> GetTilesInRoom(Grid3D<Tile> grid, int unitSize, GrimmGen gg = null)
        {
            List<Tile> result = new();
            //if room dimensions are in bounds
            if (this.bounds.xMin >= 0 && this.bounds.yMin >= 0 && this.bounds.zMin >= 0 &&
                this.bounds.xMax < grid.Size.x && this.bounds.yMax <= grid.Size.y && this.bounds.zMax < grid.Size.z)
            {
                for (float i = this.bounds.xMin; i < this.bounds.xMax; i += unitSize)
                {
                    for (float j = this.bounds.zMin; j < this.bounds.zMax; j += unitSize)
                    {
                        for (float k = this.bounds.yMin; k < this.bounds.yMax; k += unitSize)
                        {
                            Vector3Int location = new((int)i, (int)k, (int)j);
                            //Check to make sure this isn't including air space
                            //Set the tile type to Room
                            PrintDebug(location);
                            if (grid.InBounds(location))
                            {
                                Tile tile = grid[location];
                                if (tile != null)
                                {
                                    result.Add(tile);
                                    positions.Add(location);

                                }
                                else
                                {
                                    this.outOfBounds = true;
                                    if (gg != null)
                                    {
                                        foreach (Transform child in gg.WorldFloor.transform)
                                        {
                                            TileProperties props = child.GetComponent<TileProperties>();
                                            if (props.WorldPosition == location)
                                            {
                                                tile = grid[location];
                                                throw new Exception("Tiles are not being stored in grid");
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                throw new Exception($"Invalid location that should be valid: {location}");
                            }
                            //Add loop of _blankTileDimensions.y to include airspace in internal positions?
                        }
                    }
                }
            }
            else
            {
                this.outOfBounds = true;
            }

            return result;
        }

        public void identifyTileCellType(System.Random _rand, Material darkGreen, Material white, Material purple)
        {
            //Initialiaze
            this.availableRoomConnections ??= new();
            this.availableRoomConnectionCounts ??= new();
            //make a list of rooms to exclude from finding connections
            //These are rooms that this room has already connected to
            List<Room> roomsToIgnore = new();
            foreach (string dir in this.roomConnections.Keys) //Get all connection in every direction
            {
                foreach ((Room, Tile) value in this.roomConnections[dir])
                {
                    roomsToIgnore.Add(value.Item1);
                }
            }
            //Get walls from the prefab
            List<GameObject> wallsList = this.properties.walls;
            foreach (Tile tile in this.tiles)//Check all tiles that belong to the room
            {
                foreach (string neighborDir in GrimmGen.directions)//tile.neighborTiles.Keys)//for each neighbor direction check if that neighbor belongs to the same room as this tile
                {
                    if (tile.neighborTiles.TryGetValue(neighborDir, out Tile neighbor)) //If there is a neighbor in the current direction, store it in neighbor
                    {
                        if (this.tiles.Contains(neighbor))  //no walls within rooms
                        {
                            //All room tiles are preassigned CellType.Room so we don't need to change anything and just need to set the wall.
                            tile.walls[neighborDir] = (wallsList[(int)GrimmGen.WallType.None], GrimmGen.WallType.None, tile._status);
                            continue;
                        }
                        else if (neighbor.parent != null && neighbor._type == CellType.Door)
                        {
                            if (!roomsToIgnore.Contains(neighbor.parent))
                            {
                                //Set the typing, material, and wall information
                                tile.SetType(CellType.Door);
                                //tile.self.gameObject.GetComponentInChildren<Renderer>().material = purple;
                                tile.properties.RegisterMaterialCallback();
                                tile.properties.RegisterTypeCallback();
                                tile.walls[neighborDir] = (wallsList[(int)GrimmGen.WallType.Door], GrimmGen.WallType.Door, tile._status);
                                //add the connection to the list of known connections
                                if (!this.roomConnections.ContainsKey(neighborDir))
                                {
                                    this.roomConnections[neighborDir] = new();
                                }

                                this.roomConnections[neighborDir].Add((neighbor.parent, tile));
                                roomsToIgnore.Add(neighbor.parent);
                            }

                            continue;
                        }

                        if (tile._type != CellType.Door)
                        {
                            if (neighbor.parent != null && neighbor._type != CellType.None && neighbor._type != CellType.Hallway) //Check if the neighbor has the potential to be a connection
                            {
                                if (neighbor._type == CellType.None)
                                {
                                    throw new Exception("None type tile made it through");
                                }

                                //all neighbors that make it to this point are not part of the current room
                                //add all edge tiles with neighbors that are not part of this tile's room
                                this.edgeDirections[neighborDir] = tile;

                                if (!this.availableRoomConnections.ContainsKey(neighborDir)) //Check if availableRoomConnections already contains the key for the current direction. if no initialize the neighbor's parent room as well
                                {
                                    this.availableRoomConnections[neighborDir] = new()
                                    {
                                        [neighbor.parent] = new() //initialize the list
                                    }; //initialize the dictionary
                                }
                                else if (!this.availableRoomConnections[neighborDir].ContainsKey(neighbor.parent)) //Check if availableRoomConnections[neighborDir] already contains the key for neighbor.parent
                                {
                                    this.availableRoomConnections[neighborDir][neighbor.parent] = new(); //initialize the list
                                }

                                if (!this.availableRoomConnectionCounts.ContainsKey(neighbor.parent.roomPrefab.name)) //Check if the connection counts include a reference to the current neighbor's parent room. If no initialize the direction as well
                                {
                                    this.availableRoomConnectionCounts[neighbor.parent.roomPrefab.name] = new()
                                    {
                                        [neighborDir] = 0 //initialize integer
                                    }; //initialize dicitonary
                                }
                                else if (!this.availableRoomConnectionCounts[neighbor.parent.roomPrefab.name].ContainsKey(neighborDir)) //Check if the connection counts for this room already contains the current direction
                                {
                                    this.availableRoomConnectionCounts[neighbor.parent.roomPrefab.name][neighborDir] = 0; //initialize integer
                                }

                                this.availableRoomConnections[neighborDir][neighbor.parent].Add(tile);
                                this.availableRoomConnectionCounts[neighbor.parent.roomPrefab.name][neighborDir] += 1;
                            }
                            //All tiles that make it this far are edge tiles
                            tile.SetType(CellType.Edge);
                            //tile.self.gameObject.GetComponentInChildren<Renderer>().material = darkGreen;
                            tile.walls[neighborDir] = (wallsList[(int)GrimmGen.WallType.Plain], GrimmGen.WallType.Plain, tile._status);
                        }
                    }
                    else
                    {
                        //no neighbor in a direction means it's out of bounds. make this tile an edge type
                        tile.SetType(CellType.Edge);
                        //tile.self.gameObject.GetComponentInChildren<Renderer>().material = darkGreen;
                        tile.walls[neighborDir] = (wallsList[(int)GrimmGen.WallType.Plain], GrimmGen.WallType.Plain, tile._status); //Will overwrite old entries if any exist
                    }
                }
            }
            //Rules for joining rooms together
            //If Room can connect to another room and it has not connected to that room
            //Ensure only one connection between rooms per direction
            Dictionary<string, Dictionary<Room, List<Tile>>> toRemove = new();
            List<string> connectionkeys = this.availableRoomConnections.Keys.ToList();
            foreach (string directionKey in connectionkeys) //Cycle through the available directions that a connection could be in
            {
                List<Room> directionKeys = this.availableRoomConnections[directionKey].Keys.ToList();
                foreach (Room room in directionKeys) //Cycle through each room that can be connected to in the current direction
                {
                    if (roomsToIgnore.Contains(room)) //make sure there are no other connections to this room
                    {
                        continue; //go to next room
                    }

                    if (!this.roomConnections.ContainsKey(directionKey)) //make sure everything is intialized
                    {
                        this.roomConnections[directionKey] = new();
                    }

                    if (this.availableRoomConnectionCounts.ContainsKey(room.roomPrefab.name)) //Check if there is a count for the current room
                    {
                        if (this.availableRoomConnectionCounts[room.roomPrefab.name].ContainsKey(directionKey)) //if the current room connection count has a connection in the current direction
                        {
                            int possibleConnectionPoints = this.availableRoomConnectionCounts[room.roomPrefab.name][directionKey]; //Get the count, determine where to put the door, and set the door on that tile
                            if (possibleConnectionPoints > 0)
                            {
                                Tile selectedTile;
                                if (possibleConnectionPoints > 1)
                                {
                                    int selectedConnectionPoint = _rand.Next(0, possibleConnectionPoints);
                                    selectedTile = this.availableRoomConnections[directionKey][room][selectedConnectionPoint];
                                }
                                else
                                {
                                    selectedTile = this.availableRoomConnections[directionKey][room].FirstOrDefault();
                                }

                                foreach (string direction in availableRoomConnections.Keys)
                                {
                                    if (availableRoomConnections[direction].ContainsKey(room))
                                    {
                                        availableRoomConnections[direction].Remove(room);
                                    }
                                }

                                roomsToIgnore.Add(room);
                                selectedTile.SetType(CellType.Door);
                                selectedTile.walls[directionKey] = (wallsList[(int)GrimmGen.WallType.Door], GrimmGen.WallType.Door, selectedTile._status);
                                //selectedTile.self.GetComponentInChildren<Renderer>().material = purple;
                                this.roomConnections[directionKey].Add((room, selectedTile));
                            }
                        }
                    }
                }
            }
        }

        public static bool Intersect(Room a, Room b) => a.tiles.Intersect(b.tiles).Count() > 0;
    }
    public class Tile
    {
        public CellType _type;
        public Vector3Int _worldLocation;
        public Vector3Int _size;
        public GameObject self;
        public Room parent;
        public Hallway parentHallway;
        public TileProperties properties;
        public GrimmGen.Status _status;
        public Dictionary<string, (GameObject, GrimmGen.WallType, GrimmGen.Status)> walls = new();
        public Dictionary<string, Tile> neighborTiles = new();

        public Tile(CellType type, Vector3Int worldLocation, Vector3Int size, GameObject go)
        {
            _type = type;
            _worldLocation = worldLocation;
            _size = size;
            self = go;
            properties = self.GetComponent<TileProperties>();
            properties.CellType = _type;
        }

        public void addNeighbor(Tile tile, string key) => neighborTiles[key] = tile;

        public void SetType(CellType type)
        {
            _type = type;
            properties.CellType = _type;
        }
    }

    public class ConnectionDict
    {
        private readonly IDictionary<int, List<Tile>> directionTilePairs = new Dictionary<int, List<Tile>>();

        public List<Tile> this[int direction]
        {
            get => directionTilePairs[direction];
            set
            {
                if (!directionTilePairs.ContainsKey(direction))
                {
                    directionTilePairs.Add(direction, value);
                }
            }
        }
    }

    public struct connection
    {
        public Room Room;
        public Hallway Hallway;
        public ConnectionDict directionTilePairs;
        public connection(Room _firstLevelRoom, Tile _tile, int _direction)
        {
            Room = _firstLevelRoom;
            Hallway = null;
            directionTilePairs = new();
            List<Tile> tileList = new() { _tile };
            directionTilePairs[_direction] = tileList;
        }
        public connection(Hallway _firstLevelHallway, Tile _tile, int _direction)
        {
            Room = null;
            Hallway = _firstLevelHallway;
            directionTilePairs = new();
            List<Tile> tileList = new() { _tile };
            directionTilePairs[_direction] = tileList;
        }
    }

    public class Connections
    {
        public List<connection> FirstLevel; //Directly connected to source
        public List<connection> SecondLevel; //Connected to source via first level connection
        public List<connection> ThirdLevel; //Connected to source via second level connection

        public List<connection> Overlapping; //Connections that overlap with first level connections even if they are also first level connections

        public void AddFirstLevelConnection(connection firstLevelConnection)
        {
            if (!FirstLevel.Contains(firstLevelConnection))
            {
                FirstLevel.Add(firstLevelConnection);
                UpdateConnections(firstLevelConnection);
            }
        }
        public void AddSecondLevelConnection(connection secondLevelConnection) => SecondLevel.Add(secondLevelConnection);
        public void AddThirdLevelConnection(connection thirdLevelConnection) => ThirdLevel.Add(thirdLevelConnection);
        public void UpdateConnections(connection newFirstLevelConnection)
        {
            if (newFirstLevelConnection.Room != null)
            {
                //Add second level room connections
                foreach (connection secondLevelConnection in newFirstLevelConnection.Room.Connections.FirstLevel)
                {
                    AddSecondLevelConnection(secondLevelConnection);
                    FirstLevel.ForEach(c =>
                    {
                        if (c.Room.Equals(secondLevelConnection.Room))
                        {
                            Overlapping.Add(secondLevelConnection);
                        }
                    });
                    //Add third level room connections
                    foreach (connection thirdLevelConnection in secondLevelConnection.Room.Connections.FirstLevel)
                    {
                        AddThirdLevelConnection(thirdLevelConnection);
                        FirstLevel.ForEach(c =>
                        {
                            if (c.Room.Equals(thirdLevelConnection.Room))
                            {
                                Overlapping.Add(thirdLevelConnection);
                            }
                        });
                    }
                }
                //Add second level hallway connections
                foreach (connection secondLevelConnection in newFirstLevelConnection.Hallway.Connections.FirstLevel)
                {
                    AddSecondLevelConnection(secondLevelConnection);
                    //Add third level room connections
                    foreach (connection thirdLevelConnection in secondLevelConnection.Room.Connections.FirstLevel)
                    {
                        AddThirdLevelConnection(thirdLevelConnection);
                        FirstLevel.ForEach(c =>
                        {
                            if (c.Room.Equals(thirdLevelConnection.Room))
                            {
                                Overlapping.Add(thirdLevelConnection);
                            }
                        });
                    }
                }
            }
            else if (newFirstLevelConnection.Hallway != null)
            {
                //This should never run
                foreach (connection connection in newFirstLevelConnection.Hallway.Connections.FirstLevel)
                {
                    AddSecondLevelConnection(connection);
                }
            }
        }
    }

    public class Hallway
    {
        public BoundsInt bounds;
        public List<HallwayTile> hallwayTiles = new();
        public List<Tile> tiles = new();
        public Vector3Int location;
        public List<Vector3Int> positions = new();
        public Dictionary<string, Tile> edgeDirections = new();
        public Connections Connections = new();
        public Dictionary<Room, List<Tile>> roomConnections = new();
        public Dictionary<Room, Dictionary<int, List<Tile>>> availableRoomConnections = new();
        public Vector3Int roomUnitSize;
        public Vector3Int roomWorldSize;
        public int minX;
        public int maxX;
        public int minZ;
        public int maxZ;
        public int unitSize;
        public Material material;
        public GameObject hallwayPrefab;

        public Hallway(GameObject placeholderPrefab, int unitSize, Material mat = null) => InitializeHallway(placeholderPrefab, unitSize, mat);

        public void InitializeHallway(GameObject placeholderPrefab, int unitSize, Material mat)
        {
            this.hallwayPrefab = placeholderPrefab;
            this.unitSize = unitSize;
            this.material = mat;
            this.bounds = new BoundsInt();
        }

        public void AddHallwaySegment(HallwayTile hallwaySegment)
        {
            hallwaySegment.tile.parentHallway = this;
            this.hallwayTiles.Add(hallwaySegment);
            this.tiles.Add(hallwaySegment.tile);
            Vector3Int graphLocation = hallwaySegment.tile._worldLocation / unitSize;
            this.positions.Add(graphLocation);
            if (graphLocation.x < minX)
            {
                minX = graphLocation.x;
            }
            else if (graphLocation.x > maxX)
            {
                maxX = graphLocation.x;
            }

            if (graphLocation.z < minZ)
            {
                minZ = graphLocation.z;
            }
            else if (graphLocation.z > maxZ)
            {
                maxZ = graphLocation.z;
            }
        }

        public void CalculateBounds() => bounds.SetMinMax(new Vector3Int(minX, unitSize, minZ), new Vector3Int(maxX, unitSize, maxZ));

        public void identifyTileCellType(System.Random _rand, Material blue, Material darkGreen, Material white, Material purple)
        {
            this.availableRoomConnections ??= new(); //initialize the dictionary
            //Get walls from the prefab
            List<GameObject> wallsList;
            foreach (Tile tile in this.tiles)//Check all tiles that belong to the Hallway
            {
                wallsList = tile.properties.walls;
                for (int i = 0; i < GrimmGen.directions.Length; i++)//for each neighbor direction check if that neighbor belongs to the same room as this tile
                {
                    string neighborDir = GrimmGen.directions[i];
                    //if tile in direction
                    if (tile.neighborTiles.TryGetValue(neighborDir, out Tile neighbor))
                    {
                        if (this.tiles.Contains(neighbor))  //no walls within rooms
                        {
                            tile.walls[neighborDir] = (wallsList[(int)GrimmGen.WallType.None], GrimmGen.WallType.None, tile._status);
                            continue;
                        }
                        //add all edge tiles with neighbors that are not part of this tile's room
                        this.edgeDirections[neighborDir] = tile;
                        if (neighbor.parent != null && neighbor._type != CellType.None && neighbor._type != CellType.Hallway) //Check if the neighbor has the potential to be a connection
                        {
                            //all neighbors that make it to this point are not part of the current room
                            //add all edge tiles with neighbors that are not part of this tile's room
                            this.edgeDirections[neighborDir] = tile;

                            if (!this.availableRoomConnections.ContainsKey(neighbor.parent)) //Check if availableRoomConnections already contains the key for the current direction. if no initialize the neighbor's parent room as well
                            {
                                this.availableRoomConnections[neighbor.parent] = new()
                                {
                                    [i] = new() //initialize the list
                                }; //initialize the dictionary
                            }
                            else if (!this.availableRoomConnections[neighbor.parent].ContainsKey(i)) //Check if availableRoomConnections[neighborDir] already contains the key for neighbor.parent
                            {
                                this.availableRoomConnections[neighbor.parent][i] = new(); //initialize the list
                            }

                            this.availableRoomConnections[neighbor.parent][i].Add(tile);
                            //this.availableRoomConnectionCounts[neighbor.parent.roomPrefab.name] += 1; //this will always be initialized to an integer
                        }
                        //All Hallway tiles are edges and should have plain walls
                        tile.SetType(CellType.Edge);
                        //tile.self.gameObject.GetComponentInChildren<Renderer>().material = blue;
                        //Set these as plain walls initially
                        tile.walls[neighborDir] = (wallsList[(int)GrimmGen.WallType.Plain], GrimmGen.WallType.Plain, tile._status);
                    }
                    else
                    {
                        //no neighbor in a direction means it's out of bounds. make this tile an edge type
                        tile.SetType(CellType.Edge);
                        //tile.self.gameObject.GetComponentInChildren<Renderer>().material = blue;
                        tile.walls[neighborDir] = (wallsList[(int)GrimmGen.WallType.Plain], GrimmGen.WallType.Plain, tile._status); //Will overwrite old entries if any exist
                    }
                }
            }
            //Rules for connecting hallways to rooms
            //if hallway can connect two or more rooms
            ////if rooms are already connected
            //////if all rooms are already directly connected
            ////////if can't place doors 5 or more tiles away from existing doors
            //////////orphan hallway
            //////else
            ////////attempt to place hallways 5 or more tiles away from existing doors (default to as far as possible)
            ////else
            //////place one door for each room
            //If Room can connect to another room and it has not connected to that room
            //Ensure only one connection between rooms per direction

            //Create list of rooms that directly connect to each other that are also potential rooms to connect to
            List<Room> connectionsNeeded = new();
            int n = this.availableRoomConnections.Keys.Count();

            foreach (Room roomToConnect in this.availableRoomConnections.Keys)//cycle through rooms the hallway shares a wall with
            {
                List<Room> connections = new();
                foreach (string directionkey in roomToConnect.roomConnections.Keys)//cycle through which rooms each room already has a connection to
                {
                    foreach ((Room, Tile) connection in roomToConnect.roomConnections[directionkey])
                    {
                        if (this.availableRoomConnections.ContainsKey(connection.Item1))
                        {
                            //If the room has a connection to another room that the hallway has a connection to, add it to the list of preexisting connections
                            //these are the rooms that do not need doors
                            if (!connections.Contains(connection.Item1))
                            {
                                connections.Add(connection.Item1);
                            }
                        }
                    }
                }

                if (connections.Count() != this.availableRoomConnections.Keys.Count() - 1)
                {
                    //if there are any prexisting connections for this room
                    //connectionsNeeded.AddRange(connections);
                    foreach (Room room in this.availableRoomConnections.Keys.Except(connections))//cycle through all the rooms that need doors
                    {
                        if (!connectionsNeeded.Contains(room))
                        {
                            connectionsNeeded.Add(room);//add them to the list of rooms that need doors
                        }
                    }
                }
            }

            foreach (Room roomToConnect in this.availableRoomConnections.Keys)//Cycle through all the rooms the hallway can connect to
            {

                if (connectionsNeeded.Contains(roomToConnect))//only try to connect to the room if it needs a door
                {
                    foreach (int direction in this.availableRoomConnections[roomToConnect].Keys)
                    {
                        if (this.availableRoomConnections[roomToConnect][direction].Count() > 0)
                        {
                            Tile selectedTile;
                            if (!this.roomConnections.ContainsKey(roomToConnect))
                            {
                                this.roomConnections[roomToConnect] = new();
                            }

                            if (this.availableRoomConnections[roomToConnect][direction].Count() > 1)
                            {
                                int selectedConnectionPoint = _rand.Next(0, this.availableRoomConnections[roomToConnect][direction].Count());
                                selectedTile = this.availableRoomConnections[roomToConnect][direction][selectedConnectionPoint];
                            }
                            else
                            {
                                selectedTile = this.availableRoomConnections[roomToConnect][direction].FirstOrDefault();
                            }
                            //Get the direction that the door should be facing by reversing the original direction
                            string directionKey = GrimmGen.directions[direction];//GrimmGen.directions[GrimmGen.reverseOrientation[selectedTile.Item2]];
                            //Set the wall in the correct direction as a door
                            selectedTile.walls[directionKey] = (selectedTile.properties.walls[(int)GrimmGen.WallType.Door], GrimmGen.WallType.Door, selectedTile._status);
                            //Change tile type to door
                            selectedTile.SetType(CellType.Door);
                            //selectedTile._type = CellType.Door;
                            //Change the tile color to purple
                            //selectedTile.self.GetComponentInChildren<Renderer>().material = white;
                            //Add the connection to the hallway's list of connections
                            this.roomConnections[roomToConnect].Add(selectedTile);
                            //adjust the neighboring tile to have the correct typing and wall
                            Tile neighbor = selectedTile.neighborTiles[directionKey];
                            PrintDebug($"connection neighbor cell type: {neighbor._type}");
                            neighbor.walls[GrimmGen.directions[reverseOrientation[direction]]] = (neighbor.parent.properties.walls[(int)GrimmGen.WallType.Door], GrimmGen.WallType.Door, neighbor._status);
                            neighbor.SetType(CellType.Door);
                            //neighbor._type = CellType.Door;
                            // neighbor.self.GetComponentInChildren<Renderer>().material = white;
                        }
                    }
                }
            }
        }
    }

    public class HallwayTile
    {
        public GameObject hallwayPrefab;
        public Tile tile;
        public TileProperties properties;

        public HallwayTile(GameObject hallwayPrefab, Tile hallwayTile)
        {
            this.hallwayPrefab = hallwayPrefab;
            this.tile = hallwayTile;
            this.properties = this.hallwayPrefab.GetComponent<TileProperties>();
            tile.properties = this.properties;

        }
    }

    public class Prop
    {
        public GameObject PropGameObject;
        public Vector3 PropOrientation;
        public bool corner;

        public Prop(GameObject propGameObject, Vector3 propOrientation, bool isCorner)
        {
            PropGameObject = propGameObject;
            PropOrientation = propOrientation;
            corner = isCorner;
        }
    }

    private static readonly Vector3Int[] GNeighbors = {
        new(1, 0, 0), //north
        new(-1, 0, 0), //south
        new(0, 0, 1), //east
        new(0, 0, -1) //west
    };

    private static readonly int[] reverseOrientation =
    {
        1,
        0,
        3,
        2
    };

    private Vector3Int[] neighbors = new Vector3Int[GNeighbors.Length];

    static readonly string[] directions =
    {
        "East",
        "West",
        "North",
        "South"
    };

    public static List<T> NewSizedList<T>(int n, T v = default(T))
    {
        List<T> ret = new(n);
        ret.AddRange(Enumerable.Repeat(v, n));
        return ret;
    }
    private void Awake()
    {
        ServiceLocator.Global.Register(this, true);
        utils = ServiceLocator.Global.Get<Utility>();
        //Instantiate the world geometry prefab
        //TODO: add this to the initialization manager and store in blackboard
        WorldGeometry = Instantiate(WorldGeometry);

        navMeshBounds = new Bounds(new Vector3(_worldSize.x * unitSize / 2, _worldSize.y * unitSize / 2, _worldSize.z * unitSize / 2), _worldSize * unitSize);

        int count = Manager.enemySpawnPrefabs.Count;
        //NavMeshDatas = new NavMeshData[Geometries.Length][12];
        NavMeshDatas = NewSizedList<List<NavMeshData>>(Geometries.Length);
        int index = 0;
        Geometries[index] = Instantiate(Geometries[index]);
        int existingNavMeshSurfaces = Geometries[index].gameObject.GetComponents<NavMeshSurface>().Length;
        int navMeshSurfacesToAdd = Manager.enemySpawnPrefabs.Count - existingNavMeshSurfaces;
        for (int surface = existingNavMeshSurfaces; surface <= navMeshSurfacesToAdd; surface++)
        {
            Geometries[index].AddComponent<NavMeshSurface>();
        }

        Geometries[index].transform.SetParent(WorldGeometry.transform, false);
        Geometries[1] = Instantiate(Geometries[1]);
        foreach (NavMeshSurface surface in Geometries[1].GetComponents<NavMeshSurface>())
        {
            Destroy(surface);
        }

        Geometries[1].transform.SetParent(WorldGeometry.transform, false);

        NavMeshDatas[index] = NewSizedList<NavMeshData>(Manager.enemySpawnPrefabs.Count);
        NavMeshSurface[] navMeshSurfaceList = Geometries[index].gameObject.GetComponents<NavMeshSurface>();
        for (int j = 0; j < navMeshSurfaceList.Length; j++)
        {
            //Looking for issues with stuff being added to MarkupsPerSurface

            navMeshSurfaceList[j].agentTypeID = -1;
            navMeshSurfaceList[j].useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navMeshSurfaceList[j].collectObjects = CollectObjects.MarkedWithModifier;
            NavMeshDatas[index][j] = new NavMeshData();
            //NavMesh.AddNavMeshData(NavMeshDatas[index][surfaceIndex]);
            var test = new Dictionary<int, List<NavMeshBuildSource>>();
            if (SourcesPerSurface.ContainsKey(index))
            {
                SourcesPerSurface[index].Add(j, new List<NavMeshBuildSource>());
            }
            else
            {
                test.Add(j, new List<NavMeshBuildSource>());
                SourcesPerSurface.Add(index, test);
            }

            if (MarkupsPerSurface.ContainsKey(index))
            {
                MarkupsPerSurface[index].Add(j, new List<NavMeshBuildMarkup>());
            }
            else
            {
                var test1 = new Dictionary<int, List<NavMeshBuildMarkup>>
                {
                    { j, new List<NavMeshBuildMarkup>() }
                };
                MarkupsPerSurface.Add(index, test1);
            }

            if (ModifiersPerSurface.ContainsKey(index))
            {
                ModifiersPerSurface[index].Add(j, new List<NavMeshModifier>());
            }
            else
            {
                var test2 = new Dictionary<int, List<NavMeshModifier>>
                {
                    { j, new List<NavMeshModifier>() }
                };
                ModifiersPerSurface.Add(index, test2);
            }
        }
    }
    void Start()
    {
        blackboardController = ServiceLocator.For(this).Get<BlackboardController>();
        blackboard = blackboardController.GetBlackboard();
        receptionRoomKey = blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.Reception);
        navMeshAgentDictionaryKey = blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.NavMeshAgentDict);
        blackboard.TryGetValue(blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.WorldFloor), out WorldFloor);
        difficultyManager = ServiceLocator.For(this).Get<DifficultyManager>();
        blackboard.TryGetValue(blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.WorldFloor), out WorldFloor);
        _rand = new System.Random();
        navMeshBakeRetryCount = 0;
        GenerateLevel();
    }
    public void GenerateLevel()
    {
        ProcGenBudget = difficultyManager.ProcGenBudget();
        int totalTiles = 50 * 50;
        float roomTiles = totalTiles * (ProcGenBudget / 100);
        float Budget = difficultyManager.GameplaySettings.Budget;
        float MonsterBudget = Budget * difficultyManager.GameplaySettings.MonsterAllocationPercentage;
        float possibleMonsterCount = MonsterBudget / difficultyManager.GameplaySettings.MonsterPointValue;
        float monstersPerRoomTiles = roomTiles / possibleMonsterCount;
        allTiles ??= new();
        //Initialize grid
        blackboard ??= ServiceLocator.For(this).Get<BlackboardController>().GetBlackboard();
        if (blackboard.TryGetValue(blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.RandomSeed), out seed))
        {
            _rand = new System.Random(seed);
            seed = _rand.Next(seed);
        }
        else
        {
            GameplaySettings updateSettings = difficultyManager.GameplaySettings;
            seed = (Int32)Environment.TickCount;
            updateSettings.Seed = seed;
            difficultyManager.SetValues(updateSettings.GetValues());
            _rand = new System.Random(seed);

        }

        //Get base dimensions from blankTile
        GameObject go = Instantiate(_blankTile, Vector3.zero, Quaternion.identity);
        Renderer r = go.GetComponentInChildren<Renderer>();
        Vector3 dimensions = r.bounds.size;
        _originalTileMat = r.material;

        if (dimensions.x > dimensions.y)
        {
            if (dimensions.z > dimensions.x)
            {
                dimensions.x = dimensions.z;
                dimensions.y = dimensions.z;
            }
            else
            {
                dimensions.z = dimensions.x;
                dimensions.y = dimensions.x;
            }
        }
        else
        {
            if (dimensions.z > dimensions.y)
            {
                dimensions.x = dimensions.z;
                dimensions.y = dimensions.z;
            }
            else
            {
                dimensions.z = dimensions.y;
                dimensions.x = dimensions.y;
            }
        }

        unitSize = (int)dimensions.x;
        float unitSizef = (float)unitSize;
        if (!Mathf.Approximately(unitSizef, dimensions.x))
        {
            if (Mathf.Approximately(unitSizef + 1f, dimensions.x))
            {
                unitSize++;
            }
            else if (Mathf.Approximately(unitSizef - 1f, dimensions.x))
            {
                unitSize--;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        for (int i = 0; i < neighbors.Length; i++)
        {
            neighbors[i] = GNeighbors[i] * unitSize;
        }

        _blankTileDimensions = new Vector3Int((int)dimensions.x, (int)dimensions.y, (int)dimensions.z);
        //_blankTileDimensions.y = _blankTileDimensions.x;
        //Destroy reference blankTile
        Destroy(go);
        grid = new Grid3D<GrimmGen.Tile>(_worldSize * _blankTileDimensions, Vector3Int.zero);
        rooms = new List<Room>();
        hallways = new List<Hallway>();
        hallwayTiles = new List<Tile>();

        //Place all tiles for world
        //x is width
        //z is depth
        //y is height
        //x and z are the horizontal axis and can be seen as the x and y on a 2D graph
        //y is the vertical axis
        //because we want the grid to indicate the center of a tile in all three axis y needs to start in the center of the space
        //So `k` starts at _worldSize.y and the limit is increase by the same amount
        //Offset x and z by half to compensate for the tiles locations being in the center
        GenFloor(WorldFloor);

        GenLevel();
    }

    private void GenFloor(GameObject floorPrefab = null)
    {
        if (floorPrefab == null)
        {
            bool breakout = false;
            for (float i = 0; i < (_worldSize.x * unitSize); i += unitSize)
            {
                for (float j = 0; j < (_worldSize.z * unitSize); j += unitSize)
                {
                    for (float k = 0; k < (_worldSize.y * unitSize); k += unitSize)
                    {
                        //Place blankTile at location and add to grid
                        Vector3Int location = new((int)i, (int)k, (int)j);
                        if (spawnMoreThanOneLevel)
                        {
                            GameObject tile = PlaceTile(_blankTile, location, Vector3Int.one);
                            grid[location] = new Tile(CellType.None, location, _blankTileDimensions, tile);
                            grid[location].SetType(CellType.None);
                            tile.GetComponent<TileProperties>().FloorTile = grid[location];
                            if (!allTiles.Contains(grid[location])) { allTiles.Add(grid[location]); }
                        }
                        else
                        {
                            if (k == 0)
                            {
                                GameObject tile = PlaceTile(_blankTile, location, Vector3Int.one);
                                grid[location] = new Tile(CellType.None, location, _blankTileDimensions, tile);
                                grid[location].SetType(CellType.None);
                                tile.GetComponent<TileProperties>().FloorTile = grid[location];
                                if (!allTiles.Contains(grid[location])) { allTiles.Add(grid[location]); }
                            }
                            else
                            {
                                breakout = true;
                            }
                        }

                        if (breakout)
                        {
                            break;
                        }
                    }

                    if (breakout)
                    {
                        break;
                    }
                }

                if (breakout)
                {
                    break;
                }
            }

            //Each tile should know who it's neighbors are
            breakout = false;
            for (float i = 0; i < (_worldSize.x * unitSize); i += unitSize)
            {
                for (float j = 0; j < (_worldSize.z * unitSize); j += unitSize)
                {
                    for (float k = 0; k < (_worldSize.y * unitSize); k += unitSize)
                    {
                        for (int l = 0; l < neighbors.Length; l++)
                        {
                            Vector3Int offset = neighbors[l];
                            //Place blankTile at location and add to grid
                            Vector3Int location = new((int)i, (int)k, (int)j);
                            if (!grid.InBounds(location + offset)) continue;                        //Make sure the location is valid and will have a tile. TODO: Wanted to do something else here but I forgot
                            Tile tile = grid[location];                                             //Get the tile at location
                            Tile neighbor = grid[location + offset];                                //Get the tile neighbor
                            if (neighbor != null)                                                   //If the tile is null there's an issue somewhere
                            {
                                if (spawnMoreThanOneLevel)
                                {
                                    if (tile.neighborTiles.ContainsKey(directions[l]))
                                    {
                                        if (tile.neighborTiles[directions[l]] == neighbor) continue;
                                    }//Check if this neighbor was already added

                                    tile.addNeighbor(neighbor, directions[l]);                      //add the neighbor to this locations list of neighbors
                                    neighbor.addNeighbor(tile, directions[reverseOrientation[l]]);  //while we're here add this location as a neighbor of it's neighbor, remeber to switch the direction when placing in the neighbor's neighbor
                                }
                                else
                                {
                                    if (k == 0)
                                    {
                                        if (tile.neighborTiles.ContainsKey(directions[l]))
                                        {
                                            if (tile.neighborTiles[directions[l]] == neighbor) continue;
                                        }//Check if this neighbor was already added

                                        tile.addNeighbor(neighbor, directions[l]);                      //add the neighbor to this locations list of neighbors
                                        neighbor.addNeighbor(tile, directions[reverseOrientation[l]]);  //while we're here add this location as a neighbor of it's neighbor, remeber to switch the direction when placing in the neighbor's neighbor
                                    }
                                    else
                                    {
                                        breakout = true;
                                    }
                                }
                            }
                            else
                            {
                                throw new Exception($"Initial Gen: The tile {location + offset} is null when it shouldn't be");
                            }

                            if (breakout) break;
                        }

                        if (breakout) break;
                    }

                    if (breakout) break;
                }

                if (breakout) break;
            }
        }
        else
        {
            int confirmedTilesStored = 0;
            Transform[] children = floorPrefab.GetComponentsInChildren<Transform>();
            Transform parent = children[0];
            int childCount = parent.childCount;
            //foreach (Transform child in floorPrefab.transform)
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].parent == parent)
                {
                    Transform child = children[i];
                    Vector3Int position = Vector3Int.zero;
                    try
                    {
                        if (!child.gameObject.TryGetComponent<MeshCollider>(out MeshCollider mc))
                        {
                            mc = child.gameObject.AddComponent<MeshCollider>();
                        }

                        child.gameObject.GetOrAddComponent<MeshCollider>().enabled = true;
                        child.SetParent(Geometries[0].transform, false);
                        TileProperties props = child.gameObject.GetComponent<TileProperties>();
                        Tile tile = new(CellType.None, props.WorldPosition, _blankTileDimensions, child.gameObject);
                        position = tile._worldLocation;
                        grid[tile._worldLocation] = tile;
                        tile = grid[tile._worldLocation];
                        if (tile != null)
                        {
                            confirmedTilesStored++;
                            grid[tile._worldLocation].SetType(CellType.None);
                            props.FloorTile = grid[tile._worldLocation];
                            if (!allTiles.Contains(grid[tile._worldLocation])) { allTiles.Add(grid[tile._worldLocation]); }
                        }
                        else
                        {
                            //throw new Exception($"tile at {tile._worldLocation} not added to grid");
                            Debug.Log($"tile at {tile._worldLocation} not added to grid");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Log($"Tile at position {position} was not stored in Grid");
                        //throw new Exception($"Tile at position {position} was not stored in Grid");
                    }
                }
            }

            if (confirmedTilesStored == childCount)
            {
                Debug.Log($"All tiles in prefab added to Grid");
                //throw new Exception($"All tiles in prefab added to Grid");
            }
            else
            {
                int missingTileCount = childCount - confirmedTilesStored;
                Debug.Log($"{missingTileCount} tiles are missing out of {childCount}");
                //throw new Exception($"{missingTileCount} tiles are missing out of {childCount}");
            }

            //Each tile should know who it's neighbors are
            bool breakout = false;
            for (float i = 0; i < (_worldSize.x * unitSize); i += unitSize)
            {
                for (float j = 0; j < (_worldSize.z * unitSize); j += unitSize)
                {
                    for (float k = 0; k < (_worldSize.y * unitSize); k += unitSize)
                    {
                        for (int l = 0; l < neighbors.Length; l++)
                        {
                            Vector3Int offset = neighbors[l];
                            //Place blankTile at location and add to grid
                            Vector3Int location = new((int)i, (int)k, (int)j);
                            if (!grid.InBounds(location + offset)) continue;                        //Make sure the location is valid and will have a tile. TODO: Wanted to do something else here but I forgot
                            Tile tile = grid[location];                                             //Get the tile at location
                            Tile neighbor = grid[location + offset];                                //Get the tile neighbor
                            if (neighbor != null)                                                   //If the tile is null there's an issue somewhere
                            {
                                if (spawnMoreThanOneLevel)
                                {
                                    if (tile.neighborTiles.ContainsKey(directions[l]))
                                    {
                                        if (tile.neighborTiles[directions[l]] == neighbor) continue;
                                    }//Check if this neighbor was already added

                                    tile.addNeighbor(neighbor, directions[l]);                      //add the neighbor to this locations list of neighbors
                                    neighbor.addNeighbor(tile, directions[reverseOrientation[l]]);  //while we're here add this location as a neighbor of it's neighbor, remeber to switch the direction when placing in the neighbor's neighbor
                                }
                                else
                                {
                                    if (k == 0)
                                    {
                                        if (tile.neighborTiles.ContainsKey(directions[l]))
                                        {
                                            if (tile.neighborTiles[directions[l]] == neighbor) continue;
                                        }//Check if this neighbor was already added

                                        tile.addNeighbor(neighbor, directions[l]);                      //add the neighbor to this locations list of neighbors
                                        neighbor.addNeighbor(tile, directions[reverseOrientation[l]]);  //while we're here add this location as a neighbor of it's neighbor, remeber to switch the direction when placing in the neighbor's neighbor
                                    }
                                    else
                                    {
                                        breakout = true;
                                    }
                                }
                            }
                            else
                            {
                                throw new Exception($"Import: The tile {location + offset} is null when it shouldn't be");
                            }

                            if (breakout) break;
                        }

                        if (breakout) break;
                    }

                    if (breakout) break;
                }

                if (breakout) break;
            }
        }
    }

    private void GenLevel()
    {
        attempts++;
        if (attempts > MaxAttempts) { PrintDebug("Max attempts reached. Exiting GrimmGen."); return; }
        //Generate required and random rooms
        GenerateRooms();
        //Make paths through and between rooms
        if (rooms.Count > 2)
        {
            IdentifyTiles();
            Triangulate();
            CreateHallways();
            PathfindHallways();
            PlaceDoors();
            PlaceAssets();
            PruneEmptyTiles();
            PlaceMonsterSpawnLocations();
            //PlaceMonsterItemSpawnLocations();
            //GatherObstacles();            
            BakeNavMesh(false);
            SpawnMonsters();
            SpawnMonsterItems();
            ConnectPortals();
            CleanUp();
        }
        else
        {
            PrintDebug("No rooms generated, re-rolling");
            GenLevel();
        }
    }

    private void GenerateRooms()
    {
        bool receptionRoomPlaced = false;
        for (int i = 0; i < 50; i++)
        {
            receptionRoomPlaced = PlaceReceptionRoom();
            if (receptionRoomPlaced)
            {
                break;
            }
        }

        if (!receptionRoomPlaced)
        {
            PlaceReceptionRoom(true);
        }
        //make the required rooms first
        //then make a bunch of random rooms
        PlaceRequiredRooms();
        int roomDensity = (int)Math.Round((decimal)(_worldSize.x * _worldSize.z) / (decimal)ProcGenBudget, 3, MidpointRounding.ToEven);
        while (allRoomTiles.Count < roomDensity)
        {
            PlaceRandomRoom();
        }
    }

    private bool PlaceReceptionRoom(bool locationOverride = false)
    {
        //make required rooms in order
        GameObject receptionRoomPrefab = _receptionRooms.FirstOrDefault();
        Vector3Int roomLocation;
        if (locationOverride)
        {
            Vector3Int centerPoint = _worldSize * unitSize / 2;
            roomLocation = centerPoint;
        }
        else
        {
            roomLocation = new Vector3Int(
                _rand.Next(0, _worldSize.x) * unitSize,
                _rand.Next(0, _worldSize.y) * unitSize,
                _rand.Next(0, _worldSize.z) * unitSize
            );
        }

        //Define a room within the available dimensions
        Vector3Int roomSize = receptionRoomPrefab.GetComponent<TileProperties>().size;
        Room newRoom = PlaceRoom(roomLocation, roomSize, receptionRoomPrefab);
        if (newRoom != null)
        {
            if (_receptionRooms.FirstOrDefault().GetComponent<TileProperties>().name == "ReceptionRoom")
            {
                blackboard.SetValue<Room>(receptionRoomKey, newRoom);
            }

            _receptionRooms.Remove(receptionRoomPrefab);
        }

        if (newRoom != null && _receptionRooms.Count > 0)
        {
            //if a room was added and there are more rooms left, attach a room
            //calculate in which direction and how far this room should move
            //where x is the reception room and v is the room to place
            //the direction to move is -x.transform.forward and the distance is (v.worldsize.z/2 + x.wordsize.z/2)
            GameObject adjoiningRoomPrefab = _receptionRooms.FirstOrDefault();

            Room adjoiningRoom = null;
            for (float degrees = 0; degrees < 360; degrees += 90)
            {
                //Rotate the reception room and attempt to place the adjoining room for one full rotation
                if (adjoiningRoom == null)
                {
                    newRoom.roomPrefab.transform.Rotate(0, degrees, 0);
                    newRoom.roomPrefab.transform.forward = newRoom.roomPrefab.transform.right;
                }
                else
                {
                    break;
                }
                //Attempt to place the adjoining room 
                //TODO: Replace AnchorScript with Target
                Vector3 direction = (receptionRoomPrefab.GetComponentInChildren<AnchorScript>().transform.position - receptionRoomPrefab.transform.position).normalized;
                roomSize = adjoiningRoomPrefab.GetComponent<TileProperties>().size;
                float distance = (newRoom.roomWorldSize.z / 2) + (roomSize.z * unitSize / 2);
                distance++;
                direction *= distance;

                Vector3 newPosition = newRoom.roomPrefab.transform.position + direction;
                roomLocation = new Vector3Int(
                    (int)newPosition.x,
                    (int)newPosition.y,
                    (int)newPosition.z
                    );
                //rotate
                adjoiningRoom = new Room(roomLocation, roomSize, grid, adjoiningRoomPrefab, unitSize, _green);

                foreach (Tile tile in newRoom.tiles)
                {
                    //tile.self.GetComponentInChildren<Renderer>().material = _green;
                    tile.SetType(CellType.Room);//._type = CellType.Room;
                }

                rooms.Add(adjoiningRoom);
                adjoiningRoom.properties.lighting = TileProperties.Lighting.On;
                Instantiate(adjoiningRoomPrefab, roomLocation, Quaternion.identity);
                //PlaceRoom(adjoiningRoom.roomPrefab, adjoiningRoom.bounds.min, adjoiningRoom.bounds.size);
                //adjoiningRoom = PlaceRoom(roomLocation, roomSize, adjoiningRoomPrefab);

            }

            if (adjoiningRoom == null)
            {
                //Do something because we need both of these rooms;
                throw new Exception("Reception room placed badly. Redo");
            }
        }

        newRoom.properties.lighting = TileProperties.Lighting.Off;
        return newRoom != null;
    }
    private void PlaceRequiredRooms()
    {

        //Once all required rooms are placed the functions finishes
        if (_requiredRoomsList.Count > 0)
        {
            //make required rooms in order
            GameObject roomPrefab = _requiredRoomsList.FirstOrDefault();
            Vector3Int roomLocation;
            roomLocation = new Vector3Int(
                _rand.Next(0, _worldSize.x) * unitSize,
                _rand.Next(0, _worldSize.y) * unitSize,
                _rand.Next(0, _worldSize.z) * unitSize
            );

            //Define a room within the available dimensions
            Vector3Int roomSize = roomPrefab.GetComponent<TileProperties>().size;
            Room newRoom = PlaceRoom(roomLocation, roomSize, roomPrefab);
            newRoom.properties.lighting = TileProperties.Lighting.On;
            if (newRoom != null)
            {
                _requiredRoomsList.Remove(roomPrefab);
            }
            else
            {
                //If room does not meet requirements restart process
                PlaceRequiredRooms();

            }
        }
    }
    private void PlaceRandomRoom()
    {
        //Get a random room from _roomList
        GameObject roomPrefab = _roomList[_rand.Next(0, _roomList.Count())];
        //get the dimensions of the room
        Vector3Int roomSize = roomPrefab.GetComponent<TileProperties>().size;
        //pick a random location in the grid and make sure it's legal
        Vector3Int roomLocation = new(
            _rand.Next(0, _worldSize.x) * unitSize,
            _rand.Next(0, _worldSize.y) * unitSize,
            _rand.Next(0, _worldSize.z) * unitSize
        );
        PlaceRoom(roomLocation, roomSize, roomPrefab);
    }

    private Room PlaceRoom(Vector3Int roomLocation, Vector3Int roomSize, GameObject roomPrefab)
    {
        bool add = true;
        //check if the room goes out of bounds, reset if it does
        Room newRoom = new(roomLocation, roomSize, grid, roomPrefab, unitSize, _green, this);
        if (newRoom.outOfBounds)
        {
            add = false;
        }
        else if (add)
        {
            if (newRoom.bounds.xMin < 0 || newRoom.bounds.xMax > (_worldSize.x * unitSize)
            || newRoom.bounds.yMin < 0 || newRoom.bounds.yMax > (_worldSize.y * unitSize)
            || newRoom.bounds.zMin < 0 || newRoom.bounds.zMax > (_worldSize.z * unitSize))
            {
                add = false;
            }
            else if (add)
            {
                foreach (var room in rooms)
                {
                    if (Room.Intersect(room, newRoom))
                    {
                        add = false;
                        break;
                    }
                }
            }
        }
        //add the room to `rooms`
        //place the room in the world
        //assign the grid locations with either `room` or `edge` 
        //Store location of required rooms in the blackboard under their name
        //Remove room from required rooms list
        if (add)
        {
            foreach (Tile tile in newRoom.tiles)
            {
                //tile.self.GetComponentInChildren<Renderer>().material = _green;
                tile.SetType(CellType.Room);
                tile.parent = newRoom;
            }
            //if (!newRoom.roomPrefab.name.Contains("ReceptionRoom"))
            //{
            //    rooms.Add(newRoom);
            //}
            newRoom.properties.lighting = GetLighting();
            rooms.Add(newRoom);
            allRoomTiles.AddRange(newRoom.tiles);

            PlaceRoom(newRoom.roomPrefab, newRoom.bounds.min, newRoom.bounds.size);
        }
        else
        {
            newRoom.Deregister(_blankTileDimensions);
            newRoom = null;
        }

        return newRoom;
    }

    private TileProperties.Lighting GetLighting()
    {
        int random = utils.GetRandomNumber(1, 101);
        if (random < 50)
        {
            return TileProperties.Lighting.On;
        }
        else if (random < 75)
        {
            return TileProperties.Lighting.Flickering;
        }
        else
        {
            return TileProperties.Lighting.Off;
        }
    }

    private void IdentifyTiles()
    {
        foreach (Room room in rooms)
        {
            room.identifyTileCellType(_rand, _darkGreen, _white, _purple);
        }
    }
    void Triangulate()
    {
        List<Vertex> vertices = new();

        foreach (var room in rooms)
        {
            Vector2 center = new(room.bounds.center.x, room.bounds.center.z);
            Vector2 position = new(room.bounds.position.x, room.bounds.position.z);
            Vector2 size = new(room.bounds.size.x, room.bounds.size.z);
            vertices.Add(new Vertex<Room>(center, room));
        }

        delaunay = Delaunay2D.Triangulate(vertices);
    }

    void CreateHallways()
    {
        List<Prim.Edge> edges = new();

        foreach (var edge in delaunay.Edges)
        {
            edges.Add(new Prim.Edge(edge.U, edge.V));
        }

        List<Prim.Edge> mst = Prim.MinimumSpanningTree(edges, edges[0].U);

        selectedEdges = new HashSet<Prim.Edge>(mst);
        var remainingEdges = new HashSet<Prim.Edge>(edges);
        remainingEdges.ExceptWith(selectedEdges);

        foreach (var edge in remainingEdges)
        {
            if (_rand.NextDouble() < 0.125)
            {
                selectedEdges.Add(edge);
            }
        }

        foreach (Prim.Edge edge in edges)
        {
            pairs p = new(edge.U.Position, edge.V.Position);
            toDrawTriangulation.Add(p);
        }

        foreach (Prim.Edge edge in mst)
        {
            pairs p = new(edge.U.Position, edge.V.Position);
            toDrawMst.Add(p);
        }

        foreach (Prim.Edge edge in selectedEdges)
        {
            pairs p = new(edge.U.Position, edge.V.Position);
            toDrawAdditionalEdges.Add(p);
        }
    }

    void PathfindHallways()
    {
        DungeonPathfinder2D aStar = new(new Vector2Int(_worldSize.x, _worldSize.z) * unitSize, unitSize);
        int yval = 0;

        foreach (var edge in selectedEdges)
        {
            Hallway hallway = new(_emptyPrefab, unitSize);
            var startRoom = (edge.U as Vertex<Room>).Item;
            var endRoom = (edge.V as Vertex<Room>).Item;

            var startPosf = startRoom.bounds.center;
            var endPosf = endRoom.bounds.center;

            if (!grid.InBounds(new Vector3Int((int)startPosf.x, (int)startPosf.y, (int)startPosf.z)))
            {
                throw new Exception($"Start position {startPosf} out of bounds");
            }
            //Find the nearest appropriate tile and substitute that tile
            if (startPosf.x % unitSize != 0)
            {
                float remainder = startPosf.x / unitSize;
                int newMultiplier = (int)Math.Floor(remainder);
                startPosf.x = newMultiplier * unitSize;
            }

            if (startPosf.z % unitSize != 0)
            {
                float remainder = startPosf.z / unitSize;
                int newMultiplier = (int)Math.Floor(remainder);
                startPosf.z = newMultiplier * unitSize;
                //throw new Exception($"Start position {startPosf} has an x or z Value that isn't a multiple of {unitSize}");
            }

            if (endPosf.x % unitSize != 0)
            {
                float remainder = endPosf.x / unitSize;
                int newMultiplier = (int)Math.Floor(remainder);
                endPosf.x = newMultiplier * unitSize;
            }

            if (endPosf.z % unitSize != 0)
            {

                float remainder = endPosf.z / unitSize;
                int newMultiplier = (int)Math.Floor(remainder);
                endPosf.z = newMultiplier * unitSize;
                //throw new Exception($"Start position {startPosf} has an x or z Value that isn't a multiple of {unitSize}");
            }

            int destroyedCount = 0;
            bool isDestroyed = false;
            if (startRoom.roomPrefab.name.Contains("Destroyed"))
            {
                destroyedCount++;
            }

            if (endRoom.roomPrefab.name.Contains("Destroyed"))
            {
                destroyedCount++;
            }

            if (destroyedCount > 0)
            {
                if (destroyedCount > 1)
                {
                    isDestroyed = true;
                }
                else
                {
                    if (_rand.Next(-1, 1) > 0)
                    {
                        isDestroyed = true;
                    }
                }
            }

            var startPos = new Vector2Int((int)startPosf.x, (int)startPosf.z);
            var endPos = new Vector2Int((int)endPosf.x, (int)endPosf.z);

            var path = aStar.FindPath(startPos, endPos, unitSize, (DungeonPathfinder2D.Node a, DungeonPathfinder2D.Node b) =>
            {
                var pathCost = new DungeonPathfinder2D.PathCost();
                //2 situations. 
                //1) node a is part of a straight line
                //2) node a is the first node after a turn
                //In both situations we check if node b is in line with node a's previous node
                //If 1) then node b is not a turn
                //if 2) then node b is a turn
                //Check this by checking if node b is in line with both a and previous
                //If only in line with previous then it's a turn, if in line with both then it's straight. Should never only be in line with node a

                bool isTurn = false;
                string common = "";
                if (a.Previous != null)
                {
                    if (a.Previous.Position.x == a.Position.x)
                    {
                        common = "x";
                    }

                    if (a.Previous.Position.y == a.Position.y)
                    {
                        common = "y";
                    }

                    switch (common)
                    {
                        case "x":
                            if (a.Position.x != b.Position.x)
                            {
                                isTurn = true;
                            }

                            break;
                        case "y":
                            if (a.Position.y != b.Position.y)
                            {
                                isTurn = true;
                            }

                            break;
                    }
                }

                double turnCost = 0;
                if (isTurn)
                {
                    turnCost = 1;
                }

                //pathCost.cost = (Vector2Int.Distance(b.Position, endPos), turnCost);//heuristic
                pathCost.cost = (Math.Abs(endPos.x - b.Position.x) + Math.Abs(endPos.y - b.Position.y), turnCost); //h(n)
                pathCost.traversable = true;
                Vector3Int position = new(b.Position.x, yval, b.Position.y);
                if (grid.InBounds(position)) //g(n)
                {
                    try
                    {
                        Tile t = grid[position];
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message);
                    }

                    CellType type = grid[position]._type;

                    if (type == CellType.Room)
                    {
                        pathCost.cost.Item1 += 11;
                    }
                    else if (type == CellType.None)
                    {
                        pathCost.cost.Item1 += 6;
                    }
                    else if (type == CellType.Hallway)
                    {
                        pathCost.cost.Item1 += 2;
                    }
                    else if (type == CellType.Door)
                    {
                        pathCost.cost.Item1 += 0;
                    }
                    else if (type == CellType.Edge)
                    {
                        pathCost.cost.Item1 += 6;
                    }
                }
                else
                {
                    pathCost.traversable = false;
                }

                return pathCost;
            });

            if (path != null)
            {
                bool conjoinedHallways = false;
                Hallway intersectedHallway = new(_emptyPrefab, unitSize);
                for (int i = 0; i < path.Count; i++)
                {
                    var current = path[i];
                    Vector3Int position = new(current.x, yval, current.y);
                    CellType type = grid[position]._type;

                    if (type == CellType.None)
                    {
                        grid[position].SetType(CellType.Hallway);
                    }

                    if (type == CellType.Hallway)
                    {
                        conjoinedHallways = true;
                        intersectedHallway = grid[position].parentHallway;
                    }

                    if (i > 0)
                    {
                        var prev = path[i - 1];

                        var delta = current - prev;
                    }
                }

                foreach (var pos in path)
                {
                    Vector3Int position = new(pos.x, yval, pos.y);
                    if (grid[position]._type == CellType.Hallway)
                    {
                        GameObject hallwayPrefab = _hallwaysPristine.FirstOrDefault();
                        if (isDestroyed)
                        {
                            hallwayPrefab = _hallwaysDestroyed.FirstOrDefault();
                        }

                        Tile tile = grid[position];
                        HallwayTile hallwayTile = new(hallwayPrefab, tile);
                        if (isDestroyed)
                        {
                            hallwayTile.tile._status = Status.Destroyed;
                        }
                        else
                        {
                            hallwayTile.tile._status = Status.Pristine;
                        }

                        if (conjoinedHallways) //if this hallway crosses another, reassign all of it's tiles to this hallway and remove it from the list of hallways
                        {
                            foreach (HallwayTile hallwaySegment in intersectedHallway.hallwayTiles)
                            {
                                if (!hallway.hallwayTiles.Contains(hallwaySegment)) { hallway.AddHallwaySegment(hallwaySegment); }
                            }

                            hallways.Remove(intersectedHallway);
                        }

                        if (!hallwayTiles.Contains(hallwayTile.tile)) { hallwayTiles.Add(hallwayTile.tile); }

                        if (!hallway.hallwayTiles.Contains(hallwayTile)) { hallway.AddHallwaySegment(hallwayTile); }

                        PlaceHallway(hallwayPrefab, position);
                    }
                }
            }
            else
            {
                pairs pairs = new(edge.V.Position, edge.U.Position);
                toDrawFailedEdges.Add(pairs);
            }

            hallways.Add(hallway);
        }
    }

    //private void Triangulate()
    //{
    //    //Triangulate in 2D for now
    //    List<Vertex> vertices = new List<Vertex>();

    //    foreach (Room room in rooms)
    //    {
    //        vertices.Add(new Vertex<Room>((Vector3)room.bounds.position + ((Vector3)room.bounds.size) / 2, room));
    //    }
    //    if (vertices.Count > 0)
    //    {
    //        delaunay = Delaunay3D.Triangulate(vertices);
    //    }else
    //    {
    //        throw new Exception("No verticies found");
    //    }
    //}
    //private void CreateHallways()
    //{
    //    //Create hallwayTiles in 2D for now
    //    List<Prim.Edge> edges = new List<Prim.Edge>();

    //    foreach (Delaunay3D.Edge edge in delaunay.Edges)
    //    {
    //        edges.Add(new Prim.Edge(edge.U, edge.V));
    //    }
    //    if (edges.Count > 0)
    //    {

    //        List<Prim.Edge> minimumSpanningTree = Prim.MinimumSpanningTree(edges, edges[0].U);

    //        selectedEdges = new HashSet<Prim.Edge>(minimumSpanningTree);
    //        var remainingEdges = new HashSet<Prim.Edge>(edges);
    //        remainingEdges.ExceptWith(selectedEdges);

    //        foreach (Prim.Edge edge in remainingEdges)
    //        {
    //            if (_rand.NextDouble() < 0.125)
    //            {
    //                selectedEdges.Add(edge);
    //            }
    //        }
    //    } else
    //    {
    //        throw new Exception("No edges");
    //    }
    //}
    //private void PathfindHallways()
    //{
    //    //pathfind in 2d for now
    //    //If pathing through edge, make it a door.
    //    //Store door location in 
    //    //If corner, look for neighbor hallway and make the wall in that direction a door
    //    DungeonPathfinder3D aStar = new DungeonPathfinder3D(_worldSize);

    //    foreach (Prim.Edge edge in selectedEdges)
    //    {
    //        Room startRoom = (edge.U as Vertex<Room>).Item;
    //        Room endRoom = (edge.V as Vertex<Room>).Item;

    //        Vector3 startPosf = startRoom.bounds.center;
    //        Vector3 endPosf = endRoom.bounds.center;
    //        Vector3Int startPos = new Vector3Int((int)startPosf.x, (int)startPosf.y, (int)startPosf.z);
    //        Vector3Int endPos = new Vector3Int((int)endPosf.x, (int)endPosf.y, (int)endPosf.z);

    //        List<Vector3Int> path = aStar.FindPath(startPos, endPos, (DungeonPathfinder3D.Node a, DungeonPathfinder3D.Node b) => {
    //            var pathCost = new DungeonPathfinder3D.PathCost();

    //            Vector3Int delta = b.Position - a.Position;

    //            if (delta.y == 0)
    //            {
    //                //flat hallway
    //                pathCost.cost = Vector3Int.Distance(b.Position, endPos);    //heuristic

    //                if (grid[b.Position]._type == CellType.Stairs)
    //                {
    //                    return pathCost;
    //                }
    //                else if (grid[b.Position]._type == CellType.Room)
    //                {
    //                    pathCost.cost += 5;
    //                }
    //                else if (grid[b.Position]._type == CellType.None)
    //                {
    //                    pathCost.cost += 1;
    //                }

    //                pathCost.traversable = true;
    //            }
    //            else
    //            {
    //                //staircase
    //                if ((grid[a.Position]._type != CellType.None && grid[a.Position]._type != CellType.Hallway)
    //                    || (grid[b.Position]._type != CellType.None && grid[b.Position]._type != CellType.Hallway)) return pathCost;

    //                pathCost.cost = 100 + Vector3Int.Distance(b.Position, endPos);    //base cost + heuristic

    //                int xDir = Mathf.Clamp(delta.x, -1, 1);
    //                int zDir = Mathf.Clamp(delta.z, -1, 1);
    //                Vector3Int verticalOffset = new Vector3Int(0, delta.y, 0);
    //                Vector3Int horizontalOffset = new Vector3Int(xDir, 0, zDir);

    //                if (!grid.InBounds(a.Position + verticalOffset)
    //                    || !grid.InBounds(a.Position + horizontalOffset)
    //                    || !grid.InBounds(a.Position + verticalOffset + horizontalOffset))
    //                {
    //                    return pathCost;
    //                }

    //                if (grid[a.Position + horizontalOffset]._type != CellType.None
    //                    || grid[a.Position + horizontalOffset * 2]._type != CellType.None
    //                    || grid[a.Position + verticalOffset + horizontalOffset]._type != CellType.None
    //                    || grid[a.Position + verticalOffset + horizontalOffset * 2]._type != CellType.None)
    //                {
    //                    return pathCost;
    //                }

    //                pathCost.traversable = true;
    //                pathCost.isStairs = true;
    //            }

    //            return pathCost;
    //        });

    //        if (path != null)
    //        {
    //            for (int geometryIndex = 0; geometryIndex < path.Count; geometryIndex++)
    //            {
    //                var current = path[geometryIndex];

    //                if (grid[current]._type == CellType.None)
    //                {
    //                    grid[current]._type = CellType.Hallway;
    //                }

    //                if (geometryIndex > 0)
    //                {
    //                    var prev = path[geometryIndex - 1];

    //                    var delta = current - prev;

    //                    if (delta.y != 0)
    //                    {
    //                        int xDir = Mathf.Clamp(delta.x, -1, 1);
    //                        int zDir = Mathf.Clamp(delta.z, -1, 1);
    //                        Vector3Int verticalOffset = new Vector3Int(0, delta.y, 0);
    //                        Vector3Int horizontalOffset = new Vector3Int(xDir, 0, zDir);

    //                        grid[prev + horizontalOffset]._type = CellType.Stairs;
    //                        grid[prev + horizontalOffset * 2]._type = CellType.Stairs;
    //                        grid[prev + verticalOffset + horizontalOffset]._type = CellType.Stairs;
    //                        grid[prev + verticalOffset + horizontalOffset * 2]._type = CellType.Stairs;

    //                        PlaceStairs(_stairsPrefab, prev + horizontalOffset);
    //                        PlaceStairs(_stairsPrefab, prev + horizontalOffset * 2);
    //                        PlaceStairs(_stairsPrefab, prev + verticalOffset + horizontalOffset);
    //                        PlaceStairs(_stairsPrefab, prev + verticalOffset + horizontalOffset * 2);
    //                    }

    //                    Debug.DrawLine(prev + new Vector3(0.5f, 0.5f, 0.5f), current + new Vector3(0.5f, 0.5f, 0.5f), UnityEngine.Color.blue, 100, false);
    //                }
    //            }

    //            foreach (var pos in path)
    //            {
    //                if (grid[pos]._type == CellType.Hallway)
    //                {
    //                    PlaceHallway(_hallwayPrefab, pos);
    //                }
    //            }
    //        }
    //    }
    //}

    GameObject PlacePrefab(GameObject prefab, Vector3Int location, Vector3Int size, Quaternion rotation)
    {
        GameObject go = Instantiate(prefab, location, rotation);

        //go.GetComponent<MeshRenderer>().material = material;
        return go;
    }
    GameObject PlacePrefab(GameObject prefab, Vector3 location, Vector3Int size, Quaternion rotation)
    {
        GameObject go = Instantiate(prefab, location, rotation);

        //go.GetComponent<MeshRenderer>().material = material;
        return go;
    }
    GameObject PlaceTile(GameObject prefab, Vector3Int location, Vector3Int size)
    {
        Vector3Int offset = new(unitSize / 2, 0, unitSize / 2);
        GameObject go = PlacePrefab(prefab, location + offset, size, prefab.transform.rotation);
        go.transform.SetParent(Geometries[0].transform, false);
        go.GetComponent<TileProperties>().WorldPosition = location;
        return go;
    }

    void PlaceRoom(GameObject prefab, Vector3Int location, Vector3Int size)
    {
        Vector3Int offset = new(size.x / 2, size.y / 2, size.z / 2);
        GameObject go = PlacePrefab(prefab, location + offset, size, prefab.transform.rotation);
        go.transform.SetParent(Geometries[1].transform, false);
        //go.GetComponent<Transform>().localScale = size;
    }

    void PlaceHallway(GameObject prefab, Vector3Int location)
    {
        Vector3Int offset = new(unitSize / 2, unitSize / 2, unitSize / 2);
        GameObject go = PlacePrefab(prefab, location + offset, new Vector3Int(1, 1, 1), prefab.transform.rotation);
        go.transform.SetParent(Geometries[1].transform);
    }

    void PlaceStairs(GameObject prefab, Vector3Int location) => PlacePrefab(prefab, location, new Vector3Int(1, 1, 1), prefab.transform.rotation);

    void PlaceMarker(GameObject prefab, Vector3Int location, Material mat)
    {
        GameObject go = PlacePrefab(prefab, location, new Vector3Int(1, 1, 1), prefab.transform.rotation);
        go.GetComponent<Renderer>().material = mat;
    }

    void PlaceMarker(GameObject prefab, Vector3 location, Material mat)
    {
        GameObject go = PlacePrefab(prefab, location, new Vector3Int(1, 1, 1), prefab.transform.rotation);
        go.GetComponent<Renderer>().material = mat;
    }

    private void PlaceDoors()
    {
        foreach (Hallway hallway in hallways)
        {

            hallway.identifyTileCellType(_rand, _blue, _darkGreen, _white, _purple);
            foreach (Tile tile in hallway.tiles)
            {
                List<string> tempKeys = tile.walls.Keys.ToList();
                foreach (string dir in tempKeys)
                {
                    //retrieve the assigned gameobject for the wall
                    GameObject wall = tile.walls[dir].Item1;
                    //retrieve the attachment point for this wall
                    GameObject attachmentPoint = tile.self.GetComponentsInChildren<Wall>().Where(x => x.gameObject.name == dir).ToArray()[0].gameObject;
                    //retrieve the direction target for this tile
                    GameObject target = tile.self.GetComponentInChildren<Target>().gameObject;
                    //Place the prefab
                    GameObject newWall = PlacePrefab(wall, attachmentPoint.transform.position + new Vector3(0f, 1.92f, 0f), Vector3Int.one, Quaternion.identity);
                    //Set newWall to face the correct direction
                    newWall.transform.forward = -(attachmentPoint.transform.position - target.transform.position).normalized;
                    Vector3 adjustment = newWall.transform.forward * 0.5f;
                    newWall.transform.position += adjustment;
                    //Assign the wall to the parent room
                    newWall.transform.SetParent(Geometries[1].transform, true);
                }
            }
        }

        foreach (Room room in rooms)
        {
            foreach (Tile tile in room.tiles)
            {
                List<string> tempKeys = tile.walls.Keys.ToList();
                foreach (string dir in tempKeys)
                {
                    //retrieve the assigned gameobject for the wall
                    GameObject wall = tile.walls[dir].Item1;
                    //retrieve the attachment point for this wall
                    GameObject attachmentPoint = tile.self.GetComponentsInChildren<Wall>().Where(x => x.gameObject.name == dir).ToArray()[0].gameObject;
                    //retrieve the direction target for this tile
                    GameObject target = tile.self.GetComponentInChildren<Target>().gameObject;
                    //Place the prefab
                    GameObject newWall = PlacePrefab(wall, attachmentPoint.transform.position + new Vector3(0f, 0.7f, 0f), Vector3Int.one, Quaternion.identity);
                    //Set newWall to face the correct direction
                    newWall.transform.forward = -(attachmentPoint.transform.position - target.transform.position).normalized;
                    //Assign the wall to the parent room
                    newWall.transform.SetParent(Geometries[1].transform, true);
                }
            }
        }
    }

    private void MakeExtraDoors() => throw new NotImplementedException();

    private void PlaceAssets()
    {
        //Place the assets for each room
        //Make sure nothing is placed infront of a door
        //Just delete them for now,
        //TODO: dynamically adjust environmental items location based on door layout
        ////To dynamically adjust props take stock of which tiles are occupied by props
        ////and which ones are free. Record the absolute direction away from the center of the room
        ////move to an empty space and reorient with respect to the center of the room
        ////if there are no empty spaces, delete.
        Physics.SyncTransforms();

        foreach (Room room in rooms)
        {
            //Collect props and prop locations, used this information to identify empty edge tiles
            List<Tile> freeTiles = new();
            List<(Tile, List<Prop>)> doorTiles = new();
            List<Tile> edgeTiles = room.tiles.Where(t => t._type is CellType.Edge or CellType.Door).ToList();
            edgeTiles.ForEach(t =>
            {
                bool isCorner = false;
                List<Prop> props = new();
                int wallcount = t.walls.Values.Where(v => v.Item2 != WallType.None).ToArray().Length;
                if (wallcount > 1)
                {
                    isCorner = true;
                }
                //List<GameObject> propList = new();
                //foreach (Transform childTransform in room.roomPrefab.transform)
                //{
                //    if (childTransform.parent == room.roomPrefab.transform && childTransform.tag == "Prop")
                //    {
                //        propList.Add(childTransform.gameObject);
                //    }
                //}

                cube cubeToDraw = new(t.self.GetComponentInChildren<Target>().transform.position, new Vector3(unitSize, unitSize, unitSize));

                Collider[] hitColliders = Physics.OverlapBox(cubeToDraw.position, cubeToDraw.size / 2f, Quaternion.identity, Physics.AllLayers);// LayerMask.NameToLayer("Prop"));
                if (hitColliders.Length > 0)
                {
                    foreach (Collider collider in hitColliders)
                    {

                        if (collider.tag == "Prop")
                        {
                            Prop prop;
                            GameObject go = collider.gameObject;
                            if (collider.gameObject.transform.parent != room.roomPrefab.transform)
                            {
                                //Only work with top level props
                                if (collider.gameObject.TryGetComponent<PropRoot>(out PropRoot script))
                                {
                                    go = script.gameObject;
                                }
                                else
                                {
                                    go = collider.gameObject.GetComponentInParent<PropRoot>().gameObject;
                                }
                            }

                            Vector3 orientation;
                            if (isCorner)
                            {
                                var walls = t.walls.Values.Where(v => v.Item2 != WallType.None).ToList();
                                Vector3 midpoint = walls[0].Item1.transform.position + ((walls[0].Item1.transform.position - walls[1].Item1.transform.position) / 2);
                                orientation = (go.transform.position - midpoint).normalized;
                            }
                            else
                            {
                                GameObject wall = t.walls.Values.Where(v => v.Item2 != WallType.None).ToList()[0].Item1;
                                orientation = (go.transform.position - wall.transform.position).normalized;
                            }

                            prop = new Prop(go, orientation, isCorner);
                            props.Add(prop);
                        }
                    }
                }
                else
                {
                    if (t._type != CellType.Door)
                    {
                        freeTiles.Add(t);
                        toDrawFreeCube.Add(cubeToDraw);
                    }
                }

                if (t._type == CellType.Door)
                {
                    //Determine if any props are in in front of doors
                    if (props.Count > 0)
                    {
                        doorTiles.Add((t, props));
                        toDrawDoorCube.Add(cubeToDraw);
                    }
                }
            });
            //If yes, get their orientation from the wall/s on their current tile
            if (doorTiles.Count > 0)
            {

                foreach ((Tile, List<Prop>) var in doorTiles)
                {
                    //Move props to an empty edge tile
                    List<Prop> tempPropList = var.Item2.ToList();
                    foreach (Prop prop in tempPropList)
                    {
                        if (freeTiles.Count > 0)
                        {
                            int index = _rand.Next(0, freeTiles.Count);
                            prop.PropGameObject.transform.SetParent(freeTiles[index].self.transform, true);
                            freeTiles.RemoveAt(index);
                        }
                        else
                        {
                            bool isOffice = room.roomPrefab.name.Contains("Office");
                            bool isRecpetion = room.roomPrefab.name.Contains("Reception");
                            if (isOffice || isRecpetion)
                            {
                                //Don't do anything
                            }
                            else
                            {
                                prop.PropGameObject.GetComponentInChildren<NavMeshModifier>().enabled = false;
                                Destroy(prop.PropGameObject);
                            }
                        }
                    }
                }
            }
        }

        //throw new NotImplementedException();
    }
    private void PlaceMonsterSpawnLocations()
    {
        int remainingBudget = budget;
        //Monster Types in world and number to spawn
        Dictionary<string, int> monsterCount = new();
        List<string> availableMonsters = new();
        List<string> singletonMonsters = new() { "Sjena", "Vodyanoi" };

        if (blackboard.TryGetValue(navMeshAgentDictionaryKey, out navMeshAgentDictionary))
        {
            if (navMeshAgentDictionary.Count > 0)
            {
                availableMonsters = navMeshAgentDictionary.Keys.ToList();
                //select monsters for level here based on budget
                while (remainingBudget > 0)
                {
                    if (availableMonsters.Count == 0)
                    {
                        availableMonsters = navMeshAgentDictionary.Keys.Where(x => !singletonMonsters.Contains(x)).ToList();
                    }
                    //Select random monster to add to world
                    string randKey = availableMonsters.ToArray()[_rand.Next(0, availableMonsters.Count)];
                    GameObject prefab = (GameObject)navMeshAgentDictionary[randKey];
                    EnemyProperties properties = prefab.GetComponent<EnemyProperties>();

                    //check if monster type has not been selected already
                    if (!monsterCount.ContainsKey(randKey))
                    {
                        monsterCount.Add(randKey, 1);
                        selectedNavMeshAgentDictionary.Add(randKey, properties.NavMeshAgentId);
                        availableMonsters.Remove(randKey);
                    }
                    else
                    {
                        monsterCount[randKey]++;
                        availableMonsters.Remove(randKey);
                    }

                    remainingBudget -= properties.Cost;
                }
            }
            else
            {
                throw new Exception("No NavMeshAgents to work with");
            }
        }
        //Collect all tiles

        foreach (Room room in rooms)
        {
            allRoomTiles.AddRange(room.tiles);
        }
        //for each chosen monster place randomly
        foreach (string monsterName in selectedNavMeshAgentDictionary.Keys)
        {
            if (monsterName == "Sjena")
            {
                GameObject prefab = navMeshAgentDictionary[monsterName];
                Vector3Int StartLocation = new(0, -100, 0);
                monstersToSpawn.Add((prefab, StartLocation));
            }
            else if (monsterName != "Zombie")
            {
                GameObject prefab = navMeshAgentDictionary[monsterName];
                if (allRoomTiles.Count > 0)
                {
                    for (int i = 0; i < monsterCount[monsterName]; i++)
                    {
                        Vector3Int StartLocation = Vector3Int.zero;
                        Vector3Int offset = new(unitSize / 2, 0, unitSize / 2);
                        bool validStartingLoc = false;
                        int layerIndex = LayerMask.NameToLayer("Obstacle");
                        int layerMask = 1 << layerIndex;
                        while (!validStartingLoc)
                        {
                            Tile potentialStartingTile = allRoomTiles[_rand.Next(0, allRoomTiles.Count)];
                            Vector3 potentialStartingLoc = potentialStartingTile._worldLocation + offset;

                            Collider[] obstaclesInRadius = Physics.OverlapSphere(potentialStartingLoc, prefab.GetComponent<NavMeshAgent>().radius, layerMask);

                            if (obstaclesInRadius.Length == 0)
                            {
                                StartLocation = new Vector3Int((int)potentialStartingLoc.x, (int)potentialStartingLoc.y + 1, (int)potentialStartingLoc.z);
                                validStartingLoc = true;
                            }
                        }

                        if (StartLocation != Vector3Int.zero)
                        {
                            monstersToSpawn.Add((prefab, StartLocation));
                        }
                    }
                }
            }
        }
        //for (int geometryIndex = 0; geometryIndex < Geometries.Length; geometryIndex++)
        //{
        //    if (Geometries[geometryIndex].gameObject.GetComponentCount() > selectedNavMeshAgentDictionary.Keys.Count()) //if true we need more navmesh surfaces
        //    {
        //        NavMeshSurface surfaceIndex = Geometries[geometryIndex].gameObject.AddComponent<NavMeshSurface>();
        //        surfaceIndex.enabled = true;
        //    }
        //    else
        //    {
        //        break;
        //    }
        //}

        //Deactivate unused NavMeshSurfaces

        SourcesPerSurface = new();
        MarkupsPerSurface = new();
        ModifiersPerSurface = new();
        List<int> added = new();

        List<string> exclude = new()
        {
            "Sjena"
        };
        //Initialize cache
        for (int geometryIndex = 0; geometryIndex < Geometries.Length; geometryIndex++)
        {
            NavMeshSurface[] surface = Geometries[geometryIndex].gameObject.GetComponents<NavMeshSurface>().ToArray();
            int limit = surface.Length;
            if (surface.Length >= selectedNavMeshAgentDictionary.Count) { limit = selectedNavMeshAgentDictionary.Count; }

            for (int surfaceIndex = 0; surfaceIndex < limit; surfaceIndex++)
            {
                if (added.Count != selectedNavMeshAgentDictionary.Count)
                {
                    if (selectedNavMeshAgentDictionary.Values.Contains(surface[surfaceIndex].agentTypeID) || surface[surfaceIndex].agentTypeID == -1)
                    {
                        surface[surfaceIndex].agentTypeID = selectedNavMeshAgentDictionary.Values.ToArray()[surfaceIndex];
                        string name = NavMesh.GetSettingsNameFromID(surface[surfaceIndex].agentTypeID);
                        if (!exclude.Contains(name))
                        {
                            added.Add(surface[surfaceIndex].agentTypeID);
                            surface[surfaceIndex].navMeshData = NavMeshDatas[geometryIndex][surfaceIndex];
                            NavMesh.AddNavMeshData(NavMeshDatas[geometryIndex][surfaceIndex]);
                            //Dictionary<GeometryIndex, Dictionary<NavMeshSurfaceIndex, List<ObjectAssociatedWithNavMeshSurface>()>>
                            if (!SourcesPerSurface.ContainsKey(geometryIndex)) { SourcesPerSurface.Add(geometryIndex, new Dictionary<int, List<NavMeshBuildSource>>()); }

                            if (!SourcesPerSurface[geometryIndex].ContainsKey(surfaceIndex)) { SourcesPerSurface[geometryIndex].Add(added.Count-1, new List<NavMeshBuildSource>()); }

                            if (!MarkupsPerSurface.ContainsKey(geometryIndex)) { MarkupsPerSurface.Add(geometryIndex, new Dictionary<int, List<NavMeshBuildMarkup>>()); }

                            if (!MarkupsPerSurface[geometryIndex].ContainsKey(surfaceIndex)) { MarkupsPerSurface[geometryIndex].Add(added.Count - 1, new List<NavMeshBuildMarkup>()); }

                            if (!ModifiersPerSurface.ContainsKey(geometryIndex)) { ModifiersPerSurface.Add(geometryIndex, new Dictionary<int, List<NavMeshModifier>>()); }

                            if (!ModifiersPerSurface[geometryIndex].ContainsKey(surfaceIndex)) { ModifiersPerSurface[geometryIndex].Add(added.Count - 1, new List<NavMeshModifier>()); }
                        }
                        else
                        {
                            surface[surfaceIndex].enabled = false;
                        }
                    }
                    else
                    {
                        surface[surfaceIndex].enabled = false;
                    }
                }
                else
                {
                    surface[surfaceIndex].enabled = false;
                }
            }
        }
    }
    //private void PlaceMonsterItemSpawnLocations()
    //{
    //    foreach (string key in selectedNavMeshAgentDictionary.Keys)
    //    {
    //        System.Collections.Generic.IEnumerable<Type> _classes;
    //        EnemyInitializationReference _initializeManager = ServiceLocator.For(this).Get<EnemyInitializationReference>();
    //        GameObject prefab = navMeshAgentDictionary[key];
    //        EnemyProperties properties = prefab.GetComponent<EnemyProperties>();
    //        InitializeItems initializeItems;
    //        if (prefab != null)
    //        {
    //            if (_initializeManager.classDictionary.ContainsKey(key + "States"))
    //            {
    //                _classes = _initializeManager.classDictionary[key + "States"];
    //                foreach (var _class in _classes)
    //                {
    //                    if (_class.ToString().Contains("InitializeItems"))
    //                    {
    //                        itemsToSpawn.Add(_class);
    //                    }
    //                }
    //                initializeItems = Activator.CreateInstance(_classes.Where(x => x.ToString().Contains("InitializeItems")).ToArray()[0]);
    //            }
    //            else
    //            {
    //                throw new Exception($"namespace: {key + "States"} not found in classDictionary. Available keys are: {_initializeManager.classDictionary.Keys.ToCommaSeparatedString()}");
    //            }                

    //            if (properties.spawnItems.Count > 0)
    //            {
    //                //Each monster's namespace will have an InitializeItems script that will spawn the items in the correct locations
    //            }
    //        }
    //    }
    //}
    private void GatherObstacles()
    {
        //Gather and store all objects that will be on the navmesh but not walkable surfaces      
        obstacles = new List<GameObject>();
        foreach (Room room in this.rooms)
        {
            obstacles.Add(room.roomPrefab);
        }
        //extend to hallwayTiles
        //extend to any environmental objects
    }

    private void PruneEmptyTiles()
        //Remove all tiles from the world and remove from their neighbors list to avoid null reference
        //Cuts down on navmesh generation load
        //List<Tile> toSearch = grid.values.ToList();
        //for(int geometryIndex = 0; geometryIndex < toSearch.Count; geometryIndex++)
        //{
        //    Tile tile = toSearch[geometryIndex];
        //    if (tile._type == CellType.None)
        //    {
        //        //Destroy the object and remove it from the world state
        //        Destroy(tile.self);
        //        //tile.self.SetActive(false);
        //        grid[tile._worldLocation] = null;
        //    }
        //}
        => OnPruneTile?.Invoke(false);

    ////All NavMeshSurfaces are premade for the WorldFloor prefab. Update to reflect current layout
    //private void BakeNavMesh(bool Async)
    //{
    //    if (selectedNavMeshAgentDictionary.Count > 0)
    //    {
    //        if (navMeshBakeRetryCount < 2)
    //        {
    //            for (int geometryIndex = 0; geometryIndex < Geometries.Length; geometryIndex++)
    //            {
    //                NavMeshSurface[] Surfaces = Geometries[geometryIndex].gameObject.GetComponents<NavMeshSurface>();
    //                for (int surfaceIndex = 0; surfaceIndex < Surfaces.Length; surfaceIndex++)
    //                {
    //                    if (Surfaces[surfaceIndex].enabled)
    //                    {
    //                        LayerMask layerMask = Surfaces[surfaceIndex].layerMask;
    //                        //NavMeshCollectGeometry collectGeometry = Surfaces[surfaceIndex].useGeometry;
    //                        int navMeshSurface = Surfaces[surfaceIndex].defaultArea;
    //                        NavMeshBuilder.CollectSources(navMeshBounds, layerMask, NavMeshCollectGeometry.PhysicsColliders, 0, MarkupsPerSurface[geometryIndex][surfaceIndex], SourcesPerSurface[geometryIndex][surfaceIndex]);

    //                        NavMeshDatas[geometryIndex][surfaceIndex] = NavMeshBuilder.BuildNavMeshData(
    //                                                                        NavMesh.GetSettingsByIndex(Surfaces[surfaceIndex].agentTypeID),
    //                                                                        SourcesPerSurface[geometryIndex][surfaceIndex],
    //                                                                        navMeshBounds,
    //                                                                        navMeshBounds.center,
    //                                                                        Quaternion.Euler(Vector3.up));
    //                    }
    //                }
    //            }
    //        }
    //    }
    //}
    private void BakeNavMesh(bool Async)
    {

        //Bake navmesh for navmesh layer and account for obstacle layer
        //Repeat for each monster in level
        if (selectedNavMeshAgentDictionary.Count > 0)
        {
            if (navMeshBakeRetryCount < 2)
            {
                List<GameObject> gameObjects = new();
                var tileList = grid.values;
                List<NavMeshBuildMarkup> markups = new();
                List<NavMeshModifier> modifiers = new();

                //loop through all the different surfaces
                for (int geometryIndex = 0; geometryIndex < Geometries.Length; geometryIndex++)
                {

                    NavMeshSurface[] Surfaces = Geometries[geometryIndex].gameObject.GetComponents<NavMeshSurface>().Where(x => x.enabled == true).ToArray();
                    //Check if collected objects are all children of Surface
                    bool isCollectingChildren = true; // Geometries[geometryIndex].collectObjects == CollectObjects.Children;

                    for (int surfaceIndex = 0; surfaceIndex < Surfaces.Length; surfaceIndex++)
                    {
                        //if there are no markups, build the markups. Make sure this is checking the LIST count not the dictionary count
                        if (MarkupsPerSurface[geometryIndex][surfaceIndex].Count == 0)
                        {
                            //make sure this is checking the LIST count not the dictionary count
                            if (ModifiersPerSurface[geometryIndex][surfaceIndex].Count == 0)
                            {
                                //if we are collecting children and there are no modifiers caches
                                if (isCollectingChildren)
                                {
                                    //If yes put them in a list
                                    //Get modifiers from all geometries
                                    ModifiersPerSurface[geometryIndex][surfaceIndex] = new List<NavMeshModifier>(WorldGeometry.gameObject.GetComponentsInChildren<NavMeshModifier>());
                                }
                                //if we are not collecting children and there are no modifiers cached
                                else if (!isCollectingChildren)
                                {
                                    //Else get all active navmeshmodifiers and put that in the modifiers list
                                    ModifiersPerSurface[geometryIndex][surfaceIndex] = NavMeshModifier.activeModifiers;
                                }
                                //Iterate through the List of associated Modifiers
                                for (int modifierIndex = 0; modifierIndex < ModifiersPerSurface[geometryIndex][surfaceIndex].Count; modifierIndex++)
                                {
                                    //If the navmeshmodifier is one of the included layers in Surface and if the navmeshmodifier is for the correct agent type
                                    if ((Surfaces[surfaceIndex].layerMask & (1 << ModifiersPerSurface[geometryIndex][surfaceIndex][modifierIndex].gameObject.layer)) != 0)
                                    {
                                        //int resolvedID = NavMesh.GetSettingsByID(Surfaces[surfaceIndex].agentTypeID).agentTypeID;
                                        //if the navmeshsurface affects all agents
                                        if (Surfaces[surfaceIndex].agentTypeID == -1)
                                        {
                                            if (ModifiersPerSurface[geometryIndex][surfaceIndex][modifierIndex].AffectsAgentType(Surfaces[surfaceIndex].agentTypeID))
                                            {
                                                MarkupsPerSurface[geometryIndex][surfaceIndex].Add(new NavMeshBuildMarkup()
                                                {
                                                    root = ModifiersPerSurface[geometryIndex][surfaceIndex][modifierIndex].transform,
                                                    overrideArea = ModifiersPerSurface[geometryIndex][surfaceIndex][modifierIndex].overrideArea,
                                                    area = ModifiersPerSurface[geometryIndex][surfaceIndex][modifierIndex].area,
                                                    ignoreFromBuild = ModifiersPerSurface[geometryIndex][surfaceIndex][modifierIndex].ignoreFromBuild
                                                });
                                            }
                                        }
                                        //else if the navmeshsurface affects the specific agent we are checking for
                                        else if (CompareAgentId(selectedNavMeshAgentDictionary, Surfaces[surfaceIndex].agentTypeID))
                                        {
                                            if (ModifiersPerSurface[geometryIndex][surfaceIndex][modifierIndex].AffectsAgentType(Surfaces[surfaceIndex].agentTypeID))
                                            {
                                                MarkupsPerSurface[geometryIndex][surfaceIndex].Add(new NavMeshBuildMarkup()
                                                {
                                                    root = ModifiersPerSurface[geometryIndex][surfaceIndex][modifierIndex].transform,
                                                    overrideArea = ModifiersPerSurface[geometryIndex][surfaceIndex][modifierIndex].overrideArea,
                                                    area = ModifiersPerSurface[geometryIndex][surfaceIndex][modifierIndex].area,
                                                    ignoreFromBuild = ModifiersPerSurface[geometryIndex][surfaceIndex][modifierIndex].ignoreFromBuild
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (!CacheSources || SourcesPerSurface[geometryIndex][surfaceIndex].Count == 0)
                        {
                            //if all objects are children pass them in
                            if (isCollectingChildren)
                            {
                                //Collect sources from all geometries
                                Transform surface = WorldGeometry.transform;
                                LayerMask layerMask = Surfaces[surfaceIndex].layerMask;
                                NavMeshCollectGeometry collectGeometry = Surfaces[surfaceIndex].useGeometry;
                                int navMeshSurface = Surfaces[surfaceIndex].defaultArea;
                                NavMeshBuilder.CollectSources(surface, layerMask, collectGeometry, navMeshSurface, MarkupsPerSurface[geometryIndex][surfaceIndex], SourcesPerSurface[geometryIndex][surfaceIndex]);
                            }
                            else
                            {
                                //else collect sources from within the specified bounds
                                LayerMask layerMask = Surfaces[surfaceIndex].layerMask;
                                NavMeshCollectGeometry collectGeometry = Surfaces[surfaceIndex].useGeometry;
                                int navMeshSurface = Surfaces[surfaceIndex].defaultArea;
                                NavMeshBuilder.CollectSources(navMeshBounds, layerMask, collectGeometry, navMeshSurface, MarkupsPerSurface[geometryIndex][surfaceIndex], SourcesPerSurface[geometryIndex][surfaceIndex]);
                            }
                        }

                        if (Async)
                        {
                            AsyncOperation navMeshUpdateOperation = NavMeshBuilder.UpdateNavMeshDataAsync(NavMeshDatas[geometryIndex][surfaceIndex], NavMesh.GetSettingsByIndex(Surfaces[surfaceIndex].agentTypeID), SourcesPerSurface[geometryIndex][surfaceIndex], navMeshBounds);
                            OnEventTriggered.AddListener(delegate { Surfaces[surfaceIndex].BuildNavMesh(); });
                            navMeshUpdateOperation.completed += HandleNavMeshUpdate;
                        }
                        else
                        {
                            bool success = NavMeshBuilder.UpdateNavMeshData(NavMeshDatas[geometryIndex][surfaceIndex], NavMesh.GetSettingsByIndex(Surfaces[surfaceIndex].agentTypeID), SourcesPerSurface[geometryIndex][surfaceIndex], navMeshBounds);
                            Surfaces[surfaceIndex].BuildNavMesh();
                            OnNavMeshUpdate?.Invoke(navMeshBounds);
                            if (success)
                            {
                                Debug.Log("NavMesh Success");
                            }
                        }
                    }
                }
            }
        }
    }

    private bool CompareAgentId(Dictionary<string, int> dict, int id2) => dict.Values.ToList().IndexOf(id2) != -1;

    private void HandleNavMeshUpdate(AsyncOperation Operation)
    {
        bool success = Operation.isDone;
        if (success)
        {
            Debug.Log("NavMesh Success");
        }

        OnEventTriggered.Invoke();
        return;
    }

    private void SpawnMonsters()
    {
        foreach ((GameObject, Vector3) monster in monstersToSpawn)
        {
            Instantiate(monster.Item1, monster.Item2, monster.Item1.transform.rotation);
        }
    }

    private void SpawnMonsterItems()
    {
        foreach ((GameObject, Vector3) monster in monstersToSpawn)
        {
            if (monster.Item1.TryGetComponent<InitializeItems>(out InitializeItems initializeItemsScript))
            {
                initializeItemsScript.SetUpItems(monster.Item2, allRoomTiles, _rand);
            }
        }
    }

    private void ConnectPortals()
    {
        BlackboardKey lobbyPortalKey = blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.LobbyPortal);
        BlackboardKey receptionPortalKey = blackboardController.GetKey(BlackboardController.BlackboardKeyStrings.ReceptionPortal);
        if (blackboard.TryGetValue(lobbyPortalKey, out Portal lobbyPortal))
        {
            if (blackboard.TryGetValue(receptionPortalKey, out Portal receptionPortal))
            {
                lobbyPortal.targetPortal = receptionPortal;
                receptionPortal.targetPortal = lobbyPortal;
            }
        }
    }

    private void CleanUp()
    {
        //Deactivate or destory everything that isn't needed
        //TileProperties being one of them
        //Maybe it'd be better to just turn off the update for TileProperties
        TileProperties[] tilePropertiesArray = GameObject.FindObjectsByType<TileProperties>(FindObjectsSortMode.None);
        for (int i = 0; i < tilePropertiesArray.Length; i++)
        {
            tilePropertiesArray[i].shouldUpdate = false;
        }
    }

    public void Shutdown() => Destroy(WorldGeometry);

    public void Restart()
    {

    }

    private void Update()
    {
        if (_triangulation)
        {
            foreach (var pair in toDrawTriangulation)
            {
                //lineDrawer.DrawLineInGameView(pair.a, pair.b, Color.red);

                Debug.DrawLine(new Vector3(pair.a.x, .5f, pair.a.y), new Vector3(pair.b.x, .5f, pair.b.y), UnityEngine.Color.black);
            }
        }

        if (_mst)
        {
            foreach (var pair in toDrawMst)
            {
                //lineDrawer.DrawLineInGameView(pair.a, pair.b, Color.red);

                Debug.DrawLine(new Vector3(pair.a.x, .5f, pair.a.y), new Vector3(pair.b.x, .5f, pair.b.y), UnityEngine.Color.red);
            }
        }

        if (_additionalEdges)
        {
            foreach (var pair in toDrawAdditionalEdges)
            {
                //lineDrawer.DrawLineInGameView(pair.a, pair.b, Color.red);

                Debug.DrawLine(new Vector3(pair.a.x, .5f, pair.a.y), new Vector3(pair.b.x, .5f, pair.b.y), UnityEngine.Color.red);
            }
        }

        if (_failedEdges)
        {
            foreach (var pair in toDrawFailedEdges)
            {
                //lineDrawer.DrawLineInGameView(pair.a, pair.b, Color.red);

                Debug.DrawLine(new Vector3(pair.a.x, .5f, pair.a.y), new Vector3(pair.b.x, .5f, pair.b.y), UnityEngine.Color.black);
            }
        }
    }

    private void OnDrawGizmos()
    {
        //foreach (Room room in rooms)
        //{
        //    Gizmos.DrawWireCube(room.bounds.center, room.bounds.size);
        //}
        if (_overlapCubes)
        {
            foreach (cube cube in toDrawDoorCube)
            {
                // Draw a semitransparent red cube at the transforms position
                Gizmos.color = new Color(1, 0, 0, 0.5f);
                Gizmos.DrawCube(cube.position, cube.size);
            }
        }

        if (_overlapCubes)
        {
            foreach (cube cube in toDrawFreeCube)
            {
                // Draw a semitransparent red cube at the transforms position
                Gizmos.color = new Color(0, 1, 0, 0.5f);
                Gizmos.DrawCube(cube.position, cube.size);
            }
        }
    }
}
