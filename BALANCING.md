# Guía de Balanceo - Realistic Industrial Power Consumption

## Cómo Revisar y Balancear el Mod

### 1. Ubicación de los Logs

Los logs del mod se encuentran en:
```
C:\Users\[TU_USUARIO]\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\
```

Busca el archivo más reciente (por fecha) llamado `Player.log` o `Player-prev.log`

### 2. Qué Buscar en los Logs

El mod registra estadísticas detalladas cada **~10 segundos** con este formato:

```
========== POWER CONSUMPTION STATISTICS ==========
Total industrial buildings: 45
User multiplier: 150%

SMALL (≤30 tiles): 15 buildings
  Avg: 450 kW | Min: 240 kW | Max: 675 kW

MEDIUM (31-100 tiles): 18 buildings
  Avg: 1800 kW | Min: 720 kW | Max: 3300 kW

LARGE (101-250 tiles): 10 buildings
  Avg: 5400 kW | Min: 2700 kW | Max: 8400 kW

VERY LARGE (>250 tiles): 2 buildings
  Avg: 15000 kW | Min: 11250 kW | Max: 18750 kW
==================================================
```

### 3. Valores de Referencia Esperados

Con el **multiplicador de usuario al 150%** (default), estos son los valores esperados aproximados:

| Categoría | Área (tiles) | Rango Mult. Base | Con User 150% | Ejemplo |
|-----------|--------------|------------------|---------------|---------|
| **Small** | ≤30 | 0.8x - 1.5x | 120 kW - 675 kW | Pequeña fábrica 4×6 = 24 tiles |
| **Medium** | 31-100 | 1.2x - 2.2x | 540 kW - 3,300 kW | Fábrica media 8×10 = 80 tiles |
| **Large** | 101-250 | 1.8x - 2.8x | 2,700 kW - 8,400 kW | Fábrica grande 12×16 = 192 tiles |
| **Very Large** | >250 | 2.5x - 4.0x | 11,250 kW - 18,000 kW | Mega fábrica 20×20 = 400 tiles |

**Nota:** Estos valores asumen un consumo base del juego de ~300 kW por edificio.

### 4. Análisis de Balance

#### ¿El Consumo es Muy Alto?
- Revisa los valores "Avg" (promedio) en los logs
- Si el juego es muy difícil (apagones constantes), considera:
  - Reducir el multiplicador de usuario a 100%-125%
  - O ajustar los rangos en `IndustrialPowerSystem.cs` líneas 256-283

#### ¿El Consumo es Muy Bajo?
- Si el juego es demasiado fácil, considera:
  - Aumentar el multiplicador de usuario a 175%-200%
  - O ajustar los rangos de multiplicadores

#### Valores Balanceados
El mod está equilibrado cuando:
- Una ciudad pequeña (20-30 edificios industriales) necesita 1-2 plantas de energía medianas
- Una ciudad grande (100+ edificios) necesita planificación seria de energía
- Los edificios grandes consumen notablemente más que los pequeños
- Hay progresión: empezar es fácil, expandirse requiere planificación

### 5. Ajustar los Multiplicadores

Si necesitas ajustar los multiplicadores base, edita `IndustrialPowerSystem.cs`:

```csharp
// Línea 256-283 aproximadamente
if (lotArea <= 30)
{
    // Small buildings: CAMBIA ESTOS VALORES
    sizeCategory = "Small";
    float t = (float)lotArea / 30f;
    return math.lerp(0.8f, 1.5f, t);  // MIN y MAX aquí
}
else if (lotArea <= 100)
{
    // Medium buildings
    sizeCategory = "Medium";
    float t = (float)(lotArea - 30) / 70f;
    return math.lerp(1.2f, 2.2f, t);  // MIN y MAX aquí
}
// ... y así sucesivamente
```

### 6. Tabla de Ajustes Sugeridos

| Dificultad Deseada | User Mult. | Small Range | Medium Range | Large Range | Very Large Range |
|-------------------|------------|-------------|--------------|-------------|------------------|
| **Muy Fácil** | 75%-100% | 0.5x-1.0x | 0.8x-1.5x | 1.2x-2.0x | 1.5x-2.5x |
| **Fácil** | 100%-125% | 0.6x-1.2x | 1.0x-1.8x | 1.5x-2.3x | 2.0x-3.0x |
| **Normal** (default) | 125%-150% | 0.8x-1.5x | 1.2x-2.2x | 1.8x-2.8x | 2.5x-4.0x |
| **Difícil** | 150%-200% | 1.0x-2.0x | 1.5x-2.5x | 2.0x-3.5x | 3.0x-5.0x |
| **Muy Difícil** | 200%-300% | 1.5x-2.5x | 2.0x-3.5x | 3.0x-5.0x | 4.0x-7.0x |

### 7. Proceso de Testing Recomendado

1. **Inicia una ciudad nueva** o carga una partida con zona industrial activa
2. **Construye variedad de edificios**: pequeños, medianos y grandes
3. **Deja el juego correr 2-3 minutos** en velocidad normal
4. **Revisa los logs** y anota las estadísticas
5. **Observa el panel de electricidad** en el juego:
   - ¿Hay apagones frecuentes?
   - ¿La demanda es manejable?
   - ¿Necesitas construir muchas plantas?
6. **Ajusta el multiplicador** en el juego (Options > Mod Settings)
7. **Repite** hasta encontrar el balance deseado

### 8. Comandos Útiles

Para ver logs en tiempo real (PowerShell):
```powershell
Get-Content "C:\Users\[TU_USUARIO]\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\Player.log" -Wait -Tail 50
```

Para buscar solo estadísticas del mod:
```powershell
Select-String "POWER CONSUMPTION STATISTICS" "C:\Users\[TU_USUARIO]\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\Player.log" -Context 0,15
```

### 9. Notas Importantes

- **El logging detallado NO afecta el rendimiento** - solo escribe cada 10 segundos
- Los valores se cachean por edificio, así que los cambios de configuración se aplican inmediatamente
- El mod se actualiza **cada 10 frames** para sobreescribir los resets del juego
- Los multiplicadores son **acumulativos**: `Final = Base × SizeMultiplier × UserMultiplier`

### 10. Solución de Problemas

**No veo cambios en el consumo:**
- Verifica que el mod está habilitado (Mod Settings)
- Busca "Realistic Industrial Power Consumption" en los logs
- Verifica que hay edificios industriales en tu ciudad

**Consumo parece aleatorio:**
- Es normal - cada edificio tiene variación dentro de su categoría de tamaño
- La variación es consistente por edificio (usa el índice de entidad como semilla)

**Logs no muestran estadísticas:**
- Espera ~10 segundos después de cargar/iniciar
- Asegúrate de tener al menos 1 edificio industrial construido
- Busca errores en el log que indiquen problemas

---

## Feedback para Balanceo Final

Al probar, anota:
1. Multiplicador de usuario usado: _____%
2. Cantidad de edificios industriales: _____
3. Consumo total de la zona industrial: _____ MW
4. ¿Apagones frecuentes? Sí / No
5. ¿Balance divertido? Muy fácil / Fácil / Bien / Difícil / Muy difícil

Esto ayudará a encontrar los valores óptimos para el lanzamiento público del mod.
