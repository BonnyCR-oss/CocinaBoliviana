using System.Collections.Generic;
using UnityEngine;
using CocinaBoliviana.Data;

namespace CocinaBoliviana
{
    /// <summary>Un pedido vivo en la cocina, con lo que le falta y su cuenta atrás.</summary>
    public class PedidoActivo
    {
        public readonly List<DishData> Todos = new List<DishData>();
        public readonly List<DishData> Pendientes = new List<DishData>();
        public float TiempoRestante;
        public float TiempoTotal;
        public int Recompensa;

        public bool Completo => Pendientes.Count == 0;
        public float Progreso => (TiempoTotal <= 0f) ? 0f : Mathf.Clamp01(TiempoRestante / TiempoTotal);

        /// <summary>true si este pedido aún esperaba ese plato y se le tacha.</summary>
        public bool Tachar(DishData plato)
        {
            int i = Pendientes.IndexOf(plato);
            if (i < 0) return false;
            Pendientes.RemoveAt(i);
            return true;
        }
    }

    /// <summary>
    /// Genera los pedidos, les lleva el tiempo y decide si un plato entregado sirve.
    ///
    /// El emparejado es automático contra el pedido MÁS ANTIGUO que siga esperando ese plato.
    /// No se le pide al jugador que elija: en VR va con las manos ocupadas llevando comida, y
    /// además el más antiguo es siempre el que está por caducar, así que no hay decisión real.
    /// </summary>
    public class OrderManager : MonoBehaviour
    {
        [Header("De dónde salen los pedidos")]
        [Tooltip("Sus 'comidas' y 'refrescos' son el menú. Si tiene 'pedidos' escritos a mano, " +
                 "se usan esos en vez de generarlos al azar.")]
        [SerializeField] private DepartmentData departamento;

        [Header("Ritmo")]
        [SerializeField] private int maxPedidosActivos = 4;
        [SerializeField] private float segundosEntrePedidos = 25f;
        [SerializeField] private float primerPedidoTrasSegundos = 5f;

        [Tooltip("Cuánto aguanta un pedido antes de caducar, si el plato no define el suyo.")]
        [SerializeField] private float tiempoPorDefecto = 120f;

        [Tooltip("Cuántos elementos puede pedir un ticket: comida, refresco o los dos.")]
        [SerializeField] private int maxElementosPorPedido = 2;

        [Header("Referencias")]
        [SerializeField] private OrderBoard tablero;

        private readonly List<PedidoActivo> activos = new List<PedidoActivo>();
        private float proximoPedido;
        private int puntosInternos;

        public int Puntos => (LevelManager.Instance != null) ? LevelManager.Instance.Puntos : puntosInternos;
        public IReadOnlyList<PedidoActivo> Activos => activos;

        private static OrderManager cache;
        public static OrderManager Instancia
        {
            get
            {
                if (cache == null) cache = FindAnyObjectByType<OrderManager>();
                return cache;
            }
        }

        private void Awake()
        {
            cache = this;
            proximoPedido = primerPedidoTrasSegundos;
            puntosInternos = 0;
        }

        private void Update()
        {
            // Solo procesar cuenta atrás y generación de pedidos si el nivel está en curso
            if (LevelManager.Instance == null || LevelManager.Instance.IsPlaying)
            {
                ActualizarTiempos();
                GenerarSiTocaOtro();
            }

            if (tablero != null) tablero.Refrescar(activos, Puntos);
        }

        private void ActualizarTiempos()
        {
            for (int i = activos.Count - 1; i >= 0; i--)
            {
                activos[i].TiempoRestante -= Time.deltaTime;
                if (activos[i].TiempoRestante > 0f) continue;

                Debug.Log($"[OrderManager] Pedido caducado: {Describir(activos[i])}");

                if (LevelManager.Instance != null)
                {
                    LevelManager.Instance.RegistrarPedidoPerdido(activos[i]);
                }

                activos.RemoveAt(i);
            }
        }

        private void GenerarSiTocaOtro()
        {
            if (activos.Count >= maxPedidosActivos) return;

            proximoPedido -= Time.deltaTime;
            if (proximoPedido > 0f) return;

            proximoPedido = segundosEntrePedidos;
            CrearPedido();
        }

