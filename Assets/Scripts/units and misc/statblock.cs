using UnityEngine;
using System.Collections.Generic;

public class statblock
{
    public string unit_type = "melee";

    public float atackrange = 1.5f;
    public float walkrange = 3f; // walkspeed xd

    public float damage = 2f;
    public float atackdelay = 1f;
    public float health = 10f;

    public float speed = 0.5f;

    private int squadId;
    /*
    public statblock(int squadId)
    {
        this.squadId = squadId;
    }
    */
}

public enum modifierType // я сам в шоке что так по умному пишу, не спрашивайте
{
    add, multiply, set
}

public class squadModifier //я все это затеял просто потому что захотел указывать источник баффа (оно того не стоило)
{
    public modifierType operation;
    public float value;
    public string source; // not worth it 

    public squadModifier(modifierType a, float b, string c)
    {
    operation = a;
    value = b;
    source = c;
    } 
}

/*
public class squadManager : MonoBehaviour
{
    public static squadManager instantce;
    private Dictionary<int, list<squadModifier>> Modifiers;

    void Awake() //@! ну тут сам смотри как по мне страшаня хрень, а еще её вроде перед start() надо поставить
    {
        if (instantce == null)
        {
            instantce = this; //Я НЕ ЗНАЮ КАК ЭТО РАБОТАЕТ И ПОЧЕМУ И ИЗЗА ЭТОГО МНЕ СТРАШНО
            Modifiers = new Dictionary<int, list<squadModifier>>();
        }
        else{Destroy(gameObject);}
    }
    
    public void findSquad(int squadId)
    {
        if (!modifier.ContainsKey(squadId))
        {
            Modifier[squadId] = new list<squadModifier>();
        }
    }
    void StatCheck() //@! У тебя уже есть один Update, может обьединить или это не облегчит код?
    {
        foreach (list<squadModifier> modifierList in modifier.Value) 
    }

}

public list<squadModifier> getModifiers(int squadId)
{
    return squadModifier[squadId];
}
*/