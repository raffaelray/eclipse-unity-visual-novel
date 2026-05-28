using UnityEngine;
using System.Collections.Generic;

public class StatsDatabase : Singleton<StatsDatabase>
{
    public List<StatDefinition> stats;                      // Список всех статов
    public List<RelationDefinition> relations;              // Список всех отношений
    public List<FlagDefinition> flags;                      // Список всех флагов
}