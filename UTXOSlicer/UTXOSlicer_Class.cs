using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using BitcoinBlockchain.Data;
using NBitcoin;
using Newtonsoft.Json;
using OrderedBlockchainParser;

namespace UTXOSlicer
{
    public class UTXOSlicer_Class
    {        
        //和OrderedBlockchainParser_Class相关成员
        OrderedBlockchainParser_Class orderedBlockchainParser;
        public string blockchainFilePath = @".\";               //区块链文件路径        
        public string blockProcessContextFilePath = @".\";      //区块解析中断时的上下文(程序状态)文件路径
        public string blockProcessContextFileName = null;       //区块解析中断时的上下文(程序状态)文件
        public int blockQueuePoolingSize = 10;                  //区块池长度
        //新加入成员
        string UtxoSliceFilePath = @".\";                       //utxo切片文件存储路径
        string OpReturnFilePath = null;                         //opreturn文件存储路径
        string UtxoSliceFileName = null;                          //utxo切片恢复文件名
        string sliceIntervalTimeType;                           //切片间隔类型(year month day)
        int sliceIntervalTime;                                  //切片间隔长度       
        DateTime endTime = DateTime.MaxValue;                   //时间中止条件
        int endBlockHeight = int.MaxValue;                      //区块高度终止条件
        //原有成员
        public Dictionary<string, UTXOItem_Class> utxoDictionary = new Dictionary<string, UTXOItem_Class>();
        public LinkedList<opreturnOutputItem_Class> opreturnOutputLinkedList = new LinkedList<opreturnOutputItem_Class>();
        public int sliceFileAmount = 0;
        public int sameTransactionCount = 0;
        //最近的切片时间
        DateTime recentlySliceDateTime;

        public UTXOSlicer_Class() { }
        public UTXOSlicer_Class(string blockchainFilePath, string blockProcessContextFilePath, string blockProcessContextFileName,
                                string UtxoSliceFilePath, string UtxoSliceFileName, string OpReturnFilePath,
                                string sliceIntervalTimeType, int sliceIntervalTime, DateTime endTime, int endBlockHeight)
        {            
            this.blockchainFilePath = blockchainFilePath;                   //区块链文件路径
            this.blockProcessContextFilePath = blockProcessContextFilePath; //区块解析上下文文件存储路径
            this.blockProcessContextFileName = blockProcessContextFileName; //区块解析上下文恢复文件名
            this.UtxoSliceFilePath = UtxoSliceFilePath;                     //utxo切片文件存储路径
            this.UtxoSliceFileName = UtxoSliceFileName;                     //utxo切片恢复文件名
            this.OpReturnFilePath = OpReturnFilePath;                       //opreturn文件存储路径
            this.sliceIntervalTimeType = sliceIntervalTimeType;             //切片间隔类型
            this.sliceIntervalTime = sliceIntervalTime;                     //切片间隔长度            
            this.endTime = endTime;                                         //时间中止条件
            this.endBlockHeight = endBlockHeight;                           //区块高度终止条件
            parameter_Detection();                                          //参数检查

            if (UtxoSliceFileName == null || blockProcessContextFileName == null)
            { //从第0个块开始
                orderedBlockchainParser = new OrderedBlockchainParser_Class(blockchainFilePath, blockProcessContextFilePath, null);
                recentlySliceDateTime = orderedBlockchainParser.recentlySliceDateTime;
            }
            else
            { //从中断处恢复
                orderedBlockchainParser = new OrderedBlockchainParser_Class(blockchainFilePath, blockProcessContextFilePath, blockProcessContextFileName);
                recentlySliceDateTime = orderedBlockchainParser.recentlySliceDateTime;
                restore_UTXOSlicerContextForProgram();
            }
        }

