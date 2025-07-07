package com.example.tic_a.detection

import android.hardware.Sensor
import android.hardware.SensorEvent
import android.hardware.SensorEventListener
import android.hardware.SensorManager
import android.util.Log
import android.util.Size
import com.example.tic_a.models.DetectedObject
import kotlin.math.atan2
import kotlin.math.pow
import kotlin.math.sqrt

/**
 * Analyzes detected objects to calculate additional information like speed, distance and direction
 */
class ObjectAnalyzer(
    private val screenSize: Size,
    private val sensorManager: SensorManager? = null
) : SensorEventListener {
    private val objectHistory = HashMap<Int, ArrayList<Pair<DetectedObject, Long>>>()
    private val maxHistorySize = 10
    private val pixelsPerMeter = 200f // Approximate conversion factor

    // Sensor data for distance estimation
    private var accelerometerReading = FloatArray(3)
    private var magnetometerReading = FloatArray(3)
    private var cameraHeight = 1.5f // Default camera height in meters

    init {
        // Register sensors for better distance estimation
        sensorManager?.let {
            it.getDefaultSensor(Sensor.TYPE_ACCELEROMETER)?.also { accelerometer ->
                sensorManager.registerListener(
                    this,
                    accelerometer,
                    SensorManager.SENSOR_DELAY_NORMAL
                )
            }
            it.getDefaultSensor(Sensor.TYPE_MAGNETIC_FIELD)?.also { magneticField ->
                sensorManager.registerListener(
                    this,
                    magneticField,
                    SensorManager.SENSOR_DELAY_NORMAL
                )
            }
        }
    }

    /**
     * Updates the object history and calculates additional information for detected objects
     * @param detectedObjects List of detected objects
     * @param timestamp Current timestamp in milliseconds
     * @return List of objects with calculated speed, distance and direction
     */
    fun analyzeObjects(detectedObjects: List<DetectedObject>, timestamp: Long): List<DetectedObject> {
        val analyzedObjects = ArrayList<DetectedObject>()

        // Process each detected object
        for (detection in detectedObjects) {
            val objectId = detection.id

            // Get or create history for this object
            val history = objectHistory.getOrPut(objectId) { ArrayList() }

            // Calculate center point for easier tracking
            val centerX = (detection.boundingBox.left + detection.boundingBox.right) / 2
            val centerY = (detection.boundingBox.top + detection.boundingBox.bottom) / 2

            // Calculate speed if we have history
            var speed = 0f
            var direction = 0f

            if (history.isNotEmpty()) {
                val prevDetection = history.last().first
                val prevTimestamp = history.last().second
                val timeDiff = timestamp - prevTimestamp

                if (timeDiff > 0) {
                    // Previous center point
                    val prevCenterX = (prevDetection.boundingBox.left + prevDetection.boundingBox.right) / 2
                    val prevCenterY = (prevDetection.boundingBox.top + prevDetection.boundingBox.bottom) / 2

                    // Calculate distance moved in pixels
                    val deltaX = centerX - prevCenterX
                    val deltaY = centerY - prevCenterY
                    val distance = sqrt(deltaX.pow(2) + deltaY.pow(2))

                    // Calculate speed in pixels per second
                    speed = distance / (timeDiff / 1000f)

                    // Calculate direction in degrees (0 is right, 90 is down)
                    direction = if (distance > 0) {
                        val angle = atan2(deltaY, deltaX) * 180 / Math.PI.toFloat()
                        // Convert to 0-360 range
                        if (angle < 0) angle + 360 else angle
                    } else {
                        0f
                    }
                }
            }

            // Calculate distance from camera (approximate)
            val distance = calculateDistance(detection)

            // Create new object with additional info
            val analyzedObject = DetectedObject(
                id = detection.id,
                classLabel = detection.classLabel,
                confidence = detection.confidence,
                boundingBox = detection.boundingBox,
                speed = speed,
                distance = distance,
                direction = direction
            )

            // Add to result list
            analyzedObjects.add(analyzedObject)

            // Update history
            history.add(Pair(analyzedObject, timestamp))

            // Limit history size
            if (history.size > maxHistorySize) {
                history.removeAt(0)
            }
        }

        // Clean up objects that haven't been seen recently
        cleanupHistory(timestamp)

        return analyzedObjects
    }

    /**
     * Calculates approximate distance from camera to object based on object size
     * @param detection Detected object
     * @return Estimated distance in meters
     */
    private fun calculateDistance(detection: DetectedObject): Float {
        // Basic distance estimation based on object size
        // The smaller the object appears, the further it is
        val objectWidth = detection.boundingBox.width()
        val objectHeight = detection.boundingBox.height()

        // Approximate real-world sizes of common objects in meters
        val realWorldSize = when (detection.classLabel) {
            "person" -> 1.7f  // Average human height
            "car" -> 4.5f     // Average car length
            "bicycle" -> 1.7f // Average bicycle length
            "dog" -> 0.6f     // Average dog height
            "chair" -> 0.8f   // Average chair height
            else -> 1.0f      // Default for unknown objects
        }

        // Calculate distance using simple pinhole camera model
        // distance = (real world size * focal length) / apparent size in pixels
        val focalLength = 500f // Approximate focal length in pixels
        val apparentSize = maxOf(objectWidth, objectHeight)

        return if (apparentSize > 0) {
            (realWorldSize * focalLength) / apparentSize
        } else {
            10f // Default value if calculation fails
        }
    }

    /**
     * Removes objects from history that haven't been seen recently
     * @param currentTimestamp Current timestamp in milliseconds
     */
    private fun cleanupHistory(currentTimestamp: Long) {
        val idsToRemove = ArrayList<Int>()

        for ((id, history) in objectHistory) {
            if (history.isEmpty()) {
                idsToRemove.add(id)
                continue
            }

            val lastSeen = history.last().second
            if (currentTimestamp - lastSeen > 3000) { // Remove after 3 seconds
                idsToRemove.add(id)
            }
        }

        for (id in idsToRemove) {
            objectHistory.remove(id)
        }
    }

    /**
     * Converts a cardinal direction (in degrees) to a human-readable string
     * @param direction Direction in degrees (0-360)
     * @return Human-readable direction (N, NE, E, etc.)
     */
    fun directionToString(direction: Float): String {
        return when {
            direction < 22.5 -> "East"
            direction < 67.5 -> "Southeast"
            direction < 112.5 -> "South"
            direction < 157.5 -> "Southwest"
            direction < 202.5 -> "West"
            direction < 247.5 -> "Northwest"
            direction < 292.5 -> "North"
            direction < 337.5 -> "Northeast"
            else -> "East"
        }
    }

    override fun onSensorChanged(event: SensorEvent?) {
        if (event == null) return

        when (event.sensor.type) {
            Sensor.TYPE_ACCELEROMETER -> {
                System.arraycopy(event.values, 0, accelerometerReading, 0, accelerometerReading.size)
                updateCameraOrientation()
            }
            Sensor.TYPE_MAGNETIC_FIELD -> {
                System.arraycopy(event.values, 0, magnetometerReading, 0, magnetometerReading.size)
                updateCameraOrientation()
            }
        }
    }

    private fun updateCameraOrientation() {
        val rotationMatrix = FloatArray(9)
        val orientationAngles = FloatArray(3)

        // Update rotation matrix, which is needed to update the orientation angles
        SensorManager.getRotationMatrix(
            rotationMatrix,
            null,
            accelerometerReading,
            magnetometerReading
        )

        // Get orientation angles from the rotation matrix
        SensorManager.getOrientation(rotationMatrix, orientationAngles)

        // Use orientation to improve distance estimation
        // For simplicity, we're just logging it here
        Log.d(TAG, "Device orientation: pitch=${orientationAngles[1]}, roll=${orientationAngles[2]}")
    }

    override fun onAccuracyChanged(sensor: Sensor?, accuracy: Int) {
        // Not needed for this implementation
    }

    fun release() {
        sensorManager?.unregisterListener(this)
    }

    companion object {
        private const val TAG = "ObjectAnalyzer"
    }
}