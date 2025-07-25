// SpeedAnalysisTest.kt - VERSIÓN CORREGIDA SIN MOCKITO
package com.example.tic_a.speed

import android.graphics.RectF
import android.util.Size
import androidx.test.ext.junit.runners.AndroidJUnit4
import com.example.tic_a.detection.ObjectAnalyzer
import com.example.tic_a.models.DetectedObject
import junit.framework.TestCase.assertEquals
import junit.framework.TestCase.assertTrue
import org.junit.After
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import kotlin.math.abs

@RunWith(AndroidJUnit4::class)
class SpeedAnalysisTest {

    private lateinit var objectAnalyzer: ObjectAnalyzer
    private val screenSize = Size(1080, 1920)

    @Before
    fun setUp() {
        // SOLUCIÓN: Pasar null en lugar de usar mock para SensorManager
        objectAnalyzer = ObjectAnalyzer(screenSize, null)
    }

    @After
    fun tearDown() {
        objectAnalyzer.release()
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
                    assertTrue("Velocidad debe ser estable para movimiento lineal (diff: $speedDifference)",
                        speedDifference < 10f)

                    // Verificar que está cerca del valor esperado
                    val expectedDiff = abs(currentSpeed - expectedSpeed)
                    assertTrue("Velocidad debe estar cerca de $expectedSpeed px/s (actual: $currentSpeed)",
                        expectedDiff < 15f) // Tolerancia de 15 px/s
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
                val speed = analyzed.first().speed
                speeds.add(speed)

                println("Frame $i: Speed = $speed px/s") // Debug info
            }
        }

        // Verificar que hay al menos 2 mediciones de velocidad
        assertTrue("Debe haber al menos 2 mediciones de velocidad", speeds.size >= 2)

        // Verificar tendencia de aceleración (velocidad generalmente aumenta)
        // No todos los frames necesitan ser más rápidos, pero la tendencia general sí
        val firstSpeed = speeds.first()
        val lastSpeed = speeds.last()

        assertTrue("La velocidad final debe ser mayor que la inicial (aceleración)",
            lastSpeed >= firstSpeed)
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
                println("Stationary object frame $i: Speed = $speed px/s") // Debug info

                assertTrue("Objeto estacionario debe tener velocidad ~0 (actual: $speed)",
                    speed < 5f)
            }
        }
    }

    @Test
    fun testCircularMovement() {
        // Simular movimiento circular básico
        val radius = 50f
        val centerX = 200f
        val centerY = 200f

        val positions = listOf(
            RectF(centerX + radius, centerY, centerX + radius + 20f, centerY + 20f),  // 0° (Este)
            RectF(centerX, centerY + radius, centerX + 20f, centerY + radius + 20f),  // 90° (Sur)
            RectF(centerX - radius, centerY, centerX - radius + 20f, centerY + 20f),  // 180° (Oeste)
            RectF(centerX, centerY - radius, centerX + 20f, centerY - radius + 20f)   // 270° (Norte)
        )

        val timestamps = listOf(0L, 1000L, 2000L, 3000L)
        val directions = mutableListOf<Float>()
        val speeds = mutableListOf<Float>()

        for (i in positions.indices) {
            val obj = DetectedObject(1, "bicycle", 0.8f, positions[i])
            val analyzed = objectAnalyzer.analyzeObjects(listOf(obj), timestamps[i])

            if (i > 0) {
                val speed = analyzed.first().speed
                val direction = analyzed.first().direction

                speeds.add(speed)
                directions.add(direction)

                println("Circular movement frame $i: Speed = $speed px/s, Direction = $direction°")
            }
        }

        // Verificar que hay movimiento
        assertTrue("Debe haber movimiento detectado", speeds.all { it > 0f })

        // Verificar que las direcciones cambian (indicativo de movimiento circular)
        val uniqueDirections = directions.toSet()
        assertTrue("Debe haber cambios de dirección en movimiento circular",
            uniqueDirections.size > 1)

        // Las velocidades deberían ser relativamente consistentes en movimiento circular uniforme
        if (speeds.size >= 2) {
            val avgSpeed = speeds.average().toFloat()
            val speedVariations = speeds.map { abs(it - avgSpeed) }
            val maxVariation = speedVariations.maxOrNull() ?: 0f

            // Permitir algo de variación pero no demasiada
            assertTrue("Velocidades en movimiento circular deben ser relativamente consistentes",
                maxVariation < avgSpeed * 0.5f) // Max 50% de variación
        }
    }

    @Test
    fun testMultipleObjectSpeeds() {
        val timestamp1 = 1000L
        val timestamp2 = 2000L

        // Diferentes objetos con diferentes velocidades esperadas
        val objects1 = listOf(
            DetectedObject(1, "car", 0.9f, RectF(100f, 100f, 150f, 130f)),      // Se moverá rápido
            DetectedObject(2, "person", 0.8f, RectF(200f, 200f, 220f, 250f)),   // Se moverá lento
            DetectedObject(3, "bicycle", 0.7f, RectF(300f, 150f, 330f, 180f))   // Velocidad media
        )

        val objects2 = listOf(
            DetectedObject(1, "car", 0.9f, RectF(200f, 100f, 250f, 130f)),      // +100px en X
            DetectedObject(2, "person", 0.8f, RectF(210f, 200f, 230f, 250f)),   // +10px en X
            DetectedObject(3, "bicycle", 0.7f, RectF(350f, 150f, 380f, 180f))   // +50px en X
        )

        // Analizar primer frame
        objectAnalyzer.analyzeObjects(objects1, timestamp1)

        // Analizar segundo frame
        val analyzed = objectAnalyzer.analyzeObjects(objects2, timestamp2)

        val carSpeed = analyzed.find { it.classLabel == "car" }?.speed ?: 0f
        val personSpeed = analyzed.find { it.classLabel == "person" }?.speed ?: 0f
        val bicycleSpeed = analyzed.find { it.classLabel == "bicycle" }?.speed ?: 0f

        println("Multi-object speeds - Car: $carSpeed, Person: $personSpeed, Bicycle: $bicycleSpeed")

        // Verificar que se calcularon velocidades
        assertTrue("Velocidad del carro debe ser > 0", carSpeed > 0f)
        assertTrue("Velocidad de la persona debe ser > 0", personSpeed > 0f)
        assertTrue("Velocidad de la bicicleta debe ser > 0", bicycleSpeed > 0f)

        // Verificar orden de velocidades basado en el movimiento: car > bicycle > person
        assertTrue("Carro debe ser más rápido que bicicleta", carSpeed > bicycleSpeed)
        assertTrue("Bicicleta debe ser más rápida que persona", bicycleSpeed > personSpeed)

        // Verificar valores aproximados esperados (100px/s, 50px/s, 10px/s respectivamente)
        assertTrue("Velocidad del carro debe estar cerca de 100 px/s", abs(carSpeed - 100f) < 20f)
        assertTrue("Velocidad de la bicicleta debe estar cerca de 50 px/s", abs(bicycleSpeed - 50f) < 20f)
        assertTrue("Velocidad de la persona debe estar cerca de 10 px/s", abs(personSpeed - 10f) < 15f)
    }

    @Test
    fun testDirectionCalculationAccuracy() {
        val testCases = listOf(
            // (start, end, expectedDirection, tolerance, description)
            Triple(
                Pair(RectF(100f, 100f, 120f, 120f), RectF(200f, 100f, 220f, 120f)),
                Pair(0f, 15f),
                "Este (horizontal derecha)"
            ),
            Triple(
                Pair(RectF(100f, 100f, 120f, 120f), RectF(100f, 200f, 120f, 220f)),
                Pair(90f, 15f),
                "Sur (vertical abajo)"
            ),
            Triple(
                Pair(RectF(200f, 100f, 220f, 120f), RectF(100f, 100f, 120f, 120f)),
                Pair(180f, 15f),
                "Oeste (horizontal izquierda)"
            ),
            Triple(
                Pair(RectF(100f, 200f, 120f, 220f), RectF(100f, 100f, 120f, 120f)),
                Pair(270f, 15f),
                "Norte (vertical arriba)"
            )
        )

        for ((positions, expected, description) in testCases) {
            val (startPos, endPos) = positions
            val (expectedDir, tolerance) = expected

            val obj1 = DetectedObject(1, "test", 0.8f, startPos)
            val obj2 = DetectedObject(1, "test", 0.8f, endPos)

            // Analizar movimiento
            objectAnalyzer.analyzeObjects(listOf(obj1), 1000L)
            val analyzed = objectAnalyzer.analyzeObjects(listOf(obj2), 2000L)

            val actualDirection = analyzed.first().direction

            println("$description - Expected: $expectedDir°, Actual: $actualDirection°")

            // Manejar el caso especial del ángulo 0/360
            val directionDiff = minOf(
                abs(actualDirection - expectedDir),
                abs(actualDirection - expectedDir + 360f),
                abs(actualDirection - expectedDir - 360f)
            )

            assertTrue("Dirección calculada debe estar cerca de la esperada para $description. " +
                    "Expected: $expectedDir°, Actual: $actualDirection°, Diff: $directionDiff°",
                directionDiff < tolerance)
        }
    }

    @Test
    fun testSpeedConsistencyOverTime() {
        // Probar que la velocidad se mantiene consistente en movimiento uniforme
        val baseX = 100f
        val baseY = 100f
        val stepSize = 60f // 60 píxeles por segundo

        val positions = (0..5).map { i ->
            val x = baseX + (i * stepSize)
            RectF(x, baseY, x + 20f, baseY + 20f)
        }

        val timestamps = (0..5).map { i -> i * 1000L } // Cada segundo

        val speeds = mutableListOf<Float>()

        for (i in positions.indices) {
            val obj = DetectedObject(1, "consistent_mover", 0.9f, positions[i])
            val analyzed = objectAnalyzer.analyzeObjects(listOf(obj), timestamps[i])

            if (i > 0) {
                val speed = analyzed.first().speed
                speeds.add(speed)
                println("Consistency test frame $i: Speed = $speed px/s")
            }
        }

        // Verificar que tenemos suficientes mediciones
        assertTrue("Debe haber al menos 3 mediciones de velocidad", speeds.size >= 3)

        // Calcular estadísticas de consistencia
        val avgSpeed = speeds.average().toFloat()
        val speedDeviations = speeds.map { abs(it - avgSpeed) }
        val maxDeviation = speedDeviations.maxOrNull() ?: 0f
        val avgDeviation = speedDeviations.average().toFloat()

        println("Speed consistency stats - Avg: $avgSpeed, Max deviation: $maxDeviation, Avg deviation: $avgDeviation")

        // La velocidad promedio debe estar cerca del valor esperado (60 px/s)
        assertTrue("Velocidad promedio debe estar cerca de 60 px/s", abs(avgSpeed - 60f) < 15f)

        // La desviación máxima no debe ser excesiva
        assertTrue("Desviación máxima debe ser razonable", maxDeviation < 20f)

        // La desviación promedio debe ser pequeña para movimiento consistente
        assertTrue("Desviación promedio debe ser pequeña", avgDeviation < 10f)
    }

    @Test
    fun testZeroSpeedDetection() {
        // Verificar que objetos que no se mueven tienen velocidad 0
        val position = RectF(150f, 150f, 170f, 170f)
        val timestamps = listOf(1000L, 2000L, 3000L, 4000L)

        for (i in timestamps.indices) {
            val obj = DetectedObject(1, "static", 0.9f, position) // Misma posición
            val analyzed = objectAnalyzer.analyzeObjects(listOf(obj), timestamps[i])

            if (i > 0) {
                val speed = analyzed.first().speed
                println("Zero speed test frame $i: Speed = $speed px/s")

                assertTrue("Objeto estático debe tener velocidad cerca de 0", speed < 2f)
            }
        }
    }

    @Test
    fun testHighSpeedDetection() {
        // Probar detección de alta velocidad
        val startPos = RectF(0f, 100f, 20f, 120f)
        val endPos = RectF(500f, 100f, 520f, 120f) // 500 píxeles en 1 segundo

        val obj1 = DetectedObject(1, "fast_object", 0.9f, startPos)
        val obj2 = DetectedObject(1, "fast_object", 0.9f, endPos)

        objectAnalyzer.analyzeObjects(listOf(obj1), 1000L)
        val analyzed = objectAnalyzer.analyzeObjects(listOf(obj2), 2000L)

        val speed = analyzed.first().speed

        println("High speed test: Speed = $speed px/s")

        // Verificar que se detecta alta velocidad correctamente
        assertTrue("Debe detectar alta velocidad correctamente", speed > 400f)
        assertTrue("Velocidad no debe ser excesivamente alta", speed < 600f)
    }
}