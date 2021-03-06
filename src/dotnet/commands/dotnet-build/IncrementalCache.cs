// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Tools.Build
{
    internal class IncrementalCache
    {
        private const string InputsKeyName = "inputs";
        private const string OutputsKeyNane = "outputs";

        public CompilerIO CompilerIO { get; }

        public IncrementalCache(CompilerIO compilerIO)
        {
            CompilerIO = compilerIO;
        }

        public void WriteToFile(string cacheFile)
        {
            try
            {
                CreatePathIfAbsent(cacheFile);

                using (var streamWriter = new StreamWriter(new FileStream(cacheFile, FileMode.Create, FileAccess.Write, FileShare.None)))
                {
                    var rootObject = new JObject();
                    rootObject[InputsKeyName] = new JArray(CompilerIO.Inputs);
                    rootObject[OutputsKeyNane] = new JArray(CompilerIO.Outputs);

                    JsonSerializer.Create().Serialize(streamWriter, rootObject);
                }
            }
            catch (Exception e)
            {
                throw new InvalidDataException($"Could not write the incremental cache file: {cacheFile}", e);
            }
        }

        private static void CreatePathIfAbsent(string filePath)
        {
            var parentDir = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }
        }

        public static IncrementalCache ReadFromFile(string cacheFile)
        {
            try
            {
                using (var streamReader = new StreamReader(new FileStream(cacheFile, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    var jObject = JObject.Parse(streamReader.ReadToEnd());

                    if (jObject == null)
                    {
                        throw new InvalidDataException();
                    }

                    var inputs = ReadArray<string>(jObject, InputsKeyName);
                    var outputs = ReadArray<string>(jObject, OutputsKeyNane);

                    return new IncrementalCache(new CompilerIO(inputs, outputs));
                }
            }
            catch (Exception e)
            {
                throw new InvalidDataException($"Could not read the incremental cache file: {cacheFile}", e);
            }
        }

        private static IEnumerable<T> ReadArray<T>(JObject jObject, string keyName)
        {
            var array = jObject.Value<JToken>(keyName)?.Values<T>();

            if (array == null)
            {
                throw new InvalidDataException($"Could not read key {keyName}");
            }

            return array;
        }
    }
}
