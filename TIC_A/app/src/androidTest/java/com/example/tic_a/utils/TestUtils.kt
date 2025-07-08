package com.example.tic_a.utils

import android.content.Context
import android.graphics.Bitmap
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.RectF
import com.example.tic_a.models.DetectedObject
import java.io.File
import java.io.FileOutputStream
import java.text.SimpleDateFormat
import java.util.*

object TestUtils {

    /**
     * Crea un bitmap sintético para pruebas con objetos conocidos
     */
    fun createSyntheticTestImage(
        width: Int = 640,
        height: Int = 640,
        objects: List<SyntheticObject> = emptyList()
    ): Bitmap {
        val bitmap = Bitmap.createBitmap(width, height, Bitmap.Config.ARGB_8888)
        val canvas = Canvas(bitmap)

        // Fondo
        canvas.drawColor(Color.WHITE)

        // Dibujar objetos sintéticos
        objects.forEach { obj ->
            drawSyntheticObject(canvas, obj)
        }

        return bitmap
    }

    private fun drawSyntheticObject(canvas: Canvas, obj: SyntheticObject) {
        val paint = Paint().apply {
            color = obj.color
            style = Paint.Style.FILL
        }

        when (obj.type) {
            SyntheticObjectType.RECTANGLE -> {
                canvas.drawRect(obj.bounds, paint)
            }
            SyntheticObjectType.CIRCLE -> {
                val centerX = obj.bounds.centerX()
                val centerY = obj.bounds.centerY()
                val radius = minOf(obj.bounds.width(), obj.bounds.height()) / 2
                canvas.drawCircle(centerX, centerY, radius, paint)
            }
            SyntheticObjectType.PERSON_SHAPE -> {
                // Forma simple que simula una persona
                drawPersonShape(canvas, obj.bounds, paint)
            }
            SyntheticObjectType.CAR_SHAPE -> {
                // Forma simple que simula un carro
                drawCarShape(canvas, obj.bounds, paint)
            }
        }

        // Agregar texto si es necesario
        if (obj.label.isNotEmpty()) {
            val textPaint = Paint().apply {
                color = Color.BLACK
                textSize = 24f
                isAntiAlias = true
            }
            canvas.drawText(obj.label, obj.bounds.left, obj.bounds.top - 5f, textPaint)
        }
    }

    private fun drawPersonShape(canvas: Canvas, bounds: RectF, paint: Paint) {
        // Cabeza (círculo)
        val headRadius = bounds.width() * 0.15f
        canvas.drawCircle(
            bounds.centerX(),
            bounds.top + headRadius + 10f,
            headRadius,
            paint
        )

        // Cuerpo (rectángulo)
        val bodyTop = bounds.top + headRadius * 2 + 20f
        val bodyBottom = bounds.bottom - bounds.height() * 0.4f
        canvas.drawRect(
            bounds.centerX() - bounds.width() * 0.2f,
            bodyTop,
            bounds.centerX() + bounds.width() * 0.2f,
            bodyBottom,
            paint
        )

        // Piernas (líneas gruesas)
        val legPaint = Paint().apply {
            color = paint.color
            strokeWidth = 15f
        }
        canvas.drawLine(
            bounds.centerX() - 10f, bodyBottom,
            bounds.centerX() - 15f, bounds.bottom,
            legPaint
        )
        canvas.drawLine(
            bounds.centerX() + 10f, bodyBottom,
            bounds.centerX() + 15f, bounds.bottom,
            legPaint
        )
    }

    private fun drawCarShape(canvas: Canvas, bounds: RectF, paint: Paint) {
        // Cuerpo principal del carro
        val carBody = RectF(
            bounds.left,
            bounds.top + bounds.height() * 0.3f,
            bounds.right,
            bounds.bottom - bounds.height() * 0.2f
        )
        canvas.drawRect(carBody, paint)

        // Techo
        val roof = RectF(
            bounds.left + bounds.width() * 0.2f,
            bounds.top,
            bounds.right - bounds.width() * 0.2f,
            bounds.top + bounds.height() * 0.4f
        )
        canvas.drawRect(roof, paint)

        // Ruedas
        val wheelPaint = Paint().apply {
            color = Color.BLACK
            style = Paint.Style.FILL
        }
        val wheelRadius = bounds.height() * 0.1f
        canvas.drawCircle(
            bounds.left + bounds.width() * 0.2f,
            bounds.bottom - wheelRadius,
            wheelRadius,
            wheelPaint
        )
        canvas.drawCircle(
            bounds.right - bounds.width() * 0.2f,
            bounds.bottom - wheelRadius,
            wheelRadius,
            wheelPaint
        )
    }

