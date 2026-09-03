using System;
using System.Diagnostics;

namespace Alkahest.Sim
{
    /// <summary>
    /// (R130) LAS PASADAS DEL LABORATORIO DE LEYES — docs/LAB/DISENO_LABORATORIO.md.
    ///
    /// Parte de SimStepper (partial). Se ejecuta SOLO con <see cref="LabActivo"/>
    /// (ModoLaboratorio); en el juego normal ninguna línea de este archivo corre.
    /// Cuatro pasadas tras MorphTick:
    ///
    ///   1) <see cref="LabCampos"/>  — los procesos LENTOS, 1/8 de TODA la grilla
    ///      por tick (mismo estriado que DiffuseTemperature, sin depender del
    ///      sueño de chunks: una poza dormida sigue evaporando). Por material:
    ///      aire (vapor, condensación), agua (evaporación, infiltración,
    ///      decantación, depósito), porosos (percolación, capilaridad,
    ///      exudación, secado, compactación, ablandamiento, cocción, abono),
    ///      roca (rocío que gotea), planta (crecer/morir), hogar/frío/
    ///      manantial/sumidero (fuentes y desagües).
    ///   2) <see cref="LabPresion"/> — LA ÚNICA REGLA NO LOCAL: los cuerpos de
    ///      agua conectados igualan sus superficies (vasos comunicantes,
    ///      sifón, fuente artesiana).
    ///   3) <see cref="LabLuz"/>     — luz por máximo con decaimiento, cada N ticks.
    ///   4) <see cref="LabCuerpos"/> — cuerpos cohesionados (RocaSuelta): HITO
    ///      de Opus, aquí solo el gancho.
    ///
    /// CONSERVACIÓN: humedad/carga se mueven en UNIDADES (255 = una celda);
    /// toda transferencia resta en un sitio lo que suma en otro. Las únicas
    /// fuentes/sumideros son el Manantial y el Sumidero, y se cuentan.
    ///
    /// DETERMINISMO: las sales nuevas (601..631) están grep-verificadas
    /// contra SimStepper.cs (máx. 563 antes). Todo XorShift.FromCell(tick,x,y,sal).
    /// Escrituras a vecinos EN SITIO (mismo criterio que DiffuseTemperature:
    /// el orden es fijo, luego determinista).
    ///
    /// UNIDADES: "visita" = una pasada de LabCampos sobre la celda = cada 8
    /// ticks = 0,27 s a 30 Hz. Los números viven TODOS en LabParams.
    /// </summary>
    public sealed partial class SimStepper
    {
        /// <summary>true solo en ModoLaboratorio (lo pone AlkahestSim al crear el stepper).</summary>
        public bool LabActivo;

        // ---- Tiempos por fase (ms del último tick) --------------------------------
        private readonly Stopwatch _swFase = new Stopwatch();
        public double MsDifusion, MsBarrido, MsChunks, MsMorph, MsCampos, MsPresion, MsLuz, MsCuerpos;

        // ---- Libro mayor (contadores acumulados desde que nació el stepper) --------
        public long LabAguaEmitida, LabAguaSumida;
        public long LabEvaporado, LabCondensado, LabGoteos;
        public long LabDepositado, LabErosionado, LabInfiltrado, LabExudado;
        public long LabCompactado, LabAblandado, LabCocido, LabAbonado;
        public long LabPlantasNacidas, LabPlantasMuertas;
        public long LabPresionMovidas, LabCuerposCaidos, LabFracturas;
        /// <summary>(R131) AUDITORÍA DE CONSERVACIÓN. Suma de TODO lo que este stepper ha creado
        /// (+) o destruido (−) de humedad[] en unidades, contado en los dos únicos sitios que
        /// escriben humedad sin restarla en otro lado (LabNacerAgua y LabTransformar). El resto
        /// de las reglas son transferencias emparejadas, así que el invariante 3 del HANDOFF se
        /// comprueba con una sola resta: Σhumedad(t) − Σhumedad(0) DEBE ser exactamente esto.
        /// Con los contadores por CELDA no cuadraba: el sumidero traga celdas a medio llenar y
        /// el manantial las hace llenas, así que "255 × celdas" sobreestimaba el caudal real.</summary>
        public long LabBalanceU;
        /// <summary>(R131) Agua que el sumidero tragó, en UNIDADES (la de celdas, LabAguaSumida, cuenta celdas a medio llenar como enteras).</summary>
        public long LabAguaSumidaU;

        // ---- Sales propias (grep-verificadas: las de SimStepper.cs llegan a 563) --
        private const uint SalLabErosion = 601;
        private const uint SalLabManantial = 607;
        private const uint SalLabCompacta = 613;
        private const uint SalLabLatente = 617;
        private const uint SalLabPlanta = 619;
        private const uint SalLabCuerpo = 631;

        private int _labManantialCeldas = -1;

        private void LabPasadas()
        {
            _swFase.Restart();
            LabCampos();
            MsCampos = _swFase.Elapsed.TotalMilliseconds;

            _swFase.Restart();
            int cada = LabParams.PresionCadaTicks < 1 ? 1 : LabParams.PresionCadaTicks;
            if (LabParams.PresionActiva != 0 && (_tick % (uint)cada) == 0u) LabPresion();
            MsPresion = _swFase.Elapsed.TotalMilliseconds;

            _swFase.Restart();
            int cadaLuz = LabParams.LuzCadaTicks < 1 ? 1 : LabParams.LuzCadaTicks;
            if ((_tick % (uint)cadaLuz) == 0u) LabLuz();
            MsLuz = _swFase.Elapsed.TotalMilliseconds;

            _swFase.Restart();
            if (LabParams.CuerposActivos != 0) LabCuerpos();
            MsCuerpos = _swFase.Elapsed.TotalMilliseconds;
        }

        // =====================================================================
        // GANCHOS QUE LLAMA SimStepper.cs (barrido)
        // =====================================================================

