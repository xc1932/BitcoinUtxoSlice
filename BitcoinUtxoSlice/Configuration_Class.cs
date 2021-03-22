using System;
using System.Collections.Generic;
using System.Text;

namespace BitcoinUtxoSlice
{
    class Configuration_Class
    {
        //查找文件上限
        internal const int newlyLoadFileAmountCeiling = 2;
        //区块队列长度
        internal const int blockQueuePoolingSize = 10;
        //Bitcoin Core 同步数据文件夹路径
        internal const string roamingBlockFilePath = @"E:\BitcoinRoaming\blocks";
        //预处理后的区块文件夹路径
        internal const string preprocessedBlockFilePath = @"E:\Code\BlockFile";
        //切片文件存储路径
        internal const string sliceStateFilePath = @"E:\Code\BlockChainProject\workspace\SliceStateFile";
        //切片长度
        internal const int UTXOSliceLength = 4096;
    }
}
