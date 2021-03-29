using BitcoinBlockchain.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BitcoinUtxoSlice
{
    public class Block_Processing_Class
    {
        public Dictionary<string, UTXOItem_Class> utxoDictionary = new Dictionary<string, UTXOItem_Class>();
        public List<FileStatusItem_Class> fileStatusList = new List<FileStatusItem_Class>();
        Blockfile_Manager_Class blockfileManagerObject = new Blockfile_Manager_Class();
        string utxoSliceFileLocationPath = Configuration_Class.sliceStateFilePath;
        public int processedBlockAmount = 0;
        public int sliceFileAmount = 0;
        public int sameTransactionCount = 0;
        ////I.初次启动
        public void initial_Run()
        {
            if (!Directory.Exists(utxoSliceFileLocationPath))
            {
                Directory.CreateDirectory(utxoSliceFileLocationPath);
            }
            Console.WriteLine("初次启动.........");
            //获取创世块
            Block genesisBlock = get_GenesisBlock();
            //计时
            Stopwatch timer = new Stopwatch();
            timer.Start();
            Block_Pooling_Manager_Class bpmc = new Block_Pooling_Manager_Class();
            bpmc.initialize_BlockQueuePooling(genesisBlock);
            processedBlockAmount = 0;
            sliceFileAmount = 0;
            Block readyBlock;
            while ((readyBlock = bpmc.dequeue_FromBlockQueuePooling()) != null)
            {
                execute_TransactionsOfOneBlock(readyBlock);
                processedBlockAmount++;
                bool successMark = bpmc.enqueue_ToBlockQueuePooling();
                if (!successMark)
                {
                    //执行结束
                    break;
                }
                if (processedBlockAmount % Configuration_Class.UTXOSliceLength == 0)
                {
                    sliceFileAmount++;
                    Console.WriteLine("正在保存第" + (int)(processedBlockAmount / Configuration_Class.UTXOSliceLength) + "个切片状态,请勿现在终止程序..........");
                    save_SliceFile(utxoSliceFileLocationPath + "\\" + processedBlockAmount + "_" + readyBlock.BlockHeader.BlockTimestamp.ToString("yyyy年MM月dd日HH时mm分ss秒") + ".txt", bpmc, processedBlockAmount, sliceFileAmount);
                    Console.WriteLine("切片保存完成");
                }
                if (processedBlockAmount % 100 == 0)
                {
                    Console.WriteLine("已处理" + processedBlockAmount + "个区块");
                    Console.WriteLine("相同交易出现次数:" + sameTransactionCount);
                }
            }
            timer.Stop();
            Console.WriteLine("执行结束:" + timer.Elapsed);
        }

        ////II.增量处理
        public void restart()
        {
            if (!Directory.Exists(utxoSliceFileLocationPath))
            {
                Console.WriteLine("切片文件保存路径不存在");
            }
            Console.WriteLine("程序正在重启.......");
            Block_Pooling_Manager_Class bpmc = new Block_Pooling_Manager_Class();
            Stopwatch timer = new Stopwatch();
            timer.Start();
            if (restore_StatusForProgram(bpmc) == true)
            {
                timer.Stop();
                Console.WriteLine("程序重启成功.......");
                Console.WriteLine("程序重启用时:" + timer.Elapsed);
                Block readyBlock;
                Stopwatch timer1 = new Stopwatch();
                timer1.Start();
                while ((readyBlock = bpmc.dequeue_FromBlockQueuePooling()) != null)
                {
                    execute_TransactionsOfOneBlock(readyBlock);
                    processedBlockAmount++;
                    bool successMark = bpmc.enqueue_ToBlockQueuePooling();
                    if (!successMark)
                    {
                        //执行结束
                        Console.WriteLine("执行结束");
                        break;
                    }
                    if (processedBlockAmount % Configuration_Class.UTXOSliceLength == 0)
                    {
                        sliceFileAmount++;
                        Console.WriteLine("正在保存第" + (int)(processedBlockAmount / Configuration_Class.UTXOSliceLength) + "个切片状态,请勿现在终止程序..........");
                        save_SliceFile(utxoSliceFileLocationPath + "\\" + processedBlockAmount + "_" + readyBlock.BlockHeader.BlockTimestamp.ToString("yyyy年MM月dd日HH时mm分ss秒") + ".txt", bpmc, processedBlockAmount, sliceFileAmount);
                        Console.WriteLine("切片保存完成");
                    }
                    if (processedBlockAmount % 100 == 0)
                    {
                        Console.WriteLine("已处理" + processedBlockAmount + "个区块");
                        Console.WriteLine("相同交易出现次数:" + sameTransactionCount);
                    }
                }
                timer1.Stop();
                Console.WriteLine("执行结束:" + timer1.Elapsed);
            }
            else
            {
                Console.WriteLine("程序重启失败.......");
            }
        }

        ////III.处理交易
        //1.执行铸币交易
        public void execute_CoinbaseTransaction(Transaction transaction)
        {
            uint indexOfOutput = 0;
            foreach (TransactionOutput transactionOutput in transaction.Outputs)
            {
                string txhashAndIndex = transaction.TransactionHash + "#" + indexOfOutput;
                string txhash = transaction.TransactionHash.ToString();
                ulong value = transactionOutput.OutputValueSatoshi;
                string script = transactionOutput.OutputScript.ToString();
                UTXOItem_Class unSpentTxOutItem = new UTXOItem_Class(txhash, indexOfOutput, value, script);
                if (!utxoDictionary.ContainsKey(txhashAndIndex))
                {
                    utxoDictionary.Add(txhashAndIndex, unSpentTxOutItem);
                }
                else
                {
                    utxoDictionary[txhashAndIndex].utxoItemAmount++;
                    sameTransactionCount++;
                }
                indexOfOutput++;
            }
        }

        //2.执行常规交易
        public void execute_RegularTransaction(Transaction transaction)
        {
            foreach (TransactionInput transactionInput in transaction.Inputs)
            {
                string sourceTxhashAndIndex = transactionInput.SourceTransactionHash + "#" + transactionInput.SourceTransactionOutputIndex;
                if (utxoDictionary.ContainsKey(sourceTxhashAndIndex))
                {
                    if (utxoDictionary[sourceTxhashAndIndex].utxoItemAmount > 1)
                    {
                        utxoDictionary[sourceTxhashAndIndex].utxoItemAmount--;
                    }
                    else
                    {
                        utxoDictionary.Remove(sourceTxhashAndIndex);
                    }

                }
                else
                {
                    Console.WriteLine("当前交易中的输入不存在:" + sourceTxhashAndIndex);
                    return;
                }

            }
            uint indexOfOutput = 0;
            foreach (TransactionOutput transactionOutput in transaction.Outputs)
            {
                string txhashAndIndex = transaction.TransactionHash + "#" + indexOfOutput;
                string txhash = transaction.TransactionHash.ToString();
                ulong value = transactionOutput.OutputValueSatoshi;
                string script = transactionOutput.OutputScript.ToString();
                UTXOItem_Class unSpentTxOutItem = new UTXOItem_Class(txhash, indexOfOutput, value, script);
                if (!utxoDictionary.ContainsKey(txhashAndIndex))
                {
                    utxoDictionary.Add(txhashAndIndex, unSpentTxOutItem);
                }
                else
                {
                    utxoDictionary[txhashAndIndex].utxoItemAmount++;
                    sameTransactionCount++;
                }
                indexOfOutput++;
            }
        }

        //3.判断是铸币交易还是常规交易
        public bool isCoinbaseTransaction(Transaction transaction)
        {
            if (transaction.Inputs.Count == 1 && transaction.Inputs[0].SourceTransactionOutputIndex == uint.MaxValue && transaction.Inputs[0].SourceTransactionHash.IsZeroArray())
            {
                return true;
            }
            return false;
        }

        //4.验证交易是否合法
        public bool isValidTransaction(Transaction transaction)
        {
            if (!isCoinbaseTransaction(transaction))
            {
                ulong totalInputValue = 0;
                foreach (TransactionInput transactionInput in transaction.Inputs)
                {
                    string sourceTxhashAndIndex = transactionInput.SourceTransactionHash + "#" + transactionInput.SourceTransactionOutputIndex;
                    if (utxoDictionary.ContainsKey(sourceTxhashAndIndex))
                    {
                        totalInputValue += utxoDictionary[sourceTxhashAndIndex].value;
                    }
                    else
                    {
                        return false;
                    }
                }
                ulong totalOutputValue = 0;
                foreach (TransactionOutput transactionOutput in transaction.Outputs)
                {
                    totalOutputValue += transactionOutput.OutputValueSatoshi;
                }
                if (totalInputValue < totalOutputValue)
                {
                    return false;
                }
            }
            return true;
        }

        //5.执行一个区块的交易
        public void execute_TransactionsOfOneBlock(Block block)
        {
            foreach (Transaction transaction in block.Transactions)
            {
                if (isCoinbaseTransaction(transaction))
                {
                    execute_CoinbaseTransaction(transaction);
                }
                else
                {
                    if (isValidTransaction(transaction))
                    {
                        execute_RegularTransaction(transaction);
                    }
                }
            }
        }

        ////IV.恢复程序
        //1.计算已读区块总数
        public int countOfFileBlock(List<int> blockCountOfFile)
        {
            int totalAmount = 0;
            foreach (int blockAmount in blockCountOfFile)
            {
                totalAmount += blockAmount;
            }
            return totalAmount;
        }

        //2..判断是否有时间切片并返回最近的时间切片
        public bool exist_TimeingSlice(string sliceFileLocationPath, out string recentlySliceFilePath)
        {
            recentlySliceFilePath = null;
            if (!Directory.Exists(sliceFileLocationPath))
            {
                Console.WriteLine("保存切片的文件夹错误或不存在");
                return false;
            }
            DirectoryInfo dirInfo = new DirectoryInfo(sliceFileLocationPath);
            if (dirInfo.GetFiles().Length == 0)
            {
                Console.WriteLine("当前文件夹为空");
                return false;
            }
            List<int> blockHeightList = new List<int>();
            foreach (FileInfo fileInfo in dirInfo.GetFiles())
            {
                if (fileInfo.Name.EndsWith(".txt"))
                {
                    string blockHeight = fileInfo.Name.Split("_")[0];
                    if (Regex.IsMatch(blockHeight, @"^\d+$"))
                    {
                        int height = Convert.ToInt32(blockHeight);
                        blockHeightList.Add(height);
                    }
                    else
                    {
                        Console.WriteLine("当前文件夹下没有合法的txt文件");
                    }
                }
                else
                {
                    Console.WriteLine("当前文件夹下没有txt文件");
                }
            }
            if (blockHeightList.Count == 0)
            {
                Console.WriteLine("没有合法的切片文件");
                return false;
            }
            int maxHeight = 0;
            foreach (int number in blockHeightList)
            {
                if (number > maxHeight)
                {
                    maxHeight = number;
                }
            }
            string recentlySliceFileName = "";
            foreach (FileInfo fileInfo in dirInfo.GetFiles())
            {
                if (fileInfo.Name.StartsWith(maxHeight.ToString()))
                {
                    recentlySliceFileName = fileInfo.Name;
                }
            }
            if (recentlySliceFileName == "")
            {
                Console.WriteLine("没找到最近的时间切片");
            }
            recentlySliceFilePath = Path.Combine(sliceFileLocationPath, recentlySliceFileName);
            return true;
        }

        //3.从最近的时间切片恢复程序状态(正在修改.....)
        public bool restore_StatusForProgram(Block_Pooling_Manager_Class blockPoolingManagerObject)
        {
            string recentlySliceFilePath = "";
            if (exist_TimeingSlice(Configuration_Class.sliceStateFilePath, out recentlySliceFilePath))
            {
                SliceFileItem_Class sliceFileItemObject = null;
                //反序列化切片文件
                Console.WriteLine("开始提取切片中的数据.........");
                Stopwatch timer = new Stopwatch();
                timer.Start();
                try
                {
                    using (StreamReader sr = File.OpenText(recentlySliceFilePath))
                    {
                        JsonSerializer jsonSerializer = new JsonSerializer();
                        sliceFileItemObject = jsonSerializer.Deserialize(sr, typeof(SliceFileItem_Class)) as SliceFileItem_Class;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("最近的切片状态可能保存不完整或已经损坏。该错误可能是由于在保存切片状态时提前终止程序造成的，或是人为修改了最近的切片状态。");
                }
                timer.Stop();
                Console.WriteLine("提取结束.........");
                Console.WriteLine("反序列化切片用时:" + timer.Elapsed);
                //恢复区块池
                Console.WriteLine("开始恢复区块池.........");
                Stopwatch timer1 = new Stopwatch();
                timer1.Start();
                restore_blockReadBlocksFromFile(sliceFileItemObject, blockPoolingManagerObject);
                timer1.Stop();
                Console.WriteLine("恢复区块池用时:" + timer1.Elapsed);
                //恢复UTXO字典
                utxoDictionary = sliceFileItemObject.utxoDictionary;
                //恢复队列池
                Console.WriteLine("开始恢复区块队列.........");
                Stopwatch timer2 = new Stopwatch();
                timer2.Start();
                Block queueBlock;
                if ((queueBlock = restore_QueueHeaderBlock(sliceFileItemObject.lastProcessedBlockHash, blockPoolingManagerObject)) != null)
                {
                    blockPoolingManagerObject.initialize_BlockQueuePooling(queueBlock);
                }
                else
                {
                    Console.WriteLine("队首区块未找到");
                    return false;
                }
                timer2.Stop();
                Console.WriteLine("恢复区块队列用时:" + timer2.Elapsed);
                //恢复其它
                blockPoolingManagerObject.forkedBlockList = sliceFileItemObject.forkedBlockList;
                blockPoolingManagerObject.orphanBlockList = sliceFileItemObject.orphanBlockList;
                blockPoolingManagerObject.blockReadPointer = sliceFileItemObject.blockReadPointer;
                blockPoolingManagerObject.currentLoadFileNumber = sliceFileItemObject.currentLoadFileNumber;
                processedBlockAmount = sliceFileItemObject.processedBlockAmount;
                sliceFileAmount = sliceFileItemObject.sliceFileAmount;
                sameTransactionCount = sliceFileItemObject.sameTransactionCount;
                sliceFileItemObject.Dispose();
            }
            else
            {
                Console.WriteLine("时间切片不存在");
                return false;
            }
            return true;
        }

        //4.恢复区块池(blockReadBlocksFromFile)
        public void restore_blockReadBlocksFromFile(SliceFileItem_Class sliceFileItemObject, Block_Pooling_Manager_Class blockPoolingManagerObject)
        {
            foreach (FileStatusItem_Class fileStatusItemObject in sliceFileItemObject.fileStatusList)
            {
                blockPoolingManagerObject.blockCountOfFile.Add(fileStatusItemObject.totalBlocks);
                if (fileStatusItemObject.unusedBlocks == 0)
                {
                    for (int i = 0; i < fileStatusItemObject.totalBlocks; i++)
                    {
                        blockPoolingManagerObject.blockReadBlocksFromFile.Add(null);
                    }
                }
                else
                {
                    List<Block> blocksOfFile = blockfileManagerObject.load_one_blockfile(Configuration_Class.preprocessedBlockFilePath + "\\" + fileStatusItemObject.fileName);
                    foreach (Block block in blocksOfFile)
                    {
                        if (fileStatusItemObject.unusedBlockHash.Contains(block.BlockHeader.BlockHash.ToString()))
                        {
                            blockPoolingManagerObject.blockReadBlocksFromFile.Add(block);
                        }
                        else
                        {
                            blockPoolingManagerObject.blockReadBlocksFromFile.Add(null);
                        }
                    }
                }
            }
        }

        //5.恢复队首区块
        public Block restore_QueueHeaderBlock(string lastProcessedBlockHash, Block_Pooling_Manager_Class blockPoolingManagerObject)
        {
            foreach (Block block in blockPoolingManagerObject.blockReadBlocksFromFile)
            {
                if (block != null)
                {
                    if (block.BlockHeader.PreviousBlockHash.ToString() == lastProcessedBlockHash)
                    {
                        return block;
                    }
                }
            }
            return null;
        }

        ////V.其它
        //1.获取创世区块
        public Block get_GenesisBlock()
        {
            //获取创世块
            Blockfile_Manager_Class bmc = new Blockfile_Manager_Class();
            //E:\Code\BlockFile\blk0
            string loadFilePath = Configuration_Class.preprocessedBlockFilePath + "\\blk0";
            List<Block> blockList = bmc.load_one_blockfile(loadFilePath);
            Block genesisBlock = blockList[0];
            Console.WriteLine("创世区块前一个区块hash:" + genesisBlock.BlockHeader.PreviousBlockHash);
            Console.WriteLine("创世区块time:" + genesisBlock.BlockHeader.BlockTimestamp);
            Console.WriteLine("创世区块hash:" + genesisBlock.BlockHeader.BlockHash);
            Console.WriteLine("**********************************************************");
            return genesisBlock;
        }

        //2.保存切片状态(正在修改.....)
        public void save_SliceFile(string sliceFilePath, Block_Pooling_Manager_Class blockPoolingManagerObject, int processedBlockAmount, int sliceFileAmount)
        {
            if (countOfFileBlock(blockPoolingManagerObject.blockCountOfFile) != blockPoolingManagerObject.blockReadBlocksFromFile.Count)
            {
                Console.WriteLine("读取文件总数和区块池文件总数不一致");
            }
            else
            {
                record_OrphanBlock(blockPoolingManagerObject);
                fileStatusList.Clear();
                int readBlockPointer = 0;
                for (int i = 0; i < blockPoolingManagerObject.blockCountOfFile.Count; i++)
                {
                    string fileName = "blk" + i;
                    int totalBlocks = blockPoolingManagerObject.blockCountOfFile[i];
                    int unusedBlocks = 0;
                    List<string> unusedBlockHash = new List<string>();
                    for (int j = 0; j < blockPoolingManagerObject.blockCountOfFile[i]; j++)
                    {
                        if (blockPoolingManagerObject.blockReadBlocksFromFile[readBlockPointer] != null)
                        {
                            unusedBlockHash.Add(blockPoolingManagerObject.blockReadBlocksFromFile[readBlockPointer].BlockHeader.BlockHash.ToString());
                            unusedBlocks++;
                        }
                        readBlockPointer++;
                    }
                    fileStatusList.Add(new FileStatusItem_Class(fileName, totalBlocks, unusedBlocks, unusedBlockHash));
                }
                SliceFileItem_Class sliceFileItem = new SliceFileItem_Class(fileStatusList, utxoDictionary, blockPoolingManagerObject.forkedBlockList,
                    blockPoolingManagerObject.orphanBlockList, blockPoolingManagerObject.lastProcessedBlockElement.BlockHeader.BlockHash.ToString(),
                    blockPoolingManagerObject.blockReadPointer, blockPoolingManagerObject.currentLoadFileNumber, processedBlockAmount, sliceFileAmount, sameTransactionCount);
                using (StreamWriter sw = File.CreateText(sliceFilePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(sw, sliceFileItem);
                }
            }
        }

        //3.统计记录孤块
        public void record_OrphanBlock(Block_Pooling_Manager_Class blockPoolingManagerObject)
        {
            for (int i = 0; i < blockPoolingManagerObject.blockReadBlocksFromFile.Count; i++)
            {
                if (blockPoolingManagerObject.blockReadBlocksFromFile[i] != null)
                {
                    if (!blockPoolingManagerObject.forkedBlockList.Contains(blockPoolingManagerObject.blockReadBlocksFromFile[i].BlockHeader.BlockHash.ToString()))
                    {
                        if (blockPoolingManagerObject.blockReadBlocksFromFile[i].BlockHeader.BlockTimestamp < blockPoolingManagerObject.lastProcessedBlockElement.BlockHeader.BlockTimestamp)
                        {
                            blockPoolingManagerObject.orphanBlockList.Add(blockPoolingManagerObject.blockReadBlocksFromFile[i].BlockHeader.BlockHash.ToString());
                        }
                    }
                }
            }
        }

        //4.处理测试
        public void virtual_BlockProcessing(Block readyBlock, int processedBlockAmount)
        {
            Console.WriteLine("正在处理第" + processedBlockAmount + "个区块..............");
            Console.WriteLine("前一个区块hash:" + readyBlock.BlockHeader.PreviousBlockHash);
            Console.WriteLine("区块time:" + readyBlock.BlockHeader.BlockTimestamp);
            Console.WriteLine("区块hash:" + readyBlock.BlockHeader.BlockHash);
            Console.WriteLine("----------------------------------------------------------------------");
        }


    }
}
