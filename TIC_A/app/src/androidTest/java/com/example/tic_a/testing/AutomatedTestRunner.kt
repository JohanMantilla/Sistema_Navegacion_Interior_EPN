package com.example.tic_a.testing

import android.content.Context
import android.graphics.Bitmap
import android.graphics.RectF
import android.util.Log
import com.example.tic_a.detection.ObjectDetector
import com.example.tic_a.detection.YoloDetector
import com.example.tic_a.models.DetectedObject
import com.example.tic_a.utils.*
import kotlinx.coroutines.*
import org.json.JSONObject
import java.io.File
import java.io.FileWriter

class AutomatedTestRunner(
    private val context: Context,
    private val outputDir: File
) {

    private val testResults = mutableListOf<TestResult>()

    suspend fun runAllTests(): String = withContext(Dispatchers.Default) {
        Log.i(TAG, "Iniciando suite completa de pruebas...")

        // Crear directorio de salida
        outputDir.mkdirs()

        // Ejecutar diferentes categorías de pruebas
        runBasicDetectionTests()
        runAccuracyTests()
        runPerformanceTests()
        runSpeedAnalysisTests()
        runStressTests()

        // Generar reporte final
        val report = TestUtils.generateTestReport(testResults)
        saveReport(report)

        Log.i(TAG, "Suite de pruebas completada. Resultados: ${testResults.count { it.passed }}/${testResults.size} exitosas")

        return@withContext report
    }

    private suspend fun runBasicDetectionTests() {
        Log.i(TAG, "Ejecutando pruebas básicas de detección...")

        val yoloDetector = YoloDetector(context, "yolov8n.tflite", 0.25f, 0.45f)

        try {
            // Test 1: Detección con imagen sintética simple
            runTest("basic_synthetic_detection") {
                val syntheticObjects = listOf(
                    SyntheticObject(
                        SyntheticObjectType.PERSON_SHAPE,
                        RectF(100f, 50f, 200f, 300f)
                    ),
                    SyntheticObject(
                        SyntheticObjectType.CAR_SHAPE,
                        RectF(300f, 200f, 500f, 350f)
                    )
                )

                val testImage = TestUtils.createSyntheticTestImage(640, 640, syntheticObjects)
                val detections = yoloDetector.detect(testImage)

                val hasPersonDetection = detections.any { it.classLabel == "person" }
                val hasCarDetection = detections.any { it.classLabel == "car" }

                mapOf(
                    "detections_count" to detections.size,
                    "person_detected" to hasPersonDetection,
                    "car_detected" to hasCarDetection,
                    "avg_confidence" to detections.map { it.confidence }.average()
                )
            }

            // Test 2: Consistencia de detección
            runTest("detection_consistency") {
                val testImage = TestUtils.createSyntheticTestImage(640, 640)

                val detections1 = yoloDetector.detect(testImage)
                val detections2 = yoloDetector.detect(testImage)
                val detections3 = yoloDetector.detect(testImage)

                val comparison12 = TestUtils.compareDetections(detections1, detections2)
                val comparison13 = TestUtils.compareDetections(detections1, detections3)

                mapOf(
                    "similarity_12" to comparison12.similarity,
                    "similarity_13" to comparison13.similarity,
                    "consistency_score" to (comparison12.similarity + comparison13.similarity) / 2
                )
            }

            // Test 3: Umbral de confianza
            runTest("confidence_threshold_test") {
                val testImage = TestUtils.createSyntheticTestImage(640, 640)

                yoloDetector.setConfidenceThreshold(0.1f)
                val lowThresholdDetections = yoloDetector.detect(testImage)

                yoloDetector.setConfidenceThreshold(0.8f)
                val highThresholdDetections = yoloDetector.detect(testImage)

                val thresholdWorking = highThresholdDetections.size <= lowThresholdDetections.size &&
                        highThresholdDetections.all { it.confidence >= 0.8f }

                mapOf(
                    "low_threshold_count" to lowThresholdDetections.size,
                    "high_threshold_count" to highThresholdDetections.size,
                    "threshold_working" to thresholdWorking
                )
            }

        } finally {
            yoloDetector.close()
        }
    }

    private suspend fun runAccuracyTests() {
        Log.i(TAG, "Ejecutando pruebas de precisión...")

        val yoloDetector = YoloDetector(context, "yolov8n.tflite", 0.25f, 0.45f)

        try {
            // Test: Precisión con objetos conocidos
            runTest("known_objects_accuracy") {
                val knownObjects = createKnownObjectScenes()
                var totalPrecision = 0f
                var totalRecall = 0f
                var scenesProcessed = 0

                for ((scene, groundTruth) in knownObjects) {
                    val detections = yoloDetector.detect(scene)
                    val metrics = calculateAccuracyMetrics(detections, groundTruth)

                    totalPrecision += metrics.precision
                    totalRecall += metrics.recall
                    scenesProcessed++
                }

                mapOf(
                    "avg_precision" to (totalPrecision / scenesProcessed),
                    "avg_recall" to (totalRecall / scenesProcessed),
                    "scenes_tested" to scenesProcessed
                )
            }

        } finally {
            yoloDetector.close()
        }
    }

    private suspend fun runPerformanceTests() {
        Log.i(TAG, "Ejecutando pruebas de rendimiento...")

        val yoloDetector = YoloDetector(context, "yolov8n.tflite", 0.25f, 0.45f)

        try {
            // Test: Velocidad de inferencia
            runTest("inference_speed") {
                val testImage = TestUtils.createSyntheticTestImage(640, 640)
                val iterations = 10
                val times = mutableListOf<Long>()

                // Warm-up
                repeat(3) { yoloDetector.detect(testImage) }

                // Mediciones
                repeat(iterations) {
                    val startTime = System.currentTimeMillis()
                    yoloDetector.detect(testImage)
                    val endTime = System.currentTimeMillis()
                    times.add(endTime - startTime)
                }

                mapOf(
                    "avg_inference_time_ms" to times.average(),
                    "min_inference_time_ms" to times.minOrNull(),
                    "max_inference_time_ms" to times.maxOrNull(),
                    "std_deviation" to calculateStandardDeviation(times)
                ) as Map<String, Any>
            }

            // Test: Uso de memoria
            runTest("memory_usage") {
                val runtime = Runtime.getRuntime()
                val testImage = TestUtils.createSyntheticTestImage(640, 640)

                System.gc()
                val memoryBefore = runtime.totalMemory() - runtime.freeMemory()

                repeat(20) {
                    yoloDetector.detect(testImage)
                }

                System.gc()
                val memoryAfter = runtime.totalMemory() - runtime.freeMemory()

                mapOf(
                    "memory_before_mb" to (memoryBefore / (1024 * 1024)),
                    "memory_after_mb" to (memoryAfter / (1024 * 1024)),
                    "memory_increase_mb" to ((memoryAfter - memoryBefore) / (1024 * 1024))
                )
            }

        } finally {
            yoloDetector.close()
        }
    }

    private suspend fun runSpeedAnalysisTests() {
        Log.i(TAG, "Ejecutando pruebas de análisis de velocidad...")

        // Test: Cálculo de velocidad
        runTest("speed_calculation_accuracy") {
            val objectAnalyzer = com.example.tic_a.detection.ObjectAnalyzer(
                android.util.Size(1080, 1920), null
            )

            try {
                // Simular movimiento conocido
                val positions = listOf(
                    RectF(100f, 100f, 120f, 120f),  // t=0
                    RectF(200f, 100f, 220f, 120f),  // t=1000ms, +100px
                    RectF(300f, 100f, 320f, 120f)   // t=2000ms, +100px
                )

                val timestamps = listOf(0L, 1000L, 2000L)
                var calculatedSpeeds = mutableListOf<Float>()

                for (i in positions.indices) {
                    val obj = DetectedObject(1, "car", 0.9f, positions[i])
                    val analyzed = objectAnalyzer.analyzeObjects(listOf(obj), timestamps[i])

                    if (i > 0) {
                        calculatedSpeeds.add(analyzed.first().speed)
                    }
                }

                // Velocidad esperada: 100 píxeles por segundo
                val expectedSpeed = 100f
                val avgCalculatedSpeed = calculatedSpeeds.average().toFloat()
                val speedAccuracy = 1f - (kotlin.math.abs(avgCalculatedSpeed - expectedSpeed) / expectedSpeed)

                mapOf(
                    "expected_speed" to expectedSpeed,
                    "calculated_speeds" to calculatedSpeeds,
                    "avg_calculated_speed" to avgCalculatedSpeed,
                    "speed_accuracy" to speedAccuracy
                )

            } finally {
                objectAnalyzer.release()
            }
        }
    }

    private suspend fun runStressTests() {
        Log.i(TAG, "Ejecutando pruebas de estrés...")

        // Test: Detección continua
        runTest("continuous_detection_stress") {
            val yoloDetector = YoloDetector(context, "yolov8n.tflite", 0.25f, 0.45f)

            try {
                val testImage = TestUtils.createSyntheticTestImage(640, 640)
                val iterations = 100
                var successfulDetections = 0
                var totalTime = 0L

                val startTime = System.currentTimeMillis()

                repeat(iterations) {
                    try {
                        val detections = yoloDetector.detect(testImage)
                        successfulDetections++
                    } catch (e: Exception) {
                        Log.w(TAG, "Error en detección de estrés: ${e.message}")
                    }
                }

                totalTime = System.currentTimeMillis() - startTime

                mapOf(
                    "total_iterations" to iterations,
                    "successful_detections" to successfulDetections,
                    "success_rate" to (successfulDetections.toFloat() / iterations),
                    "total_time_ms" to totalTime,
                    "avg_time_per_detection" to (totalTime.toFloat() / iterations)
                )

            } finally {
                yoloDetector.close()
            }
        }
    }

    private suspend fun runTest(
        testName: String,
        testFunction: suspend () -> Map<String, Any>
    ) {
        Log.d(TAG, "Ejecutando test: $testName")

        val startTime = System.currentTimeMillis()

        try {
            val metrics = testFunction()
            val executionTime = System.currentTimeMillis() - startTime

            testResults.add(
                TestResult(
                    testName = testName,
                    passed = true,
                    executionTimeMs = executionTime,
                    metrics = metrics
                )
            )

            Log.i(TAG, "Test $testName PASÓ en ${executionTime}ms")

        } catch (e: Exception) {
            val executionTime = System.currentTimeMillis() - startTime

            testResults.add(
                TestResult(
                    testName = testName,
                    passed = false,
                    executionTimeMs = executionTime,
                    errorMessage = e.message
                )
            )

            Log.e(TAG, "Test $testName FALLÓ en ${executionTime}ms: ${e.message}")
        }
    }

    private fun createKnownObjectScenes(): List<Pair<Bitmap, List<GroundTruthObject>>> {
        return listOf(
            // Escena 1: Una persona
            TestUtils.createSyntheticTestImage(
                640, 640,
                listOf(SyntheticObject(SyntheticObjectType.PERSON_SHAPE, RectF(200f, 100f, 300f, 400f)))
            ) to listOf(
                GroundTruthObject("person", RectF(200f, 100f, 300f, 400f))
            ),

            // Escena 2: Un carro
            TestUtils.createSyntheticTestImage(
                640, 640,
                listOf(SyntheticObject(SyntheticObjectType.CAR_SHAPE, RectF(100f, 200f, 400f, 350f)))
            ) to listOf(
                GroundTruthObject("car", RectF(100f, 200f, 400f, 350f))
            ),

            // Escena 3: Múltiples objetos
            TestUtils.createSyntheticTestImage(
                640, 640,
                listOf(
                    SyntheticObject(SyntheticObjectType.PERSON_SHAPE, RectF(50f, 50f, 150f, 300f)),
                    SyntheticObject(SyntheticObjectType.CAR_SHAPE, RectF(300f, 200f, 500f, 350f))
                )
            ) to listOf(
                GroundTruthObject("person", RectF(50f, 50f, 150f, 300f)),
                GroundTruthObject("car", RectF(300f, 200f, 500f, 350f))
            )
        )
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

    private fun calculateStandardDeviation(values: List<Long>): Double {
        val mean = values.average()
        val variance = values.map { (it - mean) * (it - mean) }.average()
        return kotlin.math.sqrt(variance)
    }

    private fun saveReport(report: String) {
        try {
            val reportFile = File(outputDir, "test_report.json")
            val writer = FileWriter(reportFile)
            writer.write(report)
            writer.close()

            Log.i(TAG, "Reporte guardado en: ${reportFile.absolutePath}")
        } catch (e: Exception) {
            Log.e(TAG, "Error guardando reporte: ${e.message}")
        }
    }

    companion object {
        private const val TAG = "AutomatedTestRunner"
    }
}

// Clases de datos adicionales
data class GroundTruthObject(
    val classLabel: String,
    val boundingBox: RectF,
    val confidence: Float = 1.0f
)

data class AccuracyMetrics(
    val precision: Float,
    val recall: Float,
    val f1Score: Float,
    val truePositives: Int,
    val falsePositives: Int,
    val falseNegatives: Int
)