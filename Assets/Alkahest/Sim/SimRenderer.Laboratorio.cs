using UnityEngine;

namespace Alkahest.Sim
{
    /// <summary>
    /// (R130) TINTE DEL LABORATORIO: lo que los campos nuevos le hacen al
    /// color de una celda, para que el jugador pueda LEER turbidez, mojado y
    /// savia sin abrir ninguna vista de depuración. Parte de SimRenderer
    /// (partial); ComputeCellColor lo llama al final solo si
    /// <see cref="LabTinteActivo"/> (lo pone AlkahestSim al crear el mundo).
    /// Regla P1 respetada a medias y a propósito: el color de REFERENCIA no
    /// cambia, pero el agua turbia y la arena mojada SÍ se leen distintas —
    /// esa información es el juego del laboratorio.
    ///
    /// (R131, H2) Debajo del tinte viven las VISTAS DE DEPURACIÓN: una cuarta
    /// textura, encima de todo, que pinta el campo que se elija (temperatura,
    /// humedad, carga, reposo, luz, chunks) en vez de la materia. Es el
    /// microscopio del laboratorio: sin ella, la mitad de lo que pasa —el
    /// vapor del aire, la colmatación de un poroso, la quietud de una poza—
    /// no tiene forma de mirarse.
    /// </summary>
    public sealed partial class SimRenderer
    {
        public static bool LabTinteActivo;

        private void LabTinte(byte matId, int idx, ref byte r, ref byte g, ref byte b, ref byte alfa)
        {
            switch (matId)
            {
                case MaterialId.Water:
                {
                    int c = _grid.carga[idx];
                    if (c > 0)
                    {
                        // Hacia pardo turbio (120,100,60): a carga 255, ~75 % del camino.
                        r = (byte)(r + (120 - r) * c / 340);
                        g = (byte)(g + (100 - g) * c / 340);
                        b = (byte)(b + (60 - b) * c / 340);
                    }
                    int v = _grid.humedad[idx];
                    if (v < 200) alfa = (byte)(alfa * (90 + v * 165 / 200) / 255); // celda a medio evaporar: más transparente.
                    break;
                }
                case MaterialId.Sand:
                case MaterialId.Sedimento:
                case MaterialId.Grava:
                case MaterialId.Ash:
                case MaterialId.Fibra:
                case MaterialId.Arcilla:
                case MaterialId.Semilla:
                case MaterialId.Arenisca: // (R131) el frente mojado bajando por la arenisca SE VE: es media demostración del filtro.
                {
                    int h = _grid.humedad[idx];
                    if (h > 0)
                    {
                        int k = 255 - h * 95 / 255; // mojado: hasta -37 % de brillo y un poco de frío.
                        r = (byte)(r * k / 255);
                        g = (byte)(g * k / 255);
                        b = (byte)Mathf.Min(255, b * k / 255 + h / 12);
                    }
                    if (matId == MaterialId.Sedimento)
                    {
                        int f = _grid.carga[idx]; // fertilidad: tira a pardo oscuro rico.
                        if (f > 0) { r = (byte)(r - r * f / 900); b = (byte)(b - b * f / 700); }
                    }
                    break;
                }
                case MaterialId.Planta:
                {
                    int s = _grid.humedad[idx]; // savia: sin ella, la planta amarillea.
                    if (s < 80)
                    {
                        int t = 80 - s; // 0..80
                        r = (byte)Mathf.Min(255, r + t);
                        g = (byte)(g - t / 3);
                        b = (byte)(b - b * t / 160);
                    }
                    // (R134) El BROTE es más claro que el tallo: `aux` es la altura sobre
                    // la raíz, así que la parte joven de la planta se lee de un vistazo.
                    int alt = _grid.aux[idx];
                    if (alt > 0)
                    {
                        int k = alt > 12 ? 12 : alt; // satura pronto: no queremos una planta blanca.
                        r = (byte)Mathf.Min(255, r + k * 4);
                        g = (byte)Mathf.Min(255, g + k * 6);
                        b = (byte)Mathf.Min(255, b + k * 3);
                    }
                    break;
                }
                case MaterialId.Stone:
                case MaterialId.Terracota:
                {
                    int h = _grid.humedad[idx]; // rocío: la roca sudando.
                    if (h > 40) { int k = 255 - h * 60 / 255; r = (byte)(r * k / 255); g = (byte)(g * k / 255); }
                    break;
                }
            }
        }

