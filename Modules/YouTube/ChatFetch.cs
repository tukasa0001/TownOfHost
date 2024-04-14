using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace YoutubeLiveChatSharp
{
    public class ChatFetch
    {
        public readonly string liveId;
        private ChatData? chatData = null;
        private readonly HttpClient client;

        public ChatFetch(string liveId)
        {
            this.liveId = liveId;
            client = new HttpClient();
        }

        public async Task<Comment[]> FetchAsync()
        {
            // 初回時にchatDataを初期化する
            if (chatData == null) await FirstFetch();

            // Postする
            HttpResponseMessage chat = await FetchChat();
            if (chat == null) return new Comment[0];
            string response = chat.Content.ReadAsStringAsync().Result;

            // パースする
            List<Comment> comments = Parse(response);

            // continuationをアップデート
            chatData.UpdateContinuation(response);

            return comments.ToArray();
        }

        public Comment[] Fetch() => FetchAsync().Result;

        private List<Comment> Parse(string response)
        {
            List<Comment> comments = new List<Comment>();

            var node = JsonNode.Parse(response);
            var a = node?["continuationContents"]?["liveChatContinuation"]?["actions"];
            if (a == null) return comments;

            foreach (var item in a.AsArray())
            {
                JsonNode? chats = item["addChatItemAction"]?["item"]?["liveChatTextMessageRenderer"];

                string message = "";
                string commentId = chats?["id"]?.ToString() ?? "";
                string userName = chats?["authorName"]?["simpleText"]?.ToString() ?? "";
                string userId = chats?["authorExternalChannelId"]?.ToString() ?? "";

                if (chats == null) continue;
                foreach (var chat in chats["message"]?["runs"]?.AsArray())
                {
                    if (chat?["text"] == null) continue;
                    message += chat["text"].ToString();
                }

                comments.Add(new Comment(message, commentId, userName, userId));
            }

            return comments;
        }

        private async Task<HttpResponseMessage> FetchChat()
        {
            // paramを作る
            var param = new Dictionary<string, string>()
            {
                ["key"] = chatData.key
            };

            // データを実際に取ってくる
            var response = await client.PostAsync(
                "https://www.youtube.com/youtubei/v1/live_chat/get_live_chat?" + "key=" + param["key"],
                new StringContent(chatData.Build()));

            // OKじゃない場合はnullを返す
            if (response.StatusCode != System.Net.HttpStatusCode.OK) return null;
            //Console.WriteLine(response.Content.ReadAsStringAsync().Result);
            return response;
        }

        private async Task FirstFetch()
        {
            client.DefaultRequestHeaders.Add(
        "User-Agent",
        "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.111 Safari/537.36");

            var param = new Dictionary<string, string>()
            {
                ["v"] = liveId
            };
            var content = await new FormUrlEncodedContent(param).ReadAsStringAsync();
            var result = await client.GetAsync("https://www.youtube.com/live_chat?" + content);
            var resultContent = await result.Content.ReadAsStringAsync();

            Match matchedKey = Regex.Match(resultContent, "\"INNERTUBE_API_KEY\":\"(.+?)\"");
            Match matchedContinuation = Regex.Match(resultContent, "\"continuation\":\"(.+?)\"");
            Match matchedVisitor = Regex.Match(resultContent, "\"visitorData\":\"(.+?)\"");
            Match matchedClient = Regex.Match(resultContent, "\"clientVersion\":\"(.+?)\"");

            chatData = new ChatData(
                matchedKey.Groups[1].Value,
                matchedContinuation.Groups[1].Value,
                matchedVisitor.Groups[1].Value,
                matchedClient.Groups[1].Value);
        }
    }
}