        /// <summary>Un combustible con más agua que fuego.fibraMojadaMin no prende (ApplyPhase y TryIgnite). Solo en el laboratorio; fuera, false sin leer nada.</summary>
        private bool LabCombustibleMojado(int idx, MaterialDef def)
        {
            if (!LabActivo) return false;
            if (def.archetype == MaterialArchetype.Liquid) return false; // el aceite no "se moja".
            return _grid.humedad[idx] > LabParams.FibraMojadaMin;
        }

        /// <summary>
        /// El agua que ACABA DE MOVERSE (cayó o fluyó) arranca sedimento/arcilla
        /// vecino si lleva poca carga: la celda erosionada se vuelve AGUA TURBIA
        /// (carga 255). Simétrico del depósito: un ciclo erosión→transporte→
        /// depósito conserva el agua.
        /// </summary>
        private void LabErosion(int x, int y, int idx)
        {
            if (_grid.carga[idx] > LabParams.ErosionCargaMax) return;
            var rng = XorShift.FromCell(_tick, x, y, SalLabErosion);
            for (int d = 0; d < 4; d++)
            {
                int nx = x + DirX[d], ny = y + DirY[d];
                if (nx <= 0 || nx >= W - 1 || ny <= 0 || ny >= H - 1) continue;
                int j = CellGrid.Idx(nx, ny);
                byte m = _grid.mat[j];
                if (!LabMateriales.EsErosionable(m)) continue;
                int pct = m == MaterialId.Arcilla ? LabParams.ErosionArcillaPct : LabParams.ErosionPct;
                if (!rng.ChancePercent(pct)) continue;
                LabNacerAgua(j, _grid.temp[idx], 255);
                LabErosionado++;
                return; // una celda por evento: la erosión es un proceso, no un derrumbe.
            }
        }

        // =====================================================================
        // 1) LA PASADA DE CAMPOS
        // =====================================================================
        private void LabCampos()
        {
            if (_labManantialCeldas < 0 || (_tick & 63u) == 0u) LabContarFuentes();

            int offset = (int)(_tick % 8u);
            int n = W * H;
            var mat = _grid.mat;
            for (int i = offset; i < n; i += 8)
            {
                int x = i % W, y = i / W;
                if (x == 0 || x == W - 1 || y == 0 || y == H - 1) continue;
                byte m = mat[i];
                switch (m)
                {
                    case MaterialId.Empty: LabAire(x, y, i); break;
                    case MaterialId.Water: LabAgua(x, y, i); break;
                    case MaterialId.Sand:
                    case MaterialId.Grava:
                    case MaterialId.Sedimento:
                    case MaterialId.Ash:
                    case MaterialId.Fibra:
                    case MaterialId.Arcilla:
                    case MaterialId.Semilla:
                    case MaterialId.Arenisca: // (R131) roca porosa: percola, exuda limpio y se colmata.
                        LabPoroso(x, y, i, m); break;
                    case MaterialId.Stone:
                    case MaterialId.Terracota:
                    case MaterialId.PisoEstructural:
                    case MaterialId.RocaSuelta:
                        LabRoca(x, y, i); break;
                    case MaterialId.NucleoFrio: LabRoca(x, y, i); LabFrio(x, y, i); break;
                    case MaterialId.Planta: LabPlanta(x, y, i); break;
                    case MaterialId.Hogar: LabHogar(x, y, i); break;
                    case MaterialId.Manantial: LabManantial(x, y, i); break;
                    case MaterialId.Sumidero: LabSumidero(x, y, i); break;
                }
            }
        }

        /// <summary>Cuenta las celdas de Manantial CON CARA LIBRE (algún vecino vacío): solo ellas emiten, así el caudal pedido se reparte entre las que de verdad pueden darlo. Se recuenta cada 256 ticks (la cara puede quedar bajo el agua).</summary>
        private void LabContarFuentes()
        {
            int c = 0; var mat = _grid.mat;
            for (int i = W; i < mat.Length - W; i++)
            {
                if (mat[i] != MaterialId.Manantial) continue;
                if (mat[i + 1] == MaterialId.Empty || mat[i - 1] == MaterialId.Empty || mat[i - W] == MaterialId.Empty || mat[i + W] == MaterialId.Empty) c++;
            }
            _labManantialCeldas = c;
        }

        // ---- helpers de transformación (siempre despiertan el chunk) --------------
        private void LabTransformar(int idx, byte nuevo, int humedad, int carga)
        {
            LabBalanceU += humedad - _grid.humedad[idx]; // (R131) auditoría: lo que esta transformación crea o destruye.
            _grid.SetCell(idx, nuevo);
            _grid.humedad[idx] = (byte)humedad;
            _grid.carga[idx] = (byte)carga;
            _grid.reposo[idx] = 0;
            _grid.WakeChunk(idx % W, idx / W, _tick);
        }

        private void LabNacerAgua(int idx, byte tempRaw, int carga)
        {
            LabBalanceU += 255 - _grid.humedad[idx]; // (R131) auditoría: el agua nace llena (SetCell) sobre lo que hubiera.
            _grid.SetCell(idx, MaterialId.Water); // humedad = 255 (SetCell)
            _grid.temp[idx] = tempRaw;
            _grid.carga[idx] = (byte)carga;
            _grid.reposo[idx] = 0;
            _grid.WakeChunk(idx % W, idx / W, _tick);
        }

        private static bool LabEsAireOGas(byte m) => m == MaterialId.Empty || LabMateriales.EsGasId(m);

        /// <summary>Superficie sobre la que el vapor sobrante puede condensar: agua (suma volumen), poroso (se humedece), roca (rocío), núcleo frío (escarcha).</summary>
        private static bool LabEsSuperficieCondensable(byte m)
            => m == MaterialId.Water || LabMateriales.EsPoroso(m) || LabMateriales.EsRocaImpermeable(m) || m == MaterialId.NucleoFrio;

