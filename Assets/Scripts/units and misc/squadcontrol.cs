using System;
using UnityEngine;
public class squadcontrol : MonoBehaviour
{
    [Header("squad stats")]
    [SerializeField] public float atackrange = 1.5f;
    [SerializeField] public float walkrange = 3f;
    [SerializeField] public string our_teamname;
    [SerializeField] public string this_squadname;
    [SerializeField] public int squad_size;
    [SerializeField] public float overallhealth;

    [SerializeField] public float damage = 2f;
    [SerializeField] public float atackdelay = 1f;
    [SerializeField] public float health = 10f;
    [Header("misc")]
    [SerializeField] public GameObject[] units;
    [SerializeField] public GameObject unit_prefab;
    [SerializeField] public GameObject basedEnemyName;
    [SerializeField] public Color squadcolor;
    public float speed = 0.5f;
    [SerializeField] statblock squadstats;
    void Start()
    {
        units = new GameObject[squad_size];
        for (int i = 0; i < units.Length; i++) {
            units[i] = Instantiate(unit_prefab, transform);
            uniticontrol uniticontrol = units[i].GetComponentInChildren<uniticontrol>();
            uniticontrol.my_squadname = our_teamname + this_squadname;
            uniticontrol.my_teamname = our_teamname;
            SpriteRenderer renderer = units[i].GetComponentInChildren<SpriteRenderer>();
            renderer.color = squadcolor;
        }
        
        SetPositions();
        //SetEnemySquad(null); — убрал строчку, ведь squadcontrol Enemy в момент работы программы становятся null и вызывают ошибки
        //SetEnemySquad(basedEnemyName.GetComponent<squadcontrol>()); //так выставляется противник
    }
    private void FixedUpdate()
    {
        if (basedEnemyName != null)
        {
            squadcontrol EnemySquadComponent = basedEnemyName.GetComponent<squadcontrol>(); //сначала выношу в отдельную переменную
            if (EnemySquadComponent != null) //потом проверяю её
            {
                SetEnemySquad(EnemySquadComponent); //и только после этого она устанавливается
            }
        }
    }
    public void UpdateOverallHealth()
    {
        float health_sum = 0;
        for (int i = 0; i < units.Length; i++)
        {
            if (units[i] != null)
            {
                health_sum += units[i].GetComponent<uniticontrol>().statblock.health;
            }
        }
        overallhealth = health_sum;
    } // эта функция обновляет поле общего здоровья отряда
    public void SetEnemySquad(squadcontrol Enemy)
    {
        if (Enemy != null && units != null) { 
        string enemy_name =Enemy.our_teamname + Enemy.this_squadname;
        for (int i = 0; i < units.Length; i++)
        {
                if (units[i] != null)
                {
                    uniticontrol uniticontrol = units[i].GetComponentInChildren<uniticontrol>();
                    if (uniticontrol != null) //дополнительная проверка
                    {
                        uniticontrol.enemy_squadname = enemy_name;
                    }
                }
        }}
    } //эта функция определяет сквад противника под атаку
    public void SetPositions()
    {
        Unit_formation formation = new Unit_formation(new SkirmishGen());
        Vector3[] positions = formation.Genarate_formation(squad_size, transform.position, 3f);
        for (int i = 0; i < units.Length; i++) {
            if (units[i] != null)
            {
                units[i].transform.position = positions[i];
            }
        }
    } // Эта функция обновляет построение отряда, переставляя целевые позиции юнитов по активному генератору позиций (в данном случае - рассыпной)
    public void Gotopoint_global(Vector3 point)
    {
        transform.position = Vector3.MoveTowards(
            transform.position, point, squadstats.speed * Time.deltaTime
            );
    } // перемещение по глобальным координатам. *Нужно будет переработать, чтобы двигать юнитов по тодельности, а не весь сет.
      // Скорее всего, нужно будет в мозгах юнита убрать родительский префаб, сделав самостоятельным объектом и включать им функцию возращения на позицию. Но это потом.
    public void Gotopoint_local()
    {

    } // заглушка = должно по идее заставлять отряд двигаться к координатам, относительно командира
    public statblock GetEnemyStats_statblock(squadcontrol Enemy)
    {
        return Enemy.squadstats;
    }
}
