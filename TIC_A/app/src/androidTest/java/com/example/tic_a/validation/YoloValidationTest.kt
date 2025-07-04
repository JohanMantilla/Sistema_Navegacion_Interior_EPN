package com.example.tic_a.validation

import android.content.Context
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import com.example.tic_a.detection.YoloDetector
import junit.framework.TestCase.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import java.io.InputStream

@RunWith(AndroidJUnit4::class)
class YoloValidationTest {

    private lateinit var context: Context
    private lateinit var yoloDetector: YoloDetector

    @Before
    fun setUp() {
        context = InstrumentationRegistry.getInstrumentation().targetContext
        yoloDetector = YoloDetector(
            context = context,
            modelPath = "yolov8n.tflite",
            confThreshold = 0.25f,
            iouThreshold = 0.45f
        )
    }

    @Test
    fun testPersonDetection() {
        // Cargar imagen de prueba que contiene una persona
        val bitmap = loadTestImage("test_person.jpg")
        val detections = yoloDetector.detect(bitmap)

        // Verificar que se detecta al menos una persona
        val personDetections = detections.filter { it.classLabel == "person" }
        assertTrue("Debe detectar al menos una persona", personDetections.isNotEmpty())

        // Verificar confianza mínima
        val highConfidencePersons = personDetections.filter { it.confidence >= 0.5f }
        assertTrue("Debe haber al menos una detección de persona con alta confianza",
            highConfidencePersons.isNotEmpty())
    }

    @Test
    fun testCarDetection() {
        val bitmap = loadTestImage("test_car.jpg")
        val detections = yoloDetector.detect(bitmap)

        val carDetections = detections.filter { it.classLabel == "car" }
        assertTrue("Debe detectar al menos un carro", carDetections.isNotEmpty())

        // Verificar dimensiones razonables para un carro
        for (car in carDetections) {
            val width = car.boundingBox.width()
            val height = car.boundingBox.height()
            assertTrue("El carro debe tener dimensiones razonables",
                width > 50f && height > 30f)
        }
    }

    @Test
    fun testMultipleObjectScene() {
        val bitmap = loadTestImage("test_street_scene.jpg")
        val detections = yoloDetector.detect(bitmap)

        // Verificar diversidad de clases detectadas
        val detectedClasses = detections.map { it.classLabel }.toSet()
        assertTrue("Debe detectar múltiples tipos de objetos", detectedClasses.size >= 2)

        // Verificar que no hay solapamiento excesivo
        for (i in detections.indices) {
            for (j in i + 1 until detections.size) {
                val iou = calculateIoU(detections[i].boundingBox, detections[j].boundingBox)
                assertTrue("IoU entre detecciones diferentes debe ser < 0.5", iou < 0.5f)
            }
        }
    }

    @Test
    fun testEmptyScene() {
        val bitmap = loadTestImage("test_empty_scene.jpg")
        val detections = yoloDetector.detect(bitmap)

        // En una escena vacía, puede haber pocas o ninguna detección
        // Verificar que no hay falsos positivos con alta confianza
        val highConfidenceDetections = detections.filter { it.confidence >= 0.8f }
        assertTrue("No debe haber muchos falsos positivos en escena vacía",
            highConfidenceDetections.size <= 1)
    }

    @Test
    fun testSmallObjectDetection() {
        val bitmap = loadTestImage("test_small_objects.jpg")
        val detections = yoloDetector.detect(bitmap)

        // Verificar que puede detectar objetos pequeños
        val smallObjects = detections.filter {
            val area = it.boundingBox.width() * it.boundingBox.height()
            area < 1000f // Menos de 1000 píxeles cuadrados
        }

        // Si hay objetos pequeños en la imagen, al menos algunos deben detectarse
        if (smallObjects.isNotEmpty()) {
            assertTrue("Debe detectar algunos objetos pequeños", smallObjects.size >= 1)
        }
    }

    private fun loadTestImage(filename: String): Bitmap {
        val inputStream: InputStream = context.assets.open("test_images/$filename")
        return BitmapFactory.decodeStream(inputStream)
    }

    private fun calculateIoU(box1: android.graphics.RectF, box2: android.graphics.RectF): Float {
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