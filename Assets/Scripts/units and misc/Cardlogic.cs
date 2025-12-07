using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class cardlogic : MonoBehaviour
{
    [SerializeField] private float cardDrawInterval = 30f;
    private float timer;
    private manager gameManager;
    
    // Хранение ожидающих выборов
    private Dictionary<int, PendingChoice> pendingChoices = new Dictionary<int, PendingChoice>();
    
    // Активные эффекты
    private Dictionary<int, CardEffect> activeEffects = new Dictionary<int, CardEffect>();
    
    [System.Serializable]
    public struct CardEffect
    {
        public CardType type;
        public string displayName;
        public float multiplier;
        public int squadId;
        public string squadName;
        public DateTime appliedTime;
    }
    
    public enum CardType
    {
        Damage,
        AttackSpeed,
        Health,
        Speed,
        AttackRange,
        WalkRange
    }
    
    private class PendingChoice
    {
        public int squadId;
        public CardEffect[] options = new CardEffect[3];
        public float creationTime;
        public bool choiceMade = false;
        public Coroutine timeoutCoroutine;
    }
    
    void Start()
    {
        Debug.Log("cardlogic: Система карт инициализирована.");
        timer = cardDrawInterval;
        
        gameManager = FindObjectOfType<manager>();
        if (gameManager == null)
        {
            Debug.LogError("cardlogic: Менеджер не найден!");
            return;
        }
        
        StartCoroutine(InitialCardDistribution());
    }
    
    IEnumerator InitialCardDistribution()
    {
        Debug.Log("cardlogic: Ожидание создания всех отрядов...");
        yield return new WaitForSeconds(3f);
        
        CreateCardsForAllSquads();
        Debug.Log("cardlogic: Первоначальная раздача карточек завершена.");
    }
    
    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            Debug.Log("cardlogic: Таймер истек - раздача новых карточек.");
            CreateCardsForAllSquads();
            timer = cardDrawInterval;
        }
    }
    
    void CreateCardsForAllSquads()
    {
        Debug.Log("cardlogic: === СОЗДАНИЕ КАРТОЧЕК ДЛЯ ВСЕХ ОТРЯДОВ ===");
        
        var commanders = gameManager.GetCommandersForCardSystem();
        int totalSquadsProcessed = 0;
        
        ProcessCommanderArray(commanders.blueCommanders, ref totalSquadsProcessed);
        ProcessCommanderArray(commanders.redCommanders, ref totalSquadsProcessed);
        
        Debug.Log($"cardlogic: Обработано отрядов: {totalSquadsProcessed}");
        Debug.Log("cardlogic: === ЗАВЕРШЕНО СОЗДАНИЕ КАРТОЧЕК ===");
    }
    
    void ProcessCommanderArray(GameObject[] commanders, ref int counter)
    {
        if (commanders == null) return;
        
        foreach (var cmdObj in commanders)
        {
            if (cmdObj == null) continue;
            
            var cmd = cmdObj.GetComponentInChildren<commander>();
            if (cmd == null || cmd.squads == null) continue;
            
            foreach (var squadObj in cmd.squads)
            {
                if (squadObj == null) continue;
                
                var squad = squadObj.GetComponent<squadcontrol>();
                if (squad == null) continue;
                
                // Пропускаем отряды, которые уже ожидают выбора
                if (pendingChoices.ContainsKey(squad.squadId))
                {
                    Debug.Log($"cardlogic: Отряд {squad.squadId} уже ожидает выбора, пропускаем.");
                    continue;
                }
                
                // Создаем 3 карточки для этого отряда
                CreateThreeCardsForSquad(squad);
                counter++;
            }
        }
    }
    
    void CreateThreeCardsForSquad(squadcontrol squad)
    {
        if (squad == null) return;
        
        var cmd = squad.GetComponentInParent<commander>();
        string commanderName = cmd?.my_name ?? "Неизвестен";
        
        Debug.Log($"cardlogic: Создание 3 карточек для отряда '{squad.this_squadname}' (ID:{squad.squadId}), командир: {commanderName}");
        
        // Создаем 3 уникальные карточки
        CardEffect[] cards = new CardEffect[3];
        List<CardType> usedTypes = new List<CardType>();
        
        for (int i = 0; i < 3; i++)
        {
            CardEffect card;
            do
            {
                card = GenerateRandomCard(squad);
            } while (usedTypes.Contains(card.type));
            
            usedTypes.Add(card.type);
            cards[i] = card;
            
            Debug.Log($"cardlogic:   Карточка {i+1}: {card.displayName} (x{card.multiplier:F2})");
        }
        
        // Создаем запись ожидания выбора
        var pendingChoice = new PendingChoice
        {
            squadId = squad.squadId,
            options = cards,
            creationTime = Time.time
        };
        
        // Запускаем таймер ожидания (5 секунд)
        pendingChoice.timeoutCoroutine = StartCoroutine(WaitForChoiceTimeout(pendingChoice));
        
        pendingChoices[squad.squadId] = pendingChoice;
        
        Debug.Log($"cardlogic: Для отряда {squad.squadId} запущен таймер ожидания выбора (5 секунд)");
        
        // ВЫЗОВ ВАШЕЙ UI СИСТЕМЫ ДЛЯ ОТОБРАЖЕНИЯ ВЫБОРА
        // UISystem.Instance.ShowCardChoice(squad.squadId, cards);
    }
    
    IEnumerator WaitForChoiceTimeout(PendingChoice choice)
    {
        yield return new WaitForSeconds(5f);
        
        if (!choice.choiceMade && pendingChoices.ContainsKey(choice.squadId))
        {
            Debug.Log($"cardlogic: Таймаут! Для отряда {choice.squadId} автоматически выбрана первая карточка.");
            
            // Автоматически выбираем первую карточку (индекс 0)
            ApplyCardToSquad(choice.squadId, 0);
        }
    }
    
    // ПУБЛИЧНЫЙ МЕТОД ДЛЯ ВНЕШНЕГО ВЫЗОВА
    public void SelectCardForSquad(int squadId, int cardIndex)
    {
        Debug.Log($"cardlogic: Получен выбор карточки для отряда {squadId}: индекс {cardIndex}");
        
        if (!pendingChoices.ContainsKey(squadId))
        {
            Debug.LogWarning($"cardlogic: Для отряда {squadId} нет ожидающих выборов!");
            return;
        }
        
        var choice = pendingChoices[squadId];
        
        if (choice.choiceMade)
        {
            Debug.LogWarning($"cardlogic: Для отряда {squadId} выбор уже был сделан!");
            return;
        }
        
        if (cardIndex < 0 || cardIndex >= 3)
        {
            Debug.LogError($"cardlogic: Неверный индекс карточки {cardIndex}. Должен быть 0, 1 или 2.");
            cardIndex = 0;
        }
        
        // Останавливаем таймер ожидания
        if (choice.timeoutCoroutine != null)
        {
            StopCoroutine(choice.timeoutCoroutine);
        }
        
        choice.choiceMade = true;
        
        // Применяем выбранную карточку
        ApplyCardToSquad(squadId, cardIndex);
    }
    
    void ApplyCardToSquad(int squadId, int cardIndex)
    {
        if (!pendingChoices.ContainsKey(squadId))
        {
            Debug.LogError($"cardlogic: Не могу применить карточку - отряд {squadId} не найден!");
            return;
        }
        
        var choice = pendingChoices[squadId];
        var card = choice.options[cardIndex];
        
        // Находим отряд на сцене
        squadcontrol squad = FindSquadById(squadId);
        if (squad == null)
        {
            Debug.LogError($"cardlogic: Не могу найти отряд с ID {squadId} на сцене!");
            pendingChoices.Remove(squadId);
            return;
        }
        
        // Применяем карточку через правильные методы
        ApplyCardEffectToUnits(squad, card);
        
        pendingChoices.Remove(squadId);
        
        Debug.Log($"cardlogic: Карточка '{card.displayName}' применена к отряду '{squad.this_squadname}'");
    }
    
    void ApplyCardEffectToUnits(squadcontrol squad, CardEffect card)
    {
        // Сохраняем активный эффект
        card.appliedTime = DateTime.Now;
        activeEffects[squad.squadId] = card;
        
        Debug.Log($"cardlogic: Применение '{card.displayName}' (x{card.multiplier:F2}) к отряду '{squad.this_squadname}'");
        
        // Применяем эффект ко ВСЕМ юнитам отряда напрямую
        if (squad.units != null)
        {
            foreach (var unitObj in squad.units)
            {
                if (unitObj == null) continue;
                
                // Ищем uniticontrol в дочерних объектах (как в тестах)
                var unitCtrl = unitObj.GetComponentInChildren<uniticontrol>(true);
                if (unitCtrl == null || unitCtrl.statblock == null) continue;
                
                // Применяем множитель напрямую к statblock юнита
                ApplyMultiplierToUnitStat(unitCtrl, card);
                
                Debug.Log($"cardlogic:   Юнит '{unitObj.name}' - " +
                         $"Здоровье: {unitCtrl.statblock.health:F2}, " +
                         $"Урон: {unitCtrl.statblock.damage:F2}");
            }
        }
        
        // Обновляем общее здоровье отряда
        squad.UpdateOverallHealth();
        
        Debug.Log($"cardlogic:   Общее здоровье отряда: {squad.overallhealth:F2}");
    }
    
    void ApplyMultiplierToUnitStat(uniticontrol unitCtrl, CardEffect card)
    {
        // Применяем множитель напрямую к statblock юнита
        switch (card.type)
        {
            case CardType.Damage:
                unitCtrl.statblock.damage *= card.multiplier;
                break;
                
            case CardType.AttackSpeed:
                unitCtrl.statblock.attackdelay *= card.multiplier;
                break;
                
            case CardType.Health:
                unitCtrl.statblock.health *= card.multiplier;
                break;
                
            case CardType.Speed:
                unitCtrl.statblock.speed *= card.multiplier;
                break;
                
            case CardType.AttackRange:
                unitCtrl.statblock.attackrange *= card.multiplier;
                break;
                
            case CardType.WalkRange:
                unitCtrl.statblock.walkrange *= card.multiplier;
                break;
        }
    }
    
    CardEffect GenerateRandomCard(squadcontrol squad)
    {
        // Балансированные настройки карточек
        var cardDefinitions = new (CardType type, string name, float min, float max)[]
        {
            (CardType.Damage, "Усиление урона", 1.1f, 1.25f),
            (CardType.AttackSpeed, "Ускорение атаки", 0.7f, 0.85f),
            (CardType.Health, "Укрепление здоровья", 1.15f, 1.35f),
            (CardType.Speed, "Повышение скорости", 1.05f, 1.2f),
            (CardType.AttackRange, "Увеличение дальности", 1.1f, 1.3f),
            (CardType.WalkRange, "Расширение зоны ходьбы", 1.05f, 1.25f)
        };
        
        int randomIndex = UnityEngine.Random.Range(0, cardDefinitions.Length);
        var def = cardDefinitions[randomIndex];
        
        float multiplier = UnityEngine.Random.Range(def.min, def.max);
        
        return new CardEffect
        {
            type = def.type,
            displayName = def.name,
            multiplier = multiplier,
            squadId = squad.squadId,
            squadName = squad.this_squadname
        };
    }
    
    squadcontrol FindSquadById(int squadId)
    {
        var allSquads = FindObjectsOfType<squadcontrol>();
        foreach (var squad in allSquads)
        {
            if (squad.squadId == squadId)
                return squad;
        }
        return null;
    }
    
    // Метод для принудительной раздачи
    public void ForceCardDistribution()
    {
        Debug.Log("cardlogic: Принудительная раздача карточек!");
        CreateCardsForAllSquads();
    }
    
    // Метод для получения информации об отряде
    public string GetSquadCardInfo(int squadId)
    {
        if (activeEffects.TryGetValue(squadId, out var effect))
        {
            return $"{effect.displayName} (x{effect.multiplier:F2})";
        }
        
        if (pendingChoices.TryGetValue(squadId, out var choice))
        {
            return $"Ожидает выбора ({Time.time - choice.creationTime:F1} сек)";
        }
        
        return "Нет активных карточек";
    }
    
    // Метод для получения вариантов выбора (для UI)
    public CardEffect[] GetPendingChoices(int squadId)
    {
        if (pendingChoices.TryGetValue(squadId, out var choice))
        {
            return choice.options;
        }
        return null;
    }
    
    // Метод для проверки, ожидает ли отряд выбора
    public bool IsSquadWaitingForChoice(int squadId)
    {
        return pendingChoices.ContainsKey(squadId);
    }
    
    // Метод для получения всех активных эффектов
    public Dictionary<int, CardEffect> GetAllActiveEffects()
    {
        return new Dictionary<int, CardEffect>(activeEffects);
    }
}