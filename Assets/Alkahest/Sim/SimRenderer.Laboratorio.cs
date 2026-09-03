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
    /// HITO OPUS H5: las VISTAS de depuración (temperatura, humedad, carga,
    /// reposo, luz, chunks) como cuarta textura siguiendo el patrón de
    /// _veloTexture (Init 392-398, BuildQuad 751-758, RenderChunk, Apply).
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
    }
}
