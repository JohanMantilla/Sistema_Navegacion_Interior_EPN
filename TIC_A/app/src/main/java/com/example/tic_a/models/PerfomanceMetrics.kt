package com.example.tic_a.models

/**
 * Model class for storing performance metrics of the detection system
 */
data class PerformanceMetrics(
    val fps: Float = 0f,                // Frames per second
    val processingTimeMs: Float = 0f,   // Processing time per frame in milliseconds
    val cpuUsage: Float = 0f,           // CPU usage in percentage
    val memoryUsage: Float = 0f         // Memory usage in MB
)