        /// <summary>Calor latente: evaporar enfría (signo -1), condensar calienta (+1). Parte entera + fracción por dado determinista.</summary>
        private void LabLatente(int idx, int unidades, int signo)
        {
            int q = unidades * LabParams.Latente;
            if (q <= 0) return;
            int ent = q / 255, frac = q % 255;
            if (frac > 0)
            {
                var rng = XorShift.FromCell(_tick, idx % W, idx / W, SalLabLatente);
                if (rng.Next(255) < frac) ent++;
            }
            if (ent == 0) return;
            int v = _grid.temp[idx] + signo * ent;
            if (v < 0) v = 0; else if (v > 255) v = 255;
            _grid.temp[idx] = (byte)v;
        }

        /// <summary>Primer vecino VACÍO en el orden pedido (abajo primero para gotear/exudar; derecha primero para emitir). -1 si ninguno.</summary>
        private int LabVecinoVacio(int idx, bool abajoPrimero, bool permitirArriba)
        {
            var mat = _grid.mat;
            if (abajoPrimero)
            {
                if (mat[idx - W] == MaterialId.Empty) return idx - W;
                if (mat[idx - 1] == MaterialId.Empty) return idx - 1;
                if (mat[idx + 1] == MaterialId.Empty) return idx + 1;
                if (permitirArriba && mat[idx + W] == MaterialId.Empty) return idx + W;
                return -1;
            }
            if (mat[idx + 1] == MaterialId.Empty) return idx + 1;
            if (mat[idx - 1] == MaterialId.Empty) return idx - 1;
            if (mat[idx - W] == MaterialId.Empty) return idx - W;
            if (permitirArriba && mat[idx + W] == MaterialId.Empty) return idx + W;
            return -1;
        }

        /// <summary>Roca con rocío a 255: suelta UNA celda de agua en el vecino vacío (abajo = gotea del techo; lado = rezuma la pared).</summary>
        private void LabGotear(int idx)
        {
            int j = LabVecinoVacio(idx, abajoPrimero: true, permitirArriba: false);
            if (j < 0) return; // saturada sin sitio: espera.
            LabNacerAgua(j, _grid.temp[idx], 0);
            LabBalanceU -= _grid.humedad[idx]; // (R131) el rocío que se va del techo, auditado.
            _grid.humedad[idx] = 0;
            LabGoteos++;
        }

        // ---- AIRE ---------------------------------------------------------------
        private void LabAire(int x, int y, int i)
        {
            var hum = _grid.humedad; var mat = _grid.mat;
            int h = hum[i];
            int up = i + W, down = i - W, left = i - 1, right = i + 1;

            // 1) Difusión CONSERVATIVA del vapor con vecinos de aire/gas.
            int D = LabParams.VaporDifusion < 1 ? 1 : LabParams.VaporDifusion;
            h = LabIntercambioVapor(h, up, D);
            h = LabIntercambioVapor(h, down, D);
            h = LabIntercambioVapor(h, left, D);
            h = LabIntercambioVapor(h, right, D);

            // 2) Ascenso: el vapor pesa menos que el aire.
            if (LabParams.VaporAscenso > 0 && LabEsAireOGas(mat[up]))
            {
                int hu = hum[up];
                if (hu < h)
                {
                    int t = (h - hu + 1) / 2;
                    if (t > LabParams.VaporAscenso) t = LabParams.VaporAscenso;
                    if (t > 255 - hu) t = 255 - hu;
                    hum[up] = (byte)(hu + t); h -= t;
                }
            }

            // 3) Condensación: por encima de la saturación, el exceso pasa a la
            //    superficie vecina (techo primero: es donde se acumula el vapor).
            int sat = LabParams.Saturacion(_grid.temp[i]);
            if (h > sat)
            {
                int t = h - sat;
                if (t > LabParams.CondensaRate) t = LabParams.CondensaRate;
                int tgt = -1;
                if (LabEsSuperficieCondensable(mat[up])) tgt = up;
                else if (LabEsSuperficieCondensable(mat[left])) tgt = left;
                else if (LabEsSuperficieCondensable(mat[right])) tgt = right;
                else if (LabEsSuperficieCondensable(mat[down])) tgt = down;
                if (tgt >= 0)
                {
                    int cabe = 255 - hum[tgt];
                    if (t > cabe) t = cabe;
                    if (t > 0)
                    {
                        hum[tgt] = (byte)(hum[tgt] + t); h -= t;
                        LabCondensado += t;
                        LabLatente(tgt, t, +1);
                        byte mt = mat[tgt];
                        if (hum[tgt] >= 255 && (LabMateriales.EsRocaImpermeable(mt) || mt == MaterialId.NucleoFrio)) LabGotear(tgt);
                    }
                }
            }
            hum[i] = (byte)h;
        }

        /// <summary>Mueve (diferencia / 2·D) unidades de vapor de i a j (o de j a i si j tiene más). Devuelve la humedad de i actualizada.</summary>
        private int LabIntercambioVapor(int h, int j, int D)
        {
            if (!LabEsAireOGas(_grid.mat[j])) return h;
            int hj = _grid.humedad[j];
            int diff = h - hj;
            if (diff <= 1 && diff >= -1) return h;
            int t = diff / (2 * D);
            if (t == 0) t = diff > 0 ? 1 : -1;
            if (t > 0) { if (t > 255 - hj) t = 255 - hj; }
            else { if (-t > 255 - h) t = -(255 - h); }
            _grid.humedad[j] = (byte)(hj + t);
            return h - t;
        }

