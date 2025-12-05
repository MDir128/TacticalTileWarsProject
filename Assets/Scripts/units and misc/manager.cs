using System;
using UnityEngine;

public class manager : MonoBehaviour
{
    [Header("prefabs")]
    [SerializeField] GameObject mappref;
    [SerializeField] GameObject commander;
    [SerializeField] GameObject[] commandersfirst;
    [SerializeField] GameObject[] commanderssecond;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject map = Instantiate(mappref, transform);
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
