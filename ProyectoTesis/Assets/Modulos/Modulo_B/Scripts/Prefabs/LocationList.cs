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
        locations.Add(new Location("Casa", -78.48162966183533f, -0.23037450929773165f));
        locations.Add(new Location("Teatro Politécnico", -78.49038031315031f, -0.21158103708927942f));
        locations.Add(new Location("Museo Gustavo Orcés V", -78.49044838031831f, -0.21173661812905353f));
        locations.Add(new Location("Administración Central", -78.49068184826031f, -0.21151514592211637f));
        locations.Add(new Location("Casa Patrimonial EPN", -78.491234f, -0.212160f));
        locations.Add(new Location("Centro de Investigación de la Vivienda", -78.48162966183533f, -0.23037450929773165f));
        locations.Add(new Location("Facultad de Ingeniería Civil y Ambiental", -78.49133111538454f, -0.211720692588358f));
        locations.Add(new Location("Departamento de Ciencias Nucleares", -78.49157416355429f, -0.21122915677581156f));
        locations.Add(new Location("Centro de Investigaciones Aplicada a Polímeros", -78.491335f, -0.211795f));
        locations.Add(new Location("Laboratorio de Aguas y Microbiología", -78.491579f, -0.211263f));
        locations.Add(new Location("Ingeniería Hidraúlica", -78.49122346270165f, -0.21142727863267707f));
        locations.Add(new Location("Centro de Investigaciones y Control Ambiental", -78.49123940364892f, -0.21143411042083912f));
        locations.Add(new Location("Facultad de Ciencias", -78.49005977350862f, -0.21101737132862744f));
        locations.Add(new Location("Facultad de Ingeniería en Geología y Petróleos", -78.48962253608481f, -0.21056419602613768f));
        locations.Add(new Location("Departamento de Formación Básica", -78.49021007385343f, -0.20992656240436738f));
        locations.Add(new Location("Facultad de Ingeniería Mécanica", -78.48995573596501f, -0.20939537476657666f));
        locations.Add(new Location("Facultad de Ingeniería Électrica y Electrónica", -78.490445f, -0.211454f));
        locations.Add(new Location("Edificio de Eléctrica", -78.48931863233106f, -0.20936413108526744f));
        locations.Add(new Location("Facultad de Química", -78.4892344096735f, -0.20952442474301522f));
        locations.Add(new Location("Departamento de Ciencias de Alimentos y Biotecnología", -78.48932479562299f, -0.20962708080661044f));
        locations.Add(new Location("Facultad de Ingeniería de Sistemas", -78.48920828407155f, -0.21043200929091213f));
        locations.Add(new Location("Comedor", -78.48917274480277f, -0.21020670525450297f));
        locations.Add(new Location("Formación de Tecnólogos", -78.4886218560655f, -0.210058738903881f));
        locations.Add(new Location("Departamento de Metalurgia Extractiva", -78.48769970793002f, -0.2093262113671645f));
        locations.Add(new Location("Plaza EARME", -78.48708386208358f, -0.20966492431797437f));
        locations.Add(new Location("Procesos de Producción Mecánica", -78.48809539342808f, -0.20986992392312392f));
        locations.Add(new Location("Laboratorio Institucional de Análisis de Vehículos", -78.48682316388208f, -0.2088432531266756f));
        locations.Add(new Location("Facultad de Ciencias Administrativas", -78.48723432717138f, -0.20919728561268328f));
        locations.Add(new Location("Centro de Investigaciones y Estudios en Recursos Hidricos", -78.48734421918894f, -0.21006185184454992f));
        locations.Add(new Location("Sede Ladrón de Guevara", -78.4928101279111f, -0.21166714643360351f));
        locations.Add(new Location("Servicios Generales y Talleres", -78.49218784642292f, -0.21106780492174604f));
        locations.Add(new Location("Estadio Politécnico", -78.48924652228342f, -0.2117200827349107f));
        locations.Add(new Location("Canchas Deportivas", -78.4897946772925f, -0.21099070380135415f));
        locations.Add(new Location("Cancha de Mecánica", -78.48962557338808f, -0.20993491864942335f));
        locations.Add(new Location("Ágora Tecnólogos", -78.4886465670879f, -0.21060546638489772f));
        locations.Add(new Location("Centro de Acopio de Residuos Sólidos", -78.48835957073779f, -0.21127333190721145f));
        locations.Add(new Location("Coro", -78.48736447120308f, -0.20976057624042155f));
        locations.Add(new Location("Centro de Educación Continua (CEC)", -78.48685011275883f, -0.20925463090336474f));
        locations.Add(new Location("Ex Junior Collage Comisión de Evaluación Interna", -78.48647575444069f, -0.20877602409241622f));
        locations.Add(new Location("Gimnasio", -78.48983977838449f, -0.2118791277044733f));
        locations.Add(new Location("Aulas del Centro de Cultura Física", -78.48932908576373f, -0.21129871348157236f));
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