    /**
     * Guarda un bitmap en el almacenamiento para inspección manual
     */
    fun saveBitmapForDebugging(context: Context, bitmap: Bitmap, testName: String) {
        try {
            val timestamp = SimpleDateFormat("yyyyMMdd_HHmmss", Locale.getDefault()).format(Date())
            val filename = "${testName}_${timestamp}.png"
            val file = File(context.getExternalFilesDir("test_images"), filename)

            file.parentFile?.mkdirs()

            val out = FileOutputStream(file)
            bitmap.compress(Bitmap.CompressFormat.PNG, 100, out)
            out.flush()
            out.close()

            println("Test image saved: ${file.absolutePath}")
        } catch (e: Exception) {
            println("Error saving test image: ${e.message}")
        }
    }

    /**
     * Compara dos listas de detecciones para verificar similitud
     */
    fun compareDetections(
        detections1: List<DetectedObject>,
        detections2: List<DetectedObject>,
        iouThreshold: Float = 0.7f,
        confidenceThreshold: Float = 0.1f
    ): DetectionComparisonResult {
        var matches = 0
        var totalDetections1 = detections1.size
        var totalDetections2 = detections2.size

        val matched1 = mutableSetOf<Int>()
        val matched2 = mutableSetOf<Int>()

        for (i in detections1.indices) {
            for (j in detections2.indices) {
                if (j in matched2) continue

                val det1 = detections1[i]
                val det2 = detections2[j]

                // Verificar misma clase
                if (det1.classLabel != det2.classLabel) continue

                // Verificar IoU
                val iou = calculateIoU(det1.boundingBox, det2.boundingBox)
                if (iou < iouThreshold) continue

                // Verificar diferencia de confianza
                val confidenceDiff = kotlin.math.abs(det1.confidence - det2.confidence)
                if (confidenceDiff > confidenceThreshold) continue

                matches++
                matched1.add(i)
                matched2.add(j)
                break
            }
        }

        val similarity = if (maxOf(totalDetections1, totalDetections2) > 0) {
            matches.toFloat() / maxOf(totalDetections1, totalDetections2)
        } else {
            1.0f
        }

        return DetectionComparisonResult(
            similarity = similarity,
            matches = matches,
            totalDetections1 = totalDetections1,
            totalDetections2 = totalDetections2,
            unmatched1 = totalDetections1 - matched1.size,
            unmatched2 = totalDetections2 - matched2.size
        )
    }

    /**
     * Genera un reporte de pruebas en formato JSON
     */
    fun generateTestReport(results: List<TestResult>): String {
        val report = mutableMapOf<String, Any>()
        report["timestamp"] = SimpleDateFormat("yyyy-MM-dd HH:mm:ss", Locale.getDefault()).format(Date())
        report["total_tests"] = results.size
        report["passed_tests"] = results.count { it.passed }
        report["failed_tests"] = results.count { !it.passed }
        report["success_rate"] = results.count { it.passed }.toFloat() / results.size

        val detailedResults = results.map { result ->
            mapOf(
                "test_name" to result.testName,
                "passed" to result.passed,
                "execution_time_ms" to result.executionTimeMs,
                "error_message" to result.errorMessage,
                "metrics" to result.metrics
            )
        }

        report["detailed_results"] = detailedResults

        return org.json.JSONObject(report).toString(2)
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

// Clases de datos para testing
data class SyntheticObject(
    val type: SyntheticObjectType,
    val bounds: RectF,
    val color: Int = Color.BLUE,
    val label: String = ""
)

enum class SyntheticObjectType {
    RECTANGLE,
    CIRCLE,
    PERSON_SHAPE,
    CAR_SHAPE
}

data class DetectionComparisonResult(
    val similarity: Float,
    val matches: Int,
    val totalDetections1: Int,
    val totalDetections2: Int,
    val unmatched1: Int,
    val unmatched2: Int
)

data class TestResult(
    val testName: String,
    val passed: Boolean,
    val executionTimeMs: Long,
    val errorMessage: String? = null,
    val metrics: Map<String, Any> = emptyMap()
)