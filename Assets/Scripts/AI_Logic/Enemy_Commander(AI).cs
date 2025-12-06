using System.Collections.Generic;
using System.Data.SqlTypes;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.InputSystem.OnScreen.OnScreenStick;

public class EnemyCommander : MonoBehaviour
{
    [Header("Enemy AI Commander Stats")]
    [SerializeField] public string TeamName = "Red";
    [SerializeField] public string MyName = "Jack";
    [SerializeField] private float OurEnemyCommanderHealth = 20f;
    [SerializeField] private float DecisionsIntervalTime = 3f; //логика продолжительности отрезков времени между принятиями решений (атаковать, отступать) в секундах
    [Header("Enemy squads settings")]
    [SerializeField] private squadcontrol[] OurEnemySquads = new squadcontrol[3]; //массив отрядов (ближний бой, дальнобойные и конницы)
    [SerializeField] public GameObject unitprefab;
    [Header("Other settings")]
    private string Behavior = "Attack"; //модель поведения 
    private float LogicTime = 0f; //таймер времени следующего решения
    private commander commander;
    private bool flagdead = false;

    void Start()    
    {
        commander = gameObject.AddComponent<commander>(); //нахождение и определение типа командира 
        commander.Init(TeamName, MyName, new GameObject[3], unitprefab);
        for (int i = 0; i < 3; i++)
        {
            OurEnemySquads[i] = commander.squads[i].GetComponent<squadcontrol>();
        }
    }

    void Update()
    {
        if (flagdead == true)
        {
            return;
        }
        //определение поведения
        if (Time.time >= LogicTime)
        {
            BehaviorAssessment();
            LogicTime = Time.time + DecisionsIntervalTime; //таймер принятия следующего решения увеличивается
        }
        ChoiceBehavior(); //каждый кадр выбирается поведение
        //проверка смерти командира
        if (OurEnemyCommanderHealth <= 0)
        {
            CommanderDeath();
            return;
        }
    }

    //ПОДСЧЁТ ЮНИТОВ
    int EnemyAliveUnits() //подсчёт выживших союзных юнитов
    {
        int enemy_units = 0;
        foreach (var squad in OurEnemySquads)
        {
            if (squad != null)
            {
                enemy_units += squad.CountAliveUnits();
            }    
        }
        return enemy_units;
    }
    int PlayerAliveUnits() //подсчёт выживших юнитов игрока
    {
        squadcontrol[] AllSquads = FindObjectsByType<squadcontrol>(FindObjectsSortMode.InstanceID); //поиск сквадов игрока
        int player_units = 0;
        foreach (var squad in AllSquads)
        {
            if (squad != null && squad.our_teamname == "Blue")
            {
                player_units += squad.CountAliveUnits();
            }
        }
        return player_units;
    }

    squadcontrol ClosestEnemySquad() //нахождение ближайшего вражеского отряда 
    {
        squadcontrol[] AllSquads = FindObjectsByType<squadcontrol>(FindObjectsSortMode.InstanceID);
        squadcontrol closest_enemysquad = null;
        float mindistance = Mathf.Infinity; //полезно, ведь при будущем сравнении не найдётся отряда большего положительной бесконечности
        foreach (var squad in AllSquads)
        {
            if (squad != null && squad.our_teamname != TeamName && squad.CountAliveUnits() > 0)
            {
                float distance = Vector3.Distance(transform.position, squad.transform.position);
                if (distance < mindistance)
                {
                    mindistance = distance;
                    closest_enemysquad = squad;
                }
            }
        }
        return closest_enemysquad;
    }

    //ОЦЕНКА УГРОЗЫ И ОПРЕДЕЛЕНИЕ ПОВЕДЕНИЯ
    float ThreatAssessment() //оценка угрозы
    {
        float threat_level = 0f;
        //первый уровень — оценка здоровья командира
        float threat_health_level = ((100 - OurEnemyCommanderHealth) / 100f) * 50f; //показатель угрозы по здоровью командира (диапазон от 0 до 50)
        //второй уровень — оценка количества вражеских сил
        int my_units_num = EnemyAliveUnits(); //количество юнитов у этого вражеского командира
        int enemy_units_num = PlayerAliveUnits(); //количество юнитов у нашего игрока 
        float threat_numforce_level = 0f; //показатель уровня угрозы по разнице сил (диапазон от -50 до 100)
        if (my_units_num > 0 && enemy_units_num > 0)
        {
            if (my_units_num > enemy_units_num)
            {
                threat_numforce_level = -25f; //низкая угроза 
            }
            else if (my_units_num < enemy_units_num)
            {
                threat_numforce_level = ((float)enemy_units_num / my_units_num) * 25f; //средняя-экстремальная угроза
            }
            else
            {
                threat_numforce_level = 15f; //средняя угроза
            }
        }
        else if (my_units_num > 0 && enemy_units_num == 0)
        {
            threat_numforce_level = -50f; //очень низкая угроза
        }
        else if (my_units_num == 0 && enemy_units_num > 0)
        {
            threat_numforce_level = 50f; //высокая угроза
        }
        //общий уровень угрозы
        threat_level = threat_health_level + threat_numforce_level;
        return Mathf.Clamp(threat_level, 0f, 100f); //определение уровня угрозы конкретно в заданном диапазоне
    }    
    void BehaviorAssessment() //определение поведения при уровне угрозы
    {
        float threat_level = ThreatAssessment();
        if (threat_level < 40f) //очень низкая, низкая, средняя угроза
        {
            Behavior = "Attack";
        }
        else if (threat_level >= 40f && threat_level <= 70f) //случайный выбор поведения при высокой угрозе
        {
            int random_behavior = Random.Range(0, 3);
            if (random_behavior == 0)
            {
                Behavior = "Attack";
            }
            else if (random_behavior == 1)
            {
                Behavior = "Retreat";
            }
            else
            {
                Behavior = "Defend Commander";
            }
        }
        else if (threat_level > 70f && threat_level < 100f) //случайный выбор поведения при очень высокой угрозе
        {
            int random_behavior = Random.Range(0, 2);
            if (random_behavior == 0)
            {
                Behavior = "Retreat";
            }
            else if (random_behavior == 1)
            {
                Behavior = "Defend Commander";
            }
        }
        else //экстремальная угроза
        {
            Behavior = "Retreat";
        }
    }    

