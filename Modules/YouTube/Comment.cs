namespace YoutubeLiveChatSharp
{
    public struct Comment
    {
        public readonly string text;
        public readonly string commentId;
        public readonly string userName;
        public readonly string userId;

        public Comment(string text,string commentId, string userName, string userId)
        {
            this.text = text;
            this.commentId = commentId;
            this.userName = userName;
            this.userId = userId;
        }
    }
}
