namespace ServerTools.ServerCommands
{
    public record struct Message(string Id, string Text, long DequeueCount, long DlqDequeueCount, CommandMetadata Metadata, object OriginalMessage);
    
}
