package com.example.tic_a.models

import android.graphics.RectF
import com.google.gson.annotations.SerializedName
import org.json.JSONArray
import org.json.JSONObject

/**
 * Model class for representing a detected object with all its properties
 */
data class DetectedObject(
    @SerializedName("object_id")
    val id: Int,

    @SerializedName("class")
    val classLabel: String,

    @SerializedName("confidence")
    val confidence: Float,

    @SerializedName("bbox")
    val boundingBox: RectF,

    @SerializedName("speed")
    val speed: Float = 0f,  // in pixels per second

    @SerializedName("distance")
    val distance: Float = 0f,  // in meters

    @SerializedName("direction")
    val direction: Float = 0f  // in degrees, 0 is right, clockwise
) {
    /**
     * Converts the object to a JSON representation for communication with Component B
     */
    fun toJson(): JSONObject {
        val json = JSONObject()
        json.put("object_id", id)
        json.put("class", classLabel)
        json.put("confidence", confidence)

        // Convert bounding box to array [x_min, y_min, x_max, y_max]
        val bbox = JSONArray()
        bbox.put(boundingBox.left)
        bbox.put(boundingBox.top)
        bbox.put(boundingBox.right)
        bbox.put(boundingBox.bottom)
        json.put("bbox", bbox)

        json.put("speed", speed)
        json.put("distance", distance)
        json.put("direction", direction)

        return json
    }

    companion object {
        /**
         * Creates a DetectedObject from a JSON object
         */
        fun fromJson(json: JSONObject): DetectedObject {
            val id = json.getInt("object_id")
            val classLabel = json.getString("class")
            val confidence = json.getDouble("confidence").toFloat()

            val bboxArray = json.getJSONArray("bbox")
            val boundingBox = RectF(
                bboxArray.getDouble(0).toFloat(), // left
                bboxArray.getDouble(1).toFloat(), // top
                bboxArray.getDouble(2).toFloat(), // right
                bboxArray.getDouble(3).toFloat()  // bottom
            )

            val speed = if (json.has("speed")) json.getDouble("speed").toFloat() else 0f
            val distance = if (json.has("distance")) json.getDouble("distance").toFloat() else 0f
            val direction = if (json.has("direction")) json.getDouble("direction").toFloat() else 0f

            return DetectedObject(
                id = id,
                classLabel = classLabel,
                confidence = confidence,
                boundingBox = boundingBox,
                speed = speed,
                distance = distance,
                direction = direction
            )
        }
    }
}