        // ---- AGUA ---------------------------------------------------------------
        private void LabAgua(int x, int y, int i)
        {
            var hum = _grid.humedad; var mat = _grid.mat; var temp = _grid.temp; var carga = _grid.carga;
            int vol = hum[i];
            int up = i + W, down = i - W;

            // 1) Evaporación de superficie (aire encima, no saturado): tasa ∝ calor.
            if (mat[up] == MaterialId.Empty)
            {
                int satUp = LabParams.Saturacion(temp[up]);
                int deficit = satUp - hum[up];
                if (deficit > 0)
                {
                    int t = temp[i] - CellGrid.AmbientRaw; if (t < 0) t = 0;
                    int rate = LabParams.EvapBase + LabParams.EvapPorGrado * t;
                    rate = rate * deficit / satUp;
                    if (rate > vol) rate = vol;
                    if (rate > deficit) rate = deficit;
                    if (rate > 0)
                    {
                        hum[up] = (byte)(hum[up] + rate); vol -= rate;
                        LabEvaporado += rate;
                        LabLatente(i, rate, -1);
                    }
                }
            }
            if (vol <= 0) { LabTransformar(i, MaterialId.Empty, 0, 0); return; } // el aire se la llevó entera.

            // 2) Infiltración a porosos vecinos (abajo entero, lados a la mitad),
            //    frenada por la COLMATACIÓN del poroso (finos atrapados).
            vol = LabInfiltrarHacia(i, down, vol, 255);
            if (vol <= 0) { LabTransformar(i, MaterialId.Empty, 0, 0); return; }
            vol = LabInfiltrarHacia(i, i - 1, vol, 128);
            if (vol <= 0) { LabTransformar(i, MaterialId.Empty, 0, 0); return; }
            vol = LabInfiltrarHacia(i, i + 1, vol, 128);
            if (vol <= 0) { LabTransformar(i, MaterialId.Empty, 0, 0); return; }

            // 3) Los finos: decantan hacia el agua de abajo (más si el agua está
            //    quieta), DEPOSITAN sobre el fondo si hay bastantes y quietud, y
            //    se mezclan de lado.
            int c = carga[i];
            if (c > 0)
            {
                byte md = mat[down];
                if (md == MaterialId.Water)
                {
                    int rate = LabParams.Decantacion;
                    if (_grid.reposo[i] < LabParams.ReposoMovil) rate = rate * LabParams.FactorMovilPct / 100;
                    if (rate > c) rate = c;
                    int cabe = 255 - carga[down];
                    if (rate > cabe) rate = cabe;
                    if (rate > 0) { carga[down] = (byte)(carga[down] + rate); c -= rate; }
                }
                else if (LabMateriales.EsFondo(md) && c >= LabParams.DepositoUmbral && _grid.reposo[i] >= LabParams.DepositoReposo)
                {
                    // La celda de agua SE VUELVE sedimento húmedo: los finos ocupan
                    // su sitio. (255 de carga = una celda de finos; el agua que
                    // había queda como humedad del sedimento.)
                    // (R131) `vol`, NO 255: escribir 255 fijo CREABA agua en cada
                    // depósito (una celda a medio llenar salía del depósito con
                    // más agua de la que entró). Con 7670 depósitos en 9000 ticks
                    // el inventario se desviaba +10 % del libro mayor; con `vol`
                    // el depósito es exactamente la transferencia que dice el
                    // comentario (invariante 3 del HANDOFF: restar donde se suma).
                    LabTransformar(i, MaterialId.Sedimento, vol, 0);
                    LabDepositado++;
                    return;
                }
                if (mat[i - 1] == MaterialId.Water) c = LabMezclarCarga(c, i - 1);
                if (mat[i + 1] == MaterialId.Water) c = LabMezclarCarga(c, i + 1);
                carga[i] = (byte)c;
            }

            hum[i] = (byte)vol;
            if (_grid.reposo[i] < 255) _grid.reposo[i]++;
        }

        /// <summary>Infiltra volumen de la celda de agua `i` en el poroso `j` (si lo es). Devuelve el volumen restante. `fraccion` 255 = tasa entera, 128 = mitad (lados).</summary>
        private int LabInfiltrarHacia(int i, int j, int vol, int fraccion)
        {
            byte mj = _grid.mat[j];
            int perm = LabMateriales.Permeabilidad(mj);
            if (perm <= 0) return vol;
            int hj = _grid.humedad[j];
            if (hj >= 255) return vol;
            int libre = 255 - _grid.carga[j]; // 1 - colmatación
            long rate = (long)LabParams.Infiltracion * perm * libre * libre * fraccion / (255L * 255 * 255 * 255);
            if (rate <= 0) return vol;
            if (rate > vol) rate = vol;
            if (rate > 255 - hj) rate = 255 - hj;
            _grid.humedad[j] = (byte)(hj + rate);
            // Los finos que viajan con esa agua quedan ATRAPADOS en el poroso.
            int finos = (int)(rate * _grid.carga[i] * LabParams.ColmatacionPct / (255L * 100));
            if (finos > 0)
            {
                int cj = _grid.carga[j] + finos; if (cj > 255) cj = 255;
                _grid.carga[j] = (byte)cj;
            }
            LabInfiltrado += rate;
            return vol - (int)rate;
        }

        private int LabMezclarCarga(int c, int j)
        {
            int cj = _grid.carga[j];
            int d = c - cj;
            if (d <= 1) return c;
            int t = d * LabParams.Mezcla / 256;
            if (t < 1) t = 1;
            if (t > d / 2) t = d / 2;
            _grid.carga[j] = (byte)(cj + t);
            return c - t;
        }

