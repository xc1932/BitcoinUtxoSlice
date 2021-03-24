using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using BitcoinBlockchain.Data;
using BitcoinBlockchain.Parser;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BitcoinUtxoSlice
{
    class Blockfile_Manager_Class
    {
        public string blockFileSourcePath { get; set; }
        public string blockFileDestinationPath { get; set; }

        public Blockfile_Manager_Class()
        {
            this.blockFileSourcePath = Configuration_Class.roamingBlockFilePath;
            this.blockFileDestinationPath = Configuration_Class.preprocessedBlockFilePath;
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
                Console.WriteLine("区块文件夹源路径不存在");
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

        //2.增量分装区块链文件
        public void blockfiles_preprocessing_increment(int blockfileNumber)
        {
            int maxHeight = 0;
            if (get_maxheight_blkfolder(out maxHeight))
            {
                for (int i = maxHeight + 1; i < blockfileNumber + 1; i++)
                {
                    string destinationPathSubfolder = Path.Combine(blockFileDestinationPath, "blk" + i);
                    if (!Directory.Exists(destinationPathSubfolder))
                    {
                        Directory.CreateDirectory(destinationPathSubfolder);
                    }
                    else
                    {
                        Console.WriteLine(destinationPathSubfolder + "已经存在");
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
                    else
                    {
                        Console.WriteLine("文件名不存在");
                    }
                }
                Console.WriteLine("文件分装增量处理结束");
            }
            else
            {
                Console.WriteLine("没有找到最大的文件夹高度");
            }
        }

        //3.统计分装路径下每个区块文件的区块数量
        public void get_blockfiles_count(bool printInfoMark)
        {
            File.WriteAllText(blockFileDestinationPath + "\\blockFileCount.txt", string.Empty);
            Dictionary<string, int> blockFileCountDictionary = new Dictionary<string, int>();
            if (Directory.Exists(blockFileDestinationPath))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(blockFileDestinationPath);
                if (directoryInfo.GetDirectories().Length != 0)
                {
                    foreach (DirectoryInfo dirInfo in directoryInfo.GetDirectories().OrderBy(v => Convert.ToInt32(v.Name.Substring(3, v.Name.Length - 3))))
                    {
                        if (dirInfo.Name.StartsWith("blk"))
                        {
                            string blockFileFolder = Path.Combine(blockFileDestinationPath, dirInfo.Name);
                            Console.WriteLine("正在计算" + dirInfo.Name + "中的区块数量.........");
                            List<Block> blocks = new BlockchainParser(blockFileFolder).ParseBlockchain().ToList();
                            blockFileCountDictionary.Add(dirInfo.Name, blocks.Count);
                            if (printInfoMark)
                            {
                                Console.WriteLine(dirInfo.Name + ":" + blocks.Count);
                            }
                            File.AppendAllText(blockFileDestinationPath + "\\blockFileCount.txt", dirInfo.Name + "_" + blocks.Count + "||");
                            Console.WriteLine(dirInfo.Name + "中的区块数量记录完成");
                        }
                        else
                        {
                            Console.WriteLine("没有合法的分装文件夹");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("分装文件夹为空");
                }
            }
            else
            {
                Console.WriteLine("分装路径不正确或不存在");
            }
            Console.WriteLine("执行结束");
        }

        //4.增量统计分装路径下每个区块文件的区块数量(待改)
        public void get_blockfiles_count_increment(bool printInfoMark)
        {
            Console.WriteLine("开始增量统计文件中的区块.........");
            int maxFolderNumber = 0;
            if (get_lastrecord_foldernumber(out maxFolderNumber))
            {
                Dictionary<string, int> blockFileCountDictionary = new Dictionary<string, int>();
                if (Directory.Exists(blockFileDestinationPath))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(blockFileDestinationPath);
                    if (directoryInfo.GetDirectories().Length != 0)
                    {
                        foreach (DirectoryInfo dirInfo in directoryInfo.GetDirectories().OrderBy(v => Convert.ToInt32(v.Name.Substring(3, v.Name.Length - 3))))
                        {
                            if (dirInfo.Name.StartsWith("blk") && Convert.ToInt32(dirInfo.Name.Substring(3, dirInfo.Name.Length - 3)) > maxFolderNumber)
                            {
                                string blockFileFolder = Path.Combine(blockFileDestinationPath, dirInfo.Name);
                                Console.WriteLine("正在计算" + dirInfo.Name + "中的区块数量.........");
                                List<Block> blocks = new BlockchainParser(blockFileFolder).ParseBlockchain().ToList();
                                blockFileCountDictionary.Add(dirInfo.Name, blocks.Count);
                                if (printInfoMark)
                                {
                                    Console.WriteLine(dirInfo.Name + ":" + blocks.Count);
                                }
                                File.AppendAllText(blockFileDestinationPath + "\\blockFileCount.txt", dirInfo.Name + "_" + blocks.Count + "||");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("分装文件夹为空");
                    }
                }
                else
                {
                    Console.WriteLine("分装路径不正确或不存在");
                }
            }
            else
            {
                Console.WriteLine("没有在blockFileCount.txt中找到最大的区块文件编号");
            }
        }

        //5.读取一个编号blk*****文件夹下的blk00000.dat文件
        public List<Block> load_one_blockfile(string blockFilePath)
        {
            List<Block> oneFileBlockData = new List<Block>();
            IBlockchainParser blockFileParser = new BlockchainParser(blockFilePath);
            IEnumerable<Block> blocks = blockFileParser.ParseBlockchain();
            oneFileBlockData = blocks.ToList();
            return oneFileBlockData;
        }

        //6.统计blockFileCount.txt文件中记录的区块总数
        public void get_totalcount()
        {
            string recordStr = File.ReadAllText(blockFileDestinationPath + "\\blockFileCount.txt");
            string[] recordsList = recordStr.Split("||");
            int totalBlock = 0;
            for (int i = 0; i < recordsList.Length - 1; i++)
            {
                string[] recordItem = recordsList[i].Split("_");
                string fileName = recordItem[0];
                int blockAmount = Convert.ToInt32(recordItem[1]);
                totalBlock += blockAmount;
                Console.WriteLine(fileName + ":" + blockAmount);
            }
            Console.WriteLine("total amount:" + totalBlock);
        }

        //a.获取blockFileCount.txt中最后统计的区块文件编号
        public bool get_lastrecord_foldernumber(out int folderNumber)
        {
            string recordStr = File.ReadAllText(blockFileDestinationPath + "\\blockFileCount.txt");
            string[] recordsList = recordStr.Split("||");
            if (recordsList.Length != 0)
            {
                string recordItem = recordsList[recordsList.Length - 2];
                string folderName = recordItem.Split("_")[0];
                folderNumber = Convert.ToInt32(folderName.Substring(3, folderName.Length - 3));
                return true;
            }
            else
            {
                folderNumber = 0;
                Console.WriteLine("无记录");
                return false;
            }
        }

        //b.获取最大文件夹高度
        public bool get_maxheight_blkfolder(out int maxHeight)
        {
            maxHeight = 0;
            if (!Directory.Exists(blockFileSourcePath))
            {
                Console.WriteLine("区块文件夹源路径不存在");
                return false;
            }
            if (!Directory.Exists(blockFileDestinationPath))
            {
                Console.WriteLine("区块分装后的文件夹路径不存在");
                return false;
            }
            DirectoryInfo directoryInfo = new DirectoryInfo(blockFileDestinationPath);
            if (directoryInfo.GetDirectories().Length == 0)
            {
                Console.WriteLine("预分装后的文件夹为空");
                return false;
            }
            List<int> fileHeightList = new List<int>();
            foreach (DirectoryInfo dirInfo in directoryInfo.GetDirectories())
            {
                if (dirInfo.Name.StartsWith("blk"))
                {
                    string fileNumber = dirInfo.Name.Substring(3, dirInfo.Name.Length - 3);
                    if (Regex.IsMatch(fileNumber, @"^\d+$"))
                    {
                        int number = Convert.ToInt32(fileNumber);
                        fileHeightList.Add(number);
                    }
                    else
                    {
                        Console.WriteLine("没有规范的文件夹编号");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("当前文件加下没有正确的区块文件分装路径(blk****)");
                    return false;
                }
            }
            if (fileHeightList.Count == 0)
            {
                Console.WriteLine("没有合法编号的文件夹");
                return false;
            }
            foreach (int height in fileHeightList)
            {
                if (height > maxHeight)
                {
                    maxHeight = height;
                }
            }
            return true;
        }

        //c.给出dat文件序号，返回文件名
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
    }
}
