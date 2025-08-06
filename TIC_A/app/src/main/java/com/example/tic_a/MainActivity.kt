package com.example.tic_a

import android.Manifest
import android.content.pm.PackageManager
import android.graphics.Bitmap
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.RectF
import android.os.Bundle
import android.util.Log
import android.util.Size
import android.view.View
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.Button
import android.widget.SeekBar
import android.widget.Spinner
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.camera.core.AspectRatio
import androidx.camera.core.CameraSelector
import androidx.camera.core.ImageAnalysis
import androidx.camera.core.ImageProxy
import androidx.camera.core.Preview
import androidx.camera.lifecycle.ProcessCameraProvider
import androidx.camera.view.PreviewView
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import com.example.tic_a.detection.ObjectDetector
import com.example.tic_a.models.DetectedObject
import com.example.tic_a.models.PerformanceMetrics
import com.example.tic_a.comunication.SocketCommunication
import com.example.tic_a.comunication.DataSerializer
import org.json.JSONObject
import java.util.concurrent.ExecutorService
import java.util.concurrent.Executors
import androidx.camera.core.resolutionselector.ResolutionSelector
import androidx.camera.core.resolutionselector.ResolutionStrategy
import android.view.Surface
import androidx.camera.core.resolutionselector.AspectRatioStrategy

class MainActivity : AppCompatActivity() {
    private lateinit var previewView: PreviewView
    private lateinit var overlayView: OverlayView
    private lateinit var fpsTextView: TextView
    private lateinit var objectCountTextView: TextView
    private lateinit var confidenceSeekBar: SeekBar
    private lateinit var confidenceTextView: TextView
    private lateinit var startStopButton: Button
    private lateinit var modeSpinner: Spinner

    private lateinit var cameraExecutor: ExecutorService
    private var objectDetector: ObjectDetector? = null
    private var isDetecting = false

    // CONFIGURACIÓN DEL SERVIDOR
    private var socketCommunication: SocketCommunication? = null
    private val dataSerializer = DataSerializer()

    private val SERVER_CONFIGS = mapOf(
        "Local" to Pair("192.168.1.100", 8080),
        "ngrok" to Pair("0.tcp.sa.ngrok.io", 11517),
        "Production" to Pair("servidor.com", 8080)
    )

    private var currentServerConfig = "ngrok"

    // Configuration
    private val config = JSONObject().apply {
        put("confidence_threshold", 0.5)
        put("iou_threshold", 0.5)
        put("enable_communication", true)
        put("server_address", SERVER_CONFIGS[currentServerConfig]?.first ?: "127.0.0.1")
        put("server_port", SERVER_CONFIGS[currentServerConfig]?.second ?: 8080)
        put("model_path", "yolov8n.tflite")
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        // Initialize UI components
        previewView = findViewById(R.id.preview_view)
        overlayView = findViewById(R.id.overlay_view)
        fpsTextView = findViewById(R.id.fps_text_view)
        objectCountTextView = findViewById(R.id.object_count_text_view)
        confidenceSeekBar = findViewById(R.id.confidence_seek_bar)
        confidenceTextView = findViewById(R.id.confidence_text_view)
        startStopButton = findViewById(R.id.start_stop_button)
        modeSpinner = findViewById(R.id.mode_spinner)

        // INICIALIZAR COMUNICACIÓN CON SERVIDOR
        initializeSocketCommunication()

        // Set up confidence threshold slider
        confidenceSeekBar.max = 100
        confidenceSeekBar.progress = (config.optDouble("confidence_threshold", 0.5) * 100).toInt()
        updateConfidenceText()

        confidenceSeekBar.setOnSeekBarChangeListener(object : SeekBar.OnSeekBarChangeListener {
            override fun onProgressChanged(seekBar: SeekBar?, progress: Int, fromUser: Boolean) {
                updateConfidenceText()
            }

            override fun onStartTrackingTouch(seekBar: SeekBar?) {}

            override fun onStopTrackingTouch(seekBar: SeekBar?) {
                val threshold = seekBar?.progress?.toFloat()?.div(100f) ?: 0.5f
                config.put("confidence_threshold", threshold)
                objectDetector?.updateConfig(config)
            }
        })

        // Set up mode spinner
        val modes = arrayOf("Camera Only", "Component B Integration")
        val modeAdapter = ArrayAdapter(this, android.R.layout.simple_spinner_item, modes)
        modeAdapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
        modeSpinner.adapter = modeAdapter

        modeSpinner.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
            override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) {
                val enableCommunication = position == 1
                config.put("enable_communication", enableCommunication)
                objectDetector?.updateConfig(config)

                if (enableCommunication) {
                    // INICIAR CONEXIÓN CUANDO SE HABILITA
                    socketCommunication?.start()
                    Toast.makeText(this@MainActivity, "Component B Integration Enabled - Connecting to server...", Toast.LENGTH_SHORT).show()
                } else {
                    // CERRAR CONEXIÓN CUANDO SE DESHABILITA
                    socketCommunication?.close()
                    Toast.makeText(this@MainActivity, "Camera Only Mode", Toast.LENGTH_SHORT).show()
                }
            }

