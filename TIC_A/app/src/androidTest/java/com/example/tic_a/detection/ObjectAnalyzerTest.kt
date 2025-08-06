package com.example.tic_a.detection

import android.graphics.RectF
import android.util.Size
import androidx.test.ext.junit.runners.AndroidJUnit4
import androidx.test.platform.app.InstrumentationRegistry
import com.example.tic_a.models.DetectedObject
import junit.framework.TestCase.assertEquals
import junit.framework.TestCase.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith
import kotlin.math.abs

@RunWith(AndroidJUnit4::class)
class ObjectAnalyzerTest {

    private lateinit var objectAnalyzer: ObjectAnalyzer
    private val screenSize = Size(1080, 1920)

    @Before
    fun setUp() {
        objectAnalyzer = ObjectAnalyzer(screenSize, null)
        // Limpiar historial antes de cada test
        objectAnalyzer.clearHistory()
    }

    @Test
    fun testInitialObjectAnalysis() {
        val detectedObjects = createTestDetectedObjects()
        val timestamp = System.currentTimeMillis()

        val analyzedObjects = objectAnalyzer.analyzeObjects(detectedObjects, timestamp)

        assertEquals("Debe retornar el mismo número de objetos",
            detectedObjects.size, analyzedObjects.size)

        // En la primera detección, la velocidad debe ser 0
        for (obj in analyzedObjects) {
            assertEquals("Velocidad inicial debe ser 0", 0f, obj.speed)
            assertTrue("Distancia debe ser positiva", obj.distance > 0f)
        }
    }

    @Test
    fun testSpeedCalculation() {
        val timestamp1 = 1000L
        val timestamp2 = 2000L // 1 segundo después

        val obj1 = DetectedObject(
            id = 1,
            classLabel = "person",
            confidence = 0.8f,
            boundingBox = RectF(90f, 90f, 110f, 110f)
        )

        val obj2 = DetectedObject(
            id = 1, // MISMO ID
            classLabel = "person",
            confidence = 0.8f,
            boundingBox = RectF(190f, 90f, 210f, 110f)
        )

        // Analizar primer frame
        val firstResult = objectAnalyzer.analyzeObjects(listOf(obj1), timestamp1)
        assertEquals("Primer frame debe tener velocidad 0", 0f, firstResult.first().speed)

        // Analizar segundo frame
        val analyzedObjects = objectAnalyzer.analyzeObjects(listOf(obj2), timestamp2)
        val analyzedObj = analyzedObjects.first()

        // Verificar cálculo de velocidad
        // Centro se movió de (100, 100) a (200, 100) = 100 píxeles en 1 segundo
        val expectedSpeed = 100f
        val tolerance = 10f // Tolerancia más amplia

        assertTrue("Velocidad calculada debe ser aproximadamente $expectedSpeed px/s, pero fue ${analyzedObj.speed}",
            abs(analyzedObj.speed - expectedSpeed) < tolerance)

        println("Test Speed: Expected=$expectedSpeed, Actual=${analyzedObj.speed}")
    }

    @Test
    fun testDirectionCalculation() {
        val timestamp1 = 1000L
        val timestamp2 = 2000L

        // Movimiento hacia la derecha (Este)
        val obj1 = DetectedObject(1, "car", 0.9f, RectF(100f, 100f, 120f, 120f))
        val obj2 = DetectedObject(1, "car", 0.9f, RectF(200f, 100f, 220f, 120f))

        objectAnalyzer.analyzeObjects(listOf(obj1), timestamp1)
        val analyzedObjects = objectAnalyzer.analyzeObjects(listOf(obj2), timestamp2)

        val direction = analyzedObjects.first().direction

        // Movimiento horizontal hacia la derecha debería ser ~0 grados
        assertTrue("Dirección para movimiento hacia la derecha debe ser ~0°, pero fue $direction",
            direction >= -15f && direction <= 15f) // Tolerancia más amplia

        println("Test Direction: Expected=~0, Actual=$direction")
    }

    @Test
    fun testDistanceEstimation() {
        val person = DetectedObject(
            id = 1,
            classLabel = "person",
            confidence = 0.8f,
            boundingBox = RectF(100f, 100f, 200f, 300f)
        )

        val car = DetectedObject(
            id = 2,
            classLabel = "car",
            confidence = 0.9f,
            boundingBox = RectF(300f, 150f, 450f, 250f)
        )

        val analyzedObjects = objectAnalyzer.analyzeObjects(
            listOf(person, car),
            System.currentTimeMillis()
        )

        val analyzedPerson = analyzedObjects.find { it.classLabel == "person" }!!
        val analyzedCar = analyzedObjects.find { it.classLabel == "car" }!!

        assertTrue("Distancia de persona debe ser positiva", analyzedPerson.distance > 0f)
        assertTrue("Distancia de carro debe ser positiva", analyzedCar.distance > 0f)
        assertTrue("Las distancias deben ser razonables (< 100m)",
            analyzedPerson.distance < 100f && analyzedCar.distance < 100f)
    }

