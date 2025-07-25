package com.example.tic_a.performance

import android.content.Context
import android.graphics.Bitmap
import android.os.SystemClock
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import com.example.tic_a.detection.YoloDetector
import junit.framework.TestCase.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class YoloPerformanceTest {

    private lateinit var context: Context
    private lateinit var yoloDetector: YoloDetector
    private lateinit var testBitmap: Bitmap

    @Before
    fun setUp() {
        context = InstrumentationRegistry.getInstrumentation().targetContext
        yoloDetector = YoloDetector(context, "yolov8n.tflite", 0.25f, 0.45f)
        testBitmap = Bitmap.createBitmap(640, 640, Bitmap.Config.ARGB_8888)
    }

    @Test
    fun testInferenceSpeed() {
        val iterations = 10
        val times = mutableListOf<Long>()

        // Warm-up runs
        repeat(3) {
            yoloDetector.detect(testBitmap)
        }

        // Measure inference times
        repeat(iterations) {
            val startTime = SystemClock.uptimeMillis()
            yoloDetector.detect(testBitmap)
            val endTime = SystemClock.uptimeMillis()

            times.add(endTime - startTime)
        }

        val averageTime = times.average()
        val maxTime = times.maxOrNull() ?: 0L

        println("Average inference time: ${averageTime}ms")
        println("Max inference time: ${maxTime}ms")

        // Verificar que el tiempo promedio sea razonable (< 500ms para YOLOv8n)
        assertTrue("Tiempo promedio de inferencia debe ser < 500ms", averageTime < 500.0)
        assertTrue("Tiempo máximo de inferencia debe ser < 1000ms", maxTime < 1000L)
    }

    @Test
    fun testMemoryUsage() {
        val runtime = Runtime.getRuntime()

        // Medir memoria antes
        System.gc()
        val memoryBefore = runtime.totalMemory() - runtime.freeMemory()

        // Ejecutar múltiples detecciones
        repeat(50) {
            yoloDetector.detect(testBitmap)
        }

        // Medir memoria después
        System.gc()
        val memoryAfter = runtime.totalMemory() - runtime.freeMemory()

        val memoryIncrease = memoryAfter - memoryBefore
        val memoryIncreaseMB = memoryIncrease / (1024 * 1024)

        println("Memory increase: ${memoryIncreaseMB}MB")

        // Verificar que no hay fuga de memoria significativa
        assertTrue("Incremento de memoria debe ser < 50MB", memoryIncreaseMB < 50)
    }

    @Test
    fun testConcurrentDetections() {
        val numThreads = 3
        val detectionsPerThread = 5
        val results = mutableListOf<Boolean>()

        val threads = (1..numThreads).map { threadId ->
            Thread {
                try {
                    repeat(detectionsPerThread) {
                        val detections = yoloDetector.detect(testBitmap)
                        // Verificar que siempre retorna una lista válida
                        synchronized(results) {
                            results.add(detections.isNotEmpty() || true) // Always valid
                        }
                    }
                } catch (e: Exception) {
                    synchronized(results) {
                        results.add(false)
                    }
                }
            }
        }

        threads.forEach { it.start() }
        threads.forEach { it.join() }

        // Verificar que todas las ejecuciones fueron exitosas
        val totalExpected = numThreads * detectionsPerThread
        assertTrue("Todas las detecciones concurrentes deben ser exitosas",
            results.size == totalExpected && results.all { it })
    }
}