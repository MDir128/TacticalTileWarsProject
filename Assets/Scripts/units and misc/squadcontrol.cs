using System;
using UnityEngine;
public class squadcontrol : MonoBehaviour
{
    [Header("squad stats")]
    [SerializeField] statblock squadstats;
    [SerializeField] public float atackrange = 1.5f;
    [SerializeField] public float walkrange = 3f;
    [SerializeField] public string our_teamname;
    [SerializeField] public string this_squadname;
    [SerializeField] public int squad_size;

    [SerializeField] public float damage = 2f;
    [SerializeField] public float atackdelay = 1f;
    [SerializeField] public float health = 10f;
    [Header("misc")]
    [SerializeField] public GameObject[] units;
    [SerializeField] public GameObject unit_prefab;
    [SerializeField] public string basedAnemyName;
    [SerializeField] public Color squadcolor;
    public float speed = 0.5f;
    void Start()
    {
        units = new GameObject[squad_size];
        for (int i = 0; i < units.Length; i++) {
            units[i] = Instantiate(unit_prefab, transform);
            uniticontrol uniticontrol = units[i].GetComponentInChildren<uniticontrol>();
            uniticontrol.my_squadname = our_teamname+this_squadname;
            uniticontrol.my_teamname = our_teamname;
            SpriteRenderer renderer = units[i].GetComponentInChildren<SpriteRenderer>();
            renderer.color = squadcolor;
        }
        
        SetPositions();
        SetAnemySquad(basedAnemyName);
    }
    private void FixedUpdate()
    {
        
    }
    public void SetAnemySquad(string anemy_name)
    {
        for (int i = 0; i < units.Length; i++)
        {
            uniticontrol uniticontrol = units[i].GetComponentInChildren<uniticontrol>();
            uniticontrol.anemy_squadname=anemy_name;
        }
    }
    public void SetPositions()
    {
        Unit_formation formation = new Unit_formation(new SkirmishGen());
        Vector3[] positions = formation.Genarate_formation(squad_size, transform.position, 3f);
        for (int i = 0; i < units.Length; i++) {
            units[i].transform.position = positions[i];
        }
    }
}
