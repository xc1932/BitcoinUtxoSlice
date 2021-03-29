using BitcoinBlockchain.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BitcoinUtxoSlice
{
    public class Block_Pooling_Manager_Class
    {
        public List<Block> blockReadBlocksFromFile = new List<Block>();
        public Queue<Block> blockQueuePooling = new Queue<Block>();
        public List<string> forkedBlockList = new List<string>();//(正在修改.....)
        public List<string> orphanBlockList = new List<string>();//(正在修改.....)
        public List<int> blockCountOfFile = new List<int>();
        public Block blockQueueTailElement;
        public Block lastProcessedBlockElement;
        public int blockReadPointer = 0;
        public int currentLoadFileNumber = -1;

        //1.使用blk00000.dat初始化newlyReadBlocksFromFile中的区块
        public void initialize_NewlyReadBlocksFromFile()
        {
            if (blockReadBlocksFromFile.Count == 0 && currentLoadFileNumber == -1)
            {
                Blockfile_Manager_Class blockFileManager = new Blockfile_Manager_Class();
                currentLoadFileNumber++;
                string loadFilePath = Configuration_Class.preprocessedBlockFilePath + "\\blk" + currentLoadFileNumber;
                List<Block> blockList = blockFileManager.load_one_blockfile(loadFilePath);
                blockReadBlocksFromFile.AddRange(blockList);
                blockCountOfFile.Add(blockList.Count);
            }
        }

        //2.加载下一个文件中的区块
        public void load_NextFile_ToBlockPooling()
        {
            Blockfile_Manager_Class blockFileManager = new Blockfile_Manager_Class();
            currentLoadFileNumber++;
            string loadFilePath = Configuration_Class.preprocessedBlockFilePath + "\\blk" + currentLoadFileNumber;
            if (Directory.Exists(loadFilePath))
            {
                List<Block> blockList = blockFileManager.load_one_blockfile(loadFilePath);
                blockReadBlocksFromFile.AddRange(blockList);
                blockCountOfFile.Add(blockList.Count);
            }
        }

        //3.从区块池中查找下一个区块(正在修改.....)
        public bool search_NextBlock(Block priorBlock, out Block nextBlock)
        {
            for (int i = blockReadPointer; i < blockReadBlocksFromFile.Count; i++)
            {
                if (blockReadBlocksFromFile[i] != null)
                {
                    if (priorBlock.BlockHeader.BlockHash == blockReadBlocksFromFile[i].BlockHeader.PreviousBlockHash)
                    {
                        if (!forkedBlockList.Contains(blockReadBlocksFromFile[i].BlockHeader.BlockHash.ToString()))//排除分叉块
                        {
                            if (!orphanBlockList.Contains(blockReadBlocksFromFile[i].BlockHeader.BlockHash.ToString()))//排除孤块
                            {
                                nextBlock = blockReadBlocksFromFile[i];
                                blockReadPointer = i;
                                return true;
                            }
                        }
                    }
                }
            }
            if (blockReadPointer != 0)
            {
                for (int j = blockReadPointer - 1; j > 0; j--)
                {
                    if (blockReadBlocksFromFile[j] != null)
                    {
                        if (priorBlock.BlockHeader.BlockHash == blockReadBlocksFromFile[j].BlockHeader.PreviousBlockHash)
                        {
                            if (!forkedBlockList.Contains(blockReadBlocksFromFile[j].BlockHeader.BlockHash.ToString()))
                            {
                                if (!orphanBlockList.Contains(blockReadBlocksFromFile[j].BlockHeader.BlockHash.ToString()))
                                {
                                    nextBlock = blockReadBlocksFromFile[j];
                                    blockReadPointer = j;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            int oldBlockReadPointer = blockReadPointer;
            for (int m = 0; m < Configuration_Class.newlyLoadFileAmountCeiling; m++)
            {
                blockReadPointer = blockReadBlocksFromFile.Count;
                int oldAmount = blockReadBlocksFromFile.Count;
                load_NextFile_ToBlockPooling();
                if (blockReadBlocksFromFile.Count > oldAmount)
                {
                    for (int k = blockReadPointer; k < blockReadBlocksFromFile.Count; k++)
                    {
                        if (blockReadBlocksFromFile[k] != null)
                        {
                            if (priorBlock.BlockHeader.BlockHash == blockReadBlocksFromFile[k].BlockHeader.PreviousBlockHash)
                            {
                                if (!forkedBlockList.Contains(blockReadBlocksFromFile[k].BlockHeader.BlockHash.ToString()))
                                {
                                    if (!orphanBlockList.Contains(blockReadBlocksFromFile[k].BlockHeader.BlockHash.ToString()))
                                    {
                                        nextBlock = blockReadBlocksFromFile[k];
                                        blockReadPointer = k;
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            blockReadPointer = oldBlockReadPointer;
            nextBlock = null;
            return false;
        }

        //4.初始化区块队列
        public void initialize_BlockQueuePooling(Block Genesis)
        {
            Block priorBlock = Genesis;
            Block nextBlock;
            blockQueuePooling.Enqueue(priorBlock);
            for (int i = 0; i < Configuration_Class.blockQueuePoolingSize - 1; i++)
            {
                if (search_NextBlock(priorBlock, out nextBlock))
                {
                    blockQueuePooling.Enqueue(nextBlock);
                    if (i == Configuration_Class.blockQueuePoolingSize - 2)
                    {
                        blockQueueTailElement = nextBlock;
                    }
                    priorBlock = nextBlock;
                }
                else
                {
                    Console.WriteLine("已经找到" + i + 1 + "个区块，无法找到下一个区块");
                }
            }
        }

        //5.从区块队列池中取出一个区块
        public Block dequeue_FromBlockQueuePooling()
        {
            Block dequeueBlock = null;
            if (blockQueuePooling.Count != 0)
            {
                dequeueBlock = blockQueuePooling.Dequeue();
                lastProcessedBlockElement = dequeueBlock;
                for (int i = 0; i < blockReadPointer; i++)
                {
                    if (blockReadBlocksFromFile[i] != null)
                    {
                        if (blockReadBlocksFromFile[i].BlockHeader.BlockHash == dequeueBlock.BlockHeader.BlockHash)
                        {
                            blockReadBlocksFromFile[i] = null;
                            return dequeueBlock;
                        }
                    }
                }
                for (int j = blockReadPointer; j < blockReadBlocksFromFile.Count; j++)
                {
                    if (blockReadBlocksFromFile[j] != null)
                    {
                        if (blockReadBlocksFromFile[j].BlockHeader.BlockHash == dequeueBlock.BlockHeader.BlockHash)
                        {
                            blockReadBlocksFromFile[j] = null;
                            return dequeueBlock;
                        }
                    }
                }
            }
            return dequeueBlock;
        }

        //6.向区块队列中补充区块(正在修改.....)
        public bool enqueue_ToBlockQueuePooling()
        {
            int invalidBlockCount = 0;
            Queue<Block> tempBlockQueuePooling = new Queue<Block>();
            Block priorBlock = blockQueueTailElement;
            Block tailBlock;
            while (blockQueuePooling.Count < 10)
            {
                if (search_NextBlock(priorBlock, out tailBlock))
                {
                    blockQueuePooling.Enqueue(tailBlock);
                }
                else
                {
                    invalidBlockCount++;
                    tempBlockQueuePooling.Clear();
                    for (int i = 0; i < blockQueuePooling.Count - 1; i++)
                    {
                        tempBlockQueuePooling.Enqueue(blockQueuePooling.Dequeue());
                    }
                    forkedBlockList.Add(blockQueuePooling.Dequeue().BlockHeader.BlockHash.ToString());//向分叉块列表中添加分叉上的块
                    Console.WriteLine("出现分叉上的块");
                    blockQueuePooling.Clear();
                    for (int j = 0; j < tempBlockQueuePooling.Count; j++)
                    {
                        Block dequeueBlock = tempBlockQueuePooling.Dequeue();
                        blockQueuePooling.Enqueue(dequeueBlock);
                        if (j == tempBlockQueuePooling.Count - 1)
                        {
                            tailBlock = dequeueBlock;
                        }
                    }
                }
                if (blockQueuePooling.Count == 10)
                {
                    blockQueueTailElement = tailBlock;
                }
                if (blockQueuePooling.Count == 0)
                {
                    return false;
                }
            }
            return true;
        }

    }
}
