package com.example.tic_a.detection

import android.content.Context
import android.graphics.Bitmap
import android.graphics.RectF
import android.os.SystemClock
import android.util.Log
import com.example.tic_a.models.DetectedObject
import org.tensorflow.lite.DataType
import org.tensorflow.lite.Interpreter
import org.tensorflow.lite.support.common.FileUtil
import org.tensorflow.lite.support.common.ops.CastOp
import org.tensorflow.lite.support.common.ops.NormalizeOp
import org.tensorflow.lite.support.image.ImageProcessor
import org.tensorflow.lite.support.image.TensorImage
import org.tensorflow.lite.support.tensorbuffer.TensorBuffer
import java.io.BufferedReader
import java.io.IOException
import java.io.InputStream
import java.io.InputStreamReader
import kotlin.math.max
import kotlin.math.min
import androidx.core.graphics.scale

class YoloDetector(
    context: Context,
    modelPath: String,
    private var confThreshold: Float,
    private var iouThreshold: Float
) {
    private val labels = mutableListOf<String>()
    private val interpreter: Interpreter

    private var tensorWidth = 0
    private var tensorHeight = 0
    private var numChannel = 0
    private var numElements = 0

    // Procesador de imagen mejorado basado en el código de referencia
    private val imageProcessor = ImageProcessor.Builder()
        .add(NormalizeOp(INPUT_MEAN, INPUT_STANDARD_DEVIATION))
        .add(CastOp(INPUT_IMAGE_TYPE))
        .build()

    init {
        try {
            // Cargar modelo
            val modelBuffer = FileUtil.loadMappedFile(context, modelPath)
            val options = Interpreter.Options().apply {
                setNumThreads(4)
            }
            interpreter = Interpreter(modelBuffer, options)

            // Obtener formas de entrada y salida
            val inputShape = interpreter.getInputTensor(0).shape()
            val outputShape = interpreter.getOutputTensor(0).shape()

            Log.d(TAG, "Input shape: ${inputShape.contentToString()}")
            Log.d(TAG, "Output shape: ${outputShape.contentToString()}")

            tensorWidth = inputShape[1]
            tensorHeight = inputShape[2]
            numChannel = outputShape[1]
            numElements = outputShape[2]

            Log.d(TAG, "Model initialized: ${tensorWidth}x${tensorHeight}, channels: $numChannel, elements: $numElements")

            // Cargar labels
            loadLabels(context)

        } catch (e: Exception) {
            Log.e(TAG, "Error initializing YoloDetector", e)
            throw e
        }
    }

    private fun loadLabels(context: Context) {
        try {
            val inputStream: InputStream = context.assets.open("labels.txt")
            val reader = BufferedReader(InputStreamReader(inputStream))

            var line: String? = reader.readLine()
            while (line != null && line.isNotEmpty()) {
                labels.add(line.trim())
                line = reader.readLine()
            }

            reader.close()
            inputStream.close()

            Log.d(TAG, "Loaded ${labels.size} labels")
        } catch (e: IOException) {
            Log.e(TAG, "Error loading labels", e)
            throw e
        }
    }

    fun detect(bitmap: Bitmap): List<DetectedObject> {
        if (tensorWidth == 0 || tensorHeight == 0 || numChannel == 0 || numElements == 0) {
            Log.e(TAG, "Model not properly initialized")
            return emptyList()
        }

        val inferenceStartTime = SystemClock.uptimeMillis()

        try {
            // Redimensionar imagen
            val resizedBitmap = bitmap.scale(tensorWidth, tensorHeight, false)

            // Procesar imagen usando TensorImage (más eficiente)
            val tensorImage = TensorImage(DataType.FLOAT32)
            tensorImage.load(resizedBitmap)
            val processedImage = imageProcessor.process(tensorImage)
            val imageBuffer = processedImage.buffer

            // Crear buffer de salida
            val output = TensorBuffer.createFixedSize(
                intArrayOf(1, numChannel, numElements),
                OUTPUT_IMAGE_TYPE
            )

            // Ejecutar inferencia
            interpreter.run(imageBuffer, output.buffer)

            val inferenceTime = SystemClock.uptimeMillis() - inferenceStartTime
            Log.d(TAG, "Inference time: ${inferenceTime}ms")

            // Procesar resultados
            val detectedObjects = processResults(output.floatArray, bitmap.width, bitmap.height)
            Log.d(TAG, "Detected ${detectedObjects.size} objects")

            return detectedObjects

        } catch (e: Exception) {
            Log.e(TAG, "Error during detection", e)
            return emptyList()
        }
    }

    private fun processResults(array: FloatArray, originalWidth: Int, originalHeight: Int): List<DetectedObject> {
        val boundingBoxes = mutableListOf<BoundingBox>()

        // Procesar cada elemento de detección
        for (c in 0 until numElements) {
            var maxConf = -1.0f
            var maxIdx = -1
            var j = 4
            var arrayIdx = c + numElements * j

            // Encontrar la clase con mayor confianza
            while (j < numChannel) {
                if (array[arrayIdx] > maxConf) {
                    maxConf = array[arrayIdx]
                    maxIdx = j - 4
                }
                j++
                arrayIdx += numElements
            }

            // Filtrar por threshold de confianza
            if (maxConf > confThreshold) {
                // Extraer coordenadas (normalizadas 0-1)
                val cx = array[c] // centro x
                val cy = array[c + numElements] // centro y
                val w = array[c + numElements * 2] // ancho
                val h = array[c + numElements * 3] // alto

                // Convertir a coordenadas de esquinas
                val x1 = cx - (w / 2f)
                val y1 = cy - (h / 2f)
                val x2 = cx + (w / 2f)
                val y2 = cy + (h / 2f)

                // Validar que las coordenadas estén en rango válido
                if (x1 >= 0f && x1 <= 1f && y1 >= 0f && y1 <= 1f &&
                    x2 >= 0f && x2 <= 1f && y2 >= 0f && y2 <= 1f &&
                    maxIdx < labels.size) {

                    val clsName = labels[maxIdx]

                    boundingBoxes.add(
                        BoundingBox(
                            x1 = x1, y1 = y1, x2 = x2, y2 = y2,
                            cx = cx, cy = cy, w = w, h = h,
                            cnf = maxConf, cls = maxIdx, clsName = clsName
                        )
                    )

                    Log.d(TAG, "Detection candidate: $clsName (${String.format("%.3f", maxConf)}) at [$x1, $y1, $x2, $y2]")
                }
            }
        }

        if (boundingBoxes.isEmpty()) {
            Log.d(TAG, "No detections above confidence threshold")
            return emptyList()
        }

        Log.d(TAG, "Found ${boundingBoxes.size} detections before NMS")

        // Aplicar Non-Maximum Suppression
        val finalBoxes = applyNMS(boundingBoxes)
        Log.d(TAG, "Final detections after NMS: ${finalBoxes.size}")

        // Convertir a DetectedObject con coordenadas en píxeles de imagen original
        return finalBoxes.mapIndexed { index, box ->
            DetectedObject(
                id = index,
                classLabel = box.clsName,
                confidence = box.cnf,
                boundingBox = RectF(
                    box.x1 * originalWidth,
                    box.y1 * originalHeight,
                    box.x2 * originalWidth,
                    box.y2 * originalHeight
                )
            )
        }
    }

    private fun applyNMS(boxes: List<BoundingBox>): List<BoundingBox> {
        val sortedBoxes = boxes.sortedByDescending { it.cnf }.toMutableList()
        val selectedBoxes = mutableListOf<BoundingBox>()

        while (sortedBoxes.isNotEmpty()) {
            val first = sortedBoxes.first()
            selectedBoxes.add(first)
            sortedBoxes.remove(first)

            val iterator = sortedBoxes.iterator()
            while (iterator.hasNext()) {
                val nextBox = iterator.next()
                val iou = calculateIoU(first, nextBox)
                if (iou >= iouThreshold) {
                    iterator.remove()
                }
            }
        }

        return selectedBoxes
    }

    private fun calculateIoU(box1: BoundingBox, box2: BoundingBox): Float {
        val x1 = max(box1.x1, box2.x1)
        val y1 = max(box1.y1, box2.y1)
        val x2 = min(box1.x2, box2.x2)
        val y2 = min(box1.y2, box2.y2)
        val intersectionArea = max(0f, x2 - x1) * max(0f, y2 - y1)
        val box1Area = box1.w * box1.h
        val box2Area = box2.w * box2.h
        return intersectionArea / (box1Area + box2Area - intersectionArea)
    }

    fun setConfidenceThreshold(threshold: Float) {
        confThreshold = threshold
        Log.d(TAG, "Confidence threshold updated to: $threshold")
    }

    fun setIouThreshold(threshold: Float) {
        iouThreshold = threshold
        Log.d(TAG, "IoU threshold updated to: $threshold")
    }

    fun close() {
        interpreter.close()
    }

    // Clase auxiliar para bounding box
    data class BoundingBox(
        val x1: Float, val y1: Float, val x2: Float, val y2: Float,
        val cx: Float, val cy: Float, val w: Float, val h: Float,
        val cnf: Float, val cls: Int, val clsName: String
    )

    companion object {
        private const val TAG = "YoloDetector"
        private const val INPUT_MEAN = 0f
        private const val INPUT_STANDARD_DEVIATION = 255f
        private val INPUT_IMAGE_TYPE = DataType.FLOAT32
        private val OUTPUT_IMAGE_TYPE = DataType.FLOAT32
    }
}