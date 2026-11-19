# DeliveryRoutes — la ruta más corta para un domiciliario
 
Programa de consola en C# / .NET que lee el mapa de una ciudad (`city.txt`) y la lista de tareas de 30 paquetes (`packages.txt`) y le arma al domiciliario la ruta para recoger y entregar todo recorriendo la menor distancia posible. Muestra paso a paso a dónde ir, qué hacer en cada punto, por qué calles pasa y el total en kilómetros.
 
## Cómo ejecutarlo
 
Con los argumentos (mapa, paquetes y posición inicial):
 
```
dotnet run -- city.txt packages.txt D
```
 
Sin argumentos, el programa pide las rutas de los dos archivos y la posición inicial por consola:
 
```
dotnet run
```
 
Ejemplo pequeño de la sección 6 del taller:
 
```
dotnet run -- city_mini.txt packages_mini.txt A
```
 
Los cuatro archivos `.txt` se copian automáticamente a la carpeta de salida (`bin/`), así que también funciona al darle F5 en Visual Studio y escribir `city.txt`, `packages.txt` y `D` cuando el programa los pida.
 
## Estructura del proyecto
 
```
DeliveryRoutes/
|-- Program.cs            <- lee argumentos o consola, llama al backend e imprime
|-- city.txt, packages.txt, city_mini.txt, packages_mini.txt
|-- Backend/
    |-- Edge.cs           <- una calle que sale de un vértice (destino + metros)
    |-- Graph.cs          <- grafo con lista de adyacencia
    |-- CityFile.cs       <- carga y valida city.txt (TwoWay = 2 aristas, OneWay = 1)
    |-- ShortestPaths.cs  <- Dijkstra con PriorityQueue y reconstrucción del camino
    |-- Package.cs        <- record Package(Id, Pickup, Delivery)
    |-- PackageFile.cs    <- carga y valida packages.txt, agrupa las tareas por paquete
    |-- Stop.cs           <- una parada: ubicación + tipo (recoger/entregar) + paquete
    |-- RoutePlanner.cs   <- tabla de distancias + heurística para ordenar las paradas
    |-- RoutePrinter.cs   <- arma la salida con el formato del taller
```
 
## Estrategia 
 
### 1. Distancias exactas con Dijkstra
 
El mapa es un grafo dirigido cada calle de un solo sentido es una sola arista de un punto de partida a uno final y cada calle de doble calzada son dos aristas, una en cada sentido. Por eso la distancia de X a Y puede ser distinta a la de Y a X.
 
No necesito las distancias entre las 100 ubicaciones, solo entre las que importan la posición inicial y los puntos de recogida y entrega que son máximo 61 entonces corro Dijkstra una vez desde cada uno de esos puntos y guardo dos cosas: la tabla `distance[x, y]` con los metros mínimos, y los predecesores de cada Dijkstra para poder reconstruir el camino y mostrarlo . Con esa tabla, el resto del algoritmo ya no vuelve a tocar el grafo.
 
No sirve BFS porque BFS cuenta cantidad de calles, no metros: el camino con menos calles no siempre es el mas corto.
 
### 2. Por qué no se prueban todas las combinaciones
 
Son 60 paradas 30 de  recogidas y  30 de  entregas. Aun respetando que cada paquete se recoja antes de entregarse, quedan 60! / 2^30 órdenes posibles, un número de 73 cifras es imposible revisarlas todas. Este problema es una variante del agente viajero llamada *Pickup and Delivery Problem*, y no se conoce un algoritmo exacto y rápido para tamaños grandes. Por eso uso una heurística no garantizi el camino mas óptimo pero encuentra una ruta muy buena en pocos segundos.
 
### 3. La heurística para ordenar las paradas
 
