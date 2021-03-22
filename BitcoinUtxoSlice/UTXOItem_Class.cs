﻿using System;
using System.Collections.Generic;
using System.Text;

namespace BitcoinUtxoSlice
{
    class UTXOItem_Class
    {
        public string txhash;
        public uint index;
        public ulong value;
        public string script;

        public UTXOItem_Class() { }

        public UTXOItem_Class(string txhash, uint index, ulong value, string script)
        {
            this.txhash = txhash;
            this.index = index;
            this.value = value;
            this.script = script;
        }
    }
}
