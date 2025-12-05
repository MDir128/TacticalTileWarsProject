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
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private Dictionary<GameObject , uniticontrol> enemiescache;
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
            if (Vector3.Distance(transform.position, targetted_enemy.transform.position) < statblock.walkrange)
            MoveToTarget();
        }
        else
        {
            Return_to_point();
            FindTarget();
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
        float findrange = math.max(statblock.walkrange, statblock.attackrange);
        float clothest = findrange + 10;
        targetted_enemy = null;
        Collider2D[] possible_enemy = Physics2D.OverlapCircleAll(transform.position, findrange);
        foreach (Collider2D collider in possible_enemy)
        {
            uniticontrol unit = GetUnitscript(collider.gameObject);
            if (unit != null && unit.my_squadname == enemy_squadname)
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance < clothest)
                {
                    clothest = distance;
                    targetted_enemy = unit;
                }
            }
        }
    }
    public string Attack()
    {
        uniticontrol targetted = null;
        Collider2D[] hitpossible = Physics2D.OverlapCircleAll(transform.position, statblock.attackrange);
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
            //������� ����� �� ���������� ��������� (�٨ ����� �������� ����� �� ��������� ������)
            foreach (Collider2D collider in hitpossible)
            {
                EnemyCommander EnemyCommander = collider.GetComponent<EnemyCommander>();
                if (EnemyCommander != null && EnemyCommander.TeamName != my_teamname)
                {
                    EnemyCommander.HurtCommander(statblock.damage);
                    return "success";
                }
            }
            return "no";
        }
    }
    void MoveToTarget()
    {
        float distance = Vector3.Distance(transform.position, targetted_enemy.transform.position);
        if (targetted_enemy != null && distance>= statblock.attackrange*0.8)
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
            //����� ����������: ����� �������� ������ ����������, � �� ���������� ����� �� � ����� �������� �� ���̨�
            squadcontrol parentsquad = GetComponentInParent<squadcontrol>(); 
            if (parentsquad != null)
            {
                parentsquad.UpdateOverallHealth();
                if (parentsquad.CountAliveUnits() == 0)
                {
                    EnemyCommander EnemyCommander = FindFirstObjectByType<EnemyCommander>();
                    if (EnemyCommander != null && EnemyCommander.TeamName == my_teamname)
                    {
                        EnemyCommander.SquadDeath(parentsquad);
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

