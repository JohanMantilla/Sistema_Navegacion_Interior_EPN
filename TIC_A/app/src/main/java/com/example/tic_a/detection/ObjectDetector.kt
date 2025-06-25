package com.example.tic_a.detection

import android.content.Context
import android.graphics.Bitmap
import android.hardware.SensorManager
import android.util.Log
import android.util.Size
import com.example.tic_a.comunication.DataSerializer
import com.example.tic_a.comunication.SocketCommunication
import com.example.tic_a.models.DetectedObject
import com.example.tic_a.models.PerformanceMetrics
import com.example.tic_a.utils.PerformanceMonitor
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import kotlinx.coroutines.isActive
import kotlinx.coroutines.CancellationException
import org.json.JSONObject
import java.util.concurrent.atomic.AtomicBoolean

/**
 * Main class that coordinates object detection, analysis, and communication with Component B
 */
class ObjectDetector(
    private val context: Context,
    private val screenSize: Size,
    private val config: JSONObject = JSONObject()
) {
    // Subcomponents
    private val yoloDetector = YoloDetector(
        context = context,
        modelPath = config.optString("model_path", "yolov8n.tflite"),
        confThreshold = config.optDouble("confidence_threshold", 0.25).toFloat(), // Reducido
        iouThreshold = config.optDouble("iou_threshold", 0.45).toFloat()
    )

    private val objectAnalyzer = ObjectAnalyzer(
        screenSize = screenSize,
        sensorManager = context.getSystemService(Context.SENSOR_SERVICE) as SensorManager
    )

    private val dataSerializer = DataSerializer()

    private val socket = SocketCommunication(
        serverAddress = config.optString("server_address", "127.0.0.1"),
        serverPort = config.optInt("server_port", 8080),
        messageCallback = { handleServerMessage(it) }
    )

    private val performanceMonitor = PerformanceMonitor(context)

    // Status
    private val isRunning = AtomicBoolean(false)
    private val isProcessing = AtomicBoolean(false)
    private var lastProcessedFrame: Bitmap? = null
    private var lastDetectedObjects = listOf<DetectedObject>()

    // Callbacks
    private var onDetectionResultListener: ((List<DetectedObject>, Bitmap) -> Unit)? = null
    private var onPerformanceUpdateListener: ((PerformanceMetrics) -> Unit)? = null

    // Coroutine scope for async operations
    private val scope = CoroutineScope(Dispatchers.Default + Job())
    private var detectionJob: Job? = null

    /**
     * Starts object detection
     */
    fun start() {
        if (isRunning.get()) return

        isRunning.set(true)

        // Start communication with Component B if enabled
        if (config.optBoolean("enable_communication", true)) {
            socket.start()
        }

        Log.d(TAG, "Object detector started")
    }

    /**
     * Stops object detection
     */
    fun stop() {
        if (!isRunning.get()) return

        isRunning.set(false)

        // Cancel current detection job
        detectionJob?.cancel()
        detectionJob = null

        // Close communication
        socket.close()

        Log.d(TAG, "Object detector stopped")
    }

    /**
     * Processes a frame for object detection
     * @param frame Bitmap frame to process
     */
    fun processFrame(frame: Bitmap) {
        if (!isRunning.get()) {
            Log.d(TAG, "Detector not running, skipping frame")
            return
        }

        // Skip frame if previous detection is still processing
        if (isProcessing.get()) {
            Log.d(TAG, "Previous detection still processing, skipping frame")
            return
        }

        val mutableFrame = if (frame.isMutable) {
            frame
        } else {
            frame.copy(Bitmap.Config.ARGB_8888, true)
        }

        detectionJob = scope.launch {
            try {
                if (!isActive) return@launch

                isProcessing.set(true)
                performanceMonitor.onProcessingStart()

                Log.d(TAG, "Starting frame processing")

                // Paso 1: Detección
                val detectedObjects = withContext(Dispatchers.Default) {
                    if (!isActive) return@withContext emptyList()
                    yoloDetector.detect(mutableFrame)
                }

                if (!isActive) return@launch

                Log.d(TAG, "Detection completed, found ${detectedObjects.size} objects")

                // Paso 2: Análisis
                val analyzedObjects = objectAnalyzer.analyzeObjects(
                    detectedObjects = detectedObjects,
                    timestamp = System.currentTimeMillis()
                )

                if (!isActive) return@launch

                // Paso 3: Actualizar resultados
                lastProcessedFrame = mutableFrame
                lastDetectedObjects = analyzedObjects

                // Paso 4: Notificar en el hilo principal
                withContext(Dispatchers.Main) {
                    if (isActive && isRunning.get()) {
                        onDetectionResultListener?.invoke(analyzedObjects, mutableFrame)
                    }
                }

                // Paso 5: Enviar datos si aplica
                if (isActive && config.optBoolean("enable_communication", true) && socket.isConnected()) {
                    val metrics = performanceMonitor.getMetrics()

                    withContext(Dispatchers.Main) {
                        if (isActive && isRunning.get()) {
                            onPerformanceUpdateListener?.invoke(metrics)
                        }
                    }

                    if (isActive) {
                        val serializedData = dataSerializer.serializeData(analyzedObjects, metrics)
                        socket.queueMessage(serializedData)
                    }
                }

                performanceMonitor.onProcessingComplete()
                Log.d(TAG, "Frame processing completed successfully")

            } catch (e: CancellationException) {
                Log.d(TAG, "Detection job was cancelled")
                // No registrar como error, es cancelación normal
            } catch (e: Exception) {
                Log.e(TAG, "Error processing frame: ${e.message}", e)
            } finally {
                isProcessing.set(false)
            }
        }
    }

    /**
     * Handles messages received from Component B
     * @param message Message received from server
     */
    private fun handleServerMessage(message: String) {
        try {
            val json = JSONObject(message)

            // Handle different message types
            when (json.optString("type")) {
                "config_update" -> {
                    // Update configuration
                    val newConfig = json.optJSONObject("config") ?: return
                    updateConfig(newConfig)
                }
                "request_data" -> {
                    // Component B is requesting latest data
                    sendLatestData()
                }
                else -> {
                    Log.d(TAG, "Unknown message type: ${json.optString("type")}")
                }
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error handling message: ${e.message}")
        }
    }

    /**
     * Updates configuration with new values
     * @param newConfig New configuration values
     */
    fun updateConfig(newConfig: JSONObject) {
        try {
            // Update local config
            newConfig.keys().forEach { key ->
                config.put(key, newConfig.get(key))
            }

            // Apply config changes to components as needed
            if (newConfig.has("confidence_threshold")) {
                val threshold = newConfig.optDouble("confidence_threshold", 0.25).toFloat()
                yoloDetector.setConfidenceThreshold(threshold)
                Log.d(TAG, "Updated confidence threshold to: $threshold")
            }

            if (newConfig.has("iou_threshold")) {
                val threshold = newConfig.optDouble("iou_threshold", 0.45).toFloat()
                yoloDetector.setIouThreshold(threshold)
                Log.d(TAG, "Updated IoU threshold to: $threshold")
            }

            Log.d(TAG, "Configuration updated successfully")
        } catch (e: Exception) {
            Log.e(TAG, "Error updating configuration: ${e.message}")
        }
    }

    /**
     * Sends the latest detection data to Component B
     */
    private fun sendLatestData() {
        if (!socket.isConnected() || lastDetectedObjects.isEmpty()) {
            Log.d(TAG, "Cannot send data - socket not connected or no objects detected")
            return
        }

        try {
            val metrics = performanceMonitor.getMetrics()
            val serializedData = dataSerializer.serializeData(lastDetectedObjects, metrics)
            socket.queueMessage(serializedData)
            Log.d(TAG, "Latest data sent to Component B")
        } catch (e: Exception) {
            Log.e(TAG, "Error sending latest data: ${e.message}")
        }
    }

    /**
     * Sets a listener for detection results
     * @param listener Callback function that receives detected objects and the processed frame
     */
    fun setOnDetectionResultListener(listener: (List<DetectedObject>, Bitmap) -> Unit) {
        onDetectionResultListener = listener
    }

    /**
     * Sets a listener for performance updates
     * @param listener Callback function that receives performance metrics
     */
    fun setOnPerformanceUpdateListener(listener: (PerformanceMetrics) -> Unit) {
        onPerformanceUpdateListener = listener
    }

    /**
     * Gets current detection status
     */
    fun isDetectionRunning(): Boolean = isRunning.get()

    /**
     * Gets current processing status
     */
    fun isCurrentlyProcessing(): Boolean = isProcessing.get()

    /**
     * Gets last detected objects
     */
    fun getLastDetectedObjects(): List<DetectedObject> = lastDetectedObjects

    /**
     * Forces processing of current frame (useful for debugging)
     */
    fun forceProcessFrame(frame: Bitmap) {
        if (!isRunning.get()) {
            Log.w(TAG, "Cannot force process - detector not running")
            return
        }

        // Reset processing flag to allow forced processing
        isProcessing.set(false)
        processFrame(frame)
    }

    /**
     * Releases resources
     */
    fun release() {
        Log.d(TAG, "Releasing ObjectDetector resources")
        stop()

        // Cancel any pending jobs
        detectionJob?.cancel()

        // Release subcomponents
        yoloDetector.close()
        objectAnalyzer.release()

        Log.d(TAG, "ObjectDetector resources released")
    }

    companion object {
        private const val TAG = "ObjectDetector"
    }
}