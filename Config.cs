using AYellowpaper.SerializedCollections;
using UnityEngine;
using static AYellowpaper.SerializedCollections.SerializedDictionarySample;


[CreateAssetMenu(fileName = "New CommonConfig", menuName = "CommonConfig")]
public class CommonConfig : ScriptableObject
{
    [SerializedDictionary("Key", "Variant")]
    public SerializedDictionary<string, Variant> Datas;
}


[CreateAssetMenu(fileName = "New AbilityConfig", menuName = "AbilityConfig")]
public class AbilityConfig : ScriptableObject
{
    [SerializedDictionary("Element Type", "Description")]
    public SerializedDictionary<string, GameObject> abilityData;
}