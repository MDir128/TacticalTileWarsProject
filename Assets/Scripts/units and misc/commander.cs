using UnityEngine;

public class commander : MonoBehaviour
{
    [Header("params")]
    [SerializeField] public string our_teamname;
    [SerializeField] public string my_name;
    [SerializeField] public GameObject[] squads;
    [SerializeField] public GameObject unitprefab;
    public void Init(string our_teamname, string my_name, GameObject[] squads, GameObject unitpref)
    {
        this.our_teamname = our_teamname;
        this.my_name = my_name;
        this.squads = squads;
        this.unitprefab = unitpref;
        for (int i = 0; i < squads.Length; i++)
        {
            var t = new GameObject(my_name + i);
            squads[i] = Instantiate(t, transform.position, transform.rotation);
            Destroy(t);
            squadcontrol sqctr = squads[i].AddComponent<squadcontrol>();
            if (our_teamname == "Blue")
            {
                sqctr.squadcolor = new Color(78f/255f, 82f/255f, 185f/255f);
            }
            else if (our_teamname == "Red")
            {
                sqctr.squadcolor = new Color(161f/255f, 39f/255f, 39f/255f);
            }
            sqctr.unit_prefab = unitprefab;
            sqctr.Init(our_teamname, my_name+i);
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
