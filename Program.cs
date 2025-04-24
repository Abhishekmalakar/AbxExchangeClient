using AbxExchangeClient;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting ABX client...");

        var client = new AbxClient();
        var packets = await client.GetAllPacketsAsync();

        Console.WriteLine($"Packets received: {packets.Count}");

        string json = JsonSerializer.Serialize(packets, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync("abx_data.json", json);

        Console.WriteLine("✅ Data written to abx_data.json");
    }
}
