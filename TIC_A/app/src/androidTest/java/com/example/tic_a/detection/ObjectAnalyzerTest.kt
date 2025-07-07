// ObjectAnalyzerTest.kt - VERSIÓN CORREGIDA
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
        // SOLUCIÓN 1: Pasar null en lugar de usar mock
        // El ObjectAnalyzer debería manejar esto apropiadamente
        objectAnalyzer = ObjectAnalyzer(screenSize, null)
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

        // Primer frame: objeto en posición (100, 100)
        val obj1 = DetectedObject(
            id = 1,
            classLabel = "person",
            confidence = 0.8f,
            boundingBox = RectF(90f, 90f, 110f, 110f)
        )

        // Segundo frame: mismo objeto movido a (200, 100) - movimiento horizontal
        val obj2 = DetectedObject(
            id = 1,
            classLabel = "person",
            confidence = 0.8f,
            boundingBox = RectF(190f, 90f, 210f, 110f)
        )

        // Analizar primer frame
        objectAnalyzer.analyzeObjects(listOf(obj1), timestamp1)

        // Analizar segundo frame
        val analyzedObjects = objectAnalyzer.analyzeObjects(listOf(obj2), timestamp2)

        val analyzedObj = analyzedObjects.first()

        // Verificar cálculo de velocidad
        // Movimiento de 100 píxeles en 1 segundo = 100 px/s
        val expectedSpeed = 100f
        val tolerance = 5f

        assertTrue("Velocidad calculada debe ser aproximadamente $expectedSpeed px/s",
            abs(analyzedObj.speed - expectedSpeed) < tolerance)
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
        assertTrue("Dirección para movimiento hacia la derecha debe ser ~0°",
            direction >= -10f && direction <= 10f)
    }

    @Test
    fun testDistanceEstimation() {
        val person = DetectedObject(
            id = 1,
            classLabel = "person",
            confidence = 0.8f,
            boundingBox = RectF(100f, 100f, 200f, 300f) // 100x200 píxeles
        )

        val car = DetectedObject(
            id = 2,
            classLabel = "car",
            confidence = 0.9f,
            boundingBox = RectF(300f, 150f, 450f, 250f) // 150x100 píxeles
        )

        val analyzedObjects = objectAnalyzer.analyzeObjects(
            listOf(person, car),
            System.currentTimeMillis()
        )

        val analyzedPerson = analyzedObjects.find { it.classLabel == "person" }!!
        val analyzedCar = analyzedObjects.find { it.classLabel == "car" }!!

        assertTrue("Distancia de persona debe ser positiva", analyzedPerson.distance > 0f)
        assertTrue("Distancia de carro debe ser positiva", analyzedCar.distance > 0f)

        // Las distancias deben ser razonables (< 100m)
        assertTrue("Las distancias deben ser razonables (< 100m)",
            analyzedPerson.distance < 100f && analyzedCar.distance < 100f)
    }

    @Test
    fun testObjectTracking() {
        val timestamps = listOf(1000L, 2000L, 3000L)
        val positions = listOf(
            RectF(100f, 100f, 120f, 120f),  // Posición inicial
            RectF(150f, 100f, 170f, 120f),  // Movimiento hacia la derecha
            RectF(200f, 100f, 220f, 120f)   // Continúa moviéndose
        )

        var lastSpeed = 0f

        for (i in timestamps.indices) {
            val obj = DetectedObject(1, "bicycle", 0.7f, positions[i])
            val analyzed = objectAnalyzer.analyzeObjects(listOf(obj), timestamps[i])

            if (i > 0) {
                val currentSpeed = analyzed.first().speed
                assertTrue("Velocidad debe ser consistente", currentSpeed > 0f)

                if (i > 1) {
                    // La velocidad debería ser similar entre frames consecutivos
                    val speedDifference = abs(currentSpeed - lastSpeed)
                    assertTrue("Velocidad debe ser relativamente estable",
                        speedDifference < 20f) // Tolerancia de 20 px/s
                }
                lastSpeed = currentSpeed
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
        val originalIds = objects.map { it.id }.toSet()
        val analyzedIds = analyzedObjects.map { it.id }.toSet()
        assertEquals("IDs deben mantenerse", originalIds, analyzedIds)

        // Verificar que cada objeto tiene propiedades calculadas
        for (obj in analyzedObjects) {
            assertTrue("Distancia debe ser positiva", obj.distance > 0f)
            assertEquals("Velocidad inicial debe ser 0", 0f, obj.speed)
        }
    }

    @Test
    fun testDirectionToString() {
        // Probar conversión de grados a direcciones cardinales
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
        val oldTimestamp = System.currentTimeMillis() - 5000L // 5 segundos atrás
        objectAnalyzer.analyzeObjects(listOf(obj), oldTimestamp)

        // Analizar con timestamp actual (debería limpiar el historial)
        val currentTimestamp = System.currentTimeMillis()
        val emptyList = objectAnalyzer.analyzeObjects(emptyList(), currentTimestamp)

        assertTrue("Lista debe estar vacía después del cleanup", emptyList.isEmpty())
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
    fun testLinearMovement() {
        val positions = listOf(
            RectF(100f, 100f, 120f, 120f),  // t=0
            RectF(150f, 100f, 170f, 120f),  // t=1000ms, +50px horizontal
            RectF(200f, 100f, 220f, 120f),  // t=2000ms, +50px horizontal
            RectF(250f, 100f, 270f, 120f)   // t=3000ms, +50px horizontal
        )

        val timestamps = listOf(0L, 1000L, 2000L, 3000L)

        var previousSpeed = 0f

        for (i in positions.indices) {
            val obj = DetectedObject(1, "car", 0.9f, positions[i])
            val analyzed = objectAnalyzer.analyzeObjects(listOf(obj), timestamps[i])

            if (i > 0) {
                val currentSpeed = analyzed.first().speed

                // Verificar que la velocidad es positiva
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

    // Función auxiliar para crear objetos de prueba
    private fun createTestDetectedObjects(): List<DetectedObject> {
        return listOf(
            DetectedObject(1, "person", 0.85f, RectF(100f, 100f, 150f, 200f)),
            DetectedObject(2, "car", 0.92f, RectF(300f, 200f, 450f, 280f)),
            DetectedObject(3, "bicycle", 0.73f, RectF(200f, 150f, 230f, 180f))
        )
    }
}