package com.example.tic_a.utils

import android.app.ActivityManager
import android.content.Context
import android.os.Process
import android.util.Log
import com.example.tic_a.models.PerformanceMetrics
import java.io.RandomAccessFile
import java.util.LinkedList
import java.util.Queue

/**
 * Monitors performance metrics of the application
 */
class PerformanceMonitor(private val context: Context) {
    private val frameTimeQueue: Queue<Long> = LinkedList()
    private val maxQueueSize = 30  // For calculating FPS over the last 30 frames
    private var lastFrameTimestamp: Long = 0
    private var processingStartTime: Long = 0

    /**
     * Notifies the monitor that frame processing has started
     */
    fun onProcessingStart() {
        processingStartTime = System.currentTimeMillis()
    }

    /**
     * Notifies the monitor that frame processing has completed
     */
    fun onProcessingComplete() {
        val currentTime = System.currentTimeMillis()

        // Calculate processing time for this frame
        val processingTime = currentTime - processingStartTime

        // Record frame timestamp for FPS calculation
        frameTimeQueue.add(currentTime)
        if (frameTimeQueue.size > maxQueueSize) {
            frameTimeQueue.remove()
        }

        lastFrameTimestamp = currentTime
    }

    /**
     * Gets current performance metrics
     * @return PerformanceMetrics object with current metrics
     */
    fun getMetrics(): PerformanceMetrics {
        return PerformanceMetrics(
            fps = calculateFps(),
            processingTimeMs = calculateProcessingTime(),
            cpuUsage = getCpuUsage(),
            memoryUsage = getMemoryUsage()
        )
    }

    /**
     * Calculates frames per second based on recent frame timestamps
     * @return Current FPS
     */
    private fun calculateFps(): Float {
        if (frameTimeQueue.size < 2) return 0f

        val oldestTimestamp = frameTimeQueue.peek() ?: return 0f
        val timeSpanSec = (lastFrameTimestamp - oldestTimestamp) / 1000f

        return if (timeSpanSec > 0) {
            (frameTimeQueue.size - 1) / timeSpanSec
        } else {
            0f
        }
    }

    /**
     * Calculates average processing time per frame
     * @return Average processing time in milliseconds
     */
    private fun calculateProcessingTime(): Float {
        val currentTime = System.currentTimeMillis()
        return (currentTime - processingStartTime).toFloat()
    }

    /**
     * Gets current CPU usage for the app
     * @return CPU usage as percentage
     */
    private fun getCpuUsage(): Float {
        try {
            val pid = Process.myPid()
            val reader = RandomAccessFile("/proc/$pid/stat", "r")
            val load = reader.readLine()
            val toks = load.split(" ".toRegex()).dropLastWhile { it.isEmpty() }

            val utime = toks[13].toLong()
            val stime = toks[14].toLong()
            val cutime = toks[15].toLong()
            val cstime = toks[16].toLong()

            val total = utime + stime + cutime + cstime

            // Read CPU frequency to normalize usage
            val cpuFreq = RandomAccessFile("/sys/devices/system/cpu/cpu0/cpufreq/scaling_cur_freq", "r")
            val frequency = cpuFreq.readLine().toLong() / 1000 // kHz to MHz

            // Simple normalization - not perfect but gives an estimate
            val usage = total.toFloat() / frequency * 100
            return minOf(usage, 100f)

        } catch (e: Exception) {
            Log.e(TAG, "Error getting CPU usage: ${e.message}")
            return 0f
        }
    }

    /**
     * Gets current memory usage for the app
     * @return Memory usage in MB
     */
    private fun getMemoryUsage(): Float {
        try {
            val activityManager = context.getSystemService(Context.ACTIVITY_SERVICE) as ActivityManager
            val info = ActivityManager.MemoryInfo()
            activityManager.getMemoryInfo(info)

            val runtime = Runtime.getRuntime()
            val usedMemory = (runtime.totalMemory() - runtime.freeMemory()) / (1024f * 1024f)

            return usedMemory
        } catch (e: Exception) {
            Log.e(TAG, "Error getting memory usage: ${e.message}")
            return 0f
        }
    }

    companion object {
        private const val TAG = "PerformanceMonitor"
    }
}