        // ---- POROSOS ------------------------------------------------------------
        private void LabPoroso(int x, int y, int i, byte m)
        {
            var hum = _grid.humedad; var mat = _grid.mat;
            int h = hum[i];
            int perm = LabMateriales.Permeabilidad(m);

            // 1) Exudación: saturado y con vacío al lado o debajo → suelta UNA
            //    celda de agua LIMPIA (el poroso filtró: la carga se queda en él).
            if (h >= 255)
            {
                int j = LabVecinoVacio(i, abajoPrimero: true, permitirArriba: false);
                if (j >= 0)
                {
                    LabNacerAgua(j, _grid.temp[i], 0);
                    LabBalanceU -= hum[i]; // (R131) el agua que el poro suelta, auditada (si no, el libro perdía 255 u por exudación).
                    hum[i] = 0;
                    LabExudado++;
                    if (_grid.reposo[i] < 255) _grid.reposo[i]++;
                    return;
                }
            }

            // 2) Percolación hacia abajo (gravedad), o a un agua parcial de abajo.
            int down = i - W; byte md = mat[down];
            int permD = LabMateriales.Permeabilidad(md);
            if (permD > 0)
            {
                int hd = hum[down];
                if (hd < h)
                {
                    int t = LabParams.Percolacion * (perm < permD ? perm : permD) / 255;
                    if (t < 1) t = 1;
                    if (t > (h - hd + 1) / 2) t = (h - hd + 1) / 2;
                    if (t > 255 - hd) t = 255 - hd;
                    if (t > 0) { hum[down] = (byte)(hd + t); h -= t; }
                }
            }
            else if (md == MaterialId.Water)
            {
                int hd = hum[down];
                if (hd < 255 && h > 0)
                {
                    int t = h; if (t > 255 - hd) t = 255 - hd; if (t > LabParams.Percolacion) t = LabParams.Percolacion;
                    hum[down] = (byte)(hd + t); h -= t;
                }
            }

            // 3) Capilaridad lateral, y hacia arriba solo en los finos.
            h = LabCapilar(h, i - 1, LabParams.Capilaridad, 0);
            h = LabCapilar(h, i + 1, LabParams.Capilaridad, 0);
            if (LabMateriales.EsFino(m)) h = LabCapilar(h, i + W, LabParams.CapilarArriba, 64);

            // 4) Secado hacia el aire vecino no saturado (más con calor).
            if (h > 0 && LabParams.Secado > 0)
            {
                h = LabSecarHacia(h, i, i + W);
                h = LabSecarHacia(h, i, i - 1);
                h = LabSecarHacia(h, i, i + 1);
                h = LabSecarHacia(h, i, i - W);
            }
            hum[i] = (byte)h;

            // 5) Transformaciones lentas propias de cada material.
            switch (m)
            {
                case MaterialId.Sedimento:
                    if (h >= LabParams.CompactHumMin && h <= LabParams.CompactHumMax
                        && _grid.reposo[i] >= LabParams.CompactReposo && LabVecinosSolidos(i) >= 3)
                    {
                        var rng = XorShift.FromCell(_tick, x, y, SalLabCompacta);
                        if (rng.ChancePercent(LabParams.CompactPct))
                        {
                            LabTransformar(i, MaterialId.Arcilla, h, 0);
                            LabCompactado++;
                            return;
                        }
                    }
                    break;
                case MaterialId.Arcilla:
                    if (h >= LabParams.AblandaHum)
                    {
                        LabTransformar(i, MaterialId.Sedimento, h, 0);
                        LabAblandado++;
                        return;
                    }
                    if (_grid.temp[i] >= LabParams.TerracotaRaw && h <= LabParams.TerracotaHumMax)
                    {
                        LabTransformar(i, MaterialId.Terracota, 0, 0);
                        LabCocido++;
                        return;
                    }
                    break;
                case MaterialId.Ash:
                    if (h >= 128)
                    {
                        // Ceniza mojada = abono: se disuelve en el sustrato vecino.
                        int tgt = -1;
                        if (LabMateriales.EsSustrato(mat[down]) && mat[down] != MaterialId.Ash) tgt = down;
                        else if (LabMateriales.EsSustrato(mat[i - 1]) && mat[i - 1] != MaterialId.Ash) tgt = i - 1;
                        else if (LabMateriales.EsSustrato(mat[i + 1]) && mat[i + 1] != MaterialId.Ash) tgt = i + 1;
                        if (tgt >= 0)
                        {
                            int f = _grid.carga[tgt] + LabParams.AbonoCeniza; if (f > 255) f = 255;
                            _grid.carga[tgt] = (byte)f;
                            int hf = _grid.humedad[tgt] + h; if (hf > 255) hf = 255;
                            _grid.humedad[tgt] = (byte)hf; // su agua también pasa al sustrato.
                            LabTransformar(i, MaterialId.Empty, 0, 0);
                            LabAbonado++;
                            return;
                        }
                    }
                    break;
                case MaterialId.Semilla:
                    // HITO OPUS (plantas): germinar si está asentada sobre sustrato
                    // húmedo e iluminado — ver docs/LAB/HANDOFF_OPUS.md, hito H4.
                    break;
            }
            if (_grid.reposo[i] < 255) _grid.reposo[i]++;
        }

        private int LabCapilar(int h, int j, int factor, int umbral)
        {
            if (factor <= 0) return h;
            if (!LabMateriales.EsPoroso(_grid.mat[j])) return h;
            int hj = _grid.humedad[j];
            if (hj + umbral >= h) return h;
            int t = (h - hj) * factor / 256;
            if (t < 1) return h;
            if (t > 255 - hj) t = 255 - hj;
            _grid.humedad[j] = (byte)(hj + t);
            return h - t;
        }

        /// <summary>Seca `h` unidades de la celda `i` hacia el aire `j` si no está saturado. Cuenta como evaporación (con su calor latente).</summary>
        private int LabSecarHacia(int h, int i, int j)
        {
            if (h <= 0 || _grid.mat[j] != MaterialId.Empty) return h;
            int sat = LabParams.Saturacion(_grid.temp[j]);
            int deficit = sat - _grid.humedad[j];
            if (deficit <= 0) return h;
            int t = _grid.temp[i] - CellGrid.AmbientRaw; if (t < 0) t = 0;
            int rate = LabParams.Secado * (16 + t) / 16;
            rate = rate * deficit / sat;
            if (rate < 1) rate = 1;
            if (rate > h) rate = h;
            if (rate > deficit) rate = deficit;
            _grid.humedad[j] = (byte)(_grid.humedad[j] + rate);
            LabEvaporado += rate;
            LabLatente(i, rate, -1);
            return h - rate;
        }

