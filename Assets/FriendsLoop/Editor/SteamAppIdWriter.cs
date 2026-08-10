using System.IO;
using UnityEditor;
using UnityEngine;

namespace FriendsLoop.EditorTools
{
    /// <summary>
    /// Se asegura de que exista steam_appid.txt en la raíz del proyecto durante el desarrollo en el editor,
    /// para que SteamAPI.Init() funcione sin lanzar el juego a través de Steam. Usa el App ID de desarrollo
    /// de Valve (480, "Spacewar").
    ///
    /// IMPORTANTE: para una build de release hay que sustituir "480" por el App ID real del juego,
    /// y este archivo NUNCA debe distribuirse dentro de una build que se publique en Steam
    /// (Steam ya provee el App ID real cuando el juego se lanza desde el cliente).
    /// </summary>
    [InitializeOnLoad]
    internal static class SteamAppIdWriter
    {
        private const string DevAppId = "480";

        static SteamAppIdWriter()
        {
            EnsureSteamAppIdFile();
        }

        private static void EnsureSteamAppIdFile()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string filePath = Path.Combine(projectRoot, "steam_appid.txt");

            if (File.Exists(filePath))
            {
                return;
            }

            try
            {
                File.WriteAllText(filePath, DevAppId);
                Debug.Log("[FriendsLoop] Se creó steam_appid.txt con el App ID de desarrollo (" + DevAppId +
                    "). Recuerda reemplazarlo por el App ID real antes de publicar y no incluirlo en builds de tienda.");
            }
            catch (IOException ex)
            {
                Debug.LogWarning("[FriendsLoop] No se pudo crear steam_appid.txt: " + ex.Message);
            }
        }
    }
}
