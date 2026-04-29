using System.Reflection;
using System.Text.Json;

// Load the actual API DLL and check TrendPoint via reflection
var dllPath = @"c:\Users\kong\AiAgent\backend\SimplerJiangAiAgent.Api\bin\Debug\net9.0\SimplerJiangAiAgent.Api.dll";
var asm = Assembly.LoadFrom(dllPath);
var trendPointType = asm.GetTypes().FirstOrDefault(t => t.Name == "TrendPoint");
if (trendPointType == null) { Console.WriteLine("TrendPoint type not found!"); return; }

Console.WriteLine("TrendPoint properties:");
foreach (var p in trendPointType.GetProperties())
{
    Console.WriteLine($"  {p.Name} ({p.PropertyType.Name})");
}

// Create instance and set QoQ
var instance = Activator.CreateInstance(trendPointType)!;
trendPointType.GetProperty("Period")!.SetValue(instance, "2024-12-31");
trendPointType.GetProperty("Value")!.SetValue(instance, (double?)100.0);
trendPointType.GetProperty("YoY")!.SetValue(instance, (double?)5.5);
trendPointType.GetProperty("QoQ")!.SetValue(instance, (double?)3.3);

var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
var json = JsonSerializer.Serialize(instance, trendPointType, options);
Console.WriteLine("Serialized: " + json);
