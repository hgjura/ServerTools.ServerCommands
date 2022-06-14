namespace ServerTools.ServerCommands
{
    public record Message(string Id, string Text, long DequeueCount, long DlqDequeueCount, CommandMetadata Metadata, object OriginalMessage);
    
}