        //I.更新UTXO状态
        public void updateUTXO()
        {
            if (orderedBlockchainParser.processedBlockAmount == 0)
            {
                Console.WriteLine("初次启动.........");
            }
            else
            {
                Console.WriteLine("从中断处恢复后启动.........");
            }
            ParserBlock readyBlock;
            while ((readyBlock = orderedBlockchainParser.getNextBlock()) != null)
            {
                execute_TransactionsOfOneBlock(readyBlock);
                if (endConditionJudgment(recentlySliceDateTime, readyBlock.Header.BlockTime.DateTime))
                {
                    sliceFileAmount++;
                    Console.WriteLine("正在保存第" + sliceFileAmount + "个切片状态,请勿现在终止程序..........");
                    save_SliceFile(orderedBlockchainParser.processedBlockAmount, sliceFileAmount, readyBlock.Header.BlockTime.DateTime);
                    Console.WriteLine("UTXO切片保存完成");
                    Console.WriteLine("正在保存第" + sliceFileAmount + "个程序上下文状态,请勿现在终止程序..........");
                    orderedBlockchainParser.saveBlockProcessContext();
                    Console.WriteLine("程序上下文保存完成");
                    if (OpReturnFilePath != null)
                    {
                        Console.WriteLine("正在保存第" + sliceFileAmount + "个opreturn切片状态,请勿现在终止程序..........");
                        save_opreturnOutputsFile(orderedBlockchainParser.processedBlockAmount, readyBlock.Header.BlockTime.DateTime);
                        Console.WriteLine("opreturn切片保存完成");
                    }
                    recentlySliceDateTime = readyBlock.Header.BlockTime.DateTime;
                }
                if (orderedBlockchainParser.processedBlockAmount % 100 == 0)
                {
                    Console.WriteLine("已处理" + orderedBlockchainParser.processedBlockAmount + "个区块");
                    Console.WriteLine("当前区块时间:" + readyBlock.Header.BlockTime.DateTime);
                    Console.WriteLine("相同交易出现次数:" + sameTransactionCount);
                }
                if (readyBlock.Header.BlockTime.DateTime >= endTime)
                {
                    Console.WriteLine("当前区块时间:" + readyBlock.Header.BlockTime.DateTime);
                    Console.WriteLine("当前区块高度:" + (orderedBlockchainParser.processedBlockAmount - 1));
                    Console.WriteLine("触发时间终止条件，执行结束!!!");
                    break;
                }
                if (orderedBlockchainParser.processedBlockAmount - 1 >= endBlockHeight)
                {
                    Console.WriteLine("当前区块时间:" + readyBlock.Header.BlockTime.DateTime);
                    Console.WriteLine("当前区块高度:" + (orderedBlockchainParser.processedBlockAmount - 1));
                    Console.WriteLine("触发区块高度终止条件，执行结束!!!");
                    break;
                }
            }
        }

