# 🦯 Sistema de Asistencia de Navegación para Personas con Baja Agudeza Visual

Este repositorio forma parte de un proyecto desarrollado en el campus Rubén Orellana de la Escuela Politécnica Nacional. El objetivo es brindar una solución tecnológica que mejore la movilidad y orientación de personas con baja visión dentro del campus, integrando tecnologías como visión por computadora, realidad aumentada y algoritmos de navegación.

---

## 🎯 Objetivo del Proyecto

Diseñar e implementar un asistente de navegación accesible que permita a estudiantes y visitantes con baja agudeza visual recorrer el campus de manera más segura e independiente. El sistema proporciona retroalimentación visual, auditiva y espacial en tiempo real.

---

## 🧩 Componentes del Sistema

### 1. 👁️ Visión por Computadora

Se desarrolló un módulo en Kotlin que aprovecha técnicas de visión por computadora para detectar y reconocer señales relevantes dentro del entorno. Este componente está pensado para ejecutarse en dispositivos móviles Android, interpretando imágenes de cámaras en tiempo real.

---

### 2. 🧠 Módulo de Realidad Aumentada 

Utilizamos **AR Image Tracking** para mostrar información contextual (por ejemplo, sobre bibliotecas) cuando se detectan marcadores visuales específicos. Este módulo fue desarrollado en Unity e incluye:

- Interfaces **2D y 3D** accesibles y amigables.
- Integración con **AR Foundation** y **ARCore**.
- Renderizado de líneas con **UILine Renderer** para guiar al usuario.
- Comunicación con el Módulo A mediante **Native WebSockets**.
- Comunicación con el Módulo B mediante lectura de archivos con **Newtonsoft Json**
- Síntesis de voz con **TTS**, implementado vía **JNI** para interacción fluida con funciones nativas.

---

### 3. 🗺️ Módulo de Navegación 
El sistema de navegación se encarga de trazar rutas accesibles por el terreno del campus, evitando obstáculos. Sus características clave:

- Modelado de terrenos y caminos mediante **NavMesh**.
- Cálculo de rutas óptimas utilizando el algoritmo **A\***.
- Retroalimentación en tiempo real sobre la ubicación y la dirección del usuario dentro del campus.

---

## 🚀 Estado del Proyecto

Actualmente, cada módulo se encuentra en desarrollo activo y en pruebas dentro del entorno real del campus Rubén Orellana. Las primeras pruebas han mostrado resultados prometedores en la interacción multimodal (visual + auditiva + espacial).

---

## 👨‍💻 Tecnologías Principales

- **Kotlin** (Android - Visión por Computadora)
- **Unity** (Interfaz y lógica de navegación)
- **AR Foundation**, **ARCore**
- **JNI**, **TTS**
- **Native WebSockets**
- **NavMesh**, **A\*** (Pathfinding)

---

## 🤝 Agradecimientos

Gracias a todos quienes han apoyado este proyecto desde la Escuela Politécnica Nacional hasta cada uno de los integrantes del equipo.

---

> _"La tecnología no debe ser un privilegio, sino una herramienta para incluir a todos."_
