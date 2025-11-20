using UnityEngine;
using Unity.Mathematics;
using System;

public interface IFormatSet
{
    Vector3[] GenFormatSet(int unitcount, Vector3 center, float spacing);
}
public class SkirmishGen: IFormatSet
{
    public Vector3[] GenFormatSet(int unitcount, Vector3 center, float spacing)
    {
        Vector3[] positions = new Vector3[unitcount];
        System.Random rand = new System.Random();
        for (int i = 0; i < unitcount; i++) {
            float x = center.x+(float)(rand.NextDouble()*spacing);
            float y = center.y+(float)(rand.NextDouble()*spacing);

            positions[i] = new Vector3(x, y, center.z);
        }
        return positions;
    }
}

public class Unit_formation
{
    private IFormatSet _format;
    public Unit_formation(IFormatSet format) { _format = format; }
    public void Set_Unit_formation(IFormatSet format) { _format = format; }
    public Vector3[] Genarate_formation(int unitcount, Vector3 center, float spacing) { return _format.GenFormatSet(unitcount, center, spacing); }
}