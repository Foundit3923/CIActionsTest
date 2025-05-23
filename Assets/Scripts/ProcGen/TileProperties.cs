using System;
using System.Collections.Generic;
using Mirror.BouncyCastle.Asn1.Cmp;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class TileProperties : MonoBehaviour
{
    public enum Lighting
    {
        On,
        Off,
        Flickering
    }
    public delegate void TypeChangeEvent();
    public static event TypeChangeEvent OnTypeChange;

    public delegate void MaterialChangeEvent();
    public static event MaterialChangeEvent OnMaterialChange;
    [SerializeField] public string name;
    [SerializeField] public Vector3Int size;
    [SerializeField] public GameObject blueprintPrefab;
    [SerializeField] public Material blueprintColor;
    [SerializeField] TileProperties hallwayProps;
    [SerializeField] public Lighting lighting;
    [SerializeField] public List<GameObject> walls = new();
    public Dictionary<string, Vector3Int> neighborTiles = new();
    private GrimmGen.CellType cellType;
    [SerializeField]
    public GrimmGen.CellType CellType
    {
        get
        {
            if (cellType != null)
            {
                return cellType;
            }

            return cellType = GrimmGen.CellType.None;
        }
        set => cellType = value;//OnTypeChange();
    }
    [SerializeField] public Vector3Int WorldPosition;
    public GrimmGen.Tile FloorTile;
    public bool shouldUpdate;
    public BoundsInt bounds;
    public Vector3 v3BlueprintScale;
    public Vector2 v2BlueprintScale;
    public Vector2 halfSize;
    public Vector2 v2Size;
    Renderer render;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        shouldUpdate = true;
        render = GetComponentInChildren<Renderer>();
        Vector3Int position = new((int)transform.position.x, (int)transform.position.y, (int)transform.position.z);
        int unitSize = (int)render.bounds.size.x;
        bounds = new BoundsInt(position, size * unitSize);
        //WorldPosition = bounds.min;
        v2Size = new Vector2(size.x, size.z);
        halfSize = new Vector2(size.x / 2, size.z / 2);
        //Scale is based on hallway size. Hallway should have the same size for x y and zx
        v3BlueprintScale = size / (16 * 2);// hallwayProps.size.x;
        v2BlueprintScale = new Vector2(v3BlueprintScale.x, v3BlueprintScale.z);

    }

    private void Start() => GrimmGen.OnPruneTile += SetActive;

    public void SetActive(bool state)
    {
        if (FloorTile._type == GrimmGen.CellType.None)
        {
            this.gameObject.SetActive(state);
            if (state)
            {
                this.gameObject.GetComponentInChildren<NavMeshModifier>().enabled = true;
                this.gameObject.GetComponentInChildren<NavMeshModifier>().overrideArea = true;
                this.gameObject.GetComponentInChildren<NavMeshModifier>().area = NavMesh.GetAreaFromName("Walkable");
            }
            else
            {
                this.gameObject.GetComponentInChildren<NavMeshModifier>().overrideArea = true;
                this.gameObject.GetComponentInChildren<NavMeshModifier>().area = NavMesh.GetAreaFromName("Not Walkable");
                this.gameObject.GetComponentInChildren<NavMeshModifier>().enabled = false;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        //if (!shouldUpdate) { return; }
        //Renderer render = GetComponentInChildren<Renderer>();
        //Vector3Int position = new Vector3Int((int)transform.position.x, (int)transform.position.y, (int)transform.position.z);
        //int unitSize = (int)render.bounds.size.x;
        //bounds = new BoundsInt(position, (size * unitSize));
    }

    public Vector2 GetBlueprintScale(int scale)
    {
        v3BlueprintScale = size / scale;
        v2BlueprintScale = new Vector2(v3BlueprintScale.x, v3BlueprintScale.z);
        return v2BlueprintScale;
    }

    public List<GameObject> GetWallObjects() => throw new NotImplementedException();

    public void SetMaterial(Material material)
    {
        render.material = material;
        OnMaterialChange();
    }

    public void RegisterMaterialCallback() => OnMaterialChange += PrintTrace;

    public void RegisterTypeCallback() => OnTypeChange += PrintTrace;

    private void PrintTrace()
    {
        System.Diagnostics.StackTrace t = new();
        Debug.Log(t.ToString());
    }
}
