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

    private Dictionary<GameObject , uniticontrol> enamiescache;
    void Start()
    {
        enamiescache = new Dictionary<GameObject , uniticontrol>();
        if (statblock == null) {statblock = new statblock();}
        targetted_enemy = null;
        recharge = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (targetted_enemy != null)
        {
            if (Vector3.Distance(transform.position, targetted_enemy.transform.position) < statblock.walkrange);
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
                recharge = statblock.atackdelay;
            }
        }
        else
        {
            recharge -= Time.deltaTime;
        }
    }

    public void FindTarget()
    {
        float findrange = math.max(statblock.walkrange, statblock.atackrange);
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
        Collider2D[] hitpossible = Physics2D.OverlapCircleAll(transform.position, statblock.atackrange);
        foreach (Collider2D collider in hitpossible)
        {
            uniticontrol victim = GetUnitscript(collider.gameObject);
            if (victim != null && victim.my_teamname != my_teamname)
            {
                float clothest = statblock.atackrange + 1;
                float distance = Vector3.Distance(transform.position, victim.transform.position);
                if (distance < clothest && distance<=statblock.atackrange)
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
            return "no";
        }
    }
    void MoveToTarget()
    {
        float distance = Vector3.Distance(transform.position, targetted_enemy.transform.position);
        if (targetted_enemy != null && distance>= statblock.atackrange*0.8)
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
            Destroy(gameObject);
        }
    }
    uniticontrol GetUnitscript(GameObject gameObject) 
    {
        if (!enamiescache.ContainsKey(gameObject)) {
            try
            {
                enamiescache[gameObject] = gameObject.GetComponent<uniticontrol>();
            }
            catch { }
        }
        return enamiescache[gameObject];
    }
}

