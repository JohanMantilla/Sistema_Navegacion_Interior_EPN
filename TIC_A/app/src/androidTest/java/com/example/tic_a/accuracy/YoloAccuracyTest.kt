package com.example.tic_a.accuracy

import android.content.Context
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.graphics.RectF
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import com.example.tic_a.detection.YoloDetector
import com.example.tic_a.models.DetectedObject
import junit.framework.TestCase.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith

data class GroundTruthObject(
    val classLabel: String,
    val boundingBox: RectF,
    val confidence: Float = 1.0f
)

@RunWith(AndroidJUnit4::class)
class YoloAccuracyTest {

    private lateinit var context: Context
    private lateinit var yoloDetector: YoloDetector

    @Before
    fun setUp() {
        context = InstrumentationRegistry.getInstrumentation().targetContext
        yoloDetector = YoloDetector(context, "yolov8n.tflite", 0.25f, 0.45f)
    }

    @Test
    fun testAccuracyWithGroundTruth() {
        // Imagen con ground truth conocido
        val bitmap = loadTestImage("test_annotated_scene.jpg")
        val groundTruth = loadGroundTruth("test_annotated_scene.json")

        val detections = yoloDetector.detect(bitmap)
        val metrics = calculateAccuracyMetrics(detections, groundTruth)

        println("Precision: ${metrics.precision}")
        println("Recall: ${metrics.recall}")
        println("F1-Score: ${metrics.f1Score}")

        // Verificar métricas mínimas aceptables
        assertTrue("Precision debe ser >= 0.6", metrics.precision >= 0.6f)
        assertTrue("Recall debe ser >= 0.5", metrics.recall >= 0.5f)
        assertTrue("F1-Score debe ser >= 0.55", metrics.f1Score >= 0.55f)
    }

    @Test
    fun testClassSpecificAccuracy() {
        val bitmap = loadTestImage("test_person_car_scene.jpg")
        val groundTruth = loadGroundTruth("test_person_car_scene.json")
        val detections = yoloDetector.detect(bitmap)

        // Calcular precisión por clase
        val personMetrics = calculateClassAccuracy(detections, groundTruth, "person")
        val carMetrics = calculateClassAccuracy(detections, groundTruth, "car")

        println("Person - Precision: ${personMetrics.precision}, Recall: ${personMetrics.recall}")
        println("Car - Precision: ${carMetrics.precision}, Recall: ${carMetrics.recall}")

        // Las personas suelen ser más fáciles de detectar que los carros
        assertTrue("Person precision debe ser >= 0.7", personMetrics.precision >= 0.7f)
        assertTrue("Car precision debe ser >= 0.6", carMetrics.precision >= 0.6f)
    }

    @Test
    fun testFalsePositiveRate() {
        // Imagen sin objetos de interés
        val bitmap = loadTestImage("test_background_only.jpg")
        val detections = yoloDetector.detect(bitmap)

        // Contar detecciones con alta confianza (posibles falsos positivos)
        val highConfidenceDetections = detections.filter { it.confidence >= 0.7f }

        // En una imagen de solo fondo, no debería haber muchas detecciones de alta confianza
        assertTrue("Falsos positivos deben ser mínimos", highConfidenceDetections.size <= 2)
    }

    private fun loadTestImage(filename: String): Bitmap {
        val inputStream = context.assets.open("test_images/$filename")
        return BitmapFactory.decodeStream(inputStream)
    }

    private fun loadGroundTruth(filename: String): List<GroundTruthObject> {
        // Simulación de carga de ground truth desde JSON
        // En una implementación real, cargarías desde archivo JSON
        return when (filename) {
            "test_annotated_scene.json" -> listOf(
                GroundTruthObject("person", RectF(100f, 50f, 200f, 300f)),
                GroundTruthObject("car", RectF(300f, 200f, 500f, 350f))
            )
            "test_person_car_scene.json" -> listOf(
                GroundTruthObject("person", RectF(150f, 100f, 250f, 400f)),
                GroundTruthObject("person", RectF(400f, 120f, 480f, 380f)),
                GroundTruthObject("car", RectF(50f, 250f, 300f, 400f))
            )
            else -> emptyList()
        }
    }

    private fun calculateAccuracyMetrics(
        detections: List<DetectedObject>,
        groundTruth: List<GroundTruthObject>,
        iouThreshold: Float = 0.5f
    ): AccuracyMetrics {
        var truePositives = 0
        var falsePositives = 0
        var falseNegatives = 0

        val matchedGroundTruth = mutableSetOf<Int>()
        val matchedDetections = mutableSetOf<Int>()

        // Encontrar coincidencias
        for (i in detections.indices) {
            var bestMatch = -1
            var bestIou = 0f

            for (j in groundTruth.indices) {
                if (j in matchedGroundTruth) continue
                if (detections[i].classLabel != groundTruth[j].classLabel) continue

                val iou = calculateIoU(detections[i].boundingBox, groundTruth[j].boundingBox)
                if (iou > bestIou && iou >= iouThreshold) {
                    bestIou = iou
                    bestMatch = j
                }
            }

            if (bestMatch != -1) {
                truePositives++
                matchedGroundTruth.add(bestMatch)
                matchedDetections.add(i)
            }
        }

        falsePositives = detections.size - matchedDetections.size
        falseNegatives = groundTruth.size - matchedGroundTruth.size

        val precision = if (truePositives + falsePositives > 0) {
            truePositives.toFloat() / (truePositives + falsePositives)
        } else 0f

        val recall = if (truePositives + falseNegatives > 0) {
            truePositives.toFloat() / (truePositives + falseNegatives)
        } else 0f

        val f1Score = if (precision + recall > 0) {
            2 * (precision * recall) / (precision + recall)
        } else 0f

        return AccuracyMetrics(precision, recall, f1Score, truePositives, falsePositives, falseNegatives)
    }

    private fun calculateClassAccuracy(
        detections: List<DetectedObject>,
        groundTruth: List<GroundTruthObject>,
        className: String
    ): AccuracyMetrics {
        val classDetections = detections.filter { it.classLabel == className }
        val classGroundTruth = groundTruth.filter { it.classLabel == className }

        return calculateAccuracyMetrics(classDetections, classGroundTruth)
    }

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

data class AccuracyMetrics(
    val precision: Float,
    val recall: Float,
    val f1Score: Float,
    val truePositives: Int,
    val falsePositives: Int,
    val falseNegatives: Int
)