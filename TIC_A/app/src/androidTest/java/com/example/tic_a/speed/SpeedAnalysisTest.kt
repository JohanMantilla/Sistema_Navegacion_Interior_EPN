package com.example.tic_a.speed

import android.graphics.RectF
import android.hardware.SensorManager
import android.util.Size
import androidx.test.ext.junit.runners.AndroidJUnit4
import com.example.tic_a.detection.ObjectAnalyzer
import com.example.tic_a.models.DetectedObject
import junit.framework.TestCase.assertEquals
import junit.framework.TestCase.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import org.mockito.Mock
import org.mockito.MockitoAnnotations
import kotlin.math.abs

@RunWith(AndroidJUnit4::class)
class SpeedAnalysisTest {

    @Mock
    private lateinit var mockSensorManager: SensorManager

    private lateinit var objectAnalyzer: ObjectAnalyzer
    private val screenSize = Size(1080, 1920)

    @Before
    fun setUp() {
        MockitoAnnotations.openMocks(this)
        objectAnalyzer = ObjectAnalyzer(screenSize, mockSensorManager)
    }

    @Test
    fun testLinearMovement() {
        val positions = listOf(
            RectF(100f, 100f, 120f, 120f),  // t=0
            RectF(150f, 100f, 170f, 120f),  // t=1000ms, +50px horizontal
            RectF(200f, 100f, 220f, 120f),  // t=2000ms, +50px horizontal
            RectF(250f, 100f, 270f, 120f)   // t=3000ms, +50px horizontal
        )

        val timestamps = listOf(0L, 1000L, 2000L, 3000L)
        val expectedSpeed = 50f // 50 píxeles por segundo

        var previousSpeed = 0f

        for (i in positions.indices) {
            val obj = DetectedObject(1, "car", 0.9f, positions[i])
            val analyzed = objectAnalyzer.analyzeObjects(listOf(obj), timestamps[i])

            if (i > 0) {
                val currentSpeed = analyzed.first().speed

                // Verificar que la velocidad es consistente
                assertTrue("Velocidad debe ser positiva", currentSpeed > 0f)

                if (i > 1) {
                    // La velocidad debería ser estable para movimiento lineal
                    val speedDifference = abs(currentSpeed - previousSpeed)
                    assertTrue("Velocidad debe ser estable para movimiento lineal",
                        speedDifference < 10f)
                }

                previousSpeed = currentSpeed
            }
        }
    }

    @Test
    fun testAccelerationDetection() {
        val positions = listOf(
            RectF(100f, 100f, 120f, 120f),  // t=0
            RectF(110f, 100f, 130f, 120f),  // t=1000ms, +10px (slow)
            RectF(130f, 100f, 150f, 120f),  // t=2000ms, +20px (faster)
            RectF(160f, 100f, 180f, 120f)   // t=3000ms, +30px (even faster)
        )

        val timestamps = listOf(0L, 1000L, 2000L, 3000L)
        val speeds = mutableListOf<Float>()

        for (i in positions.indices) {
            val obj = DetectedObject(1, "bicycle", 0.8f, positions[i])
            val analyzed = objectAnalyzer.analyzeObjects(listOf(obj), timestamps[i])

            if (i > 0) {
                speeds.add(analyzed.first().speed)
            }
        }

        // Verificar que la velocidad aumenta (aceleración)
        for (i in 1 until speeds.size) {
            assertTrue("Velocidad debe aumentar (aceleración)",
                speeds[i] >= speeds[i-1])
        }
    }

    @Test
    fun testStationaryObject() {
        val stationaryPosition = RectF(200f, 200f, 220f, 220f)
        val timestamps = listOf(0L, 1000L, 2000L, 3000L)

        for (i in timestamps.indices) {
            val obj = DetectedObject(1, "person", 0.9f, stationaryPosition)
            val analyzed = objectAnalyzer.analyzeObjects(listOf(obj), timestamps[i])

            if (i > 0) {
                val speed = analyzed.first().speed
                assertTrue("Objeto estacionario debe tener velocidad ~0", speed < 5f)
            }
        }
    }

