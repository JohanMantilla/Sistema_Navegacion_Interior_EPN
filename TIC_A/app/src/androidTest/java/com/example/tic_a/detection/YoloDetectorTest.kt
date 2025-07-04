package com.example.tic_a.detection

import android.content.Context
import android.graphics.Bitmap
import android.graphics.RectF
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import com.example.tic_a.models.DetectedObject
import junit.framework.TestCase.assertEquals
import junit.framework.TestCase.assertFalse
import junit.framework.TestCase.assertNotNull
import junit.framework.TestCase.assertTrue
import org.junit.After
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.mockito.MockitoAnnotations

@RunWith(AndroidJUnit4::class)
class YoloDetectorTest {

    private lateinit var context: Context
    private lateinit var yoloDetector: YoloDetector
    private lateinit var testBitmap: Bitmap

    @Before
    fun setUp() {
        MockitoAnnotations.openMocks(this)
        context = InstrumentationRegistry.getInstrumentation().targetContext

        // Inicializar detector con configuración de prueba
        yoloDetector = YoloDetector(
            context = context,
            modelPath = "yolov8n.tflite",
            confThreshold = 0.25f,
            iouThreshold = 0.45f
        )

        // Crear bitmap de prueba (640x640 como entrada estándar de YOLO)
        testBitmap = createTestBitmap(640, 640)
    }

    @After
    fun tearDown() {
        yoloDetector.close()
    }

    @Test
    fun testDetectorInitialization() {
        assertNotNull("YoloDetector debe inicializarse correctamente", yoloDetector)
    }

    @Test
    fun testDetectWithValidBitmap() {
        // Ejecutar detección
        val detectedObjects = yoloDetector.detect(testBitmap)

        // Verificar que se ejecuta sin errores
        assertNotNull("La detección no debe retornar null", detectedObjects)
        assertTrue("La lista de objetos detectados debe ser válida", detectedObjects.size >= 0)

        // Verificar propiedades de objetos detectados
        for (obj in detectedObjects) {
            assertTrue("La confianza debe estar entre 0 y 1", obj.confidence in 0f..1f)
            assertTrue("La confianza debe superar el threshold", obj.confidence >= 0.25f)
            assertNotNull("El bounding box no debe ser null", obj.boundingBox)
            assertNotNull("La etiqueta de clase no debe ser null", obj.classLabel)
            assertTrue("La etiqueta no debe estar vacía", obj.classLabel.isNotEmpty())

            // Verificar coordenadas del bounding box
            val bbox = obj.boundingBox
            assertTrue("Left debe ser >= 0", bbox.left >= 0f)
            assertTrue("Top debe ser >= 0", bbox.top >= 0f)
            assertTrue("Right debe ser > Left", bbox.right > bbox.left)
            assertTrue("Bottom debe ser > Top", bbox.bottom > bbox.top)
        }
    }

    @Test
    fun testDetectWithEmptyBitmap() {
        val emptyBitmap = Bitmap.createBitmap(1, 1, Bitmap.Config.ARGB_8888)
        val detectedObjects = yoloDetector.detect(emptyBitmap)

        assertNotNull("Debe manejar bitmap vacío sin crash", detectedObjects)
    }

    @Test
    fun testConfidenceThresholdUpdate() {
        val newThreshold = 0.7f
        yoloDetector.setConfidenceThreshold(newThreshold)

        val detectedObjects = yoloDetector.detect(testBitmap)

        // Verificar que todas las detecciones superen el nuevo threshold
        for (obj in detectedObjects) {
            assertTrue(
                "Todas las detecciones deben superar el threshold actualizado",
                obj.confidence >= newThreshold
            )
        }
    }

    @Test
    fun testIoUThresholdUpdate() {
        val newIoUThreshold = 0.3f
        yoloDetector.setIouThreshold(newIoUThreshold)

        val detectedObjects = yoloDetector.detect(testBitmap)

        // Verificar que no hay solapamiento excesivo entre detecciones
        for (i in detectedObjects.indices) {
            for (j in i + 1 until detectedObjects.size) {
                val iou = calculateIoU(detectedObjects[i].boundingBox, detectedObjects[j].boundingBox)
                assertTrue(
                    "IoU entre detecciones debe ser menor al threshold NMS",
                    iou < newIoUThreshold
                )
            }
        }
    }

    @Test
    fun testDetectionConsistency() {
        // Ejecutar múltiples detecciones en la misma imagen
        val detections1 = yoloDetector.detect(testBitmap)
        val detections2 = yoloDetector.detect(testBitmap)

        // Las detecciones deben ser consistentes
        assertEquals(
            "El número de detecciones debe ser consistente",
            detections1.size,
            detections2.size
        )

        // Verificar que las detecciones principales coinciden
        if (detections1.isNotEmpty() && detections2.isNotEmpty()) {
            val maxConf1 = detections1.maxByOrNull { it.confidence }!!
            val maxConf2 = detections2.maxByOrNull { it.confidence }!!

            assertEquals(
                "La clase con mayor confianza debe ser consistente",
                maxConf1.classLabel,
                maxConf2.classLabel
            )
        }
    }

    @Test
    fun testBoundingBoxValidation() {
        val detectedObjects = yoloDetector.detect(testBitmap)

        for (obj in detectedObjects) {
            val bbox = obj.boundingBox

            // Verificar que las coordenadas están dentro de los límites de la imagen
            assertTrue("Left coordinate debe estar en rango válido",
                bbox.left >= 0f && bbox.left <= testBitmap.width)
            assertTrue("Top coordinate debe estar en rango válido",
                bbox.top >= 0f && bbox.top <= testBitmap.height)
            assertTrue("Right coordinate debe estar en rango válido",
                bbox.right >= 0f && bbox.right <= testBitmap.width)
            assertTrue("Bottom coordinate debe estar en rango válido",
                bbox.bottom >= 0f && bbox.bottom <= testBitmap.height)

            // Verificar dimensiones mínimas razonables
            val width = bbox.width()
            val height = bbox.height()
            assertTrue("Width debe ser positivo", width > 0f)
            assertTrue("Height debe ser positivo", height > 0f)
            assertTrue("Width debe ser razonable", width >= 5f) // Mínimo 5 píxeles
            assertTrue("Height debe ser razonable", height >= 5f) // Mínimo 5 píxeles
        }
    }

    // Función auxiliar para crear bitmap de prueba
    private fun createTestBitmap(width: Int, height: Int): Bitmap {
        return Bitmap.createBitmap(width, height, Bitmap.Config.ARGB_8888).apply {
            // Llenar con un patrón simple para simular una imagen real
            val canvas = android.graphics.Canvas(this)
            val paint = android.graphics.Paint().apply {
                color = android.graphics.Color.BLUE
            }
            canvas.drawRect(100f, 100f, 200f, 200f, paint)
        }
    }

    // Función auxiliar para calcular IoU
    private fun calculateIoU(box1: RectF, box2: RectF): Float {
        val intersectionLeft = maxOf(box1.left, box2.left)
        val intersectionTop = maxOf(box1.top, box2.top)
        val intersectionRight = minOf(box1.right, box2.right)
        val intersectionBottom = minOf(box1.bottom, box2.bottom)

        val intersectionArea = maxOf(0f, intersectionRight - intersectionLeft) *
                maxOf(0f, intersectionBottom - intersectionTop)

        val box1Area = box1.width() * box1.height()
        val box2Area = box2.width() * box2.height()

        return intersectionArea / (box1Area + box2Area - intersectionArea)
    }
}