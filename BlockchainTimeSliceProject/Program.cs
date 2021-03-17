using BitcoinBlockchain.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BlockchainTimeSliceProject
{
    class Program
    {
        static void Main(string[] args)
        {
            ////1.测试移动文件使用时间
            //Stopwatch timer = new Stopwatch();
            //timer.Start();
            //string blockFileSourcePath = @"E:\BitcoinRoaming\blocks";
            //string blockFileDestinationPath = @"E:\Code\BlockFile";
            //Blockfile_Manager_Class bmc = new Blockfile_Manager_Class(blockFileSourcePath, blockFileDestinationPath);
            //bmc.blockfiles_preprocessing(50);
            //timer.Stop();
            //Console.WriteLine("移动50个文件执行用时:" + timer.Elapsed);

            ////2.测试读取文件用时
            //Stopwatch timer1 = new Stopwatch();
            //timer1.Start();
            //Block_Pooling_Manager_Class bpmc = new Block_Pooling_Manager_Class();
            //bpmc.initialize_NewlyReadBlocksFromFile();
            //Console.WriteLine(bpmc.blockReadBlocksFromFile.Count);
            //bpmc.load_NextFile_ToBlockPooling();
            //Console.WriteLine(bpmc.blockReadBlocksFromFile.Count);
            //timer1.Stop();
            //Console.WriteLine("加载第2文件执行用时:" + timer1.Elapsed);

            ////3.测试查找下一个区块功能
            //Stopwatch timer2 = new Stopwatch();
            //timer2.Start();
            //Blockfile_Manager_Class bmc = new Blockfile_Manager_Class();
            //string loadFilePath = @"E:\Code\BlockFile\blk0";
            //List<Block> blockList = bmc.load_one_blockfile(loadFilePath);
            //Block_Pooling_Manager_Class bpmc = new Block_Pooling_Manager_Class();
            //Block priorBlock = blockList[0];
            //Block nextBlock;
            //int blockAmount = 1;
            //Console.WriteLine("前一个区块hash:" + blockList[0].BlockHeader.PreviousBlockHash);
            //Console.WriteLine("第" + blockAmount + "区块time:" + blockList[0].BlockHeader.BlockTimestamp);
            //Console.WriteLine("第" + blockAmount + "区块hash:" + blockList[0].BlockHeader.BlockHash);
            //while (bpmc.search_NextBlock(priorBlock, out nextBlock))
            //{
            //    blockAmount++;
            //    Console.WriteLine("前一个区块hash:" + nextBlock.BlockHeader.PreviousBlockHash);
            //    Console.WriteLine("第" + blockAmount + "区块time:" + nextBlock.BlockHeader.BlockTimestamp);
            //    Console.WriteLine("第" + blockAmount + "区块hash:" + nextBlock.BlockHeader.BlockHash);
            //    priorBlock = nextBlock;
            //    bpmc.search_NextBlock(priorBlock, out nextBlock);
            //    if (blockAmount == 30000)
            //    {
            //        break;
            //    }
            //}
            //timer2.Stop();
            //Console.WriteLine("获取30000区块执行用时:" + timer2.Elapsed);

            ////4.测试区块队列池初始化情况
            //Blockfile_Manager_Class bmc = new Blockfile_Manager_Class();
            //string loadFilePath = @"E:\Code\BlockFile\blk0";
            //List<Block> blockList = bmc.load_one_blockfile(loadFilePath);
            //Block_Pooling_Manager_Class bpmc = new Block_Pooling_Manager_Class();
            //Stopwatch timer3 = new Stopwatch();
            //timer3.Start();
            //bpmc.initialize_BlockQueuePooling(blockList[0]);
            //timer3.Stop();
            //Console.WriteLine(bpmc.blockQueuePooling.Count);
            //int count = 1;
            //foreach (Block block in bpmc.blockQueuePooling)
            //{
            //    Console.WriteLine("前一个区块hash:" + block.BlockHeader.PreviousBlockHash);
            //    Console.WriteLine("第" + count + "区块time:" + block.BlockHeader.BlockTimestamp);
            //    Console.WriteLine("第" + count + "区块hash:" + block.BlockHeader.BlockHash);
            //    count++;
            //}
            //Console.WriteLine("尾区块hash:" + bpmc.blockQueueTailElement.BlockHeader.PreviousBlockHash);
            //Console.WriteLine("尾区块time:" + bpmc.blockQueueTailElement.BlockHeader.BlockTimestamp);
            //Console.WriteLine("尾区块hash:" + bpmc.blockQueueTailElement.BlockHeader.BlockHash);
            //Console.WriteLine("初始化区块队列执行用时:" + timer3.Elapsed);

            ////5.测试区块队列出队功能
            //Blockfile_Manager_Class bmc = new Blockfile_Manager_Class();
            //string loadFilePath = @"E:\Code\BlockFile\blk0";
            //List<Block> blockList = bmc.load_one_blockfile(loadFilePath);
            //Block_Pooling_Manager_Class bpmc = new Block_Pooling_Manager_Class();
            //bpmc.initialize_BlockQueuePooling(blockList[0]);
            //Block block1 = bpmc.dequeue_FromBlockQueuePooling();
            //Block block2 = bpmc.dequeue_FromBlockQueuePooling();
            //Console.WriteLine("前一个区块hash:" + block1.BlockHeader.PreviousBlockHash);
            //Console.WriteLine("区块time:" + block1.BlockHeader.BlockTimestamp);
            //Console.WriteLine("区块hash:" + block1.BlockHeader.BlockHash);
            //Console.WriteLine("前一个区块hash:" + block2.BlockHeader.PreviousBlockHash);
            //Console.WriteLine("区块time:" + block2.BlockHeader.BlockTimestamp);
            //Console.WriteLine("区块hash:" + block2.BlockHeader.BlockHash);
            //Console.WriteLine(bpmc.blockReadBlocksFromFile.Contains(block1));
            //Console.WriteLine(bpmc.blockReadBlocksFromFile.Contains(block2));

            ////6.测试区块队列入队功能
            //Blockfile_Manager_Class bmc = new Blockfile_Manager_Class();
            //string loadFilePath = @"E:\Code\BlockFile\blk0";
            //List<Block> blockList = bmc.load_one_blockfile(loadFilePath);
            //Block_Pooling_Manager_Class bpmc = new Block_Pooling_Manager_Class();
            //bpmc.initialize_BlockQueuePooling(blockList[0]);
            //Block block1 = bpmc.dequeue_FromBlockQueuePooling();
            //Console.WriteLine(bpmc.blockQueuePooling.Count);
            //bpmc.enqueue_ToBlockQueuePooling();
            //Console.WriteLine(bpmc.blockQueuePooling.Count);
            //Block block2 = bpmc.dequeue_FromBlockQueuePooling();
            //Console.WriteLine(bpmc.blockQueuePooling.Count);
            //bpmc.enqueue_ToBlockQueuePooling();
            //Console.WriteLine(bpmc.blockQueuePooling.Count);

            ////7.测试系统partA执行情况
            Block_Processing_Class bpc = new Block_Processing_Class();
            bpc.run();
            //123

            ////8.测试区块文件中区块总数
            //Blockfile_Manager_Class bmc = new Blockfile_Manager_Class();
            //Console.WriteLine(bmc.calculate_AmountOfBlockFiles(50));
        }
    }
}