        private void CrearPedido()
        {
            if (departamento == null)
            {
                Debug.LogWarning("[OrderManager] Sin 'departamento' asignado; no hay menú del que sacar pedidos.");
                enabled = false;
                return;
            }

            var pedido = new PedidoActivo();

            // Si hay pedidos escritos a mano en el departamento, se respetan tal cual.
            if (departamento.pedidos != null && departamento.pedidos.Count > 0)
            {
                OrderData plantilla = departamento.pedidos[Random.Range(0, departamento.pedidos.Count)];
                if (plantilla != null && plantilla.platosSolicitados != null)
                {
                    foreach (var plato in plantilla.platosSolicitados)
                    {
                        if (plato != null) pedido.Todos.Add(plato);
                    }
                    pedido.Recompensa = plantilla.recompensa;
                    pedido.TiempoTotal = (plantilla.tiempoLimite > 0f) ? plantilla.tiempoLimite : tiempoPorDefecto;
                }
            }
            else
            {
                ArmarPedidoAlAzar(pedido);
            }

            if (pedido.Todos.Count == 0)
            {
                Debug.LogWarning("[OrderManager] El departamento no tiene comidas ni refrescos; no hay nada que pedir.");
                enabled = false;
                return;
            }

            if (pedido.TiempoTotal <= 0f) pedido.TiempoTotal = tiempoPorDefecto;
            pedido.TiempoRestante = pedido.TiempoTotal;
            pedido.Pendientes.AddRange(pedido.Todos);

            activos.Add(pedido);
            Debug.Log($"[OrderManager] Nuevo pedido: {Describir(pedido)} ({pedido.TiempoTotal:0}s)");
        }

        private void ArmarPedidoAlAzar(PedidoActivo pedido)
        {
            var menu = new List<DishData>();
            if (departamento.comidas != null) menu.AddRange(departamento.comidas);
            if (departamento.refrescos != null) menu.AddRange(departamento.refrescos);
            menu.RemoveAll(d => d == null);
            if (menu.Count == 0) return;

            int cuantos = Mathf.Clamp(Random.Range(1, maxElementosPorPedido + 1), 1, menu.Count);

            int puntos = 0;
            float tiempo = 0f;
            for (int i = 0; i < cuantos; i++)
            {
                // Se saca de la lista al elegirlo: un ticket pide cosas distintas, no
                // "Silpancho + Silpancho".
                int indice = Random.Range(0, menu.Count);
                DishData elegido = menu[indice];
                menu.RemoveAt(indice);

                pedido.Todos.Add(elegido);
                puntos += Mathf.Max(elegido.puntos, 0);

                // El tiempo lo marca el plato más lento del ticket: si no, un pedido con dos
                // platos sería más difícil que la suma de sus partes.
                tiempo = Mathf.Max(tiempo, elegido.tiempoLimite);
            }

            pedido.Recompensa = puntos;
            pedido.TiempoTotal = tiempo;
        }

        /// <summary>
        /// Intenta colocar un plato en el pedido más antiguo que lo espere.
        /// Devuelve false si ningún pedido lo pedía.
        /// </summary>
        public bool Entregar(DishData plato)
        {
            if (plato == null) return false;

            // activos está en orden de creación, así que el primero que encaje es el más viejo.
            foreach (var pedido in activos)
            {
                if (!pedido.Tachar(plato)) continue;

                if (pedido.Completo)
                {
                    if (LevelManager.Instance != null)
                    {
                        LevelManager.Instance.RegistrarEntregaExitosa(pedido.Recompensa, plato);
                    }
                    else
                    {
                        puntosInternos += pedido.Recompensa;
                    }

                    activos.Remove(pedido);
                    Debug.Log($"[OrderManager] Pedido completo. +{pedido.Recompensa} puntos (total {Puntos}).");
                }
                else
                {
                    Debug.Log($"[OrderManager] {plato.nombre} entregado. Al pedido le falta: {Describir(pedido)}");
                }
                return true;
            }

            Debug.Log($"[OrderManager] Nadie pidió {plato.nombre}.");
            return false;
        }

        private static string Describir(PedidoActivo pedido)
        {
            var nombres = new List<string>();
            foreach (var d in pedido.Pendientes)
            {
                if (d != null) nombres.Add(d.nombre);
            }
            return (nombres.Count == 0) ? "(nada)" : string.Join(" + ", nombres);
        }
    }
}