        //II.依赖函数
        //a.验证交易是否合法
        public bool isValidTransaction(Transaction transaction)
        {
            if (!transaction.IsCoinBase)
            {
                ulong totalInputValue = 0;
                foreach (TxIn transactionInput in transaction.Inputs)
                {
                    string sourceTxhashAndIndex = transactionInput.PrevOut.ToString();
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
                foreach (TxOut transactionOutput in transaction.Outputs)
                {
                    totalOutputValue += transactionOutput.Value;
                }
                if (totalInputValue < totalOutputValue)
                {
                    return false;
                }
            }
            return true;
        }

        //b.执行铸币交易
        public void execute_CoinbaseTransaction(Transaction transaction)
        {
            uint indexOfOutput = 0;
            foreach (TxOut transactionOutput in transaction.Outputs)
            {
                string txhashAndIndex = transaction.GetHash().ToString() + "-" + indexOfOutput;
                string txhash = transaction.GetHash().ToString();
                ulong value = transactionOutput.Value;
                string script = new ByteArray(transactionOutput.ScriptPubKey.ToBytes()).ToString();
                if (value == 0)
                {
                    if (transactionOutput.ScriptPubKey.ToBytes()[0] == 0x6a || transactionOutput.ScriptPubKey.ToBytes()[1] == 0x6a)
                    {
                        opreturnOutputItem_Class opreturnOutputItem = new opreturnOutputItem_Class(txhash, indexOfOutput, value, script);
                        opreturnOutputLinkedList.AddLast(opreturnOutputItem);
                    }
                    else
                    {
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
                    }
                }
                else
                {
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
                }
                indexOfOutput++;
            }
        }

        //c.执行常规交易
        public void execute_RegularTransaction(Transaction transaction)
        {
            foreach (TxIn transactionInput in transaction.Inputs)
            {
                string sourceTxhashAndIndex = transactionInput.PrevOut.ToString();
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
            foreach (TxOut transactionOutput in transaction.Outputs)
            {
                string txhashAndIndex = transaction.GetHash().ToString() + "-" + indexOfOutput;
                string txhash = transaction.GetHash().ToString();
                ulong value = transactionOutput.Value;
                string script = new ByteArray(transactionOutput.ScriptPubKey.ToBytes()).ToString();
                if (value == 0)
                {
                    if (transactionOutput.ScriptPubKey.ToBytes()[0] == 0x6a || transactionOutput.ScriptPubKey.ToBytes()[1] == 0x6a)
                    {
                        opreturnOutputItem_Class opreturnOutputItem = new opreturnOutputItem_Class(txhash, indexOfOutput, value, script);
                        opreturnOutputLinkedList.AddLast(opreturnOutputItem);
                    }
                    else
                    {
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
                    }
                }
                else
                {
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
                }
                indexOfOutput++;
            }
        }

        //d.执行一个区块的交易
        public void execute_TransactionsOfOneBlock(ParserBlock block)
        {
            foreach (Transaction transaction in block.Transactions)
            {
                if (transaction.IsCoinBase)
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

        //e.保存切片状态
        public void save_SliceFile(int processedBlockAmount, int sliceFileAmount, DateTime endBlockTime)
        {
            string UtxoSliceFileFinalPath = Path.Combine(UtxoSliceFilePath, "UtxoSlice_" + processedBlockAmount + "_" + endBlockTime.ToString("yyyy年MM月dd日HH时mm分ss秒") + ".dat");
            SliceFileItem_Class sliceFileItem = new SliceFileItem_Class(utxoDictionary, sliceFileAmount, sameTransactionCount);
            using (StreamWriter sw = File.CreateText(UtxoSliceFileFinalPath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(sw, sliceFileItem);
            }
            orderedBlockchainParser.Compress(UtxoSliceFileFinalPath, true);
        }

        //f.保存opreturn切片状态
        public void save_opreturnOutputsFile(int processedBlockAmount, DateTime endBlockTime)
        {
            string OpReturnFileFinalPath = Path.Combine(OpReturnFilePath, "OpReturn_" + processedBlockAmount + "_" + endBlockTime.ToString("yyyy年MM月dd日HH时mm分ss秒") + ".dat");
            using (StreamWriter sw = File.CreateText(OpReturnFileFinalPath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(sw, opreturnOutputLinkedList);
            }
            orderedBlockchainParser.Compress(OpReturnFileFinalPath, true);
            opreturnOutputLinkedList = new LinkedList<opreturnOutputItem_Class>();
        }

        //g.终止条件判断函数
        public bool endConditionJudgment(DateTime newlyRecentlyDatetime, DateTime recentlyBlockDatetime)
        {
            TimeSpan timeSpan = recentlyBlockDatetime - newlyRecentlyDatetime;
            if (sliceIntervalTimeType == "year")
            {
                double amountOfYear = timeSpan.TotalDays / 365;
                if (amountOfYear >= (double)sliceIntervalTime)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (sliceIntervalTimeType == "month")
            {
                double amountOfMonth = timeSpan.TotalDays / 30;
                if (amountOfMonth >= (double)sliceIntervalTime)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (sliceIntervalTimeType == "day")
            {
                if (timeSpan.TotalDays >= (double)sliceIntervalTime)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                Console.WriteLine("请输入正确的切片间隔类型!!!(year/month/day)");
                return false;
            }
        }

        //h.恢复UTXOSlicer上下文
        public void restore_UTXOSlicerContextForProgram()
        {
            string utxoSlicerContextFileFinalPath = Path.Combine(UtxoSliceFilePath, UtxoSliceFileName);
            //判断给定文件名是压缩文件还是txt文件
            FileInfo fileName = new FileInfo(utxoSlicerContextFileFinalPath);
            if (fileName.Extension == ".rar")
            {
                Console.WriteLine("正在解压UtxoSlice下文状态文件......");
                orderedBlockchainParser.Decompress(utxoSlicerContextFileFinalPath, false);
                utxoSlicerContextFileFinalPath = Path.Combine(UtxoSliceFilePath, Path.GetFileNameWithoutExtension(utxoSlicerContextFileFinalPath));
            }
            if (File.Exists(utxoSlicerContextFileFinalPath))
            {
                //1.反序列化UtxoSlice文件
                Console.WriteLine("开始提取程序上下文状态文件数据(UtxoSlice).........");
                SliceFileItem_Class sliceFileItemObject = null;
                Stopwatch timer = new Stopwatch();
                timer.Start();
                try
                {
                    using (StreamReader sr = File.OpenText(utxoSlicerContextFileFinalPath))
                    {
                        JsonSerializer jsonSerializer = new JsonSerializer();
                        sliceFileItemObject = jsonSerializer.Deserialize(sr, typeof(SliceFileItem_Class)) as SliceFileItem_Class;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("UtxoSlice文件保存不完整或已经损坏。该错误可能是由于在保存UtxoSlice文件时提前终止程序造成的，或是人为修改了最近的UtxoSlice文件。");
                }
                timer.Stop();
                Console.WriteLine("提取结束,反序列化切片用时:" + timer.Elapsed);
                //恢复UTXO字典
                utxoDictionary = sliceFileItemObject.utxoDictionary;
                //恢复其它
                sliceFileAmount = sliceFileItemObject.sliceFileAmount;
                sameTransactionCount = sliceFileItemObject.sameTransactionCount;
                sliceFileItemObject.Dispose();
                File.Delete(utxoSlicerContextFileFinalPath);//删除解压后的文件UtxoSlice文件
                Console.WriteLine("UtxoSlice上下文状态恢复成功.........");
            }
            else
            {
                Console.WriteLine(utxoSlicerContextFileFinalPath + " 文件不存在!!!");
            }
        }

        //i.参数检测
        public void parameter_Detection()
        {
            bool success = true;
            if (!Directory.Exists(blockchainFilePath))
            {
                Console.WriteLine(blockchainFilePath + " 不存在!!!");
                success = false;
            }
            if (!Directory.Exists(blockProcessContextFilePath) && blockProcessContextFileName == null)
            {
                Directory.CreateDirectory(blockProcessContextFilePath);
            }
            if (!Directory.Exists(blockProcessContextFilePath) && blockProcessContextFileName != null)
            {
                Console.WriteLine(blockProcessContextFilePath + " 不存在!!!");
                success = false;
            }
            if (Directory.Exists(blockProcessContextFilePath) && blockProcessContextFileName != null)
            {
                string path = Path.Combine(blockProcessContextFilePath, blockProcessContextFileName);
                if (!File.Exists(path))
                {
                    Console.WriteLine(path + " 不存在!!!");
                    success = false;
                }
            }
            if (!Directory.Exists(UtxoSliceFilePath) && UtxoSliceFileName == null)
            {
                Directory.CreateDirectory(UtxoSliceFilePath);
            }
            if (!Directory.Exists(UtxoSliceFilePath) && UtxoSliceFileName != null)
            {
                Console.WriteLine(UtxoSliceFilePath + "不存在或错误!!!");
                success = false;
            }
            if (Directory.Exists(UtxoSliceFilePath) && UtxoSliceFileName != null)
            {
                string path = Path.Combine(UtxoSliceFilePath, UtxoSliceFileName);
                if (!File.Exists(path))
                {
                    Console.WriteLine(path + " 不存在!!!");
                    success = false;
                }
            }
            if (!Directory.Exists(OpReturnFilePath))
            {
                Directory.CreateDirectory(OpReturnFilePath);
            }
            if (sliceIntervalTimeType != "year" && sliceIntervalTimeType != "month" && sliceIntervalTimeType != "day")
            {
                Console.WriteLine("时间间隔类型参数错误(year/month/day)!!!");
                success = false;
            }
            if (sliceIntervalTime < 0)
            {
                Console.WriteLine("时间间隔不能小于0");
                success = false;
            }
            if (endTime < new DateTime(2009, 1, 3, 18, 15, 05))
            {
                Console.WriteLine("时间不能早于 2009/1/3 18:15:05!!!");
                success = false;
            }
            if (endBlockHeight < 0)
            {
                Console.WriteLine("区块高度不能小于0!!!");
                success = false;
            }
            if (success == false)
            {
                Environment.Exit(0);
            }
        }

        public void UTXOSlicer_Class_Test()
        {
            //1.从blk00000.dat启动
            //UTXOSlicer_Class uTXOSlicer = new UTXOSlicer_Class(@"F:\data\blocks", @"F:\blockProcessContextFile", null,
            //                                                   @"F:\SliceStateFile", null, @"F:\opreturnOutputFile",
            //                                                   Configuration_Class.Month, 1, new DateTime(2009, 5, 3), 3500);
            //2.恢复启动
            UTXOSlicer_Class uTXOSlicer = new UTXOSlicer_Class(@"F:\data\blocks", @"F:\blockProcessContextFile", "BPC_418031_2016年06月26日07时57分38秒.dat.rar",
                                                               @"F:\SliceStateFile", "UtxoSlice_418031_2016年06月26日07时57分38秒.dat.rar", @"F:\opreturnOutputFile",
                                                               Configuration_Class.Month, 1, new DateTime(2021, 2, 1), 681572);
            //3.参数检查
            //UTXOSlicer_Class uTXOSlicer = new UTXOSlicer_Class(@"F:\data\blocks", @"F:\blockProcessContextFile", "BPC_279223_2014年01月08日03时01分28秒.dat.rar",
            //                                                  @"F:\SliceStateFile", "UtxoSlice_279223_2014年01月08日03时01分28秒.dat.rar", @"F:\opreturnOutputFile",
            //                                                  Configuration_Class.Month, 2, new DateTime(2015, 2, 1), -681572);
            uTXOSlicer.updateUTXO();
        }
    }
}
