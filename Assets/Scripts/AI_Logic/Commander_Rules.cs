using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.InputSystem;


public class Commander_Rules : MonoBehaviour
{
    [Header("General ruling stats")]
    [SerializeField] public string TeamName = "Blue"; //название команды
    [SerializeField] public string MyName = "Player";
    [SerializeField] private float SelectionRadius; //радиус попадания объектов в область вокруг курсора
    [SerializeField] private squadcontrol SelectedSquad; //выбранная группа
    [Header("Commander stats")]
    [SerializeField] private float CommanderHealth = 20f; //здоровье командира
    [SerializeField] private float CommanderSpeed = 5f; //скорость командира
    [SerializeField] private float CommanderSquadDistance = 3f; //дистанция между командиром и отрядом при движении
    [SerializeField] private float SafeBattleDistanceSmallerThan3 = 2.5f; //безопасное расстояние между командиром и полем боя
    [Header("Player squads settings")]
    [SerializeField] private squadcontrol[] PlayerSquads = new squadcontrol[3]; //массив отрядов (ближний бой, дальнобойные и конницы)
    [SerializeField] public GameObject unitprefab;
    private Coroutine AttackCoroutine; //ссылка на текущую корутину атаки на вражеский сквад
    private Camera scene_camera;
    private Mouse player_mouse; //создание объекта для курсора мыши игрока
    private commander commander;
    private bool flagdead = false;

    void Start()
    {
        scene_camera = Camera.main;
        player_mouse = Mouse.current;
        commander = gameObject.AddComponent<commander>();
        commander.Init(TeamName, MyName, new GameObject[3], unitprefab);
        for (int i = 0; i < 3; i++)
        {
            PlayerSquads[i] = commander.squads[i].GetComponent<squadcontrol>();
        }   
    }

    void Update()
    {
        if (CommanderHealth <= 0 && flagdead == false)
        {
            CommanderDeath();
            return;
        }
        if (player_mouse == null)
        {
            return;
        }

        PlayerSquadSelection();
        EnemySquadSelection();
    }

    Vector3 GetMouseWorldCoordinate() //приведение координат курсора мыши к мировым игровым координатам
    {
        if (scene_camera == null)
        {
            scene_camera = Camera.main;
        }
        if (player_mouse == null)
        {
            return Vector3.zero;
        }
        Vector2 mouse_position = player_mouse.position.ReadValue(); //чтение текущей позиции мыши
        Vector3 world_position = scene_camera.ScreenToWorldPoint(new Vector3(mouse_position.x, mouse_position.y, -scene_camera.transform.position.z)); //корректировка экранных координат X и Y в мировые игровые, а также глубины Z 
        return world_position;
    }

    //ВЫБОР СОЮЗНОГО СКВАДА
    void WithCursorSelectSquad() //поиск выбранного мышью отряда
    {
        Vector3 game_mouse_position = GetMouseWorldCoordinate();
        Collider2D[] possible_squad = Physics2D.OverlapCircleAll(game_mouse_position, SelectionRadius); //выделение всех возможных сквадов в радиусе курсора мыши
        SelectedSquad = null;
        foreach (Collider2D squads in possible_squad) //цикл по всем выделенным сквадам
        {
            squadcontrol squad = squads.GetComponentInParent<squadcontrol>();
            if (squad != null && squad.our_teamname == TeamName)
            {
                SelectedSquad = squad;
                Debug.Log($"SelectedSquad: {squad.this_squadname}"); //вывод сообщения в консоль
                break;
            }
        }
    }
    void PlayerSquadSelection() //обработка выбора союзной группы мышкой
    {
        if (player_mouse.leftButton.wasPressedThisFrame) //если была нажата левая кнопка мыши
        {
            WithCursorSelectSquad();
        }
    }

