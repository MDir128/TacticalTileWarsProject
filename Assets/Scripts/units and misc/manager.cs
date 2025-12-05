using System;
using UnityEngine;

public class manager : MonoBehaviour
{
    [Header("prefabs")]
    [SerializeField] GameObject mappref;
    [SerializeField] GameObject commander;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [Header("entities")]
    [SerializeField] private HexagonalMap map;
    [SerializeField] private GameObject[] commandersfirst;
    [SerializeField] private GameObject[] commanderssecond;
    void Start()
    {
        map = Instantiate(mappref, transform).GetComponent<HexagonalMap>();
        //Vector2 initialpositions = 
        for (int i = 0; i < commandersfirst.Length; i++)
        {
            commandersfirst[i] = Instantiate(commander, transform);
            commander param = commandersfirst[i].GetComponent<commander>();
            param.our_teamname = "Red";
            param.my_name = Convert.ToString(i) + "playerRed";
        }
        for (int i = 0; i < commanderssecond.Length; i++)
        {
            commandersfirst[i] = Instantiate(commander, transform);
            commander param = commanderssecond[i].GetComponent<commander>();
            param.our_teamname = "Blue";
            param.my_name = Convert.ToString(i) + "playerBlue";
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