    @Test
    fun testCircularMovement() {
        // Simular movimiento circular
        val radius = 50f
        val centerX = 200f
        val centerY = 200f

        val positions = listOf(
            RectF(centerX + radius, centerY, centerX + radius + 20f, centerY + 20f),  // 0°
            RectF(centerX, centerY + radius, centerX + 20f, centerY + radius + 20f),  // 90°
            RectF(centerX - radius, centerY, centerX - radius + 20f, centerY + 20f),  // 180°
            RectF(centerX, centerY - radius, centerX + 20f, centerY - radius + 20f)   // 270°
        )

        val timestamps = listOf(0L, 1000L, 2000L, 3000L)
        val directions = mutableListOf<Float>()

        for (i in positions.indices) {
            val obj = DetectedObject(1, "bicycle", 0.8f, positions[i])
            val analyzed = objectAnalyzer.analyzeObjects(listOf(obj), timestamps[i])

            if (i > 0) {
                directions.add(analyzed.first().direction)
            }
        }

        // Verificar que las direcciones cambian apropiadamente
        assertTrue("Debe haber cambios de dirección", directions.toSet().size > 1)
    }

    @Test
    fun testMultipleObjectSpeeds() {
        val timestamp1 = 1000L
        val timestamp2 = 2000L

        // Diferentes objetos con diferentes velocidades
        val objects1 = listOf(
            DetectedObject(1, "car", 0.9f, RectF(100f, 100f, 150f, 130f)),      // Rápido
            DetectedObject(2, "person", 0.8f, RectF(200f, 200f, 220f, 250f)),   // Lento
            DetectedObject(3, "bicycle", 0.7f, RectF(300f, 150f, 330f, 180f))   // Medio
        )

        val objects2 = listOf(
            DetectedObject(1, "car", 0.9f, RectF(200f, 100f, 250f, 130f)),      // +100px
            DetectedObject(2, "person", 0.8f, RectF(210f, 200f, 230f, 250f)),   // +10px
            DetectedObject(3, "bicycle", 0.7f, RectF(350f, 150f, 380f, 180f))   // +50px
        )

        objectAnalyzer.analyzeObjects(objects1, timestamp1)
        val analyzed = objectAnalyzer.analyzeObjects(objects2, timestamp2)

        val carSpeed = analyzed.find { it.classLabel == "car" }?.speed ?: 0f
        val personSpeed = analyzed.find { it.classLabel == "person" }?.speed ?: 0f
        val bicycleSpeed = analyzed.find { it.classLabel == "bicycle" }?.speed ?: 0f

        // Verificar orden de velocidades: car > bicycle > person
        assertTrue("Carro debe ser más rápido que bicicleta", carSpeed > bicycleSpeed)
        assertTrue("Bicicleta debe ser más rápida que persona", bicycleSpeed > personSpeed)
    }

    @Test
    fun testDirectionCalculationAccuracy() {
        val testCases = listOf(
            // (start, end, expectedDirection, tolerance)
            Pair(RectF(100f, 100f, 120f, 120f), RectF(200f, 100f, 220f, 120f)) to Pair(0f, 10f),    // Este
            Pair(RectF(100f, 100f, 120f, 120f), RectF(100f, 200f, 120f, 220f)) to Pair(90f, 10f),   // Sur
            Pair(RectF(200f, 100f, 220f, 120f), RectF(100f, 100f, 120f, 120f)) to Pair(180f, 10f),  // Oeste
            Pair(RectF(100f, 200f, 120f, 220f), RectF(100f, 100f, 120f, 120f)) to Pair(270f, 10f)   // Norte
        )

        for ((positions, expected) in testCases) {
            val (startPos, endPos) = positions
            val (expectedDir, tolerance) = expected

            val obj1 = DetectedObject(1, "test", 0.8f, startPos)
            val obj2 = DetectedObject(1, "test", 0.8f, endPos)

            objectAnalyzer.analyzeObjects(listOf(obj1), 1000L)
            val analyzed = objectAnalyzer.analyzeObjects(listOf(obj2), 2000L)

            val actualDirection = analyzed.first().direction

            // Manejar el caso especial del ángulo 0/360
            val directionDiff = minOf(
                abs(actualDirection - expectedDir),
                abs(actualDirection - expectedDir + 360f),
                abs(actualDirection - expectedDir - 360f)
            )

            assertTrue("Dirección calculada debe estar cerca de la esperada",
                directionDiff < tolerance)
        }
    }
}