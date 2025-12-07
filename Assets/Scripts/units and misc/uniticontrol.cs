using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class uniticontrol : MonoBehaviour
{
    [Header("options")]
    [SerializeField] public statblock statblock = null;
    [SerializeField] public string enemy_squadname;
    [SerializeField] public string my_squadname;
    [SerializeField] public string my_teamname;
    [SerializeField] public float recharge;
    [SerializeField] public uniticontrol targetted_enemy;
    [SerializeField] public GameObject my_position;
    [SerializeField] public EnemyCommander targetted_enemy_commander;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private Dictionary<GameObject , uniticontrol> enemiescache;
    private float NextTimeSearching = 0f;
    private float SearchingInterval = 1f;
    void Start()
    {
        enemiescache = new Dictionary<GameObject , uniticontrol>();
        if (statblock == null) {statblock = new statblock();}
        //����� ����� ���� � ��������� ��� ������ ����� ������
        if (statblock.unit_type == "melee")
        {
            statblock.attackrange = 1.5f;
            statblock.damage = 2f;
            statblock.health = 10f;
            statblock.speed = 0.5f;
        }
        if (statblock.unit_type == "range")
        {
            statblock.attackrange = 5f;
            statblock.damage = 1.5f;
            statblock.health = 8f;
            statblock.speed = 0.7f;
        }
        if (statblock.unit_type == "cavalry")
        {
            statblock.attackrange = 1.8f;
            statblock.damage = 3f;
            statblock.health = 15f;
            statblock.speed = 1.2f;
        }
        targetted_enemy = null;
        recharge = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (targetted_enemy != null)
        {
            MoveToTarget();
        }
        else
        {
            //будет осуществляться поиск цели только, если прошло время для следующего поиска
            if (Time.time >= NextTimeSearching)
            {
                FindTarget();
            }
            Return_to_point();
        }
        if (recharge <= 0) {
            string output = Attack();
            if (output != "no")
            {
                recharge = statblock.attackdelay;
            }
        }
        else
        {
            recharge -= Time.deltaTime;
        }
    }

    public void FindTarget()
    {
        if (Time.time < NextTimeSearching)
        {
            return;
        }
        float findrange = math.max(statblock.walkrange, statblock.attackrange) * 50f;
        float closest = findrange + 10;
        targetted_enemy = null;
        Collider2D[] possible_enemy = Physics2D.OverlapCircleAll(transform.position, findrange);
        foreach (Collider2D collider in possible_enemy)
        {
            uniticontrol unit = GetUnitscript(collider.gameObject);
            if (unit != null && unit.my_teamname != my_teamname)
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance < closest)
                {
                    closest = distance;
                    targetted_enemy = unit;
                    enemy_squadname = unit.my_squadname;
                }
            }
        }
        NextTimeSearching = Time.time + SearchingInterval;
    }
    public string Attack()
    {
        uniticontrol targetted = null;
        Collider2D[] hitpossible = Physics2D.OverlapCircleAll(transform.position, statblock.attackrange);
        if (targetted_enemy_commander != null)
        {
            float distance = Vector3.Distance(transform.position, targetted_enemy_commander.transform.position);
            if (distance <= statblock.attackrange)
            {
                targetted_enemy_commander.HurtCommander(statblock.damage);
                return "success";
            }
        }
        foreach (Collider2D collider in hitpossible)
        {
            uniticontrol victim = GetUnitscript(collider.gameObject);
            if (victim != null && victim.my_teamname != my_teamname)
            {
                float clothest = statblock.attackrange + 1;
                float distance = Vector3.Distance(transform.position, victim.transform.position);
                if (distance < clothest && distance<=statblock.attackrange)
                {
                    clothest = distance;
                    targetted = victim;
                }
            }
        }
        if (targetted != null) {
            targetted.Get_Hurt(statblock.damage);
            return "success";
        }
        else
        {
            foreach (Collider2D collider in hitpossible)
            {   
                EnemyCommander EnemyCommander = collider.GetComponent<EnemyCommander>();
                if (EnemyCommander != null && EnemyCommander.TeamName != my_teamname)
                {
                    EnemyCommander.HurtCommander(statblock.damage);
                    return "success";
                }

                AllyCommander AllyCommander = collider.GetComponent<AllyCommander>();
                if (AllyCommander != null && AllyCommander.TeamName != my_teamname)
                {
                    AllyCommander.HurtCommander(statblock.damage);
                    return "success";
                }

                Commander_Rules PlayerCommander = collider.GetComponent<Commander_Rules>();
                if (PlayerCommander != null && PlayerCommander.TeamName != my_teamname)
                {
                    PlayerCommander.HurtCommander(statblock.damage);
                    return "success";
                }
            }
            return "no";
        }
    }
    void MoveToTarget()
    {
        float distance = Vector3.Distance(transform.position, targetted_enemy.transform.position);
        if (targetted_enemy != null && distance> statblock.attackrange)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetted_enemy.transform.position,
                statblock.speed * Time.deltaTime
                );
        }
    }
    void Return_to_point()
    {
        if (my_position == null)
        {
            return;
        }
        transform.position = Vector3.MoveTowards(
            transform.position,
            my_position.transform.position, 
            statblock.speed * Time.deltaTime
            );
    }
    void Get_Hurt(float damagedealed)
    {
        statblock.health -= damagedealed;
        if (statblock.health <= 0)
        {
            squadcontrol parentsquad = GetComponentInParent<squadcontrol>(); 
            if (parentsquad != null)
            {
                parentsquad.UpdateOverallHealth();
                if (parentsquad.CountAliveUnits() == 0)
                {
                    EnemyCommander[] enemycommanders = FindObjectsByType<EnemyCommander>(FindObjectsSortMode.InstanceID);
                    foreach (var commander in enemycommanders)
                    {
                        if (commander != null && commander.TeamName == my_teamname)
                        {
                            commander.SquadDeath(parentsquad);
                        }
                    }
                    AllyCommander[] allycommanders = FindObjectsByType<AllyCommander>(FindObjectsSortMode.InstanceID);
                    foreach (var commander in allycommanders)
                    {
                        if (commander != null && commander.TeamName == my_teamname)
                        {
                            commander.SquadDeath(parentsquad);
                        }
                    }
                    Destroy(parentsquad.gameObject);
                    Commander_Rules PlayerCommander = FindFirstObjectByType<Commander_Rules>();
                    if (PlayerCommander != null && PlayerCommander.TeamName == my_teamname)
                    {
                        PlayerCommander.SquadDeath(parentsquad);
                    }
                    Destroy(parentsquad.gameObject);
                }
            }
            Destroy(gameObject);
        }
    }
    uniticontrol GetUnitscript(GameObject gameObject) 
    {
        if (!enemiescache.ContainsKey(gameObject)) {
            try
            {
                enemiescache[gameObject] = gameObject.GetComponent<uniticontrol>();
            }
            catch { }
        }
        return enemiescache[gameObject];
    }
}

