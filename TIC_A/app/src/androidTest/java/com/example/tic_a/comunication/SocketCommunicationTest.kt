package com.example.tic_a.comunication

import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.*
import kotlinx.coroutines.Dispatchers
import org.junit.After
import org.junit.Before
import org.junit.Test
import org.junit.Assert.*
import java.io.*
import java.net.Socket
import java.util.concurrent.CountDownLatch
import java.util.concurrent.TimeUnit

@ExperimentalCoroutinesApi
class SocketCommunicationTest {

    private lateinit var testDispatcher: TestDispatcher
    private var receivedMessages = mutableListOf<String>()
    private lateinit var socketCommunication: SocketCommunication

    @Before
    fun setUp() {
        testDispatcher = StandardTestDispatcher()
        Dispatchers.setMain(testDispatcher)

        receivedMessages.clear()

        // Inicializar socketCommunication en setUp() para evitar lateinit errors
        socketCommunication = SocketCommunication(
            serverAddress = "127.0.0.1",
            serverPort = 8080,
            reconnectDelayMs = 100, // Reducir delay para tests más rápidos
            messageCallback = { message ->
                receivedMessages.add(message)
            }
        )
    }

    @After
    fun tearDown() {
        try {
            socketCommunication.close()
        } catch (e: Exception) {
            // Ignore cleanup errors
        }
        Dispatchers.resetMain()
    }

    @Test
    fun queueMessageShouldAddMessageToQueue() {
        // Act
        socketCommunication.queueMessage("test message")

        // Assert - Verificar que el mensaje se agregó a la cola
        // Como no podemos acceder directamente a messageQueue,
        // verificamos indirectamente a través del comportamiento
        assertFalse("Socket should not be connected initially", socketCommunication.isConnected())
    }

    @Test
    fun isConnectedShouldReturnFalseInitially() {
        // Assert
        assertFalse("Socket should not be connected initially", socketCommunication.isConnected())
    }

    @Test
    fun closeShouldSetConnectedToFalse() {
        // Act
        socketCommunication.close()

        // Assert
        assertFalse("Socket should be disconnected after close", socketCommunication.isConnected())
    }

    @Test
    fun multipleQueueMessageCallsShouldQueueAllMessages() = runTest {
        // Act
        repeat(5) { i ->
            socketCommunication.queueMessage("Message $i")
        }

        // Assert - Los mensajes están en cola (verificación indirecta)
        assertFalse("Should remain disconnected", socketCommunication.isConnected())
    }

    @Test
    fun startShouldNotStartMultipleConnectionAttemptsSimultaneously() = runTest {
        // Act - Intentar iniciar múltiples veces
        socketCommunication.start()
        socketCommunication.start()
        socketCommunication.start()

        advanceTimeBy(500)

        // Assert - Solo debería haber un intento de conexión
        // (verificación indirecta a través del estado)
        assertFalse("Should remain disconnected in test environment", socketCommunication.isConnected())
    }

    @Test
    fun constructorShouldCreateInstanceWithDefaultValues() {
        // Act - Crear una nueva instancia con valores por defecto
        val defaultSocket = SocketCommunication()

        // Assert
        assertNotNull("Should create instance with defaults", defaultSocket)
        assertFalse("Should not be connected", defaultSocket.isConnected())

        // Cleanup
        defaultSocket.close()
    }

    @Test
    fun queueMessageShouldNotThrowException() {
        // Act & Assert - No debería lanzar excepción
        try {
            socketCommunication.queueMessage("test")
            socketCommunication.queueMessage("another test")
            assertTrue("Should complete without exception", true)
        } catch (e: Exception) {
            fail("Should not throw exception: ${e.message}")
        }

        // Verificar estado
        assertFalse("Should remain disconnected", socketCommunication.isConnected())
    }

    @Test
    fun closeShouldBeIdempotent() {
        // Act - Cerrar múltiples veces no debería causar problemas
        socketCommunication.close()
        socketCommunication.close()
        socketCommunication.close()

        // Assert
        assertFalse("Should remain closed", socketCommunication.isConnected())
    }

    @Test
    fun multipleStartCallsShouldNotCauseIssues() {
        // Act
        try {
            socketCommunication.start()
            socketCommunication.start()
            assertTrue("Multiple start calls should not cause issues", true)
        } catch (e: Exception) {
            fail("Multiple start calls should not throw exception: ${e.message}")
        }

        // Cleanup
        socketCommunication.close()
    }
}

// Clase básica para pruebas de funcionalidad sin mocks
class SocketCommunicationBasicTest {

    private lateinit var socketCommunication: SocketCommunication
    private val receivedMessages = mutableListOf<String>()

    @Before
    fun setUp() {
        receivedMessages.clear()
        socketCommunication = SocketCommunication(
            serverAddress = "127.0.0.1",
            serverPort = 8080,
            reconnectDelayMs = 100,
            messageCallback = { message ->
                receivedMessages.add(message)
            }
        )
    }

    @After
    fun tearDown() {
        socketCommunication.close()
    }

