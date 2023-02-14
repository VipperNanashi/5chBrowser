using _5chBrowser.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace _5chBrowser.Services
{
    public class SaveStateService
    {
        public async Task Save(State state)
        {
            await ExclusiveRunner.Run(async () =>
            {
                JsonSerializerOptions options = new()
                {
                    ReferenceHandler = ReferenceHandler.Preserve,
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() },
                    IgnoreReadOnlyProperties = true
                };

                var json = JsonSerializer.Serialize(state, options);

                var assembly = Assembly.GetEntryAssembly();
                var folder = Path.GetDirectoryName(assembly.Location);
                var path = Path.Combine(folder, "state.json");
                await File.WriteAllTextAsync(path, json);
            }, "state");
        }

        public async Task<State> Load()
        {
            return await ExclusiveRunner.Run(async () =>
            {
                try
                {
                    var assembly = Assembly.GetEntryAssembly();
                    var folder = Path.GetDirectoryName(assembly.Location);
                    var path = Path.Combine(folder, "state.json");

                    JsonSerializerOptions options = new()
                    {
                        ReferenceHandler = ReferenceHandler.Preserve,
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                        WriteIndented = true,
                        Converters = { new JsonStringEnumConverter() },
                        IgnoreReadOnlyProperties = true
                    };

                    var json = await File.ReadAllTextAsync(path);

                    return JsonSerializer.Deserialize<State>(json, options);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    return new State();
                }
            }, "state");
        }
    }
}
