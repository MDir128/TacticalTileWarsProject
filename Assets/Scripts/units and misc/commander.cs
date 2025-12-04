using UnityEngine;

public class commander : MonoBehaviour
{
    [Header("params")]
    [SerializeField] public string our_teamname;
    [SerializeField] public string my_name;
    [SerializeField] public GameObject[] squads;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i<squads.Length;i++) {
            GameObject generic_prefab = new GameObject();
            squadcontrol sqctr = generic_prefab.AddComponent<squadcontrol>();
            if (our_teamname == "Blue") {
                sqctr.squadcolor = new Color(0, 0, 255); }
            else if (our_teamname == "Blue")
            {
                sqctr.squadcolor = new Color(255, 0, 0);
            }
            squads[i] = Instantiate(generic_prefab);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
