using BitcoinBlockchain.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitcoinUTXOSliceLibrary
{
    public class Test_Class
    {
        public void sameTransaction_Test()
        {
            string path = @"E:\Code\BlockFile\blk0";
            Blockfile_Manager_Class bmc = new Blockfile_Manager_Class();
            List<Block> blockList = bmc.load_one_blockfile(path);
            foreach (Block block in blockList)
            {
                if (block.BlockHeader.BlockTimestamp.ToString() == "2010/11/14 17:59:48" || block.BlockHeader.BlockTimestamp.ToString() == "2010/11/14 21:04:51")
                {
                    Console.WriteLine("=====================================================");
                    Console.WriteLine("区块hash:" + block.BlockHeader.BlockHash.ToString());
                    Console.WriteLine("区块时间:" + block.BlockHeader.BlockTimestamp.ToString());
                    Console.WriteLine("交易数量:" + block.Transactions.Count);
                    Console.WriteLine("-----------------------------------------------");
                    foreach (Transaction tx in block.Transactions)
                    {
                        Console.WriteLine("txhash:" + tx.TransactionHash);
                        Console.WriteLine("**********输入节点:**********");
                        if (tx.Inputs.Count != 0)
                        {
                            foreach (TransactionInput txi in tx.Inputs)
                            {
                                Console.WriteLine("原交易hash" + txi.SourceTransactionHash);
                                Console.WriteLine("索引:" + txi.SourceTransactionOutputIndex);
                                Console.WriteLine("输入脚本:" + txi.InputScript);
                            }
                        }
                        Console.WriteLine("**********输出节点:**********");
                        if (tx.Outputs.Count != 0)
                        {
                            foreach (TransactionOutput txo in tx.Outputs)
                            {
                                Console.WriteLine("value:" + txo.OutputValueSatoshi);
                                Console.WriteLine("输出脚本:" + txo.OutputScript);
                            }
                        }
                        Console.WriteLine("-----------------------------------------------");
                    }
                    Console.WriteLine("=====================================================");
                }
            }
        }
    }
}