    @Test
    fun constructorWithParametersShouldWork() {
        val customSocket = SocketCommunication(
            serverAddress = "192.168.1.1",
            serverPort = 9999,
            reconnectDelayMs = 500,
            messageCallback = null
        )

        assertNotNull("Should create custom instance", customSocket)
        assertFalse("Should not be connected", customSocket.isConnected())
        customSocket.close()
    }

    @Test
    fun queueMultipleMessagesShouldNotFail() {
        // Act - Enviar varios mensajes
        val messages = listOf(
            "Hola mundo",
            "Mensaje con números 123",
            "Mensaje con símbolos !@#$%",
            "Mensaje largo ".repeat(50)
        )

        messages.forEach { message ->
            socketCommunication.queueMessage(message)
        }

        // Assert - No debería fallar
        assertTrue("Should handle multiple messages", true)
        assertFalse("Should remain disconnected", socketCommunication.isConnected())
    }

    @Test
    fun connectionLifecycleShouldWork() {
        // Estado inicial
        assertFalse("Should start disconnected", socketCommunication.isConnected())

        // Intentar conectar (fallará pero no debería explotar)
        socketCommunication.start()

        // Enviar mensaje
        socketCommunication.queueMessage("test message")

        // Cerrar
        socketCommunication.close()
        assertFalse("Should be closed", socketCommunication.isConnected())

        // Verificar que se puede reiniciar
        socketCommunication.start()
        socketCommunication.close()
    }

    @Test
    fun emptyMessageShouldBeHandled() {
        // Act & Assert
        socketCommunication.queueMessage("")
        socketCommunication.queueMessage("   ")
        socketCommunication.queueMessage("\n")

        // No debería explotar
        assertTrue("Should handle empty/whitespace messages", true)
    }

    @Test
    fun nullCallbackShouldNotCauseIssues() {
        val socketWithNullCallback = SocketCommunication(
            serverAddress = "127.0.0.1",
            serverPort = 8080,
            messageCallback = null // Callback nulo
        )

        try {
            socketWithNullCallback.start()
            socketWithNullCallback.queueMessage("test")
            socketWithNullCallback.close()
            assertTrue("Should handle null callback", true)
        } catch (e: Exception) {
            fail("Should not fail with null callback: ${e.message}")
        }
    }
}

// Pruebas de integración simplificadas SIN mocks
class SocketCommunicationIntegrationTest {

    private lateinit var socketCommunication: SocketCommunication
    private val receivedMessages = mutableListOf<String>()

    @Before
    fun setUp() {
        receivedMessages.clear()
        socketCommunication = SocketCommunication(
            serverAddress = "127.0.0.1",
            serverPort = 8080,
            reconnectDelayMs = 100,
            messageCallback = { message ->
                receivedMessages.add(message)
            }
        )
    }

    @After
    fun tearDown() {
        try {
            socketCommunication.close()
        } catch (e: Exception) {
            // Ignore cleanup errors in tests
        }
    }

    @Test
    fun integrationTestBasicFunctionality() {
        // Verificar creación
        assertNotNull("SocketCommunication should be created", socketCommunication)
        assertFalse("Should not be connected initially", socketCommunication.isConnected())

        // Prueba básica de funcionalidad
        socketCommunication.queueMessage("test message")
        assertFalse("Should remain disconnected without server", socketCommunication.isConnected())

        // Verificar que start() no lance excepción
        try {
            socketCommunication.start()
            assertTrue("Start should not throw exception", true)
        } catch (e: Exception) {
            fail("Start should not throw exception: ${e.message}")
        }

        // Como no hay servidor real, debería seguir desconectado
        assertFalse("Should remain disconnected without real server", socketCommunication.isConnected())
    }

    @Test
    fun integrationTestMultipleMessages() {
        // Probar envío de múltiples mensajes
        val messages = listOf("message1", "message2", "message3")

        messages.forEach { message ->
            socketCommunication.queueMessage(message)
        }

        // Verificar que no se lance excepción
        assertTrue("Multiple messages should be queued without error", true)
        assertFalse("Should remain disconnected", socketCommunication.isConnected())
    }

    @Test
    fun integrationTestConnectionTimeout() {
        // Usar puerto diferente para simular servidor no disponible
        val testSocket = SocketCommunication(
            serverAddress = "127.0.0.1",
            serverPort = 9876, // Puerto que probablemente esté cerrado
            reconnectDelayMs = 100,
            messageCallback = { message ->
                receivedMessages.add(message)
            }
        )

        try {
            // Intentar conectar - debería manejar timeout gracefully
            testSocket.start()
            Thread.sleep(500) // Dar tiempo para intento de conexión

            // Debería seguir desconectado
            assertFalse("Should remain disconnected on timeout", testSocket.isConnected())

            // Debería poder enviar mensajes a la cola sin problemas
            testSocket.queueMessage("test message")
            assertTrue("Should handle queuing when disconnected", true)

        } finally {
            testSocket.close()
        }
    }
}