1. **Construir una ruta por inserción.** Tomo los paquetes en un orden y los voy metiendo uno por uno. Para cada paquete pruebo todas las posiciones donde puede ir su recogida y su entrega la recogida siempre antes que la entrega y me quedo con la que menos metros agrega a la ruta
2. **Mejorar la ruta con cambios locales**, repitiendo mientras algo mejore:
   - *Reubicar un paquete*: saco su recogida y su entrega y las vuelvo a insertar en la mejor posición.
   - *Mover una parada*: saco una sola parada y pruebo ponerla en otro lugar.
3. **Reiniciar muchas veces.** El resultado depende del orden en que se insertan los paquetes, así que repito todo 1000 veces con órdenes aleatorios distintos y me quedo con la mejor ruta. Uso `Random` con semilla fija (12345), por eso el resultado es siempre el mismo.

5. ### . Verificación
 
Antes de imprimir, `IsValid` comprueba que cada paquete se recoge exactamente una vez, se entrega exactamente una vez, y la entrega es después de la recogida. Además, el total que se imprime es la suma de los tramos que se muestran.
 
### Resultados con `city.txt`, `packages.txt` y posición inicial `D`
 
| Estrategia | Recorrido total |
|---|---|
| Vecino más cercano, sin mejoras (`PlanNearestNeighbor`) | 40.52 km |
| Inserción + reubicar + mover parada + 1000 reinicios (`Plan`) | **30.15 km** |
 
El programa tarda alrededor de 4 a 5 segundos en encontrar la ruta en mi computadora peude que en otra con mejores componentes tarde menos.
 

