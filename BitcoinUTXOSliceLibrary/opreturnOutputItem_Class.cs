using System;
using System.Collections.Generic;
using System.Text;

namespace BitcoinUTXOSliceLibrary
{
    public class opreturnOutputItem_Class
    {
        public string txhash;
        public uint index;
        public ulong value;
        public string script;
        public int utxoItemAmount = 1;

        public opreturnOutputItem_Class() { }

        public opreturnOutputItem_Class(string txhash, uint index, ulong value, string script)
        {
            this.txhash = txhash;
            this.index = index;
            this.value = value;
            this.script = script;
        }
    }
}
