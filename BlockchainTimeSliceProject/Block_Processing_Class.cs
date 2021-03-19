using BitcoinBlockchain.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace BitcoinUtxoSlice
{
    class Block_Processing_Class
    {
        public void run() {
            //获取创世块
            Blockfile_Manager_Class bmc = new Blockfile_Manager_Class();
            string loadFilePath = @"E:\Code\BlockFile\blk0";
            List<Block> blockList = bmc.load_one_blockfile(loadFilePath);
            Block genesisBlock = blockList[0];
            Console.WriteLine("创世区块前一个区块hash:" + genesisBlock.BlockHeader.PreviousBlockHash);
            Console.WriteLine("创世区块time:" + genesisBlock.BlockHeader.BlockTimestamp);
            Console.WriteLine("创世区块hash:" + genesisBlock.BlockHeader.BlockHash);
            Console.WriteLine("**********************************************************");
            Stopwatch timer = new Stopwatch();
            timer.Start();
            Block_Pooling_Manager_Class bpmc = new Block_Pooling_Manager_Class();
            bpmc.initialize_BlockQueuePooling(genesisBlock);
            int processedBlockAmount = 0;
            Block readyBlock;
            while (bpmc.blockQueuePooling.Count != 0) {
                if ((readyBlock = bpmc.dequeue_FromBlockQueuePooling())!=null) {
                    processedBlockAmount++;
                } 
                virtual_BlockProcessing(readyBlock, processedBlockAmount);
                bool finished=bpmc.enqueue_ToBlockQueuePooling();
                if (!finished) {
                    break;
                }
            }
            timer.Stop();
            Console.WriteLine("解析50个文件总用时:"+timer.Elapsed);
        }

        public void virtual_BlockProcessing(Block readyBlock,int processedBlockAmount) {
            Console.WriteLine("正在处理第"+ processedBlockAmount + "个区块..............");
            Console.WriteLine("前一个区块hash:" + readyBlock.BlockHeader.PreviousBlockHash);
            Console.WriteLine("区块time:" + readyBlock.BlockHeader.BlockTimestamp);
            Console.WriteLine("区块hash:" + readyBlock.BlockHeader.BlockHash);
            Console.WriteLine("----------------------------------------------------------------------");
        }
    }
}
