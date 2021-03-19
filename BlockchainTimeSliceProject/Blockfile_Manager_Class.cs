using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using BitcoinBlockchain.Data;
using BitcoinBlockchain.Parser;
using System.Linq;
using System.Diagnostics;

namespace BitcoinUtxoSlice
{
    class Blockfile_Manager_Class
    {
        public string blockFileSourcePath { get; set; }
        public string blockFileDestinationPath { get; set; }

        public Blockfile_Manager_Class()
        {
        }

        public Blockfile_Manager_Class(string blockFileSourcePath, string blockFileDestinationPath)
        {
            this.blockFileSourcePath = blockFileSourcePath;
            this.blockFileDestinationPath = blockFileDestinationPath;
        }

        //1.将每个区块链文件分装到一个对应编号的文件夹，文件夹下的文件全命名为blk00000.dat
        public void blockfiles_preprocessing(int blockfileNumber)
        {
            if (!Directory.Exists(blockFileSourcePath))
            {
                Console.WriteLine("区块文件源路径不存在");
            }
            if (!Directory.Exists(blockFileDestinationPath))
            {
                Directory.CreateDirectory(blockFileDestinationPath);
            }
            for (int i = 0; i < blockfileNumber; i++)
            {
                string destinationPathSubfolder = Path.Combine(blockFileDestinationPath, "blk" + i);
                if (!Directory.Exists(destinationPathSubfolder))
                {
                    Directory.CreateDirectory(destinationPathSubfolder);
                }
                string fileName = get_filename(i);
                if (fileName != null)
                {
                    string sourceFileName = Path.Combine(blockFileSourcePath, fileName);
                    string destnationFileName = Path.Combine(destinationPathSubfolder, "blk00000.dat");
                    if (File.Exists(sourceFileName))
                    {
                        File.Copy(sourceFileName, destnationFileName, true);
                    }
                }
            }
            Console.WriteLine("文件处理结束................");
        }

        //a.给出dat文件序号，返回文件名
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

        //2.读取一个编号blk*****文件夹下的blk00000.dat文件
        public List<Block> load_one_blockfile(string blockFilePath)
        {
            List<Block> oneFileBlockData = new List<Block>();
            IBlockchainParser blockFileParser = new BlockchainParser(blockFilePath);
            IEnumerable<Block> blocks = blockFileParser.ParseBlockchain();
            oneFileBlockData = blocks.ToList();
            return oneFileBlockData;
        }

        //3.测试文件中的区块总数
        public int calculate_AmountOfBlockFiles(int blockFileAmount) {
            int amountOfBlockFiles = 0;
            Stopwatch timer = new Stopwatch();
            timer.Start();
            for (int i=0;i< blockFileAmount;i++) {
                Console.WriteLine("正在解析第"+i+"个文件........");
                string loadFilePath = @"E:\Code\BlockFile\blk" + i;
                if (Directory.Exists(loadFilePath))
                {
                    Stopwatch timer1 = new Stopwatch();
                    timer1.Start();
                    List<Block> newlyReadBlocksFromFile = load_one_blockfile(loadFilePath);
                    timer1.Stop();
                    amountOfBlockFiles += newlyReadBlocksFromFile.Count();
                    Console.WriteLine("第" + i + "文件区块数量:" + newlyReadBlocksFromFile.Count());
                    Console.WriteLine("解析第"+i+"个区块用时:"+timer1.Elapsed);  
                }
            }
            timer.Stop();
            Console.WriteLine("区块总数:" + amountOfBlockFiles);
            Console.WriteLine("解析区块总用时:"+timer.Elapsed);
            return amountOfBlockFiles;
        }
    }
}
