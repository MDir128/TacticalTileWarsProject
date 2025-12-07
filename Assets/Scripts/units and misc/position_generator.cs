using UnityEngine;
using Unity.Mathematics;
using System;

public interface IFormatSet
{
    Vector3[] GenFormatSet(int unitcount, Vector3 center, float spacing, Vector3 direction);
    protected Vector3[] RotatePos(Vector3[] positions, Vector3 direction)
    {
        Vector3[] newpos = new Vector3[positions.Length];
        
        return newpos;
    }
}
public class SkirmishGen: IFormatSet
{
    public Vector3[] GenFormatSet(int unitcount, Vector3 center, float spacing, Vector3 direction)
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
public class SemiRoundGen : IFormatSet {
    public Vector3[] GenFormatSet(int unitcount, Vector3 center, float spacing, Vector3 direction)
    {
        Vector3[] positions = new Vector3[unitcount];
        float radius = spacing * unitcount * 0.2f / math.PI;
        float angle_fix = Vector3.Angle(Vector3.up, direction) * (float)Math.PI/180f;
        for (int i = 0; i < unitcount; i++)
        {
            float angle;
            if (i < unitcount / 2-1)
            {
                angle = 2 * i * (float)(Math.PI / unitcount) + angle_fix;
            }
            else
            {
                angle = 2 * (i- unitcount/2-1) * (float)(Math.PI / unitcount) + angle_fix;
                radius = spacing * unitcount * 0.3f / math.PI;
            }
            float x = center.x + math.cos(angle) * radius;
            float y = center.y + math.sin(angle) * radius;
            positions[i] = /*RotatePosition(*/new Vector3(x, y, center.z)/*, forward, right)*/;
        }
        return positions;
    }
}
public class RoundGen : IFormatSet
{
    public Vector3[] GenFormatSet(int unitcount, Vector3 center, float spacing, Vector3 direction)
    {
        Vector3[] positions = new Vector3[unitcount];
        float radius = spacing * unitcount * 0.2f / math.PI;
        float angleStep = MathF.PI / (unitcount);

        int firstRowCount = (unitcount + 1) / 2;
        int secondRowCount = unitcount / 2;

        float innerRadius = spacing * firstRowCount * 0.5f / MathF.PI * 0.2f;
        float outerRadius = innerRadius + spacing * 1.5f;
        for (int i = 0; i < unitcount; i++) {
            float angle = 2 * i * (float)(Math.PI / unitcount);
            float x = center.x + math.cos(angle) * radius;
            float y = center.y + math.sin(angle) * radius;
            positions[i] = new Vector3(x,y,center.z);
        }
        return positions;
    }
}

public class Unit_formation
{
    private IFormatSet _format;
    public Unit_formation(IFormatSet format) { _format = format; }
    public void Set_Unit_formation(IFormatSet format) { _format = format; }
    public Vector3[] Genarate_formation(int unitcount, Vector3 center, float spacing, Vector3 direction) { return _format.GenFormatSet(unitcount, center, spacing, direction); }
}