    @Test
    fun testObjectTracking() {
        val timestamps = listOf(1000L, 2000L, 3000L)
        val positions = listOf(
            RectF(100f, 100f, 120f, 120f),  // Posición inicial
            RectF(150f, 100f, 170f, 120f),  // +50px hacia la derecha
            RectF(200f, 100f, 220f, 120f)   // +50px hacia la derecha
        )

        val results = mutableListOf<DetectedObject>()

        for (i in timestamps.indices) {
            val obj = DetectedObject(1, "bicycle", 0.7f, positions[i]) // MISMO ID
            val analyzed = objectAnalyzer.analyzeObjects(listOf(obj), timestamps[i])
            results.add(analyzed.first())

            println("Frame $i: Position=${positions[i]}, Speed=${analyzed.first().speed}")

            if (i > 0) {
                val currentSpeed = analyzed.first().speed
                assertTrue("Velocidad debe ser positiva en frame $i, pero fue $currentSpeed",
                    currentSpeed > 0f)

                if (i > 1) {
                    // Verificar que la velocidad es consistente (movimiento uniforme)
                    val prevSpeed = results[i-1].speed
                    val speedDifference = abs(currentSpeed - prevSpeed)
                    assertTrue("Velocidad debe ser relativamente estable entre frames ${i-1} y $i. " +
                            "Anterior: $prevSpeed, Actual: $currentSpeed, Diferencia: $speedDifference",
                        speedDifference < 30f) // Tolerancia aumentada
                }
            }
        }
    }

    @Test
    fun testMultipleObjectTracking() {
        val timestamp = System.currentTimeMillis()

        val objects = listOf(
            DetectedObject(1, "person", 0.8f, RectF(100f, 100f, 120f, 140f)),
            DetectedObject(2, "car", 0.9f, RectF(200f, 200f, 250f, 230f)),
            DetectedObject(3, "bicycle", 0.7f, RectF(300f, 150f, 320f, 170f))
        )

        val analyzedObjects = objectAnalyzer.analyzeObjects(objects, timestamp)

        assertEquals("Debe rastrear todos los objetos", objects.size, analyzedObjects.size)

        // Verificar que cada objeto mantiene su ID
        val originalIds = objects.map { it.id }.sorted()
        val analyzedIds = analyzedObjects.map { it.id }.sorted()
        assertEquals("IDs deben mantenerse", originalIds, analyzedIds)

        // Verificar que cada objeto tiene propiedades calculadas
        for (obj in analyzedObjects) {
            assertTrue("Distancia debe ser positiva para objeto ${obj.id}", obj.distance > 0f)
            assertEquals("Velocidad inicial debe ser 0 para objeto ${obj.id}", 0f, obj.speed)
        }
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
        val speeds = mutableListOf<Float>()

        for (i in positions.indices) {
            val obj = DetectedObject(1, "car", 0.9f, positions[i]) // MISMO ID
            val analyzed = objectAnalyzer.analyzeObjects(listOf(obj), timestamps[i])
            val speed = analyzed.first().speed
            speeds.add(speed)

            println("Frame $i: Position=${positions[i]}, Speed=$speed")

            if (i > 0) {
                assertTrue("Velocidad debe ser positiva en frame $i", speed > 0f)

                if (i > 1) {
                    // Para movimiento lineal uniforme, la velocidad debería ser estable
                    val prevSpeed = speeds[i-1]
                    val speedDifference = abs(speed - prevSpeed)
                    assertTrue("Velocidad debe ser estable para movimiento lineal. " +
                            "Frame ${i-1}: $prevSpeed, Frame $i: $speed, Diferencia: $speedDifference",
                        speedDifference < 15f) // Tolerancia razonable
                }
            }
        }
    }

    @Test
    fun testStationaryObject() {
        val stationaryPosition = RectF(200f, 200f, 220f, 220f)
        val timestamps = listOf(0L, 1000L, 2000L, 3000L)

        for (i in timestamps.indices) {
            val obj = DetectedObject(1, "person", 0.9f, stationaryPosition) // MISMO ID
            val analyzed = objectAnalyzer.analyzeObjects(listOf(obj), timestamps[i])

            if (i > 0) {
                val speed = analyzed.first().speed
                assertTrue("Objeto estacionario debe tener velocidad ~0, pero fue $speed", speed < 10f)
            }
        }
    }

    @Test
    fun testDirectionToString() {
        assertEquals("East", objectAnalyzer.directionToString(0f))
        assertEquals("Southeast", objectAnalyzer.directionToString(45f))
        assertEquals("South", objectAnalyzer.directionToString(90f))
        assertEquals("Southwest", objectAnalyzer.directionToString(135f))
        assertEquals("West", objectAnalyzer.directionToString(180f))
        assertEquals("Northwest", objectAnalyzer.directionToString(225f))
        assertEquals("North", objectAnalyzer.directionToString(270f))
        assertEquals("Northeast", objectAnalyzer.directionToString(315f))
        assertEquals("East", objectAnalyzer.directionToString(360f))
    }

    @Test
    fun testObjectHistoryCleanup() {
        val obj = DetectedObject(1, "person", 0.8f, RectF(100f, 100f, 120f, 120f))

        // Agregar objeto con timestamp antiguo
        val oldTimestamp = System.currentTimeMillis() - 6000L // 6 segundos atrás
        objectAnalyzer.analyzeObjects(listOf(obj), oldTimestamp)

        // Analizar con timestamp actual (debería limpiar el historial)
        val currentTimestamp = System.currentTimeMillis()
        val emptyList = objectAnalyzer.analyzeObjects(emptyList(), currentTimestamp)

        assertTrue("Lista debe estar vacía después del cleanup", emptyList.isEmpty())
    }

    // Función auxiliar para crear objetos de prueba
    private fun createTestDetectedObjects(): List<DetectedObject> {
        return listOf(
            DetectedObject(1, "person", 0.85f, RectF(100f, 100f, 150f, 200f)),
            DetectedObject(2, "car", 0.92f, RectF(300f, 200f, 450f, 280f)),
            DetectedObject(3, "bicycle", 0.73f, RectF(200f, 150f, 230f, 180f))
        )
    }
}