        // =================================================================
        // (R131, H2) LAS VISTAS DE DEPURACIÓN — la cuarta textura
        // =================================================================
        // Mismo patrón exacto que _veloTexture (R129): una Texture2D del
        // tamaño del mundo, un SpriteRenderer propio, se rellena en el mismo
        // barrido por chunks (RenderChunk) y sube a GPU al mismo ritmo. Cuatro
        // líneas en SimRenderer.cs (la excepción documentada del HANDOFF §2);
        // todo lo demás vive aquí.
        //
        // COSTE CERO CUANDO NO SE USA: la textura no se crea hasta que alguien
        // elige una vista, y el sprite se apaga al volver a Ninguna. Fuera del
        // laboratorio nadie toca VistaLab, así que el juego normal ni se entera.
        //
        // LO QUE HAY QUE SABER AL LEERLA (y está en la ayuda del panel): los
        // chunks DORMIDOS no repintan, así que un campo que cambia sin cambiar
        // la materia (la temperatura de una roca, el vapor del aire quieto)
        // puede ir hasta 30 frames por detrás — ese es el refresco completo de
        // SimRenderer (FullRefreshEveryFrames). No es un fallo de la vista: es
        // que el mundo, ahí, está dormido.

        /// <summary>Qué campo dibuja la cuarta textura. Ninguna = apagada (y sin textura creada).</summary>
        public static VistaLaboratorio VistaLab
        {
            get => _vistaLab;
            set { if (_vistaLab != value) { _vistaLab = value; _vistaLabCambiada = true; } }
        }
        private static VistaLaboratorio _vistaLab;
        private static bool _vistaLabCambiada;

        private Texture2D _vistaTexture;
        private Color32[] _vistaScratch;
        private SpriteRenderer _vistaSr;
        /// <summary>Alfa de la vista: deja adivinar la materia debajo sin competir con ella.</summary>
        private const byte VistaAlfa = 150;

        /// <summary>Lo llama RenderFrame antes de barrer: crea la textura la primera vez, enciende/apaga el sprite y fuerza el repintado al cambiar de vista.</summary>
        private void LabVistaAntesDelFrame()
        {
            if (!_vistaLabCambiada) return;
            _vistaLabCambiada = false;

            bool activa = _vistaLab != VistaLaboratorio.Ninguna;
            if (activa && _vistaTexture == null)
            {
                _vistaTexture = new Texture2D(CellGrid.W, CellGrid.H, TextureFormat.RGBA32, false, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "AlkahestVistaLabTexture",
                };
                _vistaScratch = new Color32[CellGrid.CHUNK * CellGrid.CHUNK];
                var go = new GameObject("AlkahestVistaLabSprite");
                _vistaSr = go.AddComponent<SpriteRenderer>();
                _vistaSr.sprite = Sprite.Create(_vistaTexture, new Rect(0, 0, CellGrid.W, CellGrid.H),
                    Vector2.zero, 1f / CellWorldSize, 0, SpriteMeshType.FullRect);
                _vistaSr.sortingOrder = 54; // encima del velo (52) y del personaje (50), debajo de ArquitecturaFrente (55).
                go.transform.SetParent(transform, false);
                go.transform.position = Vector3.zero;
            }
            if (_vistaSr != null) _vistaSr.enabled = activa;
            MarcarTodoSucio(); // la vista nueva tiene que llegar a TODOS los chunks, dormidos incluidos.
        }

