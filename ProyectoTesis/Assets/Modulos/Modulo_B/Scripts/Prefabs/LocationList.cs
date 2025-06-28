using System;
using System.Collections.Generic;
using UnityEngine;

public class LocationList : MonoBehaviour
{
    [Header("Referencias ScrollView")]
    public Transform contentParent;
    public GameObject itemPrefab;
    [Header("Datos")]
    public List<Location> locations = new List<Location>();
    private List<GameObject> items = new List<GameObject>();
    
    void Start()
    {
        AddDataLocations();
        GenerateDynamicList();
    }
    void AddDataLocations()
    {
        locations.Add(new Location("Facultad de sistemas", -78.4893317413561, -0.2102156881754021));
        locations.Add(new Location("Facultad de Quimica", 100, 587));
        locations.Add(new Location("Facultad de Petroleos", 36, 88));
        locations.Add(new Location("Teatro", -78.49033786644237, -0.2118316344069575));
    }
    void GenerateDynamicList()
    {
        CleanLastList();
        foreach (Location location in locations)
        {
            AddNewLocation(location);
        }
        EliminateItemPrefab();
    }

    public void CleanLastList()
    {
        foreach (GameObject item in items)
        {
            if (item != null)
            {
                DestroyImmediate(item);
            }
        }
        items.Clear();
    }
    public void EliminateItemPrefab()
    {
        foreach (Transform child in contentParent)
        {
            if (child.name == "itemLocation")
            {
                Destroy(child.gameObject);
                return;
            }
        }
    }
    public void AddNewLocation(Location newLocation)
    {
        GameObject newElement = Instantiate(itemPrefab, contentParent);
        ItemLocation newItemLocation = newElement.GetComponent<ItemLocation>();
        if (newItemLocation != null)
        {
            newItemLocation.SetElement(newLocation);
        }
        items.Add(newElement);
    }
}