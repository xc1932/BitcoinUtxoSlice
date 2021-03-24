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
    class Block_Processing_Class
    {
        public Dictionary<string, UTXOItem_Class> utxoDictionary = new Dictionary<string, UTXOItem_Class>();
        public List<FileStatusItem_Class> fileStatusList = new List<FileStatusItem_Class>();
        Blockfile_Manager_Class blockfileManagerObject = new Blockfile_Manager_Class();
        string utxoSliceFileLocationPath = Configuration_Class.sliceStateFilePath;
        public int processedBlockAmount = 0;
        public int sliceFileAmount = 0;

        ////I.初次启动
        public void initial_Run() {
            Console.WriteLine("初次启动.........");
            //获取创世块
            Block genesisBlock = get_GenesisBlock();
            //计时
            Stopwatch timer = new Stopwatch();
            timer.Start();
            Block_Pooling_Manager_Class bpmc = new Block_Pooling_Manager_Class();
            bpmc.initialize_BlockQueuePooling(genesisBlock);
            int processedBlockAmount = 0;
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
                    save_SliceFile(utxoSliceFileLocationPath + "\\" + processedBlockAmount + "_" + readyBlock.BlockHeader.BlockTimestamp.ToString("yyyy年MM月dd日HH时mm分ss秒") + ".txt", bpmc, processedBlockAmount, sliceFileAmount);
                    Console.WriteLine("正在保存第" + (int)(processedBlockAmount / Configuration_Class.UTXOSliceLength) + "个切片状态..........");
                }
                if (processedBlockAmount % 100 == 0)
                {
                    Console.WriteLine("已处理" + processedBlockAmount + "个区块");
                }
            }
            timer.Stop();
            Console.WriteLine("执行结束:" + timer.Elapsed);
        }
        
        ////II.处理交易
        //1.判断是铸币交易还是常规交易
        public bool isCoinbaseTransaction(Transaction transaction)
        {
            if (transaction.Inputs.Count == 1 && transaction.Inputs[0].SourceTransactionOutputIndex == uint.MaxValue && transaction.Inputs[0].SourceTransactionHash.IsZeroArray())
            {
                return true;
            }
            return false;
        }

        //2.验证交易是否合法
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

        //3.执行铸币交易
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
                    indexOfOutput++;
                }
                else
                {
                    utxoDictionary[txhashAndIndex].value += value;
                    indexOfOutput++;
                    //sameTransactions++;
                }
            }
        }

        //4.执行常规交易
        public void execute_RegularTransaction(Transaction transaction)
        {
            foreach (TransactionInput transactionInput in transaction.Inputs)
            {
                string sourceTxhashAndIndex = transactionInput.SourceTransactionHash + "#" + transactionInput.SourceTransactionOutputIndex;
                utxoDictionary.Remove(sourceTxhashAndIndex);
            }
            uint indexOfOutput = 0;
            foreach (TransactionOutput transactionOutput in transaction.Outputs)
            {
                string txhashAndIndex = transaction.TransactionHash + "#" + indexOfOutput;
                string txhash = transaction.TransactionHash.ToString();
                ulong value = transactionOutput.OutputValueSatoshi;
                string script = transactionOutput.OutputScript.ToString();
                UTXOItem_Class unSpentTxOutItem = new UTXOItem_Class(txhash, indexOfOutput, value, script);
                utxoDictionary.Add(txhashAndIndex, unSpentTxOutItem);
                indexOfOutput++;
            }
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

        ////III.
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

        //2.计算已读区块总数
        public int countOfFileBlock(List<int> blockCountOfFile)
        {
            int totalAmount = 0;
            foreach (int blockAmount in blockCountOfFile)
            {
                totalAmount += blockAmount;
            }
            return totalAmount;
        }

        //3.保存切片状态(正在修改.....)
        public void save_SliceFile(string sliceFilePath, Block_Pooling_Manager_Class blockPoolingManagerObject, int processedBlockAmount, int sliceFileAmount)
        {
            if (countOfFileBlock(blockPoolingManagerObject.blockCountOfFile) != blockPoolingManagerObject.blockReadBlocksFromFile.Count)
            {
                Console.WriteLine("读取文件总数和区块池文件总数不一致");
            }
            else
            {
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
                    blockPoolingManagerObject.blockReadPointer, blockPoolingManagerObject.currentLoadFileNumber, processedBlockAmount, sliceFileAmount);
                using (StreamWriter sw = File.CreateText(sliceFilePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(sw, sliceFileItem);
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
