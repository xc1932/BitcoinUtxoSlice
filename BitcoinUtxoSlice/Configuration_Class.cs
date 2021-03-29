using System;
using System.Collections.Generic;
using System.Text;

namespace BitcoinUtxoSlice
{
    public class Configuration_Class
    {
        //查找文件上限
        internal const int newlyLoadFileAmountCeiling = 2;
        //区块队列长度
        internal const int blockQueuePoolingSize = 10;
        //Bitcoin Core 同步数据文件夹路径
        //internal const string roamingBlockFilePath = @"E:\BitcoinRoaming\blocks";
        internal static string roamingBlockFilePath = Environment.CurrentDirectory + "\\blocks";
        //预处理后的区块文件夹路径
        //internal const string preprocessedBlockFilePath = @"E:\Code\BlockFile";
        internal static string preprocessedBlockFilePath = Environment.CurrentDirectory + "\\BlockFile";
        //切片文件存储路径
        //internal const string sliceStateFilePath = @"E:\Code\BlockChainProject\workspace\SliceStateFile";
        internal static string sliceStateFilePath = Environment.CurrentDirectory + "\\SliceStateFile";
        //切片长度
        internal const int UTXOSliceLength = 4096;
    }
}
