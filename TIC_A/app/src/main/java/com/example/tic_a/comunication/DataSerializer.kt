package com.example.tic_a.comunication

import android.util.Log
import com.example.tic_a.models.DetectedObject
import com.example.tic_a.models.PerformanceMetrics
import org.json.JSONArray
import org.json.JSONObject
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

/**
 * Handles serialization of data for communication with Component B
 */
class DataSerializer {
    /**
     * Serializes a list of detected objects and performance metrics into a JSON string
     * @param detectedObjects List of detected objects
     * @param performanceMetrics Performance metrics
     * @return JSON string representation
     */
    fun serializeData(
        detectedObjects: List<DetectedObject>,
        performanceMetrics: PerformanceMetrics? = null
    ): String {
        val root = JSONObject()

        // Add timestamp
        val sdf = SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'", Locale.US)
        root.put("timestamp", sdf.format(Date()))

        // Add detected objects
        val objectsArray = JSONArray()
        for (obj in detectedObjects) {
            try {
                val objJson = JSONObject()
                objJson.put("object_id", obj.id)
                objJson.put("class", obj.classLabel)
                objJson.put("confidence", obj.confidence)

                // Bounding box as array [x_min, y_min, x_max, y_max]
                val bboxArray = JSONArray()
                bboxArray.put(obj.boundingBox.left)
                bboxArray.put(obj.boundingBox.top)
                bboxArray.put(obj.boundingBox.right)
                bboxArray.put(obj.boundingBox.bottom)
                objJson.put("bbox", bboxArray)

                // Additional information
                objJson.put("speed", obj.speed)
                objJson.put("distance", obj.distance)
                objJson.put("direction", obj.direction)

                objectsArray.put(objJson)
            } catch (e: Exception) {
                Log.e(TAG, "Error serializing object: ${e.message}")
            }
        }
        root.put("objects", objectsArray)

        // Add performance metrics if available
        performanceMetrics?.let {
            val metricsJson = JSONObject()
            metricsJson.put("fps", it.fps)
            metricsJson.put("processing_time", it.processingTimeMs)
            metricsJson.put("cpu_usage", it.cpuUsage)
            metricsJson.put("memory_usage", it.memoryUsage)

            root.put("performance", metricsJson)
        }

        return root.toString()
    }

    /**
     * Deserializes a JSON string into a list of detected objects
     * @param jsonString JSON string representation
     * @return Pair containing list of detected objects and performance metrics (if available)
     */
    fun deserializeData(jsonString: String): Pair<List<DetectedObject>, PerformanceMetrics?> {
        val detectedObjects = ArrayList<DetectedObject>()
        var performanceMetrics: PerformanceMetrics? = null

        try {
            val root = JSONObject(jsonString)

            // Parse detected objects
            val objectsArray = root.getJSONArray("objects")
            for (i in 0 until objectsArray.length()) {
                val objJson = objectsArray.getJSONObject(i)
                detectedObjects.add(DetectedObject.fromJson(objJson))
            }

            // Parse performance metrics if available
            if (root.has("performance")) {
                val metricsJson = root.getJSONObject("performance")
                performanceMetrics = PerformanceMetrics(
                    fps = metricsJson.optDouble("fps", 0.0).toFloat(),
                    processingTimeMs = metricsJson.optDouble("processing_time", 0.0).toFloat(),
                    cpuUsage = metricsJson.optDouble("cpu_usage", 0.0).toFloat(),
                    memoryUsage = metricsJson.optDouble("memory_usage", 0.0).toFloat()
                )
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error deserializing data: ${e.message}")
        }

        return Pair(detectedObjects, performanceMetrics)
    }

    companion object {
        private const val TAG = "DataSerializer"
    }
}