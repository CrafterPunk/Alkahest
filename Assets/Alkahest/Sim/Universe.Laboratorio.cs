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
            mats[MaterialId.Carbon] = new MaterialDef
            {
                id = MaterialId.Carbon,
                devName = "Carbon",
                archetype = MaterialArchetype.Powder,
                baseColor = new Color32(38, 34, 32, 255), // negro mate: se distingue de la ceniza (gris claro) de un vistazo.
                colorJitter = 10,
                density = 110, // más ligero que la arena; se hunde en el agua por poco.
                fluidity = 1,
                flammable = true,
                ignitionTemp = CellGrid.CToRaw(280), // cuesta MÁS prenderlo que a la fibra (140 °C): hace falta una brasa, no una chispa.
                burnsInto = MaterialId.Fire,
                // (R135, F2) El combustible de segunda generación: cuatro veces la reserva de
                // la fibra y casi el doble de calor, con muy poco humo. Es lo que hace que un
                // horno se pueda alimentar una vez y trabajar minutos.
                // (R136, C2) Reserva 50, no 160: con 160 una carbonera al 100 % de rendimiento
                // multiplicaba por 6,3 la energía de la fibra de partida. 25 % × 50 × 22 = 275,
                // que es media fibra (½ × 40 × 14 = 280). Un carbón arde 13 s respirando y 53 s
                // ahogado; el horno sigue trabajando minutos porque la pila es MACIZA, no porque
                // la celda sea eterna.
                combustReserva = 50,
                combustPasoTicks = 8,
                combustCalorRaw = 22,
                combustHumoPct = 4,
                combustPropagacionPct = 10,
                combustLenguaPct = 30,
                combustResiduo = MaterialId.Brasa,
            };
            mats[MaterialId.Arenisca] = new MaterialDef
            {
                id = MaterialId.Arenisca,
                devName = "Arenisca",
                archetype = MaterialArchetype.StaticSolid,
                baseColor = new Color32(196, 172, 128, 255), // arena cementada: se lee como arena, se comporta como roca.
                colorJitter = 12,
                density = 235,
                caeSolido = false, // (R131) ESTÁTICA a propósito: la fisura de arena caía a la cámara profunda y se llevaba el arroyo entero.
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
            ReaplicarVapor(u); // (R132) el punto de rocío del vapor visible ya es un parámetro.
        }

        /// <summary>
        /// Vida y punto de rocío del vapor VISIBLE desde LabParams (el panel los
        /// cambia en vivo). (R132) `condensesAt` estaba escrito a mano aquí en 60 °C,
        /// contra el invariante 5 del handoff ("todo número físico vive en LabParams"),
        /// y era el número que rompía la cadena del agua: con el aire de la cueva a
        /// 20 °C, cada celda de vapor se volvía agua en el mismo tick en que salía de
        /// la zona caliente del hogar. Medido: 229 condensaciones de gas y CERO celdas
        /// de vapor vivas, ni una sola llegando a la chimenea.
        /// </summary>
        public static void ReaplicarVapor(Universe u)
        {
            int vida = LabParams.VaporVida;
            if (vida < 1) vida = 1; else if (vida > 255) vida = 255;
            var vapor = u.Get(MaterialId.Steam);
            vapor.gasLifetime = (byte)vida;
            vapor.condensesAt = CellGrid.CToRaw(LabParams.VaporCondensaC);
            // (R135, F3) El HUMO del laboratorio dura más que el del juego: una bolsa bajo el
            // techo tiene que aguantar lo suficiente para AHOGAR un fuego (F1) y para
            // oscurecer un claro. Es lo que convierte la chimenea de decorado en necesidad.
            int vh = LabParams.VidaHumo;
            if (vh < 1) vh = 1; else if (vh > 255) vh = 255;
            u.Get(MaterialId.Smoke).gasLifetime = (byte)vh;
            LabParams.VaporVidaCambiado = false;
        }
    }
}
