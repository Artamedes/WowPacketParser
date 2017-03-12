using System.Collections.Generic;
using WowPacketParser.Messages.Submessages;
using WowPacketParser.Misc;

namespace WowPacketParser.Messages
{
    public unsafe struct ClientTransferAborted
    {
        public TransferAbort TransfertAbort;
        public byte Arg;
        public uint MapID;
    }
}