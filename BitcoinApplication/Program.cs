using System;
using System.Collections.Generic;
using System.Linq;
using BitcoinBlockchain.Data;
using BitcoinBlockchain.Parser;
using OrderedBlockchainParser;
using UTXOSlicer;

namespace BitcoinApplication
{
    class Program
    {
         public static void Main(string[] args)
        {
            //IV.UTXOSlicer
            UTXOSlicer_Class uTXOSlicer = new UTXOSlicer_Class();
            uTXOSlicer.UTXOSlicer_Class_Test();
        }
    }
}
