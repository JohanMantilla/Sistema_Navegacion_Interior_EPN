// ObjectAnalyzer.kt - VERSIÓN ACTUALIZADA PARA TESTS
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
 * UPDATED: Now works without SensorManager for testing purposes
 */
// ObjectAnalyzer corregido - Mejor tracking y detección de movimiento
class ObjectAnalyzer(
    private val screenSize: Size,
    private val sensorManager: SensorManager? = null
) : SensorEventListener {

    private val objectHistory = HashMap<String, ArrayList<Pair<DetectedObject, Long>>>() // Cambio: String en vez de Int
    private val maxHistorySize = 10
    private val pixelsPerMeter = 200f
    private var nextId = 0

    // Threshold para considerar movimiento real (en píxeles)
    private val movementThreshold = 10f // Píxeles mínimos para considerar movimiento
    private val minimumTimeDiff = 100L  // Milisegundos mínimos entre mediciones

    // Sensor data
    private var accelerometerReading = FloatArray(3)
    private var magnetometerReading = FloatArray(3)
    private var cameraHeight = 1.5f

    init {
        sensorManager?.let { sm ->
            sm.getDefaultSensor(Sensor.TYPE_ACCELEROMETER)?.also { accelerometer ->
                sm.registerListener(this, accelerometer, SensorManager.SENSOR_DELAY_NORMAL)
            }
            sm.getDefaultSensor(Sensor.TYPE_MAGNETIC_FIELD)?.also { magneticField ->
                sm.registerListener(this, magneticField, SensorManager.SENSOR_DELAY_NORMAL)
            }
            Log.d(TAG, "Sensor listeners registered")
        } ?: run {
            Log.d(TAG, "No SensorManager provided - running in test mode")
        }
    }

    fun analyzeObjects(detectedObjects: List<DetectedObject>, timestamp: Long): List<DetectedObject> {
        val analyzedObjects = ArrayList<DetectedObject>()

        // CORRECCIÓN: Asignar IDs consistentes basados en posición y clase
        val trackedObjects = assignConsistentIds(detectedObjects)

        for (detection in trackedObjects) {
            val trackingId = generateTrackingId(detection)
            val history = objectHistory.getOrPut(trackingId) { ArrayList() }

            val centerX = (detection.boundingBox.left + detection.boundingBox.right) / 2
            val centerY = (detection.boundingBox.top + detection.boundingBox.bottom) / 2

            var speed = 0f
            var direction = 0f

            // CORRECCIÓN: Solo calcular velocidad si hay suficiente historia y tiempo
            if (history.isNotEmpty()) {
                val prevDetection = history.last().first
                val prevTimestamp = history.last().second
                val timeDiff = timestamp - prevTimestamp

                if (timeDiff >= minimumTimeDiff) { // Tiempo mínimo entre mediciones
                    val prevCenterX = (prevDetection.boundingBox.left + prevDetection.boundingBox.right) / 2
                    val prevCenterY = (prevDetection.boundingBox.top + prevDetection.boundingBox.bottom) / 2

                    val deltaX = centerX - prevCenterX
                    val deltaY = centerY - prevCenterY
                    val distance = sqrt(deltaX.pow(2) + deltaY.pow(2))

                    // CORRECCIÓN: Solo considerar movimiento si supera el threshold
                    if (distance > movementThreshold) {
                        speed = distance / (timeDiff / 1000f)
                        direction = if (distance > 0) {
                            val angle = atan2(deltaY, deltaX) * 180 / Math.PI.toFloat()
                            if (angle < 0) angle + 360 else angle
                        } else {
                            0f
                        }

                        Log.v(TAG, "Object $trackingId: Real movement detected - speed=$speed px/s, direction=$direction°")
                    } else {
                        // Movimiento muy pequeño, probablemente ruido
                        speed = 0f
                        direction = 0f
                        Log.v(TAG, "Object $trackingId: Movement below threshold ($distance px), treating as stationary")
                    }
                } else {
                    Log.v(TAG, "Object $trackingId: Time difference too small ($timeDiff ms), skipping speed calculation")
                }
            }

            val distance = calculateDistance(detection)

            // CORRECCIÓN: Usar ID consistente
            val analyzedObject = DetectedObject(
                id = detection.id, // Mantener el ID asignado
                classLabel = detection.classLabel,
                confidence = detection.confidence,
                boundingBox = detection.boundingBox,
                speed = speed,
                distance = distance,
                direction = direction
            )

            analyzedObjects.add(analyzedObject)

            // Actualizar historia
            history.add(Pair(analyzedObject, timestamp))

            // Limitar tamaño de historia
            if (history.size > maxHistorySize) {
                history.removeAt(0)
            }
        }

        cleanupHistory(timestamp)
        return analyzedObjects
    }

    /**
     * NUEVO: Asigna IDs consistentes basados en posición y clase de objeto
     */
    private fun assignConsistentIds(detectedObjects: List<DetectedObject>): List<DetectedObject> {
        val trackedObjects = mutableListOf<DetectedObject>()
        val usedIds = mutableSetOf<Int>()

        for (detection in detectedObjects) {
            val trackingId = generateTrackingId(detection)

            // Buscar si ya existe un objeto similar en el historial
            var assignedId = -1
            val history = objectHistory[trackingId]

            if (history != null && history.isNotEmpty()) {
                // Usar el ID del último objeto en el historial
                assignedId = history.last().first.id
            } else {
                // Buscar objetos similares por posición
                assignedId = findSimilarObjectId(detection, usedIds)
            }

            if (assignedId == -1) {
                // Crear nuevo ID
                assignedId = nextId++
            }

            usedIds.add(assignedId)

            trackedObjects.add(
                DetectedObject(
                    id = assignedId,
                    classLabel = detection.classLabel,
                    confidence = detection.confidence,
                    boundingBox = detection.boundingBox,
                    speed = detection.speed,
                    distance = detection.distance,
                    direction = detection.direction
                )
            )
        }

        return trackedObjects
    }

    /**
     * NUEVO: Busca objetos similares por posición para mantener consistencia de ID
     */
    private fun findSimilarObjectId(detection: DetectedObject, usedIds: Set<Int>): Int {
        val centerX = (detection.boundingBox.left + detection.boundingBox.right) / 2
        val centerY = (detection.boundingBox.top + detection.boundingBox.bottom) / 2

        var closestId = -1
        var minDistance = Float.MAX_VALUE
        val maxSearchDistance = 100f // Píxeles máximos para considerar el mismo objeto

        for ((trackingId, history) in objectHistory) {
            if (history.isEmpty()) continue

            val lastObject = history.last().first

            // Solo considerar objetos de la misma clase
            if (lastObject.classLabel != detection.classLabel) continue

            // No reusar IDs ya asignados en este frame
            if (usedIds.contains(lastObject.id)) continue

            val lastCenterX = (lastObject.boundingBox.left + lastObject.boundingBox.right) / 2
            val lastCenterY = (lastObject.boundingBox.top + lastObject.boundingBox.bottom) / 2

            val distance = sqrt((centerX - lastCenterX).pow(2) + (centerY - lastCenterY).pow(2))

            if (distance < maxSearchDistance && distance < minDistance) {
                minDistance = distance
                closestId = lastObject.id
            }
        }

        return closestId
    }

    /**
     * NUEVO: Genera un ID de tracking basado en clase y posición aproximada
     */
    private fun generateTrackingId(detection: DetectedObject): String {
        val centerX = ((detection.boundingBox.left + detection.boundingBox.right) / 2).toInt()
        val centerY = ((detection.boundingBox.top + detection.boundingBox.bottom) / 2).toInt()

        // Crear regiones de tracking para agrupar posiciones similares
        val regionX = centerX / 50 // Regiones de 50 píxeles
        val regionY = centerY / 50

        return "${detection.classLabel}_${regionX}_${regionY}"
    }

    private fun calculateDistance(detection: DetectedObject): Float {
        val objectWidth = detection.boundingBox.width()
        val objectHeight = detection.boundingBox.height()

        val realWorldSize = when (detection.classLabel.lowercase()) {
            "person" -> 1.7f
            "car" -> 4.5f
            "bicycle" -> 1.7f
            "motorcycle" -> 2.0f
            "bus" -> 12.0f
            "truck" -> 8.0f
            "dog" -> 0.6f
            "cat" -> 0.3f
            "chair" -> 0.8f
            "bottle" -> 0.25f
            "cell phone" -> 0.15f
            "laptop" -> 0.3f
            else -> 1.0f
        }

        val focalLength = 500f
        val apparentSize = maxOf(objectWidth, objectHeight)

        return if (apparentSize > 0) {
            val calculatedDistance = (realWorldSize * focalLength) / apparentSize
            calculatedDistance.coerceIn(0.5f, 100f)
        } else {
            10f
        }
    }

    private fun cleanupHistory(currentTimestamp: Long) {
        val maxAge = 3000L // 3 segundos
        val idsToRemove = ArrayList<String>()

        for ((id, history) in objectHistory) {
            if (history.isEmpty()) {
                idsToRemove.add(id)
                continue
            }

            val lastSeen = history.last().second
            if (currentTimestamp - lastSeen > maxAge) {
                idsToRemove.add(id)
                Log.v(TAG, "Removing object $id from history (last seen ${currentTimestamp - lastSeen}ms ago)")
            }
        }

        for (id in idsToRemove) {
            objectHistory.remove(id)
        }

        if (idsToRemove.isNotEmpty()) {
            Log.d(TAG, "Cleaned up ${idsToRemove.size} old objects from history")
        }
    }

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

    fun getTrackingStats(): Map<String, Any> {
        return mapOf(
            "tracked_objects" to objectHistory.size,
            "total_history_entries" to objectHistory.values.sumOf { it.size },
            "avg_history_per_object" to if (objectHistory.isNotEmpty()) {
                objectHistory.values.sumOf { it.size }.toFloat() / objectHistory.size
            } else 0f,
            "next_id" to nextId
        )
    }

    // ... resto de métodos del sensor sin cambios ...

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
        sensorManager?.let {
            val rotationMatrix = FloatArray(9)
            val orientationAngles = FloatArray(3)

            val success = SensorManager.getRotationMatrix(
                rotationMatrix, null, accelerometerReading, magnetometerReading
            )

            if (success) {
                SensorManager.getOrientation(rotationMatrix, orientationAngles)
                val pitch = orientationAngles[1] * 180 / Math.PI.toFloat()
                val roll = orientationAngles[2] * 180 / Math.PI.toFloat()

                Log.v(TAG, "Device orientation: pitch=$pitch°, roll=$roll°")

                cameraHeight = when {
                    pitch > 30 -> 0.8f
                    pitch < -30 -> 2.2f
                    else -> 1.5f
                }
            }
        }
    }

    override fun onAccuracyChanged(sensor: Sensor?, accuracy: Int) {
        sensor?.let {
            Log.v(TAG, "Sensor ${sensor.name} accuracy changed to $accuracy")
        }
    }

    fun release() {
        sensorManager?.let { sm ->
            sm.unregisterListener(this)
            Log.d(TAG, "Sensor listeners unregistered")
        }
        objectHistory.clear()
        Log.d(TAG, "Object history cleared")
    }

    companion object {
        private const val TAG = "ObjectAnalyzer"
    }
}