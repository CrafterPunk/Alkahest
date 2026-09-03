using UnityEngine;

namespace Alkahest.Sim
{
    /// <summary>
    /// (R130, LABORATORIO DE LEYES) Los doce materiales del laboratorio y el
    /// decreto físico del agua/vapor bajo el que corre el experimento. Parte
    /// de Universe (partial) para no engordar Universe.cs. Ver
    /// docs/LAB/DISENO_LABORATORIO.md §3.
    /// </summary>
    public sealed partial class Universe
    {
        /// <summary>Defs de los materiales del laboratorio. Vocabulario: color fijo, Liso/Neto, jamás sorteados.</summary>
        private static void CrearMaterialesLaboratorio(MaterialDef[] mats)
        {
            mats[MaterialId.Sedimento] = new MaterialDef
            {
                id = MaterialId.Sedimento,
                devName = "Sedimento",
                archetype = MaterialArchetype.Powder,
                baseColor = new Color32(128, 110, 84, 255), // pardo grisáceo: finos de cantera. Mojado se oscurece (SimRenderer.Laboratorio).
                colorJitter = 14,
                density = 150, // más denso que el agua del laboratorio (110): se hunde y se posa.
                fluidity = 1,
            };
            mats[MaterialId.Arcilla] = new MaterialDef
            {
                id = MaterialId.Arcilla,
                devName = "Arcilla",
                archetype = MaterialArchetype.StaticSolid,
                baseColor = new Color32(146, 98, 72, 255),
                colorJitter = 10,
                density = 230,
                caeSolido = true, cohesionCeldas = 2, // una veta de arcilla socavada se derrumba pronto (blanda).
            };
            mats[MaterialId.Terracota] = new MaterialDef
            {
                id = MaterialId.Terracota,
                devName = "Terracota",
                archetype = MaterialArchetype.StaticSolid,
                baseColor = new Color32(188, 108, 70, 255),
                colorJitter = 8,
                density = 240,
                caeSolido = true, cohesionCeldas = 6, // cocida: voladiza como lo cerámico.
            };
            mats[MaterialId.Grava] = new MaterialDef
            {
                id = MaterialId.Grava,
                devName = "Grava",
                archetype = MaterialArchetype.Powder,
                baseColor = new Color32(140, 136, 130, 255),
                colorJitter = 22,
                density = 200,
                fluidity = 0, // grueso: no desliza de lado, solo cae.
            };
            mats[MaterialId.Planta] = new MaterialDef
            {
                id = MaterialId.Planta,
                devName = "Planta",
                archetype = MaterialArchetype.Planta,
                baseColor = new Color32(72, 150, 58, 255),
                colorJitter = 16,
                density = 100,
                flammable = true,
                ignitionTemp = CellGrid.CToRaw(160), // arde a 160 °C (seca; mojada no prende: LabCombustibleMojado).
                burnsInto = MaterialId.Fire,
            };
            mats[MaterialId.Fibra] = new MaterialDef
            {
                id = MaterialId.Fibra,
                devName = "Fibra",
                archetype = MaterialArchetype.Powder,
                baseColor = new Color32(176, 150, 92, 255),
                colorJitter = 18,
                density = 60, // más ligera que el agua: la fibra seca FLOTA.
                fluidity = 1,
                flammable = true,
                ignitionTemp = CellGrid.CToRaw(140),
                burnsInto = MaterialId.Fire,
                // Combustión persistente (patrón del Calcinado combustible):
                // 40 unidades x 8 ticks = ~11 s por celda, residuo Brasa.
                combustReserva = 40,
                combustPasoTicks = 8,
                combustCalorRaw = 14,
                combustHumoPct = 16,
                combustPropagacionPct = 18,
                combustLenguaPct = 40,
                combustResiduo = MaterialId.Brasa,
            };
            mats[MaterialId.Hogar] = new MaterialDef
            {
                id = MaterialId.Hogar,
                devName = "Hogar",
                archetype = MaterialArchetype.StaticSolid,
                baseColor = new Color32(170, 62, 20, 255),
                colorJitter = 16,
                density = short.MaxValue,
                caeSolido = false, // eterno: no cae, no se talla, no se aspira.
                emitsGlow = true,
                patron = PatronMorfologico.Pulso,
                patronFuerza = 60,
                ritmoAnim = 12,
            };
            mats[MaterialId.NucleoFrio] = new MaterialDef
            {
                id = MaterialId.NucleoFrio,
                devName = "NucleoFrio",
                archetype = MaterialArchetype.StaticSolid,
                baseColor = new Color32(150, 200, 230, 255),
                colorJitter = 10,
                density = short.MaxValue,
                caeSolido = false,
            };
            mats[MaterialId.Manantial] = new MaterialDef
            {
                id = MaterialId.Manantial,
                devName = "Manantial",
                archetype = MaterialArchetype.StaticSolid,
                baseColor = new Color32(70, 110, 150, 255),
                colorJitter = 12,
                density = short.MaxValue,
                caeSolido = false,
            };
            mats[MaterialId.Sumidero] = new MaterialDef
            {
                id = MaterialId.Sumidero,
                devName = "Sumidero",
                archetype = MaterialArchetype.StaticSolid,
                baseColor = new Color32(30, 26, 34, 255),
                colorJitter = 6,
                density = short.MaxValue,
                caeSolido = false,
            };
            mats[MaterialId.RocaSuelta] = new MaterialDef
            {
                id = MaterialId.RocaSuelta,
                devName = "RocaSuelta",
                archetype = MaterialArchetype.StaticSolid,
                baseColor = new Color32(112, 106, 114, 255),
                colorJitter = 14,
                density = short.MaxValue,
                caeSolido = false, // NO usa la cohesión por celda: LabCuerpos mueve el bloque ENTERO.
            };
            mats[MaterialId.Semilla] = new MaterialDef
            {
                id = MaterialId.Semilla,
                devName = "Semilla",
                archetype = MaterialArchetype.Powder,
                baseColor = new Color32(120, 90, 40, 255),
                colorJitter = 12,
                density = 90,
                fluidity = 1,
            };
        }

