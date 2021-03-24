using BitcoinBlockchain.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BitcoinUtxoSlice
{
    class Program
    {
        static void Main(string[] args)
        {
            ////I.文件预处理
            Blockfile_Manager_Class bmc = new Blockfile_Manager_Class();
            //1.给出需要分装的文**数量**
            //bmc.blockfiles_preprocessing(50);
            //2.给出分装的**最大文件编号**
            //bmc.blockfiles_preprocessing_increment(120);
            //3.初次或重新统计每个文件中的区块数量
            //bmc.get_blockfiles_count(true);
            //4.增量统计每个文件中的区块数量
            //bmc.get_blockfiles_count_increment(true);
            //5.统计分装的区块总数
            //bmc.get_totalcount();

            ////II.初次运行和增量处理
            Block_Processing_Class bpc = new Block_Processing_Class();
            //bpc.initial_Run();

            ////III.测试
            Test_Class tc = new Test_Class();
            tc.sameTransaction_Test();

            Console.WriteLine("uint.MaxValue:" + uint.MaxValue);

        }
    }
}