    //СЦЕНАРИИ ПОВЕДЕНИЯ
    void AttackBehavior() //поведение при Атаке
    {
        squadcontrol enemysquad = ClosestEnemySquad();
        if (enemysquad == null)
        {
            return;
        }
        foreach (var squad in OurEnemySquads)
        {
            if (squad != null && squad.CountAliveUnits() > 0)
            {
                Vector3 attackdirection = (enemysquad.transform.position - squad.transform.position).normalized; //построение вектора траектории направления движения
                squad.transform.position += attackdirection * squad.speed * Time.deltaTime; //перемещение союзной группы
                if (enemysquad != null)
                {
                    squad.SetEnemySquad(enemysquad);
                }
            }
        }
    }
    void RetreatBehavior() //поведение при Отступлении
    {
        squadcontrol enemysquad = ClosestEnemySquad();
        if (enemysquad == null)
        {
            return;
        }
        float enemydistance = Vector3.Distance(transform.position, enemysquad.transform.position); //отступление только если слишком быстро к игроку
        if (enemydistance < 10f)
        {
            Vector3 retreatdirection = (transform.position - enemysquad.transform.position).normalized; //построение вектора от командира игрока к командиру врага
            Vector3 retreatposition = transform.position + retreatdirection * 3f; //задача точки отступления
            transform.position = Vector3.MoveTowards(transform.position, retreatposition, 1.5f * Time.deltaTime); //уход к точке отступления
        }
        else
        {
            DefendCommanderBehavior();  
        }
        foreach (var squad in OurEnemySquads)
        {
            if (squad != null)
            {
                squad.Gotopoint_global(transform.position);
            }
        }
    }
    void DefendCommanderBehavior() //поведение при Защите командира
    {
        for (int i = 0; i < OurEnemySquads.Length; i++)
        {
            if (OurEnemySquads[i] != null && OurEnemySquads[i].CountAliveUnits() > 0)
            {
                float angle = i * 120f; //угол для равномерного распределения отрядов
                Vector3 offset_from_commander = Quaternion.Euler(0, 0, angle) * Vector3.right * 1.5f;
                Vector3 defensecommanderposition = transform.position + offset_from_commander; //целевая позиция — позиция командира + смещение от него
                OurEnemySquads[i].Gotopoint_global(defensecommanderposition);
            }
        }
    }

    void CommanderMovement() //перемещение командира вместе с отрядами
    {
        Vector3 centerposition = Vector3.zero; 
        int count_alivesquads = 0;    
        foreach (var squad in OurEnemySquads)
        {
            if (squad != null && squad.CountAliveUnits() > 0)
            {
                centerposition += squad.transform.position; //суммирование позиций живых отрядов
                count_alivesquads++;
            }
        }
        if (count_alivesquads > 0)
        {
            centerposition /= count_alivesquads; //центр — среднее арифметическое от количества отрядов
            transform.position = Vector3.MoveTowards(transform.position, centerposition, 3f * Time.deltaTime); //перемещение за отрядами
        }
    }

    void ChoiceBehavior() //выбор поведения
    {
        switch (Behavior)
        {
            case "Attack":
                AttackBehavior();
                break;
            case "Retreat":
                RetreatBehavior();
                break;
            case "Defend Commander":
                DefendCommanderBehavior();
                break;  
        }
        CommanderMovement();
    }

    //УНИЧТОЖЕНИЕ КОМАНДИРА, ПОЛУЧЕНИЕ КОМАНДДИРОМ УРОНА И УНИЧТОЖЕНИЕ ОТРЯДОВ
    void CommanderDeath() //смерть командира
    {
        if (flagdead == true)
        {
            return;
        }
        flagdead = true;
        Debug.Log("Enemy Commander died!");
        StopAllCoroutines();
        squadcontrol[] allsquads = FindObjectsByType<squadcontrol>(FindObjectsSortMode.InstanceID);
        foreach (var squad in allsquads)
        {
            if (squad != null && squad.our_teamname == TeamName && squad.this_squadname.Contains(MyName))
            {
                Destroy(squad.gameObject);
            }
        }
        if (transform.parent != null)
        {
            Destroy(transform.parent.gameObject); //уничтожается родительский элемент
        }
        else
        {
            Destroy(gameObject); 
        }
    }
    public void HurtCommander(float damage) //вражеский командир получает урон
    {
        if (flagdead == true)
        {
            return;
        }
        OurEnemyCommanderHealth -= damage;
        Debug.Log($"Enemy Commander took {damage} damage. His current health: {OurEnemyCommanderHealth}");
        if (OurEnemyCommanderHealth <= 0)
        {
            OurEnemyCommanderHealth = 0;
            CommanderDeath();
        }
    }
    public void SquadDeath(squadcontrol destroyedsquad) //уничтожение всего отряда
    {
        for (int i = 0; i < OurEnemySquads.Length; i++)
        {
            if (OurEnemySquads[i] == destroyedsquad)
            {
                OurEnemySquads[i] = null;
                Debug.Log($"Enemy squad {i} destroyed");
                break;
            }
        }
    }
}
