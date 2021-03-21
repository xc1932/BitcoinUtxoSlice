using BitcoinBlockchain.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BitcoinUtxoSlice
{
    class Block_Pooling_Manager_Class
    {
        public List<Block> blockReadBlocksFromFile = new List<Block>();
        public Queue<Block> blockQueuePooling = new Queue<Block>();
        public List<FileStatus_Class> fileStatusList = new List<FileStatus_Class>();
        public Block blockQueueTailElement;
        int blockReadPointer = 0;
        int currentLoadFileNumber = -1;

        //1.使用blk00000.dat初始化newlyReadBlocksFromFile中的区块
        public void initialize_NewlyReadBlocksFromFile()
        {
            if (blockReadBlocksFromFile.Count == 0 && currentLoadFileNumber == -1)
            {
                Blockfile_Manager_Class blockFileManager = new Blockfile_Manager_Class();
                currentLoadFileNumber++;
                string loadFilePath = @"E:\Code\BlockFile\blk" + currentLoadFileNumber;
                blockReadBlocksFromFile = blockFileManager.load_one_blockfile(loadFilePath);
            }
        }

        //2.加载下一个文件中的区块
        public void load_NextFile_ToBlockPooling()
        {
            Blockfile_Manager_Class blockFileManager = new Blockfile_Manager_Class();
            currentLoadFileNumber++;
            string loadFilePath = @"E:\Code\BlockFile\blk" + currentLoadFileNumber;
            if (Directory.Exists(loadFilePath)) {
                List<Block> newlyReadBlocksFromFile = blockFileManager.load_one_blockfile(loadFilePath);
                string fileName = "blk"+ currentLoadFileNumber;
                add_FileStatusObject_ToFileStatusList(fileName, newlyReadBlocksFromFile.Count);
                blockReadBlocksFromFile.AddRange(blockFileManager.load_one_blockfile(loadFilePath));
            }  
        }

        //3.从区块池中查找下一个区块
        public bool search_NextBlock(Block priorBlock,out Block nextBlock) {
            for (int i=blockReadPointer;i< blockReadBlocksFromFile.Count;i++) {
                if (blockReadBlocksFromFile[i]!=null) {
                    if (priorBlock.BlockHeader.BlockHash == blockReadBlocksFromFile[i].BlockHeader.PreviousBlockHash)
                    {
                        nextBlock = blockReadBlocksFromFile[i];
                        blockReadPointer = i;
                        return true;
                    }
                } 
            }
            if (blockReadPointer != 0) {
                for (int j = blockReadPointer - 1; j > 0; j--)
                {
                    if (blockReadBlocksFromFile[j] != null)
                    {
                        if (priorBlock.BlockHeader.BlockHash == blockReadBlocksFromFile[j].BlockHeader.PreviousBlockHash)
                        {
                            nextBlock = blockReadBlocksFromFile[j];
                            blockReadPointer = j;
                            return true;
                        }
                    }
                }
            }
            int oldBlockReadPointer = blockReadPointer;
            for (int m=0;m<Configuration_Class.newlyLoadFileAmountCeiling;m++) {
                blockReadPointer = blockReadBlocksFromFile.Count;
                int oldAmount = blockReadBlocksFromFile.Count;
                load_NextFile_ToBlockPooling();
                if (blockReadBlocksFromFile.Count>oldAmount) {
                    for (int k = blockReadPointer; k < blockReadBlocksFromFile.Count; k++)
                    {
                        if (blockReadBlocksFromFile[k] != null)
                        {
                            if (priorBlock.BlockHeader.BlockHash == blockReadBlocksFromFile[k].BlockHeader.PreviousBlockHash)
                            {
                                nextBlock = blockReadBlocksFromFile[k];
                                blockReadPointer = k;
                                return true;
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
            }
        }

        //5.从区块队列池中取出一个区块
        public Block dequeue_FromBlockQueuePooling() {
            Block dequeueBlock = null;
            if(blockQueuePooling.Count!=0){
                dequeueBlock = blockQueuePooling.Dequeue();
                for(int i=0;i< blockReadPointer;i++)
                {
                    if (blockReadBlocksFromFile[i]!=null) {
                        if (blockReadBlocksFromFile[i].BlockHeader.BlockHash==dequeueBlock.BlockHeader.BlockHash) {
                            blockReadBlocksFromFile[i] = null;
                            update_FileStatus_ForFileStatusList(i);
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
                            update_FileStatus_ForFileStatusList(j);
                            return dequeueBlock;
                        }
                    }
                }
            }
            return dequeueBlock;
        }

        //6.向区块队列中补充区块
        public bool enqueue_ToBlockQueuePooling(){
            int invalidBlockCount = 0;
            Queue<Block> tempBlockQueuePooling = new Queue<Block>();
            Block priorBlock = blockQueueTailElement;
            Block tailBlock;
            while(blockQueuePooling.Count<10){
                if (search_NextBlock(priorBlock, out tailBlock))
                {
                    blockQueuePooling.Enqueue(tailBlock);
                }
                else {
                    invalidBlockCount++;
                    tempBlockQueuePooling.Clear();
                    for (int i=0;i< blockQueuePooling.Count-1;i++) {
                        tempBlockQueuePooling.Enqueue(blockQueuePooling.Dequeue());
                    }
                    blockQueuePooling.Clear();
                    for (int j=0;j<tempBlockQueuePooling.Count;j++) {
                        Block dequeueBlock = tempBlockQueuePooling.Dequeue();
                        blockQueuePooling.Enqueue(dequeueBlock);
                        if (j==tempBlockQueuePooling.Count-1) {
                            tailBlock = dequeueBlock;
                        }
                    }
                }
                if (blockQueuePooling.Count==10) {
                    blockQueueTailElement = tailBlock;
                }
                if (blockQueuePooling.Count==0) {
                    return false;
                }
            }
            return true;
        }

        //7.向文件状态列表中添加文件状态
        public void add_FileStatusObject_ToFileStatusList(string fileName, int totalBlocks)
        {
            FileStatus_Class fileStatusItem = new FileStatus_Class(fileName, totalBlocks);
            fileStatusList.Add(fileStatusItem);
        }

        //8.更新文件状态列表中文件的状态
        public void update_FileStatus_ForFileStatusList(int indexOfBlockAtBlockReadBlocksFromFile) {
            int indexAtBlockFile = indexOfBlockAtBlockReadBlocksFromFile+1;
            for (int i=0;i< fileStatusList.Count;i++) {
                if (indexAtBlockFile > fileStatusList[i].totalBlocks)
                {
                    indexAtBlockFile -= fileStatusList[i].totalBlocks;
                }
                else {
                    fileStatusList[i].unusedBlocks--;
                    fileStatusList[i].blockStatusArray[indexAtBlockFile-1]=1;//1为已经使用了
                    break;
                }

            }
        }


    }
}