        private int LabVecinosSolidos(int i)
        {
            int c = 0;
            if (LabEsSolidoParaCompactar(_grid.mat[i - W])) c++;
            if (LabEsSolidoParaCompactar(_grid.mat[i + W])) c++;
            if (LabEsSolidoParaCompactar(_grid.mat[i - 1])) c++;
            if (LabEsSolidoParaCompactar(_grid.mat[i + 1])) c++;
            return c;
        }

        private bool LabEsSolidoParaCompactar(byte m)
        {
            if (m == MaterialId.Empty || m == MaterialId.Water || LabMateriales.EsGasId(m) || m == MaterialId.Fire) return false;
            return true;
        }

        // ---- ROCA (rocío) --------------------------------------------------------
        private void LabRoca(int x, int y, int i)
        {
            int h = _grid.humedad[i];
            if (h == 0) return;
            if (h >= 255) { LabGotear(i); return; }
            if (LabParams.Secado > 0)
            {
                h = LabSecarHacia(h, i, i + W);
                h = LabSecarHacia(h, i, i - 1);
                h = LabSecarHacia(h, i, i + 1);
                h = LabSecarHacia(h, i, i - W);
            }
            _grid.humedad[i] = (byte)h;
        }

        // ---- PLANTA (HITO OPUS H4: aquí solo la muerte por desarraigo) -----------
        private void LabPlanta(int x, int y, int i)
        {
            byte abajo = _grid.mat[i - W];
            if (abajo != MaterialId.Planta && !LabMateriales.EsSustrato(abajo))
            {
                // Sin raíz ni tallo debajo: se seca en fibra (que cae como polvo).
                LabTransformar(i, MaterialId.Fibra, 0, 0);
                LabPlantasMuertas++;
            }
            // TODO (Opus, H4): beber del sustrato, savia hacia arriba, crecer con
            // luz, ramificar, marchitarse por sequía, fertilidad. Ver HANDOFF §H4.
        }

        // ---- HOGAR / FRÍO / MANANTIAL / SUMIDERO ---------------------------------
        private void LabHogar(int x, int y, int i)
        {
            _grid.temp[i] = (byte)LabParams.HogarRaw;
            InjectHeat(x, y, LabParams.HogarCalor);
            _grid.WakeChunk(x, y, _tick); // R55: la brasa eterna mantiene vivo su chunk (si no, lo que le acercan nunca prende).
            TryIgnite(x - 1, y); TryIgnite(x + 1, y); TryIgnite(x, y - 1); TryIgnite(x, y + 1);
        }

        private void LabFrio(int x, int y, int i)
        {
            _grid.temp[i] = (byte)LabParams.FrioRaw;
            InjectCold(x, y, LabParams.FrioPotencia);
            _grid.WakeChunk(x, y, _tick);
        }

        private void LabManantial(int x, int y, int i)
        {
            int n = _labManantialCeldas < 1 ? 1 : _labManantialCeldas;
            // caudal (celdas/s) · (8/30 s por visita) / n celdas, en milésimas.
            int porMil = LabParams.Caudal * 8000 / (30 * n);
            var rng = XorShift.FromCell(_tick, x, y, SalLabManantial);
            int veces = porMil / 1000;
            if (rng.Next(1000) < porMil % 1000) veces++;
            for (int k = 0; k < veces; k++)
            {
                int j = LabVecinoVacio(i, abajoPrimero: false, permitirArriba: true);
                if (j < 0) break; // rodeado: el manantial espera (no crea presión).
                LabNacerAgua(j, (byte)LabParams.FuenteTempRaw, LabParams.TurbidezFuente);
                LabAguaEmitida++;
            }
        }

        private void LabSumidero(int x, int y, int i)
        {
            LabTragar(i - W); LabTragar(i + W); LabTragar(i - 1); LabTragar(i + 1);
        }

        private void LabTragar(int j)
        {
            byte m = _grid.mat[j];
            if (m == MaterialId.Empty) return;
            if (_universe.Get(m).archetype != MaterialArchetype.Liquid) return;
            if (m == MaterialId.Water) { LabAguaSumida++; LabAguaSumidaU += _grid.humedad[j]; }
            LabTransformar(j, MaterialId.Empty, 0, 0);
        }

        // =====================================================================
        // 2) PRESIÓN HIDROSTÁTICA POR CUERPOS DE AGUA CONECTADOS
        // =====================================================================
        // Cada cuerpo (BFS 4-conexa sobre Water) conoce sus SUPERFICIES (agua
        // con aire encima). Si la más alta está DesnivelMin o más por encima de
        // la más baja, una celda de la más alta se muda encima de la más baja
        // (misma temperatura, misma carga). Hasta PresionCeldasPorPaso mudanzas
        // por cuerpo y paso. Es exactamente lo que hace la presión en un fluido
        // incompresible: el nivel busca igualarse por cualquier camino lleno.
        // Coste O(celdas de agua) por paso; arrays preasignados; cero allocs.
        private int[] _labVisita, _labCola, _labSup;
        private int _labPase;

