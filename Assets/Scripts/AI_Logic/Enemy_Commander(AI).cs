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
    [SerializeField] private float DecisionsIntervalTime = 3f; //������ ����������������� �������� ������� ����� ���������� ������� (���������, ���������) � ��������
    [Header("Enemy squads settings")]
    [SerializeField] private squadcontrol[] OurEnemySquads = new squadcontrol[3]; //������ ������� (������� ���, ������������ � �������)
    [SerializeField] public GameObject unitprefab;
    [Header("Other settings")]
    private string Behavior = "Attack"; //������ ��������� 
    private float LogicTime = 0f; //������ ������� ���������� �������
    private commander commander;
    private bool flagdead = false;

    void Start()    
    {
        commander = gameObject.AddComponent<commander>(); //���������� � ����������� ���� ��������� 
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
        //����������� ���������
        if (Time.time >= LogicTime)
        {
            BehaviorAssessment();
            LogicTime = Time.time + DecisionsIntervalTime; //������ �������� ���������� ������� �������������
        }
        ChoiceBehavior(); //������ ���� ���������� ���������
        //�������� ������ ���������
        if (OurEnemyCommanderHealth <= 0)
        {
            CommanderDeath();
            return;
        }
    }

    //����ר� ������
    int EnemyAliveUnits() //������� �������� ������� ������
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
    int PlayerAliveUnits() //������� �������� ������ ������
    {
        squadcontrol[] AllSquads = FindObjectsByType<squadcontrol>(FindObjectsSortMode.InstanceID); //����� ������� ������
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


    squadcontrol ClosestEnemySquad() //���������� ���������� ���������� ������ 
    {
        squadcontrol[] AllSquads = FindObjectsByType<squadcontrol>(FindObjectsSortMode.InstanceID);
        squadcontrol closest_enemysquad = null;
        float mindistance = Mathf.Infinity; //�������, ���� ��� ������� ��������� �� ������� ������ �������� ������������� �������������
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
    Transform ClosestEnemyCommander() //���������� ���������� ��������� �����
    {
        Transform closestcommander = null;
        float mindistance = Mathf.Infinity; //�������, ���� ��� ������� ��������� �� ������� ������ �������� ������������� �������������
        Commander_Rules playercommander = FindAnyObjectByType<Commander_Rules>();
        if (playercommander != null && playercommander.TeamName == "Blue")
        {
            float distance = Vector3.Distance(transform.position, playercommander.transform.position);
            if (distance < mindistance)
            {
                mindistance = distance;
                closestcommander = playercommander.transform;
            }
        }
        AllyCommander[] allycommanders = FindObjectsByType<AllyCommander>(FindObjectsSortMode.InstanceID);
        foreach (var ally in allycommanders)
        {
            if (ally.TeamName == "BLue")
            {
                float distance = Vector3.Distance(transform.position, ally.transform.position);
                if (distance < mindistance)
                {
                    mindistance = distance;
                    closestcommander = ally.transform;
                }
            }
        }
        return closestcommander;
    }

    //������ ������ � ����������� ���������
    float ThreatAssessment() //������ ������
    {
        float threat_level = 0f;
        //������ ������� � ������ �������� ���������
        float threat_health_level = ((100 - OurEnemyCommanderHealth) / 100f) * 50f; //���������� ������ �� �������� ��������� (�������� �� 0 �� 50)
        //������ ������� � ������ ���������� ��������� ���
        int my_units_num = EnemyAliveUnits(); //���������� ������ � ����� ���������� ���������
        int enemy_units_num = PlayerAliveUnits(); //���������� ������ � ������ ������ 
        float threat_numforce_level = 0f; //���������� ������ ������ �� ������� ��� (�������� �� -50 �� 100)
        if (my_units_num > 0 && enemy_units_num > 0)
        {
            if (my_units_num > enemy_units_num)
            {
                threat_numforce_level = -25f; //������ ������ 
            }
            else if (my_units_num < enemy_units_num)
            {
                threat_numforce_level = ((float)enemy_units_num / my_units_num) * 25f; //�������-������������� ������
            }
            else
            {
                threat_numforce_level = 15f; //������� ������
            }
        }
        else if (my_units_num > 0 && enemy_units_num == 0)
        {
            threat_numforce_level = -50f; //����� ������ ������
        }
        else if (my_units_num == 0 && enemy_units_num > 0)
        {
            threat_numforce_level = 50f; //������� ������
        }
        //����� ������� ������
        threat_level = threat_health_level + threat_numforce_level;
        return Mathf.Clamp(threat_level, 0f, 100f); //����������� ������ ������ ��������� � �������� ���������
    }    
    void BehaviorAssessment() //����������� ��������� ��� ������ ������
    {
        float threat_level = ThreatAssessment();
        if (threat_level < 40f) //����� ������, ������, ������� ������
        {
            Behavior = "Attack";
        }
        else if (threat_level >= 40f && threat_level <= 70f) //��������� ����� ��������� ��� ������� ������
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
        else if (threat_level > 70f && threat_level < 100f) //��������� ����� ��������� ��� ����� ������� ������
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
        else //������������� ������
        {
            Behavior = "Retreat";
        }
    }    

    //�������� ���������
    void AttackBehavior() //��������� ��� �����
    {
        squadcontrol allysquad = ClosestEnemySquad();
        Transform allycommander = ClosestEnemyCommander();
        if (allycommander != null)
        {
            foreach (var squad in OurEnemySquads)
            {
                if (squad != null && squad.CountAliveUnits() > 0)
                {
                    Vector3 direction = (allycommander.position - squad.transform.position).normalized;
                    squad.transform.position += direction * squad.speed * Time.deltaTime;
                }
            }
        }
        else if (allysquad != null)
        {
            foreach (var squad in OurEnemySquads)
            {
                if (squad != null && squad.CountAliveUnits() > 0)
                {
                    Vector3 direction = (allysquad.transform.position - squad.transform.position).normalized;
                    squad.transform.position += direction * squad.speed * Time.deltaTime;
                    squad.SetEnemySquad(allysquad);
                }
            }
        }
    }
    void RetreatBehavior() //��������� ��� �����������
    {
        squadcontrol enemysquad = ClosestEnemySquad();
        if (enemysquad == null)
        {
            return;
        }
        float enemydistance = Vector3.Distance(transform.position, enemysquad.transform.position); //����������� ������ ���� ������� ������ � ������
        if (enemydistance < 10f)
        {
            Vector3 retreatdirection = (transform.position - enemysquad.transform.position).normalized; //���������� ������� �� ��������� ������ � ��������� �����
            Vector3 retreatposition = transform.position + retreatdirection * 3f; //������ ����� �����������
            transform.position = Vector3.MoveTowards(transform.position, retreatposition, 1.5f * Time.deltaTime); //���� � ����� �����������
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
    void DefendCommanderBehavior() //��������� ��� ������ ���������
    {
        for (int i = 0; i < OurEnemySquads.Length; i++)
        {
            if (OurEnemySquads[i] != null && OurEnemySquads[i].CountAliveUnits() > 0)
            {
                float angle = i * 120f; //���� ��� ������������ ������������� �������
                Vector3 offset_from_commander = Quaternion.Euler(0, 0, angle) * Vector3.right * 1.5f;
                Vector3 defensecommanderposition = transform.position + offset_from_commander; //������� ������� � ������� ��������� + �������� �� ����
                OurEnemySquads[i].Gotopoint_global(defensecommanderposition);
            }
        }
    }

    void CommanderMovement() //����������� ��������� ������ � ��������
    {
        Vector3 centerposition = Vector3.zero; 
        int count_alivesquads = 0;    
        foreach (var squad in OurEnemySquads)
        {
            if (squad != null && squad.CountAliveUnits() > 0)
            {
                centerposition += squad.transform.position; //������������ ������� ����� �������
                count_alivesquads++;
            }
        }
        if (count_alivesquads > 0)
        {
            centerposition /= count_alivesquads; //����� � ������� �������������� �� ���������� �������
            transform.position = Vector3.MoveTowards(transform.position, centerposition, 3f * Time.deltaTime); //����������� �� ��������
        }
    }

    void ChoiceBehavior() //����� ���������
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

    //����������� ���������, ��������� ����������� ����� � ����������� �������
    void CommanderDeath() //������ ���������
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
            Destroy(transform.parent.gameObject); //������������ ������������ �������
        }
        else
        {
            Destroy(gameObject); 
        }
    }
    public void HurtCommander(float damage) //��������� �������� �������� ����
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
    public void SquadDeath(squadcontrol destroyedsquad) //����������� ����� ������
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
    public squadcontrol[] GetOurEnemySquads()
    {
        return OurEnemySquads;
    }
}
