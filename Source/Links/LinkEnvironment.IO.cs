﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Json;
using System.Linq;
using System.Net;
using System.Text;

namespace Mikodev.Links
{
    internal partial class LinkEnvironment
    {
        internal static readonly int SettingsMaximumCharacters = 32 * 1024;

        internal static readonly TimeSpan SettingsIOTimeout = TimeSpan.FromSeconds(10);

        internal static Uri NormalizeBroadcastUri(string text, int alternativePort)
        {
            const string prefix = "udp://";
            if (string.IsNullOrEmpty(text))
                goto fail;
            var result = text.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)
                ? new Uri(text)
                : new Uri(prefix + text);
            if (string.IsNullOrEmpty(result.Host) || result.PathAndQuery != "/")
                goto fail;
            if (string.IsNullOrEmpty(result.GetComponents(UriComponents.Port, UriFormat.SafeUnescaped)))
                result = new Uri($"{prefix}{result.Host}:{alternativePort}");
            return result;
        fail:
            throw new FormatException($"Invalid uri: '{text}'");
        }

        public void Load(string source)
        {
            var data = JsonValue.Load(new StringReader(source));
            var client = data["client"];
            var id = (string)client["id"];
            var name = (string)client["name"];
            var text = (string)client["text"];
            var imageHash = (string)client["image-hash"];

            var net = data["net"];
            var tcpPort = (ushort)net["tcp-port"];
            var udpPort = (ushort)net["udp-port"];
            var broadcastUris = ((JsonArray)net["broadcast-uris"]).Select(x => NormalizeBroadcastUri(x, udpPort)).Distinct().ToArray();

            if (string.IsNullOrEmpty(id))
                throw new InvalidDataException("Client id can not be empty!");
            if (broadcastUris.Length == 0)
                throw new InvalidDataException("No available broadcast address!");

            ClientId = id;
            ClientName = name;
            ClientText = text;
            ClientImageHash = imageHash;

            TcpEndPoint = new IPEndPoint(IPAddress.Any, tcpPort);
            UdpEndPoint = new IPEndPoint(IPAddress.Any, udpPort);
            BroadcastUris = broadcastUris;
        }

        public string Save()
        {
            Debug.Assert(!string.IsNullOrEmpty(ClientId));
            Debug.Assert(BroadcastUris != null && BroadcastUris.Length > 0);

            var client = new Dictionary<string, JsonValue>
            {
                ["id"] = ClientId,
                ["name"] = ClientName,
                ["text"] = ClientText,
                ["image-hash"] = ClientImageHash,
            };

            var net = new Dictionary<string, JsonValue>
            {
                ["tcp-port"] = TcpEndPoint.Port,
                ["udp-port"] = UdpEndPoint.Port,
                ["broadcast-uris"] = new JsonArray(BroadcastUris.Select(x => (JsonValue)x.OriginalString)),
            };

            var data = new JsonObject(new Dictionary<string, JsonValue>
            {
                ["client"] = new JsonObject(client),
                ["net"] = new JsonObject(net),
            });

            var buffer = new StringBuilder();
            var writer = new StringWriter(buffer);
            data.Save(writer);
            return buffer.ToString();
        }
    }
}
