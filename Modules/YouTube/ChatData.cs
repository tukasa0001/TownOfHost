using System.Text.Json.Nodes;

namespace YoutubeLiveChatSharp
{
	internal class ChatData
	{
		public readonly string key;
		public string continuation { get; private set; }
		public readonly string visitorData;
		public readonly string clientVersion;

		public ChatData(string key, string content, string visitorData, string clientVersion)
		{
			this.key = key;
			continuation = content;
			this.visitorData = visitorData;
			this.clientVersion = clientVersion;
		}

		public void UpdateContinuation(string postResult)
		{
			var node = JsonNode.Parse(postResult);
			var node2 = node["continuationContents"]["liveChatContinuation"]["continuations"][0];
			var node3 = node2["invalidationContinuationData"] ?? node2["timedContinuationData"];
			continuation = node3["continuation"].ToString();
		}

		public string Build()
		{
			return $"{{\"context\": {{\"client\": {{\"visitorData\": \"{visitorData}\",\"User-Agent\": \"Mozilla / 5.0(Windows NT 6.3; Win64; x64) AppleWebKit / 537.36(KHTML, like Gecko) Chrome / 86.0.4240.111 Safari / 537.36\",\"clientName\": \"WEB\",\"clientVersion\": \"{clientVersion}\"}}}},\"continuation\": \"{continuation}\"}}";
		}
	}
}