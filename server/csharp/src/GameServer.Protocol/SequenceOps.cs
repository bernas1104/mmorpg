namespace GameServer.Protocol;

public static class SequenceOps
{
    public static bool IsSequenceNewer(ushort incoming, ushort current) =>
        (incoming - current) < 32768;   // Check if the incoming sequence is newer than the current sequence, considering wrap-around
}
