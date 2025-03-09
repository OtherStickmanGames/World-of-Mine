using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class WorldData : MonoBehaviour
{

    public static string chuncksDirectory = $"{Application.dataPath}/Data/Chuncks/";
    public static string serverDirectory = "Server/";
    public static string clientDirectory = "Client/";


    public static ChunckData GetChunkData(Vector3 chunkPos)
    {
        var chunck = WorldGenerator.Inst.GetChunk(chunkPos);
        var chunckFileName = GetChunkName(chunck);
        var path = $"{chuncksDirectory}{serverDirectory}{chunckFileName}.json";

        return GetChunkData(path, chunck);
    }

    public static void SaveChunkData(ChunckData chunkData, Vector3 chunkPos)
    {
        var json = JsonConvert.SerializeObject(chunkData);
        var chunckFileName = GetChunkName(chunkPos);
        var path = $"{chuncksDirectory}{serverDirectory}{chunckFileName}.json";
        File.WriteAllText(path, json);
    }

    public static ChunckData GetChunkData(string path, ChunckComponent chunck)
    {
        ChunckData data;

        if (File.Exists(path))
        {
            var fileText = File.ReadAllText(path);
            data = JsonConvert.DeserializeObject<ChunckData>(fileText, settings);
            data.blocks = chunck.blocks;// Я хз зачем я это делаю
        }
        else
        {
            data = new ChunckData(chunck);
        }
        return data;
    }

    public static string GetChunkName(ChunckComponent chunck)
    {
        return $"{chunck.pos.x}_{chunck.pos.y}_{chunck.pos.z}";
    }

    public static string GetChunkName(Vector3 chunckPos)
    {
        return $"{chunckPos.x}_{chunckPos.y}_{chunckPos.z}";
    }

    private static readonly JsonSerializerSettings settings = new()
    {
        TypeNameHandling = TypeNameHandling.Auto,
        ObjectCreationHandling = ObjectCreationHandling.Replace,
        NullValueHandling = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        Error = (sender, eventArgs) =>
        {
            Debug.LogError(
                $"{eventArgs.ErrorContext.Error.Message} {eventArgs.ErrorContext.OriginalObject} {eventArgs.ErrorContext.Member}");
        }
    };
}
