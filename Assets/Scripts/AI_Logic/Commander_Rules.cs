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
    [SerializeField] private float SelectionRadius = 0.25f; //радиус попадания объектов в область вокруг курсора
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
    private Dictionary<squadcontrol, Coroutine> AttackCoroutines = new Dictionary<squadcontrol, Coroutine>(); //словарь для корутин атаки

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
        WithWASDPlayerMovement();
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
                if (squad.this_squadname.StartsWith(MyName))
                {
                    SelectedSquad = squad;
                    Debug.Log($"SelectedSquad: {squad.this_squadname}"); //вывод сообщения в консоль
                    break;
                }
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

    //ПЕРЕМЕЩЕНИЕ КОМАНДИРА СО СКВАДАМИ
    void WithWASDPlayerMovement() //перемещение командира с отрядами через клавиатуру
    {
        Vector3 movement_direction = Vector3.zero; //установка позиции по умолчанию
        if (Keyboard.current.wKey.isPressed)
        {
            movement_direction.y += 1;
        }
        if (Keyboard.current.aKey.isPressed)
        {
            movement_direction.x -= 1;
        }
        if (Keyboard.current.sKey.isPressed)
        {
            movement_direction.y -= 1;
        }
        if (Keyboard.current.dKey.isPressed)
        {
            movement_direction.x += 1;
        }
        if (movement_direction != Vector3.zero)
        {
            movement_direction.Normalize(); //нормализация вектора для статичного движения
            transform.position += movement_direction * CommanderSpeed * Time.deltaTime; //двжиение командира
            PlayerSquadsMovement(); //движение отрядов за командиром
        }
    }
    void PlayerSquadsMovement() //следование отрядов за командиром
    {
        foreach (var squad in PlayerSquads)
        {
            if (squad != null && squad.CountAliveUnits() > 0)
            {
                squad.SetEnemySquad(null); //сначала нужно сбросить цель на атаку, если она есть
                squad.PlayerSquadFollow(transform.position); //движение отрядов относительно позиции командира
            }
        }
    }

    //ДВИЖЕНИЕ К СКВАДУ ПРОТИВНИКА
    IEnumerator AttackSquadMovement(squadcontrol our_squad, squadcontrol target_enemy_squad) //логика движения союзного сквада к цели-скваду
    {
        while (our_squad != null && target_enemy_squad != null)
        {
            float squad_distance = Vector3.Distance(target_enemy_squad.transform.position, our_squad.transform.position);
            float commander_toenemy_distance = Vector3.Distance(target_enemy_squad.transform.position, transform.position);
            float commander_tosquad_distance = Vector3.Distance(our_squad.transform.position, transform.position);
            if (squad_distance > our_squad.attackrange * 0.8f) //проверка на то, нужно ли вообще двигаться
            {
                Vector3 direction = (target_enemy_squad.transform.position - our_squad.transform.position).normalized; //построение вектора траектории направления движения
                our_squad.transform.position += direction * our_squad.speed * Time.deltaTime; //перемещение союзной группы
                Vector3 commander_position = our_squad.transform.position - direction * CommanderSquadDistance; //перемещение командира за своей группой
                transform.position = Vector3.MoveTowards(transform.position, commander_position, CommanderSpeed * Time.deltaTime);
            }
            else if (commander_toenemy_distance < SafeBattleDistanceSmallerThan3) //если командир слишком далеко от отряда 
            {
                Vector3 retreat_direction = (our_squad.transform.position - transform.position).normalized;
                Vector3 safe_position = transform.position + retreat_direction * SafeBattleDistanceSmallerThan3;
                transform.position = Vector3.MoveTowards(transform.position, safe_position, CommanderSpeed * Time.deltaTime);
            }
            else if (commander_toenemy_distance > SafeBattleDistanceSmallerThan3) //если командир слишком далеко от отряда 
            {
                Vector3 direction_tosquad = (our_squad.transform.position - transform.position).normalized;
                transform.position = Vector3.MoveTowards(transform.position, our_squad.transform.position - direction_tosquad * CommanderSquadDistance, CommanderSpeed * Time.deltaTime);
            }
            yield return null; //обновляет позицию сквада каждый кадр
        }
    }
    void AttackSquadCommand(squadcontrol our_squad, squadcontrol target_enemy_squad) //инициализация атаки на вражеский сквад
    {
        Debug.Log($"Squad {our_squad.this_squadname} attacks {target_enemy_squad.this_squadname}");
        our_squad.SetEnemySquad(target_enemy_squad); //установка вражеского сквада в качестве цели
        if (AttackCoroutines.ContainsKey(our_squad))
        {
            if (AttackCoroutines[our_squad] != null)
            {
                StopCoroutine(AttackCoroutine); //если уже есть эта корутина атаки, то нужно остановить
            }
            AttackCoroutines.Remove(our_squad);
        }
        Coroutine newcoroutine = StartCoroutine(AttackSquadMovement(our_squad, target_enemy_squad)); //добавление новой корутины
        AttackCoroutines[our_squad] = newcoroutine;
    }

    //ДВИЖЕНИЕ К КОМАНДИРУ ПРОТИВНИКА
    IEnumerator AttackCommanderMovement(squadcontrol our_squad, EnemyCommander target_enemy_commander) //логика движения союзного сквада к цели-командиру
    {
        while (our_squad != null && target_enemy_commander != null)
        {
            Vector3 direction = (target_enemy_commander.transform.position - our_squad.transform.position).normalized;
            our_squad.transform.position += direction * our_squad.speed * Time.deltaTime;
            yield return null;
        }
    }
    void AttackCommanderCommand(squadcontrol our_squad, EnemyCommander target_enemy_commander) //инициализация атаки на вражеского командира
    {
        Debug.Log($"Squad {our_squad.this_squadname} attacks {target_enemy_commander.MyName}");
        our_squad.SetEnemyCommander(target_enemy_commander); //установка вражеского командира в качестве цели
        if (AttackCoroutines.ContainsKey(our_squad)) 
        {
            if (AttackCoroutines[our_squad] != null)
            {
                StopCoroutine(AttackCoroutine); //если уже есть эта корутина атаки, то нужно остановить
            }
            AttackCoroutines.Remove(our_squad);
        }
        Coroutine newcoroutine = StartCoroutine(AttackCommanderMovement(our_squad, target_enemy_commander)); //добавление новой корутины
        AttackCoroutines[our_squad] = newcoroutine;
    }

    //ВЫБОР ВРАЖЕСКОЙ ЦЕЛИ
    void WithCursorSelectEnemyTarget() //поиск выбранного мышью вражеской цели
    {
        Vector3 game_mouse_position = GetMouseWorldCoordinate();
        Collider2D[] possible_enemy_targets = Physics2D.OverlapCircleAll(game_mouse_position, SelectionRadius); //выделение всех возможных целей в радиусе курсора мыши
        bool enemyfound = false; //флаг на поиск врага
        foreach (Collider2D target in possible_enemy_targets) //цикл по всем выделенным сквадам
        {
            //выбор вражеского сквада
            squadcontrol enemy_squad = target.GetComponentInParent<squadcontrol>();
            if (enemy_squad != null && enemy_squad.our_teamname != TeamName)
            {
                if (SelectedSquad != null)
                {
                    AttackSquadCommand(SelectedSquad, enemy_squad); //определение этого вражеского сквада — противником
                    enemyfound = true; //враг найден
                    break;
                }
            }
            //выбор вражеского командира
            EnemyCommander enemy_commander = target.GetComponent<EnemyCommander>();
            if (enemy_commander != null && enemy_commander.TeamName != TeamName)
            {
                if (SelectedSquad != null)
                {
                    AttackCommanderCommand(SelectedSquad, enemy_commander); //определение этого вражеского командира — противником
                    enemyfound = true;
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
            WithCursorSelectEnemyTarget();
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
        foreach (var coroutine in AttackCoroutines.Values)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        AttackCoroutines.Clear();

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