        private void LabPresion()
        {
            if (_labVisita == null)
            {
                _labVisita = new int[W * H];
                _labCola = new int[W * H];
                _labSup = new int[W * H + 256];
            }
            _labPase++;
            if (_labPase == int.MaxValue) { Array.Clear(_labVisita, 0, _labVisita.Length); _labPase = 1; }
            int pase = _labPase;
            var mat = _grid.mat; var temp = _grid.temp; var hum = _grid.humedad; var carga = _grid.carga; var reposo = _grid.reposo;
            int minCeldas = LabParams.PresionMinCeldas, desnivel = LabParams.DesnivelMin, porPaso = LabParams.PresionCeldasPorPaso;

            for (int y = 1; y < H - 1; y++)
            {
                int fila = y * W;
                for (int x = 1; x < W - 1; x++)
                {
                    int i = fila + x;
                    if (mat[i] != MaterialId.Water || _labVisita[i] == pase) continue;

                    // ---- BFS del cuerpo ----
                    int head = 0, tail = 0, nSup = 0;
                    _labCola[tail++] = i; _labVisita[i] = pase;
                    while (head < tail)
                    {
                        int c = _labCola[head++];
                        if (mat[c + W] == MaterialId.Empty) _labSup[nSup++] = c;
                        int v;
                        v = c - 1; if (mat[v] == MaterialId.Water && _labVisita[v] != pase) { _labVisita[v] = pase; _labCola[tail++] = v; }
                        v = c + 1; if (mat[v] == MaterialId.Water && _labVisita[v] != pase) { _labVisita[v] = pase; _labCola[tail++] = v; }
                        v = c - W; if (mat[v] == MaterialId.Water && _labVisita[v] != pase) { _labVisita[v] = pase; _labCola[tail++] = v; }
                        v = c + W; if (mat[v] == MaterialId.Water && _labVisita[v] != pase) { _labVisita[v] = pase; _labCola[tail++] = v; }
                    }
                    if (tail < minCeldas || nSup < 2) continue;

                    // ---- igualar: de la superficie más alta a la más baja ----
                    for (int k = 0; k < porPaso; k++)
                    {
                        int src = -1, srcY = -1, dst = -1, dstY = int.MaxValue;
                        for (int s = 0; s < nSup; s++)
                        {
                            int c = _labSup[s];
                            if (c < 0) continue;
                            if (mat[c] != MaterialId.Water || mat[c + W] != MaterialId.Empty) { _labSup[s] = -1; continue; }
                            int cy = c / W;
                            if (cy > srcY) { srcY = cy; src = c; }
                            if (cy < dstY) { dstY = cy; dst = c; }
                        }
                        if (src < 0 || dst < 0 || src == dst || srcY - dstY < desnivel) break;

                        int target = dst + W;
                        // (R131) El aire del destino NO se aniquila: se muda al hueco que
                        // deja el agua. Es un INTERCAMBIO, como el de cualquier celda que
                        // se mueve. Antes la mudanza borraba el vapor del destino y el
                        // libro mayor perdía ~4,5 u por mudanza (−20 006 u en 9000 ticks,
                        // el 85 % del descuadre): la presión "secaba" el aire de la cueva.
                        int vaporDelHueco = hum[target];
                        _grid.SetCell(target, MaterialId.Water);
                        temp[target] = temp[src]; hum[target] = hum[src]; carga[target] = carga[src]; reposo[target] = 0;
                        _grid.SetCell(src, MaterialId.Empty);
                        hum[src] = (byte)vaporDelHueco;
                        _labVisita[target] = pase;
                        _grid.WakeChunk(target % W, target / W, _tick);
                        _grid.WakeChunk(src % W, src / W, _tick);
                        LabPresionMovidas++;

                        // superficies nuevas: bajo la fuente (si sigue siendo agua) y el destino.
                        if (mat[src - W] == MaterialId.Water && nSup < _labSup.Length) _labSup[nSup++] = src - W;
                        if (mat[target + W] == MaterialId.Empty && nSup < _labSup.Length) _labSup[nSup++] = target;
                    }
                }
            }
        }

        // =====================================================================
        // 3) LUZ — máximo con decaimiento, cuatro barridos
        // =====================================================================
        private void LabLuz()
        {
            var luz = _grid.luz; var mat = _grid.mat;
            int n = W * H;
            for (int i = 0; i < n; i++) luz[i] = LabMateriales.EmiteLuz(mat[i]) ? (byte)255 : (byte)0;
            int cx0 = LabParams.LuzCieloX0, cx1 = LabParams.LuzCieloX1;
            if (cx0 >= 0)
            {
                int yc = H - 2;
                for (int x = cx0; x <= cx1 && x < W - 1; x++) { int i = yc * W + x; if (mat[i] == MaterialId.Empty) luz[i] = 255; }
            }
            int dAire = LabParams.LuzDecayAire, dCielo = LabParams.LuzDecayCielo, dAgua = LabParams.LuzDecayAgua, dPlanta = LabParams.LuzDecayPlanta;

            // arriba → abajo (la luz del cielo cae casi sin perder)
            for (int y = H - 2; y >= 1; y--)
            {
                int fila = y * W;
                for (int x = 1; x < W - 1; x++)
                {
                    int i = fila + x;
                    int v = LabLuzDesde(i + W) - LabLuzDecay(mat[i], dCielo, dAire, dAgua, dPlanta);
                    if (v > luz[i]) luz[i] = (byte)v;
                }
            }
            // abajo → arriba
            for (int y = 1; y < H - 1; y++)
            {
                int fila = y * W;
                for (int x = 1; x < W - 1; x++)
                {
                    int i = fila + x;
                    int v = LabLuzDesde(i - W) - LabLuzDecay(mat[i], dAire, dAire, dAgua, dPlanta);
                    if (v > luz[i]) luz[i] = (byte)v;
                }
            }
            // izquierda → derecha y derecha → izquierda
            for (int y = 1; y < H - 1; y++)
            {
                int fila = y * W;
                for (int x = 1; x < W - 1; x++)
                {
                    int i = fila + x;
                    int v = LabLuzDesde(i - 1) - LabLuzDecay(mat[i], dAire, dAire, dAgua, dPlanta);
                    if (v > luz[i]) luz[i] = (byte)v;
                }
                for (int x = W - 2; x >= 1; x--)
                {
                    int i = fila + x;
                    int v = LabLuzDesde(i + 1) - LabLuzDecay(mat[i], dAire, dAire, dAgua, dPlanta);
                    if (v > luz[i]) luz[i] = (byte)v;
                }
            }
        }