        /// <summary>
        /// El decreto físico del laboratorio, aplicado sobre el universo YA
        /// creado (mismo patrón que AplicarOverridesSemillaCero, mutación en
        /// sitio): el experimento no puede depender de que la seed sorteara
        /// un agua más densa que la arena o que hierve a 80 °C. Llamado por
        /// AlkahestSim solo con ModoLaboratorio. Los valores del vapor los
        /// vuelve a aplicar el panel cuando cambian (ReaplicarVapor).
        /// </summary>
        public static void AplicarOverridesLaboratorio(Universe u)
        {
            var agua = u.Get(MaterialId.Water);
            agua.density = 110;                       // la arena (180), el sedimento (150), la grava (200) y la ceniza (120) se hunden; la fibra (60) flota.
            agua.freezesAt = CellGrid.CToRaw(0);      // 60 raw
            agua.boilsAt = CellGrid.CToRaw(100);      // 110 raw
            u.Get(MaterialId.Ice).meltsAt = CellGrid.CToRaw(5);
            u.Get(MaterialId.Oil).density = 75;       // flota sobre el agua, siempre.
            var vapor = u.Get(MaterialId.Steam);
            vapor.condensesAt = CellGrid.CToRaw(60);  // 90 raw
            ReaplicarVapor(u);
        }

        /// <summary>Vida del vapor visible desde LabParams (el panel la cambia en vivo).</summary>
        public static void ReaplicarVapor(Universe u)
        {
            int vida = LabParams.VaporVida;
            if (vida < 1) vida = 1; else if (vida > 255) vida = 255;
            u.Get(MaterialId.Steam).gasLifetime = (byte)vida;
            LabParams.VaporVidaCambiado = false;
        }
    }
}
