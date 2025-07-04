package com.example.tic_a.comunication

import android.util.Log
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import java.io.BufferedReader
import java.io.BufferedWriter
import java.io.InputStreamReader
import java.io.OutputStreamWriter
import java.net.InetSocketAddress
import java.net.Socket
import java.util.concurrent.ConcurrentLinkedQueue
import java.util.concurrent.atomic.AtomicBoolean

/**
 * Socket class for communication with Component B
 */
class SocketCommunication(
    private val serverAddress: String = "127.0.0.1",
    private val serverPort: Int = 8080,
    private val reconnectDelayMs: Long = 2000,
    private val messageCallback: ((String) -> Unit)? = null
) {
    private var socket: Socket? = null
    private var writer: BufferedWriter? = null
    private var reader: BufferedReader? = null
    private val messageQueue = ConcurrentLinkedQueue<String>()
    private val isConnected = AtomicBoolean(false)
    private val isConnecting = AtomicBoolean(false)
    private val scope = CoroutineScope(Dispatchers.IO)
    private var sendJob: Job? = null
    private var receiveJob: Job? = null
    private var connectJob: Job? = null

    /**
     * Starts the socket connection
     */
    fun start() {
        if (isConnecting.getAndSet(true)) {
            return
        }

        connectJob = scope.launch {
            while (isActive) {
                try {
                    connect()

                    // If connection successful, start send and receive jobs
                    if (isConnected.get()) {
                        startSendJob()
                        startReceiveJob()
                        break
                    }
                } catch (e: Exception) {
                    Log.e(TAG, "Error connecting to server: ${e.message}")
                    close()
                }

                // Wait before reconnecting
                delay(reconnectDelayMs)
            }

            isConnecting.set(false)
        }
    }

    /**
     * Connects to the server
     */
    private fun connect() {
        try {
            Log.d(TAG, "Connecting to $serverAddress:$serverPort...")

            socket = Socket().apply {
                connect(InetSocketAddress(serverAddress, serverPort), 5000) // 5 second timeout
                keepAlive = true
            }

            writer = BufferedWriter(OutputStreamWriter(socket!!.getOutputStream()))
            reader = BufferedReader(InputStreamReader(socket!!.getInputStream()))

            isConnected.set(true)
            Log.d(TAG, "Connected to server")
        } catch (e: Exception) {
            Log.e(TAG, "Failed to connect: ${e.message}")
            isConnected.set(false)
            throw e
        }
    }

    /**
     * Starts the job for sending messages from queue
     */
    private fun startSendJob() {
        sendJob?.cancel()
        sendJob = scope.launch {
            while (isActive && isConnected.get()) {
                try {
                    // Process messages in queue
                    while (messageQueue.isNotEmpty()) {
                        val message = messageQueue.poll() ?: continue
                        sendMessage(message)
                    }

                    delay(10) // Small delay to prevent CPU hogging
                } catch (e: Exception) {
                    Log.e(TAG, "Error in send job: ${e.message}")
                    isConnected.set(false)
                    break
                }
            }
        }
    }

    /**
     * Starts the job for receiving messages
     */
    private fun startReceiveJob() {
        receiveJob?.cancel()
        receiveJob = scope.launch {
            while (isActive && isConnected.get()) {
                try {
                    val line = reader?.readLine()
                    if (line != null) {
                        messageCallback?.invoke(line)
                    } else {
                        // Null indicates the stream is closed
                        Log.d(TAG, "Connection closed by server")
                        isConnected.set(false)
                        break
                    }
                } catch (e: Exception) {
                    Log.e(TAG, "Error in receive job: ${e.message}")
                    isConnected.set(false)
                    break
                }
            }
        }
    }

    /**
     * Queues a message to be sent
     * @param message Message to send
     */
    fun queueMessage(message: String) {
        messageQueue.add(message)

        // If not connected, try to reconnect
        if (!isConnected.get() && !isConnecting.get()) {
            start()
        }
    }

    /**
     * Sends a message immediately
     * @param message Message to send
     */
    private fun sendMessage(message: String) {
        try {
            writer?.apply {
                write(message)
                write("\n") // Line terminator
                flush()
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error sending message: ${e.message}")
            isConnected.set(false)
            throw e
        }
    }

    /**
     * Checks if socket is connected
     * @return True if connected, false otherwise
     */
    fun isConnected(): Boolean {
        return isConnected.get()
    }

    /**
     * Closes the socket connection
     */
    fun close() {
        isConnected.set(false)

        // Cancel all jobs
        sendJob?.cancel()
        receiveJob?.cancel()

        try {
            writer?.close()
            reader?.close()
            socket?.close()
        } catch (e: Exception) {
            Log.e(TAG, "Error closing socket: ${e.message}")
        }

        writer = null
        reader = null
        socket = null
    }

    companion object {
        private const val TAG = "SocketCommunication"
    }
}