## Salida completa (city.txt, packages.txt, posición inicial D) ##
```
Posición inicial: D
Paquetes: 30 Ubicaciones: 100 Calles: 142 (30 de un solo sentido)
 
1. En D: recoger paquete 18.
2. Ir de D a BE (620 m): recoger paquete 7.
   Camino: D -> BT -> BE
3. Ir de BE a X (490 m): recoger paquete 12.
   Camino: BE -> E -> X
4. Ir de X a BU (560 m): recoger paquete 8.
   Camino: X -> B -> BU
5. Ir de BU a CN (430 m): recoger paquetes 1, 2 y 3.
   Camino: BU -> CN
6. Ir de CN a BU (430 m): recoger paquete 28.
   Camino: CN -> BU
7. Ir de BU a AM (350 m): recoger paquete 30.
   Camino: BU -> AM
8. Ir de AM a AX (270 m): recoger paquete 17.
   Camino: AM -> AK -> AX
9. Ir de AX a AQ (170 m): recoger paquete 9.
   Camino: AX -> AQ
10. Ir de AQ a BS (250 m): recoger paquete 25.
   Camino: AQ -> BS
11. Ir de BS a CH (460 m): recoger paquete 10.
   Camino: BS -> CT -> CJ -> CH
12. Ir de CH a CS (770 m): recoger paquete 19.
   Camino: CH -> CJ -> CT -> BS -> CS
13. Ir de CS a AP (770 m): recoger paquete 5.
   Camino: CS -> AE -> AP
14. Ir de AP a AT (350 m): recoger paquete 27.
   Camino: AP -> AT
15. Ir de AT a AB (1470 m): recoger paquete 4.
   Camino: AT -> AQ -> AX -> F -> BV -> AB
16. Ir de AB a BV (470 m): entregar paquete 19.
   Camino: AB -> BV
17. Ir de BV a BW (1890 m): entregar paquete 30.
   Camino: BV -> F -> AX -> S -> K -> N -> BW
18. Ir de BW a CL (270 m): recoger paquete 14.
   Camino: BW -> CL
19. Ir de CL a R (360 m): recoger paquetes 6 y 21.
   Camino: CL -> R
20. Ir de R a BN (810 m): entregar paquete 7.
   Camino: R -> CL -> BN
21. Ir de BN a AO (1300 m): entregar paquete 3 y recoger paquete 11.
   Camino: BN -> Y -> AV -> CG -> AI -> AO
22. Ir de AO a AI (170 m): recoger paquete 24.
   Camino: AO -> AI
23. Ir de AI a BM (550 m): entregar paquetes 2 y 10.
   Camino: AI -> AS -> BM
24. Ir de BM a BI (1750 m): recoger paquete 13.
   Camino: BM -> AS -> BH -> BL -> CO -> BI
25. Ir de BI a Q (1380 m): recoger paquetes 22 y 29.
   Camino: BI -> CO -> AZ -> V -> O -> BY -> Q
26. Ir de Q a AA (1450 m): entregar paquete 12.
   Camino: Q -> BJ -> CI -> Z -> AA
27. Ir de AA a CP (1030 m): entregar paquete 9.
   Camino: AA -> Z -> CA -> BT -> CP
28. Ir de CP a AD (670 m): entregar paquete 5.
   Camino: CP -> BT -> P -> CB -> AD
29. Ir de AD a AR (280 m): entregar paquete 8 y recoger paquete 20.
   Camino: AD -> AR
30. Ir de AR a BB (150 m): recoger paquete 16.
   Camino: AR -> BB
31. Ir de BB a AJ (400 m): entregar paquete 29 y recoger paquetes 23 y 26.
   Camino: BB -> AJ
32. Ir de AJ a AD (830 m): entregar paquete 13.
   Camino: AJ -> BB -> AR -> AD
33. Ir de AD a AC (1200 m): entregar paquete 17.
   Camino: AD -> CB -> P -> BT -> D -> AC
34. Ir de AC a CD (130 m): entregar paquete 25.
   Camino: AC -> CD
35. Ir de CD a BY (810 m): entregar paquete 16.
   Camino: CD -> BA -> BG -> BY
36. Ir de BY a BF (1170 m): entregar paquetes 18 y 23.
   Camino: BY -> O -> I -> BG -> BA -> AG -> BF
37. Ir de BF a BE (710 m): entregar paquete 21.
   Camino: BF -> X -> E -> BE
38. Ir de BE a T (440 m): entregar paquetes 24 y 27.
   Camino: BE -> T
39. Ir de T a CU (1360 m): recoger paquete 15.
   Camino: T -> CF -> W -> CV -> BX -> CU
40. Ir de CU a CV (360 m): entregar paquete 28.
   Camino: CU -> CV
41. Ir de CV a M (700 m): entregar paquete 1.
   Camino: CV -> W -> CM -> M
42. Ir de M a AE (430 m): entregar paquete 26.
   Camino: M -> CM -> AE
43. Ir de AE a AP (370 m): entregar paquetes 11 y 22.
   Camino: AE -> AP
44. Ir de AP a AT (350 m): entregar paquetes 14 y 15.
   Camino: AP -> AT
45. Ir de AT a AQ (200 m): entregar paquete 6.
   Camino: AT -> AQ
46. Ir de AQ a CT (350 m): entregar paquete 4.
   Camino: AQ -> BS -> CT
47. Ir de CT a CR (420 m): entregar paquete 20.
   Camino: CT -> CR
Todos los paquetes fueron recogidos y entregados.
Recorrido total: 30.15 km
```
 
## Salida del ejemplo pequeño (`city_mini.txt`, `packages_mini.txt`, posición inicial `A`)
 
El total es 2.44 km, igual al óptimo del enunciado. La ruta es distinta a la del ejemplo (empieza por `C` en vez de `E`), pero tiene exactamente la misma distancia, así que también es óptima.
 
```
Posición inicial: A
Paquetes: 3 Ubicaciones: 7 Calles: 7 (1 de un solo sentido)
 
1. Ir de A a C (400 m): recoger paquete 1.
   Camino: A -> B -> C
2. Ir de C a E (350 m): recoger paquete 2.
   Camino: C -> B -> E
3. Ir de E a G (270 m): entregar paquete 1 y recoger paquete 3.
   Camino: E -> F -> G
4. Ir de G a A (820 m): entregar paquete 3.
   Camino: G -> F -> E -> B -> A
5. Ir de A a D (600 m): entregar paquete 2.
   Camino: A -> B -> C -> D
Todos los paquetes fueron recogidos y entregados.
Recorrido total: 2.44 km
```
 
