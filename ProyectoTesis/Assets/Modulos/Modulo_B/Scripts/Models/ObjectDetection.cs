using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

public class ObjectDetection
{
    public string timestamp;
    public List<Objects> objects;
    public Performance performance;
}

public class Objects{ 
    public int object_id;
    [JsonProperty("class")]
    public string classes;
    public double confidence;
    public double[] bbox;
    public double speed;
    public double distance;
    //[JsonConverter(typeof(Direction))]
    public string direction;

}

public class Performance { 
    public double fps;
    public double processing_time;
    public double cpu_usage;
    public double memory_usage;
}

/*
public enum Direction { 
    North,
    South,
    East,
    West,
    Northeast,
    Northwest,
    Southeast,
    Southwest
}
*/