    //ДВИЖЕНИЕ К ПРОТИВНИКУ
    IEnumerator AttackMovement(squadcontrol our_squad, squadcontrol target_enemy_squad) //логика движения союзного сквада к цели 
    {
        while (our_squad != null && target_enemy_squad != null)
        {
            float squad_distance = Vector3.Distance(target_enemy_squad.transform.position, our_squad.transform.position);
            float commander_distance = Vector3.Distance(target_enemy_squad.transform.position, our_squad.transform.position);
            if (squad_distance > our_squad.walkrange) //проверка на то, нужно ли вообще двигаться
            {
                Vector3 direction = (target_enemy_squad.transform.position - our_squad.transform.position).normalized; //построение вектора траектории направления движения
                our_squad.transform.position += direction * our_squad.speed * Time.deltaTime; //перемещение союзной группы
                Vector3 commander_position = our_squad.transform.position - direction * CommanderSquadDistance; //перемещение командира за своей группой
                transform.position = Vector3.MoveTowards(transform.position, commander_position, CommanderSpeed * Time.deltaTime);
            }
            else if (commander_distance > SafeBattleDistanceSmallerThan3) //если командир слишком далеко от отряда 
            {
                Vector3 safe_position = our_squad.transform.position;
                Vector3 direction_to_squad = (our_squad.transform.position - transform.position).normalized;
                safe_position -= direction_to_squad * SafeBattleDistanceSmallerThan3;
                transform.position = Vector3.MoveTowards(transform.position, safe_position, CommanderSpeed * Time.deltaTime);
            }
            else
            {
                Debug.Log($"Squad {our_squad.this_squadname} has reached {target_enemy_squad.this_squadname}. Commander is holding safe position");
            }
            yield return null; //обновляет позицию сквада каждый кадр
        }
    }
    void AttackCommand(squadcontrol our_squad, squadcontrol target_enemy_squad) //инициализация атаки на вражеский сквад
    {
        Debug.Log($"Squad {our_squad.this_squadname} attacks {target_enemy_squad.this_squadname}");
        our_squad.SetEnemySquad(target_enemy_squad); //установка вражеского сквада в качестве цели
        if (AttackCoroutine != null) //если уже есть корутина атаки на вражеский сквад, то нужно её остановить
        {
            StopCoroutine(AttackCoroutine);
        }
        AttackCoroutine = StartCoroutine(AttackMovement(our_squad, target_enemy_squad));
    }

    //ВЫБОР ВРАЖЕСКОГО СКВАДА
    void WithCursorSelectEnemySquad() //поиск выбранного мышью вражеского отряда
    {
        Vector3 game_mouse_position = GetMouseWorldCoordinate();
        Collider2D[] possible_enemy_squad = Physics2D.OverlapCircleAll(game_mouse_position, SelectionRadius); //выделение всех возможных сквадов в радиусе курсора мыши
        bool enemyfound = false; //флаг на поиск врага
        foreach (Collider2D enemy_squads in possible_enemy_squad) //цикл по всем выделенным сквадам
        {
            squadcontrol enemy_squad = enemy_squads.GetComponentInParent<squadcontrol>();
            if (enemy_squad != null && enemy_squad.our_teamname != TeamName)
            {
                if (SelectedSquad != null)
                {
                    AttackCommand(SelectedSquad, enemy_squad); //определение этого вражеского сквада — противниклм
                    enemyfound = true; //враг найден
                    break;
                }
            }
        }
        if (enemyfound == false) //если враг так и не был найден
        {
            Debug.Log("Enemy hasn't been found");
        }
    }
    void EnemySquadSelection() //обработка выбора вражеской группы мышкой
    {
        if (player_mouse.rightButton.wasPressedThisFrame && SelectedSquad != null) //если была нажата правая кнопка мыши и есть вражеская группа
        {
            WithCursorSelectEnemySquad();
        }
    }

    void CommanderMovement() //перемещение командира вместе с отрядами
    {
        Vector3 centerposition = Vector3.zero;
        int count_alivesquads = 0;
        foreach (var squad in PlayerSquads)
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

    //УНИЧТОЖЕНИЕ КОМАНДИРА, ПОЛУЧЕНИЕ КОМАНДДИРОМ УРОНА И УНИЧТОЖЕНИЕ ОТРЯДОВ
    void CommanderDeath() //смерть командира
    {
        if (flagdead == true)
        {
            return;
        }
        flagdead = true;
        Debug.Log("Player Commander died!");
        if (AttackCoroutine != null)
        {
            StopAllCoroutines();
            AttackCoroutine = null; //лучше на всякий остановить и конкретную корутину атаки
        }
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
    public void HurtCommander(float damage) //командир игрока получает урон
    {
        if (flagdead == true)
        {
            return;
        }
        CommanderHealth -= damage;
        Debug.Log($"Player Commander took {damage} damage. His current health: {CommanderHealth}");
        if (CommanderHealth <= 0)
        {
            CommanderHealth = 0;
            CommanderDeath();
        }
    }
    public void SquadDeath(squadcontrol destroyedsquad) //уничтожение всего отряда
    {
        for (int i = 0; i < PlayerSquads.Length; i++)
        {
            if (PlayerSquads[i] == destroyedsquad)
            {
                PlayerSquads[i] = null;
                Debug.Log($"Player squad {i} destroyed");
                break;
            }
        }
    }
}

