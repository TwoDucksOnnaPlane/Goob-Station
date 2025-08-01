// SPDX-FileCopyrightText: 2022 20kdc <asdd2808@gmail.com>
// SPDX-FileCopyrightText: 2022 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Javier Guardia Fernández <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Julian Giebel <juliangiebel@live.de>
// SPDX-FileCopyrightText: 2022 Kara D <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2022 Mervill <mervills.email@gmail.com>
// SPDX-FileCopyrightText: 2022 Moony <moonheart08@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 github-actions <github-actions@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 TemporalOroboros <TemporalOroboros@gmail.com>
// SPDX-FileCopyrightText: 2023 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.IntegrationTests;
using Content.MapRenderer.Painters;
using Content.Server.Maps;
using Newtonsoft.Json;
using Robust.Shared.Prototypes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

namespace Content.MapRenderer
{
    internal sealed class Program
    {
        private const string NoMapsChosenMessage = "No maps were chosen";
        private static readonly Func<string, string> ChosenMapIdNotIntMessage = id => $"The chosen id is not a valid integer: {id}";
        private static readonly Func<int, string> NoMapFoundWithIdMessage = id => $"No map found with chosen id: {id}";

        private static readonly MapPainter MapPainter = new();

        internal static async Task Main(string[] args)
        {
            if (!CommandLineArguments.TryParse(args, out var arguments))
                return;

            PoolManager.Startup();
            if (arguments.Maps.Count == 0)
            {
                Console.WriteLine("Didn't specify any maps to paint! Loading the map list...");

                await using var pair = await PoolManager.GetServerClient();
                var mapIds = pair.Server
                    .ResolveDependency<IPrototypeManager>()
                    .EnumeratePrototypes<GameMapPrototype>()
                    .Where(map => !pair.IsTestPrototype(map))
                    .Select(map => map.ID)
                    .ToArray();

                Array.Sort(mapIds);

                Console.WriteLine("Map List");
                Console.WriteLine(string.Join('\n', mapIds.Select((id, i) => $"{i,3}: {id}")));
                Console.WriteLine("Select one, multiple separated by commas or \"all\":");
                Console.Write("> ");
                var input = Console.ReadLine();
                if (input == null)
                {
                    Console.WriteLine(NoMapsChosenMessage);
                    return;
                }

                var selectedIds = new List<int>();
                if (input is "all" or "\"all\"")
                {
                    selectedIds = Enumerable.Range(0, mapIds.Length).ToList();
                }
                else
                {
                    var inputArray = input.Split(',');
                    if (inputArray.Length == 0)
                    {
                        Console.WriteLine(NoMapsChosenMessage);
                        return;
                    }

                    foreach (var idString in inputArray)
                    {
                        if (!int.TryParse(idString.Trim(), out var id))
                        {
                            Console.WriteLine(ChosenMapIdNotIntMessage(idString));
                            return;
                        }

                        selectedIds.Add(id);
                    }
                }

                var selectedMapPrototypes = new List<string>();
                foreach (var id in selectedIds)
                {
                    if (id < 0 || id >= mapIds.Length)
                    {
                        Console.WriteLine(NoMapFoundWithIdMessage(id));
                        return;
                    }

                    selectedMapPrototypes.Add(mapIds[id]);
                }

                arguments.Maps.AddRange(selectedMapPrototypes);

                if (selectedMapPrototypes.Count == 0)
                {
                    Console.WriteLine(NoMapsChosenMessage);
                    return;
                }

                Console.WriteLine($"Selected maps: {string.Join(", ", selectedMapPrototypes)}");
            }

            if (arguments.ArgumentsAreFileNames)
            {
                Console.WriteLine("Retrieving maps by file names...");
            }

            await Run(arguments);
            PoolManager.Shutdown();
        }

        private static async Task Run(CommandLineArguments arguments)
        {
            Console.WriteLine($"Creating images for {arguments.Maps.Count} maps");

            var mapNames = new List<string>();
            foreach (var map in arguments.Maps)
            {
                Console.WriteLine($"Painting map {map}");

                var mapViewerData = new MapViewerData
                {
                    Id = map,
                    Name = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(map)
                };

                mapViewerData.ParallaxLayers.Add(LayerGroup.DefaultParallax());
                var directory = Path.Combine(arguments.OutputPath, Path.GetFileNameWithoutExtension(map));

                var i = 0;
                try
                {
                    await foreach (var renderedGrid in MapPainter.Paint(map,
                                       arguments.ArgumentsAreFileNames,
                                       arguments.ShowMarkers))
                    {
                        var grid = renderedGrid.Image;
                        Directory.CreateDirectory(directory);

                        var fileName = Path.GetFileNameWithoutExtension(map);
                        var savePath = $"{directory}{Path.DirectorySeparatorChar}{fileName}-{i}.{arguments.Format}";

                        Console.WriteLine($"Writing grid of size {grid.Width}x{grid.Height} to {savePath}");

                        switch (arguments.Format)
                        {
                            case OutputFormat.webp:
                                var encoder = new WebpEncoder
                                {
                                    Method = WebpEncodingMethod.BestQuality,
                                    FileFormat = WebpFileFormatType.Lossless,
                                    TransparentColorMode = WebpTransparentColorMode.Preserve
                                };

                                await grid.SaveAsync(savePath, encoder);
                                break;

                            default:
                            case OutputFormat.png:
                                await grid.SaveAsPngAsync(savePath);
                                break;
                        }

                        grid.Dispose();

                        mapViewerData.Grids.Add(new GridLayer(renderedGrid, Path.Combine(map, Path.GetFileName(savePath))));

                        mapNames.Add(fileName);
                        i++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Painting map {map} failed due to an internal exception:");
                    Console.WriteLine(ex);
                    continue;
                }

                if (arguments.ExportViewerJson)
                {
                    var json = JsonConvert.SerializeObject(mapViewerData);
                    await File.WriteAllTextAsync(Path.Combine(arguments.OutputPath, map, "map.json"), json);
                }
            }

            var mapNamesString = $"[{string.Join(',', mapNames.Select(s => $"\"{s}\""))}]";
            Console.WriteLine($@"::set-output name=map_names::{mapNamesString}");
            Console.WriteLine($"Processed {arguments.Maps.Count} maps.");
            Console.WriteLine($"It's now safe to manually exit the process (automatic exit in a few moments...)");
        }
    }
}