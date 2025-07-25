using System.Collections.Generic;
using Newtonsoft.Json;
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
    public string name;
    public float confidence;
    public float[] bbox;
    public float speed;
    public float distance;
    public string direction;

}

public class Performance { 
    public double fps;
    public double processing_time;
    public double cpu_usage;
    public double memory_usage;
}