            override fun onNothingSelected(parent: AdapterView<*>?) {}
        }

        // Set up start/stop button
        startStopButton.setOnClickListener {
            if (isDetecting) {
                stopDetection()
            } else {
                startDetection()
            }
        }

        // Initialize camera executor
        cameraExecutor = Executors.newSingleThreadExecutor()

        // Initialize object detector
        initializeObjectDetector()

        // Request camera permission
        if (allPermissionsGranted()) {
            startCamera()
        } else {
            ActivityCompat.requestPermissions(
                this, REQUIRED_PERMISSIONS, REQUEST_CODE_PERMISSIONS
            )
        }
    }

    private fun initializeSocketCommunication() {
        try {
            val serverAddress = SERVER_CONFIGS[currentServerConfig]?.first ?: "127.0.0.1"
            val serverPort = SERVER_CONFIGS[currentServerConfig]?.second ?: 8080

            Log.d(TAG, "Initializing socket communication to $serverAddress:$serverPort")

            socketCommunication = SocketCommunication(
                serverAddress = serverAddress,
                serverPort = serverPort,
                messageCallback = { message ->
                    // MANEJAR RESPUESTAS DEL SERVIDOR
                    Log.d(TAG, "Server response: $message")
                    runOnUiThread {
                        Toast.makeText(this, "Server: $message", Toast.LENGTH_SHORT).show()
                    }
                }
            )

            Log.d(TAG, "Socket communication initialized successfully")

        } catch (e: Exception) {
            Log.e(TAG, "Error initializing socket communication", e)
            Toast.makeText(this, "Error connecting to server: ${e.message}", Toast.LENGTH_LONG).show()
        }
    }

    private fun initializeObjectDetector() {
        try {
            Log.d(TAG, "Initializing object detector...")

            val modelPath = config.optString("model_path", "yolov8n.tflite")
            Log.d(TAG, "Looking for model: $modelPath")

            val assetManager = assets
            try {
                val inputStream = assetManager.open(modelPath)
                inputStream.close()
                Log.d(TAG, "Model file found successfully")
            } catch (e: Exception) {
                Log.e(TAG, "Model file not found in assets: $modelPath", e)
                Toast.makeText(this, "Error: Model file not found ($modelPath)", Toast.LENGTH_LONG).show()
                return
            }

            val displayMetrics = resources.displayMetrics
            val screenSize = Size(displayMetrics.widthPixels, displayMetrics.heightPixels)
            Log.d(TAG, "Screen size: ${screenSize.width} x ${screenSize.height}")

            objectDetector = ObjectDetector(
                context = this,
                screenSize = screenSize,
                config = config
            ).apply {
                Log.d(TAG, "ObjectDetector instance created")

                setOnDetectionResultListener { detectedObjects, processedFrame ->
                    Log.d(TAG, "Detection result received: ${detectedObjects.size} objects")
                    runOnUiThread {
                        updateObjectCount(detectedObjects.size)
                        overlayView.updateDetections(detectedObjects, Size(640, 640))
                    }

                    // ENVIAR DATOS AL SERVIDOR SI ESTÁ HABILITADO
                    if (config.optBoolean("enable_communication", false) &&
                        socketCommunication?.isConnected() == true) {
                        sendDetectionDataToServer(detectedObjects)
                    }
                }

                setOnPerformanceUpdateListener { metrics ->
                    Log.d(TAG, "Performance update: FPS=${metrics.fps}")
                    runOnUiThread {
                        updatePerformanceMetrics(metrics)
                    }

                    // ENVIAR MÉTRICAS AL SERVIDOR OCASIONALMENTE
                    if (config.optBoolean("enable_communication", false) &&
                        socketCommunication?.isConnected() == true) {
                        // Enviar métricas cada 30 frames aproximadamente (1 vez por segundo a 30 FPS)
                        if (System.currentTimeMillis() % 1000 < 50) {
                            sendPerformanceDataToServer(metrics)
                        }
                    }
                }
            }

            Log.d(TAG, "Object detector initialized successfully")

        } catch (e: Exception) {
            Log.e(TAG, "Error initializing object detector", e)
            Toast.makeText(this, "Error initializing detector: ${e.message}", Toast.LENGTH_LONG).show()
        }
    }

    // Enviar datos de detección al servidor
    private fun sendDetectionDataToServer(detectedObjects: List<DetectedObject>) {
        try {
            val jsonData = dataSerializer.serializeData(detectedObjects)
            socketCommunication?.queueMessage(jsonData)
            Log.d(TAG, "Detection data sent to server: ${detectedObjects.size} objects")
        } catch (e: Exception) {
            Log.e(TAG, "Error sending detection data to server", e)
        }
    }

    // Enviar métricas de rendimiento al servidor
    private fun sendPerformanceDataToServer(metrics: PerformanceMetrics) {
        try {
            val jsonData = dataSerializer.serializeData(emptyList(), metrics)
            socketCommunication?.queueMessage(jsonData)
            Log.d(TAG, "Performance metrics sent to server: FPS=${metrics.fps}")
        } catch (e: Exception) {
            Log.e(TAG, "Error sending performance data to server", e)
        }
    }

    private fun startCamera() {
        val cameraProviderFuture = ProcessCameraProvider.getInstance(this)

        cameraProviderFuture.addListener({
            val cameraProvider = cameraProviderFuture.get()

            // Preview
            val previewResolutionSelector = ResolutionSelector.Builder()
                .setAspectRatioStrategy(
                    AspectRatioStrategy(
                        AspectRatio.RATIO_16_9,
                        AspectRatioStrategy.FALLBACK_RULE_AUTO
                    )
                )
                .build()

            val preview = Preview.Builder()
                .setResolutionSelector(previewResolutionSelector)
                .build()
                .also {
                    it.setSurfaceProvider(previewView.surfaceProvider)
                }

            val resolutionSelector = ResolutionSelector.Builder()
                .setResolutionStrategy(
                    ResolutionStrategy(
                        Size(640, 640),
                        ResolutionStrategy.FALLBACK_RULE_CLOSEST_HIGHER_THEN_LOWER
                    )
                )
                .build()

            val imageAnalyzer = ImageAnalysis.Builder()
                .setResolutionSelector(resolutionSelector)
                .setTargetRotation(Surface.ROTATION_0)
                .setBackpressureStrategy(ImageAnalysis.STRATEGY_KEEP_ONLY_LATEST)
                .build()
                .also {
                    it.setAnalyzer(cameraExecutor) { imageProxy ->
                        if (isDetecting) {
                            processImage(imageProxy)
                        } else {
                            imageProxy.close()
                        }
                    }
                }

            // Camera selector
            val cameraSelector = CameraSelector.DEFAULT_BACK_CAMERA

            try {
                cameraProvider.unbindAll()
                cameraProvider.bindToLifecycle(
                    this, cameraSelector, preview, imageAnalyzer
                )
            } catch (exc: Exception) {
                Log.e(TAG, "Use case binding failed", exc)
                Toast.makeText(this, "Camera binding failed: ${exc.message}", Toast.LENGTH_LONG).show()
            }

        }, ContextCompat.getMainExecutor(this))
    }

    private fun processImage(imageProxy: ImageProxy) {
        try {
            Log.d(TAG, "Processing image: ${imageProxy.width}x${imageProxy.height}, format: ${imageProxy.format}")

            val bitmap = imageProxyToBitmap(imageProxy)?.let {
                Bitmap.createScaledBitmap(it, 640, 640, true)
            }

            if (bitmap != null) {
                Log.d(TAG, "Bitmap created successfully: ${bitmap.width}x${bitmap.height}")
                objectDetector?.processFrame(bitmap)
            } else {
                Log.e(TAG, "Failed to create bitmap from ImageProxy")
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error processing image", e)
        } finally {
            imageProxy.close()
        }
    }

    private fun imageProxyToBitmap(image: ImageProxy): Bitmap? {
        return try {
            val yBuffer = image.planes[0].buffer
            val uBuffer = image.planes[1].buffer
            val vBuffer = image.planes[2].buffer

            val ySize = yBuffer.remaining()
            val uSize = uBuffer.remaining()
            val vSize = vBuffer.remaining()

            val nv21 = ByteArray(ySize + uSize + vSize)

            yBuffer.get(nv21, 0, ySize)
            vBuffer.get(nv21, ySize, vSize)
            uBuffer.get(nv21, ySize + vSize, uSize)

            val yuvImage = android.graphics.YuvImage(nv21, android.graphics.ImageFormat.NV21,
                image.width, image.height, null)
            val out = java.io.ByteArrayOutputStream()
            yuvImage.compressToJpeg(android.graphics.Rect(0, 0, image.width, image.height), 100, out)
            val imageBytes = out.toByteArray()

            val bitmap = android.graphics.BitmapFactory.decodeByteArray(imageBytes, 0, imageBytes.size)

            val rotationDegrees = image.imageInfo.rotationDegrees
            if (rotationDegrees != 0) {
                val matrix = android.graphics.Matrix()
                matrix.postRotate(rotationDegrees.toFloat())
                return Bitmap.createBitmap(bitmap, 0, 0, bitmap.width, bitmap.height, matrix, true)
            }

            bitmap
        } catch (e: Exception) {
            Log.e(TAG, "Error converting YUV to RGB, falling back to grayscale", e)
            try {
                val yPlane = image.planes[0]
                val yBuffer = yPlane.buffer
                val yBytes = ByteArray(yBuffer.remaining())
                yBuffer.get(yBytes)

                val bitmap = Bitmap.createBitmap(image.width, image.height, Bitmap.Config.ARGB_8888)
                val pixels = IntArray(image.width * image.height)

                for (i in yBytes.indices) {
                    val gray = yBytes[i].toInt() and 0xFF
                    pixels[i] = Color.rgb(gray, gray, gray)
                }

                bitmap.setPixels(pixels, 0, image.width, 0, 0, image.width, image.height)

                val rotationDegrees = image.imageInfo.rotationDegrees
                if (rotationDegrees != 0) {
                    val matrix = android.graphics.Matrix()
                    matrix.postRotate(rotationDegrees.toFloat())
                    return Bitmap.createBitmap(bitmap, 0, 0, bitmap.width, bitmap.height, matrix, true)
                }

                bitmap
            } catch (fallbackException: Exception) {
                Log.e(TAG, "Fallback bitmap conversion also failed", fallbackException)
                null
            }
        }
    }

    private fun startDetection() {
        try {
            Log.d(TAG, "Starting detection...")
            objectDetector?.start()
            isDetecting = true
            startStopButton.text = "Stop Detection"
            Toast.makeText(this, "Detection started", Toast.LENGTH_SHORT).show()
        } catch (e: Exception) {
            Log.e(TAG, "Error starting detection", e)
            Toast.makeText(this, "Error starting detection: ${e.message}", Toast.LENGTH_LONG).show()
        }
    }

    private fun stopDetection() {
        try {
            Log.d(TAG, "Stopping detection...")
            objectDetector?.stop()
            isDetecting = false
            startStopButton.text = "Start Detection"
            overlayView.clearDetections()
            updateObjectCount(0)
            updatePerformanceMetrics(PerformanceMetrics(0.0.toFloat(), 0.toFloat(), 0.0.toFloat()))
            Toast.makeText(this, "Detection stopped", Toast.LENGTH_SHORT).show()
        } catch (e: Exception) {
            Log.e(TAG, "Error stopping detection", e)
            Toast.makeText(this, "Error stopping detection: ${e.message}", Toast.LENGTH_LONG).show()
        }
    }

    private fun updateObjectCount(count: Int) {
        objectCountTextView.text = "Objects: $count"
    }

    private fun updatePerformanceMetrics(metrics: PerformanceMetrics) {
        fpsTextView.text = "FPS: ${String.format("%.1f", metrics.fps)}"
    }

    private fun updateConfidenceText() {
        val confidence = confidenceSeekBar.progress / 100.0
        confidenceTextView.text = "Confidence: ${String.format("%.2f", confidence)}"
    }

    private fun allPermissionsGranted() = REQUIRED_PERMISSIONS.all {
        ContextCompat.checkSelfPermission(baseContext, it) == PackageManager.PERMISSION_GRANTED
    }

    override fun onRequestPermissionsResult(
        requestCode: Int, permissions: Array<String>, grantResults: IntArray
    ) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults)
        if (requestCode == REQUEST_CODE_PERMISSIONS) {
            if (allPermissionsGranted()) {
                startCamera()
            } else {
                Toast.makeText(this, "Permissions not granted by the user.", Toast.LENGTH_SHORT).show()
                finish()
            }
        }
    }

    override fun onDestroy() {
        super.onDestroy()
        try {
            // CERRAR CONEXIÓN AL SERVIDOR
            socketCommunication?.close()
            objectDetector?.release()
            cameraExecutor.shutdown()
        } catch (e: Exception) {
            Log.e(TAG, "Error during cleanup", e)
        }
    }

    companion object {
        private const val TAG = "MainActivity"
        private const val REQUEST_CODE_PERMISSIONS = 10
        private val REQUIRED_PERMISSIONS = arrayOf(Manifest.permission.CAMERA)
    }
}