        /// <summary>true mientras haya que rellenar el scratch de la vista (lo consulta RenderChunk).</summary>
        private bool LabVistaRellenando => _vistaScratch != null && _vistaLab != VistaLaboratorio.Ninguna;

        /// <summary>El color de una celda en la vista activa. Rampas pensadas para LEERSE, no para ser bonitas.</summary>
        private Color32 LabVistaColor(int x, int y, int idx)
        {
            switch (_vistaLab)
            {
                case VistaLaboratorio.Temperatura:
                {
                    // Diferencia con el AMBIENTE de esa celda (el laboratorio tiene
                    // clima por celda): gris donde no pasa nada, azul lo frío, rojo
                    // lo caliente. Saturado a ±40 raw = ±80 °C.
                    int d = _grid.temp[idx] - _grid.ambient[idx];
                    int k = Mathf.Clamp(Mathf.Abs(d) * 255 / 40, 0, 255);
                    if (d > 0) return new Color32((byte)(110 + k * 145 / 255), (byte)(110 - k * 80 / 255), (byte)(110 - k * 95 / 255), VistaAlfa);
                    if (d < 0) return new Color32((byte)(110 - k * 90 / 255), (byte)(110 - k * 20 / 255), (byte)(110 + k * 145 / 255), VistaAlfa);
                    return new Color32(110, 110, 110, VistaAlfa);
                }
                case VistaLaboratorio.Humedad:
                {
                    int h = _grid.humedad[idx];
                    if (h == 0) return default;
                    return new Color32(0, (byte)(h * 210 / 255), (byte)(60 + h * 195 / 255), VistaAlfa); // negro → cian
                }
                case VistaLaboratorio.Carga:
                {
                    int c = _grid.carga[idx];
                    if (c == 0) return default;
                    return new Color32((byte)(70 + c * 185 / 255), (byte)(40 + c * 130 / 255), 0, VistaAlfa); // negro → ámbar
                }
                case VistaLaboratorio.Reposo:
                {
                    int r = _grid.reposo[idx];
                    if (r == 0) return default;
                    return new Color32((byte)(60 + r * 140 / 255), 0, (byte)(80 + r * 175 / 255), VistaAlfa); // negro → violeta
                }
                case VistaLaboratorio.Luz:
                {
                    int l = _grid.luz[idx];
                    if (l == 0) return default;
                    return new Color32((byte)l, (byte)l, (byte)(l * 240 / 255), VistaAlfa); // negro → blanco
                }
                case VistaLaboratorio.Chunks:
                {
                    // Verde lo despierto (lo que de verdad cuesta cada tick),
                    // nada lo dormido: el mapa del gasto por tick.
                    return _grid.IsChunkAwake(x / CellGrid.CHUNK, y / CellGrid.CHUNK)
                        ? new Color32(40, 200, 70, 90) : default;
                }
            }
            return default;
        }

        /// <summary>Rellena una celda del scratch de la vista (lo llama RenderChunk dentro de su bucle).</summary>
        private void LabVistaCelda(int x, int y, int idx, int scratchI)
        {
            _vistaScratch[scratchI] = LabVistaColor(x, y, idx);
        }

        /// <summary>Sube el chunk recién calculado a la textura de la vista (lo llama RenderChunk al final).</summary>
        private void LabVistaSetPixels(int x0, int y0, int w, int h)
        {
            if (LabVistaRellenando) _vistaTexture.SetPixels32(x0, y0, w, h, _vistaScratch);
        }

        /// <summary>Sube la textura a GPU al mismo ritmo que las otras tres.</summary>
        private void LabVistaApply()
        {
            if (LabVistaRellenando) _vistaTexture.Apply(false);
        }
    }

    /// <summary>(R131, H2) Los campos que la cuarta textura sabe dibujar. Ninguna = vista apagada.</summary>
    public enum VistaLaboratorio
    {
        Ninguna, Temperatura, Humedad, Carga, Reposo, Luz, Chunks
    }
}
