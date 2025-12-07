using System;
using UnityEngine;

public class manager : MonoBehaviour
{
    [Header("prefabs")]
    [SerializeField] GameObject mappref;
    [SerializeField] GameObject commanderblue;
    [SerializeField] GameObject commanderred;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [Header("entities")]
    [SerializeField] private HexagonalMap map;
    [SerializeField] private GameObject[] commandersfirst;
    [SerializeField] private GameObject[] commanderssecond;
    [Header("misc_data")]
    [SerializeField] Vector3[] castlescords;
    void Start()
    {
        map = Instantiate(mappref, transform).GetComponent<HexagonalMap>();
        commandersfirst = new GameObject[3];
        commanderssecond = new GameObject[3];
        map.onload += (s, e) =>
        {
            Get_Castles();
            for (int i = 0; i < commandersfirst.Length; i++)
            {
                commandersfirst[i] = Instantiate(commanderred, castlescords[i], new Quaternion()).gameObject;
                Debug.Log(castlescords[i]);
                commander param = commandersfirst[i].GetComponentInChildren<commander>();
                commandersfirst[i].name = Convert.ToString(i) + "playerRed";
            }
            for (int i = 0; i < commanderssecond.Length; i++)
            {
                commanderssecond[i] = Instantiate(commanderblue, castlescords[i + 3], new Quaternion());
                commander param = commanderssecond[i].GetComponentInChildren<commander>();
                commanderssecond[i].name = Convert.ToString(i) + "playerBlue";
            }
        };
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void Get_Castles()
    {
        castlescords = new Vector3[6];
        foreach (var castle in map.GetCastlesDictionary()) {
            castlescords[castle.Key-1] = new Vector3(castle.Value.x, castle.Value.y, -1f);
        }
    }

    public (GameObject[] blueCommanders, GameObject[] redCommanders) GetCommandersForCardSystem()
    {
        // Возвращаем синих (игрок) и красных (враг) командиров
        return (commanderssecond, commandersfirst);
    }
    
    // Альтернативный вариант - раздельные геттеры
    public GameObject[] GetBlueCommanders() => commanderssecond;
    public GameObject[] GetRedCommanders() => commandersfirst;

}

