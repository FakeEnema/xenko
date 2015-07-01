﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Globalization;
using System.IO;

using SharpYaml;
using SharpYaml.Events;
using SharpYaml.Serialization;
using SiliconStudio.Assets.Serializers;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.Yaml;

namespace SiliconStudio.Assets
{
    /// <summary>
    /// Helper for migrating asset to newer versions.
    /// </summary>
    static class AssetMigration
    {
        public static bool MigrateAssetIfNeeded(ILogger log, Package.LoadAssetFile loadAsset)
        {
            var assetFullPath = loadAsset.FilePath.FullPath;

            // Determine if asset was Yaml or not
            var assetFileExtension = Path.GetExtension(assetFullPath);
            if (assetFileExtension == null)
                return false;

            assetFileExtension = assetFileExtension.ToLowerInvariant();

            var serializer = AssetSerializer.FindSerializer(assetFileExtension);
            if (!(serializer is AssetYamlSerializer))
                return false;

            // We've got a Yaml asset, let's get expected and serialized versions
            var serializedVersion = 0;
            int expectedVersion;
            Type assetType;

            // Read from Yaml file the asset version and its type (to get expected version)
            // Note: It tries to read as few as possible (SerializedVersion is expected to be right after Id, so it shouldn't try to read further than that)
            using (var assetStream = loadAsset.OpenStream())
            using (var streamReader = new StreamReader(assetStream))
            {
                var yamlEventReader = new EventReader(new Parser(streamReader));

                // Skip header
                yamlEventReader.Expect<StreamStart>();
                yamlEventReader.Expect<DocumentStart>();
                var mappingStart = yamlEventReader.Expect<MappingStart>();

                var yamlSerializerSettings = YamlSerializer.GetSerializerSettings();
                var tagTypeRegistry = yamlSerializerSettings.TagTypeRegistry;
                assetType = tagTypeRegistry.TypeFromTag(mappingStart.Tag);

                expectedVersion = AssetRegistry.GetCurrentFormatVersion(assetType);

                Scalar assetKey;
                while ((assetKey = yamlEventReader.Allow<Scalar>()) != null)
                {
                    // Only allow Id before SerializedVersion
                    if (assetKey.Value == "Id")
                    {
                        yamlEventReader.Skip();
                        continue;
                    }
                    if (assetKey.Value == "SerializedVersion")
                    {
                        serializedVersion = Convert.ToInt32(yamlEventReader.Expect<Scalar>().Value, CultureInfo.InvariantCulture);
                        break;
                    }
                }
            }

            if (serializedVersion > expectedVersion)
            {
                // Try to open an asset newer than what we support (probably generated by a newer Paradox)
                throw new InvalidOperationException(string.Format("Asset of type {0} has been serialized with newer version {1}, but only version {2} is supported. Was this asset created with a newer version of Paradox?", assetType, serializedVersion, expectedVersion));
            }

            if (serializedVersion < expectedVersion)
            {
                // Perform asset upgrade
                log.Verbose("{0} needs update, from version {1} to version {2}", Path.GetFullPath(assetFullPath), serializedVersion, expectedVersion);

                // Load the asset as a YamlNode object
                var input = new StringReader(File.ReadAllText(assetFullPath));
                var yamlStream = new YamlStream();
                yamlStream.Load(input);
                var yamlRootNode = (YamlMappingNode)yamlStream.Documents[0].RootNode;

                // Check if there is any asset updater
                var assetUpgraders = AssetRegistry.GetAssetUpgraders(assetType);
                if (assetUpgraders == null)
                {
                    throw new InvalidOperationException(string.Format("Asset of type {0} should be updated from version {1} to {2}, but no asset migration path was found", assetType, serializedVersion, expectedVersion));
                }

                // Instantiate asset updaters
                var currentVersion = serializedVersion;
                while (currentVersion != expectedVersion)
                {
                    int targetVersion;
                    // This will throw an exception if no upgrader is available for the given version, exiting the loop in case of error.
                    var upgrader = assetUpgraders.GetUpgrader(currentVersion, out targetVersion);
                    upgrader.Upgrade(currentVersion, targetVersion, log, yamlRootNode);
                    currentVersion = targetVersion;
                }

                // Make sure asset is updated to latest version
                YamlNode serializedVersionNode;
                var newSerializedVersion = 0;
                if (yamlRootNode.Children.TryGetValue(new YamlScalarNode("SerializedVersion"), out serializedVersionNode))
                {
                    newSerializedVersion = Convert.ToInt32(((YamlScalarNode)serializedVersionNode).Value);
                }

                if (newSerializedVersion != expectedVersion)
                {
                    throw new InvalidOperationException(string.Format("Asset of type {0} was migrated, but still its new version {1} doesn't match expected version {2}.", assetType, newSerializedVersion, expectedVersion));
                }

                log.Info("{0} updated from version {1} to version {2}", Path.GetFullPath(assetFullPath), serializedVersion, expectedVersion);

                var preferredIndent = YamlSerializer.GetSerializerSettings().PreferredIndent;

                // Save asset back to disk
                using (var memoryStream = new MemoryStream())
                {
                    using (var streamWriter = new StreamWriter(memoryStream))
                    {
                        yamlStream.Save(streamWriter, true, preferredIndent);
                    }
                    loadAsset.AssetContent = memoryStream.ToArray();
                }

                return true;
            }

            return false;
        }
    }
}