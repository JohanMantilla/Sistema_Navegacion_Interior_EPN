package com.example.tic_a.detection

import android.content.Context
import android.graphics.Bitmap
import android.util.Size
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import junit.framework.TestCase.assertTrue
import org.json.JSONObject
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class ObjectDetectorIntegrationTest {

    private lateinit var context: Context
    private lateinit var objectDetector: ObjectDetector
    private lateinit var testBitmap: Bitmap

    @Before
    fun setUp() {
        context = InstrumentationRegistry.getInstrumentation().targetContext

        val config = JSONObject().apply {
            put("confidence_threshold", 0.25)
            put("iou_threshold", 0.45)
            put("enable_communication", false) // Deshabilitado para pruebas
            put("model_path", "yolov8n.tflite")
        }

        objectDetector = ObjectDetector(
            context = context,
            screenSize = Size(1080, 1920),
            config = config
        )

        testBitmap = Bitmap.createBitmap(640, 640, Bitmap.Config.ARGB_8888)
    }

    @Test
    fun testCompleteDetectionPipeline() {
        var detectionReceived = false
        var performanceReceived = false

        // Configurar listeners
        objectDetector.setOnDetectionResultListener { objects, frame ->
            detectionReceived = true
            assertTrue("Frame no debe ser null", frame != null)
        }

        objectDetector.setOnPerformanceUpdateListener { metrics ->
            performanceReceived = true
            assertTrue("FPS debe ser >= 0", metrics.fps >= 0f)
        }

        // Iniciar detector
        objectDetector.start()
        assertTrue("Detector debe estar ejecutándose", objectDetector.isDetectionRunning())

        // Procesar frame
        objectDetector.processFrame(testBitmap)

        // Esperar un momento para que se complete el procesamiento
        Thread.sleep(2000)

        // Verificar que se recibieron callbacks
        assertTrue("Debe recibirse resultado de detección", detectionReceived)

        // Limpiar
        objectDetector.stop()
        objectDetector.release()
    }
}