// OverlayView sin cambios
class OverlayView @JvmOverloads constructor(
    context: android.content.Context,
    attrs: android.util.AttributeSet? = null,
    defStyleAttr: Int = 0
) : View(context, attrs, defStyleAttr) {

    private var detectedObjects: List<DetectedObject> = emptyList()
    private var imageWidth: Int = 640
    private var imageHeight: Int = 640

    private val paint = Paint().apply {
        color = Color.GREEN
        strokeWidth = 6f
        style = Paint.Style.STROKE
    }
    private val textPaint = Paint().apply {
        color = Color.GREEN
        textSize = 36f
        style = Paint.Style.FILL
        setShadowLayer(2f, 2f, 2f, Color.BLACK)
    }
    private val backgroundPaint = Paint().apply {
        color = Color.argb(128, 0, 0, 0)
        style = Paint.Style.FILL
    }

    fun updateDetections(objects: List<DetectedObject>, processingImageSize: Size? = null) {
        detectedObjects = objects
        processingImageSize?.let {
            imageWidth = it.width
            imageHeight = it.height
        }
        Log.d("OverlayView", "Updating overlay with ${objects.size} detections")
        invalidate()
    }

    fun clearDetections() {
        detectedObjects = emptyList()
        invalidate()
    }

    override fun onDraw(canvas: Canvas) {
        super.onDraw(canvas)

        if (detectedObjects.isEmpty() || width == 0 || height == 0) {
            return
        }

        canvas.let { c ->
            detectedObjects.forEach { obj ->
                val scaleX = width.toFloat() / imageWidth.toFloat()
                val scaleY = height.toFloat() / imageHeight.toFloat()
                val scale = minOf(scaleX, scaleY)

                val scaledWidth = imageWidth * scale
                val scaledHeight = imageHeight * scale
                val offsetX = (width - scaledWidth) / 2f
                val offsetY = (height - scaledHeight) / 2f

                val rect = RectF(
                    obj.boundingBox.left * scale + offsetX,
                    obj.boundingBox.top * scale + offsetY,
                    obj.boundingBox.right * scale + offsetX,
                    obj.boundingBox.bottom * scale + offsetY
                )

                rect.intersect(0f, 0f, width.toFloat(), height.toFloat())

                if (rect.width() > 0 && rect.height() > 0) {
                    c.drawRect(rect, paint)

                    val label = "${obj.classLabel} (${String.format("%.2f", obj.confidence)})"
                    val textBounds = android.graphics.Rect()
                    textPaint.getTextBounds(label, 0, label.length, textBounds)

                    val textX = rect.left
                    val textY = maxOf(rect.top - 10f, textBounds.height().toFloat() + 10f)

                    c.drawRect(
                        textX - 5f,
                        textY - textBounds.height() - 5f,
                        textX + textBounds.width() + 5f,
                        textY + 5f,
                        backgroundPaint
                    )

                    c.drawText(label, textX, textY, textPaint)

                    var offsetY = 35f

                    if (obj.speed > 5f) {
                        val speedText = "Speed: ${String.format("%.1f", obj.speed)} px/s"
                        c.drawRect(
                            rect.left - 5f,
                            rect.bottom + offsetY - textBounds.height() - 5f,
                            rect.left + textPaint.measureText(speedText) + 5f,
                            rect.bottom + offsetY + 5f,
                            backgroundPaint
                        )
                        c.drawText(speedText, rect.left, rect.bottom + offsetY, textPaint)
                        offsetY += 35f
                    }

                    if (obj.distance > 0f) {
                        val distanceText = "Distance: ${String.format("%.1f", obj.distance)} m"
                        c.drawRect(
                            rect.left - 5f,
                            rect.bottom + offsetY - textBounds.height() - 5f,
                            rect.left + textPaint.measureText(distanceText) + 5f,
                            rect.bottom + offsetY + 5f,
                            backgroundPaint
                        )
                        c.drawText(distanceText, rect.left, rect.bottom + offsetY, textPaint)
                    }
                }
            }
        }
    }
}