        /// <summary>Luz que un vecino TRANSMITE: solo el aire, los gases, el agua, la planta y las fuentes (fuego/brasa/hogar). Los sólidos reciben pero no transmiten.</summary>
        private int LabLuzDesde(int j)
        {
            byte m = _grid.mat[j];
            if (m == MaterialId.Empty || m == MaterialId.Water || m == MaterialId.Planta || LabMateriales.EsGasId(m) || LabMateriales.EmiteLuz(m)) return _grid.luz[j];
            return 0;
        }

        private static int LabLuzDecay(byte m, int dVert, int dAire, int dAgua, int dPlanta)
        {
            if (m == MaterialId.Empty || LabMateriales.EsGasId(m)) return dVert;
            if (m == MaterialId.Water) return dAgua;
            if (m == MaterialId.Planta) return dPlanta;
            return dAire; // sólidos: se iluminan como la celda de aire que los toca, y no transmiten (LabLuzDesde).
        }

        // =====================================================================
        // 4) CUERPOS COHESIONADOS — HITO OPUS H6 (ver HANDOFF). Gancho vacío.
        // =====================================================================
        private void LabCuerpos()
        {
            // TODO (Opus, H6): componentes conectados de RocaSuelta; sin apoyo → el
            // bloque entero baja una celda; aux = ticks de caída; al aterrizar con
            // caída ≥ FracturaCaida, FracturaPct de las celdas → Grava; golpes de
            // cincel acumulan daño. Reutilizar _labVisita/_labCola de LabPresion.
        }

        // =====================================================================
        // TÉRMICA PROPIA DEL LABORATORIO (sustituye a DiffuseTemperature)
        // =====================================================================
        // Misma cobertura 1/8 y mismo redondeo simétrico (regla 9), con
        // conductividad k (0..16) y capacidad c (1..8) por clase de material,
        // convección en el aire (el calor sube) y tirón hacia ambient[] cada
        // TiroAmbienteTicks. Contracción garantizada: el paso se recorta al
        // rango [min(dj,0), max(dj,0)] de las diferencias con los vecinos.
        private void LabDifusionTermica()
        {
            int offset = (int)(_tick % 8u);
            int n = W * H;
            var temp = _grid.temp; var mat = _grid.mat;
            uint tiro = (uint)(LabParams.TiroAmbienteTicks < 8 ? 8 : LabParams.TiroAmbienteTicks);
            uint rondas = tiro / 8u; if (rondas < 1u) rondas = 1u;
            bool ambientSweep = ((_tick >> 3) % rondas) == 0u;
            bool conv = LabParams.Conveccion != 0;

            for (int i = offset; i < n; i += 8)
            {
                int x = i % W, y = i / W;
                if (x == 0 || x == W - 1 || y == 0 || y == H - 1) continue;

                byte m = mat[i];
                int ki = LabK(m), ci = LabC(m);
                int cur = temp[i];
                int flujo = 0, dMin = 0, dMax = 0;
                bool aire = conv && m == MaterialId.Empty;

                flujo += LabFlujoTermico(cur, i - 1, ki, false, false, ref dMin, ref dMax);
                flujo += LabFlujoTermico(cur, i + 1, ki, false, false, ref dMin, ref dMax);
                flujo += LabFlujoTermico(cur, i - W, ki, aire, true, ref dMin, ref dMax);
                flujo += LabFlujoTermico(cur, i + W, ki, aire, false, ref dMin, ref dMax);

                int step = flujo / (64 * ci); // truncamiento hacia cero: simétrico en signo.
                if (step == 0 && flujo != 0) step = flujo > 0 ? 1 : -1;
                if (step > dMax) step = dMax; else if (step < dMin) step = dMin;
                int next = cur + step;

                if (ambientSweep)
                {
                    int ad = _grid.ambient[i] - next;
                    if (ad != 0) next += ad > 0 ? 1 : -1;
                }
                if (next < 0) next = 0; else if (next > 255) next = 255;
                temp[i] = (byte)next;
            }
        }

        private int LabFlujoTermico(int cur, int j, int ki, bool conveccion, bool vecinoAbajo, ref int dMin, ref int dMax)
        {
            int kj = LabK(_grid.mat[j]);
            int k = ki < kj ? ki : kj;
            int d = _grid.temp[j] - cur;
            if (d < dMin) dMin = d; else if (d > dMax) dMax = d;
            if (conveccion && d > 0) { if (vecinoAbajo) k *= 2; else k /= 2; } // el aire caliente de abajo sube; el de arriba no baja.
            return d * k;
        }

        private static int LabK(byte m)
        {
            switch (m)
            {
                case MaterialId.Empty: return LabParams.KAire;
                case MaterialId.Water: return LabParams.KAgua;
                case MaterialId.Steam: case MaterialId.Smoke: case MaterialId.Fire: return LabParams.KGas;
                case MaterialId.Arcilla: case MaterialId.Terracota: return LabParams.KArcilla;
                case MaterialId.Stone: case MaterialId.PisoEstructural: case MaterialId.Ice: case MaterialId.Hogar:
                case MaterialId.NucleoFrio: case MaterialId.Manantial: case MaterialId.Sumidero: case MaterialId.RocaSuelta:
                case MaterialId.Arenisca:
                    return LabParams.KRoca;
                default: return LabParams.KPolvo;
            }
        }

        private static int LabC(byte m)
        {
            switch (m)
            {
                case MaterialId.Empty: case MaterialId.Steam: case MaterialId.Smoke: case MaterialId.Fire: return LabParams.CAire;
                case MaterialId.Water: case MaterialId.Ice: return LabParams.CAgua;
                case MaterialId.Stone: case MaterialId.PisoEstructural: case MaterialId.Arcilla: case MaterialId.Terracota: case MaterialId.Hogar:
                case MaterialId.NucleoFrio: case MaterialId.Manantial: case MaterialId.Sumidero: case MaterialId.RocaSuelta:
                case MaterialId.Arenisca:
                    return LabParams.CRoca;
                default: return LabParams.CPolvo;
            }
        }
    }
}
