using BitcoinBlockchain.Data;
using BitcoinBlockchain.Parser;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace OrderedBlockchainParser
{
    public class OrderedBlockchainParser_Class
    {        
        public string blockchainFilePath = @".\";           //区块链文件路径        
        public string blockProcessContextFilePath = @".\";  //区块解析中断时的上下文(程序状态)文件路径
        public string blockProcessContextFileName = null;   //区块解析中断时的上下文(程序状态)文件
        public int blockQueuePoolingSize = 10;              //区块池长度
        int newlyLoadFileAmountCeiling = 10;                 //查找文件上限

        public List<FileStatusItem_Class> fileStatusList = new List<FileStatusItem_Class>();
        public List<ParserBlock> blockReadBlocksFromFile = new List<ParserBlock>();
        public Queue<ParserBlock> blockQueuePooling = new Queue<ParserBlock>();
        public List<string> forkedBlockList = new List<string>();
        public List<string> orphanBlockList = new List<string>();
        public List<int> blockCountOfFile = new List<int>();
        public ParserBlock blockQueueTailElement;
        public ParserBlock lastProcessedBlockElement;
        public int processedBlockAmount = 0;
        public int blockReadPointer = 0;
        public int currentLoadFileNumber = -1;
        public DateTime recentlySliceDateTime;
        public bool missingBlockFile = false;

        //全部使用默认值，并且不从中断处恢复
        public OrderedBlockchainParser_Class() {
            ParserBlock genesisBlock = get_GenesisBlock(true);
            initialize_BlockQueuePooling(genesisBlock);
        }

        //主要使用                                      
        public OrderedBlockchainParser_Class(string blockchainFilePath, string blockProcessContextFilePath, 
            string blockProcessContextFileName=null,int blockQueuePoolingSize=10) 
        {
            this.blockchainFilePath = blockchainFilePath;
            this.blockProcessContextFilePath = blockProcessContextFilePath;
            this.blockProcessContextFileName = blockProcessContextFileName;
            this.blockQueuePoolingSize = blockQueuePoolingSize;

            if (blockProcessContextFileName == null)
            {
                ParserBlock genesisBlock = get_GenesisBlock(true);
                initialize_BlockQueuePooling(genesisBlock);
                recentlySliceDateTime = genesisBlock.Header.BlockTime.DateTime;
            }
            else
            { //从中断处恢复
                restore_BlockProcessContextForProgram();
            }
        }

        //I.获取下一个区块
        public ParserBlock getNextBlock()
        {
            ParserBlock nextBlock = dequeue_FromBlockQueuePooling();
            processedBlockAmount++;
            if (!enqueue_ToBlockQueuePooling()) {
                if (missingBlockFile) {
                    Console.WriteLine("本次请求区块已返回,但没有充足的区块文件补区块池。区块队列无法中添加下一个区块!!!");
                }                
            }
            return nextBlock;           
        }

        //II.保存区块解析中断时的上下文状态
        public void saveBlockProcessContext() 
        {
            if (countOfFileBlock(blockCountOfFile) != blockReadBlocksFromFile.Count)
            {
                Console.WriteLine("读取文件总数和区块池文件总数不一致");
            }
            else
            {
                record_OrphanBlock();
                fileStatusList.Clear();
                int readBlockPointer = 0;
                for (int i = 0; i < blockCountOfFile.Count; i++)
                {
                    string fileName = get_filename(i);                    
                    int totalBlocks = blockCountOfFile[i];
                    int unusedBlocks = 0;
                    List<string> unusedBlockHash = new List<string>();
                    for (int j = 0; j < blockCountOfFile[i]; j++)
                    {
                        if (blockReadBlocksFromFile[readBlockPointer] != null)
                        {
                            unusedBlockHash.Add(blockReadBlocksFromFile[readBlockPointer].Header.GetHash().ToString());
                            unusedBlocks++;
                        }
                        readBlockPointer++;
                    }
                    fileStatusList.Add(new FileStatusItem_Class(fileName, totalBlocks, unusedBlocks, unusedBlockHash));
                }
                string blockProcessContextFileFinalPath = Path.Combine(blockProcessContextFilePath,"BPC_"+ processedBlockAmount+ "_"+
                    lastProcessedBlockElement.Header.BlockTime.ToString("yyyy年MM月dd日HH时mm分ss秒") + ".dat");
                BlockProcessContextModel_Class blockProcessContextModel = new BlockProcessContextModel_Class(fileStatusList,forkedBlockList,orphanBlockList,
                    lastProcessedBlockElement.Header.GetHash().ToString(),processedBlockAmount,blockReadPointer,currentLoadFileNumber,
                    lastProcessedBlockElement.Header.BlockTime.DateTime);
                using (StreamWriter sw = File.CreateText(blockProcessContextFileFinalPath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(sw, blockProcessContextModel);
                }
                Compress(blockProcessContextFileFinalPath,true);
            }
        }

        //III.依赖函数
        //a.根据序号得到区块文件名
        public string get_filename(int number)
        {
            StringBuilder fileNameStr = new StringBuilder("blk");
            if (number >= 0 && number <= 9)
            {
                fileNameStr.Append("0000").Append(number).Append(".dat");
            }
            else if (number >= 10 && number <= 99)
            {
                fileNameStr.Append("000").Append(number).Append(".dat");
            }
            else if (number >= 100 && number <= 999)
            {
                fileNameStr.Append("00").Append(number).Append(".dat");
            }
            else if (number >= 1000 && number <= 9999)
            {
                fileNameStr.Append("0").Append(number).Append(".dat");
            }
            else if (number >= 10000 && number <= 99999)
            {
                fileNameStr.Append("").Append(number).Append(".dat");
            }
            else
            {
                Console.WriteLine("区块文件序号不在合法范围内");
                return null;
            }
            return fileNameStr.ToString();
        }

        //b.加载下一个区块文件到区块池
        public void load_NextFile_ToBlockPooling()
        {
            if (Directory.Exists(blockchainFilePath))
            {                
                currentLoadFileNumber++;
                string blockfileName = get_filename(currentLoadFileNumber);
                string blockfile = Path.Combine(blockchainFilePath, blockfileName);
                Console.WriteLine("正在加载:"+ blockfile);
                if (File.Exists(blockfile))
                {
                    IBlockchainParser blockFileParser = new BlockchainParser(blockchainFilePath, blockfileName, 1);
                    List<ParserBlock> blocksOfOneBlockFile = blockFileParser.ParseBlockchain().ToList();
                    blockReadBlocksFromFile.AddRange(blocksOfOneBlockFile);
                    blockCountOfFile.Add(blocksOfOneBlockFile.Count);
                }
                else
                {
                    missingBlockFile = true;
                    Console.WriteLine("区块文件:" + blockfile + " 不存在!!!");
                }
            }
            else {
                Console.WriteLine("区块链文件路径不存在!!!");
            }
        }

        //c.从区块池中查找下一个区块
        public bool search_NextBlock_FromBlockPooling(ParserBlock priorBlock, out ParserBlock nextBlock)
        {
            if (priorBlock != null)
            {
                for (int i = blockReadPointer; i < blockReadBlocksFromFile.Count; i++)
                {
                    if (blockReadBlocksFromFile[i] != null)
                    {
                        if (priorBlock.Header.GetHash() == blockReadBlocksFromFile[i].Header.HashPrevBlock)
                        {
                            if (!forkedBlockList.Contains(blockReadBlocksFromFile[i].Header.GetHash().ToString()))//排除分叉块
                            {
                                if (!orphanBlockList.Contains(blockReadBlocksFromFile[i].Header.GetHash().ToString()))//排除孤块
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
                            if (priorBlock.Header.GetHash() == blockReadBlocksFromFile[j].Header.HashPrevBlock)
                            {
                                if (!forkedBlockList.Contains(blockReadBlocksFromFile[j].Header.GetHash().ToString()))
                                {
                                    if (!orphanBlockList.Contains(blockReadBlocksFromFile[j].Header.GetHash().ToString()))
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
                for (int m = 0; m < newlyLoadFileAmountCeiling; m++)
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
                                if (priorBlock.Header.GetHash() == blockReadBlocksFromFile[k].Header.HashPrevBlock)
                                {
                                    if (!forkedBlockList.Contains(blockReadBlocksFromFile[k].Header.GetHash().ToString()))
                                    {
                                        if (!orphanBlockList.Contains(blockReadBlocksFromFile[k].Header.GetHash().ToString()))
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
            else {
                Console.WriteLine("查找下一个区块时,前一个区块不能为空!!!");
                nextBlock = null;
                return false;
            }                       
        }

        //d.向区块队列池中补充区块
        public bool enqueue_ToBlockQueuePooling()
        {
            int invalidBlockCount = 0;
            Queue<ParserBlock> tempBlockQueuePooling = new Queue<ParserBlock>();
            ParserBlock priorBlock = blockQueueTailElement;
            ParserBlock tailBlock;
            while (blockQueuePooling.Count < blockQueuePoolingSize)
            {
                if (search_NextBlock_FromBlockPooling(priorBlock, out tailBlock))
                {
                    blockQueuePooling.Enqueue(tailBlock);
                    priorBlock = tailBlock;
                }
                else
                {
                    if (missingBlockFile)
                    {
                        Console.WriteLine("区块文件全部加载，找不到下一个区块文件，将处理结束!!!");
                        return false;
                    }
                    else
                    {
                        invalidBlockCount++;
                        tempBlockQueuePooling.Clear();
                        for (int i = 0; i < blockQueuePooling.Count - 1; i++)
                        {
                            tempBlockQueuePooling.Enqueue(blockQueuePooling.Dequeue());
                        }
                        forkedBlockList.Add(blockQueuePooling.Dequeue().Header.GetHash().ToString());//向分叉块列表中添加分叉上的块
                        Console.WriteLine("出现分叉上的块,正在收集分叉上的块!!!");
                        blockQueuePooling.Clear();
                        for (int j = 0; j < tempBlockQueuePooling.Count; j++)
                        {
                            ParserBlock dequeueBlock = tempBlockQueuePooling.Dequeue();
                            blockQueuePooling.Enqueue(dequeueBlock);
                            if (j == tempBlockQueuePooling.Count - 1)
                            {
                                priorBlock = dequeueBlock;
                            }
                        }

                    }                    
                }
                if (blockQueuePooling.Count == blockQueuePoolingSize)
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

        //e.从区块队列池中取出一个区块
        public ParserBlock dequeue_FromBlockQueuePooling()
        {
            ParserBlock dequeueBlock = null;
            if (blockQueuePooling.Count != 0)
            {
                dequeueBlock = blockQueuePooling.Dequeue();
                lastProcessedBlockElement = dequeueBlock;
                for (int i = 0; i < blockReadPointer; i++)
                {
                    if (blockReadBlocksFromFile[i] != null)
                    {
                        if (blockReadBlocksFromFile[i].Header.GetHash() == dequeueBlock.Header.GetHash())
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
                        if (blockReadBlocksFromFile[j].Header.GetHash() == dequeueBlock.Header.GetHash())
                        {
                            blockReadBlocksFromFile[j] = null;
                            return dequeueBlock;
                        }
                    }
                }
            }
            return dequeueBlock;
        }

        //f.初始化区块队列池
        public void initialize_BlockQueuePooling(ParserBlock firstBlock)
        {
            ParserBlock priorBlock = firstBlock;
            ParserBlock nextBlock;
            blockQueuePooling.Enqueue(priorBlock);
            for (int i = 0; i < blockQueuePoolingSize - 1; i++)
            {
                if (search_NextBlock_FromBlockPooling(priorBlock, out nextBlock))
                {
                    blockQueuePooling.Enqueue(nextBlock);
                    if (i == blockQueuePoolingSize - 2)
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

        //g.获取创世区块
        public ParserBlock get_GenesisBlock(bool displayMark)
        {
            if (Directory.Exists(blockchainFilePath))
            {                
                Console.WriteLine("正在获取创世区块.........");
                string blockfileName = get_filename(0);
                string blockchainFile = Path.Combine(blockchainFilePath, blockfileName);
                if (File.Exists(blockchainFile))
                {
                    IBlockchainParser blockFileParser = new BlockchainParser(blockchainFilePath, blockfileName, 1);
                    List<ParserBlock> blocksOfFirstBlockFile = blockFileParser.ParseBlockchain().ToList();
                    ParserBlock genesisBlock = blocksOfFirstBlockFile[0];
                    if (displayMark) {
                        Console.WriteLine("创世区块前一个区块hash:" + genesisBlock.Header.HashPrevBlock);
                        Console.WriteLine("创世区块time:" + genesisBlock.Header.BlockTime);
                        Console.WriteLine("创世区块hash:" + genesisBlock.Header.GetHash());
                        Console.WriteLine("**********************************************************");
                    }                    
                    return genesisBlock;
                }
                else
                {
                    Console.WriteLine("区块文件:" + blockchainFile + " 不存在!!!");
                    return null;
                }
            }
            else
            {
                Console.WriteLine("区块链文件路径不存在!!!");
                return null;
            }            
        }

        //h.计算已读区块总数
        public int countOfFileBlock(List<int> blockCountOfFile)
        {
            int totalAmount = 0;
            foreach (int blockAmount in blockCountOfFile)
            {
                totalAmount += blockAmount;
            }
            return totalAmount;
        }

        //i.统计记录孤块
        public void record_OrphanBlock()
        {
            for (int i = 0; i < blockReadBlocksFromFile.Count; i++)
            {
                if (blockReadBlocksFromFile[i] != null)
                {
                    if (!forkedBlockList.Contains(blockReadBlocksFromFile[i].Header.GetHash().ToString()))
                    {
                        if (blockReadBlocksFromFile[i].Header.BlockTime < lastProcessedBlockElement.Header.BlockTime)
                        {
                            orphanBlockList.Add(blockReadBlocksFromFile[i].Header.GetHash().ToString());
                            blockReadBlocksFromFile[i] = null;//从区块池中删除孤块
                        }
                    }
                }
            }
        }

        //j.恢复区块池上下文状态
        public void restore_blockReadBlocksFromFile(List<FileStatusItem_Class> fileStatusList)
        {
            foreach (FileStatusItem_Class fileStatusItemObject in fileStatusList)
            {
                blockCountOfFile.Add(fileStatusItemObject.totalBlocks);
                if (fileStatusItemObject.unusedBlocks == 0)
                {
                    for (int i = 0; i < fileStatusItemObject.totalBlocks; i++)
                    {
                        blockReadBlocksFromFile.Add(null);
                    }
                }
                else
                {
                    IBlockchainParser blockFileParser = new BlockchainParser(blockchainFilePath, fileStatusItemObject.fileName, 1);
                    List<ParserBlock> blocksOfFile = blockFileParser.ParseBlockchain().ToList();
                    foreach (ParserBlock block in blocksOfFile)
                    {
                        if (fileStatusItemObject.unusedBlockHash.Contains(block.Header.GetHash().ToString()))
                        {
                            blockReadBlocksFromFile.Add(block);
                        }
                        else
                        {
                            blockReadBlocksFromFile.Add(null);
                        }
                    }
                }
            }
        }

        //k.恢复队首区块
        public ParserBlock restore_QueueHeaderBlock(string lastProcessedBlockHash)
        {
            foreach (ParserBlock block in blockReadBlocksFromFile)
            {
                if (block != null)
                {
                    if (block.Header.HashPrevBlock.ToString() == lastProcessedBlockHash)
                    {
                        return block;
                    }
                }
            }
            return null;
        }

        //l.从给定的BPC文件恢复程序状态
        public void restore_BlockProcessContextForProgram()
        {            
            string blockProcessContextFileFinalPath = Path.Combine(blockProcessContextFilePath, blockProcessContextFileName);
            //判断给定文件名是压缩文件还是txt文件
            FileInfo fileName = new FileInfo(blockProcessContextFileFinalPath);
            if (fileName.Extension==".rar") {
                Console.WriteLine("正在解压BPC下文状态文件......");
                Decompress(blockProcessContextFileFinalPath,false);
                blockProcessContextFileFinalPath = Path.Combine(blockProcessContextFilePath, Path.GetFileNameWithoutExtension(blockProcessContextFileFinalPath));                
            }            
            if (File.Exists(blockProcessContextFileFinalPath))
            {
                //1.反序列化BPC文件
                Console.WriteLine("开始提取程序上下文状态文件数据(BPC).........");
                BlockProcessContextModel_Class blockProcessContextModelObject = null;
                Stopwatch timer = new Stopwatch();
                timer.Start();
                try
                {
                    using (StreamReader sr = File.OpenText(blockProcessContextFileFinalPath))
                    {
                        JsonSerializer jsonSerializer = new JsonSerializer();
                        blockProcessContextModelObject = jsonSerializer.Deserialize(sr, typeof(BlockProcessContextModel_Class)) as BlockProcessContextModel_Class;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("BPC文件保存不完整或已经损坏。该错误可能是由于在保存BPC文件时提前终止程序造成的，或是人为修改了最近的BPC文件。");
                }
                timer.Stop();
                Console.WriteLine("提取结束,反序列化切片用时:" + timer.Elapsed);
                //2.恢复区块池
                Console.WriteLine("开始恢复区块池.........");
                Stopwatch timer1 = new Stopwatch();
                timer1.Start();
                restore_blockReadBlocksFromFile(blockProcessContextModelObject.fileStatusList);
                timer1.Stop();
                Console.WriteLine("恢复区块池用时:" + timer1.Elapsed);
                //3.恢复参数
                forkedBlockList = blockProcessContextModelObject.forkedBlockList;
                orphanBlockList = blockProcessContextModelObject.orphanBlockList;
                blockReadPointer = blockProcessContextModelObject.blockReadPointer;
                currentLoadFileNumber = blockProcessContextModelObject.currentLoadFileNumber;
                processedBlockAmount = blockProcessContextModelObject.processedBlockAmount;
                recentlySliceDateTime = blockProcessContextModelObject.blockProcessContextDatetime;
                blockProcessContextModelObject.Dispose();
                //4.恢复队列池
                Console.WriteLine("开始恢复区块队列.........");
                Stopwatch timer2 = new Stopwatch();
                timer2.Start();
                ParserBlock queueBlock;
                if ((queueBlock = restore_QueueHeaderBlock(blockProcessContextModelObject.lastProcessedBlockHash)) != null)
                {
                    initialize_BlockQueuePooling(queueBlock);
                }
                else
                {
                    Console.WriteLine("队首区块未找到,恢复失败!!!");
                }
                timer2.Stop();
                Console.WriteLine("恢复区块队列用时:" + timer2.Elapsed);
                Console.WriteLine("BPC上下文状态恢复成功.........");
                File.Delete(blockProcessContextFileFinalPath);//删除解压后的文件BPC文件
            }
            else {
                Console.WriteLine(blockProcessContextFileFinalPath + " 文件不存在!!!");
            }
        }

        //m.压缩流测试
        public void Compress(string fileName,bool deleteMark) {
            FileInfo fileToCompress = new FileInfo(fileName);
            using (FileStream originalFileStream = fileToCompress.OpenRead())
            {
                if ((File.GetAttributes(fileToCompress.FullName) & FileAttributes.Hidden) != FileAttributes.Hidden & fileToCompress.Extension != ".rar")
                {
                    using (FileStream compressedFileStream = File.Create(fileToCompress.FullName + ".rar"))
                    {
                        using (GZipStream compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
                        {
                            originalFileStream.CopyTo(compressionStream);
                        }
                    }
                }
            }
            if (deleteMark) {
                File.Delete(fileName);
            }            
        }

        //n.解压缩流测试
        public void Decompress(string fileName, bool deleteMark)
        {
            FileInfo fileToDecompress = new FileInfo(fileName);
            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                string currentFileName = fileToDecompress.FullName;
                string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);

                using (FileStream decompressedFileStream = File.Create(newFileName))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                    }
                }
            }
            if (deleteMark)
            {
                File.Delete(fileName);//删除压缩文件
            }
        }

    }
}
