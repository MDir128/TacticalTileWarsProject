using System;
using UnityEngine;
using UnityEngine.Rendering;

public class squadcontrol : MonoBehaviour
{
    [Header("squad stats")]
    [SerializeField] public float attackrange = 1.5f;
    [SerializeField] public float walkrange = 3f;
    [SerializeField] public string our_teamname;
    [SerializeField] public string this_squadname;
    [SerializeField] public int squad_size = 21;
    [SerializeField] public float overallhealth;

    [SerializeField] public float damage = 2f;
    [SerializeField] public float attackdelay = 1f;
    [SerializeField] public float health = 10f;
    [Header("misc")]
    [SerializeField] public GameObject[] units = new GameObject[21];
    [SerializeField] public GameObject unit_prefab;
    [SerializeField] public GameObject basedEnemyName;
    [SerializeField] public Color squadcolor;
    public float speed = 0.5f;
    [SerializeField] statblock squadstats;

    public int squadId;  //для того чтобы я находил того кто получил модификации
    private static int nextSquadId =1;
    public void Init(string our_teamname, string this_squadname)
    {
        this.our_teamname = our_teamname;
        this.this_squadname = this_squadname;
        // Создания юнитов
        {
            if (squadstats == null)
            {
                squadstats = new statblock();
            }

            squadId = nextSquadId;
            nextSquadId++; //Теперь отряды нумерованны, ну что за антиутопия!
                           //squadModifier[squadId] = new list<squadModifier>()

            units = new GameObject[squad_size];
            for (int i = 0; i < units.Length; i++)
            {
                units[i] = Instantiate(unit_prefab, transform);
                uniticontrol uniticontrol = units[i].GetComponentInChildren<uniticontrol>();
                uniticontrol.my_squadname = our_teamname + this_squadname;
                uniticontrol.my_teamname = our_teamname;
                SpriteRenderer renderer = units[i].GetComponentInChildren<SpriteRenderer>();
                renderer.color = squadcolor;
            }

            SetPositions();
            UpdateOverallHealth();
            //SetEnemySquad(null); — убрал строчку, ведь squadcontrol Enemy в момент работы программы становятся null и вызывают ошибки
            //SetEnemySquad(basedEnemyName.GetComponent<squadcontrol>()); //так выставляется противник
        }
    }
    void Start()
    {

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
                //ДОБАВИЛ ДОПОЛНИТЕЛЬНУЮ ПРОВЕРКУ
                uniticontrol currunit = units[i].GetComponent<uniticontrol>();
                if (currunit != null && currunit.statblock != null)
                {
                    health_sum += currunit.statblock.health;
                }
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
        Unit_formation formation = new Unit_formation(new SemiRoundGen());
        Vector3[] positions = formation.Genarate_formation(squad_size, transform.position, 3f, new Vector3(1,0,0));
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
    //НОВЫЙ МЕТОД ПОДСЧЁТА ЮНИТОВ В СКВАДЕ
    public int CountAliveUnits()
    {
        int alive_units_number = 0;
        if (units == null)
        {
            return 0;
        }
        for (int i = 0; i < units.Length; i++)
        {
            if (units[i] != null)
            {
                uniticontrol oneunit = units[i].GetComponent<uniticontrol>(); //получение ссылки на объект каждого юнита
                if (oneunit != null && oneunit.statblock != null && oneunit.statblock.health > 0)
                {
                    alive_units_number++;
                }
            }
        }
        return alive_units_number;
    }
}

